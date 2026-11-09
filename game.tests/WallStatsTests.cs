using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A wall's numbers on its build card: its leak a minute by the leak sign (tip-signs), and nothing of meteors (unified-damage).</summary>
public class WallStatsTests
{
    private static TipLine Leak(string rate) => new($"−{rate} l/min a cell", Sign: Sign.Leak);

    [Fact]
    public void GiveEachWallItsLeakAlone()
    {
        Assert.Equal([Leak("0.003")], WallStats.Of(Materials.Get("scrap")));
        Assert.Equal([Leak("0.002")], WallStats.Of(Materials.Get("iron")));
        Assert.Equal([Leak("0.009")], WallStats.Of(Materials.Get("stone")));
        Assert.Equal([Leak("0.012")], WallStats.Of(Materials.Get("soil")));
        foreach (var seal in new[] { "glass", "mica", "hide" })
            Assert.Equal([Leak("0.0015")], WallStats.Of(Materials.Get(seal)));
    }
}
