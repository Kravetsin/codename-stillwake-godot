using Stillwake.Game.Features.Logistics;
using Stillwake.Sim;
using Stillwake.Sim.Logistics;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The drone badge (0062): its look and tooltip, this peer's own switch shown at once, and a drone drawn between ticks.</summary>
public class ServedTests
{
    [Fact]
    public void TheBadgeIsCrossedWhileSwitchedOffLitWhereABayReachesAndGreyWhereNone()
    {
        Assert.Equal(BadgeLook.Lit, Served.LookOf(served: true, reached: true));
        Assert.Equal(BadgeLook.Unreached, Served.LookOf(served: true, reached: false));
        Assert.Equal(BadgeLook.Off, Served.LookOf(served: false, reached: true));
        Assert.Equal(BadgeLook.Off, Served.LookOf(served: false, reached: false));
        Assert.Equal(("logistics.off", "logistics.switchOn"), Served.TipOf(BadgeLook.Off));
        Assert.Equal("logistics.switchOff", Served.TipOf(BadgeLook.Lit).Click);
    }

    [Fact]
    public void ShowsThisPeersLastSwitchInFlightBeforeItsTick()
    {
        Command[] inFlight = [new SetServed(3, false), new SetServed(4, false), new SetServed(3, true)];
        Assert.True(Served.Shown(false, 3, inFlight));
        Assert.False(Served.Shown(true, 4, inFlight));
        Assert.True(Served.Shown(true, 9, inFlight));
    }

    [Fact]
    public void ADroneIsDrawnCarriedOnTowardItsNextCentreNeverPastIt()
    {
        Assert.Equal((1600.0, 500.0), Served.Smoothed(1500, 500, 2500, 500, 200, 0.5));
        Assert.Equal((500.0, 900.0), Served.Smoothed(500, 1000, 500, 500, 200, 0.5));
        Assert.Equal((2500.0, 500.0), Served.Smoothed(2400, 500, 2500, 500, 200, 1));
        Assert.Equal((500.0, 500.0), Served.Smoothed(500, 500, 500, 500, 200, 0.7));
    }
}
