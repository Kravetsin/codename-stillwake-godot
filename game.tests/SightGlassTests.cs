using Stillwake.Game.Shared;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>A store's sight glass (sight-glass): empty only at none, any liquid at least a hi-res pixel, any level to that pixel, full only full.</summary>
public class SightGlassTests
{
    private const int Tank = Spec.Air.TankCapacity;

    [Fact]
    public void ShowsAnyLevelToAHiResPixelEmptyOnlyAtNoneFullOnlyFull()
    {
        Assert.Equal(40, SightGlass.Steps);
        Assert.Equal(0, SightGlass.Height(0, Tank));
        Assert.Equal(0.25f, SightGlass.Height(1, Tank));
        // A litre and thirty in a 150 litre tank no longer look alike, as they did by fifths.
        Assert.Equal(0.25f, SightGlass.Height(1_000, Tank));
        Assert.Equal(2f, SightGlass.Height(30_000, Tank));
        Assert.Equal(5f, SightGlass.Height(Tank / 2, Tank));
        Assert.Equal(9.75f, SightGlass.Height(Tank - 1, Tank));
        Assert.Equal(10f, SightGlass.Height(Tank, Tank));
        // A cistern saved holding 225 litres, before it held 150 (liquid-volumes), shows full.
        Assert.Equal(10f, SightGlass.Height(225_000, Spec.Water.CisternCapacity));
    }

    [Fact]
    public void AFuelStoreSaysHowLongItLastsAtItsDraw()
    {
        Assert.Equal("Nothing drawn from it", Features.Fuel.FuelShows.Lasting(null));
        Assert.Equal("Lasts 45 s at this draw", Features.Fuel.FuelShows.Lasting(45 * 20));
        Assert.Equal("Lasts 12 min at this draw", Features.Fuel.FuelShows.Lasting(12 * 60 * 20 + 59 * 20));
        Assert.Equal("Lasts 2 h 13 min at this draw", Features.Fuel.FuelShows.Lasting(160_000));
        var tank = Spec.Machines.Get("fuelTank");
        Assert.Equal(["Biofuel · 12.50/100.00 l · 12 cans", "Lasts 1 min at this draw"], Features.Fuel.FuelShows.Says(tank, 12_500, 1200)!);
    }

    [Fact]
    public void EveryStoreWithAGlassHasItsLevelAndItsSurface()
    {
        foreach (var kind in new[] { "tank", "cistern", "fuelTank" })
        {
            Assert.True(Png.Has($"{kind}_level"), kind);
            Assert.True(Png.Has($"{kind}_level_top"), kind);
        }
        Assert.True(Png.Has("tank_empty"));
    }
}
