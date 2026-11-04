using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The pointer over a machine's risen picture, built or planned (<see cref="Risen"/>).</summary>
public class RisenTests
{
    private static int Rise(string kind) => kind == "arcBench" ? 3 : 0;

    [Fact]
    public void MovesThePointerOverARisenPictureIntoItsFootprintsTopRow()
    {
        // A built bench on the left, a planned one on the right, a crate between: rows 6-7 of a room nine rows deep.
        var world = new TestWorld(Layout.Parse("""
            ##########
            #........#
            #........#
            #WW.KK...#
            ##########
            """).Doubled());
        int width = world.M.Grid.Width;
        world.Step(new PlacePlan(6 * width + 12, new Placement("machine", "arcBench")));
        var (grid, building) = (world.M.Grid, world.M.Building);
        Assert.Equal((3.5, 6.5), Risen.Over(grid, building, (3.5, 3.2), Rise));
        Assert.Equal((13.5, 6.5), Risen.Over(grid, building, (13.5, 5.9), Rise));
        // Past the rise, over a crate that rises none, and in the footprint itself: as it is.
        Assert.Equal((3.5, 2.2), Risen.Over(grid, building, (3.5, 2.2), Rise));
        Assert.Equal((8.5, 5.5), Risen.Over(grid, building, (8.5, 5.5), Rise));
        Assert.Equal((13.5, 7.5), Risen.Over(grid, building, (13.5, 7.5), Rise));
        Assert.Null(Risen.Under(grid, building, 5 * width + 10, Rise));
    }
}
