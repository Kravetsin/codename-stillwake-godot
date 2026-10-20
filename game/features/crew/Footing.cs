using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The ground under a foot, from the hero's feet, in pixels: <see cref="Below"/> how far down it is under the column of its
/// ankle (up is negative), and under the whole boot, heel to toe, the <see cref="Lowest"/> and the <see cref="Highest"/>.
/// </summary>
public readonly record struct Underfoot(double Below, double Lowest, double Highest);

/// <summary>
/// How a boot sits: with its shin as the pose has it, or, as far as <see cref="Level"/> says (0 to 1), level, as the ground pick
/// page draws a boot on a slope. None on the flat.
/// </summary>
public readonly record struct Shoe(double Level);

/// <summary>How the legs stand this frame: each leg's bend and its boot, and how far the hips and body <see cref="Drop"/>, in pixels.</summary>
public readonly record struct Stance(Bend FrontLeg, Bend BackLeg, Shoe FrontShoe, Shoe BackShoe, double Drop);

/// <summary>
/// Where one foot is to stand this frame, from the hero's feet the way it faces: the sole under its ankle, whether it is down,
/// so its hip must reach it, the ground under it, how far the page's walk has taken over (<see cref="Steep"/>), and how high it
/// is lifted over the ground (<see cref="Lift"/>).
/// </summary>
public readonly record struct Footfall(Vec Sole, bool Down, Underfoot Ground, double Steep, double Lift = 0);

/// <summary>
/// The legs on uneven ground, a pass over the pose (<see cref="Poses.LimbsOf"/>) while the hero stands, walks or runs.
/// On a slope the feet go as the ground pick page's walker puts them: each a short stride (<see cref="FootLook.Stride"/>)
/// about the middle, lifted <see cref="FootLook.Lift"/> as it swings, over the ground under it; the hips just low enough for
/// a foot that is down to reach, eased; each leg reaching hip to ankle, knee forward, a swinging one lowered rather than
/// folded past <see cref="FootLook.Fold"/>. The boot stays level, as the page's: a foot that is down rests on the lowest
/// ground under it, so none of it floats, its other end in the ground behind the slope's front lip (<c>GridView</c>,
/// <see cref="FootLook.Lip"/>). Blended in by how steep the ground is, eased, so on the flat the pose's own legs stay.
/// One per figure: it keeps the steepness and the drop between frames.
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

    /// <summary>How far either side of the middle the ground is read for how steep it is.</summary>
    private const double Span = 4;

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
        var front = Fall(pose, true, doing, travelled, _steep, ship!, hero);
        var back = Fall(pose, false, doing, travelled, _steep, ship!, hero);
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

    /// <summary>
    /// Where a foot of <paramref name="pose"/> stands on <paramref name="ship"/>'s ground: as far along and as high as
    /// <see cref="Page"/> says. As the page's walk takes over, a foot that is down sets the end of its level boot over the lower
    /// ground where the page sets its foot, its heel going up a slope, its toe down one, and rests on the lowest ground under the
    /// boot, so none of it floats; a swinging one is lifted over the ground under its ankle.
    /// </summary>
    public static Footfall Fall(Limbs pose, bool front, Pose doing, double travelled, double steep, Ship ship, Hero hero)
    {
        var at = Page(pose, front, doing, travelled, steep);
        double shift = 0;
        if (at.Y < Down && Below(ship, hero, at.X - 1) is double behind && Below(ship, hero, at.X + 1) is double ahead)
            shift = (ahead < behind ? Heel : ahead > behind ? -Toe : 0) * steep;
        return Fall(pose, front, doing, travelled, steep, Under(ship, hero, at.X + shift), shift);
    }

    /// <summary>
    /// Where a foot stands over the ground <paramref name="under"/> its boot (none for none in reach: the hero's own level), the
    /// ankle <paramref name="shift"/> along from where <see cref="Page"/> puts it: over the ground under its ankle on the flat; as
    /// the page's walk takes over, a foot that is down on the lowest ground under its level boot.
    /// </summary>
    public static Footfall Fall(Limbs pose, bool front, Pose doing, double travelled, double steep, Underfoot? under, double shift = 0)
    {
        var at = Page(pose, front, doing, travelled, steep);
        var ground = under ?? default;
        bool down = at.Y < Down;
        double rests = ground.Below + (down ? ground.Lowest - ground.Below : 0) * steep;
        return new Footfall(new Vec(at.X + shift, rests - at.Y), down, ground, steep, Math.Max(0, at.Y));
    }

    /// <summary>
    /// How far the hips drop from the pose's: toward the page's natural height as the page's walk takes over, and lower
    /// for a foot that is down to reach it, at most <see cref="FootLook.MostDrop"/>.
    /// </summary>
    public static double DropFor(Limbs pose, Footfall front, Footfall back, double steep)
    {
        double hips = (ToFigure(HipOf(true), pose, 0).Y + ToFigure(HipOf(false), pose, 0).Y) / 2;
        double drop = (-Rig.Hip * 0.98 - hips) * steep;
        foreach (var (isFront, fall) in (ReadOnlySpan<(bool, Footfall)>)[(true, front), (false, back)])
        {
            if (!fall.Down) continue;
            var hip = ToFigure(HipOf(isFront), pose, 0);
            double reach = Reaches(pose, isFront, steep).Reach;
            var ankle = AnkleFor(fall, Lies(pose, isFront ? pose.FrontLeg : pose.BackLeg));
            double dx = ankle.X - hip.X, dy = ankle.Y - hip.Y;
            drop = Math.Max(drop, dy - Math.Sqrt(Math.Max(0, reach * reach - dx * dx)));
        }
        drop = Math.Max(0, Math.Min(drop, FootLook.MostDrop));
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
        // Turned with the shin, or level, blended.
        double x = (along * c - down * s) * (1 - shoe.Level) + along * shoe.Level;
        double y = (along * s + down * c) * (1 - shoe.Level) + down * shoe.Level;
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
        var ankle = AnkleFor(fall, Lies(pose, leg));
        if (!fall.Down && fall.Steep > 0)
        {
            // Swinging, the foot comes down, into the ground no deeper than its lip hides, before its knee folds past Fold.
            var hipAt = ToFigure(HipOf(front), pose, drop);
            double near = Reaches(pose, front, fall.Steep).Near, dx = ankle.X - hipAt.X;
            if (Math.Abs(dx) < near)
                ankle = ankle with { Y = Math.Max(ankle.Y, Math.Min(hipAt.Y + Math.Sqrt(near * near - dx * dx), fall.Ground.Highest + FootLook.Lip - 1 - Ankle)) };
        }
        var at = ToParts(ankle, pose, drop);
        var hip = HipOf(front);
        var bend = Rig.Reach(new Vec(at.X - hip.X, at.Y - hip.Y), Rig.Leg with { Lower = Rig.Leg.Lower - Ankle }, -1);
        return (bend, fall.Steep == 0 ? default : new Shoe(fall.Steep));
    }

    /// <summary>Where the ankle goes for a footfall: over its sole as the pose's own boot has it on the flat, straight above it, the boot level, on a slope.</summary>
    private static Vec AnkleFor(Footfall fall, double shin) => AnkleOver(fall.Sole, shin, fall.Steep);

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
        double lowest = at, highest = at;
        // Heel to toe a pixel and a half apart, the ends included: a corner under the boot is not missed.
        for (double x = -Heel; x <= Toe + 1e-9; x += (Heel + Toe) / 6)
            if (Below(ship, hero, along + x) is double ground) (lowest, highest) = (Math.Max(lowest, ground), Math.Min(highest, ground));
        return new Underfoot(at, lowest, highest);
    }

    private static double Settled(double from, double to, double share)
    {
        double next = from + (to - from) * share;
        return Math.Abs(next - to) < 1e-3 ? to : next;
    }

    private static double Square(double v) => v * v;
}
