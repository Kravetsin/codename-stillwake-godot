using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Crew;

namespace Stillwake.Game;

/// <summary>Which foot a step of the body moves: the near one, ahead, or the far one, behind.</summary>
public enum Foot { Front, Back }

/// <summary>
/// One key of a body channel (axe-combos): at <paramref name="Ms"/> from its step's start, on the real clock, the channel at
/// <paramref name="Value"/>, eased in from the key before by <paramref name="Ease"/>.
/// </summary>
public readonly record struct BodyKey(double Ms, double Value, Ease Ease = Ease.Sine);

/// <summary>
/// A foot stepped (axe-combos): from <paramref name="FromMs"/> to <paramref name="ToMs"/> of its step, to <paramref name="To"/>
/// pixels ahead of where it stands at rest, eased along and lifted <paramref name="Lift"/> pixels at mid-step on a half sine.
/// </summary>
public readonly record struct FootStep(Foot Foot, double To, double FromMs, double ToMs, double Lift);

/// <summary>
/// How the body moves through a combo's step (axe-combos), channels of <see cref="BodyKey"/>s: the torso's <paramref name="Lean"/>
/// (radians, forward positive), the hip's <paramref name="Dip"/> (pixels down) and <paramref name="Push"/> (pixels ahead of the
/// feet's middle), the far arm (<paramref name="FarArm"/>, radians from hanging, forward positive, and <paramref name="FarElbow"/>),
/// the far hand's <paramref name="Grip"/> on the haft (0 free, 1 holding), and the <paramref name="Feet"/>' steps. A channel with no
/// keys stays at rest; the first key of each is where the step before left it.
/// </summary>
public sealed record BodyMotion(
    IReadOnlyList<BodyKey> Lean, IReadOnlyList<BodyKey> Dip, IReadOnlyList<BodyKey> Push, IReadOnlyList<BodyKey> FarArm,
    IReadOnlyList<BodyKey> FarElbow, IReadOnlyList<BodyKey> Grip, IReadOnlyList<FootStep> Feet)
{
    /// <summary>The body at rest throughout: every channel at its rest, the feet where they stand.</summary>
    public static readonly BodyMotion Rest = new([], [], [], [], [], [], []);
}

/// <summary>
/// One step of a weapon's combo (axe-combos): its arm's <paramref name="Arm"/>, played over <paramref name="Ms"/> and striking at
/// <paramref name="HitShare"/> of it (none for the back to rest, which strikes nothing), the shares its blade cuts through, from the
/// top of its wind-up (<paramref name="CutFrom"/>) to the end of its follow-through (<paramref name="CutTo"/>), and its
/// <paramref name="Body"/>.
/// </summary>
public sealed record ComboStep(SwingMotion Arm, double Ms, double? HitShare, double CutFrom, double CutTo, BodyMotion Body);

/// <summary>
/// The combos the user picked (axe-combos, «Тело в ударе», card «На максимум»), held to chain them, and the numbers of the body
/// that plays them: a new weapon's combo is rows here.
/// </summary>
public static class Combos
{
    private const Ease L = Ease.Linear, S = Ease.Sine, I = Ease.In;
    private const double Hit = Overhand.HitAt;

    /// <summary>How long the arm and the body take back to rest once the button is let go after a step, in ms.</summary>
    public const double BackMs = 250;

    /// <summary>How long the body layer eases out, in ms, when it stops applying or the hero turns round.</summary>
    public const double LayerMs = 100;

    /// <summary>How much of the torso's lean the head takes back, and how much it turns after the blade, of its turn from the aim line.</summary>
    public const double HeadSteady = 0.6, HeadFollow = 0.08;

    /// <summary>How far the blade's turn from the aim line the head follows, up (negative) and down, in radians.</summary>
    public const double HeadFollowUp = -2.4, HeadFollowDown = 1.2;

    /// <summary>The far elbow's bend at rest, in radians: as the standing pose hangs it.</summary>
    public const double FarElbowRest = 0.22;

    /// <summary>How far short of straight a leg reaching its foot stays, in pixels, so it is never locked.</summary>
    public const double LegSlack = 0.03;

    /// <summary>
    /// The blade's trail: <see cref="TrailMs"/> long, sampled every <see cref="TrailStepMs"/>, at most <see cref="TrailMost"/>
    /// opaque at the blade, fainter where the edge moves slower than <see cref="TrailFast"/> pixels a ms; in <see cref="TrailColour"/>.
    /// </summary>
    public const double TrailMs = 90, TrailStepMs = 3, TrailMost = 0.35, TrailFast = 0.15;

    /// <summary>The trail's colour, a pale wash.</summary>
    public const string TrailColour = "efe6d2";

    /// <summary>
    /// The far hand on the haft: <see cref="GripBelow"/> pixels along it below the near fist, slid between <see cref="HaftFrom"/>
    /// and <see cref="HaftTo"/> (toward the butt positive) to where the arm reaches, <see cref="GripSlack"/> pixels short of straight.
    /// </summary>
    public const double GripBelow = 3.5, HaftFrom = -4, HaftTo = 5.5, GripSlack = 0.2;

    /// <summary>
    /// The back to rest: the far foot home first over <see cref="BackFarMs"/>, then the near one from <see cref="BackNearFromMs"/>
    /// to <see cref="BackNearToMs"/>, or alone over <see cref="BackNearAloneMs"/>; each lifted <see cref="BackLift"/> pixels.
    /// </summary>
    public const double BackFarMs = 100, BackNearFromMs = 110, BackNearToMs = 230, BackNearAloneMs = 120, BackLift = 1.4;

    /// <summary>The furthest a step carries a foot ahead of where it stands, in pixels: how far ahead the floor is looked at for room.</summary>
    public const double MostAheadPx = 15;

    private static BodyKey K(double ms, double value, Ease ease = S) => new(ms, value, ease);

    private static SwingMotion Arm(double cutFrom, double hit, MotionKey[] keys) => new(MotionKind.Swing, (cutFrom + hit) / 2, keys);

    /// <summary>«Цепь» chained: <see cref="FixedMotions.Chain"/> ending in the pose the backhand starts from.</summary>
    private static readonly MotionKey[] ChainOn = [.. FixedMotions.Chain.Keys.Take(FixedMotions.Chain.Keys.Count - 1), new(1, 0.9, 0.45, 0.1)];

    /// <summary>
    /// The fire axe's three steps (card B «С весом» of the arm, level 3 of the body): «Цепь», a backhand rising cut, a heavy
    /// finisher from over the head with the far hand on the haft; hits at 204.75, 630 and 1212 ms of a 1500 ms cycle.
    /// </summary>
    public static readonly IReadOnlyList<ComboStep> FireAxe =
    [
        new(Arm(0.33, Hit, ChainOn), 450, Hit, 0.33, 0.56, new(
            Lean: [K(0, 0, L), K(140, -0.12), K(155, -0.12, L), K(204.75, 0.32, I), K(254.75, 0.32, L), K(430, 0)],
            Dip: [K(0, 0, L), K(150, 0, L), K(204.75, 2.3), K(254.75, 2.3, L), K(310, 0)],
            Push: [K(0, 0, L), K(150, -0.6), K(204.75, -0.5), K(254.75, -0.5, L), K(430, 0)],
            FarArm: [K(0, 0, L), K(150, 0.45), K(220, -0.55), K(260, -0.5, L), K(450, 0)],
            FarElbow: [K(0, 0.22, L), K(150, 0.35), K(220, 0.45), K(450, 0.22)],
            Grip: [],
            Feet: [new(Foot.Front, 6, 120, 204.75, 1.8), new(Foot.Back, 3, 300, 400, 1.5)])),
        new(Arm(0.16, 0.4,
        [
            new(0, 0.9, 0.45, 0.1), new(0.16, 1.05, 0.5, 0.1, S), new(0.4, 0, 0, 0, I), new(0.55, -0.6, 0.35, -0.15), new(1, -1.3, 0.85, -0.45, S),
        ]), 450, 0.4, 0.16, 0.55, new(
            Lean: [K(0, 0, L), K(72, -0.1), K(180, 0.2), K(230, 0.2, L), K(400, 0)],
            Dip: [K(0, 0, L), K(72, 0.8), K(180, 0)],
            Push: [K(0, 0, L), K(72, -0.5), K(180, 0), K(230, 0, L), K(420, 0)],
            FarArm: [K(0, 0, L), K(72, 0.4), K(185, -0.5), K(240, -0.45, L), K(450, 0.25)],
            FarElbow: [K(0, 0.22, L), K(72, 0.35), K(185, 0.45), K(450, 0.3)],
            Grip: [],
            Feet: [new(Foot.Front, 10, 95, 180, 1.6), new(Foot.Back, 7, 290, 390, 1.5)])),
        new(Arm(0.4, 0.52,
        [
            new(0, -1.3, 0.85, -0.45), new(0.3, -2.3, 0, 0.3, S), new(0.4, -2.35, 0.05, 0.3), new(0.52, 0, 0, 0, I), new(0.62, 0.3, 0, 0.3),
            new(1, 0, 0, 0, S),
        ]), 600, 0.52, 0.4, 0.62, new(
            Lean: [K(0, 0, L), K(180, -0.14), K(240, -0.15, L), K(312, 0.45, I), K(362, 0.45, L), K(590, 0)],
            Dip: [K(0, 0, L), K(240, 0, L), K(312, 4), K(362, 4, L), K(430, 0)],
            Push: [K(0, 0, L), K(200, -0.8), K(312, 1), K(362, 1, L), K(590, 0)],
            FarArm: [K(0, 0.25, L), K(180, 0.55), K(240, 0.55, L), K(320, -0.6), K(380, -0.5, L), K(600, 0)],
            FarElbow: [K(0, 0.3, L), K(180, 0.5), K(320, 0.35), K(600, 0.22)],
            Grip: [K(0, 0, L), K(70, 0, L), K(180, 1), K(412, 1, L), K(500, 0)],
            Feet:
            [
                new(Foot.Front, 15, 225, 312, 1.8), new(Foot.Front, 7, 385, 455, 1.4), new(Foot.Back, 0, 460, 525, 1.4),
                new(Foot.Front, 0, 530, 595, 1.4),
            ])),
    ];

    /// <summary>Every channel of the back to rest: from where the step left it (its first key) to rest over <see cref="BackMs"/>.</summary>
    public static IReadOnlyList<BodyKey> BackOf(double rest) => [K(0, rest, L), K(BackMs, rest)];
}
