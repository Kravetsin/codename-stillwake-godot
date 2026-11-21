using System;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Shared;

/// <summary>A frame square as drawn: where it is, how far along its flight (1 landed), its heading, where its thread runs from.</summary>
public readonly record struct FrameSquare(float X, float Y, float Flight, float Dx, float Dy, float FromX, float FromY)
{
    /// <summary>Whether it sits in its place.</summary>
    public bool Landed => Flight >= 1;

    /// <summary>Whether it is out at all: on its way or landed.</summary>
    public bool Out => Flight > 0;
}

/// <summary>
/// The drone's sparks framing a window (drone-frame, the pick «Нити»): each square flies straight from the drone to its place on
/// <see cref="SootFade"/>'s clock, and straight home as the clock runs down; every <see cref="DroneInk.ThreadEvery"/>-th trails its
/// thread from where it left. Screen px. Client only.
/// </summary>
public static class SootRun
{
    /// <summary>
    /// Square <paramref name="i"/> of <paramref name="spots"/> laid at <paramref name="origin"/>, at <paramref name="fade"/>'s time, the
    /// drone's eye at <paramref name="drone"/> (none: it appears in place).
    /// </summary>
    public static FrameSquare At(SootSpots spots, int i, SootFade fade, (float X, float Y) origin, (float X, float Y)? drone)
    {
        float q = fade.Landed(spots.Lag(i));
        var (sx, sy) = spots.Spot(i);
        float x = origin.X + sx, y = origin.Y + sy;
        if (q >= 1 || q <= 0 || drone is not var (dx, dy)) return new FrameSquare(x, y, drone == null && q > 0 ? 1 : q, 0, 0, x, y);
        float e = q * q * (3 - 2 * q), wx = x - dx, wy = y - dy, d = MathF.Sqrt(wx * wx + wy * wy);
        if (d < 1e-4f) (wx, wy, d) = (0, 1, 1);
        // Out it heads for its place, threaded from the drone; home it heads for the drone, threaded from its place.
        return fade.Opening
            ? new FrameSquare(dx + wx * e, dy + wy * e, q, wx / d, wy / d, dx, dy)
            : new FrameSquare(dx + wx * e, dy + wy * e, q, -wx / d, -wy / d, x, y);
    }

    /// <summary>Whether square <paramref name="i"/> trails a thread as it flies.</summary>
    public static bool Threads(int i) => i % I.ThreadEvery == 0;
}
