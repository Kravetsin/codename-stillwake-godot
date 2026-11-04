using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>How a melee tool's stroke moves the arm: swung over in an arc, as the axe (0040), or thrust straight at the point, as the spear.</summary>
public enum StrokeKind { Swing, Thrust }

/// <summary>
/// One melee tool's strokes (salvage-tools), in real time: each plays through <paramref name="Ms"/>, striking at
/// <paramref name="HitShare"/> of it, and the next may begin <paramref name="PaceMs"/> after it began, its weapon's pace, so a
/// stroke shorter than the pace rests between. Its whoosh's loudest moment lies <paramref name="PeakS"/> into its file.
/// </summary>
public sealed record Stroke(StrokeKind Kind, double Ms, double PaceMs, double HitShare, double PeakS = Stillwake.Game.Sound.SwingPeakS)
{
    /// <summary>When in a stroke its whoosh begins: its loudest moment (<see cref="PeakS"/> in) as the blade comes down, or the spear goes out.</summary>
    public double WhooshMs => Kind == StrokeKind.Swing
        ? (AxeSwing.WindShare + AxeSwing.StrikeShare / 2) * Ms - PeakS * 1000
        : Math.Max(0, HitShare * Ms - PeakS * 1000);
}

/// <summary>Every tool struck with by hand, by its item: the axe, the sledgehammer and the spear.</summary>
public static class Strokes
{
    private static double PaceMs(Content.Weapon weapon) => weapon.Pace * 1000.0 / Sim.World.TicksPerSecond;

    /// <summary>The fire axe's: a stroke its pace long (0040).</summary>
    public static readonly Stroke Axe = new(StrokeKind.Swing, PaceMs(Content.Weapons.Axe), PaceMs(Content.Weapons.Axe), AxeSwing.HitAt);

    /// <summary>
    /// The sledgehammer's: the axe's swing slowed so the blow lands <see cref="HammerBlowMs"/> into it, then rest till its pace of
    /// three seconds is out.
    /// </summary>
    public static readonly Stroke Hammer = new(StrokeKind.Swing, HammerBlowMs / AxeSwing.HitAt, PaceMs(Content.Weapons.Sledgehammer), AxeSwing.HitAt, Stillwake.Game.Sound.HammerSwingPeakS);

    /// <summary>How long the sledgehammer's swing takes to its blow: a heavy wind-up, slower than the axe's whole swing.</summary>
    public const double HammerBlowMs = 800;

    /// <summary>
    /// The spear's, the user's pick «Короткий укол» (jab): <see cref="JabMs"/>, the elbow cocked and straightened, the hit at 215 ms;
    /// played its whole pace, the arm held aimed at rest after the jab, so a held thrust never drops the spear between.
    /// </summary>
    public static readonly Stroke Spear = new(StrokeKind.Thrust, PaceMs(Content.Weapons.Spear), PaceMs(Content.Weapons.Spear), 215 / PaceMs(Content.Weapons.Spear), Stillwake.Game.Sound.SpearThrustPeakS);

    /// <summary>How long the jab itself takes, out and back.</summary>
    public const double JabMs = 450;

    /// <summary>The strokes <paramref name="item"/> makes in the hand; none for a tool that is not struck with.</summary>
    public static Stroke? Of(string? item) =>
        item == Content.Weapons.Axe.Item ? Axe : item == Content.Weapons.Sledgehammer.Item ? Hammer : item == Content.Weapons.Spear.Item ? Spear : null;

    /// <summary>
    /// The jab's elbow at <paramref name="phase"/> of a thrust, in radians bent (the pick's keys): from its rest bend cocked further
    /// back to <see cref="Cocked"/> by 150 ms, snapped straight by 215 ms, held to 265 ms, eased back to rest by <see cref="JabMs"/>.
    /// </summary>
    public static double ElbowAt(double phase)
    {
        double ms = phase * Spear.Ms;
        if (ms < 150) return Mix(RestElbow, Cocked, Smooth(ms / 150));
        if (ms < 215) return Mix(Cocked, 0, Math.Pow((ms - 150) / 65, 2));
        if (ms < 265) return 0;
        return ms < JabMs ? Mix(0, RestElbow, Smooth((ms - 265) / (JabMs - 265))) : RestElbow;
    }

    /// <summary>The spear's elbow between thrusts, as it is held (<see cref="HeldLook.Spear"/>), in radians bent.</summary>
    public static double RestElbow => HeldLook.Spear.Elbow;

    /// <summary>How far the elbow cocks back before a thrust, in radians bent.</summary>
    public const double Cocked = 1.75;

    private static double Smooth(double t) => t * t * (3 - 2 * t);
    private static double Mix(double a, double b, double t) => a + (b - a) * t;
}
