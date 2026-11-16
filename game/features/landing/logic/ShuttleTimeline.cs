using System;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// The crew shuttle's trip as the user's pick «A — Под днищем» plays it (shuttle-flight, slice 3): where the craft is drawn, its
/// skids, its flames and how far its rider's camera has pulled out, at each moment. Leaving, <c>s</c> is seconds since it set off,
/// from the voyage's ticks; arriving, <c>a</c> is seconds since <see cref="Arrivals"/> first saw it, frame time. Offsets are world
/// pixels from the corner of its picture standing parked (a cell 8). Client constants: the sim reads none of these.
/// </summary>
public static class ShuttleTimeline
{
    /// <summary>Its run's halves slide apart between these, the sim's run open under them since it set off.</summary>
    public const double OpenFrom = 0.55, OpenTo = 1.0;
    /// <summary>It sinks through the run between these, its skids folding.</summary>
    public const double SinkFrom = 1.2, SinkTo = 2.7;
    /// <summary>The run slides shut behind it by <see cref="ShutTo"/>, the sim's <c>Shuttle.FallTicks</c>.</summary>
    public const double ShutFrom = 2.7, ShutTo = 3.0;
    /// <summary>It flies off to the right under the ship's belly till here, gathering speed.</summary>
    public const double PassTo = 5.5;
    /// <summary>The rider's dip to black from the ship to its flight screen, darkest at its peak, as it is gone.</summary>
    public const double DipFrom = 5.2, DipPeak = PassTo, DipTo = 5.8;
    /// <summary>How far under its parked spot it flies: its 72 px craft and the run's cell below the deck, and a cell clear.</summary>
    public const double SinkPx = 88;
    /// <summary>How far right it has flown by <see cref="PassTo"/>, out of sight.</summary>
    public const double PassPx = 1150;

    /// <summary>Arriving: the white falls while it drifts in far left; then it brakes on its retros to its spot.</summary>
    public const double DriftS = 0.5, ApproachS = 2.8;
    /// <summary>At the vessel, the camera closes back in over this once it stands.</summary>
    public const double SettleS = 1.8;
    /// <summary>How far left of its spot it comes in from: at the drift's start, and as it begins to brake.</summary>
    public const double DriftPx = 648, ApproachPx = 568;
    /// <summary>Home, it stops under the run, the run's halves slide apart, it rises through and they slide shut.</summary>
    public const double HomeOpenFrom = 3.0, RiseFrom = 3.3, RiseTo = 4.5, HomeShutTo = 4.8;

    /// <summary>How long an arriving shuttle is held from the grid: by the vessel, till the camera stands; home, till the run shuts.</summary>
    public static double Hold(bool aboard) => aboard ? HomeShutTo : DriftS + ApproachS + SettleS;

    /// <summary>
    /// One moment of the shuttle: its picture's offset from its parked corner, its skids (0 down, 1 half, 2 up), its three stern
    /// nozzles' flames and its nose retros' (0 none to 1 long), how far its rider's camera has pulled out (0 the follow, 1 the far
    /// frame) and how far apart its run's halves are drawn (null none drawn, else 0 shut to 1 apart).
    /// </summary>
    public sealed record Moment(double X, double Y, int Fold, double Stern0, double Stern1, double Stern2, double Retro, double Blend, double? Open);

    /// <summary>
    /// A shuttle <paramref name="s"/> seconds after setting off, <paramref name="aboard"/> the ship: aboard it sinks through its run and
    /// flies off under the belly; off the ship it stands, nobody there sees it lift off, its rider's camera pulling out all the same.
    /// </summary>
    public static Moment Leaving(double s, bool aboard)
    {
        double sink = Ease(Clamp01((s - SinkFrom) / ((SinkTo - SinkFrom) * 0.9)));
        double u = Clamp01((s - SinkTo) / (PassTo - SinkTo));
        double blend = s < SinkTo ? 0.25 * sink : 0.25 + 0.75 * Ease(u / 0.5);
        if (!aboard) return new Moment(0, 0, 0, 0, 0, 0, 0, blend, null);
        double? open = s < ShutTo ? Math.Min(Ease((s - OpenFrom) / (OpenTo - OpenFrom)), 1 - Ease((s - ShutFrom) / (ShutTo - ShutFrom))) : null;
        if (s < SinkTo)
        {
            double k = (s - SinkFrom) / (SinkTo - SinkFrom), fold = Clamp01((k - 0.1) / 0.5) * 2, flame = k > 0.75 ? 0.3 : 0;
            return new Moment(0, SinkPx * sink, Folded(fold), 0, flame, flame, 0, blend, open);
        }
        double g = Clamp01((u - 0.05) / 0.3);
        return new Moment(PassPx * u * u, SinkPx + Math.Sin(u * 3) * 3, 2, 0.5 * g + 0.4 * u, 0.6 * g + 0.3 * u, 0.7 * g + 0.3 * u, 0, blend, open);
    }

    /// <summary>
    /// A shuttle <paramref name="a"/> seconds after it was seen arriving, <paramref name="aboard"/> home: in from the far left on its
    /// retros, skids out, to its spot by the vessel, the camera then closing in; home, under the belly to below its run and up through it.
    /// </summary>
    public static Moment Arriving(double a, bool aboard)
    {
        double low = aboard ? SinkPx : 0;
        if (a < DriftS)
            return new Moment(-DriftPx + (DriftPx - ApproachPx) * a / DriftS, low - 30, 2, 0.7, 0.7, 0.8, 0, 1, null);
        double u = Clamp01((a - DriftS) / ApproachS);
        if (a < DriftS + ApproachS)
        {
            double x = -ApproachPx * Math.Pow(1 - u, 4), y = low - (1 - u) * (1 - u) * 30;
            double main = u < 0.25 ? 1 : 0, retro = u > 0.25 && u < 0.9 ? 0.5 + 0.2 * Math.Sin(u * 40) : 0;
            int fold = aboard ? 2 : Folded(2 - Clamp01((u - 0.68) / 0.22) * 2);
            double? open = aboard ? Ease((a - HomeOpenFrom) / (RiseFrom - HomeOpenFrom)) : null;
            return new Moment(x, y, fold, 0.6 * main, 0.6 * main, 0.7 * main, retro, 1, open is > 0 ? open : null);
        }
        double since = a - DriftS - ApproachS;
        if (!aboard) return new Moment(0, 0, 0, 0, 0, 0, 0, 1 - EaseInOut3(Clamp01(since / SettleS)), null);
        double r = Clamp01((a - RiseFrom) / (RiseTo - RiseFrom)), rise = Ease(r);
        double flame = r < 1 ? 0.3 : 0;
        double shut = 1 - Ease((a - RiseTo) / (HomeShutTo - RiseTo));
        return new Moment(0, SinkPx * (1 - rise), Folded(2 - Clamp01((r - 0.5) / 0.4) * 2), 0, flame, flame, 0,
            1 - EaseInOut3(Clamp01((a - RiseFrom) / (HomeShutTo - RiseFrom))), shut > 0 ? shut : null);
    }

    /// <summary>
    /// The middle of the craft standing from <paramref name="cell"/> on a grid <paramref name="width"/> wide, moved by
    /// <paramref name="m"/>, in world pixels: what its rider's far frame is centred toward.
    /// </summary>
    public static (double X, double Y) Middle(int cell, int width, Moment m) =>
        (cell % width * Camera.Cell + 96 + m.X, cell / width * Camera.Cell + 44 + m.Y);

    /// <summary>The picture of skids folded <paramref name="fold"/>, 0 to 2: the nearest of the three drawn.</summary>
    private static int Folded(double fold) => (int)Math.Round(Math.Clamp(fold, 0, 2), MidpointRounding.AwayFromZero);

    /// <summary>The rider's dip to black at the cut from the world to its flight screen, as the shuttle is gone.</summary>
    public static double Dip(double s) =>
        s < DipFrom || s > DipTo ? 0 : s < DipPeak ? (s - DipFrom) / (DipPeak - DipFrom) : (DipTo - s) / (DipTo - DipPeak);

    private static double Ease(double x)
    {
        x = Clamp01(x);
        return x * x * (3 - 2 * x);
    }

    private static double EaseInOut3(double u) => u < 0.5 ? 4 * u * u * u : 1 - Math.Pow(-2 * u + 2, 3) / 2;

    private static double Clamp01(double x) => Math.Clamp(x, 0, 1);
}
