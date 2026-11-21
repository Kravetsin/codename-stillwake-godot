using System;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The light drone shows a gesture as its sparks' fan over itself (drone-threads): a point's turned to its target, the rest up.</summary>
public class DroneSignTests
{
    public static TheoryData<Gesture> All => new(Enum.GetValues<Gesture>());

    [Theory]
    [MemberData(nameof(All))]
    public void ItKnowsEveryGestureByTheMotionTheFigureIsGiven(Gesture gesture) =>
        Assert.Equal(gesture, DroneSign.KindOf(GestureMotions.Of(gesture)));

    [Fact]
    public void AMotionOfNoGestureShowsNothing() => Assert.Null(DroneSign.KindOf(GestureMotions.Of(Gesture.Wave) with { Ms = 1 }));

    [Theory]
    [InlineData(1, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(0.6, 0.8)]
    [InlineData(-0.6, -0.8)]
    public void APointsFanHeadsWhereItPoints(double x, double y)
    {
        float turn = DroneSign.Turned(Gesture.Point, (x * 30, y * 30)) ?? float.NaN;
        Assert.Equal(x, Math.Cos(turn), 5);
        Assert.Equal(y, Math.Sin(turn), 5);
    }

    [Fact]
    public void APointAtTheDroneItselfStandsUp() => Assert.Null(DroneSign.Turned(Gesture.Point, (0, 0)));

    [Theory]
    [InlineData(Gesture.Wave)]
    [InlineData(Gesture.Yes)]
    [InlineData(Gesture.No)]
    public void TheOthersStandUp(Gesture gesture) => Assert.Null(DroneSign.Turned(gesture, (-5, 3)));

    [Fact]
    public void APointedFanTurnsWithItsTargetAndKeepsItsSparksOnIt()
    {
        var swarm = new DroneSwarm();
        swarm.Become(SwarmShape.Sign, 0, 0, DroneSign.Turned(Gesture.Point, (1, 0)));
        swarm.Become(SwarmShape.Sign, 0, 2, DroneSign.Turned(Gesture.Point, (0, 1)));
        Assert.Equal(MathF.PI / 2, swarm.TurnOf(SwarmShape.Sign, 0, 2), 5);
        // Its rays head down now: every spark on it is below the core.
        for (int i = 0; i < DroneSwarm.Shared; i++)
        {
            int spark = Enumerable.Range(0, Stillwake.Game.Shared.DroneInk.Sparks).First(k => DroneSwarm.RankOf(k) == i);
            Assert.True(swarm.SparkAt(spark, 0, 2).Y > 0, $"spark {spark} not on the turned fan");
        }
    }
}
