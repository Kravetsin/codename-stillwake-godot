using Stillwake.Game.Features.Atmosphere;
using Stillwake.Game.Shared;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>What the pointer says of the air's machines: a tank its liquid, a vaporizer or the pod the air it makes (tip-signs).</summary>
public class AirSaysTests
{
    [Fact]
    public void ATankTellsItsLiquidOfItsCapacityAndTheWholeCanistersItIsByTheCanister()
    {
        var tank = Spec.Machines.Get("tank");
        Assert.Equal([new TipLine("3.00/150.00 l · 1 canisters", Icon: "canister")], AirSays.Says(tank, Spec.Air.Canister, 0)!);
        // What is short of a canister is no canister yet, nor short of a litre's hundredth a hundredth.
        Assert.Equal([new TipLine("87.99/150.00 l · 29 canisters", Icon: "canister")], AirSays.Says(tank, 87_999, 0)!);
    }

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
