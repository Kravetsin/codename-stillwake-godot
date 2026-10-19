using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How plans are drawn: walls joined as built ones are, the dashes along the silhouette of each group of plans, and the cut.</summary>
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
    public void ABuiltPipesStubIsItsArmsTowardThePlansOnlyLeavingItsJointToTheGrid()
    {
        // The joint of pipe_0, 6 pixels square in the cell's middle; a built pipe's joint drawn as a ghost made it look unbuilt.
        var joint = (X: 5, Y: 5, W: 6, H: 6);
        Assert.Equal([(5, 11, 6, 5)], Blueprint.ArmsOf(Blueprint.Down, joint, Cell));
        Assert.Equal([(5, 0, 6, 5), (11, 5, 5, 6), (5, 11, 6, 5), (0, 5, 5, 6)], Blueprint.ArmsOf(15, joint, Cell));
        Assert.Empty(Blueprint.ArmsOf(0, joint, Cell));
        foreach (var arm in Blueprint.ArmsOf(15, joint, Cell))
            Assert.False(arm.X < joint.X + joint.W && joint.X < arm.X + arm.W && arm.Y < joint.Y + joint.H && joint.Y < arm.Y + arm.H);
    }

    /// <summary>A mask from rows drawn with '#' for an opaque pixel.</summary>
    private static List<Rim> RimOf(params string[] rows) =>
        PlanOutline.RimOf((x, y) => rows[y][x] == '#', rows[0].Length, rows.Length);

    private static readonly IReadOnlyList<Rim> Square = PlanOutline.RimOf((_, _) => true, Cell, Cell);

    [Fact]
    public void ARimStepsClockwiseAlongEverySideOfAnOpaquePixelFacingAClearOneOrTheEdge()
    {
        Assert.Equal([new Rim(1, 0, 1, 0), new Rim(2, 0, 0, 1), new Rim(2, 1, -1, 0), new Rim(1, 1, 0, -1)], RimOf(".#", ".."));
        // Two pixels side by side: their common side is no rim.
        Assert.Equal(6, RimOf("##").Count);
        // A ladder's rails and rungs: every pixel's sides that face the gaps too.
        Assert.Equal(7 * 4 - 6 * 2, RimOf("#.#", "###", "#.#").Count);
        Assert.Equal(4 * Cell, Square.Count);
    }

    [Fact]
    public void ACellOfWallIsOutlinedAlongItsSidesClockwiseEachToldItsCorner()
    {
        // Four sides of 16 from the top-left, every corner convex, counted on round it.
        Assert.Equal(
            [new OutlineRun(0, 0, 16, 1, 0, 0, true), new OutlineRun(16, 0, 16, 0, 1, 16, true), new OutlineRun(16, 16, 16, -1, 0, 32, true), new OutlineRun(0, 16, 16, 0, -1, 48, true)],
            PlanOutline.Runs(PlanOutline.Merge([(0, 0, Square)]), Cell * Cell));
        // A notch in the side: every side kept, a pixel long too; round its concave corners each starts where the one before ended.
        var notched = PlanOutline.Runs(PlanOutline.Merge([(0, 0, RimOf("###", "#..", "###"))]), 99);
        Assert.Equal(8, notched.Count);
        for (int i = 1; i < notched.Count; i++) Assert.Equal(notched[i - 1].Along + notched[i - 1].Length, notched[i].Along);
        Assert.Contains(new OutlineRun(3, 1, 2, -1, 0, 4, false), notched);
        Assert.Contains(new OutlineRun(1, 1, 1, 0, 1, 6, false), notched);
        Assert.Contains(new OutlineRun(1, 2, 2, 1, 0, 7, true), notched);
    }

    [Fact]
    public void TheDashesAreAStripAsWideAsTheLineOutsideEachSideFillingConvexCornersAndCountedOnRoundThem()
    {
        // At zoom 2, a line a screen pixel wide is half a world pixel: a strip on each side's left, outside the cell.
        var strips = PlanOutline.Strips(PlanOutline.Runs(PlanOutline.Merge([(0, 0, Square)]), Cell * Cell), 0.5);
        Assert.Equal(
            [new DashStrip(0, 0, 1, 0, 16.5, 0.5, 0), new DashStrip(16, 0, 0, 1, 16.5, 0.5, 16.5), new DashStrip(16, 16, -1, 0, 16.5, 0.5, 33), new DashStrip(0, 16, 0, -1, 16.5, 0.5, 49.5)],
            strips);
        // Round a concave corner a strip is not lengthened: the next starts where it meets it. A second loop counts afresh.
        var notched = PlanOutline.Strips(PlanOutline.Runs(PlanOutline.Merge([(0, 0, RimOf("###", "#..", "###"))]), 99), 0.25);
        Assert.Contains(new DashStrip(3, 1, -1, 0, 2, 0.25, 4.5), notched);
        Assert.Contains(new DashStrip(1, 1, 0, 1, 1, 0.25, 6.5), notched);
        var two = PlanOutline.Strips(PlanOutline.Runs(PlanOutline.Merge([(0, 0, RimOf("#.", ".#"))]), 99), 0.5);
        Assert.Equal(2, two.Count(s => s.Along == 0));
        Assert.Equal(3 + 1.5, two.Max(s => s.Along));
    }

    [Fact]
    public void ARowOfWallsIsOneOutlineTheSidesTheyShareDroppedAndGroupsNeverJoin()
    {
        var row = PlanOutline.Of([("wall", 16, 0, Square), ("wall", 32, 0, Square), ("wall", 48, 0, Square)], Cell * Cell);
        Assert.Equal(
            [new OutlineRun(16, 0, 48, 1, 0, 0, true), new OutlineRun(64, 0, 16, 0, 1, 48, true), new OutlineRun(64, 16, 48, -1, 0, 64, true), new OutlineRun(16, 16, 16, 0, -1, 112, true)],
            row);
        // Two machines side by side, each its own outline; a wall beside a door, too.
        Assert.Equal(8, PlanOutline.Of([("machine1", 0, 0, Square), ("machine2", 16, 0, Square)], Cell * Cell).Count);
        Assert.Equal(8, PlanOutline.Of([("wall", 0, 0, Square), ("door", 16, 0, Square)], Cell * Cell).Count);
        // Two floors side by side are one strip; one under them, a cell lower, is apart: their strips never touch.
        var floor = RimOf("####", "####");
        Assert.Equal(8, PlanOutline.Of([("floor", 0, 0, floor), ("floor", 4, 0, floor), ("floor", 0, 16, floor)], Cell * Cell).Count);
    }

    [Fact]
    public void AHoleSmallerThanACellIsPartOfTheSilhouetteAndARoomWalledRoundIsOutlinedInsideToo()
    {
        // A ladder's gaps between its rungs: only the outside is outlined.
        var ladder = PlanOutline.Runs(PlanOutline.Merge([(0, 0, RimOf("#..#", "####", "#..#", "#..#", "####", "#..#"))]), Cell * Cell);
        Assert.DoesNotContain(ladder, r => r.X is >= 1 and <= 3 && r.Y is >= 2 and <= 4);
        Assert.Contains(PlanOutline.Runs(PlanOutline.Merge([(0, 0, RimOf("#..#", "####", "#..#", "#..#", "####", "#..#"))]), 0), r => r.X is >= 1 and <= 3 && r.Y is >= 2 and <= 4);
        // Eight walls round an empty cell: the ring outside and the one inside the room.
        var walls = new List<(string, int, int, IReadOnlyList<Rim>)>();
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                if (x != 1 || y != 1) walls.Add(("wall", x * Cell, y * Cell, Square));
        var room = PlanOutline.Of(walls, Cell * Cell);
        Assert.Equal(8, room.Count);
        Assert.Equal(4 * 48 + 4 * 16, room.Sum(r => r.Length));
        Assert.Equal(4, room.Count(r => r.X is >= 16 and <= 32 && r.Y is >= 16 and <= 32));
    }

    [Fact]
    public void TwoPixelsTouchingOnlyAtACornerAreTwoLoops()
    {
        var runs = PlanOutline.Runs(PlanOutline.Merge([(0, 0, RimOf("#.", ".#"))]), 99);
        Assert.Equal(8, runs.Count);
        Assert.Equal(2, runs.Count(r => r.Along == 0));
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
