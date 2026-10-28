using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Rooms;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/building/delivery.test.ts, ported, then paid a part at a time (0063): blueprints paid and built, things taken apart.</summary>
public class BuildingDeliveryTests
{
    /// <summary>What every hero in these tests carries from the start.</summary>
    private const int StockScrap = 360, StockIron = 60, StockPlate = 60, StockGear = 20, StockTube = 20;

    /// <summary>A world from <paramref name="map"/>, doubled (0104), with peers 0 to 3 carrying the stock.</summary>
    private static Scenario Stocked(Layout map)
    {
        var w = new Scenario(map, doubled: true);
        foreach (int peer in new[] { 0, 1, 2, 3 }) w.GiveItems(peer, ("scrap", StockScrap), ("iron", StockIron), ("plate", StockPlate), ("gear", StockGear), ("tube", StockTube));
        return w;
    }

    private static Scenario Stocked(string map) => Stocked(Layout.Parse(map));

    private static readonly Placement IronWall = Placement.Wall("iron");
    private static readonly Placement Door = new("structure", "door");
    private static readonly Placement Tank = new("machine", "tank");
    private static readonly Material Iron = Materials.Get("iron"), Scrap = Materials.Get("scrap");
    private static readonly int IronWallTicks = Iron.BuildTicks;
    private static int CostIn(IReadOnlyList<(string Item, int Count)> cost, string item) =>
        cost.Where(c => c.Item == item).Sum(c => c.Count);
    /// <summary>The tick of its work part <paramref name="part"/> of machine <paramref name="spec"/>'s blueprint begins at, an item of its cost a part.</summary>
    private static int Begins(MachineSpec spec, int part) => spec.BuildTicks * part / Costs.PartsOf(spec.Cost);
    /// <summary>The plates an iron wall takes, and the scrap a scrap one does.</summary>
    private static readonly int IronPlates = CostIn(Iron.Cost, "plate"), ScrapUnits = CostIn(Scrap.Cost, "scrap");

    /// <summary>Nine characters wide and five tall, doubled to 18 by 10 cells: a door, six cells tall, fits its room (0104).</summary>
    private const string Open = """
        #########
        #.......#
        #.......#
        #.......#
        #########
        """;
    /// <summary>The cell of the character at (<paramref name="x"/>, <paramref name="y"/>) of a nine-wide map: its block's top-left.</summary>
    private static int At(int x, int y) => 2 * y * 18 + 2 * x;
    /// <summary>The cell below <paramref name="cell"/>: a block's bottom-left below its top-left.</summary>
    private static int Below(int cell) => cell + 18;

    /// <summary>E on <paramref name="plan"/> by peer <paramref name="from"/>, and the tick it lands on.</summary>
    private static Scenario Press(Scenario w, int plan, int from = 0) => w.Do(new Work(plan) { From = from }).Run();

    /// <summary><paramref name="ticks"/> presses of E on <paramref name="plan"/> by peer <paramref name="from"/>, one a tick: E held.</summary>
    private static Scenario Hold(Scenario w, int plan, int ticks, int from = 0)
    {
        for (int i = 0; i < ticks; i++) Press(w, plan, from);
        return w;
    }

    /// <summary>How much of <paramref name="item"/> peer <paramref name="peer"/> carries.</summary>
    private static int Count(Scenario w, string item, int peer = 0) =>
        w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);

    /// <summary>How much of <paramref name="item"/> lies on the floor.</summary>
    private static int Lying(Scenario w, string item) => w.M.Items.Floor.Sum(s => s.Item == item ? s.Count : 0);

    private static Plan? PlanOf(Scenario w, int id) => w.M.Building.Plans.FirstOrDefault(p => p.Id == id);
    private static Scenario PlacePlan(Scenario w, int cell, Placement placement) => w.Do(new PlacePlan(cell, placement)).Run();
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static (string, int)[] Delivered(Plan? p) => p?.Delivered.Select(s => (s.Item, s.Count)).ToArray() ?? [];

    // Blueprints.

    [Fact]
    public void BlueprintsCostNothingToPlace()
    {
        var w = PlacePlan(Stocked(Open), At(1, 1), IronWall);
        Assert.Equal(BuildingTests.Of(Plan.Blueprint(1, At(1, 1), IronWall)), BuildingTests.Of(PlanOf(w, 1)!));
        Assert.Equal(StockPlate, Count(w, "plate"));
        Assert.Equal<(string, int)>(Iron.Cost, w.M.Building.CostOf(1)!);
        Assert.Equal<(string, int)>(Iron.Cost, w.M.Building.Lacking(1));
    }

    [Fact]
    public void BlueprintsTakeAnItemAsEachPartOfTheirWorkBeginsAndStopWhereAPartIsNotPaidFor()
    {
        // A wall cell costs one item now (0104): a tank's items, its plates first, make the parts.
        var tank = Machines.Get("tank");
        int parts = tank.Cost.Sum(c => c.Count);
        Assert.Equal("plate", tank.Cost[0].Item);
        var w = new Scenario(Open);
        w.GiveItems(5, ("plate", 1));
        PlacePlan(w, At(1, 1), Tank);
        Assert.Equal((0, parts), w.M.Building.Parts(1));
        Press(w, 1, 5);
        Assert.Equal([("plate", 1)], Delivered(PlanOf(w, 1)));
        Assert.Equal((0, 1), (Count(w, "plate", 5), PlanOf(w, 1)!.Progress));
        Assert.Equal((1, parts), w.M.Building.Parts(1));
        var rest = tank.Cost.Select(c => c.Item == "plate" ? (c.Item, c.Count - 1) : c).ToArray();
        Assert.Equal<(string, int)>(rest, w.M.Building.Lacking(1));
        // The second part begins at the build's ticks times 1/parts, rounded down.
        int second = Begins(tank, 1);
        Assert.True(second > 1);
        Hold(w, 1, second - 1, 5);
        Assert.Empty(w.Refusals);
        Press(w, 1, 5);
        Assert.Equal(["items.notEnough"], Reasons(w));
        Assert.Equal("items.notEnough", w.M.Building.WorkProblem(1, 5));
        Assert.Equal(second, PlanOf(w, 1)!.Progress);
        Assert.Equal((1, parts), w.M.Building.Parts(1));
        Assert.Empty(w.M.Grid.Machines);
        w.GiveItems(5, rest);
        Assert.Null(w.M.Building.WorkProblem(1, 5));
        Press(w, 1, 5);
        Assert.Equal((rest[0].Item2 - 1, second + 1), (Count(w, "plate", 5), PlanOf(w, 1)!.Progress));
        Assert.Equal((2, parts), w.M.Building.Parts(1));
        Hold(w, 1, tank.BuildTicks - second - 1, 5);
        Assert.Single(w.Refusals);
        Assert.All(tank.Cost, c => Assert.Equal(0, Count(w, c.Item, 5)));
        Assert.Equal("tank", w.M.Grid.MachineAt(At(1, 1))?.Kind);
        Assert.Empty(w.M.Building.Plans);
    }

    [Fact]
    public void BlueprintsAreBuiltByAsManyTicksOfWorkAsTheyTakeTheFirstPayingForTheOnlyPart()
    {
        var w = PlacePlan(Stocked(Open), At(1, 1), IronWall);
        Press(w, 1);
        Assert.Equal<(string, int)>(Iron.Cost, Delivered(PlanOf(w, 1)));
        Assert.Equal(StockPlate - IronPlates, Count(w, "plate"));
        Hold(w, 1, IronWallTicks - 2);
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(At(1, 1)));
        Assert.Equal(IronWallTicks - 1, PlanOf(w, 1)!.Progress);
        Press(w, 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(Iron, w.M.Grid.WallAt(At(1, 1)));
        Assert.Empty(w.M.Building.Plans);
        Assert.Equal(StockPlate - IronPlates, Count(w, "plate"));
    }

    [Fact]
    public void BlueprintsCostAndTakeABackWallWhatAWallOfItsMaterialDoesRefundedWhenTakenApart()
    {
        var w = Stocked(Open);
        w.Do(new Deconstruct(At(1, 1), "back")).Run();
        Hold(w, 1, Costs.ApartTicks(Scrap.BuildTicks));
        Assert.Null(w.M.Grid.BackAt(At(1, 1)));
        Assert.Equal(StockScrap + ScrapUnits, Count(w, "scrap"));
        PlacePlan(w, At(1, 1), Placement.BackWall("iron"));
        Assert.Equal<(string, int)>(Iron.Cost, w.M.Building.CostOf(2)!);
        Hold(w, 2, IronWallTicks - 1);
        Assert.Null(w.M.Grid.BackAt(At(1, 1)));
        Press(w, 2);
        Assert.Equal(Iron, w.M.Grid.BackAt(At(1, 1)));
        Assert.Equal(StockPlate - IronPlates, Count(w, "plate"));
    }

    [Fact]
    public void BlueprintsArePaidInTheCostsOrderEachPartWithTheFirstItemCarriedAndStillLacking()
    {
        // A vent: its plates, then its tubes, a part each.
        var vent = Machines.Get("vent");
        Assert.Equal(["plate", "tube"], vent.Cost.Select(c => c.Item));
        int plates = CostIn(vent.Cost, "plate"), tubes = CostIn(vent.Cost, "tube");
        var w = new Scenario(Open);
        w.GiveItems(5, ("tube", 9), ("plate", 9));
        w.GiveItems(6, ("tube", 9));
        PlacePlan(w, At(1, 1), new Placement("machine", "vent"));
        PlacePlan(w, At(4, 1), new Placement("machine", "vent"));
        Press(w, 1, 5);
        Assert.Equal([("plate", 1)], Delivered(PlanOf(w, 1)));
        Hold(w, 1, Begins(vent, plates), 5);
        Assert.Equal([("plate", plates), ("tube", 1)], Delivered(PlanOf(w, 1)));
        Assert.Equal((9 - plates, 8), (Count(w, "plate", 5), Count(w, "tube", 5)));
        // Carrying no plate, the first item it still lacks, a hero pays its tubes first, then is short.
        Press(w, 2, 6);
        Assert.Equal([("tube", 1)], Delivered(PlanOf(w, 2)));
        Hold(w, 2, Begins(vent, tubes), 6);
        Assert.Equal(["items.notEnough"], Reasons(w));
        Assert.Equal([("tube", tubes)], Delivered(PlanOf(w, 2)));
        Assert.Equal((Begins(vent, tubes), 9 - tubes), (PlanOf(w, 2)!.Progress, Count(w, "tube", 6)));
        Assert.Equal<(string, int)>([("plate", plates)], w.M.Building.Lacking(2));
    }

    [Fact]
    public void BlueprintsArePaidByEachWorkingHeroFromTheirOwnInventoryAsTheyReachAPart()
    {
        // A tank: its plates first, a part an item of its cost.
        var tank = Machines.Get("tank");
        Assert.Equal("plate", tank.Cost[0].Item);
        int second = Begins(tank, 1), third = Begins(tank, 2);
        var w = new Scenario(Open);
        w.GiveItems(5, ("plate", 1));
        w.GiveItems(6, tank.Cost.ToArray());
        PlacePlan(w, At(1, 1), Tank);
        Hold(w, 1, second, 5);
        Assert.Equal((0, second), (Count(w, "plate", 5), PlanOf(w, 1)!.Progress));
        // Out of plates at the second part: the hero who brought what it had cannot work on.
        Assert.Equal("items.notEnough", w.M.Building.WorkProblem(1, 5));
        int plates = CostIn(tank.Cost, "plate");
        Press(w, 1, 6);
        Assert.Equal(plates - 1, Count(w, "plate", 6));
        Press(w, 1, 6);
        // Both at work on one tick each, twice as fast; the first still pays nothing at the third part.
        int both = (third - second - 2) / 2;
        for (int t = 0; t < both; t++) w.Do(new Work(1) { From = 5 }).Do(new Work(1) { From = 6 }).Run();
        Assert.Equal(second + 2 + 2 * both, PlanOf(w, 1)!.Progress);
        if (PlanOf(w, 1)!.Progress < third) Press(w, 1, 6);
        Assert.Equal(third, PlanOf(w, 1)!.Progress);
        Assert.Empty(w.Refusals);
        Press(w, 1, 5);
        Assert.Equal(["items.notEnough"], Reasons(w));
        Hold(w, 1, tank.BuildTicks - third, 6);
        Assert.Equal("tank", w.M.Grid.MachineAt(At(1, 1))?.Kind);
        Assert.Equal((1, 0), (Count(w, "plate", 6), Count(w, "tube", 6)));
    }

    [Fact]
    public void BlueprintsBuildAMachineOverItsWholeFootprintOnceItIsPaidAndWorked()
    {
        var w = PlacePlan(Stocked(Open), At(1, 1), Tank);
        Hold(w, 1, Machines.Get("tank").BuildTicks);
        Assert.Empty(w.Refusals);
        var machine = Assert.Single(w.M.Grid.Machines);
        Assert.Equal(("tank", At(1, 1)), (machine.Kind, machine.Cell));
        foreach (int cell in Enumerable.Range(0, 16).Select(i => At(1, 1) + i / 4 * 18 + i % 4))
            Assert.Equal(machine.Id, w.M.Grid.MachineAt(cell)?.Id);
        var cost = Machines.Get("tank").Cost;
        Assert.Equal((StockPlate - CostIn(cost, "plate"), StockTube - CostIn(cost, "tube")), (Count(w, "plate"), Count(w, "tube")));
    }

    [Fact]
    public void BlueprintsAreBuiltTwiceAsFastByTwoHeroesAndKeepTheWorkDoneWhenTheyStop()
    {
        var w = PlacePlan(Stocked(Open), At(1, 1), IronWall);
        // Short of done by a tick at least, whatever the wall's ticks, odd or even.
        int half = (IronWallTicks - 1) / 2;
        for (int i = 0; i < half; i++) w.Do(new Work(1) { From = 0 }).Do(new Work(1) { From = 1 }).Run();
        Assert.Equal(2 * half, PlanOf(w, 1)!.Progress);
        w.Run(50);
        Assert.Equal(2 * half, PlanOf(w, 1)!.Progress);
        Hold(w, 1, IronWallTicks - 2 * half);
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(At(1, 1)));
        // Only the hero whose tick began the one part paid.
        Assert.Equal((StockPlate - IronPlates, StockPlate), (Count(w, "plate", 0), Count(w, "plate", 1)));
    }

    [Fact]
    public void BlueprintsRefuseEFromAHeroCarryingNoneOfWhatTheyLackTakingNothing()
    {
        var w = new Scenario(Open);
        w.GiveItems(5, ("scrap", 50));
        PlacePlan(w, At(1, 1), IronWall);
        Press(w, 1, 5);
        Press(w, 1, 7);
        Assert.Equal(["items.notEnough", "items.notEnough"], Reasons(w));
        Assert.Equal("items.notEnough", w.M.Building.WorkProblem(1, 5));
        Assert.Empty(Delivered(PlanOf(w, 1)));
        Assert.Equal(0, PlanOf(w, 1)!.Progress);
        Assert.Equal(50, Count(w, "scrap", 5));
    }

    [Fact]
    public void BlueprintsRefuseEOnAPlanThatIsNotThere()
    {
        var w = Stocked(Open);
        Press(w, 3);
        Assert.Equal(["building.noPlan"], Reasons(w));
    }

    [Fact]
    public void BlueprintsLetWhatWasPutInFallWhereTheyStoodWhenCancelledAMarkDropsNothing()
    {
        var w = new Scenario(Open);
        w.GiveItems(5, ("plate", 9), ("tube", 9));
        PlacePlan(w, At(1, 1), Tank);
        // Into the tank's second part: two plates put in, the rest not yet.
        Hold(w, 1, Begins(Machines.Get("tank"), 1) + 1, 5);
        Assert.Equal([("plate", 2)], Delivered(PlanOf(w, 1)));
        w.Do(new CancelPlan(1) { From = 2 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Building.Plans);
        // Fallen from the blueprint's cell to the floor below it, the bottom of the doubled room, a few ticks down.
        w.Run(20);
        var lying = Assert.Single(w.M.Items.Floor);
        Assert.Equal(("plate", 2, Below(At(1, 3))), (lying.Item, lying.Count, lying.Cell));
        Assert.Equal((7, 9), (Count(w, "plate", 5), Count(w, "tube", 5)));
        w.Do(new Deconstruct(At(0, 1), "structure")).Run();
        w.Do(new CancelPlan(2)).Run();
        Assert.Equal(2, Lying(w, "plate"));
    }

    [Fact]
    public void BlueprintsAreWorkedInCreativeModeAsTheyAreAnOlderOneTooTakingNothing()
    {
        var w = new Scenario(Open);
        w.GiveItems(5, ("plate", 1));
        w.GiveItems(7, ("plate", 9), ("gear", 9));
        PlacePlan(w, At(1, 1), Door);
        Press(w, 1, 5);
        w.Do(new SetCreative(true)).Run();
        // A vent of several parts, as an iron wall was before a wall cell cost one plate (0104).
        var vent = Machines.Get("vent");
        int ventTicks = vent.BuildTicks, parts = Costs.PartsOf(vent.Cost);
        PlacePlan(w, At(3, 1), new Placement("machine", "vent"));
        Assert.Empty(w.M.Building.Lacking(2));
        Hold(w, 1, Builds.Get("door").BuildTicks - 1, 7);
        Hold(w, 2, ventTicks / 2, 7);
        // Drawn as far built as its work has gone, nothing paid.
        Assert.Equal((Enumerable.Range(0, parts).Count(k => Begins(vent, k) < ventTicks / 2), parts), w.M.Building.Parts(2));
        Assert.Empty(Delivered(PlanOf(w, 2)));
        Hold(w, 2, ventTicks - ventTicks / 2, 7);
        Assert.Empty(w.Refusals);
        Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(At(1, 1)));
        Assert.Equal("vent", w.M.Grid.MachineAt(At(3, 1))?.Kind);
        Assert.Equal((9, 9), (Count(w, "plate", 7), Count(w, "gear", 7)));
    }

    // Taking apart.

    [Fact]
    public void TakesAWallApartInItsShareOfTheBuildTimeRefundingItsMaterial()
    {
        var w = Stocked(Open).Do(new Deconstruct(At(0, 1), "structure")).Run();
        Hold(w, 1, Costs.ApartTicks(Scrap.BuildTicks) - 1);
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(At(0, 1)));
        Press(w, 1);
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(At(0, 1)));
        Assert.Equal(StockScrap + ScrapUnits, Count(w, "scrap"));
    }

    [Fact]
    public void TellsWhatAMarkGivesBackWhichIsWhatTakingItApartRefunds()
    {
        var w = Stocked(Layout.Parse(["#########", "#TT.....#", "#TT.....#", "#########"]));
        w.Do(new Deconstruct(At(2, 2), "machine")).Do(new Deconstruct(At(0, 1), "structure")).Run();
        PlacePlan(w, At(4, 1), IronWall);
        Assert.Equal<(string, int)>(Machines.Get("tank").Cost, w.M.Building.RefundOf(1)!);
        Assert.Equal<(string, int)>(Scrap.Cost, w.M.Building.RefundOf(2)!);
        // A blueprint gives nothing back when taken apart: it is cancelled. Nor does a plan that is not there.
        Assert.Null(w.M.Building.RefundOf(3));
        Assert.Null(w.M.Building.RefundOf(9));
        int tubes = Count(w, "tube");
        Hold(w, 1, Costs.ApartTicks(Machines.Get("tank").BuildTicks));
        Assert.Equal(tubes + CostIn(Machines.Get("tank").Cost, "tube"), Count(w, "tube"));
    }

    [Fact]
    public void TakesAShareOfTheBuildTimeRoundedUpSoNothingComesApartInNoTime()
    {
        Assert.Equal(20, Costs.ApartTicks(2 * 20));
        Assert.Equal(21, Costs.ApartTicks(2 * 20 + 1));
        Assert.Equal(1, Costs.ApartTicks(1));
    }

    [Fact]
    public void LetsWhatDoesNotFitFallWhereTheThingStood()
    {
        var w = Stocked(Open);
        w.GiveItems(0, ("iron", Content.Items.Get("iron").Stack - StockIron), ("plate", Content.Items.Get("plate").Stack - StockPlate));
        w.GiveItems(0, ("gear", Content.Items.Get("gear").Stack - StockGear), ("tube", Content.Items.Get("tube").Stack - StockTube));
        w.GiveItems(0, ("scrap", (Inventory.Slots - 4) * Content.Items.Get("scrap").Stack - StockScrap));
        w.Do(new Deconstruct(At(0, 1), "structure")).Run();
        Hold(w, 1, Costs.ApartTicks(Scrap.BuildTicks));
        Assert.Empty(w.Refusals);
        var lying = Assert.Single(w.M.Items.Floor);
        Assert.Equal(("scrap", ScrapUnits, At(0, 1)), (lying.Item, lying.Count, lying.Cell));
    }

    [Fact]
    public void RefusesToWorkOnACrateHoldingSomethingUntilItIsEmptied()
    {
        var w = Stocked(Layout.Parse(["#KK.#"]));
        w.Do(new Move(new Items.Place(1), new Target(Crate: 1), 5) { From = 1 }).Run();
        w.Do(new Deconstruct(2, "machine")).Run();
        Assert.Equal(["building.notEmpty"], Reasons(w));
        w.Do(new Move(new Items.Place(0, 1), new Target(), 99) { From = 1 }).Run();
        w.Do(new Deconstruct(2, "machine")).Run();
        // Filled again after it was marked: work is refused, the mark kept.
        w.Do(new Move(new Items.Place(1), new Target(Crate: 1), 1) { From = 1 }).Run();
        Press(w, 1);
        Assert.Equal("building.notEmpty", w.M.Building.WorkProblem(1, 0));
        Assert.Equal(0, PlanOf(w, 1)?.Progress);
        w.Do(new Move(new Items.Place(0, 1), new Target(), 99) { From = 1 }).Run();
        Hold(w, 1, Costs.ApartTicks(Machines.Get("crate").BuildTicks));
        Assert.Empty(w.M.Grid.Machines);
        Assert.Equal(StockIron + CostIn(Machines.Get("crate").Cost, "iron"), Count(w, "iron"));
    }

    [Fact]
    public void TakesAMachineApartFromAnyCellOfItsMark()
    {
        var w = Stocked(Layout.Parse(["#########", "#TT.....#", "#TT.....#", "#########"]));
        w.Do(new Deconstruct(At(2, 2), "machine")).Run();
        Hold(w, 1, Costs.ApartTicks(Machines.Get("tank").BuildTicks));
        Assert.Empty(w.M.Grid.Machines);
        Assert.Equal(StockPlate + CostIn(Machines.Get("tank").Cost, "plate"), Count(w, "plate"));
    }

    /// <summary>Beyond delivery.test.ts: the generic suites never deliver, as no fuzzed hero carries anything; a save holding deliveries plays on alike.</summary>
    [Fact]
    public void SavesAndLoadsPlansPartPaidWorkedAndMarked()
    {
        var w = new Scenario(Open);
        w.GiveItems(5, ("plate", 1));
        PlacePlan(w, At(1, 1), Door);
        Press(w, 1, 5);
        w.Do(new Deconstruct(At(0, 1), "structure")).Run();
        Hold(w, 2, 2);
        var before = w.World.Save();
        var plans = w.M.Building.Plans.Select(BuildingTests.Of).ToArray();
        w.Reload();
        Assert.Equal(before, w.World.Save());
        Assert.Equal(plans, w.M.Building.Plans.Select(BuildingTests.Of));
        Assert.Equal([("plate", 1)], Delivered(PlanOf(w, 1)));
    }

    // Conservation.

    /// <summary>A placePlan (with a placement), a deconstruct (with a layer), a cancelPlan, or E held for <c>Ticks</c>.</summary>
    private sealed record Step(int Cell, Placement? Placement, string? Layer, int Id, int Ticks, int From);

    private static readonly string[] Counted = ["scrap", "iron", "plate", "gear", "tube"];

    /// <summary>What the heroes carry, what lies on the floor and what blueprints were given, of <paramref name="item"/>.</summary>
    private static int Held(Scenario w, string item) =>
        Enumerable.Range(0, 5).Sum(peer => Count(w, item, peer)) + Lying(w, item)
        + w.M.Building.Plans.Sum(p => p.Delivered.Where(s => s.Item == item).Sum(s => s.Count));

    /// <summary>What is built of <paramref name="item"/> on the 180 cells of the doubled map: what taking it all apart would give.</summary>
    private static int Built(Scenario w, string item)
    {
        int total = 0, doorCells = 0;
        var grid = w.M.Grid;
        for (int c = 0; c < 180; c++)
        {
            if (grid.WallAt(c) is { } wall) total += CostIn(wall.Cost, item);
            if (grid.StructureAt(c) is Structure.DoorClosed or Structure.DoorOpen) doorCells++;
        }
        // A door is a piece of twelve cells (0104); doors built side by side come apart as one doorway, giving back each.
        var door = Builds.Get("door");
        total += CostIn(door.Cost, item) * doorCells / (door.Across * door.Tall);
        foreach (var m in grid.Machines) total += CostIn(Machines.Get(m.Kind).Cost, item);
        return total;
    }

    /// <summary>Of each counted item, everything held and built.</summary>
    private static int[] Total(Scenario w) => Counted.Select(i => Held(w, i) + Built(w, i)).ToArray();

    /// <summary>
    /// Runs <paramref name="c"/> for a tick, <paramref name="total"/> being what was held and built before it.
    /// Nothing is made, and nothing lost but stacks lying in space that leave the grid by Items' floor
    /// rules: inside the ship a stack stops at the edge. Whether that held; <paramref name="total"/> is kept current.
    /// </summary>
    private static bool Conserves(Scenario w, Command c, ref int[] total)
    {
        var inSpace = w.M.Items.Floor.Where(s => w.M.Rooms.RoomAt(s.Cell) == RoomsModule.Space).Select(s => (s.Id, s.Item, s.Count)).ToList();
        // Counted only around a tick when something may leave, and after each step: every tick is slow.
        if (inSpace.Count > 0 && !Total(w).SequenceEqual(total)) return false;
        w.Do(c).Run();
        if (inSpace.Count == 0) return true;
        var after = Total(w);
        for (int i = 0; i < Counted.Length; i++)
        {
            int lost = total[i] - after[i];
            int left = inSpace.Where(s => s.Item == Counted[i] && w.M.Items.Floor.All(f => f.Id != s.Id)).Sum(s => s.Count);
            if (lost < 0 || lost > left) return false;
        }
        total = after;
        return true;
    }

    [Property(MaxTest = 60)]
    public Property ConservesItemsAcrossInventoriesTheFloorAndBlueprintsPayingNoMoreThanACostNorWorkingPastWhatIsPaid()
    {
        var placement = Gen.Elements(IronWall, Placement.Wall("scrap"), Door, Tank);
        // The hull's cells too: taken apart, it lets what lies about out into space.
        var cell = Gen.Choose(0, 179);
        var sender = Gen.Choose(0, 4);
        var id = Gen.Frequency((3, Gen.Choose(1, 5)), (2, Gen.Choose(1, 20)));
        var step = Gen.OneOf(
            from c in cell from p in placement select new Step(c, p, null, 0, 0, 0),
            from c in cell from l in Gen.Elements("structure", "machine") select new Step(c, null, l, 0, 0, 0),
            from i in id from f in sender select new Step(0, null, null, i, 0, f),
            // E comes in holds on one plan, so plans reach their end.
            from i in id
            from t in Gen.Frequency((2, Gen.Choose(1, 220)), (1, Gen.Choose(1, 8)))
            from f in sender
            select new Step(0, null, null, i, t, f));
        var steps = from n in Gen.Choose(0, 40) from s in step.ArrayOf(n) select s;
        // Shrunk a step at a time, so a counterexample reads short.
        var arb = Arb.From(steps, all => Enumerable.Range(0, all.Length).Select(i => all.Where((_, j) => j != i).ToArray()));
        return Prop.ForAll(arb, all =>
        {
            var w = Stocked(Open);
            // Peer 4 carries little, so blueprints are left part paid, and has room for little, so refunds fall.
            w.GiveItems(4, ("scrap", 3), ("plate", 1), ("canister", (Inventory.Slots - 2) * Content.Items.Get("canister").Stack));
            var total = Total(w);
            foreach (var s in all)
            {
                Command c = s.Placement != null ? new PlacePlan(s.Cell, s.Placement)
                    : s.Layer != null ? new Deconstruct(s.Cell, s.Layer)
                    : s.Ticks > 0 ? new Work(s.Id) { From = s.From }
                    : new CancelPlan(s.Id) { From = s.From };
                for (int t = 0; t < Math.Max(1, s.Ticks); t++)
                    if (!Conserves(w, c, ref total)) return false;
                if (!Total(w).SequenceEqual(total)) return false;
                foreach (var p in w.M.Building.Plans)
                {
                    if (p.Progress >= (w.M.Building.TicksFor(p.Id) ?? 0)) return false;
                    if (p.Place == null) continue;
                    var cost = w.M.Building.CostOf(p.Id) ?? [];
                    if (p.Delivered.Any(d => d.Count > CostIn(cost, d.Item))) return false;
                    // Work never passes the end of the parts paid for: of N parts of T ticks, k paid end at T * k / N.
                    int parts = cost.Sum(c => c.Count), paid = p.Delivered.Sum(d => d.Count);
                    if (p.Progress > (w.M.Building.TicksFor(p.Id) ?? 0) * paid / parts) return false;
                }
            }
            return true;
        });
    }
}
