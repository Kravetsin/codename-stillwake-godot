using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;
using Tune = Stillwake.Game.Features.Crew.DroneThoughts.Tune;

namespace Stillwake.Game.Tests;

/// <summary>The light drone's thoughts and moods (drone-thoughts): what shows over it, when, and what holds it back or cuts it.</summary>
public class DroneThoughtsTests
{
    private static readonly Vec Anchor = new(400, 200);
    private static readonly DroneSenses Mine = new(Anchor, Anchor, Surface: 1, Own: true, Alarms: true);
    private static readonly DroneDanger Meteor = new(7, new Vec(480, 120), 1);
    private static readonly DroneDanger Bolt = new(9, new Vec(410, 40), 1, Spread: 2);

    private static DroneWill Will() => new(new Random(1));

    /// <summary>What shows over it each step of <paramref name="stepMs"/> for <paramref name="ms"/>, sensing <paramref name="senses"/>.</summary>
    private static List<DroneThought?> Run(DroneWill will, DroneSenses senses, double ms, double stepMs = 16)
    {
        var shown = new List<DroneThought?>();
        for (double t = 0; t < ms; t += stepMs)
        {
            will.Pick(senses, stepMs);
            shown.Add(will.Thoughts.Showing);
        }
        return shown;
    }

    private static DroneSenses Felt(bool heat, int degrees) => heat ? Mine with { Heat = degrees } : Mine with { Cold = degrees };

    [Theory]
    [InlineData(false, DroneThought.Cold)]
    [InlineData(true, DroneThought.Heat)]
    public void ColdOrHeatShowsOnceThenAgainOnlyAStepDeeperAndItsMarkResetsOnlyAtZero(bool heat, DroneThought thought)
    {
        var will = Will();
        Assert.Contains(thought, Run(will, Felt(heat, 1), 100));
        Assert.DoesNotContain(thought, Run(will, Felt(heat, Tune.Step), 5000)[^100..]);
        Assert.Contains(thought, Run(will, Felt(heat, Tune.Step + 1), 100));
        Run(will, Felt(heat, Tune.Step + 1), 5000);
        Assert.DoesNotContain(thought, Run(will, Felt(heat, 1), 5000));
        // Back up to the level shown: its mark stayed, so nothing again.
        Assert.DoesNotContain(thought, Run(will, Felt(heat, Tune.Step + 1), 5000));
        Run(will, Felt(heat, 0), 100);
        Assert.Contains(thought, Run(will, Felt(heat, 1), 100));
    }

    [Fact]
    public void WithoutTheAlarmsKnownNoAlarmShowsYetTheMoodsDo()
    {
        var will = Will();
        var shown = Run(will, Mine with { Alarms = false, Cold = 10, Heat = 10, Meteor = Meteor }, 3000);
        Assert.All(shown, s => Assert.Null(s));
        Assert.True(will.Pick(Mine with { Alarms = false, Meteor = Meteor }, 16).Darting);
        Assert.Contains(DroneThought.Love, Run(will, Mine with { Alarms = false, Woke = true }, 100));
    }

    [Fact]
    public void ADartsDangerShowsRedWhileItDartsThenFallsApart()
    {
        var will = Will();
        var intent = default(DroneIntent);
        for (int ms = 0; ms < 4000; ms += 16)
        {
            intent = will.Pick(Mine with { Meteor = Meteor }, 16);
            Assert.Equal(DroneThought.Meteor, will.Thoughts.Showing);
        }
        Assert.Equal(DroneThought.Meteor, intent.Thought);
        Assert.True(intent.Tint > 0);
        var gone = will.Pick(Mine, 16);
        Assert.Null(gone.Thought);
        Assert.Contains(null, Run(will, Mine, DroneInk.ThoughtOut * 1000 + 50));
    }

    [Fact]
    public void ABoltCutsTheSnowflakeWhichNeverReturns()
    {
        var will = Will();
        Run(will, Mine with { Cold = 1 }, 200);
        Assert.Equal(DroneThought.Cold, will.Thoughts.Showing);
        Assert.Contains(DroneThought.Bolt, Run(will, Mine with { Cold = 1, Bolt = Bolt }, 500));
        Assert.DoesNotContain(DroneThought.Cold, Run(will, Mine with { Cold = 1 }, 5000));
    }

    public static TheoryData<string> Higher => ["gesture", "window", "icon"];

    private static DroneSenses Above(DroneSenses senses, string higher) => higher switch
    {
        "gesture" => senses with { Signing = true },
        "window" => senses with { Window = new Vec(416, 250) },
        _ => senses with { Icon = true },
    };

    [Theory]
    [MemberData(nameof(Higher))]
    public void AGestureWindowOrIconHoldsColdBackAndCutsItForGood(string higher)
    {
        var will = Will();
        Assert.All(Run(will, Above(Mine with { Cold = 1 }, higher), 1000), s => Assert.Null(s));
        Assert.Contains(DroneThought.Cold, Run(will, Mine with { Cold = 1 }, 100));
        Run(will, Above(Mine with { Cold = 1 }, higher), 100);
        Assert.Null(will.Thoughts.Showing);
        Assert.DoesNotContain(DroneThought.Cold, Run(will, Mine with { Cold = 1 }, 5000));
    }

    [Fact]
    public void CuriousOnceAMachineWithinWonderAndNotTwiceInAGap()
    {
        var will = Will();
        var lamp = new DroneWonder(31, new Vec(200, 150));
        var intent = will.Pick(Mine with { Wonder = lamp }, 16);
        Assert.Equal(DroneThought.Curious, intent.Thought);
        Assert.True(intent.Look.X < 0);
        Assert.DoesNotContain(DroneThought.Curious, Run(will, Mine with { Wonder = lamp }, Tune.MoodGapMs + 10_000, 100)[30..]);
        var other = new DroneWonder(77, new Vec(600, 150));
        will = Will();
        Run(will, Mine with { Wonder = lamp }, 3000);
        var shown = Run(will, Mine with { Wonder = other }, Tune.MoodGapMs, 100);
        Assert.DoesNotContain(DroneThought.Curious, shown[..^50]);
        Assert.Contains(DroneThought.Curious, shown);
    }

    [Fact]
    public void BoredAfterStandingStillAMinuteAgainOnlyAfterMoving()
    {
        var will = Will();
        Assert.DoesNotContain(DroneThought.Bored, Run(will, Mine, Tune.BoredAfterMs - 1000, 100));
        Assert.Contains(DroneThought.Bored, Run(will, Mine, 2000, 100));
        Assert.DoesNotContain(DroneThought.Bored, Run(will, Mine, 3 * Tune.MoodGapMs, 100)[30..]);
        var moved = new Vec(Anchor.X + 3 * Tune.BoredBox, Anchor.Y);
        Assert.Contains(DroneThought.Bored, Run(will, Mine with { Anchor = moved, At = moved }, Tune.BoredAfterMs + 1000, 100));
    }

    [Fact]
    public void LoveOnWakingPressesItToTheHelmetAndWaitsBehindAWindowNeverLost()
    {
        var will = Will();
        var window = new Vec(416, 250);
        will.Pick(Mine with { Woke = true, Window = window }, 16);
        Assert.All(Run(will, Mine with { Window = window }, 5000), s => Assert.Null(s));
        var intent = will.Pick(Mine, 16);
        Assert.Equal(DroneThought.Love, intent.Thought);
        Run(will, Mine, 600);
        var pressed = will.Pick(Mine, 16);
        Assert.True(Math.Abs(pressed.Goal.X - Anchor.X) < 10);
        // Down from its perch over the head to the helmet, below the anchor, above the neck.
        Assert.InRange(pressed.Goal.Y, Anchor.Y, Anchor.Y + DroneFlight.Tune.Above);
    }

    [Fact]
    public void PrideAfterADeedLessonRisesTheBody()
    {
        var will = Will();
        will.Pick(Mine with { Proud = true }, 16);
        Assert.Equal(DroneThought.Pride, will.Thoughts.Showing);
        Run(will, Mine, 600);
        Assert.True(will.Pick(Mine, 16).Bearing.Off.Y < 0);
    }

    [Fact]
    public void NothingShowsOverAFriendsDroneNorWhileItsHeroIsOutOrQuiet()
    {
        var all = Mine with { Woke = true, Proud = true, Cold = 9, Meteor = Meteor, Wonder = new DroneWonder(31, new Vec(200, 150)) };
        var friend = Will();
        Assert.All(Run(friend, all with { Own = false }, 3000), s => Assert.Null(s));
        var quiet = Will();
        Assert.All(Run(quiet, all with { Quiet = true }, 3000), s => Assert.Null(s));
    }
}
