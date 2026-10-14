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
