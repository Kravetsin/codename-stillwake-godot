using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Crew;

/// <summary>What a marker's badge holds: the pod's silhouette for home, a figure lying for this player's body, a dot in the suit's colour for a friend.</summary>
public enum MarkerIcon { Home, Body, Friend }

/// <summary>
/// A place this player may want to find again, pointed at from the screen's edge while it is off the screen:
/// home, the body this player left last, a friend.
/// </summary>
/// <param name="X">Where it is, in cells of the world.</param>
/// <param name="Colour">The chevron's and the icon's colour, 0xRRGGBB.</param>
/// <param name="Peer">The friend's peer, which tells two friends' markers apart; none for home and the body.</param>
public sealed record Marker(MarkerIcon Icon, double X, double Y, int Colour, int? Peer = null);

/// <summary>A rectangle on the screen, in pixels: what a marker keeps out of, the HUD's panels.</summary>
public readonly record struct Box(double Left, double Top, double Right, double Bottom)
{
    public bool Holds(double x, double y) => x > Left && x < Right && y > Top && y < Bottom;

    public Box Grown(double by) => new(Left - by, Top - by, Right + by, Bottom + by);
}

/// <summary>Where a marker is drawn: hidden while its target is on the screen, else at the edge pointing at it.</summary>
/// <param name="Angle">Toward the target from where it is drawn, in radians, 0 to the right and down positive, as the screen's y runs.</param>
public readonly record struct Placed(bool OnScreen, double X, double Y, double Angle);

/// <summary>The markers' list and their geometry at the screen's edge, without Godot, so it is tested.</summary>
public static class Markers
{
    /// <summary>Home's colour: a pale white, apart from every suit (<see cref="Suits.All"/>) and from danger's red.</summary>
    public const int HomeColour = 0xe2e8f0;

    /// <summary>How long a marker takes to fade in once its target leaves the screen.</summary>
    public const double FadeInS = 0.25;

    /// <summary>
    /// This player's markers: home at the crew's <paramref name="spawn"/> cell, where a hero is made again; then the
    /// body it left last (<see cref="Bodies.Latest"/>), half a cell above where its stacks lie, in its suit's
    /// colour, while there is one; then each friend at its body's middle in its suit's colour, in the order given.
    /// </summary>
    public static List<Marker> Of(int spawn, int width, IReadOnlyList<(int Id, Hero Hero, int Colour)> friends, (Pile Pile, int Colour)? body = null)
    {
        var list = new List<Marker>(friends.Count + 2) { new(MarkerIcon.Home, spawn % width + 0.5, spawn / width + 0.5, HomeColour) };
        if (body is var (pile, suit)) list.Add(new Marker(MarkerIcon.Body, pile.X / Hero.Unit, pile.Y / Hero.Unit - 0.5, suit));
        foreach (var (id, hero, colour) in friends)
        {
            var middle = hero.Middle;
            list.Add(new Marker(MarkerIcon.Friend, (double)middle.X / Hero.Unit, (double)middle.Y / Hero.Unit, colour, id));
        }
        return list;
    }

    /// <summary>A world point in cells as it lands on a screen <paramref name="w"/> by <paramref name="h"/>, the camera centred on <paramref name="cameraX"/>, <paramref name="cameraY"/> (cells) at <paramref name="pixelsPerCell"/>.</summary>
    public static (double X, double Y) ToScreen(double x, double y, double cameraX, double cameraY, double pixelsPerCell, double w, double h) =>
        ((x - cameraX) * pixelsPerCell + w / 2, (y - cameraY) * pixelsPerCell + h / 2);

    /// <summary>Whether a point lands on a screen <paramref name="w"/> by <paramref name="h"/>, its edges counted on it.</summary>
    public static bool OnScreen(double x, double y, double w, double h) => x >= 0 && x <= w && y >= 0 && y <= h;

    /// <summary>
    /// Where the marker of a target at <paramref name="x"/>, <paramref name="y"/> on the screen is drawn: nowhere while
    /// it is on the screen <paramref name="w"/> by <paramref name="h"/>; else where the line from the middle of
    /// <paramref name="inner"/> (the screen less its margin, less what covers its bottom) to the target crosses
    /// <paramref name="inner"/>'s edge, then slid along that edge out of every box of <paramref name="keepOut"/>
    /// grown by <paramref name="radius"/>, the marker's own reach, and turned toward the target from there.
    /// </summary>
    public static Placed Place(double x, double y, double w, double h, Box inner, double radius, IReadOnlyList<Box> keepOut)
    {
        if (OnScreen(x, y, w, h)) return new Placed(true, x, y, 0);
        double cx = (inner.Left + inner.Right) / 2, cy = (inner.Top + inner.Bottom) / 2;
        double halfW = (inner.Right - inner.Left) / 2, halfH = (inner.Bottom - inner.Top) / 2;
        double dx = x - cx, dy = y - cy;
        double tx = dx == 0 ? double.PositiveInfinity : halfW / Math.Abs(dx);
        double ty = dy == 0 ? double.PositiveInfinity : halfH / Math.Abs(dy);
        // On a left or right edge the marker slides up and down it; on the top or bottom, along it.
        bool upright = tx <= ty;
        double t = Math.Min(tx, ty);
        double px = upright ? (dx > 0 ? inner.Right : inner.Left) : cx + dx * t;
        double py = upright ? cy + dy * t : (dy > 0 ? inner.Bottom : inner.Top);
        if (upright) py = Slid(py, inner.Top, inner.Bottom, keepOut.Select(b => b.Grown(radius)).Where(b => b.Left < px && px < b.Right).Select(b => (b.Top, b.Bottom)));
        else px = Slid(px, inner.Left, inner.Right, keepOut.Select(b => b.Grown(radius)).Where(b => b.Top < py && py < b.Bottom).Select(b => (b.Left, b.Right)));
        return new Placed(false, px, py, Math.Atan2(y - py, x - px));
    }

    /// <summary>
    /// The point of <paramref name="min"/>..<paramref name="max"/> nearest <paramref name="at"/> inside none of
    /// <paramref name="spans"/>: <paramref name="at"/> itself, else the nearest end of a span; <paramref name="at"/>
    /// still when the spans cover the whole edge.
    /// </summary>
    private static double Slid(double at, double min, double max, IEnumerable<(double Low, double High)> spans)
    {
        var covered = spans.ToList();
        bool Free(double p) => p >= min && p <= max && !covered.Any(s => s.Low < p && p < s.High);
        double best = at, by = double.PositiveInfinity;
        foreach (double p in covered.SelectMany(s => new[] { s.Low, s.High }).Prepend(at))
            if (Free(p) && Math.Abs(p - at) < by) (best, by) = (p, Math.Abs(p - at));
        return best;
    }

    /// <summary>A marker's opacity after <paramref name="seconds"/>: gone at once while its target is on the screen, faded in over <see cref="FadeInS"/> while it is off.</summary>
    public static double Fade(double shown, bool onScreen, double seconds) =>
        onScreen ? 0 : Math.Min(1, shown + seconds / FadeInS);

    /// <summary>How many cells lie between two points of the world in cells, rounded to the nearest: what the tip says.</summary>
    public static int CellsBetween(double fromX, double fromY, double toX, double toY)
    {
        double dx = toX - fromX, dy = toY - fromY;
        return (int)Math.Round(Math.Sqrt(dx * dx + dy * dy), MidpointRounding.AwayFromZero);
    }
}
