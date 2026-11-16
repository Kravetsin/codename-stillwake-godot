using System;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// The ship's flight between planets as the screen shows it (solar-system): how far along its course it is, and how deep in the
/// warp, 0 standing to 1 at full speed, eased in over its first <see cref="RampS"/> and out over its last, so the stars rush and
/// settle rather than jump.
/// </summary>
public static class Warp
{
    /// <summary>Seconds the warp takes to build up, and to die down before arrival.</summary>
    public const double RampS = 4;

    /// <summary>
    /// How far along a course of <paramref name="flightTicks"/> it is, 0 to 1, with <paramref name="ticksLeft"/> left and
    /// <paramref name="progress"/> of the next tick played; 0 with no course.
    /// </summary>
    public static double Along(int? ticksLeft, int flightTicks, double progress) =>
        ticksLeft is int left ? Math.Clamp((flightTicks - left + progress) / flightTicks, 0, 1) : 0;

    /// <summary>How deep in the warp: smoothly up from the set-off, smoothly down to the arrival; 0 with no course.</summary>
    public static float Depth(int? ticksLeft, int flightTicks, double progress)
    {
        if (ticksLeft is not int left) return 0;
        double gone = flightTicks - left + progress, toGo = left - progress;
        double t = Math.Clamp(Math.Min(gone, toGo) / (RampS * World.TicksPerSecond), 0, 1);
        return (float)(t * t * (3 - 2 * t));
    }
}
