using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The overhand (0040, swing-motions): raised back over the shoulder for <see cref="WindShare"/> of the stroke, brought down
/// through the aim line to its point in <see cref="StrikeShare"/>, crossing it at <see cref="HitAt"/>, carried on through at
/// <see cref="DownSpeed"/>, eased back to the line. The one motion whose keys are built at the press for its aim
/// (<see cref="ShapeFor"/>), so an aim high or low is raised or carried through less.
/// </summary>
public static class Overhand
{
    /// <summary>The stroke's shape, in shares of it and radians from the aim line: raised <see cref="Raised"/> back, then down to <see cref="Through"/> past it.</summary>
    public const double WindShare = 0.35, StrikeShare = 0.12, Raised = 2.1, Through = 0.3;

    /// <summary>The share of a stroke at which the edge crosses the aim line: when it strikes.</summary>
    public const double HitAt = WindShare + StrikeShare * Raised / (Raised + Through);

    /// <summary>The least it is raised, and brought through, however it is aimed, in radians.</summary>
    public const double LeastRaised = 0.5, LeastThrough = 0.1;

    /// <summary>How fast the arm comes down, in radians a share of the stroke: its whole turn over the downstroke.</summary>
    public const double DownSpeed = (Raised + Through) / StrikeShare;

    /// <summary>
    /// The farthest back over the head it raises the straight arm, in radians from hanging (<see cref="Bend.Upper"/>): a little
    /// past straight up, as far as a shoulder goes. Aimed high it is raised less, so it never twists behind the back.
    /// </summary>
    public const double Overhead = 3.6;

    /// <summary>The farthest behind hanging it brings the straight arm through, in radians: aimed low it goes on less.</summary>
    public const double Behind = 1.0;

    /// <summary>The overhand: its keys built at the press, its whoosh loudest in the middle of the downstroke.</summary>
    public static readonly SwingMotion Motion = new(MotionKind.Swing, WindShare + StrikeShare / 2, []);

    /// <summary>
    /// How far a stroke along <paramref name="line"/>, radians from straight ahead of the shoulder, down positive, is raised and
    /// brought through for a fan <paramref name="half"/> radians either side: through the whole fan, at least <see cref="Through"/>,
    /// raised <see cref="Raised"/>, each cut short so the arm passes neither <see cref="Overhead"/> nor <see cref="Behind"/>.
    /// </summary>
    public static (double Raised, double Through) ShapeFor(double line, double half)
    {
        double level = Math.PI / 2 - line;
        return (Math.Clamp(Overhead - level, LeastRaised, Raised), Math.Clamp(level + Behind, LeastThrough, Math.Max(Through, half)));
    }

    /// <summary>
    /// The keys of a stroke shaped <paramref name="shape"/>, the elbow bent <paramref name="elbow"/> throughout: eased up, down hard
    /// across the line at <see cref="HitAt"/>, on through at <see cref="DownSpeed"/>, eased back, at rest at either end.
    /// </summary>
    public static MotionKey[] KeysFor((double Raised, double Through) shape, double elbow) =>
    [
        new(0, 0, elbow),
        new(WindShare, -shape.Raised, elbow, Ease: Ease.Sine),
        new(HitAt, 0, elbow),
        new(HitAt + shape.Through / DownSpeed, shape.Through, elbow),
        new(1, 0, elbow, Ease: Ease.Sine),
    ];

    /// <summary>
    /// The share of a stroke shaped <paramref name="shape"/> at which the falling edge is <paramref name="turn"/> radians from its
    /// line, back (negative) or through (positive): held to the downstroke and the follow-through.
    /// </summary>
    public static double ShareAt(double turn, (double Raised, double Through) shape) => turn < 0
        ? WindShare + (HitAt - WindShare) * Math.Max(0, 1 + turn / shape.Raised)
        : HitAt + Math.Min(turn, shape.Through) / DownSpeed;
}

/// <summary>The jab (the spear's «Короткий укол»): thrust, the shaft aimed through the point, the elbow cocked and straightened.</summary>
public static class Jab
{
    /// <summary>The jab: no shoulder keys, its elbow on <see cref="Strokes.ElbowAt"/>, its whoosh loudest at the hit.</summary>
    public static readonly SwingMotion Motion = new(MotionKind.Thrust, null, [], Strokes.ElbowAt);
}

/// <summary>
/// The fixed-key motions the user picked (swing-motions), each drawn for its sector's middle and turned to the aim: struck at
/// <see cref="Overhand.HitAt"/>, the stroke's hit, with the shoulder on the aim line, the elbow as held and the wrist straight,
/// so the item's <see cref="AxeSwing.Lead"/> holds; at rest so at either end.
/// </summary>
public static class FixedMotions
{
    private const double Hit = Overhand.HitAt;

    /// <summary>«Короткая рубка», the axe's and the pickaxe's under the feet: raised a little ahead, the wrist cocking the blade up, then chopped down, the wrist driving it into the floor.</summary>
    public static readonly SwingMotion Chop = Of(
    [
        new(0, 0, 0), new(0.3, -1.25, 0.15, -0.55, Ease.Sine), new(Hit, 0, 0, 0, Ease.In), new(0.53, 0.1, 0, 0.35), new(1, 0, 0, 0, Ease.Sine),
    ]);

    /// <summary>«Плечо подаёт», the pickaxe's peck up, straight and down: mostly the elbow, the shoulder lifting a little and driving the blow.</summary>
    public static readonly SwingMotion Peck = Of(
    [
        new(0, 0, 0), new(0.3, -0.45, 1.15, -0.15, Ease.Sine), new(Hit, 0, 0, 0, Ease.In), new(0.56, 0.18, 0, 0.2), new(1, 0, 0, 0, Ease.Sine),
    ]);

    /// <summary>«Цепь», the axe's straight and down: laid back over the shoulder, then shoulder, elbow and wrist in turn, the wrist throwing the blade last.</summary>
    public static readonly SwingMotion Chain = Of(
    [
        new(0, 0, 0), new(0.33, -1.55, 1.0, -0.55, Ease.Sine), new(0.405, -0.75, 0.9, -0.5, Ease.In), new(0.435, -0.2, 0.35, -0.4),
        new(Hit, 0, 0, 0), new(0.56, 0.3, 0, 0.4), new(1, 0, 0, 0, Ease.Sine),
    ]);

    /// <summary>«Тяжёлый подъём», the sledgehammer's straight: swung back low and paused, heaved onto the shoulder and held, then brought down.</summary>
    public static readonly SwingMotion HeaveStraight = Of(
    [
        new(0, 0, 0), new(0.12, 1.2, 0.1, 0.7, Ease.Sine), new(0.18, 1.25, 0.1, 0.75), new(0.32, -1.7, 1.1, -0.4, Ease.Sine),
        new(0.38, -1.8, 1.15, -0.45), new(Hit, 0, 0, 0, Ease.In), new(0.52, 0.3, 0, 0.25), new(1, 0, 0, 0, Ease.Sine),
    ]);

    /// <summary>The heave aimed down: back less far, heaved higher over the shoulder.</summary>
    public static readonly SwingMotion HeaveDown = Of(
    [
        new(0, 0, 0), new(0.12, 0.62, 0.1, 0.7, Ease.Sine), new(0.18, 0.65, 0.1, 0.75), new(0.32, -2.6, 0.9, -0.4, Ease.Sine),
        new(0.38, -2.7, 0.95, -0.45), new(Hit, 0, 0, 0, Ease.In), new(0.52, 0.3, 0, 0.25), new(1, 0, 0, 0, Ease.Sine),
    ]);

    /// <summary>A swing of <paramref name="keys"/>, its whoosh loudest halfway from the key the arm is furthest back at to the hit.</summary>
    private static SwingMotion Of(MotionKey[] keys)
    {
        var back = keys[0];
        foreach (var key in keys)
            if (key.Shoulder < back.Shoulder) back = key;
        return new(MotionKind.Swing, (back.Share + Hit) / 2, keys);
    }
}

/// <summary>
/// The arm as a play leaves it (axe-combos): its turn from straight ahead of the shoulder less the item's lead, its line and the
/// shoulder's turn from it together, the elbow's bend and the wrist's turn.
/// </summary>
public readonly record struct ArmPose(double Turn, double Elbow, double Wrist);

/// <summary>What a play begun as another is replaced starts from (axe-combos): that one's arm, and its body if it moved one.</summary>
public readonly record struct Splice(ArmPose Arm, BodyState? Body);

/// <summary>
/// A motion as one stroke plays it (swing-motions, axe-combos), chosen once at the press by its aim and kept: its
/// <see cref="Sector"/>, its <see cref="Motion"/>, its <see cref="Keys"/>, the item's <see cref="Lead"/>; the overhand's
/// <see cref="Shape"/>, its keys built for its aim; for a fixed motion, the two shares its edge crosses the fan's sides at, sampled
/// then, for a combo's step its cut. It plays over <see cref="Ms"/>, striking at <see cref="HitShare"/> (none for the back to rest);
/// a combo's <see cref="Step"/> moves the <see cref="Body"/> too, from <see cref="BodyFrom"/>, rest for none. <see cref="Aim"/> is
/// the aim angle it was pressed at.
/// </summary>
public sealed record MotionPlay(Sector Sector, SwingMotion Motion, IReadOnlyList<MotionKey> Keys, double Lead,
    (double Raised, double Through)? Shape = null, (double Back, double Through) Shares = default, double Ms = 0, double? HitShare = null,
    int? Step = null, BodyMotion? Body = null, BodyState? BodyFrom = null, double Aim = 0)
{
    /// <summary>How finely a fixed motion's keys are sampled at the press for <see cref="ShareAt"/>: every 1/100 of the stroke.</summary>
    private const int Samples = 100;

    /// <summary>
    /// <paramref name="stroke"/>'s motion for an aim angle of <paramref name="aim"/> radians, as played from the press: its combo's
    /// <paramref name="step"/> where its sector has a combo and the step is in it, else the sector's own motion. Begun as another is
    /// replaced, its first key is <paramref name="from"/>'s pose, so the arm and the body never jump; a step with none starts at rest.
    /// </summary>
    public static MotionPlay Of(Stroke stroke, double aim, int? step = null, Splice? from = null)
    {
        var sector = Sectors.Of(aim);
        double lead = AxeSwing.Lead(stroke.Look);
        if (step is int s && stroke.ComboIn(sector) is { } combo && s >= 0 && s < combo.Count)
        {
            var c = combo[s];
            double line = Sectors.Turned(sector, aim);
            return new(sector, c.Arm, Spliced(c.Arm.Keys, from?.Arm ?? new ArmPose(line, 0, 0), line), lead, null, (c.CutFrom, c.CutTo), c.Ms,
                c.HitShare, s, c.Body, from?.Body, aim);
        }
        var single = Single(stroke, sector, aim, lead);
        return from is { Arm: var arm } && single.Keys.Count > 0 ? single with { Keys = Spliced(single.Keys, arm, single.LineOf(aim)) } : single;
    }

    private static MotionPlay Single(Stroke stroke, Sector sector, double aim, double lead)
    {
        var motion = stroke.Motions[sector];
        var play = new MotionPlay(sector, motion, motion.Keys, lead, Ms: stroke.Ms, HitShare: stroke.HitShare, Aim: aim);
        if (motion.Kind == MotionKind.Thrust) return play;
        if (ReferenceEquals(motion, Overhand.Motion))
        {
            var shape = Overhand.ShapeFor(aim + lead, stroke.Half);
            return play with { Keys = Overhand.KeysFor(shape, stroke.Look.Elbow), Shape = shape };
        }
        var turns = Turns(motion.Keys, stroke.Look, lead);
        return play with { Shares = (Sampled(turns, -stroke.Half), Sampled(turns, stroke.Half)) };
    }

    /// <summary><paramref name="keys"/> begun at <paramref name="from"/>, its shoulder turned by how far apart its line and <paramref name="line"/> are.</summary>
    private static MotionKey[] Spliced(IReadOnlyList<MotionKey> keys, ArmPose from, double line)
    {
        var spliced = new MotionKey[keys.Count];
        for (int i = 0; i < keys.Count; i++) spliced[i] = keys[i];
        spliced[0] = keys[0] with { Shoulder = from.Turn - line, Elbow = from.Elbow, Wrist = from.Wrist };
        return spliced;
    }

    /// <summary>
    /// The back to rest after <paramref name="ended"/>, a combo's step let go (axe-combos): the arm from where the step left it to
    /// rest on its line, the body's channels to rest and the feet home, over <see cref="Combos.BackMs"/>, striking nothing.
    /// </summary>
    public static MotionPlay Back(MotionPlay ended)
    {
        var end = ended.SpliceAt(ended.Ms);
        MotionKey[] keys = [new(0, 0, 0), new(1, 0, 0, 0, Ease.Sine)];
        return new(ended.Sector, new SwingMotion(MotionKind.Swing, null, keys), Spliced(keys, end.Arm, ended.LineOf(ended.Aim)), ended.Lead,
            Ms: Combos.BackMs, Body: end.Body is { } body ? ComboBody.BackOf(body) : null, BodyFrom: end.Body, Aim: ended.Aim);
    }

    /// <summary>The line it plays along for an aim of <paramref name="aim"/>: the overhand along the aim, a fixed motion turned at most half its sector.</summary>
    public double LineOf(double aim) => Shape != null ? aim : Sectors.Turned(Sector, aim);

    /// <summary>The arm at <paramref name="phase"/>, along the line it was pressed along.</summary>
    public ArmPose PoseAt(double phase) => new(LineOf(Aim) + ShoulderAt(Keys, phase), ElbowAt(phase), WristAt(phase));

    /// <summary>What a play replacing this one <paramref name="ms"/> into it starts from: the arm and the body as they are then.</summary>
    public Splice SpliceAt(double ms) => new(PoseAt(Math.Min(1, ms / Ms)), ComboBody.At(this, ms));

    /// <summary>Whether it leaves the arm and the body at rest: a step that does needs no back to rest.</summary>
    public bool EndsAtRest
    {
        get
        {
            const double Near = 1e-9;
            var (arm, body) = SpliceAt(Ms);
            return Math.Abs(arm.Turn - LineOf(Aim)) < Near && Math.Abs(arm.Elbow) < Near && Math.Abs(arm.Wrist) < Near
                && (body is not { } b || b.Near(BodyState.Rest, Near));
        }
    }

    /// <summary>Whether the blade comes up through its hit, as the backhand does: the air's cut goes the way it does.</summary>
    public bool Rising => HitShare is double hit && Keys.Count > 0
        && ShoulderAt(Keys, Math.Min(1, hit + 0.01)) < ShoulderAt(Keys, Math.Max(0, hit - 0.01));

    /// <summary>
    /// The arm's angle from straight ahead of the shoulder at <paramref name="phase"/>, the aim now <paramref name="aim"/>
    /// (<see cref="AxeSwing.ArmAt"/>): the overhand along the aim, a fixed motion turned at most half its sector.
    /// </summary>
    public double AngleAt(double aim, double phase) => LineOf(aim) + Lead + ShoulderAt(Keys, phase);

    /// <summary>The elbow's bend at <paramref name="phase"/>, in radians.</summary>
    public double ElbowAt(double phase) => Motion.Elbow?.Invoke(phase) ?? (Keys.Count > 0 ? Along(Keys, phase, k => k.Elbow) : 0);

    /// <summary>The item's turn against the forearm at <paramref name="phase"/>, in radians, down positive (<see cref="Rig.HeldTurn"/>).</summary>
    public double WristAt(double phase) => Keys.Count > 0 ? Along(Keys, phase, k => k.Wrist) : 0;

    /// <summary>
    /// The share at which the edge's middle is <paramref name="turn"/> radians from the aim line, back (negative) or through: the
    /// overhand's as it always was; a fixed motion's for the fan's sides, sampled at the press.
    /// </summary>
    public double ShareAt(double turn) => Shape is { } shape ? Overhand.ShareAt(turn, shape) : turn < 0 ? Shares.Back : Shares.Through;

    /// <summary>The shoulder's turn from the aim line at <paramref name="phase"/> of <paramref name="keys"/>.</summary>
    internal static double ShoulderAt(IReadOnlyList<MotionKey> keys, double phase) => keys.Count > 0 ? Along(keys, phase, k => k.Shoulder) : 0;

    private static double Along(IReadOnlyList<MotionKey> keys, double phase, Func<MotionKey, double> of)
    {
        if (phase <= keys[0].Share) return of(keys[0]);
        for (int i = 1; i < keys.Count; i++)
        {
            var (was, to) = (keys[i - 1], keys[i]);
            if (phase >= to.Share) continue;
            double t = (phase - was.Share) / (to.Share - was.Share);
            return of(was) + (of(to) - of(was)) * Eased(to.Ease, t);
        }
        return of(keys[^1]);
    }

    /// <summary><paramref name="t"/> of the way from one key to the next, eased by <paramref name="ease"/>.</summary>
    internal static double Eased(Ease ease, double t) => ease switch
    {
        Ease.Sine => (1 - Math.Cos(t * Math.PI)) / 2,
        Ease.In => t * t,
        _ => t,
    };

    /// <summary>
    /// How far the middle of <paramref name="look"/>'s edge is from the aim line at <paramref name="phase"/> of
    /// <paramref name="keys"/>, in radians, down positive: the shoulder, the elbow and the wrist together, held as drawn (0040).
    /// </summary>
    internal static double EdgeTurnAt(IReadOnlyList<MotionKey> keys, HeldLook look, double lead, double phase)
    {
        var (from, to) = AxeSwing.EdgeAt(look, 0, Along(keys, phase, k => k.Elbow), Along(keys, phase, k => k.Wrist));
        double held = Math.Atan2((from.Y + to.Y) / 2, (from.X + to.X) / 2);
        return ShoulderAt(keys, phase) + Math.IEEERemainder(lead + held, 2 * Math.PI);
    }

    /// <summary>The edge's turn from the aim line (<see cref="EdgeTurnAt"/>) every 1/<see cref="Samples"/> of the stroke.</summary>
    private static double[] Turns(IReadOnlyList<MotionKey> keys, HeldLook look, double lead)
    {
        var turns = new double[Samples + 1];
        for (int i = 0; i <= Samples; i++) turns[i] = EdgeTurnAt(keys, look, lead, (double)i / Samples);
        return turns;
    }

    /// <summary>
    /// The first share of <paramref name="turns"/>, after the edge is furthest back, at which it has come down to
    /// <paramref name="turn"/>, or as far through as it goes after.
    /// </summary>
    private static double Sampled(double[] turns, double turn)
    {
        int back = 0;
        for (int i = 1; i < turns.Length; i++)
            if (turns[i] < turns[back]) back = i;
        double most = double.MinValue;
        for (int i = back; i < turns.Length; i++) most = Math.Max(most, turns[i]);
        double wanted = Math.Min(turn, most);
        for (int i = back; i < turns.Length; i++)
            if (turns[i] >= wanted) return (double)i / Samples;
        return 1;
    }
}
