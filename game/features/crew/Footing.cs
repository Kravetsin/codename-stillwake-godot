using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The ground under a foot, from the hero's feet, in pixels: <see cref="Below"/> how far down it is (up is negative), and
/// <see cref="Slope"/> how it runs, down per pixel ahead the way the hero faces, at most 1 (45°) either way.
/// </summary>
public readonly record struct Underfoot(double Below, double Slope);

/// <summary>
/// How a boot sits: with its shin as the pose has it, or, as far as <see cref="Along"/> says (0 to 1), upright and sheared
/// to the ground's <see cref="Slope"/>: each column of its picture shifted up or down with the slope, its sole following the
/// ground in pixel steps, the column under the ankle where it was. None on the flat.
/// </summary>
public readonly record struct Shoe(double Slope, double Along);

/// <summary>How the legs stand this frame: each leg's bend and its boot, and how far the hips and body <see cref="Drop"/>, in pixels.</summary>
public readonly record struct Stance(Bend FrontLeg, Bend BackLeg, Shoe FrontShoe, Shoe BackShoe, double Drop);

/// <summary>
/// Where one foot is to stand this frame, from the hero's feet the way it faces: its sole, whether it is down, so its hip
/// must reach it, the ground under it (how far down and how it runs), and how far the page's walk has taken over
/// (<see cref="Steep"/>): how far its boot is sheared along the ground rather than turned with its shin.
/// </summary>
public readonly record struct Footfall(Vec Sole, bool Down, Underfoot Ground, double Steep);

/// <summary>
/// The legs on uneven ground, a pass over the pose (<see cref="Poses.LimbsOf"/>) while the hero stands, walks or runs.
/// On a slope the feet go as the ground pick page's walker puts them: each a short stride (<see cref="FootLook.Stride"/>)
/// about the middle, lifted <see cref="FootLook.Lift"/> as it swings, over the ground under it; the hips just low enough for
/// a foot that is down to reach, eased, but not so low that a knee folds past <see cref="FootLook.Fold"/> (the lower foot
/// then touches with its toe or heel); each leg reaching hip to ankle, knee forward. The boot is sheared upright to the
/// slope rather than turned, so it stands on it, never in it, and never swings up into the knee. Blended in by how steep
/// the ground is, eased, so on the flat the pose's own legs stay. One per figure: it keeps the steepness and the drop.
/// </summary>
public sealed class Footing
{
    /// <summary>
    /// How far above the sole the ankle is, in pixels: the top of the boot in <c>hero_shin.png</c>, whose sole is the
    /// shin's end (<see cref="Rig.Leg"/>).
    /// </summary>
    public const double Ankle = 4;

    /// <summary>How far the boot reaches ahead of the column under the ankle, to its toe, and behind it, to its heel, in pixels.</summary>
    public const double Toe = 6, Heel = 3;

    /// <summary>The page's: a leg that is down bends at least this much, reaching this share of its length.</summary>
    private const double Straight = 0.97;

    /// <summary>A foot lifted less than this, in pixels, is down (the page's).</summary>
    private const double Down = 0.3;

    /// <summary>How far either side of the middle the ground is read for how steep it is, and either side of a foot for its slope.</summary>
    private const double Span = 4, Lean = 2;

    private double _drop, _steep;

    /// <summary>How steep the ground under the hero is, eased: 0 on the flat, 1 at 45°; how far the page's walk has taken over.</summary>
    public double Steep => _steep;

    /// <summary>
    /// How much quicker than <paramref name="pose"/>'s own the stride runs on the steepest ground: the page's, two radians a
    /// stride's length.
    /// </summary>
    public static double Quicken(Pose pose) =>
        2 / FootLook.Stride / (2 * Math.PI / ((pose == Pose.Run ? Poses.Stride.Run : Poses.Stride.Walk) * Rig.Cell));

    /// <summary>
    /// The legs of <paramref name="pose"/>, the hero <paramref name="doing"/> it <paramref name="travelled"/> cells into its
    /// stride (as <see cref="Moment.Travelled"/>), on <paramref name="ship"/>'s ground, <paramref name="elapsedMs"/> after the
    /// last frame. With no ship, or not standing, walking or running, the pose's own legs, the hips easing back up.
    /// </summary>
    public Stance Place(Limbs pose, Pose doing, double travelled, Ship? ship, Hero hero, double elapsedMs)
    {
        double share = 1 - Math.Pow(1 - FootLook.Ease, elapsedMs / Hero.StepMs);
        bool on = ship != null && doing is Pose.Stand or Pose.Walk or Pose.Run;
        _steep = Settled(_steep, on ? SteepUnder(ship!, hero) : 0, share);
        if (!on)
        {
            _drop = Settled(_drop, 0, share);
            return new Stance(pose.FrontLeg, pose.BackLeg, default, default, _drop);
        }
        var front = Fall(pose, true, doing, travelled, _steep, Under(ship!, hero, Page(pose, true, doing, travelled, _steep).X));
        var back = Fall(pose, false, doing, travelled, _steep, Under(ship!, hero, Page(pose, false, doing, travelled, _steep).X));
        _drop = Settled(_drop, DropFor(pose, front, back, _steep), share);
        return Solve(pose, front, back, _drop);
    }

    /// <summary>
    /// Where a foot of <paramref name="pose"/> goes along the ground and how high it is lifted, before the ground is read: the
    /// pose's own, blended by <paramref name="steep"/> toward the page's walker. Its <see cref="Vec.Y"/> is the lift, up positive.
    /// </summary>
    public static Vec Page(Limbs pose, bool front, Pose doing, double travelled, double steep)
    {
        var own = FootOf(pose, front);
        if (steep == 0) return new Vec(own.X, -own.Y);
        int side = front ? 1 : -1;
        double x, lift;
        if (doing == Pose.Stand) (x, lift) = (side * (Rig.HipApart + 0.6), 0);
        else
        {
            // In step with the pose: its front foot farthest ahead a quarter stride in, where the page's is at phase 0.
            double cycle = doing == Pose.Run ? Poses.Stride.Run : Poses.Stride.Walk;
            double phase = travelled / cycle * 2 * Math.PI - Math.PI / 2 + (front ? 0 : Math.PI);
            x = side * Rig.HipApart * 0.4 + FootLook.Stride / 2 * Math.Cos(phase);
            lift = Math.Max(0, -Math.Sin(phase)) * (doing == Pose.Run ? FootLook.RunLift : FootLook.Lift);
        }
        return new Vec(own.X + (x - own.X) * steep, -own.Y + (lift + own.Y) * steep);
    }

    /// <summary>Where a foot stands over the ground <paramref name="under"/> it (none for none in reach: the hero's own level): as far along and as high over it as <see cref="Page"/> says.</summary>
    public static Footfall Fall(Limbs pose, bool front, Pose doing, double travelled, double steep, Underfoot? under)
    {
        var at = Page(pose, front, doing, travelled, steep);
        var ground = under ?? default;
        return new Footfall(new Vec(at.X, ground.Below - at.Y), at.Y < Down, ground, steep);
    }

    /// <summary>
    /// How far the hips drop from the pose's: toward the page's natural height as the page's walk takes over, and lower
    /// for a foot that is down to reach it; never so far that a foot that is down folds its knee tighter than
    /// <see cref="FootLook.Fold"/> (the lower then reaches with its toe or heel), and at most <see cref="FootLook.MostDrop"/>.
    /// </summary>
    public static double DropFor(Limbs pose, Footfall front, Footfall back, double steep)
    {
        double hips = (ToFigure(HipOf(true), pose, 0).Y + ToFigure(HipOf(false), pose, 0).Y) / 2;
        double drop = (-Rig.Hip * 0.98 - hips) * steep, most = FootLook.MostDrop;
        foreach (var (isFront, fall) in (ReadOnlySpan<(bool, Footfall)>)[(true, front), (false, back)])
        {
            var hip = ToFigure(HipOf(isFront), pose, 0);
            var leg = isFront ? pose.FrontLeg : pose.BackLeg;
            var (reach, near) = Reaches(pose, isFront, steep);
            // A swinging foot can come down to the ground, no lower: the knee is kept from folding past Fold there too.
            var sole = fall.Down ? fall.Sole : fall.Sole with { Y = fall.Ground.Below };
            var ankle = AnkleOver(sole, Lies(pose, leg), fall.Steep);
            double dx = ankle.X - hip.X, dy = ankle.Y - hip.Y;
            if (fall.Down) drop = Math.Max(drop, dy - Math.Sqrt(Math.Max(0, reach * reach - dx * dx)));
            if (Math.Abs(dx) < near && (fall.Down || fall.Steep > 0)) most = Math.Min(most, dy - Math.Sqrt(near * near - dx * dx));
        }
        drop = Math.Max(0, Math.Min(drop, most));
        return drop < 1e-6 ? 0 : drop;
    }

    /// <summary>
    /// How far from its hip one leg of <paramref name="pose"/> reaches its ankle: as far as the pose's own or nearly straight,
    /// as the page's to its sole; and as near as the pose's folds or <see cref="FootLook.Fold"/>, as the slope takes over.
    /// </summary>
    private static (double Reach, double Near) Reaches(Limbs pose, bool front, double steep)
    {
        var hip = ToFigure(HipOf(front), pose, 0);
        var posed = AnkleOver(FootOf(pose, front), Lies(pose, front ? pose.FrontLeg : pose.BackLeg), 0);
        double length = Rig.Leg.Upper + Rig.Leg.Lower - Ankle, own = Math.Sqrt(Square(posed.X - hip.X) + Square(posed.Y - hip.Y));
        double straight = Math.Max(own, Straight * length), folded = Math.Min(own, FootLook.Fold * length);
        return (straight + (Straight * length - straight) * steep, folded + (FootLook.Fold * length - folded) * steep);
    }

    /// <summary>The legs of <paramref name="pose"/> with the hips dropped <paramref name="drop"/>, each foot where its footfall says; one out of reach is reached for straight and hangs.</summary>
    public static Stance Solve(Limbs pose, Footfall front, Footfall back, double drop)
    {
        var (frontLeg, frontShoe) = Leg(pose, true, front, drop);
        var (backLeg, backShoe) = Leg(pose, false, back, drop);
        return new Stance(frontLeg, backLeg, frontShoe, backShoe, drop);
    }

    /// <summary>Where the sole of <paramref name="leg"/> is, its shin's end, from the hero's feet the way it faces, with the body dropped <paramref name="drop"/>.</summary>
    public static Vec SoleOf(Limbs pose, bool front, Bend leg, double drop)
    {
        var hip = HipOf(front);
        var end = Rig.EndOf(leg, Rig.Leg);
        return ToFigure(new Vec(hip.X + end.X, hip.Y + end.Y), pose, drop);
    }

    /// <summary>Where the knee of <paramref name="leg"/> is, from the hero's feet the way it faces, with the body dropped <paramref name="drop"/>.</summary>
    public static Vec KneeOf(Limbs pose, bool front, Bend leg, double drop)
    {
        var hip = HipOf(front);
        return ToFigure(new Vec(hip.X + Math.Sin(leg.Upper) * Rig.Leg.Upper, hip.Y + Math.Cos(leg.Upper) * Rig.Leg.Upper), pose, drop);
    }

    /// <summary>
    /// Where a point of the boot of <paramref name="leg"/>, sat as <paramref name="shoe"/>, is: <paramref name="along"/> pixels
    /// ahead of the column under the ankle, <paramref name="down"/> pixels below the ankle (<see cref="Ankle"/> at the sole).
    /// </summary>
    public static Vec BootAt(Limbs pose, bool front, Bend leg, Shoe shoe, double drop, double along, double down)
    {
        double shin = Lies(pose, leg);
        var ankle = AnkleOver(SoleOf(pose, front, leg, drop), shin, 0);
        var (c, s) = (Math.Cos(shin), Math.Sin(shin));
        // Turned with the shin, or upright and sheared to the slope, blended.
        double x = (along * c - down * s) * (1 - shoe.Along) + along * shoe.Along;
        double y = (along * s + down * c) * (1 - shoe.Along) + (down + along * shoe.Slope) * shoe.Along;
        return new Vec(ankle.X + x, ankle.Y + y);
    }

    /// <summary>How the shin of <paramref name="leg"/> is turned from straight down, radians clockwise the way the hero faces.</summary>
    public static double Lies(Limbs pose, Bend leg) => pose.Tilt - (leg.Upper + leg.Lower);

    private static (Bend Leg, Shoe Shoe) Leg(Limbs pose, bool front, Footfall fall, double drop)
    {
        var leg = front ? pose.FrontLeg : pose.BackLeg;
        if (drop == 0 && fall.Sole == FootOf(pose, front) && fall.Steep == 0) return (leg, default);
        // Hip to ankle, knee forward: the ankle over the sole as the pose's boot has it on the flat, straight above it as the
        // sheared boot has it on a slope.
        var ankle = AnkleOver(fall.Sole, Lies(pose, leg), fall.Steep);
        if (!fall.Down && fall.Steep > 0)
        {
            // Swinging, the foot comes down toward the ground, no further than onto it, before its knee folds past Fold.
            var hipAt = ToFigure(HipOf(front), pose, drop);
            double near = Reaches(pose, front, fall.Steep).Near, dx = ankle.X - hipAt.X;
            if (Math.Abs(dx) < near)
                ankle = ankle with { Y = Math.Max(ankle.Y, Math.Min(hipAt.Y + Math.Sqrt(near * near - dx * dx), ankle.Y + fall.Ground.Below - fall.Sole.Y)) };
        }
        var at = ToParts(ankle, pose, drop);
        var hip = HipOf(front);
        var bend = Rig.Reach(new Vec(at.X - hip.X, at.Y - hip.Y), Rig.Leg with { Lower = Rig.Leg.Lower - Ankle }, -1);
        if (fall.Steep == 0) return (bend, default);
        // A foot that is down but short of the ground, its leg straight, reaches it with its toe or heel: its boot sheared
        // steeper than the slope, by no more than Tip, till that end touches.
        double slope = fall.Ground.Slope;
        if (fall.Down)
        {
            var reached = AnkleOver(SoleOf(pose, front, bend, drop), Lies(pose, bend), 0);
            double Gap(double along) => fall.Ground.Below + slope * (reached.X + along - fall.Sole.X) - (reached.Y + Ankle + along * slope);
            if (Gap(0) > 0.05)
            {
                double toe = slope + Gap(Toe) / Toe, heel = slope - Gap(-Heel) / Heel;
                double tipped = Math.Abs(toe - slope) < Math.Abs(heel - slope) ? toe : heel;
                slope += Math.Clamp(tipped - slope, -FootLook.Tip, FootLook.Tip);
            }
        }
        return (bend, new Shoe(slope, fall.Steep));
    }

    /// <summary>The ankle over a sole at <paramref name="sole"/>: along a shin turned <paramref name="shin"/>, or straight above it as far as <paramref name="upright"/> says.</summary>
    private static Vec AnkleOver(Vec sole, double shin, double upright) =>
        new(sole.X + Ankle * Math.Sin(shin) * (1 - upright), sole.Y - Ankle * (Math.Cos(shin) * (1 - upright) + upright));

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

    /// <summary>How steep the ground is under the hero's middle, 0 to 1 (45°); flat where it is not read either side.</summary>
    private static double SteepUnder(Ship ship, Hero hero) =>
        Below(ship, hero, -Span) is double l && Below(ship, hero, Span) is double r ? Math.Min(1, Math.Abs(r - l) / (2 * Span)) : 0;

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
