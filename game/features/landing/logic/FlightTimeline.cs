using System;
using Stillwake.Sim;
using Stillwake.Sim.Landing;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// When each picture of a drop pod's flight plays and how far along it is (the descent's slice 2, its storyboard): time from the
/// flight's ticks, never a clock of its own, so a save loaded or a guest joining mid-flight shows the right moment. <c>s</c> is
/// seconds since the launch tick; <c>k</c> the flight screen's fraction; <c>a</c> seconds since an arrival was first seen.
/// Lengths are world pixels (a cell 8). The sim never reads these: client constants.
/// </summary>
public static class FlightTimeline
{
    /// <summary>The deck slides open over these first seconds.</summary>
    public const double OpenS = 0.3;
    /// <summary>The pod falls from its home between these, the run shut by the sim at <c>FallTicks</c>, the second's end.</summary>
    public const double DropFrom = 0.3, DropTo = 1.0;
    /// <summary>The deck slides shut from here to <see cref="DropTo"/>, the pod already below it.</summary>
    public const double ShutFrom = 0.75;
    /// <summary>How far the pod has fallen by <see cref="DropTo"/>: out of the hull, the deck shut over it.</summary>
    public const double DropPx = 150;
    /// <summary>It keeps falling, gathering speed, till here: three times its fall to <see cref="DropTo"/>, far under the ship.</summary>
    public const double FallShownTo = DropFrom + 3 * (DropTo - DropFrom);
    /// <summary>The rider's dip to black from the ship to the flight screen, darkest at its peak.</summary>
    public const double DipFrom = 1.0, DipPeak = 1.15, DipTo = 1.3;
    /// <summary>A touchdown: down from <see cref="FromPx"/> over <see cref="Descend"/>, a bounce, held until <see cref="Release"/>.</summary>
    public const double Descend = 3.0, BounceFrom = 3.0, BounceTo = 3.35, Release = 3.5;
    /// <summary>How high over its cell a pod lands from: eighteen cells.</summary>
    public const double FromPx = 18 * 8;
    /// <summary>How high a pod lifting off has risen after <see cref="RiseS"/>: nine cells; it rises on, gathering speed.</summary>
    public const double RisePx = 9 * 8, RiseS = 1.0;
    /// <summary>How high the bounce lifts it, at most.</summary>
    public const double BouncePx = 2;
    /// <summary>A lift-off from a planet is seen this long, three times <see cref="RiseS"/>, the pod far overhead by then.</summary>
    public const double LiftS = 3 * RiseS;

    /// <summary>The flight's length in ticks: a descent's or a return's.</summary>
    public static int Total(Flight f) => f.Up ? Content.Landing.AscentTicks : Content.Landing.DescentTicks;

    /// <summary>The flight's length in seconds.</summary>
    public static double Length(Flight f) => Total(f) / (double)World.TicksPerSecond;

    /// <summary>Seconds since flight <paramref name="f"/> launched, <paramref name="progress"/> the share of the next tick drawn.</summary>
    public static double Seconds(Flight f, double progress) => (Total(f) - f.TicksLeft + progress) / World.TicksPerSecond;

    /// <summary>How open the deck is drawn, 0 shut to 1 apart: sliding open, then shut behind the pod.</summary>
    public static double Open(double s) => Math.Min(Ease(s / OpenS), 1 - Ease((s - ShutFrom) / (DropTo - ShutFrom)));

    /// <summary>How far the pod has fallen below its home, in pixels, gathering speed.</summary>
    public static double Drop(double s)
    {
        double k = Math.Clamp((s - DropFrom) / (DropTo - DropFrom), 0, (FallShownTo - DropFrom) / (DropTo - DropFrom));
        return DropPx * k * k;
    }

    /// <summary>How black the rider's screen is at the cut from the ship to the flight.</summary>
    public static double Dip(double s) =>
        s < DipFrom || s > DipTo ? 0 : s < DipPeak ? (s - DipFrom) / (DipPeak - DipFrom) : (DipTo - s) / (DipTo - DipPeak);

    /// <summary>The flight screen's fraction, 0 at the dip to 1 at the white, the flight <paramref name="length"/> seconds long.</summary>
    public static double Fraction(double s, double length) => Clamp01((s - DipFrom) / (length - 2));

    /// <summary>
    /// How deep in a planet's air the pod is at fraction <paramref name="k"/>: leaving it on the way up; down, the flight is space
    /// alone, star lines to the white (the user cut the re-entry for a shorter flight).
    /// </summary>
    public static double Air(double k, bool up) => up ? 1 - Ease((k - 0.1) / 0.5) : 0;

    /// <summary>How high over its cell an arriving pod is drawn, <paramref name="a"/> seconds after it was seen: slowing, then a bounce.</summary>
    public static double Touchdown(double a)
    {
        double k = Clamp01(a / Descend), landed = 1 - Math.Pow(1 - k, 3);
        double bounce = a > BounceFrom && a < BounceTo ? Math.Sin((a - BounceFrom) / (BounceTo - BounceFrom) * Math.PI) * BouncePx : 0;
        return FromPx * (1 - landed) + bounce;
    }

    /// <summary>How high a pod lifting off is drawn over its cell, gathering speed.</summary>
    public static double Lift(double s)
    {
        double k = Math.Clamp(s / RiseS, 0, LiftS / RiseS);
        return RisePx * k * k;
    }

    /// <summary>Where a pod whose rider is held at <paramref name="floor"/> stands from, its top-left: the inverse of <see cref="LandingModule.FloorOf"/>.</summary>
    public static int PodCellOf(int floor, int width)
    {
        var spec = LandingModule.PodSpec;
        return floor - (spec.Height - 1) * width - (spec.Width / 2 - 1);
    }

    private static double Ease(double x)
    {
        x = Clamp01(x);
        return x * x * (3 - 2 * x);
    }

    private static double Clamp01(double x) => Math.Clamp(x, 0, 1);
}
