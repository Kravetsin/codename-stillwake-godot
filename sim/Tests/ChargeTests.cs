using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>Charge on the item (0053): batteries, the charging station that fills them, and the belt an electric tool drains.</summary>
public class ChargeTests
{
    private const string Battery = "battery";
    private static readonly int Full = Content.Items.Get(Battery).Capacity!.Value;
    private const int S = Charge.Scale;
    /// <summary>What the station puts in a tick at full power, stored: what it draws.</summary>
    private static readonly int PerTick = Machines.Get("charger").Draws!.Value * S;
    private static readonly int Head = Inventory.Slots, Back = Inventory.Slots + 1, Belt = Inventory.Slots + 2;

    /// <summary>
    /// Nine wide: a solar panel out in space, cabled to a charging station (machine 2) in a room with a crate
    /// (machine 3). With <paramref name="half"/> a generator and three lamps on the same panel: ten drawn of five made.
    /// </summary>
    private static Scenario Station(bool half = false, bool cabled = true)
    {
        string[] rows = half
            ? ["PPP       ", "          ", "##########", "#C.GGLLL.#", "#C.GG.KK.#", "##########"]
            : ["PPP      ", "         ", "#########", "#C.KK...#", "#C......#", "#########"];
        var cables = new List<((int X, int Y) A, (int X, int Y) B)>();
        if (cabled) cables.Add(((0, 0), (1, 3)));
        if (half) cables.AddRange([((0, 0), (3, 3)), ((0, 0), (5, 3)), ((0, 0), (6, 3)), ((0, 0), (7, 3))]);
        var w = new Scenario(Layout.Parse(rows, null, cables), doubled: true);
        Assert.Equal("charger", w.M.Grid.MachineById(Charger)!.Kind);
        return w;
    }

    private const int Charger = 2;
    private static int CrateOf(Scenario w) => w.M.Grid.Machines.Single(m => m.Kind == "crate").Id;

    private static Stack Charged(int charge) => new(Battery, 1, charge);
    private static Stack? InStation(Scenario w) => w.M.Items.Crate(Charger)[0];
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static Scenario Move(Scenario w, Slot src, Target dst, int count = 1, int from = 0) =>
        w.Do(new Move(src, dst, count) { From = from }).Run();

    // Batteries.

    [Fact]
    public void ABatteryIsAStackOfOneWornOnTheBeltHoldingEighteenHundredPowerTicks()
    {
        var spec = Content.Items.Get(Battery);
        Assert.Equal((1, "belt"), (spec.Stack, spec.Equip));
        // What a machine drawing 3 uses in 600 ticks, stored scaled.
        Assert.Equal(1800 * S, Full);
        Assert.Equal(["plate", "coil", "wire", "circuit"], Crafts.Get(Battery).Cost.Select(c => c.Item));
        Assert.DoesNotContain(Battery, Content.Research.StartKnown);
    }

    [Fact]
    public void ABatteryIsCraftedEmpty()
    {
        // At a circuit assembler cabled to a panel, as its tests power one.
        string[] rows = ["PPP      ", "         ", "#########", "#YY.....#", "#YY.....#", "#########"];
        var w = new Scenario(Layout.Parse(rows, null, [((0, 0), (1, 3))]), doubled: true)
            .GiveItems(0, Crafts.Get(Battery).Cost.ToArray());
        int assembler = w.M.Grid.MachinesOf("assembler").Single().Id;
        w.Do(new Stillwake.Sim.Crafting.Craft(assembler, Battery, 1) { From = 0 }).Run(Crafts.Get(Battery).Ticks * 1000 / Content.Stations.Get("assembler").Speed);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack(Battery, 1, 0), w.M.Items.Crate(assembler)[Machines.Get("assembler").Inputs!.Value]);
    }

    [Fact]
    public void BatteriesGoIntoBeltSlotsOnlyEachOfTheThree()
    {
        var w = new Scenario("#KK....#").Put(0, Charged(10)).Put(1, Charged(20)).Put(2, Charged(30));
        Move(w, new Slot(0), new Target(Head));
        Move(w, new Slot(0), new Target(Back));
        Assert.Equal(["items.notEquipment", "items.notEquipment"], Reasons(w));
        for (int i = 0; i < 3; i++) Move(w, new Slot(i), new Target(Belt + i));
        Assert.Equal(2, w.Refusals.Count);
        Assert.Equal([Charged(10), Charged(20), Charged(30)], w.M.Items.Inventory(0).Skip(Belt).Take(3));
    }

    // The charging station.

    [Fact]
    public void TheStationIsOneByTwoDrawsThreeAndIsBoughtInTheTree()
    {
        var spec = Machines.Get("charger");
        // One by two as a map draws it, a character a 16 px cell (0104).
        Assert.Equal((1, 2, 3, 1, true), (spec.Width / 2, spec.Height / 2, spec.Draws, spec.Slots, spec.Charges));
        Assert.Equal(["frame", "coil", "wire", "circuit"], spec.Cost.Select(c => c.Item));
        Assert.DoesNotContain("charger", Content.Research.StartKnown);
        Assert.Equal(600, Full / PerTick);
    }

    [Fact]
    public void FillsAnEmptyBatteryInSixHundredTicksAtFullPowerAndStopsAtFull()
    {
        var w = Station().Put(0, Charged(0), crate: Charger);
        w.Run(599);
        Assert.Equal(Charged(599 * PerTick), InStation(w));
        Assert.True(w.M.Power.Charges(Charger));
        Assert.Equal((5, 3, 1000), (w.M.Power.Networks[0].Supply, w.M.Power.Networks[0].Demand, w.M.Power.Networks[0].Satisfaction));
        w.Run();
        Assert.Equal(Charged(Full), InStation(w));
        // Full, it rests from the next tick: it draws nothing, and the charge holds.
        w.Run(20);
        Assert.Equal(Charged(Full), InStation(w));
        Assert.Equal(0, w.M.Power.Networks[0].Demand);
        Assert.False(w.M.Power.Charges(Charger));
    }

    [Fact]
    public void ChargesAtHalfSpeedOnANetworkGivingHalfOfWhatIsDrawn()
    {
        var w = Station(half: true).Put(0, Charged(0), crate: Charger);
        w.Run(600);
        Assert.Equal((5, 10, 500), (w.M.Power.Networks[0].Supply, w.M.Power.Networks[0].Demand, w.M.Power.Networks[0].Satisfaction));
        Assert.Equal(Charged(Full / 2), InStation(w));
        w.Run(600);
        Assert.Equal(Charged(Full), InStation(w));
    }

    [Fact]
    public void RestsEmptyAndChargesNothingOffAnyNetwork()
    {
        var w = Station();
        w.Run(10);
        Assert.Equal(0, w.M.Power.Networks[0].Demand);
        Assert.False(w.M.Power.Charges(Charger));
        var cut = Station(cabled: false).Put(0, Charged(5), crate: Charger);
        cut.Run(50);
        Assert.Equal(Charged(5), InStation(cut));
        Assert.False(cut.M.Power.Charges(Charger));
    }

    [Fact]
    public void TakesOnlyWhatHoldsChargeRefusingAnythingElsePutOrSwappedIn()
    {
        var w = Station(cabled: false).GiveItems(0, ("scrap", 10)).Put(1, Charged(7));
        Move(w, new Slot(0), new Target(0, Charger), 10);
        Move(w, new Slot(0), new Target(Crate: Charger), 10);
        Assert.Equal(["items.notBattery", "items.notBattery"], Reasons(w));
        Move(w, new Slot(1), new Target(0, Charger));
        Assert.Equal(Charged(7), InStation(w));
        // The scrap onto the battery there, or the battery there onto the scrap: either swap puts scrap in.
        Move(w, new Slot(0), new Target(0, Charger), 10);
        Move(w, new Slot(0, Charger), new Target(0));
        Assert.Equal(["items.notBattery", "items.notBattery", "items.notBattery", "items.notBattery"], Reasons(w));
        Assert.Equal(Charged(7), InStation(w));
        Assert.Equal(new Stack("scrap", 10), w.M.Items.Inventory(0)[0]);
    }

    // Charge goes with the battery.

    [Fact]
    public void ChargeGoesWithTheBatteryThroughMovesSwapsCratesAndTheStation()
    {
        var w = Station(cabled: false).Put(0, Charged(100)).Put(1, Charged(200));
        int crate = CrateOf(w);
        Move(w, new Slot(0), new Target(5));
        Assert.Equal(Charged(100), w.M.Items.Inventory(0)[5]);
        // Two batteries holding different charge swap, rather than refuse as two full stacks of one item would.
        Move(w, new Slot(5), new Target(1));
        Assert.Equal((Charged(100), Charged(200)), (w.M.Items.Inventory(0)[1], w.M.Items.Inventory(0)[5]));
        Move(w, new Slot(1), new Target(Crate: crate));
        Move(w, new Slot(0, crate), new Target(0, Charger));
        Move(w, new Slot(0, Charger), new Target(Belt));
        Assert.Empty(w.Refusals);
        Assert.Equal(Charged(100), w.M.Items.Inventory(0)[Belt]);
        // Onto the inventory as a whole: into its first empty own slot.
        Move(w, new Slot(Belt), new Target(Crate: crate));
        Move(w, new Slot(0, crate), new Target());
        Assert.Equal(Charged(100), w.M.Items.Inventory(0)[0]);
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void ChargeGoesWithTheBatteryDroppedAndPickedUp()
    {
        var w = Station(cabled: false).Put(0, Charged(1234));
        w.Do(new Drop(new Slot(0), 1, w.At(5, 4), 1) { From = 0 }).Run(30);
        var lying = Assert.Single(w.M.Items.Floor);
        Assert.Equal((Battery, 1234), (lying.Item, lying.Charge));
        w.Do(new PickUp(lying.Id) { From = 1 }).Run();
        Assert.Empty(w.M.Items.Floor);
        Assert.Equal(Charged(1234), w.M.Items.Inventory(1)[0]);
    }

    [Fact]
    public void TwoBatteriesLyingInOneCellStayTwo()
    {
        var w = Station(cabled: false).Put(0, Charged(1)).Put(1, Charged(1));
        w.Do(new Drop(new Slot(0), 1, w.At(5, 4), 0) { From = 0 }).Do(new Drop(new Slot(1), 1, w.At(5, 4), 0) { From = 0 }).Run(30);
        Assert.Equal(2, w.M.Items.Floor.Count);
    }

    [Fact]
    public void ChargeIsSavedOnlyWhereThereIsSomeAndLoadsBack()
    {
        var w = Station(cabled: false).Put(0, Charged(321)).Put(1, Charged(0)).Put(Belt, Charged(Full)).Put(0, Charged(9), crate: Charger);
        w.Do(new Drop(new Slot(0), 1, w.At(5, 4), 0) { From = 0 }).Run(30);
        string save = w.World.Save();
        // Stacks holding none write no charge, so a save with no battery is as it was before charge.
        Assert.Equal(3, save.Split("\"charge\"").Length - 1);
        w.Reload();
        Assert.Equal(Charged(0), w.M.Items.Inventory(0)[1]);
        Assert.Equal(Charged(Full), w.M.Items.Inventory(0)[Belt]);
        Assert.Equal(Charged(9), InStation(w));
        Assert.Equal(321, Assert.Single(w.M.Items.Floor).Charge);
        Assert.Equal(save, w.World.Save());
    }

    // The belt, drained by an electric tool.

    [Fact]
    public void DrainsTheFirstBatteryOnTheBeltThatCanPayThenTheNext()
    {
        // Drained in power-ticks, a tool's draw for a tick; held stored scaled.
        var w = new Scenario(Layout.Empty).Put(Belt, Charged(2 * S)).Put(Belt + 1, Charged(10 * S)).Put(0, Charged(Full));
        Assert.Equal((12 * S, 2 * Full), w.M.Items.BeltCharge(0));
        // The first holds too little for 3: the second pays it whole.
        Assert.True(w.M.Items.Drain(0, 3));
        Assert.Equal([Charged(2 * S), Charged(7 * S), null], w.M.Items.Inventory(0).Skip(Belt).Take(3));
        Assert.True(w.M.Items.Drain(0, 1));
        Assert.Equal([Charged(S), Charged(7 * S), null], w.M.Items.Inventory(0).Skip(Belt).Take(3));
        Assert.True(w.M.Items.Drain(0, 7));
        // Neither holds 3 now; the full battery in the bag is not worn, so it pays nothing.
        Assert.False(w.M.Items.Drain(0, 3));
        Assert.Equal([Charged(S), Charged(0), null], w.M.Items.Inventory(0).Skip(Belt).Take(3));
        Assert.Equal(Charged(Full), w.M.Items.Inventory(0)[0]);
        Assert.Equal((S, 2 * Full), w.M.Items.BeltCharge(0));
        Assert.False(w.M.Items.Drain(1, 1));
        Assert.Equal((0, 0), w.M.Items.BeltCharge(1));
    }

    [Fact]
    public void DrainsNothingInCreativeModeAndStillWorks()
    {
        var w = new Scenario(Layout.Empty).Put(Belt, Charged(10));
        w.Do(new SetCreative(true)).Run();
        Assert.True(w.M.Items.Drain(0, 3));
        Assert.True(w.M.Items.Drain(1, 3));
        Assert.Equal(Charged(10), w.M.Items.Inventory(0)[Belt]);
    }

    // Invariants.

    private static readonly Gen<Slot> Places = Gen.Frequency(
        (4, Gen.Choose(0, 4).Select(slot => new Slot(slot))),
        (3, Gen.Choose(Belt, Inventory.Total - 1).Select(slot => new Slot(slot))),
        (1, Gen.Choose(Head, Back).Select(slot => new Slot(slot))),
        (2, Gen.Choose(0, 3).Select(slot => new Slot(slot, 3))),
        (3, Gen.Constant(new Slot(0, Charger))));

    private abstract record Step;
    private sealed record Moving(Move Move) : Step;
    private sealed record Dropping(Drop Drop) : Step;
    private sealed record Picking(int Which, int From) : Step;
    private sealed record Waiting(int Ticks) : Step;

    private static Gen<Step[]> Steps(int width)
    {
        var move = from src in Places
                   from dst in Gen.Frequency((5, Places.Select(p => (Target)p)), (1, Gen.Constant(new Target())), (1, Gen.Constant(new Target(Crate: 3))))
                   from count in Gen.Elements(1, 2, 999)
                   from peer in Gen.Choose(0, 1)
                   select (Step)new Moving(new Move(src, dst, count) { From = peer });
        // At column x of the map's row 4, the room's lower row, doubled (0104).
        var drop = from src in Places
                   from x in Gen.Choose(1, width - 2)
                   from facing in Gen.Choose(-1, 1)
                   from peer in Gen.Choose(0, 1)
                   select (Step)new Dropping(new Drop(src, 1, 2 * 4 * (2 * width) + 2 * x, facing) { From = peer });
        var pick = from which in Gen.Choose(0, 5) from peer in Gen.Choose(0, 1) select (Step)new Picking(which, peer);
        var wait = Gen.Choose(1, 200).Select(t => (Step)new Waiting(t));
        var step = Gen.Frequency((6, move), (2, drop), (2, pick), (1, wait));
        return from n in Gen.Choose(0, 60) from s in step.ArrayOf(n) select s;
    }

    /// <summary>Two peers with batteries of every charge, empty to full, and scrap; one battery in the crate, one in the station.</summary>
    private static Scenario Stocked(bool cabled) =>
        Station(cabled: cabled).GiveItems(0, ("scrap", 40)).GiveItems(1, ("iron", 7))
            .Put(1, Charged(0)).Put(2, Charged(Full)).Put(Belt, Charged(Full - 1), peer: 1).Put(3, Charged(777), peer: 1)
            .Put(0, Charged(Full / 3), crate: 3).Put(0, Charged(5), crate: Charger);

    private static void Play(Scenario w, Step s)
    {
        switch (s)
        {
            case Moving m: w.Do(m.Move).Run(); break;
            case Dropping d: w.Do(d.Drop).Run(); break;
            case Picking p:
                var floor = w.M.Items.Floor;
                w.Do(new PickUp(floor.Count > 0 ? floor[p.Which % floor.Count].Id : 99) { From = p.From }).Run();
                break;
            case Waiting t: w.Run(t.Ticks); break;
        }
    }

    /// <summary>Every stack held anywhere: both inventories, the crate, the station and the floor.</summary>
    private static List<Stack> Everywhere(Scenario w)
    {
        List<Stack?> all =
        [
        .. new[] { 0, 1 }.SelectMany(peer => w.M.Items.Inventory(peer)),
        .. w.M.Items.Crate(3),
        .. w.M.Items.Crate(Charger),
        .. w.M.Items.Floor.Select(f => new Stack(f.Item, f.Count, f.Charge)),
        ];
        return all.OfType<Stack>().ToList();
    }

    [Property(MaxTest = 150)]
    public Property ChargeIsOnlyOnWhatHasACapacityAndNeverPastIt() =>
        Prop.ForAll(Steps(9).ToArbitrary(), steps =>
        {
            var w = Stocked(cabled: true);
            foreach (var s in steps)
            {
                Play(w, s);
                foreach (var stack in Everywhere(w))
                {
                    var capacity = Content.Items.Get(stack.Item).Capacity;
                    Assert.InRange(stack.Charge, 0, capacity ?? 0);
                    if (capacity != null) Assert.Equal(1, stack.Count);
                }
            }
        });

    [Property(MaxTest = 150)]
    public Property MovesDropsAndPickUpsKeepEveryItemAndAllTheCharge() =>
        Prop.ForAll(Steps(9).ToArbitrary(), steps =>
        {
            // No power, so nothing charges: what is there only moves.
            var w = Stocked(cabled: false);
            static (int Charge, List<(string, int)> Items) Totals(Scenario w)
            {
                var all = Everywhere(w);
                var items = all.GroupBy(s => s.Item).Select(g => (g.Key, g.Sum(s => s.Count))).OrderBy(e => e.Key, StringComparer.Ordinal).ToList();
                return (all.Sum(s => s.Charge), items);
            }
            var start = Totals(w);
            foreach (var s in steps)
            {
                Play(w, s);
                var now = Totals(w);
                Assert.Equal(start.Charge, now.Charge);
                Assert.Equal(start.Items, now.Items);
            }
        });
}
