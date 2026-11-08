using Stillwake.Game.Features.Atmosphere;
using Stillwake.Game.Shared;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>What the pointer says of the air's machines: a tank its liquid, a vaporizer or the pod the air it makes (tip-signs).</summary>
public class AirSaysTests
{
    [Fact]
    public void ATankTellsItsLiquidOfItsCapacityByTheCanister() =>
        Assert.Equal([new TipLine("3.000/150.000 l", Icon: "canister")], AirSays.Says(Spec.Machines.Get("tank"), Spec.Air.Canister, 0)!);

    [Fact]
    public void AVaporizerAndThePodTellTheAirTheyMakeAMinuteToATenthRoundedDownByTheOxygenSign()
    {
        static TipLine Made(string n) => new($"+{n} l/min", Sign: Sign.Oxygen);
        Assert.Equal([Made("1.200")], AirSays.Says(Spec.Machines.Get("generator"), 0, 1000)!);
        Assert.Equal([Made("0.4488")], AirSays.Says(Spec.Machines.Get("generator"), 0, 374)!);
        Assert.Equal([Made("0.000")], AirSays.Says(Spec.Machines.Get("lifepod"), 0, 0)!);
    }

    [Fact]
    public void SaysNothingOfAnyOtherMachine() => Assert.Null(AirSays.Says(Spec.Machines.Get("vent"), 0, 0));
}
