using System.Collections.Generic;
using Spec = Stillwake.Content;

namespace Stillwake.Game;

/// <summary>Which foot a step of the body moves: the near one, ahead, or the far one, behind.</summary>
public enum Foot { Front, Back }

/// <summary>
/// One key of a body channel (body-answers): at <paramref name="At"/> on its answer's clock the channel at <paramref name="Value"/>
/// from rest, eased in from the key before by <paramref name="Ease"/>.
/// </summary>
public readonly record struct BodyKey(double At, double Value, Ease Ease = Ease.Sine);

/// <summary>
/// A foot stepped (body-answers): from <paramref name="From"/> to <paramref name="To"/> on its answer's clock, to
/// <paramref name="Ahead"/> pixels ahead of where it stands at rest, lifted <paramref name="Lift"/> pixels at mid-step on a half sine.
/// </summary>
public readonly record struct FootStep(Foot Foot, double Ahead, double From, double To, double Lift);

/// <summary>
/// How the body answers an action (body-answers), channels of <see cref="BodyKey"/>s from rest: the torso's <paramref name="Lean"/>
/// (radians, forward positive), the hip's <paramref name="Dip"/> (pixels down) and <paramref name="Push"/> (pixels ahead), the far
/// arm's turn and its elbow's (<paramref name="FarArm"/>, <paramref name="FarElbow"/>, radians, forward positive), the far hand's
/// <paramref name="Grip"/> on the haft (0 free, 1 holding), the aimed arm's <paramref name="Kick"/> up (radians), and the
/// <paramref name="Feet"/>' steps. A channel is at rest before its first key and stays at its last.
/// </summary>
public sealed record BodyAnswer(
    IReadOnlyList<BodyKey> Lean, IReadOnlyList<BodyKey> Dip, IReadOnlyList<BodyKey> Push, IReadOnlyList<BodyKey> FarArm,
    IReadOnlyList<BodyKey> FarElbow, IReadOnlyList<BodyKey> Grip, IReadOnlyList<BodyKey> Kick, IReadOnlyList<FootStep> Feet);

/// <summary>
/// How the body answers each weapon (body-answers): a swung tool's stroke leans into it on the stroke's own clock, in shares of
/// the stroke from its hit (negative before), so a slow tool's answer is as slow; a gun's shot throws it back on the real clock,
/// in ms from the shot. Each is scaled: a stroke by its tool's heft, a shot by its gun's <c>Recoil</c> over the railgun's.
/// </summary>
public static class BodyAnswers
{
    private const Ease L = Ease.Linear, S = Ease.Sine, I = Ease.In;

    private static BodyKey K(double at, double value, Ease ease = S) => new(at, value, ease);

    /// <summary>A raised blade brought down: leaning back as it rises, into the stroke as it lands, the near foot stepping in.</summary>
    public static readonly BodyAnswer Overhand = new(
        Lean: [K(-0.4, 0, L), K(-0.15, -0.08), K(0, 0.2, I), K(0.06, 0.2, L), K(0.45, 0)],
        Dip: [K(-0.06, 0, L), K(0, 1.6), K(0.06, 1.6, L), K(0.35, 0)],
        Push: [K(-0.4, 0, L), K(-0.15, -0.4), K(0, 0.6), K(0.06, 0.6, L), K(0.45, 0)],
        FarArm: [K(-0.4, 0, L), K(-0.15, 0.4), K(0.02, -0.5), K(0.08, -0.45, L), K(0.5, 0)],
        FarElbow: [K(-0.4, 0, L), K(-0.15, 0.12), K(0.02, 0.22), K(0.5, 0)],
        Grip: [],
        Kick: [],
        Feet: [new(Foot.Front, 3, -0.2, 0, 1.4), new(Foot.Front, 0, 0.2, 0.42, 1.2)]);

    /// <summary>A heavy head heaved: the far hand on the haft from the wind-up, the body thrown down after it, a long step in.</summary>
    public static readonly BodyAnswer Heave = new(
        Lean: [K(-0.4, 0, L), K(-0.15, -0.12), K(0, 0.3, I), K(0.08, 0.3, L), K(0.45, 0)],
        Dip: [K(-0.06, 0, L), K(0, 3), K(0.08, 3, L), K(0.4, 0)],
        Push: [K(-0.4, 0, L), K(-0.15, -0.6), K(0, 1), K(0.08, 1, L), K(0.45, 0)],
        FarArm: [K(-0.4, 0, L), K(-0.15, 0.5), K(0.02, -0.55), K(0.1, -0.5, L), K(0.5, 0)],
        FarElbow: [K(-0.4, 0, L), K(-0.15, 0.25), K(0.02, 0.12), K(0.5, 0)],
        Grip: [K(-0.4, 0, L), K(-0.2, 1), K(0.12, 1, L), K(0.3, 0)],
        Kick: [],
        Feet: [new(Foot.Front, 5, -0.18, 0, 1.6), new(Foot.Front, 0, 0.22, 0.45, 1.3)]);

    /// <summary>A thrust: the body lunging after the point, the near foot stepping in with it.</summary>
    public static readonly BodyAnswer Jab = new(
        Lean: [K(-0.3, 0, L), K(-0.12, -0.05), K(0, 0.16, I), K(0.08, 0.16, L), K(0.45, 0)],
        Dip: [K(-0.05, 0, L), K(0, 1), K(0.08, 1, L), K(0.4, 0)],
        Push: [K(-0.3, 0, L), K(-0.12, -0.5), K(0, 2.5, I), K(0.08, 2.5, L), K(0.45, 0)],
        FarArm: [K(-0.3, 0, L), K(-0.12, 0.3), K(0, -0.35), K(0.45, 0)],
        FarElbow: [],
        Grip: [],
        Kick: [],
        Feet: [new(Foot.Front, 4, -0.14, 0, 1.2), new(Foot.Front, 0, 0.2, 0.45, 1)]);

    /// <summary>
    /// A shot's recoil, a railgun's in full, in ms from the shot: the torso thrown back and the gun arm up at once, the hip back,
    /// the far foot stepping back to take it, all eased back by <see cref="RecoilMs"/>.
    /// </summary>
    public static readonly BodyAnswer Recoil = new(
        Lean: [K(0, 0, L), K(40, -0.22, Ease.Out), K(110, -0.2, L), K(350, 0)],
        Dip: [K(30, 0, L), K(90, 1.2), K(300, 0)],
        Push: [K(0, 0, L), K(40, -3, Ease.Out), K(110, -2.6, L), K(350, 0)],
        FarArm: [K(0, 0, L), K(50, 0.45, Ease.Out), K(350, 0)],
        FarElbow: [K(0, 0, L), K(50, 0.3), K(350, 0)],
        Grip: [],
        Kick: [K(0, 0, L), K(35, 0.5, Ease.Out), K(90, 0.42, L), K(330, 0)],
        Feet: [new(Foot.Back, -4, 20, 110, 1.3), new(Foot.Back, 0, 220, 340, 1)]);

    /// <summary>How long a shot's recoil plays, in ms.</summary>
    public const double RecoilMs = 350;

    /// <summary>The recoil a gun's shot is answered with in full: the railgun's.</summary>
    public const double FullRecoil = 220;

    /// <summary>
    /// How fast a shot shoves a hero standing on the ground, in its gun's <c>Recoil</c>: braked by the floor from the first step, a
    /// railgun's level shot goes about half a cell back, a nailer's under a tenth.
    /// </summary>
    public const double Shove = 1.3;

    /// <summary>A swung tool's answer and how heavy it is, 1 the fire axe's; none for a tool that does not swing.</summary>
    public static (BodyAnswer Answer, double Heft)? StrokeOf(string? tool) => Spec.Weapons.Of(tool) switch
    {
        var w when w == Spec.Weapons.Sledgehammer => (Heave, 1.3),
        var w when w == Spec.Weapons.Axe => (Overhand, 1),
        var w when w == Spec.Weapons.Pickaxe => (Overhand, 0.75),
        var w when w == Spec.Weapons.Spear => (Jab, 1),
        _ => null,
    };

    /// <summary>How long the body layer eases out, in ms, when it stops applying or the hero turns round.</summary>
    public const double LayerMs = 100;

    /// <summary>How much of the torso's lean the head takes back.</summary>
    public const double HeadSteady = 0.6;

    /// <summary>How far short of straight a leg reaching its foot stays, in pixels, so it is never locked.</summary>
    public const double LegSlack = 0.03;

    /// <summary>
    /// The far hand on the haft: <see cref="GripBelow"/> pixels along it below the near fist, slid between <see cref="HaftFrom"/>
    /// and <see cref="HaftTo"/> (toward the butt positive) to where the arm reaches, <see cref="GripSlack"/> pixels short of straight.
    /// </summary>
    public const double GripBelow = 3.5, HaftFrom = -4, HaftTo = 5.5, GripSlack = 0.2;

    /// <summary>The furthest an answer carries a foot ahead of where it stands, in pixels: how far ahead the floor is looked at for room.</summary>
    public const double MostAheadPx = 8;
}
