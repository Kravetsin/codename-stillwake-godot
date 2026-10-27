using System;
using Feel = Stillwake.Game.HeroMotion;
using Rung = Stillwake.Game.HeroMotion.Ladder;

namespace Stillwake.Game.Features.Crew;

/// <summary>What a hero's figure is doing, read from its movement: never decided anywhere else.</summary>
public enum Pose { Stand, Walk, Run, Air, Climb, Float, Lie, Tumble, Sit, Strapped }

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
/// the head's turn, while the hands hold the plan; <see cref="OutMs"/>, how long since the hero passed out,
/// <see cref="Slump"/>, what picks the heap it ends in, and <see cref="OutTilt"/>, how the body was turned
/// as it passed out, which in space it keeps. Afloat, <see cref="Swim"/> and <see cref="Fast"/> blend from
/// drifting still into swimming and on into swimming fast, each 0 to 1; the figure eases them, and with
/// none they follow the hero's speed at once. <see cref="Wields"/> while the front hand holds an item, the
/// axe or the harpoon: afloat, or flailing in a long fall, that arm keeps calm. <see cref="Momentum"/>, what the figure
/// read from the motion over time (<see cref="Motion"/>, 0090): the lean, the landing's depth, the jump's and
/// the fall's timers; none for a body at rest in it. <see cref="Ledge"/>, on a ladder, how far its top is over the feet in
/// pixels (<see cref="Hero.LedgeAbove"/>), or in the air the ledge a jump steps up onto (<see cref="Hero.MantleLedge"/>): what
/// the hands pull the body up onto (0092); none away from one.
/// </summary>
public readonly record struct Moment(double Travelled, double TimeMs, double? Landing = null, double? Plan = null,
    double? OutMs = null, int Slump = 0, double OutTilt = 0, double? Swim = null, double? Fast = null, bool Wields = false,
    Momentum? Momentum = null, double? Ledge = null);

/// <summary>The limbs for each pose (apps/game/src/features/crew/pose.ts), and where the head looks.</summary>
public static class Poses
{
    /// <summary>Cells walked per full cycle of the legs: two steps. A climb's is <c>Ladder.Stride</c> cells (<see cref="Ladder"/>).</summary>
    public static class Stride
    {
        public const double Walk = 5.6, Run = 9.2;
    }

    /// <summary>
    /// Sliding down a ladder (0092): both hands high on the rails, the legs hanging straight, the feet off the rungs.
    /// </summary>
    public static class OnLadder
    {
        public static readonly Limbs Slide = new(B(2.78, 0.12), B(2.58, 0.18), B(0.2, -0.15), B(-0.12, -0.08), 0, 0, false);
    }

    /// <summary>
    /// Afloat, upright (0036): swimming along the floor the body leans into it by <see cref="Lean"/>
    /// radians, by <see cref="LeanFast"/> swimming fast, and sinking it tucks forward by <see cref="Tuck"/>
    /// more; rising it stays upright. The free hand paddles once every <see cref="PaddleMs"/>, or
    /// <see cref="PaddleFastMs"/> swimming fast, timed rather than by distance, so a swim on the diagonal
    /// keeps its pace. Still, the body rocks once every <see cref="SwayMs"/>.
    /// </summary>
    public static class Adrift
    {
        public const double Lean = 0.22, LeanFast = 0.38, Tuck = 0.06;
        public const double PaddleMs = 1100, PaddleFastMs = 700, SwayMs = 5200;
    }

    /// <summary>How far, in pixels, the body sinks into a landing at depth 1; how deep and how long by the fall is <see cref="Motion"/>'s (0090).</summary>
    public static class Land
    {
        public const double Sink = Rig.Hip * 0.22;
    }

    /// <summary>
    /// The air's poses (0090): <see cref="Rise"/>, knees tucked and arms up; <see cref="Fall"/>, legs reaching for the floor
    /// and arms out; <see cref="Top"/>, the knees drawn to the chest at a jump's top; <see cref="Push"/>, the legs straight
    /// under the body pushing off, the arms thrown up.
    /// </summary>
    public static class Airborne
    {
        public static readonly Limbs Rise = new(B(2.3, 0.35), B(1.9, 0.5), B(1, -1.3), B(0.25, -0.8), 0, 0, false);
        public static readonly Limbs Fall = new(B(1.4, 0.4), B(1.1, 0.5), B(0.35, -0.45), B(-0.05, -0.3), 0, 0, false);
        public static readonly Limbs Top = new(B(2.0, 0.5), B(1.7, 0.6), B(1.25, -1.6), B(0.6, -1.3), 0, 0, false);
        public static readonly Limbs Push = new(B(2.6, 0.2), B(2.2, 0.25), B(-0.1, -0.08), B(-0.35, -0.1), 0, 0, false);
    }

    /// <summary>
    /// Braking out of a run: the front leg planted straight ahead, the back one folding under, the arms thrown forward. Mixed
    /// in as far as the body leans back from slowing (<see cref="Motion"/>).
    /// </summary>
    public static readonly Limbs Brake = new(B(0.7, 0.45), B(0.5, 0.6), B(0.55, -0.05), B(-0.3, -0.95), 0, 0, false);

    /// <summary>
    /// Sitting on an edge in despair (the title, 2026-10-25-title-scene): the hips on it at the feet's point, the thighs out over
    /// it, the shins hanging alike, barely stirring; hunched forward by <see cref="Slump"/>, the arms hanging limp between the
    /// knees; a slow breath every <see cref="BreathMs"/>, the legs drifting once every <see cref="SwingMs"/>; every
    /// <see cref="SighMs"/> a heavy sigh, the body rising by <see cref="SighLift"/> over <see cref="SighLongMs"/> and sinking back.
    /// </summary>
    public static class Seated
    {
        public const double Slump = 0.44, BreathMs = 5600, SwingMs = 13000, SighMs = 15000, SighLongMs = 4200, SighLift = 0.12;
    }

    /// <summary>How far into a heavy sigh a seated hero is, <paramref name="timeMs"/> in, 0 to 1: a quick breath in, a long one out.</summary>
    public static double Sigh(double timeMs)
    {
        double into = timeMs % Seated.SighMs;
        if (into >= Seated.SighLongMs) return 0;
        double t = into / Seated.SighLongMs;
        return t < 0.3 ? Math.Sin(t / 0.3 * Math.PI / 2) : Math.Pow(Math.Cos((t - 0.3) / 0.7 * Math.PI / 2), 2);
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
    /// <summary>Within this many cells of the body's middle the pointer turns nobody: no flicker over the hero.</summary>
    private const double FaceDeadzone = 0.5;

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

    /// <summary>Whether the hands can hold the plan or a tool in <paramref name="pose"/>: on a ladder too (0077: dig while climbing), not passed out nor sitting.</summary>
    public static bool HoldsIn(Pose pose) => pose is not (Pose.Lie or Pose.Tumble or Pose.Sit or Pose.Strapped);

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
        if (hero.Vx == 0) return Pose.Stand;
        return Math.Abs(hero.Vx) > Stillwake.Content.Hero.Walk ? Pose.Run : Pose.Walk;
    }

    /// <summary>
    /// Whether <paramref name="hero"/>, in <paramref name="now"/> after <paramref name="was"/>, has just
    /// jumped: off the deck or a ladder into the air, rising at least a quarter of a jump's push, so a tap's
    /// halved rise counts too (0089). Walking off an edge leaves the hero falling, not rising. Read from the
    /// motion alone, so others' heroes, seen up to a send late and a few steps into the rise, jump as their own
    /// peer saw them.
    /// </summary>
    public static bool Jumped(Pose? was, Pose now, Hero hero) =>
        was is Pose.Stand or Pose.Walk or Pose.Run or Pose.Climb && now == Pose.Air && hero.Vy <= -Stillwake.Content.Hero.Jump / 4;

    /// <summary>
    /// Whether the hero has just landed, in <paramref name="now"/> after <paramref name="was"/>, having last
    /// fallen at <paramref name="fall"/> in the air. At a jump's top, or with its head against a ceiling, the
    /// controller leaves it no speed for a step, which reads as standing; it lands only coming down.
    /// </summary>
    public static bool Landed(Pose? was, Pose now, int fall) =>
        was == Pose.Air && now is Pose.Stand or Pose.Walk or Pose.Run && fall > 0;

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
        var motion = moment.Momentum ?? Momentum.AtRest;
        // Slowing to a stop, the body brakes as far as it leans back from it.
        if (pose is Pose.Stand or Pose.Walk && motion.Lean < 0) body = Mix(body, Brake, Math.Clamp(-motion.Lean / Feel.Lean.Back, 0, 1));
        var landed = pose is Pose.Stand or Pose.Walk or Pose.Run ? Grounded(LandingOf(Leaned(body, motion), moment.Landing, motion.Depth)) : body;
        if (pose == Pose.Run && moment.Landing is not > 0) landed = landed with { Lift = landed.Lift + Bounce(moment.Travelled) };
        if (moment.Plan is not double look || !HoldsIn(pose)) return landed;
        var front = PlanHand(look);
        double along = Hold.FarHand * Sheet.Length;
        double turn = SheetTurn(look);
        var far = new Vec(front.X + Math.Cos(turn) * along, front.Y + Math.Sin(turn) * along);
        // Afloat one hand holds it and the other swims on.
        if (pose == Pose.Float) return landed with { FrontArm = Rig.Reach(front, Rig.Arm, 1) };
        return landed with { FrontArm = Rig.Reach(front, Rig.Arm, 1), BackArm = Rig.Reach(far, Rig.Arm, 1) };
    }

    /// <summary>On the floor, the body comes down until its lower foot stands on it.</summary>
    private static Limbs Grounded(Limbs limbs)
    {
        double lowest = Math.Max(Rig.EndOf(limbs.FrontLeg, Rig.Leg).Y, Rig.EndOf(limbs.BackLeg, Rig.Leg).Y);
        return limbs with { Lift = lowest - Rig.Hip };
    }

    /// <summary>
    /// On the floor, the body leans into its acceleration (0090). No skid pose: tapping left and right made the legs dance
    /// on the spot.
    /// </summary>
    private static Limbs Leaned(Limbs limbs, Momentum motion) => limbs with { Tilt = limbs.Tilt + motion.Lean };

    /// <summary>
    /// Touching down, the knees bend deep and straighten again, the feet under the hips; <paramref name="depth"/> times the
    /// sink of <see cref="Land.Sink"/> (0090).
    /// </summary>
    private static Limbs LandingOf(Limbs limbs, double? landing, double depth)
    {
        if (landing is not double l || l == 0) return limbs;
        // Down fast and up slower: the deepest a third of the way through.
        double t = 1 - l;
        double sink = Land.Sink * depth * (t < 1.0 / 3 ? t * 3 : (1 - t) * 1.5);
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
                        FrontArm: B(-0.3 * s, 0.25 + 0.25 * Positive(-c)),
                        BackArm: B(0.3 * s, 0.25 + 0.25 * Positive(c)),
                        FrontLeg: B(0.5 * s, -(0.08 + 0.8 * Positive(c))),
                        BackLeg: B(-0.5 * s, -(0.08 + 0.8 * Positive(-c))),
                        Tilt: 0.03, Lift: 0, Lying: false);
                }
            case Pose.Run:
                {
                    double p = Phase(Stride.Run), s = Math.Sin(p);
                    double q = p / (2 * Math.PI);
                    // Leaning well in, the arms pumping hard against the legs, elbows opening as they swing back; each leg
                    // pushes off nearly straight under the body, then folds heel to seat and drives its knee high.
                    double c = Math.Cos(p);
                    return new Limbs(
                        FrontArm: B(-Feel.Run.Pump * s, 1.5 + 0.2 * c),
                        BackArm: B(Feel.Run.Pump * s, 1.5 - 0.2 * c),
                        FrontLeg: Running(q),
                        BackLeg: Running(q + 0.5),
                        Tilt: Feel.Run.Lean, Lift: 0, Lying: false);
                }
            case Pose.Air:
                return InTheAir(hero, moment);
            case Pose.Climb:
                return Climbing(hero, moment.Ledge);
            case Pose.Float:
                return Floating(hero, moment.TimeMs, moment.Swim ?? (Swimming(hero) ? 1 : 0), moment.Fast ?? (SwimmingFast(hero) ? 1 : 0),
                    moment.Wields);
            case Pose.Lie:
                // With no time given, long since: lying still.
                return Collapse(moment.OutMs ?? 1e9, moment.Slump);
            case Pose.Sit:
                return Sitting(moment.TimeMs);
            case Pose.Strapped:
                return Strapped(moment.TimeMs);
            default:
                return Limp(moment);
        }
    }

    /// <summary>
    /// In the air (0090): from rising into falling as the speed up or down says, the knees tucked near the top, the legs
    /// pushing straight just after a jump, the arms flailing a long fall (the front one calm while it holds an item), and
    /// the body tipped into a fast way across.
    /// </summary>
    private static Limbs InTheAir(Hero hero, Moment moment)
    {
        var motion = moment.Momentum ?? Momentum.AtRest;
        double k = Math.Clamp((double)(hero.Vy - Feel.Air.RiseBelow) / Feel.Air.Blend, 0, 1);
        var limbs = Mix(Airborne.Rise, Airborne.Fall, k * k * (3 - 2 * k));
        if (TopOf(hero.Vy) is var top and > 0) limbs = Mix(limbs, Airborne.Top, top);
        if (PushOf(motion.SinceJumpMs) is var push and > 0) limbs = Mix(limbs, Airborne.Push, push);
        if (FlailOf(hero.Vy, motion.FallingMs) is var flail and > 0)
        {
            double t = moment.TimeMs;
            limbs = limbs with
            {
                FrontArm = moment.Wields ? limbs.FrontArm : limbs.FrontArm with { Upper = limbs.FrontArm.Upper + 0.35 * flail * Math.Sin(t / 70) },
                BackArm = limbs.BackArm with { Upper = limbs.BackArm.Upper - 0.3 * flail * Math.Sin(t / 85 + 1) },
                FrontLeg = limbs.FrontLeg with { Upper = limbs.FrontLeg.Upper + 0.15 * flail * Math.Sin(t / 110) },
            };
        }
        limbs = limbs with { Tilt = DriftOf(hero) };
        // Stepping up onto a ledge the jump fell just short of (0092): the ladder top's pull-up, at its end.
        return moment.Ledge is double ledge ? PullUp(limbs, ledge) : limbs;
    }

    /// <summary>
    /// How far over the feet the higher foot is drawn in the air at <paramref name="vy"/>, in pixels (0092): the knees tucked
    /// rising and at a jump's top, hanging falling. A jump whose feet fall short of a ledge by less, against it, steps up onto
    /// it (<see cref="Hero"/>'s ledge assist): it looked as if the foot was on it, so it is.
    /// </summary>
    public static double FootLift(int vy)
    {
        var air = InTheAir(new Hero(0, 0, 0, vy, false, 1), new Moment(0, 0));
        return Rig.Hip - Math.Min(Rig.EndOf(air.FrontLeg, Rig.Leg).Y, Rig.EndOf(air.BackLeg, Rig.Leg).Y);
    }

    /// <summary>How far the knees tuck at a jump's top, moving <paramref name="vy"/> up or down: up to <c>Air.Tuck</c> at still.</summary>
    public static double TopOf(int vy)
    {
        int apex = Feel.Air.Apex;
        return Math.Abs(vy) < apex ? (1 - (double)Math.Abs(vy) / apex) * Feel.Air.Tuck : 0;
    }

    /// <summary>How far the legs push straight <paramref name="sinceJumpMs"/> after a jump: from 1 to none over <c>Air.PushMs</c>.</summary>
    public static double PushOf(double sinceJumpMs) =>
        sinceJumpMs < Feel.Air.PushMs ? 1 - sinceJumpMs / Feel.Air.PushMs : 0;

    /// <summary>How hard the arms flail falling at <paramref name="vy"/> for <paramref name="fallingMs"/>: from none to 1.</summary>
    public static double FlailOf(int vy, double fallingMs) =>
        vy > Feel.Air.FlailSpeed && fallingMs > Feel.Air.FlailAfterMs ? Math.Clamp((fallingMs - Feel.Air.FlailAfterMs) / Feel.Air.FlailMs, 0, 1) : 0;

    /// <summary>How far the body tips into its way across in the air, forward when it goes the way it faces.</summary>
    public static double DriftOf(Hero hero) =>
        Math.Abs(hero.Vx) > Feel.Air.DriftSpeed ? Feel.Air.Drift * Math.Sign(hero.Vx) * hero.Facing : 0;

    /// <summary>
    /// Passing out: the knees give way for <see cref="BuckleMs"/>, the body topples onto its back for
    /// <see cref="FallMs"/>, faster and faster, and the limbs flop and settle for <see cref="FlopMs"/>.
    /// </summary>
    public static class Faint
    {
        public const double BuckleMs = 280, FallMs = 380, FlopMs = 600, Ms = BuckleMs + FallMs + FlopMs;
    }

    /// <summary>A number from <paramref name="lo"/> to <paramref name="hi"/>, the same for a slump and a limb every time.</summary>
    private static double Pick(int slump, int k, double lo, double hi)
    {
        double x = Math.Sin(slump * 12.9898 + k * 78.233) * 43758.5453;
        return lo + (hi - lo) * (x - Math.Floor(x));
    }

    /// <summary>
    /// A hero passing out <paramref name="ms"/> ago, as a rag doll: limp arms, knees buckling, a topple
    /// onto its back, and a flop into a heap of its own, picked by <paramref name="slump"/>.
    /// </summary>
    private static Limbs Collapse(double ms, int slump)
    {
        var standing = new Limbs(B(0.05, 0.18), B(-0.05, 0.22), B(0.02, -0.04), B(-0.02, -0.04), 0, 0, true);
        var buckled = new Limbs(B(0.15, 0.35), B(-0.1, 0.3), B(0.75, -1.5), B(0.55, -1.3), -0.12, -Rig.Hip * 0.4, true);
        var heap = new Limbs(
            B(Pick(slump, 0, -0.5, 2.4), Pick(slump, 1, 0.1, 1.3)),
            B(Pick(slump, 2, -0.6, 1.6), Pick(slump, 3, 0.1, 1.2)),
            B(Pick(slump, 4, -0.1, 0.9), -Pick(slump, 5, 0.05, 1.3)),
            B(Pick(slump, 6, -0.2, 0.7), -Pick(slump, 7, 0.05, 1.0)),
            -Math.PI / 2, 0, true);
        if (ms < Faint.BuckleMs)
        {
            double u = ms / Faint.BuckleMs;
            return Mix(standing, buckled, u * u * (3 - 2 * u));
        }
        ms -= Faint.BuckleMs;
        if (ms < Faint.FallMs)
        {
            double v = ms / Faint.FallMs;
            var falling = Mix(buckled, heap, v);
            // The arms trail the falling body and swing up past it; the body only gathers speed.
            double trail = 1.2 * Math.Sin(v * Math.PI);
            return falling with
            {
                FrontArm = falling.FrontArm with { Upper = falling.FrontArm.Upper + trail },
                BackArm = falling.BackArm with { Upper = falling.BackArm.Upper + trail * 0.8 },
                Tilt = -0.12 + (0.12 - Math.PI / 2) * v * v,
                Lift = -Rig.Hip * 0.4 * (1 - v),
            };
        }
        ms -= Faint.FallMs;
        // Hitting the floor, everything bounces and wobbles a little, then lies still.
        double wobble = Math.Exp(-ms / 140) * Math.Sin(ms / 35);
        Bend Flop(Bend b, double by) => new(b.Upper + 0.3 * by * wobble, b.Lower + 0.5 * by * wobble);
        return heap with
        {
            FrontArm = Flop(heap.FrontArm, 1),
            BackArm = Flop(heap.BackArm, -0.8),
            FrontLeg = Flop(heap.FrontLeg, 0.7),
            BackLeg = Flop(heap.BackLeg, -0.6),
            Tilt = -Math.PI / 2 + 0.06 * wobble,
            Lift = 1.5 * Math.Max(0, wobble),
        };
    }

    /// <summary>
    /// Passed out in space: the body stays turned as it was, the limbs gone slack, floating up a little
    /// and stirring slowly, each faint its own way.
    /// </summary>
    private static Limbs Limp(Moment moment)
    {
        int slump = moment.Slump;
        double stir = Math.Sin(moment.TimeMs / DriftMs * 2 * Math.PI);
        return new Limbs(
            B(Pick(slump, 0, 0.9, 1.7) + 0.08 * stir, Pick(slump, 1, 0.4, 1.0)),
            B(Pick(slump, 2, 0.5, 1.3) - 0.08 * stir, Pick(slump, 3, 0.4, 1.1)),
            B(Pick(slump, 4, 0.2, 0.7) + 0.05 * stir, -Pick(slump, 5, 0.3, 1.0)),
            B(Pick(slump, 6, -0.1, 0.4) - 0.05 * stir, -Pick(slump, 7, 0.2, 0.8)),
            moment.OutTilt, 0, false);
    }

    /// <summary>
    /// Sitting on a ledge (<see cref="Seated"/>): the body lowered by the hip's height, so the hips rest where the feet would
    /// stand, the thighs level ahead and the shins hanging over the edge, the breath and the sighs lifting the shoulders.
    /// </summary>
    private static Limbs Sitting(double timeMs)
    {
        double breath = Math.Sin(timeMs / Seated.BreathMs * 2 * Math.PI), swing = Math.Sin(timeMs / Seated.SwingMs * 2 * Math.PI);
        // The limbs turn with the body: tilted forward, the thighs are raised as much again to stay level.
        double tilt = Seated.Slump - 0.02 * breath - Seated.SighLift * Sigh(timeMs);
        // Seen side on, both legs hang alike, the far one behind the near; the arms hang limp, the hands between the knees.
        var leg = B(Math.PI / 2 + tilt, -(Math.PI / 2 - 0.04 * swing));
        return new Limbs(
            FrontArm: B(0.28 + 0.02 * breath, 0.45),
            BackArm: B(0.2 + 0.02 * breath, 0.5),
            FrontLeg: leg,
            BackLeg: leg with { Lower = leg.Lower + 0.04 },
            Tilt: tilt, Lift: -Rig.Hip, Lying: false);
    }

    /// <summary>
    /// Strapped into a drop pod's seat (landing): sitting upright, the hips where the feet would stand, the thighs level ahead,
    /// the shins straight down and the hands on the knees; only a slow breath stirs the shoulders.
    /// </summary>
    private static Limbs Strapped(double timeMs)
    {
        double tilt = 0.02 * Math.Sin(timeMs / Seated.BreathMs * 2 * Math.PI);
        var leg = B(Math.PI / 2 + tilt, -Math.PI / 2);
        return new Limbs(
            FrontArm: B(0.5 + tilt, 0.9),
            BackArm: B(0.4 + tilt, 0.95),
            FrontLeg: leg,
            BackLeg: leg,
            Tilt: tilt, Lift: -Rig.Hip, Lying: false);
    }

    /// <summary>The share of a running stride a foot is on the floor, pushing the body on.</summary>
    private const double Stance = 0.42;

    /// <summary>
    /// How far the running body rises <paramref name="travelled"/> cells into its run: nothing with a foot on the floor,
    /// <see cref="Feel.Run.Bounce"/> pixels at the middle of each stride's flight, both feet off it.
    /// </summary>
    public static double Bounce(double travelled)
    {
        double half = travelled / Stride.Run * 2, h = half - Math.Floor(half);
        // Each leg stands for Stance of its cycle, the other half a cycle behind: both are off the floor from 2 Stance of
        // each half to its end, and the body rises and falls over that.
        return h < 2 * Stance ? 0 : Feel.Run.Bounce * Math.Sin(Math.PI * (h - 2 * Stance) / (1 - 2 * Stance));
    }

    /// <summary>
    /// A running leg at <paramref name="cycle"/> through its stride: on the floor from under the hips to
    /// behind them, nearly straight; then off it, the heel kicked up and the knee brought through high.
    /// </summary>
    private static Bend Running(double cycle)
    {
        double p = cycle - Math.Floor(cycle);
        if (p < Stance) return B(0.4 - 0.95 * p / Stance, -0.12);
        double u = (p - Stance) / (1 - Stance);
        // Back from behind to high ahead, then down to meet the floor under the hips again.
        double thigh = u < 0.7 ? -0.6 + 1.85 * Math.Sin(u / 0.7 * Math.PI / 2) : 1.25 - 0.85 * (u - 0.7) / 0.3;
        return B(thigh, -(0.12 + 2.1 * Math.Sin(u * Math.PI)));
    }

    /// <summary>
    /// Where a hand or foot on a ladder is <paramref name="cycle"/> through its climb: holding on from <paramref name="top"/>
    /// down <paramref name="range"/> for <c>Ladder.Grip</c> of the cycle, then let go and brought back up, out from the ladder
    /// by <paramref name="outward"/> on the way.
    /// </summary>
    private static Vec OnRung(double cycle, double top, double range, double x, double outward)
    {
        double p = cycle - Math.Floor(cycle), grip = Rung.Grip;
        if (p < grip) return new Vec(x, top + p / grip * range);
        double q = (p - grip) / (1 - grip);
        return new Vec(x - outward * Math.Sin(q * Math.PI), top + (1 - q) * range);
    }

    /// <summary>How far a ladder's slide shows, from the climb down's speed (none) to the slide's (all), read from the fall alone (0092).</summary>
    public static double SlideOf(int vy) =>
        Math.Clamp((double)(vy - Stillwake.Content.Hero.ClimbDown) / (Stillwake.Content.Hero.Slide - Stillwake.Content.Hero.ClimbDown), 0, 1);

    /// <summary>How far the hands have gone onto a ledge <paramref name="ledge"/> pixels over the feet: from their highest hold, over <c>Ladder.PullBlend</c>.</summary>
    public static double PullOf(double? ledge) =>
        ledge is double l ? Math.Clamp((Rig.Shoulder - Rung.HandTop - l) / Rung.PullBlend, 0, 1) : 0;

    /// <summary>
    /// On a ladder (0092): hands over the head reaching in turn, each with the opposite foot, a hand over the other every half
    /// of <c>Ladder.Stride</c> cells climbed, read from where <paramref name="hero"/> is, so every hero shows it alike from its
    /// position; sliding, the hands high on the rails by the fall's speed; near the top, a <paramref name="ledge"/> pixels over
    /// the feet, pulled up onto it (<see cref="PullUp"/>).
    /// </summary>
    private static Limbs Climbing(Hero hero, double? ledge)
    {
        double cycle = -(double)hero.Y / Hero.Unit / Rung.Stride;
        Vec Hand(double at) => OnRung(at, Rung.HandTop, Rung.HandRange, Rung.HandOut, Rung.HandSwing);
        Vec Foot(double at) => OnRung(at, Rung.FootTop, Rung.FootRange, Rung.FootOut, Rung.FootSwing);
        // Elbows bend as arms do, forearms forward, the elbows below the hands as when pulling up.
        var limbs = new Limbs(
            FrontArm: Rig.Reach(Hand(cycle), Rig.Arm, 1),
            BackArm: Rig.Reach(Hand(cycle + 0.5), Rig.Arm, 1),
            FrontLeg: Rig.Reach(Foot(cycle + 0.5), Rig.Leg, -1),
            BackLeg: Rig.Reach(Foot(cycle), Rig.Leg, -1),
            Tilt: 0, Lift: 0, Lying: false);
        if (ledge is double l && PullOf(l) > 0) limbs = PullUp(limbs, l);
        double slide = SlideOf(hero.Vy);
        return slide > 0 ? Mix(limbs, OnLadder.Slide, slide) : limbs;
    }

    /// <summary>
    /// Pulled up onto a ledge <paramref name="ledge"/> pixels over the feet (0092): the hands on it ahead, staying there as the
    /// body rises past them, closing in under the shoulder to push down; the front foot stepping up onto it, the back leg
    /// straightening under the body; the body leaning into it. At the ledge, all but standing on it.
    /// </summary>
    private static Limbs PullUp(Limbs climbing, double ledge)
    {
        double pull = PullOf(ledge), down = Rig.Shoulder - ledge, reach = 0.98 * (Rig.Arm.Upper + Rig.Arm.Lower);
        double ahead = Math.Clamp(Math.Sqrt(Math.Max(0, reach * reach - down * down)), 1.5, Rung.HandOut);
        double knee = Math.Clamp((Rung.KneeFrom - ledge) / Rung.KneeBlend, 0, 1);
        double push = Math.Clamp((Rung.PushFrom - ledge) / Rung.PushFrom, 0, 1);
        var step = Rig.Reach(new Vec(Rig.W * 0.3, Math.Max(2.5, Rig.Hip - ledge)), Rig.Leg, -1);
        var under = Rig.Reach(new Vec(-Rig.W * 0.05, Rig.Hip - 0.3), Rig.Leg, -1);
        return climbing with
        {
            FrontArm = MixBend(climbing.FrontArm, Rig.Reach(new Vec(ahead, down), Rig.Arm, 1), pull),
            BackArm = MixBend(climbing.BackArm, Rig.Reach(new Vec(ahead - 1.5, down), Rig.Arm, 1), pull),
            FrontLeg = MixBend(climbing.FrontLeg, step, knee),
            BackLeg = MixBend(climbing.BackLeg, under, push),
            Tilt = Rung.Lean * pull * Math.Clamp(ledge / Rung.LeanTill, 0, 1),
        };
    }

    private static Bend MixBend(Bend a, Bend b, double by) => new(a.Upper + Apart(a.Upper, b.Upper) * by, a.Lower + Apart(a.Lower, b.Lower) * by);

    /// <summary>Whether the hero swims, rather than drifting still.</summary>
    public static bool Swimming(Hero hero) => hero.Vx != 0 || hero.Vy != 0;

    /// <summary>Whether the hero moves faster than it swims: flying on a jetpack (0047).</summary>
    public static bool SwimmingFast(Hero hero) =>
        Math.Abs(hero.Vx) > Stillwake.Content.Hero.Swim || Math.Abs(hero.Vy) > Stillwake.Content.Hero.Swim;

    /// <summary>
    /// Afloat in no gravity, upright (0036): still, it drifts, arms floating a little up, knees loose, the
    /// body rocking slowly; blended by <paramref name="swim"/> into swimming, and that by
    /// <paramref name="fast"/> into swimming fast, so one turns into the next without a jump.
    /// </summary>
    private static Limbs Floating(Hero hero, double timeMs, double swim, double fast, bool wields)
    {
        double drift = Math.Sin(timeMs / DriftMs * 2 * Math.PI), sway = Math.Sin(timeMs / Adrift.SwayMs * 2 * Math.PI);
        var still = new Limbs(
            B(0.15 + 0.06 * drift, 0.35), B(0.4 - 0.08 * drift, 0.55),
            B(0.25 + 0.1 * sway, -(0.5 + 0.1 * drift)), B(-0.08 * sway, -(0.35 - 0.1 * drift)),
            0.03 * sway, 2 * drift, false);
        if (swim <= 0) return still;
        var swimming = Paddling(hero, timeMs / Adrift.PaddleMs, Adrift.Lean, 0.4, wields, drift);
        if (fast > 0) swimming = Mix(swimming, Paddling(hero, timeMs / Adrift.PaddleFastMs, Adrift.LeanFast, 0.6, wields, drift), fast);
        return swim >= 1 ? swimming : Mix(still, swimming, swim);
    }

    /// <summary>
    /// Swimming upright, <paramref name="stroke"/> paddles in: leaning <paramref name="lean"/> into a swim
    /// along the floor and tucked forward sinking; the free hand paddling <paramref name="reach"/> radians
    /// either way, both hands with nothing in the front one; the legs trailing, drawn up sinking and
    /// hanging rising.
    /// </summary>
    private static Limbs Paddling(Hero hero, double stroke, double lean, double reach, bool wields, double drift)
    {
        double p = stroke * 2 * Math.PI;
        int along = Math.Sign(hero.Vx) * hero.Facing, sinking = Math.Sign(hero.Vy);
        // Brought forward with the elbow bent, then pulled down and back nearly straight.
        Bend Paddle(double lag) => B(0.75 + reach * Math.Sin(p + lag), 0.75 + 0.35 * Math.Cos(p + lag));
        Bend Leg(double lag)
        {
            double kick = 0.1 * Math.Sin(p + lag);
            if (sinking > 0) return B(0.85 + kick, -1.35);
            return along != 0 ? B(-0.2 + kick, -(0.55 + 0.15 * Math.Cos(p + lag))) : B(0.05 + kick, -0.2);
        }
        var front = wields ? B(-0.05 + 0.12 * Math.Sin(p), 0.35) : Paddle(Math.PI);
        return new Limbs(front, Paddle(0), Leg(Math.PI), Leg(0), along * lean + (sinking > 0 ? Adrift.Tuck : 0), 0.5 * drift, false);
    }

    /// <summary>How far <paramref name="to"/> is from <paramref name="from"/>, the short way round, in radians.</summary>
    public static double Apart(double from, double to) => ((to - from) % (2 * Math.PI) + 3 * Math.PI) % (2 * Math.PI) - Math.PI;

    /// <summary>
    /// Rammed (0103): the arms flung up before the visor, the front knee drawn up, the back leg braced; the body tilted
    /// <c>HeroMotion.Flinch.Tilt</c> away from the blow by <see cref="Flinched"/>.
    /// </summary>
    public static readonly Limbs Flinching = new(B(1.75, 1.35), B(1.25, 1.0), B(0.55, -0.7), B(-0.3, -0.35), 0, 0, false);

    /// <summary>How far into a flinch begun <paramref name="ms"/> ago the figure is drawn, 0 to 1: quick in, easing out.</summary>
    public static double FlinchOf(double ms)
    {
        double t = ms / Feel.Flinch.Ms;
        if (t is < 0 or >= 1) return 0;
        if (t < Feel.Flinch.In) return t / Feel.Flinch.In;
        double out_ = (t - Feel.Flinch.In) / (1 - Feel.Flinch.In);
        return 1 - out_ * out_ * (3 - 2 * out_);
    }

    /// <summary>
    /// <paramref name="limbs"/> flinching <paramref name="by"/> (0 to 1) from a blow on the side <paramref name="ahead"/> says,
    /// +1 before the figure: thrown back from one before it, forward from one behind. On the floor (<paramref name="pose"/>) its
    /// lower foot stays on it.
    /// </summary>
    public static Limbs Flinched(Limbs limbs, double by, int ahead, Pose pose)
    {
        if (by <= 0 || !HoldsIn(pose)) return limbs;
        var flinch = Flinching with { Tilt = limbs.Tilt - (ahead >= 0 ? 1 : -1) * Feel.Flinch.Tilt, Lift = limbs.Lift };
        var mixed = Mix(limbs, flinch, by) with { Lift = limbs.Lift, Lying = limbs.Lying };
        return pose is Pose.Stand or Pose.Walk or Pose.Run ? Grounded(mixed) : mixed;
    }

    /// <summary>
    /// <paramref name="a"/> turned toward <paramref name="b"/> by <paramref name="by"/>, angles the short
    /// way round, so limbs blend in without unwinding.
    /// </summary>
    private static Limbs Mix(Limbs a, Limbs b, double by)
    {
        double Angle(double x, double y) => x + (((y - x) % (2 * Math.PI) + 3 * Math.PI) % (2 * Math.PI) - Math.PI) * by;
        Bend Limb(Bend x, Bend y) => new(Angle(x.Upper, y.Upper), Angle(x.Lower, y.Lower));
        return new Limbs(Limb(a.FrontArm, b.FrontArm), Limb(a.BackArm, b.BackArm), Limb(a.FrontLeg, b.FrontLeg),
            Limb(a.BackLeg, b.BackLeg), Angle(a.Tilt, b.Tilt), a.Lift + (b.Lift - a.Lift) * by, a.Lying);
    }
}
