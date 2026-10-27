using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// How thick a planet's fog is (fog-edges), 0 clear to 1 whole, by a position in cells: on a column (<see cref="Column"/>), and
/// over the whole screen by where its middle is (<see cref="Screen"/>). Each side the same, by the distance from the grid's
/// nearer edge. The fog's shader draws <see cref="Column"/> again, pixel by pixel; the sound muffles by it.
/// </summary>
public static class FogShare
{
    /// <summary>
    /// The fog on the column at <paramref name="x"/>, in cells: whole over a band and past the grid, so its hollow is never seen;
    /// thinning over the <see cref="FogEdges.Out"/> columns past it to <see cref="FogLooks.Edge"/>, where a moved hero falls out of it;
    /// gone <see cref="FogLooks.Lead"/> columns further in.
    /// </summary>
    public static double Column(FogEdges e, double x)
    {
        double t = Inward(e, x);
        double edge = e.Fog + e.Out;
        if (t >= edge + FogLooks.Lead) return 0;
        if (t >= edge) return FogLooks.Edge * Smooth((edge + FogLooks.Lead - t) / FogLooks.Lead);
        if (t > e.Fog) return FogLooks.Edge + (1 - FogLooks.Edge) * Smooth((edge - t) / Math.Max(1, e.Out));
        return 1;
    }

    /// <summary>The fog over the whole screen with its middle at <paramref name="x"/>, in cells: none in the zone, easing to whole at the seam.</summary>
    public static double Screen(FogEdges e, double x)
    {
        double t = Inward(e, x);
        if (t >= e.Fog) return 0;
        return t >= e.Fog / 2.0 ? Smooth((e.Fog - t) / (e.Fog / 2.0)) : 1;
    }

    /// <summary>How far <paramref name="x"/> lies in from the grid's nearer side, in cells.</summary>
    private static double Inward(FogEdges e, double x) => Math.Min(x, e.Width - x);

    private static double Smooth(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }
}

/// <summary>
/// The fog over the whole screen as it is drawn, frame by frame (fog-edges): <see cref="FogShare.Screen"/> where the screen's middle
/// is, and after a move across the fog — its middle gone more than half the grid's width in a frame, the fog over it before or
/// after — held whole for <see cref="FogLooks.FlashMs"/>, then eased back over <see cref="FogLooks.FadeMs"/>. A beam-down, a load
/// or a wake at the spawn, the fog on neither side, flashes nothing; nor does the first frame.
/// </summary>
public sealed class FogScreen(FogEdges edges)
{
    private double? _lastX;
    private double _holdMs, _fadeMs;

    /// <summary>The fog over the screen this frame, its middle at <paramref name="x"/> in cells, <paramref name="elapsedMs"/> after the last.</summary>
    public double Step(double x, double elapsedMs)
    {
        double now = FogShare.Screen(edges, x);
        if (_lastX is double last && Math.Abs(x - last) > edges.Width / 2.0 && (now > 0 || FogShare.Screen(edges, last) > 0))
            (_holdMs, _fadeMs) = (FogLooks.FlashMs, FogLooks.FadeMs);
        _lastX = x;
        if (_holdMs > 0)
        {
            _holdMs -= elapsedMs;
            return 1;
        }
        if (_fadeMs <= 0) return now;
        _fadeMs = Math.Max(0, _fadeMs - elapsedMs);
        double left = _fadeMs / FogLooks.FadeMs;
        return now + (1 - now) * left;
    }
}
