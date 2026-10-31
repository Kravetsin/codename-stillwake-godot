using Stillwake.Game.Features.Research;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The rings laid out, and how the blocks tie to each other (2026-10-30-tech-rings).</summary>
public class TechRingsTests
{
    private static TechLink Link(string id) => TechLinks.Of(id);

    [Fact]
    public void EveryBlockStandsOnItsTabAndNoTwoOverlap()
    {
        foreach (var tab in Spec.Research.Tabs)
        {
            var spots = new TechRings(tab).Spots;
            var blocks = Spec.Research.Nodes.Where(n => n.Tab == tab.Id && !n.Gate).ToList();
            Assert.Equal(blocks.Count, spots.Count);
            foreach (var a in spots)
                foreach (var b in spots.Where(b => string.CompareOrdinal(a.Key, b.Key) < 0))
                    Assert.True(Math.Sqrt(Math.Pow(a.Value.X - b.Value.X, 2) + Math.Pow(a.Value.Y - b.Value.Y, 2)) >= 100, $"{a.Key} on {b.Key}");
        }
    }

    [Fact]
    public void ALaterRingStandsFurtherOutThanAnEarlierOne()
    {
        foreach (var tab in Spec.Research.Tabs)
        {
            var rings = new TechRings(tab);
            double Far(string id) => Math.Sqrt(Math.Pow(rings.Spots[id].X - rings.Middle, 2) + Math.Pow(rings.Spots[id].Y - rings.Middle, 2));
            var blocks = Spec.Research.Nodes.Where(n => n.Tab == tab.Id && !n.Gate).ToList();
            foreach (var near in blocks)
                foreach (var far in blocks.Where(b => b.Ring > near.Ring))
                    Assert.True(Far(far.Id) > Far(near.Id), $"{far.Id} inside {near.Id}");
            Assert.All(blocks, b => Assert.True(Far(b.Id) >= TechRings.HubRadius));
        }
    }

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
        Assert.Contains("wire", Link("coil").From);
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
        Assert.Contains("furnace", Link("plate").From);
    }
}
