using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The build menu laid out from content (0114): its tabs and cards in the order the rows' menu slots give.</summary>
public class BuildMenuDataTests
{
    private static string Ids(Tab tab) => $"{tab.Id}: {string.Join(" ", tab.Tools.Select(t => t.Id))}";

    [Fact]
    public void KeepsTheMenusOrderTabByTabAndCardByCard()
    {
        Assert.Equal(
        [
            "structure: wallScrap wallIron wallSoil wallStone wallAlloy backWallScrap backWallIron backWallSoil backWallStone "
                + "backWallAlloy floor ladder door hatch",
            "air: vent tank generator",
            "power: solarPanel burnerGenerator lamp arcLamp charger capacitorBank",
            "production: furnace workbench analyzer former crusher smelter assembler growCapsule arcBench",
            "storage: crate droneBay",
            "ship: computer radarAntenna launchPlatform dropPod engine",
        ], Tools.Tabs.Select(Ids));
    }

    [Fact]
    public void ShowsTheShipsTabWithNoCardButNoOtherTabWithNone()
    {
        Assert.True(Tools.Shown(new Tab(MenuTabs.Ship, [])));
        Assert.False(Tools.Shown(new Tab(MenuTabs.Storage, [])));
        Assert.True(Tools.Shown(Tools.Tabs.Single(t => t.Id == MenuTabs.Storage)));
    }

    [Fact]
    public void AMachineRowWithAMenuSlotIsACardWhereItsOrderPutsItWithNoCode()
    {
        var press = new MachineSpec("press", 4, 4, [], [("frame", 1)], BuildTicks: 100, Draws: 2, Menu: new(MenuTabs.Power, 45));
        var shed = new MachineSpec("shed", 2, 2, [], [("scrap", 1)], BuildTicks: 10, Menu: new(MenuTabs.Storage, 1));
        var unlisted = new MachineSpec("unlisted", 2, 2, [], [("scrap", 1)], BuildTicks: 10);
        var tabs = Tools.TabsOf(Materials.All, Builds.All, [.. Machines.All, press, shed, unlisted]);

        Assert.Equal("power: solarPanel burnerGenerator lamp arcLamp press charger capacitorBank", Ids(tabs.Single(t => t.Id == "power")));
        Assert.Equal("storage: shed crate droneBay", Ids(tabs.Single(t => t.Id == "storage")));
        Assert.DoesNotContain(tabs.SelectMany(t => t.Tools), t => t.Id == "unlisted");
        var card = Assert.IsType<PlaceTool>(tabs.SelectMany(t => t.Tools).Single(t => t.Id == "press"));
        Assert.Equal(new Sim.Grid.Placement("machine", "press"), card.Placement);
        // The menu's own tabs are untouched by the fixture.
        Assert.DoesNotContain(Tools.All, t => t.Id is "press" or "shed");
    }

    [Fact]
    public void EverySlotNamesATabAndEveryBuiltThingHasOne()
    {
        var slots = Machines.All.Select(m => m.Menu).Concat(Builds.All.Select(b => b.Menu))
            .Concat(Materials.All.SelectMany(m => new[] { m.Menu, m.BackMenu })).OfType<MenuSlot>();
        Assert.All(slots, s => Assert.Contains(s.Tab, MenuTabs.All));
        Assert.All(Machines.All, m => Assert.Equal(!m.Fixed, m.Menu != null));
        Assert.All(Builds.All, b => Assert.NotNull(b.Menu));
        Assert.All(Materials.All, m => Assert.Equal(m.Built, m.Menu != null && m.BackMenu != null));
        Assert.All(Materials.All, m => Assert.Equal(m.Menu == null, m.BackMenu == null));
    }
}
