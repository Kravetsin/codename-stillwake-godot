using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How the blocks tie to each other.</summary>
public class TechLinksTests
{
    private static TechLink Link(string id) => TechLinks.Of(id);

    [Fact]
    public void WhatTakesAPlateIsShownEvenIfNotYetOpen()
    {
        Assert.Contains("door", Link("plate").UsedIn);
        Assert.Contains("engine", Link("plate").UsedIn.Concat(Link("nozzle").UsedIn));
    }

    [Fact]
    public void ACraftSaysWhatItIsMadeFromAndAt()
    {
        Assert.Contains(Link("wire").MadeAt, m => m == "workbench");
        Assert.Contains(Link("coil").Takes, t => t.Item == "wire");
    }

    [Fact]
    public void AnItemWithNoBlockTiesToo()
    {
        Assert.Contains("wallScrap", Link("scrapChunk").UsedIn);
        Assert.Contains("furnace", Link("hullPlate").UsedIn);
        Assert.Empty(Link("scrapChunk").Takes);
    }

    [Fact]
    public void WhatOnlyTheMarketOpensStillShowsUnderGoesInto() =>
        Assert.Contains("solarPanel", Link("frame").UsedIn);

    [Fact]
    public void AStationTiesToWhatIsMadeAtIt()
    {
        Assert.Contains("wire", Link("workbench").UsedIn);
        Assert.Contains("furnace", Link("iron").MadeAt);
    }

    [Fact]
    public void OnlyDirectTiesCount()
    {
        Assert.Contains("iron", Link("furnace").UsedIn);
        Assert.DoesNotContain("plate", Link("furnace").UsedIn);
        Assert.DoesNotContain("furnace", Link("plate").MadeAt);
        Assert.DoesNotContain(Link("plate").Takes, t => t.Item == "furnace");
    }
}
