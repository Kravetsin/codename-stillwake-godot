using Stillwake.Game.Features.Grid;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>Which band's stone a planet's cell is drawn in (2026-11-08-planet-layers).</summary>
public class StrataTests
{
    private static readonly Spec.PlanetType Planet = Spec.PlanetTypes.First;

    [Fact]
    public void TheBoundariesWobbleWithinTheirReachAndBothWays()
    {
        var shifts = Enumerable.Range(0, Planet.Ground.Width).Select(Strata.Wobble).ToList();
        Assert.All(shifts, s => Assert.InRange(s, -Strata.Reach, Strata.Reach));
        Assert.Contains(shifts, s => s < 0);
        Assert.Contains(shifts, s => s > 0);
    }

    [Fact]
    public void AboveItsOwnStoneTheLandIsPlainAndBelowEachCellIsItsRowsBandGiveOrTakeTheWobble()
    {
        var strata = new Strata(Planet, from: 100);
        int surface = Planet.Ground.Surface;
        Assert.Equal(Strata.Plain, strata.BandOf(0, surface + 100 - Strata.Reach - 1));
        Assert.NotEqual(Strata.Plain, strata.BandOf(0, surface + 100 + Strata.Reach));
        for (int x = 0; x < Planet.Ground.Width; x += 37)
            for (int y = surface + 100 + Strata.Reach; y < Planet.Ground.Height; y += 11)
            {
                int band = strata.BandOf(x, y);
                Assert.True(band == Stillwake.Sim.Terrain.BandOf(Planet, y - Strata.Reach) || band == Stillwake.Sim.Terrain.BandOf(Planet, y + Strata.Reach)
                    || band == Stillwake.Sim.Terrain.BandOf(Planet, y), $"{x},{y}: {band}");
            }
    }

    [Fact]
    public void EveryBandOfThePlanetIsDrawnSomewhere()
    {
        var strata = new Strata(Planet, from: 100);
        var seen = Enumerable.Range(Planet.Ground.Surface, Planet.Ground.Height - Planet.Ground.Surface).Select(y => strata.BandOf(0, y)).ToHashSet();
        for (int band = 0; band < strata.Bands; band++) Assert.Contains(band, seen);
    }

    [Fact]
    public void OnlyStoneAndItsVeinsAreDrawnByBand()
    {
        Assert.True(Strata.Banded(Spec.Materials.Get("stone")));
        Assert.True(Strata.Banded(Spec.Materials.Get("copperVein")));
        Assert.False(Strata.Banded(Spec.Materials.Get("soil")));
        Assert.False(Strata.Banded(Spec.Materials.Get("bedrock")));
    }
}
