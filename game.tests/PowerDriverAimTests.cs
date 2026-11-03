using Stillwake.Game.Features.Building;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The power driver's square under the pointer (power-driver): its cells, what it works and in what order, and its tip.</summary>
public class PowerDriverAimTests
{
    private const int Unit = Hero.Unit, Width = 16;

    /// <summary>A hall a character a cell, sixteen wide and ten high, walled round.</summary>
    private static TestWorld Hall() => new(Layout.Parse(
    [
        new string('#', Width),
        .. Enumerable.Repeat("#" + new string('.', Width - 2) + "#", 8),
        new string('#', Width),
    ]));

    /// <summary>A hero whose body is centred on the middle of cell (x, y).</summary>
    private static Hero CellHero(int x, int y) => new(x * Unit + Unit / 2, y * Unit + Unit / 2 + Hero.MiddleUp, 0, 0, false, 1);

    private static int C(int x, int y) => y * Width + x;

    private static readonly Placement Scrap = Placement.Wall("scrap");

    /// <summary>The square of side 2 round the cell corner at (x, y) and the targets it gives the hero at (2, 8), in creative mode.</summary>
    private static List<WorkAim> TargetsAt(TestWorld w, double x, double y, Hero? hero = null) =>
        Aiming.WorkTargets(w.M, 0, hero ?? CellHero(2, 8), Aiming.SquareAt(Width, 10, (x, y), 2)!.Value.Cells);

    [Fact]
    public void TheSquareLiesRoundTheNearestCellCornerKeptOnTheGridItsCellsOnceTheBottomRowFirst()
    {
        // Round the corner nearest the point, as the placing stamp lies.
        var (x, y, cells) = Aiming.SquareAt(Width, 10, (5.4, 3.6), 2)!.Value;
        Assert.Equal((4, 3), (x, y));
        Assert.Equal([C(4, 4), C(5, 4), C(4, 3), C(5, 3)], cells);
        // At the grid's edge it is kept on it, four cells, none twice.
        var edge = Aiming.SquareAt(Width, 10, (0.2, 9.9), 2)!.Value;
        Assert.Equal((0, 8), (edge.X, edge.Y));
        Assert.Equal([C(0, 9), C(1, 9), C(0, 8), C(1, 8)], edge.Cells);
        Assert.Equal([C(Width - 2, 1), C(Width - 1, 1), C(Width - 2, 0), C(Width - 1, 0)], Aiming.SquareAt(Width, 10, (Width - 0.1, 0.1), 2)!.Value.Cells);
        // Off the grid there is none; a side of 1 is the cell under the point.
        Assert.Null(Aiming.SquareAt(Width, 10, (-0.5, 3), 2));
        Assert.Equal([C(5, 3)], Aiming.SquareAt(Width, 10, (5.4, 3.6), 1)!.Value.Cells);
    }

    [Fact]
    public void ItWorksTheSquareWhileChargedInItsModeRunningOrNotElseOneCell()
    {
        var driver = Spec.Screwdrivers.PowerDriver;
        // Charged, it shows the square before the press, not only once the sim runs it.
        Assert.Equal(2, Aiming.SideOf(driver, square: true, charged: true));
        // Switched to one cell, or flat: one cell.
        Assert.Equal(1, Aiming.SideOf(driver, square: false, charged: true));
        Assert.Equal(1, Aiming.SideOf(driver, square: true, charged: false));
        // The screwdriver is always one cell; so is a hand with no builder.
        Assert.Equal(1, Aiming.SideOf(Spec.Screwdrivers.Basic, square: true, charged: true));
        Assert.Equal(1, Aiming.SideOf(null, square: true, charged: true));
    }

    [Fact]
    public void TheSquareShowsOnlyOverAPlanOrAMarkElseThePointerAimsAtOneCell()
    {
        var w = Hall().Step(new SetCreative(true));
        // Over the hall's empty floor: no square, so no outline and no work at nothing.
        Assert.Null(Aiming.WorkedSquare(w.M, 0, CellHero(2, 8), (6, 4), 2));
        // One wall planned in it: the whole square shows, its one target that plan.
        w.Step(new PlacePlan(C(6, 4), Scrap));
        var planned = Aiming.WorkedSquare(w.M, 0, CellHero(2, 8), (6, 4), 2);
        Assert.Equal((5, 3), (planned!.Value.Square.X, planned.Value.Square.Y));
        Assert.Equal([w.M.Building.PlansAt(C(6, 4))[0].Id], planned.Value.Targets.Select(t => t.Plan));
        // A back wall marked to come down shows it too.
        w.Step(new Deconstruct(C(10, 4), "back"));
        var marked = Aiming.WorkedSquare(w.M, 0, CellHero(9, 8), (10, 4), 2);
        Assert.Equal([w.M.Building.PlansAt(C(10, 4))[0].Id], marked!.Value.Targets.Select(t => t.Plan));
        // One cell, there is no square at all.
        Assert.Null(Aiming.WorkedSquare(w.M, 0, CellHero(2, 8), (6, 4), 1));
    }

    [Fact]
    public void ItWorksTheBottomRowFirstLeftToRightAndTheFrontBeforeTheBackWallInEachCell()
    {
        var w = Hall().Step(new SetCreative(true));
        // A wall planned and the back wall marked in each cell of the square from (5, 3) to (6, 4): plan ids by cell.
        int[] cells = [C(5, 3), C(6, 3), C(5, 4), C(6, 4)];
        foreach (int cell in cells) w.Step(new PlacePlan(cell, Scrap), new Deconstruct(cell, "back"));
        int Front(int cell) => w.M.Building.PlansAt(cell).Single(p => p.Place != null).Id;
        int Back(int cell) => w.M.Building.PlansAt(cell).Single(p => p.Clear == "back").Id;
        var targets = TargetsAt(w, 6, 4);
        Assert.Equal(
            [Front(C(5, 4)), Back(C(5, 4)), Front(C(6, 4)), Back(C(6, 4)), Front(C(5, 3)), Back(C(5, 3)), Front(C(6, 3)), Back(C(6, 3))],
            targets.Select(t => t.Plan));
        Assert.All(targets, t => Assert.Null(t.Problem));
    }

    [Fact]
    public void ItTakesOnePlanALayerACellTheOldestThatCanBeWorkedElseTheOldestToShowWhy()
    {
        // A tank planned for a hero carrying nothing, and the back wall behind its corner marked: one cell, two layers.
        var w = Hall();
        w.Step(new PlacePlan(C(5, 3), new Placement("machine", "tank")), new Deconstruct(C(5, 3), "back"));
        var targets = TargetsAt(w, 5, 3.5);
        Assert.Equal([new WorkAim(1, "items.notEnough"), new WorkAim(2, null)], targets);
        // The screwdriver, one cell, works the one plan there it can.
        Assert.Equal(new WorkAim(2, null), Aiming.AimAt(w.M, 0, CellHero(2, 8), (5.5, 3.5))?.Work);
    }

    [Fact]
    public void AMachineUnderSeveralCellsOfTheSquareIsWorkedOnce()
    {
        var w = Hall().Step(new SetCreative(true));
        w.Step(new PlacePlan(C(5, 3), new Placement("machine", "tank")));
        var footprint = w.M.Grid.Footprint("tank", C(5, 3))!;
        Assert.True(footprint.Count > 1);
        var square = Aiming.SquareAt(Width, 10, (6, 4), 2)!.Value.Cells;
        Assert.True(square.Count(footprint.Contains) > 1);
        Assert.Equal([new WorkAim(1, null)], TargetsAt(w, 6, 4));
    }

    [Fact]
    public void ACellWithNothingOrOutOfReachStopsNoneOfTheOthers()
    {
        var w = Hall().Step(new SetCreative(true));
        // Planned in the square's bottom-right cell only: the pointer on the top-left one, empty, still works it.
        w.Step(new PlacePlan(C(6, 4), Scrap));
        Assert.Equal([new WorkAim(1, null)], TargetsAt(w, 5.6, 3.6));
        Assert.Null(Aiming.AimAt(w.M, 0, CellHero(2, 8), (5.5, 3.5))?.Work);
        // A hero at the reach's edge reaches the near column and not the far: each cell is reached by its own middle.
        int near = Spec.Hero.Reach / Unit;
        w.Step(new PlacePlan(C(1 + near, 3), Scrap), new PlacePlan(C(2 + near, 3), Scrap));
        var hero = CellHero(1, 3);
        Assert.True(Aiming.Reaches(hero, C(1 + near, 3), Width));
        Assert.False(Aiming.Reaches(hero, C(2 + near, 3), Width));
        Assert.Equal([new WorkAim(2, null)], TargetsAt(w, 2 + near, 3.5, hero));
    }

    [Fact]
    public void ABrokenMachineWithNoPlanIsRepairedOnceFromTheSquare()
    {
        var w = new TestWorld(Layout.Parse(["#########", "#.L.....#", "#########"]).Doubled());
        int lamp = w.M.Grid.Machines.First(m => m.Kind == "lamp").Id;
        w.Edit(root => root["slices"]!["power"]!["broken"] = new System.Text.Json.Nodes.JsonArray(lamp));
        var cell = w.M.Grid.MachineById(lamp)!.Cell;
        int width = w.M.Grid.Width;
        var square = Aiming.SquareAt(width, w.M.Grid.Height, (cell % width + 1, cell / width + 1), 2)!.Value.Cells;
        var targets = Aiming.WorkTargets(w.M, 0, CellHero(cell % width + 3, cell / width), square);
        Assert.Equal(lamp, Assert.Single(targets).Repair);
    }

    [Fact]
    public void ItSendsEachWorkableTargetItsTicksCappedPerTargetAndNothingForARefusedOne()
    {
        WorkAim[] targets = [new(3, null), new(4, "items.notEnough"), new(0, null, Repair: 9)];
        // The pointer's own aim, out of reach and on nothing, does not stop the square.
        var aim = new Aim(1, false);
        var fresh = Aiming.UsesOf(aim, pressed: false, held: true, 5, -1, targets: targets);
        Assert.Equal(new Command[] { new Work(3), new Repair(9) }, fresh.Commands);
        Assert.Equal(5, fresh.WorkedOnTick);
        // After a stall, each target catches up its own few ticks at most, at the tool's pace.
        var stalled = Aiming.UsesOf(aim, pressed: false, held: true, 100, 10, pace: 1, targets);
        Assert.Equal(5, stalled.Commands.Count(c => c is Work { Plan: 3 }));
        Assert.Equal(5, stalled.Commands.Count(c => c is Repair));
        Assert.Equal(10, stalled.Commands.Count);
        // Let go, or with nothing workable, nothing is sent.
        Assert.Empty(Aiming.UsesOf(aim, pressed: false, held: false, 5, 4, targets: targets).Commands);
        Assert.Empty(Aiming.UsesOf(aim, pressed: false, held: true, 5, -1, targets: [new WorkAim(4, "items.notEnough")]).Commands);
    }

    [Fact]
    public void RunningItSharesFourTicksATickOverTheSquareTwoAtMostOnOnePlan()
    {
        var driver = Spec.Screwdrivers.PowerDriver;
        Assert.Equal((2, 4), Aiming.PaceOf(driver, running: driver.Item));
        // Flat, or another item running: the screwdriver's tick a tick on one plan.
        Assert.Equal((1, 1), Aiming.PaceOf(driver, running: null));
        Assert.Equal((1, 1), Aiming.PaceOf(driver, running: "drill"));
        Assert.Equal((1, 1), Aiming.PaceOf(Spec.Screwdrivers.Basic, running: null));
        // Four walls a tick each; fewer plans twice; three, the first in the square's order gets the fourth tick.
        Assert.Equal([1, 1, 1, 1], Aiming.SharesOf(4, 2, 4));
        Assert.Equal([2, 2], Aiming.SharesOf(2, 2, 4));
        Assert.Equal([2], Aiming.SharesOf(1, 2, 4));
        Assert.Equal([2, 1, 1], Aiming.SharesOf(3, 2, 4));
        // So the square is never slower than one cell, and a machine under it goes at twice the screwdriver's pace.
        WorkAim[] three = [new(3, null), new(4, null), new(5, null)];
        var sent = Aiming.UsesOf(new Aim(1, false), pressed: false, held: true, 5, 4, pace: 2, three, budget: 4).Commands;
        Assert.Equal(new Command[] { new Work(3), new Work(3), new Work(4), new Work(5) }, sent);
        var one = Aiming.UsesOf(new Aim(1, true, Work: new WorkAim(7, null)), pressed: false, held: true, 5, 4, pace: 2, budget: 4).Commands;
        Assert.Equal(new Command[] { new Work(7), new Work(7) }, one);
    }

    [Fact]
    public void TheScrewdriverStillWorksTheOnePlanUnderThePointer()
    {
        // A wall planned and the back wall marked in one cell: one cell, one plan, the oldest it can work.
        var w = Hall().Step(new SetCreative(true));
        w.Step(new PlacePlan(C(5, 3), Scrap), new Deconstruct(C(5, 3), "back"), new PlacePlan(C(6, 3), Scrap));
        var aim = Aiming.AimAt(w.M, 0, CellHero(2, 8), (5.5, 3.5));
        Assert.Equal([new Work(1)], Aiming.UsesOf(aim, pressed: false, held: true, 5, -1).Commands);
    }

    [Fact]
    public void TheSquaresPromptBuildsWhileAnyTargetCanElseSaysTheFirstRefusal()
    {
        var w = Hall();
        w.Step(new PlacePlan(C(5, 4), Scrap), new Deconstruct(C(6, 4), "back"));
        var aim = Aiming.AimAt(w.M, 0, CellHero(2, 8), (5.5, 4.5))!;
        var targets = TargetsAt(w, 6, 4);
        Assert.Equal([new WorkAim(1, "items.notEnough"), new WorkAim(2, null)], targets);
        Assert.Equal(new Prompt("use.takeApart", null), Prompts.Of(w.M, Aiming.OverSquare(aim, targets)));
        Assert.Equal(new Prompt("use.build", "items.notEnough"), Prompts.Of(w.M, Aiming.OverSquare(aim, [targets[0]])));
        // With no target, the pointer's press is said, and no work.
        Assert.Null(Aiming.OverSquare(aim, []).Work);
    }

    [Fact]
    public void TheSquaresTipSumsWhatItsBlueprintsStillNeedAgainstWhatIsCarried()
    {
        var w = Hall().Give(0, ("scrap", 3), ("scrapChunk", 40));
        int[] cells = [C(5, 4), C(6, 4), C(5, 3)];
        foreach (int cell in cells) w.Step(new PlacePlan(cell, Scrap));
        var cost = w.M.Building.CostOf(1)!;
        // The first part of the first wall paid (0063): it is delivered, so the sum needs one less of it.
        w.Step(new Work(1));
        var delivered = w.M.Building.Plans[0].Delivered;
        Assert.NotEmpty(delivered);
        var carried = w.M.Items.Totals(0);
        var tip = SquareTip.Of(w.M.Building, w.M.Grid, Aiming.SquareAt(Width, 10, (6, 4), 2)!.Value.Cells, carried)!;
        var expected = cost.Select(c => new CostLine(c.Item, carried.Where(h => h.Item == c.Item).Sum(h => h.Count),
            3 * c.Count - delivered.Where(d => d.Item == c.Item).Sum(d => d.Count))).ToList();
        Assert.Equal(new PlanTip(Scrap, expected).Tip, tip);
        // Mixed blueprints say their sum without one name; none lacking anything, no tip.
        w.Step(new PlacePlan(C(6, 3), Placement.Wall("iron")));
        Assert.Null(SquareTip.Of(w.M.Building, w.M.Grid, Aiming.SquareAt(Width, 10, (6, 4), 2)!.Value.Cells, carried)!.Name);
        Assert.Null(SquareTip.Of(w.M.Building, w.M.Grid, [C(9, 6)], carried));
    }

    [Fact]
    public void TheSquaresTipOverMarksSumsWhatTheyAllGiveBackDifferentWallsToo()
    {
        var w = Hall().Step(new SetCreative(true));
        // The square round the hall's bottom-left corner: the floor's wall at (0, 9) and (1, 9), the side wall at (0, 8).
        var cells = Aiming.SquareAt(Width, 10, (1, 9), 2)!.Value.Cells;
        var wall = w.M.Building.CostFor(Tools.BuiltAt(C(0, 9), "structure", w.M.Grid)!);
        w.Step(new Deconstruct(C(0, 9), "structure"), new Deconstruct(C(1, 9), "structure"));
        // Two walls alike: under their name, each item twice what one gives back.
        var two = SquareTip.Of(w.M.Building, w.M.Grid, cells, [])!;
        Assert.NotNull(two.Name);
        Assert.Equal(wall.Where(c => c.Count > 0).Select(c => $"+{2 * c.Count}"), two.Lines.Select(l => l.Text));
        // A back wall marked too: all three summed, with no one name.
        var back = w.M.Building.CostFor(Tools.BuiltAt(C(1, 8), "back", w.M.Grid)!);
        w.Step(new Deconstruct(C(1, 8), "back"));
        var three = SquareTip.Of(w.M.Building, w.M.Grid, cells, [])!;
        Assert.Null(three.Name);
        var summed = wall.Concat(wall).Concat(back).GroupBy(c => c.Item).Select(g => $"+{g.Sum(c => c.Count)}");
        Assert.Equal(summed, three.Lines.Select(l => l.Text));
    }
}
