using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A drag and how it is judged: apps/game/src/features/building/drag.test.ts, one for one.</summary>
public class DragTests
{
    private static readonly PlaceTool Wall = new("wallIron", Placement.Wall("iron"), "wall");
    private static readonly PlaceTool Tank = new("tank", new Placement("machine", "tank"), "tank");
    private static readonly PlaceTool BackWall = new("backWallScrap", Placement.BackWall(), "backWall");
    private static readonly PlaceTool Floor = new("floor", new Placement("structure", "floor"), "floor");
    private static readonly Remove Removal = new(["machine", "conduit", "structure", "back"]);
    private const int W = 10;

    [Fact]
    public void LaysALineAlongTheLongerAxisEitherWay()
    {
        Assert.Equal([12, 13, 14, 15], Drag.LineOf(W, Floor, new(2, 1), new(5, 2)));
        Assert.Equal([15, 14, 13], Drag.LineOf(W, Floor, new(5, 1), new(3, 1)));
        Assert.Equal([12, 22, 32, 42], Drag.LineOf(W, Floor, new(2, 1), new(3, 4)));
        Assert.Equal([32, 22, 12], Drag.LineOf(W, Floor, new(2, 3), new(2, 1)));
    }

    [Fact]
    public void RunsAcrossOnATieAndIsOneCellWhenItDoesNotMove()
    {
        Assert.Equal([12, 13, 14], Drag.LineOf(W, Floor, new(2, 1), new(4, 3)));
        Assert.Equal([12], Drag.LineOf(W, Floor, new(2, 1), new(2, 1)));
    }

    [Fact]
    public void PlacesAMachineOnlyWhereItBegan() =>
        Assert.Equal([12], Drag.LineOf(W, Tank, new(2, 1), new(7, 1)));

    [Fact]
    public void FillsTheRectangleBetweenTheCornersWithWallsOrBackWallsRowByRowFromWhereItBegan()
    {
        Assert.Equal([12, 13, 14, 22, 23, 24, 32, 33, 34], Drag.LineOf(W, Wall, new(2, 1), new(4, 3)));
        Assert.Equal([34, 33, 32, 24, 23, 22, 14, 13, 12], Drag.LineOf(W, BackWall, new(4, 3), new(2, 1)));
        Assert.Equal([15, 14, 25, 24], Drag.LineOf(W, Wall, new(5, 1), new(4, 2)));
        // Dragged straight it is a line, as any card's; not moved, one cell.
        Assert.Equal([12, 13, 14, 15], Drag.LineOf(W, Wall, new(2, 1), new(5, 1)));
        Assert.Equal([12], Drag.LineOf(W, BackWall, new(2, 1), new(2, 1)));
    }

    [Fact]
    public void KeepsAWallRectangleWithinItsMostCellsEachWayFromWhereItBegan()
    {
        const int wide = 100, most = Drag.MostAcross;
        var cells = Drag.LineOf(wide, Wall, new(60, 60), new(0, 99));
        Assert.Equal(most * most, cells.Count);
        Assert.Equal((60 + most - 1) * wide + 60 - most + 1, cells[^1]);
        // A line of any other card is not cut short.
        Assert.Equal(61, Drag.LineOf(wide, Floor, new(60, 60), new(0, 60)).Count);
    }

    [Fact]
    public void StartsOnAPressOnTheGridAndEndsOnTheReleaseKeptToTheGrid()
    {
        var started = Drag.OnButton(null, new ButtonEvent(true, (2.5, 1.2)), 10, 5);
        Assert.Equal(((Spot?)new Spot(2, 1), ((Spot, Spot)?)null), started);
        var ended = Drag.OnButton(started.Pressed, new ButtonEvent(false, (14, -3)), 10, 5);
        Assert.Equal(((Spot?)null, ((Spot, Spot)?)(new Spot(2, 1), new Spot(9, 0))), ended);
    }

    [Fact]
    public void StartsNothingOnAPressOffTheGridAndAReleaseAloneEndsNothing()
    {
        Assert.Equal(((Spot?)null, ((Spot, Spot)?)null), Drag.OnButton(new Spot(1, 1), new ButtonEvent(true, (-0.5, 2)), 10, 5));
        Assert.Equal(((Spot?)null, ((Spot, Spot)?)null), Drag.OnButton(null, new ButtonEvent(false, (3, 2)), 10, 5));
    }

    // Drawn a character a cell, the tank its whole 4×4 (0104); twelve cells wide: cell (x, y) is y * 12 + x. Cell 17 is open,
    // with no back wall: nothing to take apart there.
    private static TestWorld Start() =>
        new(Layout.Parse(["############", "#TTTT -....#", "#TTTT......#", "#TTTT......#", "#TTTT......#", "############"]));

    private static PlacePlan Plan(int cell, Placement placement) => new(cell, placement);

    private static void Is(string? problem, Command[] commands, Verdict verdict)
    {
        Assert.Equal(problem, verdict.Problem);
        Assert.Equal(commands, verdict.Commands);
    }

    [Fact]
    public void SendsAPlanWhereTheSimWouldTakeItAndGivesTheSimsReasonWhereNot()
    {
        var w = Start();
        var verdicts = Drag.Judge(Wall, [5, 17, 0], w.M.Building);
        Assert.Equal(["grid.occupied", null, "grid.occupied"], verdicts.Select(v => v.Problem));
        Assert.Equal([Plan(17, Wall.Placement)], verdicts[1].Commands);
        Assert.Empty(verdicts[0].Commands);
    }

    [Fact]
    public void PlansAWallInEveryCellOfARectangleTheSimWouldTakeAndSaysWhyNotInTheRest()
    {
        var w = Start();
        // From the tank's right column across three cells of the room: the tank's two cells refused, the rest planned.
        var verdicts = Drag.Judge(Wall, Drag.LineOf(12, Wall, new(4, 1), new(7, 2)), w.M.Building);
        Assert.Equal([16, 17, 18, 19, 28, 29, 30, 31], verdicts.Select(v => v.Cell));
        Assert.Equal([16, 28], verdicts.Where(v => v.Problem != null).Select(v => v.Cell));
        Assert.Equal(
            new[] { 17, 18, 19, 29, 30, 31 }.Select(c => (Command)Plan(c, Wall.Placement)),
            verdicts.SelectMany(v => v.Commands));
    }

    [Fact]
    public void TakesAMachineFirstThenAConduitThenTheStructure()
    {
        var w = Start();
        Assert.Equal(
            [new Deconstruct(14, "machine"), new Deconstruct(18, "conduit"), new Deconstruct(0, "structure")],
            Drag.Judge(Removal, [14, 18, 0], w.M.Building).SelectMany(v => v.Commands));
        Assert.Equal("building.nothingThere", Drag.Judge(Removal, [17], w.M.Building)[0].Problem);
    }

    [Fact]
    public void TakesOnlyFromTheLayersItRemovesFrom()
    {
        var w = Start();
        var ship = new Remove(["machine", "structure", "back"]);
        var pipes = new Remove(["conduit"]);
        Assert.Equal([new Deconstruct(18, "back")], Drag.Judge(ship, [18], w.M.Building).SelectMany(v => v.Commands));
        var verdicts = Drag.Judge(pipes, [18, 0], w.M.Building);
        Is(null, [new Deconstruct(18, "conduit")], verdicts[0]);
        Is("building.nothingThere", [], verdicts[1]);
    }

    [Fact]
    public void SendsNothingWhereTheSimWouldRefuseAMark()
    {
        var w = Start();
        // The wall and the wall behind it, both marked: nothing left to mark there.
        w.Step(new Deconstruct(0, "structure"), new Deconstruct(0, "back"));
        var verdicts = Drag.Judge(Removal, [17, 0], w.M.Building);
        Is("building.nothingThere", [], verdicts[0]);
        Is("building.planned", [], verdicts[1]);
    }

    [Fact]
    public void MarksAMachineOnceHoweverManyOfItsCellsTheLineCrosses()
    {
        var w = Start();
        Assert.Equal([new Deconstruct(13, "machine")], Drag.Judge(Removal, [13, 14], w.M.Building).SelectMany(v => v.Commands));
    }

    [Fact]
    public void BegunOnAPlanCancelsEveryPlanInACellAMachineOnceHoweverManyCellsItCrosses()
    {
        var w = Start();
        w.Step(Plan(21, Wall.Placement), Plan(21, new Placement("conduit", "pipe")), Plan(17, new Placement("machine", "tank")));
        // Ids follow the order the plans were made: wall 1, pipe 2, tank 3 over (5, 1) to (8, 4).
        var verdicts = Drag.Judge(Removal, [21, 33, 18, 17, 29], w.M.Building);
        Assert.Equal([null, "building.noPlan", null, null, null], verdicts.Select(v => v.Problem));
        Assert.Equal([new CancelPlan(1), new CancelPlan(2), new CancelPlan(3)], verdicts.SelectMany(v => v.Commands));
    }

    [Fact]
    public void BegunOnAWallsBlueprintCancelsWallsAndBackWallsAcrossTheRectangleAndLetsOtherPlansBe()
    {
        var w = Start();
        // Wall 1 at 19, back wall 2 at 17, wall 3 at 31, pipe 4 at 30.
        w.Step(Plan(19, Wall.Placement), Plan(17, Placement.BackWall()), Plan(31, Wall.Placement), Plan(30, new Placement("conduit", "pipe")));
        var cells = Drag.CellsOf(12, Removal, new(7, 1), new(5, 2), w.M.Building);
        Assert.Equal([19, 18, 17, 31, 30, 29], cells);
        var verdicts = Drag.Judge(Removal, cells, w.M.Building);
        Assert.Equal([null, "building.noPlan", null, null, "building.noPlan", "building.noPlan"], verdicts.Select(v => v.Problem));
        Assert.Equal([new CancelPlan(1), new CancelPlan(2), new CancelPlan(3)], verdicts.SelectMany(v => v.Commands));
    }

    [Fact]
    public void RemovesAlongALineBegunOnAnyOtherPlanOnAMarkOrOnWhatIsBuilt()
    {
        var w = Start();
        w.Step(Plan(19, Wall.Placement), Plan(19, new Placement("conduit", "pipe")), Plan(30, new Placement("conduit", "pipe")),
            new Deconstruct(0, "structure"));
        // A wall's blueprint sharing its cell with a pipe's, a pipe's, a mark, what is built: each a line, as before.
        foreach (var from in new Spot[] { new(7, 1), new(6, 2), new(0, 0), new(2, 1) })
            Assert.Equal(Drag.LineOf(12, Removal, from, new(1, 2)), Drag.CellsOf(12, Removal, from, new(1, 2), w.M.Building));
        // Placing, as LineOf lays it.
        Assert.Equal(Drag.LineOf(12, Wall, new(7, 1), new(5, 2)), Drag.CellsOf(12, Wall, new(7, 1), new(5, 2), w.M.Building));
    }

    [Fact]
    public void BegunOnAMarkTakesTheMarkOffBegunOnWhatIsBuiltLeavesPlansBe()
    {
        var w = Start();
        w.Step(new Deconstruct(0, "structure"), Plan(19, Wall.Placement));
        // The mark is plan 1, the wall's blueprint plan 2.
        Assert.Equal([new CancelPlan(1)], Drag.Judge(Removal, [0], w.M.Building).SelectMany(v => v.Commands));
        Assert.Equal(
            [new Deconstruct(14, "machine"), new Deconstruct(19, "back")],
            Drag.Judge(Removal, [14, 19], w.M.Building).SelectMany(v => v.Commands));
    }
}

/// <summary>The grid near the pointer: apps/game/src/features/building/near-grid.test.ts, one for one.</summary>
public class NearGridTests
{
    [Fact]
    public void DrawsTheEdgesOfTheCellUnderThePointerStrongestAndFadesToNothingAtItsReach()
    {
        var edges = NearGrid.Around((50.5, 50.5), 100, 100);
        double strongest = edges.Max(e => e.Alpha);
        Assert.Equal(strongest, edges.First(e => e.X1 == 50 && e.Y1 == 50 && e.X2 == 50).Alpha);
        Assert.Contains(edges, e => (e.X1 == 50 || e.X1 == 51) && (e.X2 == 50 || e.X2 == 51));
        foreach (var e in edges)
        {
            double mx = (e.X1 + e.X2) / 2.0 - 50.5, my = (e.Y1 + e.Y2) / 2.0 - 50.5;
            Assert.True(Math.Sqrt(mx * mx + my * my) < NearGrid.Near);
            Assert.True(e.Alpha > 0);
        }
        Assert.True(edges.Where(e => Math.Abs(e.X1 - 50) > 3).Max(e => e.Alpha) < strongest);
    }

    [Fact]
    public void DrawsNothingPastTheEdgesOfTheGrid()
    {
        foreach (var e in NearGrid.Around((0.5, 99.5), 100, 100))
        {
            Assert.True(Math.Min(e.X1, e.X2) >= 0);
            Assert.True(Math.Max(e.Y1, e.Y2) <= 100);
        }
    }
}
