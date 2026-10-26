using Stillwake.Game.Features.Creatures;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// A creature's legs at speed (0102): the user saw every kind twitch very fast fleeing, legs at 7 strides a second. Now strides
/// are counted by time at a rate that rises with speed and is capped, the same at any frame rate, so a flight is a gallop.
/// </summary>
public class CreatureGaitTests
{
    public static TheoryData<string> Designs() => [.. CreatureLooks.Runs.Keys];

    /// <summary>A species' speed in pixels a second at <paramref name="perTick"/> thousandths of a cell a tick.</summary>
    private static double Px(int perTick) => perTick / 1000.0 * Camera.Cell * 20;

    [Theory]
    [MemberData(nameof(Designs))]
    public void StridesRiseWithSpeedSmoothlyAndNeverPassTheirCap(string design)
    {
        var run = CreatureLooks.Runs[design];
        double last = 0;
        for (double v = 0.25; v <= 2000; v += 0.25)
        {
            double now = CreatureGait.StridesAt(v, run);
            Assert.True(now >= last && now - last < 0.05, $"{design}: {last:F3} to {now:F3} strides a second at {v} px/s");
            Assert.True(now <= run.MostStrides, $"{design}: {now:F2} strides a second at {v}");
            last = now;
        }
        // At a crawl a stride is its walking length, so its feet keep to the ground.
        Assert.Equal(1 / run.Stride, CreatureGait.StridesAt(0.01, run) / 0.01, 3);
    }

    [Fact]
    public void EveryKindWalksAtItsOldPaceAndFleesAtHalfTheOldCadenceOrLess()
    {
        foreach (var species in Spec.Creatures.All.Where(s => CreatureLooks.Varieties.ContainsKey(s.Id)))
        {
            var run = CreatureLooks.Runs[species.Look];
            double walk = CreatureGait.StridesAt(Px(((Spec.Creatures.Walker)species.Body).Walk), run), flee = CreatureGait.StridesAt(Px(((Spec.Creatures.Walker)species.Body).Run), run);
            // Before: 2.6 a second walking, 7.3 fleeing at 30 cells a second.
            Assert.InRange(walk, 1.7, 2.7);
            Assert.InRange(flee, 2.5, 3.5);
            Assert.True(flee < walk * 1.8, $"{species.Id}: {flee:F2} fleeing against {walk:F2} walking");
        }
    }

    /// <summary>
    /// A creature running as the view draws it: a walk for a second, a flight for two, a stop, at <paramref name="fps"/>, its
    /// frames even or ragged (±30%), the sim's speed changing only on its ticks. Its strides at the end and the most its rate
    /// changed from one frame to the next, after easing.
    /// </summary>
    private static (double Phase, double Jolt) Ran(CreatureLooks.Run run, double fps, bool ragged)
    {
        var gait = new CreatureGait(run);
        var rng = new System.Random(3);
        double now = 0, rate = -1, jolt = 0;
        while (now < 4000)
        {
            double ms = 1000 / fps * (ragged ? 0.7 + 0.6 * rng.NextDouble() : 1);
            int tick = (int)(now / 50);
            double speed = tick < 20 ? Px(200) : tick < 60 ? Px(1500) : 0;
            double before = gait.Phase;
            gait.Step(ms, speed, grounded: true, fleeing: tick is >= 20 and < 60);
            now += ms;
            double r = (gait.Phase - before) / ms * 1000;
            if (rate >= 0 && tick is > 23 and < 60) jolt = System.Math.Max(jolt, System.Math.Abs(r - rate));
            rate = r;
        }
        return (gait.Phase, jolt);
    }

    [Theory]
    [MemberData(nameof(Designs))]
    public void TheLegsGoTheSameAtSixtyFramesAndAHundredAndFortyFourEvenOrRaggedAndNeverJolt(string design)
    {
        var run = CreatureLooks.Runs[design];
        var (sixty, jolt60) = Ran(run, 60, false);
        foreach (var (fps, ragged) in new[] { (144.0, false), (60.0, true), (144.0, true) })
        {
            var (phase, jolt) = Ran(run, fps, ragged);
            Assert.True(System.Math.Abs(phase - sixty) < 0.02 * sixty, $"{design}: {phase:F3} strides at {fps} fps against {sixty:F3} at 60");
            Assert.True(jolt < 0.01, $"{design}: the rate jolted {jolt:F4} a second between frames at {fps} fps");
        }
        Assert.True(jolt60 < 0.01);
    }

    [Fact]
    public void ItGallopsOnlyAtAFlightAndItsLegsRestStanding()
    {
        var run = CreatureLooks.Runs["classic"];
        CreatureGait At(double speed)
        {
            var g = new CreatureGait(run);
            for (int i = 0; i < 120; i++) g.Step(1000.0 / 60, speed, true, false);
            return g;
        }
        Assert.Equal((0, 0), (At(0).Walking, At(0).Gallop));
        Assert.Equal((1, 0), (At(Px(200)).Walking, At(Px(200)).Gallop));
        // A blow's knock, 400 a tick, is no gallop; a flight is a whole one.
        Assert.Equal(0, At(Px(400)).Gallop);
        Assert.Equal(1, At(Px(1500)).Gallop, 3);
        var air = new CreatureGait(run);
        for (int i = 0; i < 60; i++) air.Step(1000.0 / 60, Px(800), grounded: false, fleeing: true);
        Assert.Equal(0, air.Phase);
        Assert.True(air.Air > 0.99 && air.Flee > 0.99);
    }

    [Theory]
    [MemberData(nameof(Designs))]
    public void APoseStandsItsLegsStillAndShowsEachEyeStateAndMovesOnlyALittleFromFrameToFrame(string design)
    {
        var pose = new CreaturePose(CreatureArt.Designs[design], CreatureLooks.Runs[design]);
        var rig = CreatureArt.Designs[design].Rig;
        CreaturePose.Doing Still(double eye = 1, double graze = 0) => new(0.3, 0, 0, 0, 0, graze, 0, 1000, 0, eye, null);
        pose.Set(Still());
        for (int i = 0; i < rig.Count; i++)
            if (rig[i].Drive == CreatureArt.Drive.Leg) Assert.Equal((0, 0, 0), (pose.Joints[i].A, pose.Joints[i].X, pose.Joints[i].Y));
        Assert.Equal(CreaturePose.EyeState.Open, pose.Eye);
        pose.Set(Still(eye: 0.1));
        Assert.Equal(CreaturePose.EyeState.Blink, pose.Eye);
        pose.Set(Still(eye: 1.45));
        Assert.Equal(CreaturePose.EyeState.Wide, pose.Eye);
        pose.Set(Still(graze: 1));
        Assert.Equal(CreaturePose.EyeState.Half, pose.Eye);
        // Galloping at its most strides a second, a 144th of a second moves no node far: no twitch.
        var run = CreatureLooks.Runs[design];
        double step = run.MostStrides / 144, most = 0;
        var was = new CreaturePose.Joint[rig.Count];
        for (int f = 0; f < 144; f++)
        {
            pose.Set(new(f * step, 1, 1, 0, 1, 0, 0, f * 1000.0 / 144, 1, 1, null));
            if (f > 0)
                for (int i = 0; i < rig.Count; i++)
                    most = System.Math.Max(most, System.Math.Abs(pose.Joints[i].A - was[i].A) + System.Math.Abs(pose.Joints[i].Y - was[i].Y) / 8);
            pose.Joints.CopyTo(was, 0);
        }
        Assert.True(most < 0.2, $"{design}: a node moved {most:F3} in a frame");
    }
}
