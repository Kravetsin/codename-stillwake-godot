using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// One melee tool's strokes (salvage-tools), in real time: each plays through <paramref name="Ms"/>, striking at
/// <paramref name="HitShare"/> of it, and the next may begin <paramref name="PaceMs"/> after it began, its weapon's pace, so a
/// stroke shorter than the pace rests between. It plays the motion of the sector it is aimed in (<paramref name="Motions"/>,
/// swing-motions), <paramref name="Look"/> in the hand. Its whoosh's loudest moment lies <paramref name="PeakS"/> into its file. A
/// swing sweeps its weapon's fan, <paramref name="Half"/> radians either side of its line (tool-reach). Held, its <paramref name="Combo"/>, if it has
/// one, chains step after step in the Straight and Down sectors (axe-combos).
/// </summary>
public sealed record Stroke(
    SectorMotions Motions, HeldLook Look, double Ms, double PaceMs, double HitShare, double PeakS = Stillwake.Game.Sound.SwingPeakS, double Half = 0,
    IReadOnlyList<ComboStep>? Combo = null)
{
    /// <summary>When in <paramref name="play"/> its whoosh begins: its loudest moment (<see cref="PeakS"/> in) at the motion's, at its hit for none.</summary>
    public double WhooshMsOf(MotionPlay play) => Math.Max(0, (play.Motion.WhooshShare ?? play.HitShare ?? HitShare) * play.Ms - PeakS * 1000);

    /// <summary>The combo it chains aimed in <paramref name="sector"/>: its own, straight ahead and down; none up or under the feet.</summary>
    public IReadOnlyList<ComboStep>? ComboIn(Sector sector) => sector is Sector.Straight or Sector.Down ? Combo : null;
}

/// <summary>Every tool struck with by hand, by its item: the axe, the sledgehammer, the spear and the pickaxe.</summary>
public static class Strokes
{
    private static double PaceMs(Content.Weapon weapon) => weapon.Pace * 1000.0 / Sim.World.TicksPerSecond;

    /// <summary>Half <paramref name="weapon"/>'s fan, in radians: how far either side of its line its rays go.</summary>
    public static double HalfOf(Content.Weapon weapon) => Math.PI * weapon.Arc / Content.Headings.Turn;

    /// <summary>
    /// The fire axe's: a stroke its pace long (0040), overhand up, the chain straight and down, the chop under the feet (swing-motions);
    /// held straight or down, its combo (axe-combos).
    /// </summary>
    public static readonly Stroke Axe = new(new(Overhand.Motion, FixedMotions.Chain, FixedMotions.Chain, FixedMotions.Chop), HeldLook.FireAxe,
        PaceMs(Content.Weapons.Axe), PaceMs(Content.Weapons.Axe), Overhand.HitAt, Half: HalfOf(Content.Weapons.Axe), Combo: Combos.FireAxe);

    /// <summary>
    /// The sledgehammer's: a stroke its pace long, 1.8 s, the blow 0.82 s in; heaved straight and down, overhand up and under the
    /// feet (swing-motions).
    /// </summary>
    public static readonly Stroke Hammer = new(new(Overhand.Motion, FixedMotions.HeaveStraight, FixedMotions.HeaveDown, Overhand.Motion), HeldLook.Sledgehammer,
        PaceMs(Content.Weapons.Sledgehammer), PaceMs(Content.Weapons.Sledgehammer), Overhand.HitAt, Stillwake.Game.Sound.HammerSwingPeakS, HalfOf(Content.Weapons.Sledgehammer));

    /// <summary>The pickaxe's (the-pickaxe): a stroke its pace long, a blow every 0.3 s; the peck up, straight and down, the chop under the feet (swing-motions).</summary>
    public static readonly Stroke Pick = new(new(FixedMotions.Peck, FixedMotions.Peck, FixedMotions.Peck, FixedMotions.Chop), HeldLook.Pickaxe,
        PaceMs(Content.Weapons.Pickaxe), PaceMs(Content.Weapons.Pickaxe), Overhand.HitAt, Half: HalfOf(Content.Weapons.Pickaxe));

    /// <summary>
    /// The spear's, the user's pick «Короткий укол» (jab): <see cref="JabMs"/>, the elbow cocked and straightened, the hit at <see cref="JabHitMs"/>;
    /// played its whole pace, the arm held aimed at rest after the jab, so a held thrust never drops the spear between.
    /// </summary>
    public static readonly Stroke Spear = new(SectorMotions.All(Jab.Motion), HeldLook.Spear, PaceMs(Content.Weapons.Spear), PaceMs(Content.Weapons.Spear), JabHitMs / PaceMs(Content.Weapons.Spear), Stillwake.Game.Sound.SpearThrustPeakS);

    /// <summary>The jab's times, in ms: the elbow cocked by <c>JabCockMs</c>, snapped straight to the hit by <c>JabHitMs</c>, held straight to <c>JabHoldMs</c>, back at rest by <c>JabMs</c>.</summary>
    public const double JabCockMs = 150, JabHitMs = 215, JabHoldMs = 265, JabMs = 450;

    /// <summary>The strokes <paramref name="item"/> makes in the hand; none for a tool that is not struck with.</summary>
    public static Stroke? Of(string? item) =>
        item == Content.Weapons.Axe.Item ? Axe : item == Content.Weapons.Sledgehammer.Item ? Hammer : item == Content.Weapons.Spear.Item ? Spear
        : item == Content.Weapons.Pickaxe.Item ? Pick : null;

    /// <summary>
    /// The jab's elbow at <paramref name="phase"/> of a thrust, in radians bent (the pick's keys): from its rest bend cocked further
    /// back to <see cref="Cocked"/> by <see cref="JabCockMs"/>, snapped straight by <see cref="JabHitMs"/>, held to <see cref="JabHoldMs"/>, eased back to rest by <see cref="JabMs"/>.
    /// </summary>
    public static double ElbowAt(double phase)
    {
        double ms = phase * Spear.Ms;
        if (ms < JabCockMs) return Mix(RestElbow, Cocked, Smooth(ms / JabCockMs));
        if (ms < JabHitMs) return Mix(Cocked, 0, Math.Pow((ms - JabCockMs) / (JabHitMs - JabCockMs), 2));
        if (ms < JabHoldMs) return 0;
        return ms < JabMs ? Mix(0, RestElbow, Smooth((ms - JabHoldMs) / (JabMs - JabHoldMs))) : RestElbow;
    }

    /// <summary>The spear's elbow between thrusts, as it is held (<see cref="HeldLook.Spear"/>), in radians bent.</summary>
    public static double RestElbow => HeldLook.Spear.Elbow;

    /// <summary>How far the elbow cocks back before a thrust, in radians bent.</summary>
    public const double Cocked = 1.75;

    private static double Smooth(double t) => t * t * (3 - 2 * t);
    private static double Mix(double a, double b, double t) => a + (b - a) * t;
}
