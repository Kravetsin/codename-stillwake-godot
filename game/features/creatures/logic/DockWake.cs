using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// How a capsule's spider is seen waking (derelict-enemies), over its dock's <c>Wake</c> ticks, as shares of it: its hazard lamp flashing
/// all through; its iris opening; the spider shown folded on its shelf once the iris is mostly open, unfolding, then dropping to the floor
/// under gravity. Plain C#, tested.
/// </summary>
public static class DockWake
{
    /// <summary>The shares of its waking where each part of it begins and ends.</summary>
    public const double IrisFrom = 0.08, IrisTo = 0.38, ShownAt = 0.3, UnfoldFrom = 0.42, UnfoldTo = 0.72, DropFrom = 0.75;

    /// <summary>How often its hazard lamp turns from red to amber and back a second, waking.</summary>
    public const double FlashHz = 6;

    /// <summary>
    /// How far through its waking it is, 0..1, with <paramref name="left"/> of its <paramref name="wake"/> ticks to go and
    /// <paramref name="progress"/> of the way to the next.
    /// </summary>
    public static double Progress(int left, int wake, double progress) =>
        wake <= 0 ? 1 : Math.Clamp((wake - left + Math.Clamp(progress, 0, 1)) / wake, 0, 1);

    /// <summary>How open its iris is at <paramref name="p"/>: 0 shut, 1 open.</summary>
    public static double Iris(double p) => TickMotion.Ease((p - IrisFrom) / (IrisTo - IrisFrom));

    /// <summary>Whether the spider is seen in its capsule at <paramref name="p"/>: hidden behind the iris before.</summary>
    public static bool Shown(double p) => p >= ShownAt;

    /// <summary>How folded the spider is at <paramref name="p"/>: 1 folded on its shelf, 0 standing.</summary>
    public static double Folded(double p) => 1 - TickMotion.Ease((p - UnfoldFrom) / (UnfoldTo - UnfoldFrom));

    /// <summary>How far down from its shelf to the floor it has dropped at <paramref name="p"/>: 0..1, falling ever faster.</summary>
    public static double Drop(double p)
    {
        double t = Math.Clamp((p - DropFrom) / (1 - DropFrom), 0, 1);
        return t * t;
    }

    /// <summary>Whether its hazard lamp shows red (else amber) <paramref name="seconds"/> into its waking.</summary>
    public static bool Red(double seconds) => (int)Math.Floor(seconds * FlashHz * 2) % 2 == 0;
}
