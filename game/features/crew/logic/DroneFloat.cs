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

    /// <summary>How far it is from its place, pixels, as last followed.</summary>
    public double Off { get; private set; }

    /// <summary>Where it is, pixels of the world; none before the first <see cref="Follow"/>.</summary>
    public (double X, double Y)? At { get; private set; }

    /// <summary>How long after it is sent to another place it flies there unleashed, ms, so the leash never jerks it across, and how fast it eases meanwhile.</summary>
    public const double FreeMs = 500, SwitchMs = 70;

    private bool _elsewhere;
    private double _freeMs;

    /// <summary>
    /// Moves it after the neck at <paramref name="neck"/>, or to <paramref name="instead"/> (projecting a drone window before the
    /// hero, drone-holo), by <paramref name="elapsedMs"/>, <paramref name="timeMs"/> into its bob.
    /// </summary>
    public (double X, double Y) Follow((double X, double Y) neck, int facing, double elapsedMs, double timeMs, (double X, double Y)? instead = null)
    {
        double bob = BobPx * Math.Sin(2 * Math.PI * timeMs / BobMs);
        var place = instead is { } there ? (X: there.X, Y: there.Y + bob) : (X: neck.X - facing * Behind, Y: neck.Y - Above + bob);
        if (_elsewhere != instead.HasValue) (_elsewhere, _freeMs) = (instead.HasValue, FreeMs);
        _freeMs = Math.Max(0, _freeMs - elapsedMs);
        var at = At is { } was && Apart(was, place) <= Lost ? was : place;
        double k = 1 - Math.Exp(-elapsedMs / (_freeMs > 0 ? SwitchMs : FollowMs));
        at = (at.X + (place.X - at.X) * k, at.Y + (place.Y - at.Y) * k);
        double off = Apart(at, place);
        if (off > Leash && _freeMs <= 0) at = (place.X + (at.X - place.X) * Leash / off, place.Y + (at.Y - place.Y) * Leash / off);
        (At, Off) = (at, Apart(at, place));
        return at;
    }

    private static double Apart((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
