using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Sorting the bag or a crate (0008): merged, ordered by group and id, from the first slot.</summary>
public class SortTests
{
    private static readonly int Bag = Inventory.Hotbar;
    private static readonly int CrateSlots = Machines.Get("crate").Slots!.Value;

    /// <summary>A crate at the left and a furnace at the right.</summary>
    private static Scenario Store() => new(Layout.Parse(["#KK.FF#", "#...FF#"]));

    private static int CrateOf(Scenario w) => w.M.Grid.MachineAt(w.At(1, 0))!.Id;
    private static int FurnaceOf(Scenario w) => w.M.Grid.MachineAt(w.At(4, 0))!.Id;

    private static Scenario PutAll(Scenario w, IEnumerable<(int Slot, Stack Stack)> stacks, int peer = 0, int? crate = null)
    {
        foreach (var (slot, stack) in stacks) w.Put(slot, stack, peer, crate);
        return w;
    }

    private static Stack?[] BagOf(Scenario w, int peer = 0) => [.. w.M.Items.Inventory(peer).Take(Inventory.Slots).Skip(Bag)];

    /// <summary>Each item, with each charge, and how many of it, over <paramref name="slots"/>.</summary>
    private static List<(string Item, int Charge, int Count)> Totals(IEnumerable<Stack?> slots) =>
        [.. slots.OfType<Stack>().GroupBy(s => (s.Item, s.Charge)).Select(g => (g.Key.Item, g.Key.Charge, g.Sum(s => s.Count)))
            .OrderBy(t => t.Item, StringComparer.Ordinal).ThenBy(t => t.Charge)];

    private static void Same(IEnumerable<Stack?> want, IEnumerable<Stack?> got) => Assert.Equal(want.ToList(), got.ToList());

    [Fact]
    public void MergesHalfEmptyStacksOfTheBagUpToTheirLimitAndFillsItFromItsFirstSlot()
    {
        var w = PutAll(new Scenario(Layout.Empty),
            [(12, new Stack("scrap", 100)), (20, new Stack("scrap", 200)), (30, new Stack("scrap", 450)), (15, new Stack("iron", 5)), (35, new Stack("wire", 7))]);
        w.Do(new Sort()).Run();
        Assert.Empty(w.Refusals);
        Stack?[] want = [new("iron", 5), new("scrap", 500), new("scrap", 250), new("wire", 7)];
        Same([.. want, .. new Stack?[Inventory.Slots - Bag - want.Length]], BagOf(w));
    }

    [Fact]
    public void OrdersToolsThenWearablesThenMaterialsAndPartsThenSalvageThenTheRestEachByItsId()
    {
        string[] mixed = ["supplyPod", "scrapChunk", "wire", "jetpack", "harpoon", "plate", "fireAxe", "battery", "rock", "drill"];
        var w = PutAll(new Scenario(Layout.Empty), mixed.Select((item, i) => (Inventory.Slots - 1 - 2 * i, new Stack(item, 1))));
        w.Do(new Sort()).Run();
        Assert.Equal(
            ["drill", "fireAxe", "harpoon", "battery", "jetpack", "plate", "wire", "rock", "scrapChunk", "supplyPod"],
            BagOf(w).Take(mixed.Length).Select(s => s!.Item));
        Assert.All(BagOf(w).Skip(mixed.Length), s => Assert.Null(s));
    }

    [Fact]
    public void LeavesTheHotbarAndWhatIsWornAsTheyAre()
    {
        var w = PutAll(new Scenario(Layout.Empty), [(0, new Stack("scrap", 3)), (4, new Stack("wire", 1)), (8, new Stack("scrap", 2)), (25, new Stack("scrap", 10))])
            .Wear(0, "jetpack").Put(Inventory.Slots + 2, new Stack("battery", 1, 7000));
        var before = w.M.Items.Inventory(0).ToArray();
        w.Do(new Sort()).Run();
        var after = w.M.Items.Inventory(0);
        Same(before.Take(Bag), after.Take(Bag));
        Same(before.Skip(Inventory.Slots), after.Skip(Inventory.Slots));
        // The bag's scrap is not merged into the hotbar's: only the bag moves.
        Assert.Equal(new Stack("scrap", 10), after[Bag]);
    }

    [Fact]
    public void SortsEverySlotOfACrateFromItsFirst()
    {
        var w = Store();
        int crate = CrateOf(w);
        PutAll(w, [(26, new Stack("iron", 40)), (3, new Stack("scrapChunk", 4)), (10, new Stack("iron", 470)), (0, new Stack("planks", 9)), (5, new Stack("scrapChunk", 9))], crate: crate);
        w.Do(new Sort(crate)).Run();
        Assert.Empty(w.Refusals);
        Stack?[] want = [new("iron", 500), new("iron", 10), new("planks", 9), new("scrapChunk", 10), new("scrapChunk", 3)];
        Same([.. want, .. new Stack?[CrateSlots - want.Length]], w.M.Items.Crate(crate));
        // The sender's own bag is not touched by a crate's sort.
        Assert.Empty(w.Stored().Inventories);
    }

    [Fact]
    public void BatteriesHoldingOtherChargeNeverMergeAndKeepWhatTheyHoldTheFullestFirst()
    {
        var w = PutAll(new Scenario(Layout.Empty),
        [
            (30, new Stack("battery", 1, 1000)), (11, new Stack("battery", 1, Charge.Battery)), (20, new Stack("battery", 1)),
            (22, new Stack("battery", 1, 1000)), (14, new Stack("drone", 1, 5000)), (16, new Stack("drone", 1, 9000)),
        ]);
        var was = Totals(w.M.Items.Inventory(0));
        w.Do(new Sort()).Run();
        Assert.Equal(was, Totals(w.M.Items.Inventory(0)));
        Same(
            [new("battery", 1, Charge.Battery), new("battery", 1, 1000), new("battery", 1, 1000), new("battery", 1), new("drone", 1, 9000), new("drone", 1, 5000)],
            BagOf(w).Take(6));
    }

    [Fact]
    public void RefusesAMachineThatIsNoCrateAndOneThatIsNotThereChangingNothing()
    {
        var w = Store();
        int furnace = FurnaceOf(w);
        w.Put(0, new Stack("planks", 3), crate: furnace).Put(3, new Stack("scrap", 4), crate: furnace);
        var before = w.M.Items.Crate(furnace).ToArray();
        w.Do(new Sort(furnace)).Do(new Sort(99)).Run();
        Assert.Equal(["items.notCrate", "items.noCrate"], w.Refusals.Select(r => r.Reason));
        Same(before, w.M.Items.Crate(furnace));
    }

    [Fact]
    public void TwoPlayersSortingOneCrateInOneTickApplyInTurnAndTwoWorldsAgree()
    {
        Scenario Played()
        {
            var w = Store();
            int crate = CrateOf(w);
            PutAll(w, [(7, new Stack("wire", 30)), (2, new Stack("wire", 90)), (19, new Stack("gear", 1))], crate: crate);
            w.Put(20, new Stack("scrap", 5), peer: 1).Put(10, new Stack("scrap", 5), peer: 1);
            return w.Do(new Sort(crate) { From = 0 }).Do(new Sort(crate) { From = 1 }).Do(new Sort() { From = 1 }).Run();
        }
        var a = Played();
        Assert.Empty(a.Refusals);
        // The second sort finds the crate sorted already and changes nothing.
        Stack?[] want = [new("gear", 1), new("wire", 100), new("wire", 20)];
        Same([.. want, .. new Stack?[CrateSlots - want.Length]], a.M.Items.Crate(CrateOf(a)));
        Assert.Equal(new Stack("scrap", 10), a.M.Items.Inventory(1)[Bag]);
        Assert.Equal(a.World.Save(), Played().World.Save());
    }

    [Fact]
    public void AClientShowsItsOwnSortAtOnceAndAMoveAfterItActsOnTheSortedSlots()
    {
        var w = Store();
        int crate = CrateOf(w);
        PutAll(w, [(5, new Stack("wire", 30)), (9, new Stack("wire", 20))], crate: crate);
        var shown = w.M.Items.AfterMoves(0, [new Sort(crate), new Move(new Items.Place(0, crate), new Target(Bag), 50)]);
        Assert.Equal(new Stack("wire", 50), shown.Inventory[Bag]);
        Assert.All(shown.Crates.Single().Slots, s => Assert.Null(s));
        // A refused sort shows nothing.
        Assert.Empty(w.M.Items.AfterMoves(0, [new Sort(FurnaceOf(w))]).Crates);
    }

    /// <summary>The fuzz draws sorts of the bag and of a crate, taken, and of a furnace and of no machine, refused.</summary>
    [Fact]
    public void TheFuzzDrawsSortsTakenAndRefused()
    {
        var w = Store();
        w.GiveItems(0, ("scrap", 700), ("iron", 30), ("planks", 12)).FillCrate(CrateOf(w), ("wire", 150)).Put(0, new Stack("planks", 2), crate: FurnaceOf(w));
        var rng = new Rng(new RngState { S = 3 });
        var taken = new List<Sort>();
        for (int i = 0; i < 3000; i++)
        {
            if (w.M.Items.Fuzz(rng, 0) is not { } c) continue;
            int refused = w.Refusals.Count;
            w.Do(c).Run();
            if (c is Sort s && w.Refusals.Count == refused) taken.Add(s);
        }
        Assert.Contains(taken, s => s.Crate == null);
        Assert.Contains(taken, s => s.Crate == CrateOf(w));
        Assert.Contains(w.Refusals, r => r.Command is Sort && r.Reason == "items.notCrate");
        Assert.Contains(w.Refusals, r => r.Command is Sort && r.Reason == "items.noCrate");
    }

    private static readonly string[] Drawn = ["scrap", "iron", "wire", "rock", "scrapChunk", "battery", "drone", "fireAxe", "jetpack", "supplyPod", "circuit"];

    /// <summary>Any inventory: a stack of any of <see cref="Drawn"/> in any slot, from one to its limit, charge where it holds one.</summary>
    private static Gen<(int Slot, Stack Stack)[]> Inventories() =>
        (from slot in Gen.Choose(0, Inventory.Slots - 1)
         from item in Gen.Elements(Drawn)
         from count in Gen.Choose(1, Content.Items.Get(item).Stack)
         from charge in Gen.Elements(0, 1000, 5000, Charge.Battery)
         select (slot, new Stack(item, count, Content.Items.Get(item).Capacity == null ? 0 : charge))).ArrayOf();

    [Property(MaxTest = 300)]
    public Property SortingKeepsEveryItemAndChargeLeavesTheHotbarAndIsDoneInOneGo() =>
        Prop.ForAll(Inventories().ToArbitrary(), stacks =>
        {
            var w = PutAll(new Scenario(Layout.Empty), stacks).Wear(0, "battery");
            var before = w.M.Items.Inventory(0).ToArray();
            w.Do(new Sort()).Run();
            var after = w.M.Items.Inventory(0).ToArray();
            var bag = after.Take(Inventory.Slots).Skip(Bag).ToArray();
            int filled = bag.Count(s => s != null);
            var groups = bag.OfType<Stack>().GroupBy(s => (s.Item, s.Charge));
            bool ordered = bag.Take(filled).Zip(bag.Skip(1).Take(filled - 1)).All(p =>
                Content.Items.Get(p.First!.Item).Group < Content.Items.Get(p.Second!.Item).Group
                || (Content.Items.Get(p.First.Item).Group == Content.Items.Get(p.Second.Item).Group && string.CompareOrdinal(p.First.Item, p.Second.Item) <= 0));
            w.Do(new Sort()).Run();
            return w.Refusals.Count == 0
                && Totals(before).SequenceEqual(Totals(after))
                && before.Take(Bag).SequenceEqual(after.Take(Bag))
                && before.Skip(Inventory.Slots).SequenceEqual(after.Skip(Inventory.Slots))
                // From the first slot of the bag, with no gaps, in order, and only the last of a kind short of full.
                && bag.Take(filled).All(s => s != null) && ordered
                && groups.All(g => g.Count(s => s.Count < Content.Items.Get(s.Item).Stack) <= 1 && g.All(s => s.Count >= 1))
                && w.M.Items.Inventory(0).SequenceEqual(after);
        });
}
