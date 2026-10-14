using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>From packages/sim/src/modules/grid/grid.test.ts: what walls and back walls are made of, as atmosphere reads it.</summary>
public class GridTests
{
    /// <summary>"backs a cell in a material, says which", and the material of "places every layer where it fits".</summary>
    [Fact]
    public void SaysWhatAWallAndABackWallAreMadeOf()
    {
        var w = new Scenario("#..\n...");
        Assert.Equal("scrap", w.M.Grid.WallAt(0)?.Id);
        Assert.Equal("scrap", w.M.Grid.BackAt(1)?.Id);
        Assert.Null(w.M.Grid.WallAt(1));
        w.Do(new ClearAt(1, "back")).Do(new Place(2, Placement.Wall("iron"))).Run();
        Assert.Null(w.M.Grid.BackAt(1));
        Assert.Equal("iron", w.M.Grid.WallAt(2)?.Id);
        w.Do(new Place(1, Placement.BackWall("gold"))).Do(new Place(1, Placement.BackWall("iron"))).Run();
        Assert.Equal(new[] { "grid.noSuchMaterial" }, w.Refusals.Select(r => r.Reason));
        Assert.Equal("iron", w.M.Grid.BackAt(1)?.Id);
        Assert.Null(w.M.Grid.WallAt(-1));
        Assert.Null(w.M.Grid.BackAt(6));
    }
}

/// <summary>From grid.test.ts: "toggles a client has in flight", folded by <c>AfterToggles</c> (0014).</summary>
public class GridAfterTogglesTests
{
    [Fact]
    public void ShowTheLayersAsTheirCommandsWouldLeaveThemSkippingWhatTheyWouldRefuse()
    {
        var w = new Scenario("#D#v\n#D#-");
        Command[] toggles = [new ToggleDoor(5), new ToggleValve(3), new ToggleDoor(0), new ToggleValve(7)];
        var (structure, conduit) = w.M.Grid.AfterToggles(toggles);
        foreach (var t in toggles) w.Do(t);
        w.Run();
        Assert.Equal(new[] { "grid.noDoor", "grid.noValve" }, w.Refusals.Select(r => r.Reason));
        Assert.Equal(w.M.Grid.StructureLayer.ToArray(), structure);
        Assert.Equal(w.M.Grid.ConduitLayer.ToArray(), conduit);
    }

    [Fact]
    public void FoldInOrderSoEachToggleSeesTheOnesBeforeItAndTheWorldIsLeftBe()
    {
        var w = new Scenario("#d#\n#D#");
        ulong hash = w.World.Hash();
        int[] live = w.M.Grid.StructureLayer.ToArray();
        var (structure, _) = w.M.Grid.AfterToggles([new ToggleDoor(1), new ToggleDoor(4)]);
        // A mixed doorway: the first closes it all, as its open cell, and the second opens it all.
        Assert.Equal(new[] { live[1], live[1] }, new[] { structure[1], structure[4] });
        Assert.Equal(hash, w.World.Hash());
        Assert.Equal(live, w.M.Grid.StructureLayer.ToArray());
    }
}
