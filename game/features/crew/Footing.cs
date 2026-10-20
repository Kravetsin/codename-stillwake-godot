using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The ground under a foot, from the hero's feet, in pixels: <see cref="Below"/> how far down it is (up is negative), and
/// <see cref="Slope"/> how it runs, down per pixel ahead the way the hero faces, at most 1 (45°) either way.
/// </summary>
public readonly record struct Underfoot(double Below, double Slope);

/// <summary>
/// How the legs stand this frame: each leg's bend, how far each boot turns from its shin to lie along the ground (radians,
/// clockwise the way the hero faces), and how far the hips and body <see cref="Drop"/>, in pixels.
/// </summary>
public readonly record struct Stance(Bend FrontLeg, Bend BackLeg, double FrontBoot, double BackBoot, double Drop);

/// <summary>
/// Foot placement on uneven ground, a pass over the pose (<see cref="Poses.LimbsOf"/>) while the hero stands, walks or
/// runs: each foot keeps the pose's place along the ground and its height above it, the ground under it as the client's
/// grid has it, slopes too; its boot turns to lie along the slope; the hips drop, eased, just enough for the lower foot to
/// reach; each leg is solved again hip to foot, the knee forward. On flat ground it changes nothing. One per figure: it
/// keeps the drop and the stride's squeeze between frames.
/// </summary>
public sealed class Footing
{
    /// <summary>
    /// How far above the sole the boot turns, in pixels: the top of the boot in <c>hero_shin.png</c>, whose sole is the
    /// shin's end (<see cref="Rig.Leg"/>).
    /// </summary>
    public const double Ankle = 4;

    /// <summary>How far the boot reaches ahead of the sole under the ankle, to its toe, and behind it, to its heel, in pixels.</summary>
    public const double Toe = 6, Heel = 3;

    /// <summary>A planted leg never needs the hips lower than this share of its length straight lets it reach.</summary>
    private const double Straight = 0.97;

    /// <summary>Nor nearer its hip than this share of its length lets its ankle come: a knee folded past it reads as a kneel.</summary>
    private const double Fold = 0.45;

    /// <summary>How far either side of the middle the ground is read for how steep it is, and either side of a foot for its slope.</summary>
    private const double Span = 4, Lean = 2;

    private double _drop, _squeeze = 1;

    /// <summary>The share of the stride's width the steps keep on steep ground, 1 on the flat: the stride runs quicker by as much.</summary>
    public double Squeeze => _squeeze;

    /// <summary>
    /// The legs of <paramref name="pose"/> for <paramref name="hero"/> standing on <paramref name="ship"/>'s ground,
    /// <paramref name="elapsedMs"/> after the last frame; with no ship, off the ground, the pose's own legs, the hips
    /// easing back up.
    /// </summary>
    public Stance Place(Limbs pose, Ship? ship, Hero hero, double elapsedMs)
    {
        double share = 1 - Math.Pow(1 - FootLook.Ease, elapsedMs / Hero.StepMs);
        if (ship == null)
        {
            (_drop, _squeeze) = (Settled(_drop, 0, share), Settled(_squeeze, 1, share));
            return new Stance(pose.FrontLeg, pose.BackLeg, 0, 0, _drop);
        }
        double steep = Below(ship, hero, -Span) is double l && Below(ship, hero, Span) is double r ? Math.Min(1, Math.Abs(r - l) / (2 * Span)) : 0;
        _squeeze = Settled(_squeeze, 1 - FootLook.Squeeze * steep, share);
        var front = Under(ship, hero, FootOf(pose, true).X * _squeeze);
        var back = Under(ship, hero, FootOf(pose, false).X * _squeeze);
        _drop = Settled(_drop, DropFor(pose, front, back, _squeeze), share);
        return Solve(pose, front, back, _squeeze, _drop);
    }

    /// <summary>
    /// How far the hips drop for each foot of <paramref name="pose"/>, its place along squeezed by <paramref name="squeeze"/>,
    /// to reach the ground under it, <paramref name="front"/> and <paramref name="back"/> (none for no ground in reach): at most
    /// <see cref="FootLook.MostDrop"/>, and never so far that a foot on higher ground folds its knee past
    /// <see cref="Fold"/>; the lower foot hangs short instead. 0 where the pose's legs reach already.
    /// </summary>
    public static double DropFor(Limbs pose, Underfoot? front, Underfoot? back, double squeeze)
    {
        var (frontNeeds, frontAllows) = Bounds(pose, true, front, squeeze);
        var (backNeeds, backAllows) = Bounds(pose, false, back, squeeze);
        double drop = Math.Min(Math.Min(Math.Max(frontNeeds, backNeeds), Math.Min(frontAllows, backAllows)), FootLook.MostDrop);
        return drop < 1e-6 ? 0 : drop;
    }

    /// <summary>
    /// For one foot, how far the hips must drop for its ankle to reach where its boot stands on the ground under it, and how
    /// far they may before its knee folds tighter than <see cref="Fold"/> or the pose's own, the foot let down onto the ground.
    /// </summary>
    private static (double Needs, double Allows) Bounds(Limbs pose, bool front, Underfoot? under, double squeeze)
    {
        if (under is not { } u) return (0, double.MaxValue);
        var hip = ToFigure(HipOf(front), pose, 0);
        var sole = FootOf(pose, front);
        var ankle = AnkleFor(pose, front, sole, u, squeeze, 0);
        var (reach, near) = Reaches(pose, front);
        double dx = ankle.X - hip.X, dy = ankle.Y - hip.Y, down = dy + Math.Max(0, -sole.Y);
        return (dy - Math.Sqrt(Math.Max(0, reach * reach - dx * dx)), Math.Abs(dx) >= near ? double.MaxValue : down - Math.Sqrt(near * near - dx * dx));
    }

    /// <summary>
    /// How far from its hip one leg of <paramref name="pose"/> reaches its ankle: as far as the pose's own, or nearly
    /// straight; and as near as it folds, or <see cref="Fold"/>.
    /// </summary>
    private static (double Reach, double Near) Reaches(Limbs pose, bool front)
    {
        var leg = front ? pose.FrontLeg : pose.BackLeg;
        var hip = ToFigure(HipOf(front), pose, 0);
        var posed = AnkleOf(SoleOf(pose, front, leg, 0), Lies(pose, leg, 0));
        double length = Rig.Leg.Upper + Rig.Leg.Lower - Ankle, own = Math.Sqrt(Square(posed.X - hip.X) + Square(posed.Y - hip.Y));
        return (Math.Max(own, Straight * length), Math.Min(own, Fold * length));
    }


    /// <summary>
    /// The legs of <paramref name="pose"/> with the hips dropped <paramref name="drop"/>, each foot's place along squeezed
    /// by <paramref name="squeeze"/> and lowered by the ground under it, its boot along the slope; a foot out of reach is
    /// reached for straight and hangs. With flat ground, no drop and no squeeze, the pose itself.
    /// </summary>
    public static Stance Solve(Limbs pose, Underfoot? front, Underfoot? back, double squeeze, double drop)
    {
        var (frontLeg, frontBoot) = Leg(pose, true, front ?? default, squeeze, drop);
        var (backLeg, backBoot) = Leg(pose, false, back ?? default, squeeze, drop);
        return new Stance(frontLeg, backLeg, frontBoot, backBoot, drop);
    }

    /// <summary>Where the sole of <paramref name="leg"/> is, from the hero's feet the way it faces, with the body dropped <paramref name="drop"/>.</summary>
    public static Vec SoleOf(Limbs pose, bool front, Bend leg, double drop)
    {
        var hip = HipOf(front);
        var end = Rig.EndOf(leg, Rig.Leg);
        return ToFigure(new Vec(hip.X + end.X, hip.Y + end.Y), pose, drop);
    }

    /// <summary>Where the boot of <paramref name="leg"/>, turned <paramref name="boot"/> from its shin, touches down: its sole under the ankle.</summary>
    public static Vec TurnedSoleOf(Limbs pose, bool front, Bend leg, double boot, double drop)
    {
        var ankle = AnkleOf(SoleOf(pose, front, leg, drop), Lies(pose, leg, 0));
        double lies = Lies(pose, leg, boot);
        return new Vec(ankle.X - Ankle * Math.Sin(lies), ankle.Y + Ankle * Math.Cos(lies));
    }

    /// <summary>How the sole of <paramref name="leg"/>'s boot, turned <paramref name="boot"/> from its shin, lies: radians clockwise from level, the way the hero faces.</summary>
    public static double Lies(Limbs pose, Bend leg, double boot) => pose.Tilt - (leg.Upper + leg.Lower) + boot;

    private static (Bend Leg, double Boot) Leg(Limbs pose, bool front, Underfoot under, double squeeze, double drop)
    {
        var leg = front ? pose.FrontLeg : pose.BackLeg;
        if (under == default && squeeze == 1 && drop == 0) return (leg, 0);
        var sole = FootOf(pose, front);
        var hip = ToFigure(HipOf(front), pose, drop);
        // A lifted foot comes down toward the ground, no further than onto it, rather than fold its knee past Fold.
        var ankle = AnkleFor(pose, front, sole, under, squeeze, 0);
        double near = Reaches(pose, front).Near, dx = ankle.X - hip.X;
        double lower = Math.Abs(dx) >= near ? 0 : Math.Clamp(hip.Y + Math.Sqrt(near * near - dx * dx) - ankle.Y, 0, Math.Max(0, -sole.Y));
        if (lower > 0) ankle = AnkleFor(pose, front, sole, under, squeeze, lower);
        var at = ToParts(ankle, pose, drop);
        var parts = HipOf(front);
        // Hip to ankle: the boot below it lies as the ground does, whatever the shin does.
        var bend = Rig.Reach(new Vec(at.X - parts.X, at.Y - parts.Y), Rig.Leg with { Lower = Rig.Leg.Lower - Ankle }, -1);
        return (bend, LiesFor(pose, front, sole with { Y = sole.Y + lower }, under) - Lies(pose, bend, 0));
    }

    /// <summary>
    /// How a foot's boot lies on <paramref name="under"/>, its sole at <paramref name="sole"/>: as the pose's shin has it, but on
    /// a slope turned no further from it than its toe or heel clears the ground, as high as the sole is lifted; flush on it
    /// standing there. On the flat, as the pose has it.
    /// </summary>
    private static double LiesFor(Limbs pose, bool front, Vec sole, Underfoot under)
    {
        double slope = Math.Atan(under.Slope), lift = Math.Max(0, -sole.Y) * Math.Cos(slope), loose = (1 - Math.Abs(under.Slope)) * Math.PI;
        double down = Math.Asin(Math.Min(1, lift / Toe)) + loose, up = Math.Asin(Math.Min(1, lift / Heel)) + loose;
        return slope + Math.Clamp(Lies(pose, front ? pose.FrontLeg : pose.BackLeg, 0) - slope, -up, down);
    }

    /// <summary>
    /// Where the ankle goes for its boot, lying as <see cref="LiesFor"/> says, to touch the ground where the pose's sole would,
    /// let down <paramref name="lower"/> toward it.
    /// </summary>
    private static Vec AnkleFor(Limbs pose, bool front, Vec sole, Underfoot under, double squeeze, double lower)
    {
        var at = new Vec(sole.X * squeeze, sole.Y + under.Below + lower);
        return AnkleOf(at, LiesFor(pose, front, sole with { Y = sole.Y + lower }, under));
    }

    /// <summary>The ankle over a boot's sole at <paramref name="sole"/>, lying <paramref name="lies"/>.</summary>
    private static Vec AnkleOf(Vec sole, double lies) => new(sole.X + Ankle * Math.Sin(lies), sole.Y - Ankle * Math.Cos(lies));


    /// <summary>The pose's sole, from the hero's feet.</summary>
    private static Vec FootOf(Limbs pose, bool front) => SoleOf(pose, front, front ? pose.FrontLeg : pose.BackLeg, 0);

    private static Vec HipOf(bool front) => new(front ? Rig.HipApart : -Rig.HipApart, -Rig.Hip);

    private static Vec Turned(Vec v, double by)
    {
        double c = Math.Cos(by), s = Math.Sin(by);
        return new Vec(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    /// <summary>
    /// A point of the legs' frame (the body's parts, hanging from its middle as <c>Figure</c> places them), from the hero's
    /// feet: turned with the body by its tilt round the middle, lifted by its lift and lowered by <paramref name="drop"/>.
    /// </summary>
    private static Vec ToFigure(Vec p, Limbs pose, double drop)
    {
        var turned = Turned(new Vec(p.X, p.Y + Rig.H / 2), pose.Tilt);
        return new Vec(turned.X, turned.Y - Rig.H / 2 - pose.Lift + drop);
    }

    private static Vec ToParts(Vec f, Limbs pose, double drop)
    {
        var turned = Turned(new Vec(f.X, f.Y + Rig.H / 2 + pose.Lift - drop), -pose.Tilt);
        return new Vec(turned.X, turned.Y - Rig.H / 2);
    }

    /// <summary>How far below the hero's feet the ground is at <paramref name="along"/> pixels ahead of them; none in reach.</summary>
    private static double? Below(Ship ship, Hero hero, double along)
    {
        int x = hero.X + (int)Math.Round(hero.Facing * along / Rig.Cell * Hero.Unit);
        int reach = (int)(FootLook.Reach / Rig.Cell * Hero.Unit);
        return Hero.GroundAt(ship, x, hero.Y, reach, reach) is int ground ? (ground - hero.Y) * Rig.Cell / Hero.Unit : null;
    }

    private static Underfoot? Under(Ship ship, Hero hero, double along)
    {
        if (Below(ship, hero, along) is not double at) return null;
        double ahead = Below(ship, hero, along + Lean) ?? at, behind = Below(ship, hero, along - Lean) ?? at;
        return new Underfoot(at, Math.Clamp((ahead - behind) / (2 * Lean), -1, 1));
    }

    private static double Settled(double from, double to, double share)
    {
        double next = from + (to - from) * share;
        return Math.Abs(next - to) < 1e-3 ? to : next;
    }

    private static double Square(double v) => v * v;
}
