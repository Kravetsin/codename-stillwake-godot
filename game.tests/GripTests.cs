using Stillwake.Game.Features.Logistics;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A salvage drone's net (salvage-drones): open going for a load, half shut as it takes one, shut round it, open again once it is put in.</summary>
public class GripTests
{
    private const double Frame = 1.0 / 60;

    private static Grip Run(Grip grip, double seconds, bool held)
    {
        for (double t = 0; t < seconds - 1e-9; t += Frame) grip.Step(Frame, held);
        return grip;
    }

    [Fact]
    public void ItsNetIsOpenGoingForALoadHalfShutAsItTakesOneThenShutRoundIt()
    {
        var grip = Run(new Grip(false), 1, false);
        Assert.Equal(GripPose.Open, grip.Pose);
        int mid = 0;
        for (grip.Step(Frame, true); grip.Pose == GripPose.Mid; grip.Step(Frame, true)) mid++;
        Assert.Equal(GripPose.Shut, grip.Pose);
        Assert.InRange(mid * Frame, SalvageLook.GripMidS - Frame, SalvageLook.GripMidS + Frame);
        Run(grip, 5, true);
        Assert.Equal(GripPose.Shut, grip.Pose);
    }

    [Fact]
    public void ItOpensOnceTheLoadIsPutInAndShutsAgainOnTheNext()
    {
        var grip = Run(new Grip(true), 0.5, true);
        Assert.Equal(GripPose.Shut, grip.Pose);
        grip.Step(Frame, false);
        Assert.Equal(GripPose.Open, grip.Pose);
        grip.Step(Frame, true);
        Assert.Equal(GripPose.Mid, grip.Pose);
    }

    [Fact]
    public void OneMetAlreadyHoldingALoadIsShut()
    {
        Assert.Equal(GripPose.Shut, new Grip(true).Pose);
        Assert.Equal(GripPose.Shut, Run(new Grip(true), Frame, true).Pose);
        Assert.Equal(GripPose.Open, new Grip(false).Pose);
    }
}
