using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Crew;

namespace Stillwake.Game;

/// <summary>How a motion's key is eased into from the one before: evenly, or (1 − cos πt) / 2.</summary>
public enum Ease { Linear, Sine }

/// <summary>
/// One key of a swung motion (swing-motions): at <paramref name="Share"/> of the stroke, the shoulder turned
/// <paramref name="Shoulder"/> radians from the aim line, down positive, the elbow bent <paramref name="Elbow"/>, eased in from the
/// key before by <paramref name="Ease"/>.
/// </summary>
public readonly record struct MotionKey(double Share, double Shoulder, double Elbow, Ease Ease = Ease.Linear);

/// <summary>Where a stroke is aimed, by its aim angle from straight ahead of the shoulder: each sector plays its own motion.</summary>
public enum Sector { Up, Straight, Down, Under }

/// <summary>
/// The four aim sectors (swing-motions), in radians from straight ahead, down positive: up to −45°, to 20°, to 60°, to 90°, a
/// bound in the sector with the smaller angle. A motion is drawn for its sector's middle and turned within half its sector.
/// </summary>
public static class Sectors
{
    /// <summary>Each sector's lower bound and upper bound, in degrees, by <see cref="Sector"/>.</summary>
    private static readonly (double From, double To)[] Bounds = [(-90, -45), (-45, 20), (20, 60), (60, 90)];

    /// <summary>The sector <paramref name="aim"/> lies in, any angle: one in the back half mirrored to the front first.</summary>
    public static Sector Of(double aim)
    {
        double front = Front(aim);
        for (int i = 0; i < Bounds.Length - 1; i++)
            if (front <= Radians(Bounds[i].To)) return (Sector)i;
        return Sector.Under;
    }

    /// <summary><paramref name="aim"/> mirrored into the front half, −π/2 to π/2: the same height, ahead.</summary>
    public static double Front(double aim)
    {
        double a = Math.IEEERemainder(aim, 2 * Math.PI);
        return a > Math.PI / 2 ? Math.PI - a : a < -Math.PI / 2 ? -Math.PI - a : a;
    }

    /// <summary>The middle of <paramref name="sector"/>, the aim its motions are drawn for, in radians.</summary>
    public static double Middle(Sector sector) => Radians((Bounds[(int)sector].From + Bounds[(int)sector].To) / 2);

    /// <summary>Half <paramref name="sector"/>'s width, in radians: the most a fixed motion is turned from its middle.</summary>
    public static double HalfSector(Sector sector) => Radians((Bounds[(int)sector].To - Bounds[(int)sector].From) / 2);

    /// <summary>The line a fixed motion of <paramref name="sector"/> is played along for <paramref name="aim"/>: turned from its middle at most half the sector.</summary>
    public static double Turned(Sector sector, double aim)
    {
        double middle = Middle(sector), half = HalfSector(sector);
        return middle + Math.Clamp(aim - middle, -half, half);
    }

    private static double Radians(double degrees) => degrees * Math.PI / 180;
}

/// <summary>
/// How the arm moves through a stroke, apart from the weapon that plays it (swing-motions): swung through its
/// <paramref name="Keys"/>, or thrust; its whoosh loudest at <paramref name="WhooshShare"/> of it, at the stroke's hit if none. The
/// overhand is <paramref name="Shaped"/>: its keys are built at the press for its aim (<see cref="Overhand.KeysFor"/>). The jab
/// has no keys: its arm is aimed as a gun's, its elbow bent by <paramref name="Elbow"/> at each share.
/// </summary>
public sealed record SwingMotion(StrokeKind Kind, double? WhooshShare, IReadOnlyList<MotionKey> Keys, bool Shaped = false, Func<double, double>? Elbow = null);

/// <summary>The motion a weapon plays in each aim sector (swing-motions).</summary>
public sealed record SectorMotions(SwingMotion Up, SwingMotion Straight, SwingMotion Down, SwingMotion Under)
{
    /// <summary>The same motion in every sector.</summary>
    public static SectorMotions All(SwingMotion motion) => new(motion, motion, motion, motion);

    public SwingMotion this[Sector sector] => sector switch
    {
        Sector.Up => Up,
        Sector.Straight => Straight,
        Sector.Down => Down,
        _ => Under,
    };

    /// <summary>Every sector's, up to under the feet.</summary>
    public IEnumerable<SwingMotion> Each => [Up, Straight, Down, Under];
}

/// <summary>
/// The overhand (0040, swing-motions): raised back over the shoulder for <see cref="WindShare"/> of the stroke, brought down
/// through the aim line to its point in <see cref="StrikeShare"/>, crossing it at <see cref="HitAt"/>, carried on through at
/// <see cref="DownSpeed"/>, eased back to the line. Its keys are built at the press from <see cref="ShapeFor"/>, so an aim high or
/// low is raised or carried through less.
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

    /// <summary>The overhand: its keys built at the press, its whoosh loudest in the middle of the downstroke.</summary>
    public static readonly SwingMotion Motion = new(StrokeKind.Swing, WindShare + StrikeShare / 2, [], Shaped: true);

    /// <summary>
    /// How far a stroke along <paramref name="line"/>, radians from straight ahead of the shoulder, down positive, is raised and
    /// brought through for a fan <paramref name="half"/> radians either side: through the whole fan, at least <see cref="Through"/>,
    /// raised <see cref="Raised"/>, each cut short so the arm passes neither <see cref="AxeSwing.Overhead"/> nor <see cref="AxeSwing.Behind"/>.
    /// </summary>
    public static (double Raised, double Through) ShapeFor(double line, double half)
    {
        double level = Math.PI / 2 - line;
        return (Math.Clamp(AxeSwing.Overhead - level, LeastRaised, Raised),
            Math.Clamp(level + AxeSwing.Behind, LeastThrough, Math.Max(Through, half)));
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
    public static readonly SwingMotion Motion = new(StrokeKind.Thrust, null, [], Elbow: Strokes.ElbowAt);
}

/// <summary>
/// A motion as one stroke plays it (swing-motions), chosen once at the press by its aim and kept: its <see cref="Sector"/>, its
/// <see cref="Motion"/>, its <see cref="Keys"/> (the overhand's built for its aim), the item's <see cref="Lead"/>, and for a fixed
/// motion the two shares its edge crosses the fan's sides at, sampled then.
/// </summary>
public sealed record MotionPlay(Sector Sector, SwingMotion Motion, IReadOnlyList<MotionKey> Keys, double Lead,
    (double Raised, double Through)? Shape = null, (double Back, double Through) Shares = default)
{
    /// <summary><paramref name="stroke"/>'s motion for an aim angle of <paramref name="aim"/> radians, as played from the press.</summary>
    public static MotionPlay Of(Stroke stroke, double aim)
    {
        var sector = Sectors.Of(aim);
        var motion = stroke.Motions[sector];
        double lead = AxeSwing.Lead(stroke.Look);
        if (motion.Kind == StrokeKind.Thrust) return new(sector, motion, motion.Keys, lead);
        if (motion.Shaped)
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
    public static double ShoulderAt(IReadOnlyList<MotionKey> keys, double phase) => keys.Count > 0 ? Along(keys, phase, k => k.Shoulder) : 0;

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

    /// <summary>The first share, every 1/100, after the arm is furthest back, at which it has come down to <paramref name="turn"/>, or as far as it goes.</summary>
    private static double Sampled(IReadOnlyList<MotionKey> keys, double turn)
    {
        double back = 0, most = double.MinValue, least = double.MaxValue;
        foreach (var key in keys)
        {
            if (key.Shoulder < least) (least, back) = (key.Shoulder, key.Share);
            most = Math.Max(most, key.Shoulder);
        }
        double wanted = Math.Min(turn, most);
        for (int i = 0; i <= 100; i++)
            if (i / 100.0 >= back && ShoulderAt(keys, i / 100.0) >= wanted) return i / 100.0;
        return 1;
    }
}
