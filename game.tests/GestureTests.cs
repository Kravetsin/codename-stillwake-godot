using System;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Signs made with the near hand in place of words (gestures-for-words): when one starts, how it plays and ends, and how a friend's is heard.</summary>
public class GestureTests
{
    public static TheoryData<Gesture> All => new(Enum.GetValues<Gesture>());

    [Theory]
    [InlineData(Pose.Stand, true)]
    [InlineData(Pose.Walk, true)]
    [InlineData(Pose.Run, true)]
    [InlineData(Pose.Air, true)]
    [InlineData(Pose.Float, true)]
    [InlineData(Pose.Climb, false)]
    [InlineData(Pose.Sit, false)]
    [InlineData(Pose.Strapped, false)]
    [InlineData(Pose.Lie, false)]
    [InlineData(Pose.Tumble, false)]
    public void StartsOnlyOnItsFeetJumpingOrAfloat(Pose pose, bool starts) => Assert.Equal(starts, Gestures.CanStart(pose, handsBusy: false, holdsPlan: false));

    [Fact]
    public void NeverWithBusyHandsOrThePlanOut()
    {
        Assert.False(Gestures.CanStart(Pose.Stand, handsBusy: true, holdsPlan: false));
        Assert.False(Gestures.CanStart(Pose.Stand, handsBusy: false, holdsPlan: true));
    }

    [Theory]
    [MemberData(nameof(All))]
    public void PlaysItsLengthAndEnds(Gesture gesture)
    {
        var play = new GesturePlay();
        play.Start(gesture, new Point(5, 7));
        play.Step(Gestures.MsOf(gesture) - 1);
        Assert.Equal(gesture, play.Kind);
        Assert.Equal(new Busy(Gestures.WorkOf(gesture), new Point(5, 7), 1), play.Sent);
        play.Step(1);
        Assert.Null(play.Kind);
        Assert.Null(play.Sent);
    }

    [Theory]
    [MemberData(nameof(All))]
    public void TakesNothingOfTheArmAtItsStartAndEnd(Gesture gesture)
    {
        Assert.Equal(0, Gestures.WeightAt(GestureMotions.Of(gesture), 0));
        Assert.Equal(0, Gestures.WeightAt(GestureMotions.Of(gesture), Gestures.MsOf(gesture)));
        Assert.Equal(1, Gestures.WeightAt(GestureMotions.Of(gesture), Gestures.MsOf(gesture) / 2));
    }

    [Fact]
    public void PointsAlongTheLineToItsTarget()
    {
        foreach (var (x, y) in new[] { (1.0, 0.0), (1.0, 1.0), (0.3, -1.0), (1.0, -0.2) })
        {
            // Held still after its jabs, the arm straight along the line.
            var point = GestureMotions.Of(Gesture.Point);
            var end = Rig.EndOf(Gestures.ArmAt(point, point.Ms * 0.75, (x, y)).Arm, Rig.Arm);
            Assert.InRange(Math.Atan2(end.Y, end.X) - Math.Atan2(y, x), -0.05, 0.05);
        }
    }

    [Fact]
    public void WavesOverhead()
    {
        for (double ms = 0; ms <= Gestures.MsOf(Gesture.Wave); ms += 100)
        {
            var hand = Rig.EndOf(Gestures.ArmAt(GestureMotions.Of(Gesture.Wave), ms, default).Arm, Rig.Arm);
            // Above the shoulder by more than the upper arm reaches: the forearm stands up over the elbow.
            Assert.True(-hand.Y > Rig.Arm.Upper, $"{ms}: {hand}");
        }
    }

    [Fact]
    public void AFriendsGesturePlaysOncePerNewCount()
    {
        var played = new GesturePlay();
        var sent = new Busy(Handwork.Wave, default, 3);
        played.Hear(sent);
        Assert.Equal(Gesture.Wave, played.Kind);
        played.Step(500);
        // The same gesture heard again plays on; it does not start over.
        played.Hear(sent);
        Assert.Equal(500, played.Ms);
        // Its end heard as no work: it plays out here on its own clock.
        played.Hear(null);
        Assert.Equal(Gesture.Wave, played.Kind);
        played.Step(Gestures.MsOf(Gesture.Wave));
        Assert.Null(played.Kind);
        // Sent once more after it ended here: not played again.
        played.Hear(sent);
        Assert.Null(played.Kind);
        // The next one, numbered anew, plays from its start, where it points.
        played.Hear(new Busy(Handwork.Point, new Point(1, 2), 4));
        Assert.Equal((Gesture.Point, new Point(1, 2), 0.0), (played.Kind!.Value, played.At, played.Ms));
    }

    [Fact]
    public void OtherWorkOfAFriendsHandsCutsItsGestureOff()
    {
        var played = new GesturePlay();
        played.Hear(new Busy(Handwork.Point, default, 1));
        played.Hear(new Busy(Handwork.Chop, default, 9));
        Assert.Null(played.Kind);
    }

    [Fact]
    public void EveryGestureGoesThroughTheWireAsItsOwnWork()
    {
        foreach (var gesture in Enum.GetValues<Gesture>()) Assert.Equal(gesture, Gestures.Of(Gestures.WorkOf(gesture)));
        Assert.Null(Gestures.Of(Handwork.Chop));
    }

    [Fact]
    public void TheThumbStandsUpForYesAndDownForNo()
    {
        // The thumb sticks out ahead of the hanging hand (frame 3); the hand turns with the forearm, mirrored for no.
        static double ThumbUp(Gesture gesture)
        {
            var motion = GestureMotions.Of(gesture);
            var arm = Gestures.ArmAt(motion, motion.Ms / 2, default).Arm;
            double turn = arm.Upper + arm.Lower, ahead = motion.Mirrored ? -1 : 1;
            return ahead * Math.Sin(turn);
        }
        Assert.True(ThumbUp(Gesture.Yes) > 0.9);
        Assert.True(ThumbUp(Gesture.No) < -0.9);
    }

    private static Wheel<Gesture> NewWheel() => new(Gestures.WheelSides);

    /// <summary>One step of the gesture key: a tap points.</summary>
    private static Gesture? Step(Wheel<Gesture> wheel, bool held, double ms, (double, double) at) => wheel.Step(held, ms, at, Wheel.Middle, Gesture.Point);

    [Fact]
    public void ATapPoints()
    {
        var wheel = NewWheel();
        Assert.Null(Step(wheel, true, 16, (100, 100)));
        Assert.Null(Step(wheel, true, 100, (102, 101)));
        Assert.False(wheel.Open);
        Assert.Equal(Gesture.Point, Step(wheel, false, 16, (102, 101)));
        // Nothing after: the key let go once.
        Assert.Null(Step(wheel, false, 16, (102, 101)));
    }

    [Fact]
    public void HeldItOpensAndLettingGoInTheMiddlePicksNothing()
    {
        var wheel = NewWheel();
        Step(wheel, true, 16, (100, 100));
        Step(wheel, true, Wheel.TapMs, (105, 100));
        Assert.True(wheel.Open);
        Assert.Null(wheel.Toward);
        Assert.Null(Step(wheel, false, 16, (105, 100)));
        Assert.False(wheel.Open);
    }

    [Theory]
    [InlineData(0, -40, Gesture.Yes)]
    [InlineData(5, -40, Gesture.Yes)]
    [InlineData(35, 20, Gesture.No)]
    [InlineData(40, -5, Gesture.No)]
    [InlineData(-35, 20, Gesture.Wave)]
    [InlineData(-40, -5, Gesture.Wave)]
    public void LettingGoPicksTheSideThePointerMovedToward(double dx, double dy, Gesture picked)
    {
        var wheel = NewWheel();
        Step(wheel, true, 16, (100, 100));
        Step(wheel, true, Wheel.TapMs, (100 + dx, 100 + dy));
        Assert.Equal(picked, wheel.Toward);
        Assert.Equal(picked, Step(wheel, false, 16, (100 + dx, 100 + dy)));
    }

    [Fact]
    public void AQuickFlickPicksWithoutTheWheel()
    {
        var wheel = NewWheel();
        Step(wheel, true, 16, (100, 100));
        Assert.Equal(Gesture.Wave, Step(wheel, false, 50, (60, 100)));
    }
}
