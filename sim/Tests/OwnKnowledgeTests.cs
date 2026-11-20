using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Each player learns for themself (own-knowledge): an item is found once it has been in their inventory, by any road, and what it
/// makes understood is known to them alone; the market sells to its buyer, a ship's machine goes by what anyone bought.
/// </summary>
public class OwnKnowledgeTests
{
    /// <summary>A room with a crate, knowing nothing at the start.</summary>
    private const string Room = """
        ##########
        #........#
        #......KK#
        ##########
        """;

    /// <summary>The room with peers 0 and 1 aboard, paired, knowing nothing.</summary>
    private static Scenario Two()
    {
        var w = new Scenario(Layout.Parse(Room) with { Known = [] }, doubled: true);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new Enter(w.M.Crew.Spawn) { From = 1 }).Run();
        return w;
    }

    private static int Crate(Scenario w) => w.M.Grid.MachinesOf("crate")[0].Id;

    [Fact]
    public void WhatOneHoldsIsTheirFindAndTheirBlueprintAlone()
    {
        var w = Two().GiveItems(0, ("scrapChunk", 1), ("hullPlate", 1));
        w.Run();
        Assert.True(w.M.Knowledge.HasFound(0, "hullPlate"));
        Assert.False(w.M.Knowledge.HasFound(1, "hullPlate"));
        Assert.True(w.M.Research.Knows(0, "furnace"));
        Assert.False(w.M.Research.Knows(1, "furnace"));
        Assert.True(w.M.Research.KnowsAny("furnace"));
        Assert.Equal("building.unknown", w.M.Building.Problem(w.At(1, 1), new Placement("machine", "furnace"), 1));
    }

    [Fact]
    public void ACratesStackIsNoOnesFindTillTakenThenTheTakersAlone()
    {
        var w = Two();
        int crate = Crate(w);
        w.FillCrate(crate, ("scrapChunk", 5), ("hullPlate", 2));
        w.Run(3);
        Assert.False(w.M.Knowledge.HasFound(0, "hullPlate"));
        Assert.False(w.M.Knowledge.HasFound(1, "hullPlate"));
        int at = w.M.Items.Crate(crate).ToList().FindIndex(s => s?.Item == "hullPlate");
        w.Do(new Move(new Items.Place(at, crate), new Target(), 1) { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Knowledge.HasFound(1, "hullPlate"));
        Assert.False(w.M.Knowledge.HasFound(0, "hullPlate"));
        Assert.Equal("hullPlate", w.M.Knowledge.Found(1)[^1]);
    }

    [Fact]
    public void ABagUpgradeTheCrewBoughtOpensEveryonesRows()
    {
        var w = Two();
        var first = new Items.Place(Inventory.Start);
        Assert.False(w.M.Items.SlotOpen(1, first));
        w.M.Knowledge.Grant("bag1");
        Assert.True(w.M.Items.SlotOpen(0, first));
        Assert.True(w.M.Items.SlotOpen(1, first));
    }

    [Fact]
    public void AFriendJoiningLateKnowsTheStartAlone()
    {
        var w = new Scenario(Layout.Parse(Room) with { Known = ["wallScrap"] }, doubled: true);
        w.Do(new Enter(w.M.Crew.Spawn)).Run().GiveItems(0, ("scrapChunk", 1), ("hullPlate", 1)).Run(50);
        w.Do(new Enter(w.M.Crew.Spawn) { From = 3 }).Run();
        Assert.True(w.M.Research.Knows(3, "wallScrap"));
        Assert.False(w.M.Research.Knows(3, "furnace"));
        Assert.True(w.M.Research.Knows(0, "furnace"));
    }

    [Fact]
    public void AFindOutlivesASaveAndLoad()
    {
        var w = Two().GiveItems(1, ("scrapChunk", 1), ("hullPlate", 1));
        w.Run();
        var loaded = w.Reload();
        Assert.True(loaded.M.Knowledge.HasFound(1, "hullPlate"));
        Assert.True(loaded.M.Research.Knows(1, "furnace"));
        Assert.False(loaded.M.Research.Knows(0, "furnace"));
        Assert.Equal(w.Run().World.Hash(), loaded.World.Hash());
    }
}
