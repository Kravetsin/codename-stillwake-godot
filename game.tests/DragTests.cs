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
    private static readonly Remove Removal = Tools.RemovalIn(Focus.Objects);
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

    /// <summary>What a drag of the right button judges over <paramref name="cells"/> in <paramref name="focus"/> (0110).</summary>
    private static List<Verdict> Removing(TestWorld w, Focus focus, IReadOnlyList<int> cells) =>
        Drag.Judge(Tools.RemovalIn(focus), cells, w.M.Building, w.M.Grid);

    private static List<int> Rectangle(Spot from, Spot to) => Drag.CellsOf(12, Removal, from, to);

    [Fact]
    public void TakesWhatIsInTheFocusedLayerOnly()
    {
        var w = Start();
        Assert.Equal([new Deconstruct(14, "machine")], Removing(w, Focus.Objects, [14]).SelectMany(v => v.Commands));
        Assert.Equal([new Deconstruct(14, "back")], Removing(w, Focus.Back, [14]).SelectMany(v => v.Commands));
        Assert.Equal([new Deconstruct(0, "structure")], Removing(w, Focus.Walls, [0]).SelectMany(v => v.Commands));
        // A wall is no object, a machine no wall, and the open cell at 17 holds no wall.
        Is("building.nothingThere", [], Removing(w, Focus.Objects, [0])[0]);
        Is("building.nothingThere", [], Removing(w, Focus.Walls, [14])[0]);
        Is("building.nothingThere", [], Removing(w, Focus.Walls, [17])[0]);
    }

    [Fact]
    public void SendsNothingWhereTheSimWouldRefuseAMark()
    {
        var w = Start();
        // The wall marked already: nothing left to mark there.
        w.Step(new Deconstruct(0, "structure"));
        var verdicts = Removing(w, Focus.Walls, [17, 0]);
        Is("building.nothingThere", [], verdicts[0]);
        Is("building.planned", [], verdicts[1]);
    }

    [Fact]
    public void MarksAMachineOnceHoweverManyOfItsCellsTheLineCrosses()
    {
        var w = Start();
        Assert.Equal([new Deconstruct(13, "machine")], Removing(w, Focus.Objects, [13, 14]).SelectMany(v => v.Commands));
    }

    [Fact]
    public void BegunOnAPlanCancelsEveryPlanOfTheLayerInACellAMachineOnceHoweverManyCellsItCrosses()
    {
        var w = Start();
        w.Step(Plan(21, Floor.Placement), Plan(17, new Placement("machine", "tank")));
        // Ids follow the order the plans were made: floor 1, tank 2 over (5, 1) to (8, 4).
        var verdicts = Removing(w, Focus.Objects, [21, 33, 18, 17, 29]);
        Assert.Equal([null, "building.noPlan", null, null, null], verdicts.Select(v => v.Problem));
        Assert.Equal([new CancelPlan(1), new CancelPlan(2)], verdicts.SelectMany(v => v.Commands));
    }

    [Fact]
    public void BegunOnAPlanCancelsOnlyTheFocusedLayersPlansAcrossTheRectangle()
    {
        var w = Start();
        // Wall 1 at 19, back wall 2 at 17, wall 3 at 31.
        w.Step(Plan(19, Wall.Placement), Plan(17, Placement.BackWall()), Plan(31, Wall.Placement));
        var cells = Rectangle(new(7, 1), new(5, 2));
        Assert.Equal([19, 18, 17, 31, 30, 29], cells);
        Assert.Equal([new CancelPlan(1), new CancelPlan(3)], Removing(w, Focus.Walls, cells).SelectMany(v => v.Commands));
        Assert.Equal([new CancelPlan(2)], Removing(w, Focus.Back, Rectangle(new(5, 1), new(7, 2))).SelectMany(v => v.Commands));
    }

    [Fact]
    public void RemovesOverTheRectangleBetweenItsCornersMarkingEachThingOfTheLayerInItOnce()
    {
        var w = Start();
        // From (3, 1) to (6, 2): two of the tank's columns, the gap and back walls; the objects take the tank alone.
        var cells = Rectangle(new(3, 1), new(6, 2));
        Assert.Equal([15, 16, 17, 18, 27, 28, 29, 30], cells);
        Assert.Equal([new Deconstruct(15, "machine")], Removing(w, Focus.Objects, cells).SelectMany(v => v.Commands));
        // Begun on the tank, the back takes every back wall in it, those behind the tank too.
        var back = Removing(w, Focus.Back, cells).SelectMany(v => v.Commands).Cast<Deconstruct>().ToList();
        Assert.All(back, d => Assert.Equal("back", d.Layer));
        Assert.Contains(new Deconstruct(15, "back"), back);
        Assert.Contains(new Deconstruct(28, "back"), back);
        Assert.Equal(back.Count, back.Distinct().Count());
        // Dragged back the other way, the same rectangle from its other corner.
        Assert.Equal([30, 29, 28, 27, 18, 17, 16, 15], Rectangle(new(6, 2), new(3, 1)));
        // Placing, as LineOf lays it.
        Assert.Equal(Drag.LineOf(12, Wall, new(7, 1), new(5, 2)), Drag.CellsOf(12, Wall, new(7, 1), new(5, 2)));
    }

    [Fact]
    public void BegunOnAMarkTakesTheMarkOffBegunOnWhatIsBuiltLeavesPlansBe()
    {
        var w = Start();
        w.Step(new Deconstruct(0, "structure"), Plan(19, Wall.Placement));
        // The mark is plan 1, the wall's blueprint plan 2.
        Assert.Equal([new CancelPlan(1)], Removing(w, Focus.Walls, [0]).SelectMany(v => v.Commands));
        Assert.Equal([new Deconstruct(14, "machine")], Removing(w, Focus.Objects, [14, 19]).SelectMany(v => v.Commands));
    }
}
