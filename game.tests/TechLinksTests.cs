using Stillwake.Content;
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
    public void ATieIsNamedFromThePickedThingsSide()
    {
        Assert.Equal(Tie.MadeAt, TechLinks.TieOf("iron", "furnace"));
        Assert.Equal(Tie.From, TechLinks.TieOf("coil", "wire"));
        Assert.Equal(Tie.UsedIn, TechLinks.TieOf("workbench", "wire"));
        Assert.Null(TechLinks.TieOf("coil", "furnace"));
    }

    [Fact]
    public void ABlueprintNamesTheBuildMenuTabItSitsIn()
    {
        Assert.Equal(MenuTabs.Production, TechLinks.MenuTabOf("furnace"));
        Assert.Equal(MenuTabs.Structure, TechLinks.MenuTabOf("wallScrap"));
        Assert.Equal(MenuTabs.Structure, TechLinks.MenuTabOf("backWallScrap"));
        Assert.Null(TechLinks.MenuTabOf("iron"));
    }

    [Fact]
    public void OnlyDirectTiesCount()
    {
        Assert.Contains("iron", Link("furnace").UsedIn);
        Assert.DoesNotContain("plate", Link("furnace").UsedIn);
        Assert.DoesNotContain("furnace", Link("plate").MadeAt);
        Assert.DoesNotContain(Link("plate").Takes, t => t.Item == "furnace");
    }

    /// <summary>
    /// A card says "never made, only found" of what nothing makes alone: not of what is smelted or crushed, as iron ore and gravel
    /// are, nor of what the pod makes from nothing. Every item a card calls found has no station making it.
    /// </summary>
    [Fact]
    public void OnlyWhatNothingMakesIsCalledFound()
    {
        Assert.Equal(Origin.Found, TechLinks.OriginOf("scrapChunk"));
        Assert.Equal(Origin.Found, TechLinks.OriginOf("canister"));
        Assert.Equal(Origin.Worked, TechLinks.OriginOf("ironOre"));
        Assert.Equal(["asteroidOre"], Link("ironOre").From);
        Assert.Equal(Origin.Worked, TechLinks.OriginOf("gravel"));
        Assert.Equal(["scrap", "hullPlate", "ironOre"], Link("iron").From);
        Assert.Equal(Origin.Worked, TechLinks.OriginOf("quartz"));
        Assert.Contains("crusher", Link("quartz").MadeAt);
        Assert.Contains("quartz", Link("crusher").UsedIn);
        Assert.Equal(Origin.Free, TechLinks.OriginOf("suit"));
        // "Made from nothing, at the lifepod": no other station makes a craft of no parts.
        Assert.All(Crafts.All.Where(c => c.Cost.Count == 0), c => Assert.Equal(["lifepod"], Stations.All.Where(s => s.Rows.Any(r => r.Contains(c.Item))).Select(s => s.Machine)));
        Assert.Equal(Origin.Made, TechLinks.OriginOf("wire"));
        foreach (var item in Content.Items.All.Where(i => TechLinks.OriginOf(i.Id) == Origin.Found))
        {
            Assert.Empty(Link(item.Id).MadeAt);
            Assert.False(Crafts.TryGet(item.Id, out _), item.Id);
        }
    }
}
