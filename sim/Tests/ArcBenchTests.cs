using Stillwake.Sim.Building;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using Craft = Stillwake.Sim.Crafting.Craft;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The arc bench (0099): a tool clamped in its one slot takes a module onto its stack and gives it up again; it holds only a
/// tool a module fits; it is not taken apart while it holds one; it crafts the arc branch's crafts, paid by hand, the
/// accumulator among them, which holds three batteries' charge and works wherever a battery does.
/// </summary>
public class ArcBenchTests
{
    private const string Drill = "drill", Arc = "arcModule", Accumulator = "accumulator";
    private static readonly int Belt = Spec.Inventory.Slots + 2;

    /// <summary>
    /// A sealed room with an arc bench, a charging station and a crate; a solar panel out under the stars cabled to the
    /// charging station, its 5 over the 3 it draws.
    /// </summary>
    private static readonly string[] Rows =
    [
        "PPP       ",
        "          ",
        "##########",
        "#...C....#",
        "#WW.C.KK.#",
        "##########",
    ];

    private static Scenario World() => new Scenario(Layout.Parse(Rows, null, [((0, 0), (4, 3))]), doubled: true)
        .GiveItems(0, (Drill, 1), (Arc, 2), ("iron", 10));

    private static int Id(Scenario w, string kind) => w.M.Grid.MachinesOf(kind).Single().Id;
    private static int Bench(Scenario w) => Id(w, "arcBench");
    private static int SlotOf(Scenario w, string item) => w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
    private static int Empty(Scenario w) => w.M.Items.Inventory(0).ToList().FindIndex(s => s == null);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static Stack? Tool(Scenario w) => w.M.Items.Crate(Bench(w))[0];

    private static Scenario Clamp(Scenario w, string item = Drill) =>
        w.Do(new Move(new Slot(SlotOf(w, item)), new Target(Crate: Bench(w)), 1)).Run();

    private static Scenario FitArc(Scenario w) => w.Do(new Fit(new Slot(SlotOf(w, Arc)), Bench(w))).Run();

    [Fact]
    public void FitsTheArcIntoTheDrillOnTheBenchOntoItsStackAndTakesItOutAgain()
    {
        var w = World();
        Clamp(w);
        Assert.Equal(new Stack(Drill, 1), Tool(w));
        FitArc(w);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack(Drill, 1, Module: Arc), Tool(w));
        Assert.Equal(1, w.M.Items.Inventory(0).Count(s => s?.Item == Arc));
        // On the tool's stack, not the bench's: nothing is fitted into the bench, and it boosts nothing.
        Assert.Empty(w.M.Items.FittedTo(Bench(w)));
        Assert.Equal(default, w.M.Items.Boost(Bench(w)));

        // Taken off the bench, the fitted drill hits twice as hard, as any fitted drill (0098).
        int into = Empty(w);
        w.Do(new Move(new Slot(0, Bench(w)), new Target(into), 1)).Run();
        Assert.Equal(new Stack(Drill, 1, Module: Arc), w.M.Items.Inventory(0)[into]);
        Assert.Equal(4, w.M.Items.Damage(0, Drill));

        // Back on the bench and the module taken out into a slot named, then the drill plain again.
        Clamp(w);
        int slot = Empty(w);
        w.Do(new Unfit(Bench(w), Arc, slot)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack(Drill, 1), Tool(w));
        Assert.Equal(new Stack(Arc, 1), w.M.Items.Inventory(0)[slot]);
        // And wherever it fits, with no slot named.
        FitArc(w).Do(new Unfit(Bench(w), Arc)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(2, w.M.Items.Inventory(0).Count(s => s?.Item == Arc));
    }

    [Fact]
    public void FitsAModuleLyingInTheCrateAndKeepsTheFittedToolThroughASave()
    {
        var w = World();
        int crate = Id(w, "crate");
        w.Do(new Move(new Slot(SlotOf(w, Arc)), new Target(3, crate), 1)).Run();
        Clamp(w).Do(new Fit(new Slot(3, crate), Bench(w))).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Items.Crate(crate)[3]);
        w.Reload();
        Assert.Equal(new Stack(Drill, 1, Module: Arc), Tool(w));
    }

    [Fact]
    public void RefusesEachFitUnfitAndMoveForItsReasonLeavingEverythingAsItWas()
    {
        var w = World();
        int bench = Bench(w), arc = SlotOf(w, Arc), iron = SlotOf(w, "iron");
        // Nothing on it yet: no tool to fit into or take out of.
        w.Do(new Fit(new Slot(arc), bench)).Do(new Unfit(bench, Arc)).Run();
        Assert.Equal(["items.noTool", "items.noTool"], Reasons(w));
        w.Refusals.Clear();
        // Only a tool a module fits goes on it: iron and the module itself are refused, put in or swapped in.
        w.Do(new Move(new Slot(iron), new Target(Crate: bench), 1)).Do(new Move(new Slot(arc), new Target(0, bench), 1)).Run();
        Assert.Equal(["items.notTool", "items.notTool"], Reasons(w));
        w.Refusals.Clear();
        Clamp(w);
        w.Do(new Move(new Slot(0, bench), new Target(iron), 1)).Run();
        Assert.Equal(["items.notTool"], Reasons(w));
        w.Refusals.Clear();
        FitArc(w);
        Assert.Empty(w.Refusals);

        string Items() => string.Join(";", w.M.Items.Inventory(0).Select(s => s?.ToString())) + "|" + string.Join(";", w.M.Items.Crate(bench));
        var was = Items();
        arc = SlotOf(w, Arc);
        w.Do(new Fit(new Slot(iron), bench))
            .Do(new Fit(new Slot(arc), bench))
            .Do(new Unfit(bench, "iron"))
            .Do(new Unfit(bench, Arc, iron))
            .Do(new Unfit(bench, Arc, -1))
            // Its other slots are its crafts' outputs, only taken from.
            .Do(new Move(new Slot(iron), new Target(1, bench), 1))
            .Run();
        Assert.Equal(["items.notModule", "items.fitted", "items.notFitted", "items.full", "items.noSlot", "items.takeOnly"], Reasons(w));
        Assert.Equal(was, Items());

        // A plain tool has nothing to take out.
        w.Do(new Unfit(bench, Arc)).Run();
        w.Refusals.Clear();
        w.Do(new Unfit(bench, Arc)).Run();
        Assert.Equal(["items.notFitted"], Reasons(w));
    }

    [Fact]
    public void ABenchHoldingAToolIsNotTakenApartAndDronesNeverServeIt()
    {
        var w = World();
        int bench = Bench(w), cell = w.M.Grid.MachineById(bench)!.Cell;
        Assert.False(w.M.Logistics.Servable(bench));
        Clamp(w).Do(new Deconstruct(cell, "machine")).Run();
        Assert.Equal(["building.notEmpty"], Reasons(w));
        w.Refusals.Clear();
        w.Do(new Move(new Slot(0, bench), new Target(), 1)).Do(new Deconstruct(cell, "machine")).Run();
        Assert.Empty(w.Refusals);
        Assert.Single(w.M.Building.Plans);
    }

    [Fact]
    public void CraftsTheArcBranchPaidByHandAnAccumulatorOfAShardABatteryCopperAndACircuitInTenSeconds()
    {
        var w = World().GiveItems(0, (Spec.Ruins.Shard, 2), ("copper", 2), ("circuit", 3), ("coil", 2), ("frame", 1));
        // A battery holding charge, wound into it: made empty, as a crafted battery is, its charge lost there.
        w.Put(20, new Stack("battery", 1, Spec.Charge.Battery));
        int bench = Bench(w);
        Assert.Equal([(Spec.Ruins.Shard, 1), ("battery", 1), ("copper", 2), ("circuit", 1)], Spec.Crafts.Get(Accumulator).Cost);
        w.Do(new Craft(bench, Accumulator, 1)).Do(new Craft(bench, Arc, 1)).Do(new Craft(bench, Accumulator, 1, FromInputs: true)).Run();
        Assert.Equal(["crafting.noInputs"], Reasons(w));
        Assert.Equal(0, w.M.Items.Totals(0).Where(t => t.Item is "battery" or "copper" or "coil" or "frame" || t.Item == Spec.Ruins.Shard).Sum(t => t.Count));
        // Unpowered, at full speed: a unit in its 200 ticks, the first begun on the tick it was queued.
        Assert.Equal(1000, w.M.Crafting.Progress(bench));
        w.Run(Spec.Crafts.Get(Accumulator).Ticks - 1);
        Assert.Equal(new Stack(Accumulator, 1), w.M.Items.Crate(bench)[1]);
        w.Run(Spec.Crafts.Get(Arc).Ticks);
        Assert.Equal(new Stack(Arc, 1), w.M.Items.Crate(bench)[2]);
        Assert.Empty(w.M.Crafting.Queue(bench));
        // What it made is taken as any station's output; the tool slot never held any of it.
        Assert.Null(Tool(w));
    }

    [Fact]
    public void AnAccumulatorOnTheBeltRunsTheDrillThreeTimesAsLongAsABattery()
    {
        var w = World().Put(Belt, new Stack(Accumulator, 1, Spec.Charge.Accumulator));
        Assert.Equal((Spec.Charge.Accumulator, Spec.Charge.Accumulator), w.M.Items.BeltCharge(0));
        int draw = Spec.Items.Get(Drill).Draw!.Value, ticks = Spec.Charge.Accumulator / Spec.Charge.Scale / draw;
        Assert.Equal(3 * Spec.Charge.Battery / Spec.Charge.Scale / draw, ticks);
        w.Do(new Run(Drill, true)).Run(ticks);
        Assert.Equal(Drill, w.M.Items.Running(0));
        Assert.Equal(0, w.M.Items.Inventory(0)[Belt]!.Charge);
        w.Run();
        Assert.Null(w.M.Items.Running(0));
    }

    [Fact]
    public void AnAccumulatorChargesInTheChargingStationThreeTimesAsLongAsABattery()
    {
        var w = World();
        int charger = Id(w, "charger");
        w.Put(0, new Stack(Accumulator, 1), crate: charger).Run(2);
        int rate = Spec.Machines.Get("charger").Draws!.Value * Spec.Charge.Scale;
        int charge = w.M.Items.Crate(charger)[0]!.Charge;
        Assert.InRange(charge, rate, 2 * rate);
        int left = (Spec.Charge.Accumulator - charge) / rate;
        Assert.Equal(3 * Spec.Charge.Battery / rate, left + charge / rate);
        w.Run(left - 1);
        Assert.True(w.M.Items.Crate(charger)[0]!.Charge < Spec.Charge.Accumulator);
        w.Run(2);
        Assert.Equal(new Stack(Accumulator, 1, Spec.Charge.Accumulator), w.M.Items.Crate(charger)[0]);
    }
}
