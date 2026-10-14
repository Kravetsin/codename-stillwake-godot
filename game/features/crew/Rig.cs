using System;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Crew;

/// <summary>A point in pixels of the world, or a direction: +x forward, +y down.</summary>
public readonly record struct Vec(double X, double Y);

/// <summary>
/// Two angles for a limb, in radians: <see cref="Upper"/> from its joint, hanging straight down at 0
/// and turned forward when positive; <see cref="Lower"/> the bend at the elbow or knee, forward when
/// positive.
/// </summary>
public readonly record struct Bend(double Upper, double Lower);

/// <summary>A limb's two segments, joint to joint, and its width, in pixels.</summary>
public readonly record struct Segments(double Upper, double Lower, double Width);

/// <summary>
/// The hero's figure as joints and limbs (apps/game/src/features/crew/rig.ts): a chibi with a helmet
/// near a third of its height, in pixels of the world.
/// </summary>
public static class Rig
{
    /// <summary>Pixels per cell at zoom 1, as the camera draws the world.</summary>
    public const double Cell = 16;

    public const double W = (double)Spec.Width / Hero.Unit * Cell;
    public const double H = (double)Spec.Height / Hero.Unit * Cell;

    /// <summary>Where the joints sit, in pixels above the feet.</summary>
    public const double Hip = H * 0.42, Shoulder = H * 0.61, Neck = H * 0.71;

    /// <summary>Hips apart, so two legs read as two even standing still.</summary>
    public const double HipApart = W * 0.14;

    public static readonly Segments Arm = new(H * 0.145, H * 0.145, W * 0.27);

    /// <summary>A leg's reach is the hip's height.</summary>
    public static readonly Segments Leg = new(Hip * 0.5, Hip * 0.5, W * 0.34);

    /// <summary>Where a limb bent as <paramref name="bend"/> ends, from its joint.</summary>
    public static Vec EndOf(Bend bend, Segments size)
    {
        double turn = bend.Upper + bend.Lower;
        return new Vec(
            Math.Sin(bend.Upper) * size.Upper + Math.Sin(turn) * size.Lower,
            Math.Cos(bend.Upper) * size.Upper + Math.Cos(turn) * size.Lower);
    }

    /// <summary>
    /// The bend that puts a limb's end at <paramref name="target"/>, from its joint, or as near as it
    /// reaches. With <paramref name="bends"/> 1 the middle joint sits on the side the angles fall, as an
    /// elbow below a hand held before the chest; with -1 on the side they rise, as a knee in front.
    /// </summary>
    public static Bend Reach(Vec target, Segments size, int bends)
    {
        double a = size.Upper, b = size.Lower;
        double far = Math.Sqrt(target.X * target.X + target.Y * target.Y);
        // Never quite straight nor quite folded, where the angles turn unstable.
        double d = Math.Min(a + b - 1e-3, Math.Max(Math.Abs(a - b) + 1e-3, far));
        double toward = Math.Atan2(target.X, target.Y);
        double atJoint = Math.Acos(Math.Clamp((a * a + d * d - b * b) / (2 * a * d), -1, 1));
        double atBend = Math.Acos(Math.Clamp((a * a + b * b - d * d) / (2 * a * b), -1, 1));
        return new Bend(toward - bends * atJoint, bends * (Math.PI - atBend));
    }
}
