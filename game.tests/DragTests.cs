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
    private static readonly Remove Removal = new(["machine", "conduit", "structure", "back"]);
    private const int W = 10;

    [Fact]
    public void LaysALineAlongTheLongerAxisEitherWay()
    {
        Assert.Equal([12, 13, 14, 15], Drag.LineOf(W, Wall, new(2, 1), new(5, 2)));
        Assert.Equal([15, 14, 13], Drag.LineOf(W, Wall, new(5, 1), new(3, 1)));
        Assert.Equal([12, 22, 32, 42], Drag.LineOf(W, Wall, new(2, 1), new(3, 4)));
        Assert.Equal([32, 22, 12], Drag.LineOf(W, Wall, new(2, 3), new(2, 1)));
    }

    [Fact]
    public void RunsAcrossOnATieAndIsOneCellWhenItDoesNotMove()
    {
        Assert.Equal([12, 13, 14], Drag.LineOf(W, Wall, new(2, 1), new(4, 3)));
        Assert.Equal([12], Drag.LineOf(W, Wall, new(2, 1), new(2, 1)));
    }

    [Fact]
    public void PlacesAMachineOnlyWhereItBegan() =>
        Assert.Equal([12], Drag.LineOf(W, Tank, new(2, 1), new(7, 1)));

    [Fact]
    public void StartsOnAPressOnTheGridAndEndsOnTheReleaseKeptToTheGrid()
    {
        var started = Drag.OnButton(null, new ButtonEvent(true, (2.5, 1.2), false), 10, 5);
        Assert.Equal(((Spot?)new Spot(2, 1), ((Spot, Spot)?)null), started);
        var ended = Drag.OnButton(started.Pressed, new ButtonEvent(false, (14, -3), false), 10, 5);
        Assert.Equal(((Spot?)null, ((Spot, Spot)?)(new Spot(2, 1), new Spot(9, 0))), ended);
    }

    [Fact]
    public void StartsNothingOnAPressOffTheGridAndAReleaseAloneEndsNothing()
    {
        Assert.Equal(((Spot?)null, ((Spot, Spot)?)null), Drag.OnButton(new Spot(1, 1), new ButtonEvent(true, (-0.5, 2), false), 10, 5));
        Assert.Equal(((Spot?)null, ((Spot, Spot)?)null), Drag.OnButton(null, new ButtonEvent(false, (3, 2), false), 10, 5));
    }

    // Seven cells wide: cell (x, y) is y * 7 + x. Cell 10 is open, with no back wall: nothing to take apart there.
    private static TestWorld Start() => new(Layout.Parse(["#######", "#TT -.#", "#TT...#", "#######"]));

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
        var verdicts = Drag.Judge(Wall, [3, 10, 0], w.M.Building);
        Assert.Equal(["grid.occupied", null, "grid.occupied"], verdicts.Select(v => v.Problem));
        Assert.Equal([Plan(10, Wall.Placement)], verdicts[1].Commands);
        Assert.Empty(verdicts[0].Commands);
    }

    [Fact]
    public void TakesAMachineFirstThenAConduitThenTheStructure()
    {
        var w = Start();
        Assert.Equal(
            [new Deconstruct(9, "machine"), new Deconstruct(11, "conduit"), new Deconstruct(0, "structure")],
            Drag.Judge(Removal, [9, 11, 0], w.M.Building).SelectMany(v => v.Commands));
        Assert.Equal("building.nothingThere", Drag.Judge(Removal, [10], w.M.Building)[0].Problem);
    }

    [Fact]
    public void TakesOnlyFromTheLayersItRemovesFrom()
    {
        var w = Start();
        var ship = new Remove(["machine", "structure", "back"]);
        var pipes = new Remove(["conduit"]);
        Assert.Equal([new Deconstruct(11, "back")], Drag.Judge(ship, [11], w.M.Building).SelectMany(v => v.Commands));
        var verdicts = Drag.Judge(pipes, [11, 0], w.M.Building);
        Is(null, [new Deconstruct(11, "conduit")], verdicts[0]);
        Is("building.nothingThere", [], verdicts[1]);
    }

    [Fact]
    public void SendsNothingWhereTheSimWouldRefuseAMark()
    {
        var w = Start();
        // The wall and the wall behind it, both marked: nothing left to mark there.
        w.Step(new Deconstruct(0, "structure"), new Deconstruct(0, "back"));
        var verdicts = Drag.Judge(Removal, [10, 0], w.M.Building);
        Is("building.nothingThere", [], verdicts[0]);
        Is("building.planned", [], verdicts[1]);
    }

    [Fact]
    public void MarksAMachineOnceHoweverManyOfItsCellsTheLineCrosses()
    {
        var w = Start();
        Assert.Equal([new Deconstruct(8, "machine")], Drag.Judge(Removal, [8, 9], w.M.Building).SelectMany(v => v.Commands));
    }

    [Fact]
    public void BegunOnAPlanCancelsEveryPlanInACellAMachineOnceHoweverManyCellsItCrosses()
    {
        var w = Start();
        w.Step(Plan(12, Wall.Placement), Plan(12, new Placement("conduit", "pipe")), Plan(10, new Placement("machine", "tank")));
        // Ids follow the order the plans were made: wall 1, pipe 2, tank 3 over cells 10, 11, 17, 18.
        var verdicts = Drag.Judge(Removal, [12, 19, 11, 10, 17], w.M.Building);
        Assert.Equal([null, "building.noPlan", null, null, null], verdicts.Select(v => v.Problem));
        Assert.Equal([new CancelPlan(1), new CancelPlan(2), new CancelPlan(3)], verdicts.SelectMany(v => v.Commands));
    }

    [Fact]
    public void BegunOnAMarkTakesTheMarkOffBegunOnWhatIsBuiltLeavesPlansBe()
    {
        var w = Start();
        w.Step(new Deconstruct(0, "structure"), Plan(12, Wall.Placement));
        // The mark is plan 1, the wall's blueprint plan 2.
        Assert.Equal([new CancelPlan(1)], Drag.Judge(Removal, [0], w.M.Building).SelectMany(v => v.Commands));
        Assert.Equal(
            [new Deconstruct(9, "machine"), new Deconstruct(12, "back")],
            Drag.Judge(Removal, [9, 12], w.M.Building).SelectMany(v => v.Commands));
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
