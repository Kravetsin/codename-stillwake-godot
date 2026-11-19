using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The drone projecting before the hero (drone-holo): while a drone window is open or the plan out, the drone floats before the
/// hero's waist and its motes (<see cref="DroneMotesLook"/>) flicker on over it, hold but for a faint flicker, and go out. Client
/// only. Lengths in world px.
/// </summary>
public static class SheetHoloLook
{
    /// <summary>How long it takes to come on and to go out.</summary>
    public const double OnMs = 100, OffMs = 80;

    /// <summary>
    /// The drone before the hero, <see cref="OverAhead"/> past its middle the way it faces and <see cref="OverUp"/> over its feet;
    /// its motes <see cref="OverRise"/> over it.
    /// </summary>
    public const double OverAhead = 16, OverUp = 12, OverRise = 9;

    /// <summary>How near its place before the hero the drone first is before the motes come on, pixels; they stay on as it follows after.</summary>
    public const double OverArrived = 1.5;

    /// <summary>The light the motes throw on what is near, a share of the planner sheet's.</summary>
    public const float Light = 1.2f;

    /// <summary>How far in it is after <paramref name="elapsedMs"/> more, from <paramref name="was"/>, while <paramref name="on"/> or not.</summary>
    public static double Step(double was, bool on, double elapsedMs) =>
        on ? Math.Min(1, was + elapsedMs / OnMs) : Math.Max(0, was - elapsedMs / OffMs);

    /// <summary>
    /// Its alpha, 0..1 of <see cref="DroneSign.Alpha"/>, <paramref name="shown"/> in and coming on or not: coming on it flickers
    /// (bright, nearly out, bright, dim, whole), going out it fades.
    /// </summary>
    public static double Brightness(double shown, bool coming)
    {
        if (!coming || shown >= 1) return shown;
        return shown < 0.25 ? 0.85 : shown < 0.45 ? 0.15 : shown < 0.7 ? 1 : shown < 0.85 ? 0.4 : 1;
    }

    /// <summary>Its faint flicker at <paramref name="timeMs"/>, 0..1, a different one for each <paramref name="seed"/>; dips by <see cref="DroneSign.FlickerDepth"/> of it.</summary>
    public static double Flicker(double timeMs, double seed)
    {
        double t = timeMs / 1000;
        return Math.Clamp(0.5 + 0.3 * Math.Sin(t * 31.7 + seed) + 0.22 * Math.Sin(t * 13.1 + seed * 2.3) + 0.18 * Math.Sin(t * 77.3 + seed * 0.7), 0, 1);
    }
}
