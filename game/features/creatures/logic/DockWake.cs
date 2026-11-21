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

    /// <summary>The steps its iris opens by between shut and open: its pictures <c>guard_capsule_iris_1</c> on (guards.js).</summary>
    public const int IrisSteps = 5;

    /// <summary>Which of its iris's pictures shows at <paramref name="p"/>: 0 shut, 1 to <see cref="IrisSteps"/> opening, one more open.</summary>
    public static int IrisStep(double p) => Math.Clamp((int)Math.Floor(Iris(p) * (IrisSteps + 1)), 0, IrisSteps + 1);

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

/// <summary>
/// How a stand lets its escort go (derelict-enemies), the pick page's S1 «Люлька» over its dock's <c>Wake</c> ticks, as shares of it: the
/// waist clamp swinging up; the drum paying out its cable as the robot is lowered its hang to the floor; the yoke's hooks opening and the
/// yoke rising off its shoulders; the robot unfolding, its red lamp flickering on; the beacon flashing all through. Plain C#, tested.
/// </summary>
public static class StandWake
{
    /// <summary>How high the robot hangs asleep, in cells over the floor; how far the clamp swings up, in radians; how far the yoke rises off it, in cells.</summary>
    public const double Hang = 0.4, ClampSwing = 1.3, YokeRise = 0.3;

    /// <summary>The shares of its waking where each part of it begins and ends.</summary>
    public const double ClampTo = 0.25, LowerFrom = 0.3, LowerTo = 0.8, LampFrom = 0.4, LampTo = 0.55, HooksFrom = 0.85, HooksTo = 0.95, RiseFrom = 0.86, UnfoldFrom = 0.82;

    /// <summary>How often the beacon flashes a second while its robot wakes.</summary>
    public const double BeaconHz = 2;

    private static double Ease(double from, double to, double p) => TickMotion.Ease((p - from) / (to - from));

    /// <summary>How far the clamp has swung up at <paramref name="p"/>, in radians.</summary>
    public static double Clamp(double p) => ClampSwing * Ease(0, ClampTo, p);

    /// <summary>How high the robot still hangs over its floor at <paramref name="p"/>, in cells.</summary>
    public static double Hung(double p) => Hang * (1 - Ease(LowerFrom, LowerTo, p));

    /// <summary>How open the yoke's hooks are at <paramref name="p"/>, 0..1.</summary>
    public static double Hooks(double p) => Ease(HooksFrom, HooksTo, p);

    /// <summary>How far the yoke has risen off the robot's shoulders at <paramref name="p"/>, in cells.</summary>
    public static double Risen(double p) => YokeRise * Ease(RiseFrom, 1, p);

    /// <summary>How folded the robot is at <paramref name="p"/>: 1 stowed in its stand, 0 standing.</summary>
    public static double Folded(double p) => 1 - Ease(UnfoldFrom, 1, p);

    /// <summary>Its lamp at <paramref name="p"/>: dark, then flickering on <paramref name="seconds"/> into its waking, then lit; 0..1.</summary>
    public static double Lamp(double p, double seconds) =>
        p < LampFrom ? 0 : p >= LampTo ? 1 : (int)Math.Floor(seconds * 14) % 3 == 0 ? 0.2 : 1;

    /// <summary>Whether the beacon is lit <paramref name="seconds"/> into its robot's waking.</summary>
    public static bool Beacon(double seconds) => (int)Math.Floor(seconds * BeaconHz * 2) % 2 == 0;
}
