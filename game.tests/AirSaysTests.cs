using Stillwake.Game.Features.Atmosphere;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>What the pointer says of the air's machines: a tank its liquid, a vaporizer or the pod the air it makes.</summary>
public class AirSaysTests
{
    [Fact]
    public void ATankTellsItsLiquidOfItsCapacity() =>
        Assert.Equal(["Liquid O₂ · 9000/36000"], AirSays.Says(Spec.Machines.Get("tank"), Spec.Air.Canister, 0)!);

    [Fact]
    public void AVaporizerAndThePodTellTheAirTheyMakeASecondToATenthRoundedDown()
    {
        Assert.Equal(["Makes 20 O₂ a second"], AirSays.Says(Spec.Machines.Get("generator"), 0, 1000)!);
        Assert.Equal(["Makes 7.4 O₂ a second"], AirSays.Says(Spec.Machines.Get("generator"), 0, 374)!);
        Assert.Equal(["Makes 0 O₂ a second"], AirSays.Says(Spec.Machines.Get("lifepod"), 0, 0)!);
    }

    [Fact]
    public void SaysNothingOfAnyOtherMachine() => Assert.Null(AirSays.Says(Spec.Machines.Get("vent"), 0, 0));
}
