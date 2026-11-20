using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The drone's first meeting with its hero (drone-meeting), timed as the pick «Вихрь + Капли» timed it.</summary>
public class MeetingTests
{
    private const double Step = 1.0 / 240;
    private static readonly Vec Core = new(80, 60), Palm = new(92, 52), Idle = new(95, 28);

    private static IEnumerable<double> Sweep(double from = 0, double to = Meeting.End)
    {
        for (double u = from; u <= to + 1e-9; u += Step) yield return u;
    }

    private static Plate Home(int i) => DroneSwarm.PlateIn(SwarmShape.Idle, i, 0, 3);

    [Fact]
    public void ItKneelsGivesBloodWakesTheCoreRaisesItsSootArmoursItAndRisesInThePicksOrder()
    {
        // Each beat's start, from the press: the kneel, the first drop, the cracks, the ring soaked in, the lift, the soot, the lens,
        // the four plates, the rise, the stand, the end.
        double FirstWhen(Func<double, bool> holds)
        {
            foreach (double u in Sweep())
                if (holds(u)) return u;
            return double.PositiveInfinity;
        }
        double kneels = FirstWhen(u => Meeting.Kneel(u) > 0);
        double bleeds = FirstWhen(u => Meeting.DropsOut(u, Palm, Core, 1) > 0);
        double cracks = FirstWhen(u => Meeting.Wake(u) > 0);
        double soaked = FirstWhen(u => u > Meeting.Times.BurstTo && Meeting.DropsOut(u, Palm, Core, 1) == 0);
        double lifts = FirstWhen(u => !Meeting.CoreLies(u));
        double rises = FirstWhen(u => Meeting.Form(u) > 0);
        double lights = FirstWhen(u => Meeting.Lens(u).On > 0);
        var lands = new List<double>();
        for (int i = 0; i < Meeting.Plates; i++)
        {
            int plate = i;
            lands.Add(FirstWhen(u => Meeting.PlateOf(plate, u, 0, Core, Idle, Home(plate)).Stage == PlateStage.Home));
        }
        double flies = FirstWhen(u => !Meeting.DrawsDrone(u));
        double stands = FirstWhen(u => u > Meeting.Times.KneelTo && Meeting.Kneel(u) < 1);
        double[] beats = [kneels, bleeds, cracks, soaked, lifts, rises, lights, .. lands, flies, stands, Meeting.End];
        for (int i = 1; i < beats.Length; i++) Assert.True(beats[i - 1] < beats[i], $"beat {i - 1} at {beats[i - 1]:F3} s, {i} at {beats[i]:F3} s");
        Assert.InRange(kneels, 0.3, 0.3 + Step);
        Assert.InRange(bleeds, 1.45, 1.75);
        Assert.True(soaked <= Meeting.Times.Soaked + Step);
        // Each a beat after the last, a fifth of a second from the debris.
        for (int i = 0; i < lands.Count; i++) Assert.InRange(lands[i], 8.9 + 0.75 * i, 8.9 + 0.75 * i + Step);
        Assert.Equal((1.0, 0.0), (Meeting.Kneel(Meeting.Times.KneelTo), Meeting.Kneel(Meeting.End)));
    }

    [Fact]
    public void OnePlateAtMostIsInFlightAndEachGoesAwayFlyingHomeOnce()
    {
        var stages = new PlateStage[Meeting.Plates];
        foreach (double u in Sweep())
        {
            int flying = 0;
            for (int i = 0; i < Meeting.Plates; i++)
            {
                var stage = Meeting.PlateOf(i, u, u + 40, Core, Idle, Home(i)).Stage;
                Assert.True(stage >= stages[i], $"plate {i} went back from {stages[i]} to {stage} at {u:F3} s");
                stages[i] = stage;
                if (stage == PlateStage.Flying) flying++;
            }
            Assert.True(flying <= 1, $"{flying} plates in flight at {u:F3} s");
        }
        Assert.All(stages, s => Assert.Equal(PlateStage.Home, s));
    }

    [Fact]
    public void TheRingHasSoakedInBeforeTheSootRises()
    {
        Assert.True(Meeting.Times.Soaked < Meeting.Times.FormFrom);
        foreach (int side in (int[])[1, -1])
            foreach (double u in Sweep())
            {
                bool drops = false;
                for (int i = 0; i < Meeting.Drops; i++)
                    drops |= Meeting.DropAt(i, u, Palm, Core, side) is { Alpha: > 0 };
                bool rising = false;
                for (int i = 0; i < Meeting.Motes; i++) rising |= Meeting.MoteRise(i, u) > 0;
                Assert.False(drops && rising, $"drops still out as the soot rises at {u:F3} s");
                if (u >= Meeting.Times.Soaked) Assert.False(drops, $"drops still out at {u:F3} s");
            }
    }

    [Fact]
    public void TheDroneLiftsFromTheCoreAndEndsInItsPlaceByTheHead()
    {
        Assert.Equal(Core, Meeting.DroneAt(0, Core, 1, Idle, 5));
        Assert.Equal(Core, Meeting.DroneAt(Meeting.Times.LiftFrom, Core, -1, Idle, 5));
        foreach (double u in Sweep(Meeting.Times.RiseTo))
            Assert.Equal(Idle, Meeting.DroneAt(u, Core, 1, Idle, u * 7));
        // Hanging over the core between, a little away from the hero, whichever side it kneels on.
        var right = Meeting.DroneAt(10, Core, 1, Idle, 0);
        var left = Meeting.DroneAt(10, Core, -1, Idle, 0);
        Assert.True(right.Y < Core.Y - 8 && right.X < Core.X && left.X > Core.X);
    }

    [Fact]
    public void OneDroneAtATimeTheMeetingsTillItRisesTheHerosAfter()
    {
        foreach (double u in Sweep(0))
        {
            bool meeting = Meeting.FormsDrone(u), own = !Meeting.DrawsDrone(u);
            Assert.False(meeting && own, $"two drones at {u:F3} s");
            if (u >= Meeting.Times.LiftFrom) Assert.True(meeting || own, $"no drone at {u:F3} s");
        }
    }

    [Fact]
    public void TheHeadSwimsAsAWeakerLastBreathNeverReachingTheInk()
    {
        double most = 0;
        foreach (double u in Sweep(-1, Meeting.End + 1)) most = Math.Max(most, Meeting.Dizzy(u));
        Assert.Equal(Meeting.Strong, most, 6);
        Assert.True(most < Tunnel.Sinks);
        Assert.Equal(0, Tunnel.Of(most, 3).Ink);
        // Held while the blood is drawn, eased off as the hero stands.
        Assert.Equal(Meeting.Strong, Meeting.Dizzy(Meeting.Times.Soaked), 6);
        Assert.Equal((0.0, 0.0), (Meeting.Dizzy(0), Meeting.Dizzy(Meeting.End)));
    }

    [Fact]
    public void EveryNumberIsFiniteOverTheWholeMeeting()
    {
        Span<Vec> points = stackalloc Vec[41];
        foreach (int side in (int[])[1, -1])
            foreach (double u in Sweep(-0.5, Meeting.End + 0.5))
            {
                var drone = Meeting.DroneAt(u, Core, side, Idle, u * 3);
                var (on, glow) = Meeting.Lens(u);
                var (colour, alpha) = Meeting.CrackLook(u);
                Finite(u, Meeting.Kneel(u), Meeting.Lean(u), Meeting.Reach(u), Meeting.Look(u, 0.3), Meeting.Dizzy(u), Meeting.Wake(u),
                    Meeting.Lift(u), Meeting.Rise(u), Meeting.Form(u), drone.X, drone.Y, Meeting.AngleOf(u, drone, Idle, 2), on, glow,
                    Meeting.Halo(u, Meeting.Breath(u)), Meeting.DrinkGlow(u), Meeting.Soak(u), Meeting.LensShift(u), Meeting.DebrisIn(u),
                    colour.R, colour.G, colour.B, alpha);
                for (int i = 0; i < Meeting.Drops; i++)
                    if (Meeting.DropAt(i, u, Palm, Core, side) is { } d) Finite(u, d.At.X, d.At.Y, d.Tail.X, d.Tail.Y, d.Alpha, d.Radius);
                for (int k = 0; k < Meeting.CrackCount; k++)
                {
                    int n = Meeting.CrackAt(k, u, drone, 0.4, side, points);
                    for (int j = 0; j < n; j++) Finite(u, points[j].X, points[j].Y);
                }
                for (int i = 0; i < Meeting.Motes; i++)
                    if (Meeting.MoteAt(i, u, Core, drone, 0.4) is { } m) Finite(u, m.At.X, m.At.Y, m.Side, m.Alpha);
                for (int i = 0; i < Meeting.Plates; i++)
                {
                    var plate = Meeting.PlateOf(i, u, u + 40, Core, drone, Home(i));
                    Finite(u, plate.At.X, plate.At.Y, plate.Turn, plate.Flip, Meeting.ArcAlpha(i, u), Meeting.LandFlash(i, u));
                    var (first, _) = Meeting.ArcSeeds(i, u);
                    int n = Meeting.Arc(drone, plate.At, first, points);
                    for (int j = 0; j < n; j++) Finite(u, points[j].X, points[j].Y);
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

    private static void Finite(double u, params double[] numbers)
    {
        foreach (double n in numbers) Assert.True(double.IsFinite(n), $"{n} at {u:F3} s");
    }
}
