using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// Plans and ghosts of doorways (0104): their segments join as built ones do, over planned and built cells together, so a
/// lone segment and a run across look a hatch and a run down a door before anything is built.
/// </summary>
public class PlannedDoorTests
{
    private const int W = 12;
    private static int At(int x, int y) => y * W + x;

    /// <summary>A room a cell a character, a hatch segment built in its top row at (3, 1).</summary>
    private static readonly IGridQueries Grid = new TestWorld(Layout.Parse(
    [
        "############",
        "#..DD......#",
        "#..........#",
        "#..........#",
        "#..........#",
        "#..........#",
        "#..........#",
        "#..........#",
        "#..........#",
        "############",
    ])).M.Grid;

    private static DoorShape.Spot SpotOf(int cell, params int[] planned) =>
        DoorShape.SpotOf(cell, W, Tools.DoorLinks(Grid, planned.ToHashSet()));

    private static int[] Box(int x, int y, int across, int tall) =>
        [.. from dy in Enumerable.Range(0, tall) from dx in Enumerable.Range(0, across) select At(x + dx, y + dy)];

    [Fact]
    public void ALonePlannedHatchSegmentIsDrawnAHatchAlone()
    {
        var planned = Box(6, 4, 2, 1);
        var spot = SpotOf(At(6, 4), planned);
        Assert.Equal(new DoorShape.Spot(Hatch: true, Along: 0, Length: 2, Across: 0), spot);
        Assert.Equal(("hatch_closed_0", (0, 0)), DoorShape.TileOf(spot, closed: true));
    }

    [Fact]
    public void ThreePlannedSegmentsSideBySideJoinIntoOneHatch()
    {
        var planned = Box(4, 4, 6, 1);
        Assert.Equal(new DoorShape.Spot(true, 2, 6, 0), SpotOf(At(6, 4), planned));
        Assert.Equal("hatch_closed_10", DoorShape.TileOf(SpotOf(At(6, 4), planned), closed: true).Picture);
        // Three door segments side by side are a hatch two rows thick, one doorway too.
        var thick = Box(4, 4, 6, 2);
        Assert.Equal(new DoorShape.Spot(true, 5, 6, 1), SpotOf(At(9, 5), thick));
    }

    [Fact]
    public void PlannedDoorSegmentsDownJoinIntoADoor()
    {
        var planned = Box(6, 3, 2, 4);
        Assert.Equal(new DoorShape.Spot(Hatch: false, Along: 3, Length: 4, Across: 1), SpotOf(At(7, 6), planned));
        Assert.Equal("door_closed_1", DoorShape.TileOf(SpotOf(At(7, 6), planned), closed: true).Picture);
    }

    [Fact]
    public void APlanBesideABuiltHatchJoinsIt()
    {
        var planned = Box(5, 1, 2, 1);
        Assert.Equal(new DoorShape.Spot(true, 3, 4, 0), SpotOf(At(6, 1), planned));
        Assert.Equal(new DoorShape.Spot(true, 0, 4, 0), SpotOf(At(3, 1), planned));
    }
}
