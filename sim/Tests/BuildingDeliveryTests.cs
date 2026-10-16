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

/// <summary>packages/sim/src/modules/building/delivery.test.ts, ported: blueprints paid and built, things taken apart.</summary>
public class BuildingDeliveryTests
{
    /// <summary>What every hero in these tests carries from the start.</summary>
    private const int StockScrap = 360, StockIron = 60, StockPlate = 60, StockGear = 20;

    /// <summary>A world from <paramref name="map"/> with peers 0 to 3 carrying the stock.</summary>
    private static Scenario Stocked(Layout map)
    {
        var w = new Scenario(map);
        foreach (int peer in new[] { 0, 1, 2, 3 }) w.GiveItems(peer, ("scrap", StockScrap), ("iron", StockIron), ("plate", StockPlate), ("gear", StockGear));
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
    /// <summary>The plates an iron wall takes, and the scrap a scrap one does.</summary>
    private static readonly int IronPlates = CostIn(Iron.Cost, "plate"), ScrapUnits = CostIn(Scrap.Cost, "scrap");

    /// <summary>Nine cells wide: cell (x, y) is y * 9 + x.</summary>
    private const string Open = """
        #########
        #.......#
        #.......#
        #########
        """;
    private static int At(int x, int y) => y * 9 + x;

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
    public void BlueprintsTakeWhatTheyLackOnEThenAreBuiltByAsManyTicksOfWorkAsTheyTake()
    {
        var w = PlacePlan(Stocked(Open), At(1, 1), IronWall);
        Press(w, 1);
        Assert.Equal<(string, int)>(Iron.Cost, Delivered(PlanOf(w, 1)));
        Assert.Equal(0, PlanOf(w, 1)!.Progress);
        Assert.Empty(w.M.Building.Lacking(1));
        Assert.Equal(StockPlate - IronPlates, Count(w, "plate"));
        Hold(w, 1, IronWallTicks - 1);
        Assert.Equal(Structure.Empty, w.M.Grid.StructureAt(At(1, 1)));
        Assert.Equal(IronWallTicks - 1, PlanOf(w, 1)!.Progress);
        Press(w, 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(At(1, 1)));
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
        Hold(w, 2, IronWallTicks);
        Assert.Null(w.M.Grid.BackAt(At(1, 1)));
        Press(w, 2);
        Assert.Equal(Iron, w.M.Grid.BackAt(At(1, 1)));
        Assert.Equal(StockPlate - IronPlates, Count(w, "plate"));
    }

    [Fact]
    public void BlueprintsTakePartFromAHeroCarryingTooLittleAndTheRestFromAnother()
    {
        var w = new Scenario(Open);
        w.GiveItems(5, ("plate", 1));
        w.GiveItems(6, ("plate", 9), ("gear", 9));
        PlacePlan(w, At(1, 1), Door);
        Press(w, 1, 5);
        Assert.Equal([("plate", 1)], Delivered(PlanOf(w, 1)));
        Assert.Equal(0, Count(w, "plate", 5));
        // Still lacking: the hero who brought what it had cannot work it yet.
        Assert.Equal("items.notEnough", w.M.Building.WorkProblem(1, 5));
        Press(w, 1, 6);
        Hold(w, 1, Builds.Get("door").BuildTicks, 5);
        Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(At(1, 1)));
        // The second paid only what was still lacking.
        var door = Builds.Get("door").Cost;
        Assert.Equal((9 - (CostIn(door, "plate") - 1), 9 - CostIn(door, "gear")), (Count(w, "plate", 6), Count(w, "gear", 6)));
    }

    [Fact]
    public void BlueprintsBuildAMachineOverItsWholeFootprintOnceItIsPaidAndWorked()
    {
        var w = PlacePlan(Stocked(Open), At(1, 1), Tank);
        Hold(w, 1, 1 + Machines.Get("tank").BuildTicks);
        var machine = Assert.Single(w.M.Grid.Machines);
        Assert.Equal(("tank", At(1, 1)), (machine.Kind, machine.Cell));
        foreach (int cell in new[] { At(1, 1), At(2, 1), At(1, 2), At(2, 2) })
            Assert.Equal(machine.Id, w.M.Grid.MachineAt(cell)?.Id);
        Assert.Equal(StockPlate - CostIn(Machines.Get("tank").Cost, "plate"), Count(w, "plate"));
    }

    [Fact]
    public void BlueprintsAreBuiltTwiceAsFastByTwoHeroesAndKeepTheWorkDoneWhenTheyStop()
    {
        var w = PlacePlan(Stocked(Open), At(1, 1), IronWall);
        Press(w, 1);
        int half = IronWallTicks / 2;
        for (int i = 0; i < half; i++) w.Do(new Work(1) { From = 0 }).Do(new Work(1) { From = 1 }).Run();
        Assert.Equal(2 * half, PlanOf(w, 1)!.Progress);
        w.Run(50);
        Assert.Equal(2 * half, PlanOf(w, 1)!.Progress);
        Hold(w, 1, IronWallTicks - 2 * half);
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(At(1, 1)));
        // Only the hero who delivered paid.
        Assert.Equal(StockPlate, Count(w, "plate", 1));
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
    public void BlueprintsLetWhatWasDeliveredFallWhereTheyStoodWhenCancelledAMarkDropsNothing()
    {
        var w = new Scenario(Open);
        w.GiveItems(5, ("plate", 1));
        PlacePlan(w, At(1, 1), Door);
        Press(w, 1, 5);
        w.Do(new CancelPlan(1) { From = 2 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Building.Plans);
        // Fallen from the blueprint's cell to the floor below it.
        var lying = Assert.Single(w.M.Items.Floor);
        Assert.Equal(("plate", 1, At(1, 2)), (lying.Item, lying.Count, lying.Cell));
        w.Do(new Deconstruct(At(0, 1), "structure")).Run();
        w.Do(new CancelPlan(2)).Run();
        Assert.Equal(1, Lying(w, "plate"));
    }

    /// <summary>The conservation property's counterexample: what lies in space leaves the grid, as Items' floor rules say.</summary>
    [Fact]
    public void BlueprintsCancelledInTheHullLetWhatWasDeliveredRiseOutOfItIntoSpaceAndDriftAway()
    {
        var w = Stocked(Open).Do(new Deconstruct(At(0, 0), "structure")).Run();
        Hold(w, 1, Costs.ApartTicks(Scrap.BuildTicks));
        PlacePlan(w, At(0, 3), new Placement("conduit", "pipe"));
        Press(w, 2);
        w.Do(new CancelPlan(2)).Run(3);
        // Up the hull's left column, out at its open corner, which is space.
        Assert.Equal(At(0, 0), Assert.Single(w.M.Items.Floor).Cell);
        Assert.Equal(RoomsModule.Space, w.M.Rooms.RoomAt(At(0, 0)));
        // Drifting back a cell's width at most takes it past the grid's left edge, gone.
        w.Run(1000 / Content.Floor.SpaceDrift);
        Assert.Empty(w.M.Items.Floor);
        Assert.Equal(StockScrap + ScrapUnits - CostIn(Builds.Get("pipe").Cost, "scrap"), Count(w, "scrap"));
    }

    [Fact]
    public void BlueprintsAreWorkedInCreativeModeAsTheyAreAnOlderOneTooTakingNothing()
    {
        var w = new Scenario(Open);
        w.GiveItems(5, ("plate", 1));
        PlacePlan(w, At(1, 1), Door);
        Press(w, 1, 5);
        w.Do(new SetCreative(true)).Run();
        PlacePlan(w, At(3, 1), IronWall);
        Assert.Empty(w.M.Building.Lacking(2));
        Hold(w, 1, Builds.Get("door").BuildTicks, 7);
        Hold(w, 2, IronWallTicks, 7);
        Assert.Empty(w.Refusals);
        Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(At(1, 1)));
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(At(3, 1)));
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
        w.GiveItems(0, ("gear", Content.Items.Get("gear").Stack - StockGear));
        w.GiveItems(0, ("scrap", (Inventory.Slots - 3) * Content.Items.Get("scrap").Stack - StockScrap));
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
    public void TakesAMachineApartFromAnyCellOfItsMarkAndAPipeOutOfAWall()
    {
        var w = Stocked(Layout.Parse(["#########", "#TT.....#", "#TT.....#", "#########"]));
        w.Do(new Deconstruct(At(2, 2), "machine")).Run();
        Hold(w, 1, Costs.ApartTicks(Machines.Get("tank").BuildTicks));
        Assert.Empty(w.M.Grid.Machines);
        Assert.Equal(StockPlate + CostIn(Machines.Get("tank").Cost, "plate"), Count(w, "plate"));
        PlacePlan(w, At(0, 1), new Placement("conduit", "pipe"));
        Hold(w, 2, 1 + Builds.Get("pipe").BuildTicks);
        w.Do(new Deconstruct(At(0, 1), "conduit")).Run();
        Hold(w, 3, Costs.ApartTicks(Builds.Get("pipe").BuildTicks));
        Assert.Equal(Conduit.None, w.M.Grid.ConduitAt(At(0, 1)));
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(At(0, 1)));
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

    private static readonly string[] Counted = ["scrap", "iron", "plate", "gear"];

    /// <summary>What the heroes carry, what lies on the floor and what blueprints were given, of <paramref name="item"/>.</summary>
    private static int Held(Scenario w, string item) =>
        Enumerable.Range(0, 5).Sum(peer => Count(w, item, peer)) + Lying(w, item)
        + w.M.Building.Plans.Sum(p => p.Delivered.Where(s => s.Item == item).Sum(s => s.Count));

    /// <summary>What is built of <paramref name="item"/> on the 36 cells of the map: what taking it all apart would give.</summary>
    private static int Built(Scenario w, string item)
    {
        int total = 0;
        var grid = w.M.Grid;
        for (int c = 0; c < 36; c++)
        {
            if (grid.WallAt(c) is { } wall) total += CostIn(wall.Cost, item);
            if (grid.StructureAt(c) is Structure.DoorClosed or Structure.DoorOpen) total += CostIn(Builds.Get("door").Cost, item);
            if (grid.ConduitAt(c) == Conduit.Pipe) total += CostIn(Builds.Get("pipe").Cost, item);
        }
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
    public Property ConservesItemsAcrossInventoriesTheFloorAndBlueprintsDeliveringNoMoreThanACost()
    {
        var placement = Gen.Elements(IronWall, Placement.Wall("scrap"), Door, new Placement("conduit", "pipe"), Tank);
        // The hull's cells too: taken apart, it lets what lies about out into space.
        var cell = Gen.Choose(0, 35);
        var sender = Gen.Choose(0, 4);
        var id = Gen.Frequency((3, Gen.Choose(1, 5)), (2, Gen.Choose(1, 20)));
        var step = Gen.OneOf(
            from c in cell from p in placement select new Step(c, p, null, 0, 0, 0),
            from c in cell from l in Gen.Elements("structure", "conduit", "machine") select new Step(c, null, l, 0, 0, 0),
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
                }
            }
            return true;
        });
    }
}
