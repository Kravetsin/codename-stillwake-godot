using Stillwake.Game.Features.Logistics;
using Stillwake.Sim;
using Stillwake.Sim.Logistics;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The request slots and the role toggle (0062): what a click, the wheel and a right-click leave, how a slot looks, own commands shown at once.</summary>
public class RequestsTests
{
    private static readonly Request[] Two = [new("scrapChunk", 10), new("planks", 20)];

    [Fact]
    public void AClickWithAStackSetsItsItemAtItsCountInThatSlotOrAddsOneAtTheEnd()
    {
        Assert.Equal([new Request("iron", 50), Two[1]], Requests.Set(Two, 0, "iron", 50));
        Assert.Equal([.. Two, new Request("iron", 7)], Requests.Set(Two, 3, "iron", 7));
    }

    [Fact]
    public void TheWheelChangesACountByOneOrTenWithShiftNeverUnderOneAndARightClickClears()
    {
        Assert.Equal((1, 10), (Requests.StepOf(false), Requests.StepOf(true)));
        Assert.Equal(11, Requests.Nudge(Two, 0, 1)[0].Count);
        Assert.Equal(1, Requests.Nudge(Two, 0, -10)[0].Count);
        Assert.Same(Two, Requests.Nudge(Two, 2, 1));
        Assert.Equal([Two[1]], Requests.Clear(Two, 0));
        Assert.Same(Two, Requests.Clear(Two, 5));
    }

    [Fact]
    public void ASlotIsEmptyMetOrShort()
    {
        Assert.Equal(RequestLook.Empty, Requests.LookOf(null, 0));
        Assert.Equal(RequestLook.Met, Requests.LookOf(Two[0], 10));
        Assert.Equal(RequestLook.Unmet, Requests.LookOf(Two[0], 9));
    }

    [Fact]
    public void ShowsThisPeersLastRequestsAndRoleInFlightBeforeTheirTick()
    {
        Command[] inFlight = [new SetRequests(3, [new("iron", 1)]), new SetRole(4, Role.Provider), new SetRequests(3, [new("iron", 2)]), new SetRequests(5, [])];
        Assert.Equal([new Request("iron", 2)], Requests.Shown(Two, 3, inFlight));
        Assert.Same(Two, Requests.Shown(Two, 9, inFlight));
        Assert.Empty(Requests.Shown(Two, 5, inFlight));
        Assert.Equal(Role.Provider, Requests.RoleShown(Role.Storage, 4, inFlight));
        Assert.Equal(Role.Requester, Requests.RoleShown(Role.Requester, 7, inFlight));
    }

    [Fact]
    public void ADroneIsDrawnCarriedOnTowardItsNextCentreNeverPastIt()
    {
        Assert.Equal((1600.0, 500.0), Requests.Smoothed(1500, 500, 2500, 500, 200, 0.5));
        Assert.Equal((500.0, 900.0), Requests.Smoothed(500, 1000, 500, 500, 200, 0.5));
        Assert.Equal((2500.0, 500.0), Requests.Smoothed(2400, 500, 2500, 500, 200, 1));
        Assert.Equal((500.0, 500.0), Requests.Smoothed(500, 500, 500, 500, 200, 0.7));
    }
}
