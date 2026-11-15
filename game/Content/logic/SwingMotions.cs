using System;
using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>How a motion moves the arm: swung through its keys, or thrust, the arm aimed as a gun is.</summary>
public enum MotionKind { Swing, Thrust }

/// <summary>
/// How a motion's key is eased into from the one before: evenly, by (1 − cos πt) / 2, by t², slow off the key before and
/// fastest into this one, or by 1 − (1 − t)², a jolt off it slowing into this one (a recoil, body-answers).
/// </summary>
public enum Ease { Linear, Sine, In, Out }

/// <summary>
/// One key of a swung motion (swing-motions): at <paramref name="Share"/> of the stroke, the shoulder turned
/// <paramref name="Shoulder"/> radians from the aim line, down positive, the elbow bent <paramref name="Elbow"/>, the item turned
/// <paramref name="Wrist"/> radians against the forearm, down (clockwise) positive as the shoulder, eased in from the key before
/// by <paramref name="Ease"/>.
/// </summary>
public readonly record struct MotionKey(double Share, double Shoulder, double Elbow, double Wrist = 0, Ease Ease = Ease.Linear);

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

    /// <summary>How far past a bound, in radians, an angle still counts as on it: a mirrored bound lands a rounding off.</summary>
    private const double OnBound = 1e-9;

    /// <summary>The sector <paramref name="aim"/> lies in, any angle: one in the back half mirrored to the front first.</summary>
    public static Sector Of(double aim)
    {
        double front = Front(aim);
        for (int i = 0; i < Bounds.Length - 1; i++)
            if (front <= Radians(Bounds[i].To) + OnBound) return (Sector)i;
        return Sector.Under;
    }

    /// <summary><paramref name="aim"/> mirrored into the front half, −π/2 to π/2: the same height, ahead.</summary>
    private static double Front(double aim)
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
/// <paramref name="Keys"/>, or thrust; its whoosh loudest at <paramref name="WhooshShare"/> of it, at the stroke's hit if none. A
/// thrust has no keys: its arm is aimed as a gun's, its elbow bent by <paramref name="Elbow"/> at each share.
/// </summary>
public sealed record SwingMotion(MotionKind Kind, double? WhooshShare, IReadOnlyList<MotionKey> Keys, Func<double, double>? Elbow = null);

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
}
