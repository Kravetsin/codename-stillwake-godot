using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Placing plans, the near hand shows the drone where they go (drone-holo).</summary>
public class PlanPointTests
{
    private static double ShareOf(PlanPoint point) => point.Arm.Ms / point.Arm.Motion.Ms;

    [Fact]
    public void APlanPlacedRaisesTheArmAtItJabsOnceAndLetsItDownAfter()
    {
        var point = new PlanPoint();
        point.Placed((40, -8));
        Assert.Equal((40.0, -8.0), point.At);
        point.Step(PlanPoint.InMs, may: true);
        Assert.Equal(1, point.Weight);
        Assert.InRange(ShareOf(point), PlanPoint.JabFrom, PlanPoint.JabTo);
        point.Step(1000, may: true);
        Assert.Equal(PlanPoint.JabTo, ShareOf(point), 9);
        point.Step(PlanPoint.OutMs, may: true);
        Assert.Equal(0, point.Weight);
    }

    [Fact]
    public void PlansComingQuicklyMoveThePointWithoutRestartingAJabMidway()
    {
        var point = new PlanPoint();
        point.Placed((10, 0));
        point.Step(50, may: true);
        double share = ShareOf(point);
        point.Placed((20, 0));
        Assert.Equal((20.0, 0.0), point.At);
        Assert.Equal(share, ShareOf(point), 9);
    }

    [Fact]
    public void ThePlanAwayTakesTheArmDownAtOnce()
    {
        var point = new PlanPoint();
        point.Placed((10, 0));
        point.Step(PlanPoint.InMs, may: true);
        point.Step(1, may: false);
        Assert.Equal(0, point.Weight);
    }
}
