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
        new(WindShare, -shape.Raised, elbow, Ease.Sine),
        new(HitAt, 0, elbow),
        new(HitAt + shape.Through / DownSpeed, shape.Through, elbow),
        new(1, 0, elbow, Ease.Sine),
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
/// A motion as one stroke plays it (swing-motions), chosen once at the press by its aim and kept: its <see cref="Sector"/>, its
/// <see cref="Motion"/>, its <see cref="Keys"/>, the item's <see cref="Lead"/>; the overhand's <see cref="Shape"/>, its keys built
/// for its aim; for a fixed motion, the two shares its edge crosses the fan's sides at, sampled then.
/// </summary>
public sealed record MotionPlay(Sector Sector, SwingMotion Motion, IReadOnlyList<MotionKey> Keys, double Lead,
    (double Raised, double Through)? Shape = null, (double Back, double Through) Shares = default)
{
    /// <summary>How finely a fixed motion's keys are sampled at the press for <see cref="ShareAt"/>: every 1/100 of the stroke.</summary>
    private const int Samples = 100;

    /// <summary><paramref name="stroke"/>'s motion for an aim angle of <paramref name="aim"/> radians, as played from the press.</summary>
    public static MotionPlay Of(Stroke stroke, double aim)
    {
        var sector = Sectors.Of(aim);
        var motion = stroke.Motions[sector];
        double lead = AxeSwing.Lead(stroke.Look);
        if (motion.Kind == MotionKind.Thrust) return new(sector, motion, motion.Keys, lead);
        if (ReferenceEquals(motion, Overhand.Motion))
        {
            var shape = Overhand.ShapeFor(aim + lead, stroke.Half);
            return new(sector, motion, Overhand.KeysFor(shape, stroke.Look.Elbow), lead, shape);
        }
        return new(sector, motion, motion.Keys, lead, null, (Sampled(motion.Keys, -stroke.Half), Sampled(motion.Keys, stroke.Half)));
    }

    /// <summary>
    /// The arm's angle from straight ahead of the shoulder at <paramref name="phase"/>, the aim now <paramref name="aim"/>
    /// (<see cref="AxeSwing.ArmAt"/>): the overhand along the aim, a fixed motion turned at most half its sector.
    /// </summary>
    public double AngleAt(double aim, double phase) => (Shape != null ? aim : Sectors.Turned(Sector, aim)) + Lead + ShoulderAt(Keys, phase);

    /// <summary>The elbow's bend at <paramref name="phase"/>, in radians.</summary>
    public double ElbowAt(double phase) => Motion.Elbow?.Invoke(phase) ?? (Keys.Count > 0 ? Along(Keys, phase, k => k.Elbow) : 0);

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
            double eased = to.Ease == Ease.Sine ? (1 - Math.Cos(t * Math.PI)) / 2 : t;
            return of(was) + (of(to) - of(was)) * eased;
        }
        return of(keys[^1]);
    }

    /// <summary>The first share, every 1/<see cref="Samples"/>, after the arm is furthest back, at which it has come down to <paramref name="turn"/>, or as far as it goes.</summary>
    private static double Sampled(IReadOnlyList<MotionKey> keys, double turn)
    {
        double back = 0, most = double.MinValue, least = double.MaxValue;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i].Shoulder < least) (least, back) = (keys[i].Shoulder, keys[i].Share);
            most = Math.Max(most, keys[i].Shoulder);
        }
        double wanted = Math.Min(turn, most);
        for (int i = 0; i <= Samples; i++)
        {
            double share = (double)i / Samples;
            if (share >= back && ShoulderAt(keys, share) >= wanted) return share;
        }
        return 1;
    }
}
