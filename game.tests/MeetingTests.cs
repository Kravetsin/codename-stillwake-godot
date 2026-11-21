using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The drone's first meeting with its hero (drone-meeting), timed as the picked vision («Стена») timed it.</summary>
public class MeetingTests
{
    private const double Step = 1.0 / 240;
    private static readonly Vec Core = new(80, 60);

    private static IEnumerable<double> Sweep(double from = 0, double to = Meeting.End)
    {
        for (double u = from; u <= to + 1e-9; u += Step) yield return u;
    }

    private static MeetingScene Scene(int side = 1) => new(Core, new Vec(Core.X + side * 16, Core.Y + 12), side, 3.7);

    private static Vec Idle(MeetingScene s) => new(s.Feet.X - 6, s.Feet.Y - 50);

    private static Vec Palm(MeetingScene s, double u) => new(s.HeroAt(u).Feet.X - s.Side * 4, s.HeroAt(u).Feet.Y - 20);

    private static Plate Home(int i, double u) => DroneSwarm.PlateIn(SwarmShape.Idle, i, 0, (float)u);

    private static double FirstWhen(Func<double, bool> holds)
    {
        foreach (double u in Sweep())
            if (holds(u)) return u;
        return double.PositiveInfinity;
    }

    [Fact]
    public void ItsBeatsComeInThePicksOrder()
    {
        var s = Scene();
        // E; the HUD gone; the core lit, lifted; the crumble; the void; the first drop; each ring full; the black; the eye; back to
        // the wreck; the core home; each plate; the drone by the head; the end.
        double hud = FirstWhen(u => Meeting.Hud(u) < 0.5);
        double lit = FirstWhen(u => Meeting.Light(u) > 0.5);
        double lifts = FirstWhen(u => !Meeting.CoreLies(u));
        double crumbles = FirstWhen(u => MeetingFlakes.Crumbled(u) > 0);
        double vision = FirstWhen(u => Meeting.PhaseOf(u) == MeetingPhase.Vision);
        double bleeds = FirstWhen(u => Threads(s, u) > 0);
        double black = FirstWhen(u => MeetingScene.BlackOf(0, u) > 0);
        double eye = FirstWhen(u => Meeting.LensOn(u) > 0);
        double back = FirstWhen(u => Meeting.PhaseOf(u) == MeetingPhase.Back);
        double home = FirstWhen(u => Meeting.PhaseOf(u) == MeetingPhase.Home);
        var lands = new List<double>();
        for (int i = 0; i < Meeting.Plates; i++)
        {
            int plate = i;
            lands.Add(FirstWhen(u => s.PlateOf(plate, u, Core, Home(plate, u)).Stage == PlateStage.Home));
        }
        double drone = FirstWhen(u => !Meeting.DrawsDrone(u));
        double[] beats = [Meeting.Times.Press, hud, lit, lifts, crumbles, vision, bleeds, s.Full(0), s.Full(1), s.Full(2), black, eye, back, home, .. lands, drone, Meeting.End];
        for (int i = 1; i < beats.Length; i++) Assert.True(beats[i - 1] < beats[i], $"beat {i - 1} at {beats[i - 1]:F3} s, {i} at {beats[i]:F3} s");
        Assert.InRange(crumbles, Meeting.Times.InFrom, Meeting.Times.InFrom + Step);
        Assert.InRange(vision, Meeting.Times.InTo, Meeting.Times.InTo + Step);
        Assert.InRange(bleeds, Meeting.Times.Ring0 - Meeting.Times.Fly, Meeting.Times.Ring0 + 2);
        Assert.InRange(eye, Meeting.Times.LensFrom, Meeting.Times.LensFrom + Step);
        Assert.InRange(back, Meeting.Times.OutFrom, Meeting.Times.OutFrom + Step);
        for (int i = 0; i < lands.Count; i++) Assert.InRange(lands[i], 19.8 + i, 19.8 + i + Step);
        Assert.Equal((1.0, 0.0, 0.0), (Meeting.Hud(Meeting.End), Meeting.Grade(Meeting.End), Meeting.Grade(0)));
    }

    [Fact]
    public void TheThreeRingsFillInTurnEveryDropOnItsOwnRing()
    {
        double[] begins = [Meeting.Times.Ring0, Meeting.Times.Ring1, Meeting.Times.Ring2];
        foreach (int side in (int[])[1, -1])
        {
            var s = Scene(side);
            for (int o = 0; o < 3; o++)
            {
                Assert.Equal(0, s.Arrived(o, begins[o]));
                Assert.Equal(s.Count(o), s.Arrived(o, s.Full(o)));
                Assert.True(s.Full(o) < begins[o] + 4, $"ring {o} full only at {s.Full(o):F3} s");
                if (o < 2) Assert.True(s.Full(o) < begins[o + 1], $"ring {o} full at {s.Full(o):F3} s, after ring {o + 1} begins");
            }
            Assert.Equal(MeetingScene.Drops, s.Count(0) + s.Count(1) + s.Count(2));
            // Each drop lands on its ring where the ring passes nearest the hero: on its ring from then, never back in flight.
            for (int i = 0; i < MeetingScene.Drops; i++)
            {
                Assert.InRange(s.Arrives(i), begins[i % 3], begins[i % 3] + 4);
                Assert.Null(s.DropAt(i, s.Arrives(i) - Step, s.CoreAt(s.Arrives(i), Idle(s), 0)));
            }
        }
    }

    [Fact]
    public void TheBloodTurnsFromRedToBlackAsTheEyeOpens()
    {
        var s = Scene();
        for (int i = 0; i < MeetingScene.Drops; i++)
        {
            Assert.Equal(0, MeetingScene.BlackOf(i, Meeting.Times.BlackFrom));
            Assert.Equal(1, MeetingScene.BlackOf(i, Meeting.Times.BlackTo));
            var red = s.DropAt(i, 11.55, s.CoreAt(11.55, Idle(s), 0))!.Value;
            var dark = s.DropAt(i, 14, s.CoreAt(14, Idle(s), 0))!.Value;
            Assert.Equal(Meeting.Blood, red.Fill);
            Assert.Equal(Meeting.Black, dark.Fill);
            Assert.Equal(0, red.Ember);
        }
        // Some keep an ember once black, none while red.
        int embers = 0;
        for (int i = 0; i < MeetingScene.Drops; i++)
            if (s.DropAt(i, 15, s.CoreAt(15, Idle(s), 0))!.Value.Ember > 0) embers++;
        Assert.InRange(embers, 1, MeetingScene.Drops / 2);
    }

    [Fact]
    public void TheLensIsDeadTillTheEyeBeatAndOpenAfter()
    {
        var s = Scene();
        foreach (double u in Sweep(-0.5, Meeting.End + 0.5))
        {
            double on = s.FaceOf(u).Lens;
            if (u < Meeting.Times.LensFrom) Assert.True(on == 0, $"the lens lit {on:F3} at {u:F3} s");
            if (u >= Meeting.Times.LensTo) Assert.True(on == 1, $"the lens only {on:F3} at {u:F3} s");
        }
    }

    [Fact]
    public void TheCoreShrinksBackExactlyWhereItBeganToGrowThenRisesToTheHead()
    {
        foreach (int side in (int[])[1, -1])
        {
            var s = Scene(side);
            var idle = Idle(s);
            Assert.Equal(new MeetingCore(Core, 1, 0), s.CoreAt(0, idle, 0));
            var grows = s.CoreAt(Meeting.Times.InFrom - Step, idle, 0);
            var back = s.CoreAt(Meeting.Times.OutTo, idle, 0);
            Assert.Equal((grows.At, grows.R), (back.At, back.R));
            Assert.Equal((s.Hover, 1.0), (back.At, back.R));
            Assert.Equal(new MeetingCore(s.VoidCore, Meeting.VoidR, s.VoidAngle(10)), s.CoreAt(10, idle, 0));
            Assert.Equal(idle, s.CoreAt(Meeting.Times.RiseTo, idle, 0.5).At);
            // Smooth all along: no jump over a frame anywhere.
            var last = s.CoreAt(0, idle, 0);
            foreach (double u in Sweep(Step))
            {
                var now = s.CoreAt(u, idle, 0);
                double moved = Math.Sqrt((now.At.X - last.At.X) * (now.At.X - last.At.X) + (now.At.Y - last.At.Y) * (now.At.Y - last.At.Y));
                Assert.True(moved < 1.5, $"the core jumped {moved:F2} px at {u:F3} s");
                Assert.True(Math.Abs(Math.Log(now.R / last.R)) < 0.05, $"the core's size jumped at {u:F3} s");
                last = now;
            }
            // The hero stands where it pressed E before and after the vision.
            Assert.Equal(s.Feet, s.HeroAt(1).Feet);
            Assert.Equal(s.Feet, s.HeroAt(Meeting.Times.OutTo).Feet);
            Assert.Equal(Meeting.VoidScale, s.HeroAt(10).Scale);
        }
    }

    [Fact]
    public void PlatesComeOnlyOnceBackFromTheVisionOneInFlightAtMost()
    {
        var s = Scene();
        var stages = new PlateStage[Meeting.Plates];
        foreach (double u in Sweep())
        {
            int flying = 0;
            for (int i = 0; i < Meeting.Plates; i++)
            {
                var stage = s.PlateOf(i, u, Core, Home(i, u)).Stage;
                Assert.True(stage >= stages[i], $"plate {i} went back from {stages[i]} to {stage} at {u:F3} s");
                if (Meeting.PhaseOf(u) != MeetingPhase.Home) Assert.Equal(PlateStage.Away, stage);
                if (Meeting.ArcAlpha(i, u) > 0) Assert.Equal(MeetingPhase.Home, Meeting.PhaseOf(u));
                stages[i] = stage;
                if (stage == PlateStage.Flying) flying++;
            }
            Assert.True(flying <= 1, $"{flying} plates in flight at {u:F3} s");
        }
        Assert.All(stages, stage => Assert.Equal(PlateStage.Home, stage));
    }

    [Fact]
    public void TheHeadSwimsLighterThanTheLastBreathNeverReachingTheInk()
    {
        double most = 0;
        foreach (double u in Sweep(-1, Meeting.End + 1)) most = Math.Max(most, Meeting.Dizzy(u));
        Assert.Equal(Meeting.Level, most, 6);
        Assert.True(most < Tunnel.Sinks);
        Assert.Equal(0, Tunnel.Of(most, 3).Ink);
        Assert.Equal(Meeting.Level, Meeting.Dizzy(10), 6);
        Assert.Equal((0.0, 0.0, 0.0), (Meeting.Dizzy(Meeting.Times.DizzyFrom), Meeting.Dizzy(Meeting.Times.DizzyGone), Meeting.Dizzy(Meeting.End)));
    }

    [Fact]
    public void TheSimSparesTheHeroForTheWholeScene()
    {
        double seconds = Meeting.End - Meeting.Times.Press;
        Assert.True(seconds * Stillwake.Sim.World.TicksPerSecond <= global::Stillwake.Content.Crew.MeetingTicks);
        Assert.True((seconds + 0.1) * Stillwake.Sim.World.TicksPerSecond > global::Stillwake.Content.Crew.MeetingTicks);
    }

    [Fact]
    public void TheFlakesCoverTheScreenAndAllGoBeforeTheVision()
    {
        var flakes = new MeetingFlakes(1280, 720, new Vec(600, 500));
        Assert.Equal(MeetingFlakes.Across * MeetingFlakes.Down, flakes.Flakes.Count);
        double area = 0;
        foreach (var f in flakes.Flakes)
        {
            area += Math.Abs((f.A.X * f.B.Y - f.B.X * f.A.Y) + (f.B.X * f.C.Y - f.C.X * f.B.Y) + (f.C.X * f.D.Y - f.D.X * f.C.Y) + (f.D.X * f.A.Y - f.A.X * f.D.Y)) / 2;
            Assert.Equal(f.Middle, MeetingFlakes.At(f, 0)!.Value.Middle);
        }
        Assert.Equal(1280 * 720, area, 0);
        Assert.Equal(0, MeetingFlakes.Crumbled(Meeting.Times.InFrom));
        Assert.Equal(1, MeetingFlakes.Crumbled(Meeting.Times.InFrom + (Meeting.Times.InTo - Meeting.Times.InFrom) * 0.85), 6);
        Assert.NotEmpty(flakes.Stars);
    }

    [Fact]
    public void EveryNumberIsFiniteOverTheWholeMeeting()
    {
        Span<Vec> points = stackalloc Vec[41];
        foreach (int side in (int[])[1, -1])
        {
            var s = Scene(side);
            foreach (double u in Sweep(-0.5, Meeting.End + 0.5))
            {
                var core = s.CoreAt(u, Idle(s), 0.3);
                var hero = s.HeroAt(u);
                var face = s.FaceOf(u);
                var (drinking, embers) = s.VoidGlow(u, core.R);
                var (breathing, waking) = Meeting.SmallHalos(u, Meeting.Breath(u), core.R);
                Finite(u, core.At.X, core.At.Y, core.R, core.Angle, hero.Feet.X, hero.Feet.Y, hero.Scale, hero.Turn, hero.Kneel, hero.Lean, hero.Reach,
                    face.Soak, face.Lens, face.Glow, drinking, embers, breathing, waking, Meeting.Hud(u), Meeting.Grade(u),
                    Meeting.Dizzy(u), Meeting.Halo(u, Meeting.Breath(u)), Meeting.LensShift(u), Meeting.DebrisIn(u), Meeting.Rings(u), Meeting.Spread(u));
                for (int i = 0; i < MeetingScene.Drops; i++)
                {
                    if (s.DropAt(i, u, core) is { } d) Finite(u, d.At.X, d.At.Y, d.Tail.X, d.Tail.Y, d.Mid.X, d.Mid.Y, d.Radius, d.Ember, d.Lit, d.SheenAlpha);
                    for (int strand = 0; strand < MeetingScene.Strands; strand++)
                    {
                        int n = s.ThreadAt(i, strand, u, Palm(s, u), core, points);
                        for (int j = 0; j < n; j++) Finite(u, points[j].X, points[j].Y);
                    }
                }
                for (int i = 0; i < Meeting.Plates; i++)
                {
                    var plate = s.PlateOf(i, u, core.At, Home(i, u));
                    Finite(u, plate.At.X, plate.At.Y, plate.Turn, plate.Flip, Meeting.ArcAlpha(i, u), Meeting.LandFlash(i, u));
                    var (first, _) = Meeting.ArcSeeds(i, u);
                    int n = Meeting.Arc(core.At, plate.At, first, points);
                    for (int j = 0; j < n; j++) Finite(u, points[j].X, points[j].Y);
                }
            }
        }
    }

    [Fact]
    public void AFriendMeetsTheDroneOnlyWhenSeenPairingHereTheCoreNotLost()
    {
        var watch = new MeetingWatch();
        object world = new(), loaded = new();
        // Seen first already paired, as on joining or loading: no meeting.
        Assert.False(watch.Turned(world, 2, true, false));
        Assert.False(watch.Turned(world, 3, false, false));
        Assert.True(watch.Turned(world, 3, true, false));
        Assert.False(watch.Turned(world, 3, true, false));
        // Paired as the core is lost: none.
        Assert.False(watch.Turned(world, 4, false, false));
        Assert.False(watch.Turned(world, 4, true, true));
        // A new world forgets what it saw.
        Assert.False(watch.Turned(world, 5, false, false));
        Assert.False(watch.Turned(loaded, 5, true, false));
    }

    [Fact]
    public void KneelingTheNearBootStandsFlatOnTheFloorTheHipsLowAndTheHandsFree()
    {
        var hero = new Hero(0, 0, 0, 0, false, 1);
        var knelt = Poses.LimbsOf(Pose.Kneel, hero, new Moment(0, 0, Kneel: new Kneeling(1, 0.4)));
        Assert.Equal(Poses.SoleOf(knelt.FrontLeg, knelt.FrontFoot, Rig.HipApart, knelt.Tilt), knelt.Lift, 6);
        Assert.Equal(0, Poses.Flat(knelt.FrontLeg, knelt.Tilt) - knelt.FrontFoot, 6);
        Assert.True(knelt.Lift < -Rig.Hip / 3, $"the hips {knelt.Lift:F1} px down");
        Assert.Equal(0.4, knelt.Tilt, 6);
        var standing = Poses.LimbsOf(Pose.Kneel, hero, new Moment(0, 0, Kneel: new Kneeling(0, 0)));
        Assert.True(Math.Abs(standing.Lift) < 1, $"standing {standing.Lift:F2} px off the floor");
        Assert.False(Poses.HoldsIn(Pose.Kneel));
    }

    [Fact]
    public void EachThreadJoinsItsRingAlongTheOrbitWithNoCorner()
    {
        Span<Vec> points = stackalloc Vec[MeetingScene.ThreadPoints];
        foreach (int side in (int[])[1, -1])
        {
            var s = Scene(side);
            double sharpest = 0, worstJoin = 0;
            for (int i = 0; i < MeetingScene.Drops; i++)
            {
                double arrives = s.Arrives(i);
                var core = s.CoreAt(arrives, Idle(s), 0);
                int n = s.ThreadAt(i, 0, arrives - 1e-6, Palm(s, arrives), core, points);
                Assert.Equal(MeetingScene.ThreadPoints, n);
                // The head lands on the drop, heading the way the drop then winds.
                var drop = s.DropAt(i, arrives, core)!.Value.At;
                var on = s.DropAt(i, arrives + 0.01, s.CoreAt(arrives + 0.01, Idle(s), 0))!.Value.At;
                Assert.True(Apart(points[n - 1], drop) < 0.3, $"drop {i}'s thread ends {Apart(points[n - 1], drop):F2} px off it");
                worstJoin = Math.Max(worstJoin, Turn(points[n - 2], points[n - 1], on));
                for (double u = arrives - Meeting.Times.Fly; u < arrives + Meeting.Times.Fly; u += 0.05)
                    for (int strand = 0; strand < MeetingScene.Strands; strand++)
                    {
                        n = s.ThreadAt(i, strand, u, Palm(s, u), s.CoreAt(u, Idle(s), 0), points);
                        for (int j = 2; j < n; j++) sharpest = Math.Max(sharpest, Turn(points[j - 2], points[j - 1], points[j]));
                    }
            }
            Assert.True(worstJoin < 20, $"a thread meets its ring {worstJoin:F1} degrees off the orbit");
            Assert.True(sharpest < 30, $"a thread turns {sharpest:F1} degrees between two of its segments");
        }
    }

    [Fact]
    public void TheBloodFliesAsManyThreadsEachFinerThanTheArm()
    {
        Assert.True(MeetingScene.Thread < 0.5 * Rig.Arm.Width * Meeting.VoidScale, $"a thread {MeetingScene.Thread:F2} px wide");
        var s = Scene();
        int most = 0;
        foreach (double u in Sweep(Meeting.Times.Ring0 - Meeting.Times.Fly, Meeting.Times.BlackFrom)) most = Math.Max(most, Threads(s, u));
        Assert.True(most >= 30, $"at most {most} threads in the air at once");
    }

    private static int Threads(MeetingScene s, double u)
    {
        int n = 0;
        Span<Vec> points = stackalloc Vec[MeetingScene.ThreadPoints];
        var core = s.CoreAt(u, Idle(s), 0);
        for (int i = 0; i < MeetingScene.Drops; i++)
            for (int strand = 0; strand < MeetingScene.Strands; strand++)
                if (s.ThreadAt(i, strand, u, Palm(s, u), core, points) > 0) n++;
        return n;
    }

    private static double Apart(Vec a, Vec b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    /// <summary>How many degrees the way from a to b turns to go on to c.</summary>
    private static double Turn(Vec a, Vec b, Vec c)
    {
        double ux = b.X - a.X, uy = b.Y - a.Y, vx = c.X - b.X, vy = c.Y - b.Y;
        return Math.Abs(Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy)) * 180 / Math.PI;
    }

    private static void Finite(double u, params double[] numbers)
    {
        foreach (double n in numbers) Assert.True(double.IsFinite(n), $"{n} at {u:F3} s");
    }
}
