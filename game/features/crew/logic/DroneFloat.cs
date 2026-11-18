using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Where the light drone floats by its hero's head (light-drone, a try): above and behind the helmet, bobbing, easing after
/// the head as it moves and never further than <see cref="Leash"/> from its place; a jump far away (a respawn) puts it there.
/// It ignores walls: a tunnel's ceiling may hide it, an accepted limitation of the stand-in.
/// </summary>
public sealed class DroneFloat
{
    /// <summary>Its place from the neck, pixels: behind the head the way the hero faces, and above it.</summary>
    public const double Behind = 6, Above = 16;

    /// <summary>How fast it eases after its place, the time to close about two thirds of the way, ms.</summary>
    public const double FollowMs = 150;

    /// <summary>Its bob, pixels up and down, and how long one takes, ms.</summary>
    public const double BobPx = 1, BobMs = 1800;

    /// <summary>The furthest it lags behind its place, pixels; further than <see cref="Lost"/> it jumps there.</summary>
    public const double Leash = 14, Lost = 64;

    /// <summary>Where it is, pixels of the world; none before the first <see cref="Follow"/>.</summary>
    public (double X, double Y)? At { get; private set; }

    /// <summary>Moves it after the neck at <paramref name="neck"/> by <paramref name="elapsedMs"/>, <paramref name="timeMs"/> into its bob.</summary>
    public (double X, double Y) Follow((double X, double Y) neck, int facing, double elapsedMs, double timeMs)
    {
        var place = (X: neck.X - facing * Behind, Y: neck.Y - Above + BobPx * Math.Sin(2 * Math.PI * timeMs / BobMs));
        var at = At is { } was && Apart(was, place) <= Lost ? was : place;
        double k = 1 - Math.Exp(-elapsedMs / FollowMs);
        at = (at.X + (place.X - at.X) * k, at.Y + (place.Y - at.Y) * k);
        double off = Apart(at, place);
        if (off > Leash) at = (place.X + (at.X - place.X) * Leash / off, place.Y + (at.Y - place.Y) * Leash / off);
        At = at;
        return at;
    }

    private static double Apart((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
