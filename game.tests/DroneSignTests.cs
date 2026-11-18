using System;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The light drone shows a gesture as a hologram over itself (light-drone): risen with the gesture's blend, a point turned to its target.</summary>
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
    [MemberData(nameof(All))]
    public void ItRisesOutOfTheLensWithTheGestureHoldsAndSinksBackAsItEnds(Gesture gesture)
    {
        var motion = GestureMotions.Of(gesture);
        var lens = (0.6, 0.2);
        Assert.Equal(0, DroneSign.At(motion, 0, 0).Alpha);
        Assert.Equal(0, DroneSign.At(motion, motion.Ms, 0).Alpha);
        Assert.Equal(lens, DroneSign.Place(DroneSign.At(motion, 0, 0).Risen, lens));
        Assert.Equal(DroneSign.Least, DroneSign.At(motion, 0, 0).Scale, 9);
        // Half-way in it stands risen over the drone, whole.
        var held = DroneSign.At(motion, motion.Ms / 2, 0);
        Assert.Equal(1, held.Risen, 9);
        Assert.Equal(1, held.Scale, 9);
        Assert.Equal((0, -DroneSign.Above), DroneSign.Place(held.Risen, lens));
        // Between, it follows the arm's own blend.
        double ms = Gestures.BlendMs / 2;
        Assert.Equal(Gestures.WeightAt(motion, ms), DroneSign.At(motion, ms, 0).Risen, 9);
        Assert.Equal(Gestures.WeightAt(motion, motion.Ms - ms), DroneSign.At(motion, motion.Ms - ms, 0).Risen, 9);
    }

    [Fact]
    public void ItStandsClearOverTheDroneAndIsAboutACellTall()
    {
        Assert.InRange(DroneSign.Height, 8, 10);
        // The drone's disc is 10 px across: the sign's lower edge stays above it.
        Assert.True(DroneSign.Above - DroneSign.Height / 2 > 5);
    }

    [Fact]
    public void ItFlickersBelowItsAlphaButNeverOut()
    {
        double least = 1, most = 0;
        for (int ms = 0; ms < 20_000; ms += 7)
        {
            double f = DroneSign.Flicker(ms);
            (least, most) = (Math.Min(least, f), Math.Max(most, f));
        }
        Assert.InRange(least, 1 - DroneSign.FlickerDepth - 1e-9, 1);
        Assert.InRange(most, least, 1 + 1e-9);
        Assert.True(most - least > DroneSign.FlickerDepth / 2);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(0.6, 0.8)]
    [InlineData(-0.6, -0.8)]
    public void APointsHangingFingerTurnsToWhereItPoints(double x, double y)
    {
        var (turn, mirrored) = DroneSign.Turned(Gesture.Point, (x * 30, y * 30));
        // The wheel's point hangs, its finger down: turned clockwise by the turn, after the mirror, which keeps it down.
        Assert.Equal(x, -Math.Sin(turn), 9);
        Assert.Equal(y, Math.Cos(turn), 9);
        Assert.Equal(x < 0, mirrored);
    }

    [Theory]
    [InlineData(Gesture.Wave)]
    [InlineData(Gesture.Yes)]
    [InlineData(Gesture.No)]
    public void TheOthersStandAsTheWheelShowsThem(Gesture gesture) => Assert.Equal((0.0, false), DroneSign.Turned(gesture, (-5, 3)));
}
