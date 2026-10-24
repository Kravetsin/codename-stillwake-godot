using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// Plans and ghosts of doorways (0104, 0111): planned cells join as built ones do, over planned and built cells together,
/// each by the code it is planned as, so a planned door stays a door and a planned hatch a hatch.
/// </summary>
public class PlannedDoorTests
{
    private const int W = 12;
    private static int At(int x, int y) => y * W + x;

    /// <summary>A room a cell a character, a hatch built in its top row at (3, 1).</summary>
    private static readonly IGridQueries Grid = new TestWorld(Layout.Parse(
    [
        "############",
        "#..hh......#",
        "#..........#",
        "#..........#",
        "#..........#",
        "#..........#",
        "#..........#",
        "#..........#",
        "#..........#",
        "############",
    ])).M.Grid;

    private static Dictionary<int, int> Codes(int code, params int[] cells) => cells.ToDictionary(c => c, _ => code);

    private static DoorShape.Spot SpotOf(int cell, Dictionary<int, int> planned, bool hatch) =>
        DoorShape.SpotOf(cell, W, Tools.DoorLinks(Grid, planned), hatch);

    private static int[] Box(int x, int y, int across, int tall) =>
        [.. from dy in Enumerable.Range(0, tall) from dx in Enumerable.Range(0, across) select At(x + dx, y + dy)];

    [Fact]
    public void ALonePlannedHatchIsDrawnAHatchAlone()
    {
        var planned = Codes(Structure.HatchClosed, Box(6, 4, 2, 1));
        var spot = SpotOf(At(6, 4), planned, hatch: true);
        Assert.Equal(new DoorShape.Spot(Hatch: true, Along: 0, Length: 2, Across: 0), spot);
        Assert.Equal(("hatch_closed_0", (0, 0)), DoorShape.TileOf(spot, closed: true));
    }

    [Fact]
    public void HatchesPlannedSideBySideStayDoorwaysOfTheirOwn()
    {
        var planned = Codes(Structure.HatchClosed, Box(4, 4, 6, 1));
        Assert.Equal(new DoorShape.Spot(true, 0, 2, 0), SpotOf(At(4, 4), planned, true));
        Assert.Equal(new DoorShape.Spot(true, 1, 2, 0), SpotOf(At(5, 4), planned, true));
        Assert.Equal(new DoorShape.Spot(true, 0, 2, 0), SpotOf(At(6, 4), planned, true));
        Assert.Equal("hatch_closed_0", DoorShape.TileOf(SpotOf(At(4, 4), planned, true), closed: true).Picture);
        Assert.Equal("hatch_closed_0", DoorShape.TileOf(SpotOf(At(5, 4), planned, true), closed: true).Picture);
    }

    [Fact]
    public void PlannedDoorSegmentsDownJoinIntoADoor()
    {
        var planned = Codes(Structure.DoorClosed, Box(6, 3, 2, 4));
        Assert.Equal(new DoorShape.Spot(Hatch: false, Along: 3, Length: 4, Across: 1), SpotOf(At(7, 6), planned, hatch: false));
        Assert.Equal("door_closed_1", DoorShape.TileOf(SpotOf(At(7, 6), planned, false), closed: true).Picture);
    }

    [Fact]
    public void APlanBesideABuiltHatchStaysItsOwnDoorway()
    {
        var planned = Codes(Structure.HatchClosed, Box(5, 1, 2, 1));
        Assert.Equal(new DoorShape.Spot(true, 0, 2, 0), SpotOf(At(5, 1), planned, true));
        Assert.Equal(new DoorShape.Spot(true, 1, 2, 0), SpotOf(At(6, 1), planned, true));
        // The built hatch keeps its own pair, not one run of four.
        Assert.Equal(new DoorShape.Spot(true, 0, 2, 0), SpotOf(At(3, 1), planned, true));
        Assert.Equal(new DoorShape.Spot(true, 1, 2, 0), SpotOf(At(4, 1), planned, true));
    }

    [Fact]
    public void PlannedDoorsAndHatchesKeepTheirKinds()
    {
        var planned = Codes(Structure.DoorClosed, Box(6, 3, 2, 4));
        foreach (int c in Box(6, 7, 2, 1)) planned[c] = Structure.HatchClosed;
        // The hatch under the door's last row joins neither the door nor a hatch beyond its pair.
        Assert.Equal(new DoorShape.Spot(Hatch: false, Along: 3, Length: 4, Across: 1), SpotOf(At(7, 6), planned, hatch: false));
        Assert.Equal(new DoorShape.Spot(true, 0, 2, 0), SpotOf(At(6, 7), planned, hatch: true));
    }
}
