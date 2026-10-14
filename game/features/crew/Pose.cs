using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>What a hero's figure is doing, read from its movement: never decided anywhere else.</summary>
public enum Pose { Stand, Walk, Air, Climb, Float, Lie, Tumble }

/// <summary>
/// Each limb's bend (<see cref="Rig"/>), and how the whole body is placed: <see cref="Tilt"/> radians
/// it turns forward around its middle, a body lying down at -π/2; <see cref="Lift"/> pixels it rises,
/// drifting in space, and on the floor below zero by as much as the lower foot is short of it, so bent
/// legs never float and straight ones never sink in; <see cref="Lying"/> whether it rests on the floor
/// however it is turned, rather than standing on its feet.
/// </summary>
public readonly record struct Limbs(Bend FrontArm, Bend BackArm, Bend FrontLeg, Bend BackLeg, double Tilt, double Lift, bool Lying);

/// <summary>
/// Where the figure is in its motion: <see cref="Travelled"/>, the cells moved in this pose, sets the
/// stride so feet keep pace with the floor; <see cref="TimeMs"/> drives what moves without travel,
/// breathing and drifting; <see cref="Landing"/> runs from 1 at touching down to 0; <see cref="Plan"/>,
/// the head's turn, while the hands hold the plan.
/// </summary>
public readonly record struct Moment(double Travelled, double TimeMs, double? Landing = null, double? Plan = null);

/// <summary>The limbs for each pose (apps/game/src/features/crew/pose.ts), and where the head looks.</summary>
public static class Poses
{
    /// <summary>Cells walked, climbed or swum per full cycle of the limbs: two steps, two strokes.</summary>
    public static class Stride
    {
        public const double Walk = 3.6, Climb = 1.2, Float = 3;
    }

    /// <summary>How long a landing takes to spring back, and how far, in pixels, the body sinks into it.</summary>
    public static class Land
    {
        public const double Ms = 220, Sink = Rig.Hip * 0.22;
    }

    /// <summary>How far the head turns toward the pointer, in radians: up, and down.</summary>
    public static class Head
    {
        public const double Up = 0.6, Down = 0.5;
    }

    /// <summary>
    /// The plan's sheet: <see cref="Length"/> along its face; <see cref="Ahead"/> how far its far edge
    /// rises, looking ahead, and <see cref="PerLook"/> how much a look turns it.
    /// </summary>
    public static class Sheet
    {
        public const double Length = Rig.W * 0.85, Ahead = 0.45, PerLook = 0.6;
    }

    /// <summary>
    /// The plan held before the chest: the front hand <see cref="Arm"/> radians forward of hanging, turned
    /// by <see cref="PerLook"/> of the head's turn, <see cref="Reach"/> of the arm's length out; the far
    /// hand <see cref="FarHand"/> along it.
    /// </summary>
    public static class Hold
    {
        public const double Arm = 1.05, PerLook = 0.8, Reach = 0.8, FarHand = 0.35;
    }

    private const double BreathMs = 2600, DriftMs = 3400;
    /// <summary>Radians a hero passed out in space turns per second.</summary>
    private const double Tumble = 0.7;
    /// <summary>The share of a climbing cycle a hand or foot holds on, sliding down as the body rises past it.</summary>
    private const double Grip = 0.6;
    /// <summary>Within this many cells of the body's middle the pointer turns nobody: no flicker over the hero.</summary>
    private const double FaceDeadzone = 0.25;

    /// <summary>
    /// The head's turn toward a point <paramref name="dx"/> ahead of its neck, the way the hero faces, and
    /// <paramref name="dy"/> below it; positive looks down. Behind the hero it only looks up or down, as at
    /// a point as far ahead.
    /// </summary>
    public static double HeadTurn(double dx, double dy)
    {
        if (dx == 0 && dy == 0) return 0;
        return Math.Max(-Head.Up, Math.Min(Head.Down, Math.Atan2(dy, Math.Abs(dx))));
    }

    /// <summary>How <paramref name="hero"/>'s head turns to look at <paramref name="point"/>, in cells; straight ahead with no point.</summary>
    public static double LookOf(Hero hero, (double X, double Y)? point)
    {
        if (point is not { } p) return 0;
        double neckX = (double)hero.X / Hero.Unit, neckY = (double)hero.Y / Hero.Unit - Rig.Neck / Rig.Cell;
        return HeadTurn((p.X - neckX) * hero.Facing, p.Y - neckY);
    }

    /// <summary>The side of <paramref name="hero"/> the pointer is on, in cells; none without a pointer, and its own right over the hero.</summary>
    public static int? FaceOf((double X, double Y)? pointer, Hero hero)
    {
        if (pointer is not { } p) return null;
        double dx = p.X - (double)hero.X / Hero.Unit;
        if (Math.Abs(dx) < FaceDeadzone) return hero.Facing;
        return dx < 0 ? -1 : 1;
    }

    /// <summary>Whether the hands can hold the plan in <paramref name="pose"/>: not on a ladder, and not passed out.</summary>
    public static bool HoldsIn(Pose pose) => pose is not (Pose.Climb or Pose.Lie or Pose.Tumble);

    /// <summary>The sheet's turn, clockwise, for the head's turn <paramref name="look"/>.</summary>
    public static double SheetTurn(double look) => -Sheet.Ahead + look * Sheet.PerLook;

    /// <summary>Where the front hand holds the plan, from the shoulder, for the head's turn <paramref name="look"/>.</summary>
    public static Vec PlanHand(double look)
    {
        double angle = Hold.Arm - look * Hold.PerLook;
        double outward = Hold.Reach * (Rig.Arm.Upper + Rig.Arm.Lower);
        return new Vec(Math.Sin(angle) * outward, Math.Cos(angle) * outward);
    }

    /// <summary>The pose of <paramref name="hero"/>: its body in space or not, passed out or not.</summary>
    public static Pose PoseOf(Hero hero, bool inSpace, bool passedOut)
    {
        if (inSpace) return passedOut ? Pose.Tumble : Pose.Float;
        if (passedOut) return Pose.Lie;
        if (hero.Climbing) return Pose.Climb;
        // Standing on something, the controller leaves no fall speed.
        if (hero.Vy != 0) return Pose.Air;
        return hero.Vx == 0 ? Pose.Stand : Pose.Walk;
    }

    private static Bend B(double upper, double lower) => new(upper, lower);
    private static double Positive(double n) => Math.Max(0, n);

    /// <summary>
    /// The limbs for <paramref name="pose"/> at <paramref name="moment"/>. Swinging limbs are set by their
    /// angles; hands and feet that must be somewhere, on a ladder's rungs, on the floor after a landing,
    /// on the plan, by where they are.
    /// </summary>
    public static Limbs LimbsOf(Pose pose, Hero hero, Moment moment)
    {
        var body = BodyOf(pose, hero, moment);
        var landed = pose is Pose.Stand or Pose.Walk ? Grounded(LandingOf(body, moment.Landing)) : body;
        if (moment.Plan is not double look || !HoldsIn(pose)) return landed;
        var front = PlanHand(look);
        double along = Hold.FarHand * Sheet.Length;
        double turn = SheetTurn(look);
        var far = new Vec(front.X + Math.Cos(turn) * along, front.Y + Math.Sin(turn) * along);
        return landed with { FrontArm = Rig.Reach(front, Rig.Arm, 1), BackArm = Rig.Reach(far, Rig.Arm, 1) };
    }

    /// <summary>On the floor, the body comes down until its lower foot stands on it.</summary>
    private static Limbs Grounded(Limbs limbs)
    {
        double lowest = Math.Max(Rig.EndOf(limbs.FrontLeg, Rig.Leg).Y, Rig.EndOf(limbs.BackLeg, Rig.Leg).Y);
        return limbs with { Lift = lowest - Rig.Hip };
    }

    /// <summary>Touching down, the knees bend deep and straighten again, the feet under the hips.</summary>
    private static Limbs LandingOf(Limbs limbs, double? landing)
    {
        if (landing is not double l || l == 0) return limbs;
        // Down fast and up slower: the deepest a third of the way through.
        double t = 1 - l;
        double sink = Land.Sink * (t < 1.0 / 3 ? t * 3 : (1 - t) * 1.5);
        Bend Foot(double x) => Rig.Reach(new Vec(x, Rig.Hip - sink), Rig.Leg, -1);
        return limbs with
        {
            FrontLeg = Foot(Rig.W * 0.08),
            BackLeg = Foot(-Rig.W * 0.06),
            FrontArm = B(0.55, 0.6),
            BackArm = B(0.35, 0.7),
        };
    }

    private static Limbs BodyOf(Pose pose, Hero hero, Moment moment)
    {
        double Phase(double cycle) => moment.Travelled / cycle * 2 * Math.PI;
        double Wave(double periodMs) => Math.Sin(moment.TimeMs / periodMs * 2 * Math.PI);
        switch (pose)
        {
            case Pose.Stand:
                {
                    double breath = Wave(BreathMs);
                    return new Limbs(B(0.05 * breath, 0.18), B(-0.05 * breath, 0.22), B(0.02, -0.04), B(-0.02, -0.04), 0, 0, false);
                }
            case Pose.Walk:
                {
                    double p = Phase(Stride.Walk), s = Math.Sin(p), c = Math.Cos(p);
                    // A leg swinging forward lifts its knee; the one under the body stays straight.
                    return new Limbs(
                        FrontArm: B(-0.45 * s, 0.3 + 0.35 * Positive(-c)),
                        BackArm: B(0.45 * s, 0.3 + 0.35 * Positive(c)),
                        FrontLeg: B(0.55 * s, -(0.08 + 0.9 * Positive(c))),
                        BackLeg: B(-0.55 * s, -(0.08 + 0.9 * Positive(-c))),
                        Tilt: 0.05, Lift: 0, Lying: false);
                }
            case Pose.Air:
                // Rising, knees tucked and arms up; falling, legs reach for the floor and arms out.
                return hero.Vy < 0
                    ? new Limbs(B(2.3, 0.35), B(1.9, 0.5), B(1, -1.3), B(0.25, -0.8), 0, 0, false)
                    : new Limbs(B(1.4, 0.4), B(1.1, 0.5), B(0.35, -0.45), B(-0.05, -0.3), 0, 0, false);
            case Pose.Climb:
                return Climbing(moment.Travelled / Stride.Climb);
            case Pose.Float:
                return Floating(hero, Phase(Stride.Float), Wave(DriftMs));
            case Pose.Lie:
                return new Limbs(B(0.4, 0.6), B(-0.3, 0.3), B(0.35, -0.7), B(0.05, -0.2), -Math.PI / 2, 0, true);
            default:
                return new Limbs(B(1.4, 0.6), B(0.6, 0.9), B(0.5, -0.9), B(-0.2, -0.5), moment.TimeMs / 1000 * Tumble, 0, false);
        }
    }

    /// <summary>
    /// Where a hand or foot on a ladder is at <paramref name="cycle"/> through its climb: holding on from
    /// <paramref name="top"/> down to <paramref name="top"/> + <paramref name="range"/>, then let go and
    /// brought back up, out from the ladder by <paramref name="outward"/> on the way.
    /// </summary>
    private static Vec OnRung(double cycle, double top, double range, double x, double outward)
    {
        double p = cycle - Math.Floor(cycle);
        if (p < Grip) return new Vec(x, top + p / Grip * range);
        double q = (p - Grip) / (1 - Grip);
        return new Vec(x - outward * Math.Sin(q * Math.PI), top + (1 - q) * range);
    }

    /// <summary>Hands over the head taking rungs in turn, each with the opposite foot, knees coming up.</summary>
    private static Limbs Climbing(double cycle)
    {
        double arm = Rig.Arm.Upper + Rig.Arm.Lower;
        // Out past the helmet, so the hands are never across the visor.
        Vec Hand(double at) => OnRung(at, -arm * 0.85, arm * 0.6, Rig.W * 0.6, Rig.W * 0.2);
        Vec Foot(double at) => OnRung(at, Rig.Hip - arm * 0.75, arm * 0.7, Rig.W * 0.25, Rig.W * 0.25);
        // Elbows point down and back, as when pulling up.
        return new Limbs(
            FrontArm: Rig.Reach(Hand(cycle), Rig.Arm, -1),
            BackArm: Rig.Reach(Hand(cycle + 0.5), Rig.Arm, -1),
            FrontLeg: Rig.Reach(Foot(cycle + 0.5), Rig.Leg, -1),
            BackLeg: Rig.Reach(Foot(cycle), Rig.Leg, -1),
            Tilt: 0, Lift: 0, Lying: false);
    }

    /// <summary>
    /// Swimming in no gravity: a breaststroke, arms reaching forward and pulling to the sides, then
    /// folding back, with a frog kick; at rest, limbs loose and drifting.
    /// </summary>
    private static Limbs Floating(Hero hero, double phase, double drift)
    {
        bool moving = hero.Vx != 0 || hero.Vy != 0;
        // Leaning into a swim along the floor, upright otherwise.
        double tilt = hero.Vx != 0 ? 0.35 : 0.08;
        double lift = 1.5 * drift;
        if (!moving)
            return new Limbs(
                B(0.75 + 0.12 * drift, 0.7), B(0.5 - 0.12 * drift, 0.8),
                B(0.4 + 0.08 * drift, -0.65), B(-0.05 - 0.08 * drift, -0.45), tilt, lift, false);
        double c = Math.Cos(phase), s = Math.Sin(phase);
        // Reaching at the start of the cycle, pulled to the sides halfway, elbows folding on the way back.
        Bend Arm(double lag) => B(1.55 + 1.15 * c - lag, 0.3 + 1.2 * Positive(-s));
        Bend Leg(double lag) => B(0.45 + 0.35 * s - lag, -(0.2 + 1.2 * Positive(s)));
        return new Limbs(Arm(0), Arm(0.25), Leg(0), Leg(0.3), tilt, lift, false);
    }
}
