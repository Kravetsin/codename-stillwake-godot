using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;
using T = Stillwake.Game.Features.Crew.DroneFlight.Tune;

namespace Stillwake.Game.Tests;

/// <summary>The light drone's mind (drone-motion ADR): what it flies to, where it looks and when it turns red.</summary>
public class DroneWillTests
{
    private static readonly Vec Anchor = new(400, 200);
    private static readonly DroneSenses Still = new(Anchor, Anchor, Surface: 1);
    private static readonly Vec WindowAt = new(416, 250);
    private static readonly DroneDanger Meteor = new(7, new Vec(480, 120), 1);

    private static DroneWill Will(int seed = 1) => new(new Random(seed));

    private static bool InHome(Vec goal, Vec anchor) => Math.Abs(goal.X - anchor.X) <= T.HomeX + 1e-9 && Math.Abs(goal.Y - anchor.Y) <= T.HomeY + 1e-9;

    [Fact]
    public void AWindowOutranksADartADartAPerch()
    {
        var will = Will();
        var all = Still with { Window = WindowAt, Meteor = Meteor };
        Assert.Equal(WindowAt, will.Pick(all, 16).Goal);
        var dart = will.Pick(all with { Window = null }, 16);
        Assert.Equal(Meteor.At, dart.Goal);
        Assert.True(dart.Darting);
        var settle = will.Pick(Still, 16);
        Assert.False(settle.Darting);
        Assert.True(InHome(settle.Goal, Anchor));
    }

    /// <summary>A machine to learn from (drone-lessons): flown to and looked at, under a window and a dart, over a perch; never a dart, never red.</summary>
    [Fact]
    public void AVisitOutranksAPerchButNotAWindowOrADart()
    {
        var will = Will();
        var machine = new Vec(300, 180);
        var visit = will.Pick(Still with { Visit = machine }, 16);
        Assert.Equal(machine, visit.Goal);
        Assert.True(visit.Look.X < 0);
        Assert.False(visit.Darting);
        Assert.Equal(0, visit.Tint);
        Assert.Equal(WindowAt, will.Pick(Still with { Visit = machine, Window = WindowAt }, 16).Goal);
        Assert.Equal(Meteor.At, will.Pick(Still with { Visit = machine, Meteor = Meteor }, 16).Goal);
    }

    [Fact]
    public void ADartLastsWhileItsDangerDoesThenItReturnsAndNeverDartsTwiceAtTheSameOne()
    {
        var will = Will();
        var coming = Still with { Meteor = Meteor };
        for (int ms = 0; ms < 3000; ms += 16)
        {
            var intent = will.Pick(coming, 16);
            Assert.Equal(Meteor.At, intent.Goal);
            Assert.True(intent.Darting);
        }
        // A bolt replaces the dart under way; once it is gone the meteor, still coming, never darts it again.
        var bolt = new DroneDanger(9, new Vec(Anchor.X - 3 * Rig.Cell, 40), 1, Spread: 1);
        var replaced = will.Pick(coming with { Bolt = bolt }, 16);
        Assert.Equal(new Vec(bolt.At.X, Anchor.Y), replaced.Goal);
        var back = will.Pick(coming, 16);
        Assert.False(back.Darting);
        Assert.True(InHome(back.Goal, Anchor));
        Assert.Equal(0, back.Tint);
        // Landed, it returns home; the next meteor darts it anew.
        Assert.True(InHome(will.Pick(Still, 16).Goal, Anchor));
        Assert.True(will.Pick(Still with { Meteor = Meteor with { Cell = 8 } }, 16).Darting);
    }

    [Fact]
    public void ADartsDangerFlashesItRedAndAWindowHoldsItInPlaceLookingAtTheDanger()
    {
        var will = Will();
        double reddest = 0;
        for (int ms = 0; ms < 1000; ms += 16)
        {
            var intent = will.Pick(Still with { Meteor = Meteor, Window = WindowAt, At = WindowAt }, 16);
            Assert.Equal(WindowAt, intent.Goal);
            Assert.False(intent.Darting);
            var way = DroneFlight.Minus(Meteor.At, WindowAt);
            Assert.Equal(DroneFlight.Scaled(way, 1 / DroneFlight.Length(way)), intent.Look);
            reddest = Math.Max(reddest, intent.Tint);
        }
        Assert.True(reddest > 0.9);
        Assert.Equal(new Vec(0, -1), will.Pick(Still with { Window = WindowAt }, 16).Look);
    }

    [Fact]
    public void AHunterLeansItHisWayUnderADartButNotInAWindow()
    {
        var hunter = new Vec(Anchor.X - 200, Anchor.Y);
        var plain = Will(3).Pick(Still with { Meteor = Meteor }, 16);
        var leaning = Will(3).Pick(Still with { Meteor = Meteor, Hunter = hunter }, 16);
        Assert.Equal(new Vec(plain.Goal.X - T.Lean, plain.Goal.Y), leaning.Goal);
        Assert.Equal(WindowAt, Will(3).Pick(Still with { Window = WindowAt, Hunter = hunter }, 16).Goal);
        Assert.Equal(Will(3).Pick(Still, 16).Goal, Will(3).Pick(Still with { Hunter = null }, 16).Goal);
    }

    [Fact]
    public void AMeteorBeyondNoticeABoltOutsideItsZoneAndAHazardOfAnotherSurfaceAreIgnored()
    {
        var far = Meteor with { At = new Vec(Anchor.X + T.Notice + 1, Anchor.Y) };
        Assert.False(Will().Pick(Still with { Meteor = far }, 16).Darting);
        Assert.True(Will().Pick(Still with { Meteor = far with { At = new Vec(Anchor.X + T.Notice - 1, Anchor.Y) } }, 16).Darting);
        var outside = new DroneDanger(9, new Vec(Anchor.X + (2 + T.BoltMargin + 1) * Rig.Cell, 40), 1, Spread: 2);
        Assert.False(Will().Pick(Still with { Bolt = outside }, 16).Darting);
        Assert.True(Will().Pick(Still with { Bolt = outside with { Spread = 3 } }, 16).Darting);
        Assert.False(Will().Pick(Still with { Meteor = Meteor with { Surface = 2 } }, 16).Darting);
        Assert.False(Will().Pick(Still with { Bolt = outside with { Spread = 3, Surface = 0 } }, 16).Darting);
    }

    [Fact]
    public void LowOnAirItPulsesRedAndPassedOutItKeepsToTheAnchor()
    {
        double Reddest(DroneSenses senses)
        {
            var will = Will();
            double most = 0;
            for (int ms = 0; ms < T.PulseMs * 2; ms += 16) most = Math.Max(most, will.Pick(senses, 16).Tint);
            return most;
        }
        Assert.True(Reddest(Still with { Low = true }) > 0.9);
        Assert.Equal(0, Reddest(Still));
        Assert.True(Reddest(Still with { Low = true, Out = true, Meteor = Meteor }) > 0.9);
        var will = Will();
        for (int ms = 0; ms < 20000; ms += 16)
        {
            var intent = will.Pick(Still with { Out = true, Meteor = Meteor }, 16);
            Assert.Equal(Anchor, intent.Goal);
            Assert.False(intent.Darting);
        }
    }

    [Fact]
    public void StandingAMinuteItRePerchesWhenBoredAndStaysHome()
    {
        foreach (int seed in (int[])[1, 2, 3])
        {
            var will = Will(seed);
            var flight = new DroneFlight();
            var goal = will.Pick(Still, 16).Goal;
            double since = 0, sat = 0;
            int perches = 0;
            for (int ms = 16; ms < 60000; ms += 16)
            {
                var intent = will.Pick(Still with { At = flight.At }, 16);
                flight.Step(Anchor, default, intent.Goal, false, intent.Darting, 16);
                Assert.True(InHome(intent.Goal, Anchor));
                Assert.True(Math.Abs(flight.At.X - Anchor.X) <= T.HomeX + 0.5 && Math.Abs(flight.At.Y - Anchor.Y) <= T.HomeY + 0.5,
                    $"flew out of its home to {flight.At}");
                since += 16;
                if (intent.Goal != goal) (goal, sat, since, perches) = (intent.Goal, Math.Max(sat, since), 0, perches + 1);
            }
            Assert.InRange(sat, T.BoredMinMs - 16, T.BoredMaxMs + 16);
            Assert.InRange(perches, 60000 / T.BoredMaxMs, 60000 / T.BoredMinMs + 1);
        }
    }

    [Fact]
    public void InLampAndScanTheLensFollowsThePointerWhateverItDoes()
    {
        var aim = new Vec(0.6, 0.8);
        var will = Will();
        Assert.Equal(aim, will.Pick(Still with { ModeLook = aim }, 16).Look);
        Assert.Equal(aim, will.Pick(Still with { ModeLook = aim, Meteor = Meteor }, 16).Look);
        var free = Will().Pick(Still with { Meteor = Meteor }, 16).Look;
        Assert.NotEqual(aim, free);
    }
}
