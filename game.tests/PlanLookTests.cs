using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How plans are drawn: walls joined as built ones are, and the dashes round each group of plans.</summary>
public class PlanLookTests
{
    private const int Cell = 16;

    [Fact]
    public void APlannedWallJoinsPlannedAndBuiltWallsOfAnyMaterialButNothingElse()
    {
        // Built: a wall at 1, a door at 6, a floor at 9, beside the planned wall at 5 (4 wide).
        var grid = new TestWorld("""
            .#..
            ..D.
            .=..
            """).M.Grid;
        var wall = Placement.Wall("iron");
        IEnumerable<Placement> Planned(int c) => c == 4 ? [Placement.Wall("scrap")] : [];
        Assert.Equal(Blueprint.Up | Blueprint.Left, Tools.LinksOf(wall, 5, 4, Planned, grid));
        // A row's end never joins the next row's start.
        Assert.Equal(0, Tools.LinksOf(wall, 8, 4, c => c == 7 ? [wall] : [], grid));
    }

    [Fact]
    public void APlannedPipeJoinsBuiltPipesAndAMachineJoinsNothing()
    {
        var grid = new TestWorld("""
            .-..
            ..#.
            """).M.Grid;
        Assert.Equal(Blueprint.Up, Tools.LinksOf(new Placement("conduit", "pipe"), 5, 4, _ => [], grid));
        Assert.Equal(Blueprint.Up | Blueprint.Right, Tools.LinksOf(new Placement("conduit", "valve"), 5, 4, c => c == 6 ? [new Placement("conduit", "pipe")] : [], grid));
        Assert.Equal(0, Tools.LinksOf(new Placement("machine", "vent"), 5, 4, c => [new Placement("machine", "vent")], grid));
    }

    [Fact]
    public void ABuiltPipeBesidePlannedOnesReachesTowardThemButNeverRoundARowsEnd()
    {
        var grid = new TestWorld("""
            .-.-
            ....
            """).M.Grid;
        bool Built(int c) => grid.ConduitAt(c) != Conduit.None;
        Assert.Equal([(1, Blueprint.Right | Blueprint.Down), (3, Blueprint.Left)], Tools.StubsOf(new HashSet<int> { 2, 5 }, 4, Built).OrderBy(s => s.Cell));
        // Cell 4 starts the row under the pipe at 3, which ends its own: no stub.
        Assert.Empty(Tools.StubsOf(new HashSet<int> { 4 }, 4, Built));
        // What reaches is the caller's to say: a ghost's reach takes planned pipes too.
        Assert.Equal([(6, Blueprint.Left)], Tools.StubsOf(new HashSet<int> { 5 }, 4, c => c == 6));
    }

    [Fact]
    public void OutlinesARowOfWallsAsOneBoxClockwiseAPixelInsideItsEdge()
    {
        var runs = PlanOutline.Of([(1, "wall", Cell), (2, "wall", Cell), (3, "wall", Cell)], 8, Cell);
        Assert.Equal(8, runs.Count);
        Assert.Contains(new OutlineRun(16, 0, 16, 1, 0), runs);      // the top, rightward
        Assert.Contains(new OutlineRun(63, 0, 16, 0, 1), runs);      // the right end, downward
        Assert.Contains(new OutlineRun(63, 15, 16, -1, 0), runs);    // the bottom, leftward
        Assert.Contains(new OutlineRun(16, 15, 16, 0, -1), runs);    // the left end, upward
        Assert.DoesNotContain(runs, r => r.Dy != 0 && r.X is 31 or 32 or 47 or 48);
    }

    [Fact]
    public void FloorsJoinOnlySideBySideAlongTheirStripAndGroupsNeverJoinEachOther()
    {
        // Two floors side by side, a third under the first: two boxes, four pixels tall.
        var floors = PlanOutline.Of([(0, "floor", 4), (1, "floor", 4), (8, "floor", 4)], 8, Cell);
        Assert.Equal(6 + 4, floors.Count);
        Assert.Contains(new OutlineRun(31, 3, 16, -1, 0), floors);
        Assert.Contains(new OutlineRun(31, 0, 4, 0, 1), floors);
        // Two machines side by side, each its own box; a cell at a row's end, not joined to the next row's start.
        Assert.Equal(8, PlanOutline.Of([(0, "machine1", Cell), (1, "machine2", Cell)], 8, Cell).Count);
        Assert.Equal(8, PlanOutline.Of([(7, "wall", Cell), (8, "wall", Cell)], 8, Cell).Count);
    }

    [Fact]
    public void DashesMarchAlongEachRunsWayTheSameOnEveryRunOfALine()
    {
        Assert.Equal(new[] { (0, 0, 3, 1), (5, 0, 3, 1) }, Dashes(new OutlineRun(0, 0, 10, 1, 0), 0));
        // Moved a pixel along: a rightward run's dashes step right.
        Assert.Equal(new[] { (1, 0, 3, 1), (6, 0, 3, 1) }, Dashes(new OutlineRun(0, 0, 10, 1, 0), 1));
        // A leftward run's step left.
        Assert.Equal(new[] { (7, 9, 3, 1), (2, 9, 3, 1) }, Dashes(new OutlineRun(9, 9, 10, -1, 0), 1));
        Assert.Equal(new[] { (4, 3, 1, 2) }, Dashes(new OutlineRun(4, 4, 3, 0, -1), 0));

        static (int, int, int, int)[] Dashes(OutlineRun run, double offset) =>
            PlanOutline.Dashes(run, offset, 3, 5).Select(d => (d.X, d.Y, d.W, d.H)).ToArray();
    }

    [Fact]
    public void APlanBeingBuiltTurnsRealFromTheBottomAsItIsWorkedAndAFloorOrAConduitFromTheLeft()
    {
        // A wall at cell (2, 3) half worked: its bottom half real, its top half a plan.
        var wall = PlanCut.Of((2 * Cell, 3 * Cell, Cell, Cell), PlanCut.FromLeftFor("structure", "wall"), 1, 2);
        Assert.Equal(((32, 56, 16, 8), (32, 48, 16, 8)), wall.Split((32, 48, 16, 16)));
        // A scrap wall of 13 ticks fills tick by tick, a row or two at a time, never back: no part reveals itself whole.
        var rows = Enumerable.Range(0, 14).Select(t => Height(PlanCut.Of((0, 0, Cell, Cell), false, t, 13), 0)).ToArray();
        Assert.Equal([0, 1, 2, 3, 4, 6, 7, 8, 9, 11, 12, 13, 14, 16], rows);
        // No work leaves it all a plan; all of it, all real.
        Assert.Equal((0, 16), (Height(PlanCut.Of((0, 0, Cell, Cell), false, 0, 2), 0), Height(PlanCut.Of((0, 0, Cell, Cell), false, 2, 2), 0)));
        // A tank two cells square, 5 parts, 3 in: 32 * 3 / 5 = 19 rows real, rounded down, in the lower cell 16 and the upper 3.
        var tank = PlanCut.Of((0, 0, 2 * Cell, 2 * Cell), false, 3, 5);
        Assert.Equal(19, Height(tank, 0) + Height(tank, Cell));
        Assert.Equal((3, 16), (Height(tank, 0), Height(tank, Cell)));
        // A floor and a valve, half in, real on their left.
        Assert.True(PlanCut.FromLeftFor("structure", "floor") && PlanCut.FromLeftFor("conduit", "valve"));
        Assert.False(PlanCut.FromLeftFor("back", "backWall") || PlanCut.FromLeftFor("machine", "vent"));
        var floor = PlanCut.Of((Cell, 0, Cell, Cell), true, 1, 2);
        Assert.Equal(((16, 0, 8, 4), (24, 0, 8, 4)), floor.Split((16, 0, 16, 4)));

        static int Height(PlanCut cut, int y) => cut.Split((0, y, 2 * Cell, Cell)).Real.H;
    }
}
