using Stillwake.Game.Features.Building;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The brush: the square of cells a strike or a build stroke covers round the cell corner nearest the pointer.</summary>
public class BrushTests
{
    private const int W = 10, H = 6;
    private static readonly PlaceTool Wall = new("wallIron", Placement.Wall("iron"), "wall");
    private static readonly PlaceTool BackWall = new("backWallScrap", Placement.BackWall(), "backWall");
    private static readonly PlaceTool Floor = new("floor", new Placement("structure", "floor"), "floor");
    private static readonly PlaceTool Ladder = new("ladder", new Placement("structure", "ladder"), "ladder");
    private static readonly PlaceTool Door = new("door", new Placement("structure", "door"), "door");
    private static readonly PlaceTool Pipe = new("pipe", new Placement("conduit", "pipe"), "pipe");
    private static readonly PlaceTool Tank = new("tank", new Placement("machine", "tank"), "tank");
    private static readonly Remove Removal = new(["machine", "conduit", "structure", "back"]);

    private static BuildingModule Building() => new TestWorld(Layout.Parse(["..........", "..........", "..........", "..........", "..........", ".........."])).M.Building;

    [Fact]
    public void AnEvenBrushIsRoundTheCellCornerNearestThePointerAnOddOneCentredOnItsCell()
    {
        Assert.Equal((4, 2), Brush.Corner(W, H, (4.7, 3.2), 2, 2));
        Assert.Equal((3, 3), Brush.Corner(W, H, (4.2, 3.8), 2, 2));
        Assert.Equal((4, 3), Brush.Corner(W, H, (4.7, 3.2), 1, 1));
        Assert.Equal((3, 2), Brush.Corner(W, H, (4.7, 3.2), 3, 3));
        // A floor's stamp, two cells by one, is round the nearest column line, in the pointer's row.
        Assert.Equal((4, 3), Brush.Corner(W, H, (4.7, 3.2), 2, 1));
    }

    [Fact]
    public void TheBrushStaysOnTheGrid()
    {
        Assert.Equal((0, 0), Brush.Corner(W, H, (0.2, 0.2), 2, 2));
        Assert.Equal((8, 4), Brush.Corner(W, H, (9.9, 5.9), 2, 2));
        Assert.Equal([0, 1, 10, 11], Brush.Cells(W, H, (0.2, 0.2), 2));
    }

    [Fact]
    public void ItsCellsAreAscendingAndPreciseIsOneCell()
    {
        Assert.Equal([24, 25, 34, 35], Brush.Cells(W, H, (4.7, 3.2), 2));
        Assert.Equal([34], Brush.Cells(W, H, (4.7, 3.2), Brush.SideOf(precise: true)));
        Assert.Equal(Stillwake.Content.Builds.Brush, Brush.SideOf(precise: false));
    }

    [Fact]
    public void WallsBackWallsAndLaddersTakeTheSquareAFloorItsTopRowTheRestOneCell()
    {
        Assert.Equal((2, 2), Drag.StampOf(Wall, 2));
        Assert.Equal((2, 2), Drag.StampOf(BackWall, 2));
        Assert.Equal((2, 2), Drag.StampOf(Ladder, 2));
        Assert.Equal((2, 1), Drag.StampOf(Floor, 2));
        foreach (Act act in (Act[])[Door, Pipe, Tank, Removal]) Assert.Equal((1, 1), Drag.StampOf(act, 2));
        foreach (Act act in (Act[])[Wall, Ladder, Floor]) Assert.Equal((1, 1), Drag.StampOf(act, 1));
    }

    [Fact]
    public void ALadderDraggedDownIsTwoCellsWideEachCellOnce()
    {
        var cells = Drag.StrokeOf(W, H, Ladder, (4.9, 1.9), (4.9, 4.1), Building(), side: 2);
        Assert.Equal([14, 15, 24, 25, 34, 35, 44, 45], cells);
    }

    [Fact]
    public void AFloorDraggedAcrossIsOneRowThePointersAndPreciseIsOneCell()
    {
        Assert.Equal([12, 13, 14], Drag.StrokeOf(W, H, Floor, (2.6, 1.5), (4.4, 1.2), Building(), side: 2));
        Assert.Equal([14], Drag.StrokeOf(W, H, Ladder, (4.9, 1.9), (4.9, 1.9), Building(), side: 1));
    }

    [Fact]
    public void AWallRectangleTakesBothEndsSquaresRowByRowFromWhereItBegan()
    {
        var cells = Drag.StrokeOf(W, H, Wall, (2.9, 1.9), (5.1, 3.1), Building(), side: 2);
        Assert.Equal([12, 13, 14, 15, 22, 23, 24, 25, 32, 33, 34, 35], cells);
        var back = Drag.StrokeOf(W, H, Wall, (5.1, 3.1), (2.9, 1.9), Building(), side: 2);
        Assert.Equal([35, 34, 33, 32, 25, 24, 23, 22, 15, 14, 13, 12], back);
        // A click, not dragged: one square.
        Assert.Equal([12, 13, 22, 23], Drag.StrokeOf(W, H, Wall, (2.9, 1.9), (2.9, 1.9), Building(), side: 2));
    }

    [Fact]
    public void AMachineADoorAndAPipeArePlacedInTheCellUnderThePointerOnce()
    {
        Assert.Equal([12], Drag.StrokeOf(W, H, Tank, (2.9, 1.9), (7.2, 1.4), Building(), side: 2));
        Assert.Equal([12, 22, 32], Drag.StrokeOf(W, H, Door, (2.9, 1.9), (2.9, 3.4), Building(), side: 2));
        Assert.Equal([12, 13, 14], Drag.StrokeOf(W, H, Pipe, (2.9, 1.9), (4.1, 1.4), Building(), side: 2));
    }
}
