using Stillwake.Content;
using Stillwake.Game.Dev;
using Stillwake.Game.Features.Grid;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Xunit;
using V = Stillwake.Content.Vessels;

namespace Stillwake.Game.Tests;

/// <summary>A wrecked vessel's client views (vessel-wrecks, W-1): built from the same chain as the sim, and the dev flag's chain.</summary>
public class WreckViewsTests
{
    [Fact]
    public void TheDevFlagWritesEachEventAndItsAgeAndNoneAnIntactVessel()
    {
        Assert.Equal([new WreckStep(WreckKind.Explosion), new WreckStep(WreckKind.Fire), new WreckStep(WreckKind.Asteroid, Old: true)],
            WreckChain.Parse("blast,fire,rock:old"));
        Assert.Equal([new WreckStep(WreckKind.Icing), new WreckStep(WreckKind.Break)], WreckChain.Parse("ice, break"));
        Assert.Empty(WreckChain.Parse("none"));
        Assert.Throws<ArgumentException>(() => WreckChain.Parse("blast,flood"));
        Assert.Equal("blast,fire,rock:old", WreckChain.Write(WreckChain.Parse("blast,fire,rock:old")));
    }

    [Theory]
    [InlineData("shuttle", "blast")]
    [InlineData("freighter", "rock,break")]
    [InlineData("liner", "blast,fire,rock")]
    [InlineData("liner", "rock,break,ice:old")]
    [InlineData("tanker", "blast,break,fire")]
    public void EveryViewOfAWreckIsMadeFromItsBuiltPartsTheSpawnInsideItsArrivalRoom(string cls, string chain)
    {
        for (uint seed = 1; seed <= 8; seed++)
        {
            var built = Shipyard.Build(V.Class(cls)!, seed, WreckChain.Parse(chain));
            var lights = new VesselLights(built);
            var (sx, sy) = Shipyard.Door(built);
            Assert.Contains(built.Vessel.Compartments, c => c.Holds(sx, sy));
            Assert.NotNull(new VesselDress(built));
            Assert.NotNull(new VesselSigns(built));
            Assert.NotNull(Beacons.Of(built.Vessel));
            // The lamps are what the built vessel holds: those an event took are gone with it.
            Assert.All(lights.Lamps, l => Assert.Contains(built.Parts, p => p.Layer == "machine" && p.X == l.X && p.Y == l.Y));
        }
    }
}
