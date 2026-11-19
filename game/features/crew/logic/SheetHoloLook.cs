using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The drone's hologram in the planner sheet's place (drone-holo, the pick «Дуга»): while a drone window is open or a plan is
/// held, a band of light curved round the hero's eyes, its middle where the sheet's would be, two rows of dim squares in it.
/// It flickers on, holds still but for a faint flicker, and goes out squashing flat. Client only. Lengths in world px.
/// </summary>
public static class SheetHoloLook
{
    /// <summary>How long it takes to come on and to go out.</summary>
    public const double OnMs = 100, OffMs = 80;

    /// <summary>Half the band's length along its curve, and half its thickness across it.</summary>
    public const double HalfLength = 8.5, HalfThick = 2.8;

    /// <summary>Its squares: how many along each of its two rows, their side, and how far each row is off the band's middle.</summary>
    public const int Squares = 6;
    public const double Square = 1.2, RowOff = 1.25;

    /// <summary>The band's fill, its rim and width, the squares' alpha, all of the hologram's; the marker dot's radius, alpha and paleness.</summary>
    public const double Fill = 0.1, Rim = 0.75, RimWidth = 0.3, SquareAlpha = 0.42, DotRadius = 0.5, DotAlpha = 0.9, DotPale = 0.5;

    /// <summary>Where along the band the marker dot sits, a share of its half length past the middle.</summary>
    public const double DotAt = 0.86;

    /// <summary>The cone from the drone's lens: its alpha at the lens and at the band, of the hologram's.</summary>
    public const double ConeAtLens = 0.45, ConeAtBand = 0.12;

    /// <summary>How thick it stays at the last moment of going out, a share of its full thickness.</summary>
    public const double FlatAtLast = 0.2;

    /// <summary>The light it throws on the helmet and what is near, a share of the planner sheet's.</summary>
    public const float Light = 1.2f;

    /// <summary>The band round <paramref name="eye"/> through <paramref name="middle"/>, the sheet's middle: its radius, the angle to its middle, half its span in radians.</summary>
    public static (double Radius, double Toward, double HalfSpan) Arc((double X, double Y) eye, (double X, double Y) middle)
    {
        double dx = middle.X - eye.X, dy = middle.Y - eye.Y, radius = Math.Max(Math.Sqrt(dx * dx + dy * dy), HalfLength);
        return (radius, Math.Atan2(dy, dx), HalfLength / radius);
    }

    /// <summary>
    /// The angle round the eyes of the spot <paramref name="along"/> the band (-1 its top, 1 its bottom, <see cref="SheetPoke"/>)
    /// of one <paramref name="toward"/> with half span <paramref name="half"/>, its hero facing <paramref name="facing"/>.
    /// </summary>
    public static double AngleOf(double toward, double half, double along, int facing) => toward + along * half * facing;

    /// <summary>How far along the band, -1..1, the line from <paramref name="eye"/> to <paramref name="at"/> crosses it, or its end nearer.</summary>
    public static double AlongTo((double X, double Y) eye, double toward, double half, (double X, double Y) at, int facing)
    {
        double turn = Math.Atan2(at.Y - eye.Y, at.X - eye.X) - toward;
        turn = Math.IEEERemainder(turn, 2 * Math.PI);
        return Math.Clamp(turn / half * facing, -1, 1);
    }

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

    /// <summary>How thick it is, a share of its full thickness: whole coming on, flattening as it goes out.</summary>
    public static double Thickness(double shown, bool coming) => coming ? 1 : FlatAtLast + (1 - FlatAtLast) * shown;

    /// <summary>Its faint flicker at <paramref name="timeMs"/>, 0..1, a different one for each <paramref name="seed"/>; dips by <see cref="DroneSign.FlickerDepth"/> of it.</summary>
    public static double Flicker(double timeMs, double seed)
    {
        double t = timeMs / 1000;
        return Math.Clamp(0.5 + 0.3 * Math.Sin(t * 31.7 + seed) + 0.22 * Math.Sin(t * 13.1 + seed * 2.3) + 0.18 * Math.Sin(t * 77.3 + seed * 0.7), 0, 1);
    }
}
