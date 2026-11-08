using Stillwake.Game.Features.Logistics;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A salvage drone's net (salvage-drones, salvage-trips): open going for a load, half shut as each catch comes in, shut round them, open again once they are put in.</summary>
public class GripTests
{
    private const double Frame = 1.0 / 60;

    private static Grip Run(Grip grip, double seconds, int held)
    {
        for (double t = 0; t < seconds - 1e-9; t += Frame) grip.Step(Frame, held);
        return grip;
    }

    /// <summary>The frames it stays half shut holding <paramref name="held"/>.</summary>
    private static int Mid(Grip grip, int held)
    {
        int mid = 0;
        for (grip.Step(Frame, held); grip.Pose == GripPose.Mid; grip.Step(Frame, held)) mid++;
        return mid;
    }

    [Fact]
    public void ItsNetIsOpenGoingForALoadHalfShutAsItTakesOneThenShutRoundIt()
    {
        var grip = Run(new Grip(0), 1, 0);
        Assert.Equal(GripPose.Open, grip.Pose);
        int mid = Mid(grip, 1);
        Assert.Equal(GripPose.Shut, grip.Pose);
        Assert.InRange(mid * Frame, SalvageLook.GripMidS - Frame, SalvageLook.GripMidS + Frame);
        Run(grip, 5, 1);
        Assert.Equal(GripPose.Shut, grip.Pose);
    }

    [Fact]
    public void ItHalfShutsAgainAsEachCatchMoreComesIn()
    {
        var grip = Run(new Grip(1), 0.5, 1);
        Assert.Equal(GripPose.Shut, grip.Pose);
        foreach (int held in new[] { 2, 3, 4 })
        {
            Assert.InRange(Mid(grip, held) * Frame, SalvageLook.GripMidS - Frame, SalvageLook.GripMidS + Frame);
            Assert.Equal(GripPose.Shut, Run(grip, 1, held).Pose);
        }
    }

    [Fact]
    public void ItOpensOnceTheLoadIsPutInAndShutsAgainOnTheNext()
    {
        var grip = Run(new Grip(3), 0.5, 3);
        Assert.Equal(GripPose.Shut, grip.Pose);
        grip.Step(Frame, 0);
        Assert.Equal(GripPose.Open, grip.Pose);
        grip.Step(Frame, 1);
        Assert.Equal(GripPose.Mid, grip.Pose);
    }

    [Fact]
    public void OneMetAlreadyHoldingALoadIsShut()
    {
        Assert.Equal(GripPose.Shut, new Grip(2).Pose);
        Assert.Equal(GripPose.Shut, Run(new Grip(2), Frame, 2).Pose);
        Assert.Equal(GripPose.Open, new Grip(0).Pose);
    }
}
