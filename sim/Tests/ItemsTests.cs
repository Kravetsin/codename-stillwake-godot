using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Items;
using Xunit;
using Cost = System.Collections.Generic.IReadOnlyList<(string Item, int Count)>;

namespace Stillwake.Sim.Tests;

public sealed record Buy(Cost Cost) : Command;
public sealed record Refund(Cost Cost) : Command;

/// <summary>Pays and refunds through items as building does, refusing what it cannot; keeps receipts.</summary>
public sealed class Trader : Module<Trader.Receipts>
{
    public sealed class Receipts
    {
        public List<Cost> Paid { get; } = [];
    }

    public override string Id => "trader";
    public IReadOnlyList<Cost> Paid => State.Paid;

    public Trader(IItemsQueries items, IItemsOps ops)
    {
        Handles<Buy>(c => items.ShortOf(c.From, c.Cost), c => State.Paid.Add(ops.Take(c.From, c.Cost)));
        Handles<Refund>(c => items.FullFor(c.From, c.Cost), c => ops.Give(c.From, c.Cost));
    }

    protected override Receipts Init(Layout layout) => new();
}

/// <summary>packages/sim/src/modules/items/items.test.ts, ported.</summary>
public class ItemsTests
{
    private static readonly string[] All = ["scrap", "iron", "canister"];
    private const int Scrap = 360, Iron = 60, Canisters = 3;
    private static int StackOf(string item) => Content.Items.Get(item).Stack;
    private static readonly int CrateSlots = Machines.Get("crate").Slots!.Value;

    private static Items.Place Own(int slot) => new(slot);
    private static Items.Place In(int crate, int slot) => new(slot, crate);

    /// <summary>A world of the registered modules and a <see cref="Trader"/>, read back through <paramref name="trader"/>.</summary>
    private static Scenario Trading(Layout layout, out Func<Trader> trader)
    {
        Trader? made = null;
        trader = () => made!;
        return new Scenario(layout, with: m => [made = new Trader(m.Items, m.Items)]);
    }

    private static int Count(Scenario w, string item, int peer = 0) =>
        w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);

    private static int[] Totals(Scenario w, int peer = 0) => All.Select(item => Count(w, item, peer)).ToArray();
    private static Stack?[] Empty(int n) => new Stack?[n];

    /// <summary>A world with no map, peer 0 carrying scrap and iron.</summary>
    private static Scenario Stocked(out Func<Trader> trader) =>
        Trading(Layout.Empty, out trader).GiveItems(0, ("scrap", Scrap), ("iron", Iron));

    private static Scenario Stocked() => Stocked(out _);

    /// <summary>Two crates, machines 1 and 2: the first holds scrap, iron and canisters, the second nothing.</summary>
    private static Scenario Crates() =>
        new Scenario("#KK.KK#").FillCrate(1, ("scrap", Scrap), ("iron", Iron), ("canister", Canisters));

    private static Scenario DoMove(Scenario w, Items.Place src, Target dst, int n, int from = 0) =>
        w.Do(new Move(src, dst, n) { From = from }).Run();

    private static IReadOnlyList<Stack?> Crate(Scenario w, int id) => w.M.Items.Crate(id);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    // Inventories.

    [Fact]
    public void InventoriesStartEmptyAndNoneIsKeptUntilItChanges()
    {
        var w = new Scenario(Layout.Empty);
        Assert.Equal(Empty(Inventory.Total), w.M.Items.Inventory(3));
        Assert.False(w.M.Items.Creative);
        Assert.Empty(w.Stored().Inventories);
        Assert.Empty(w.Stored().Crates);
    }

    [Fact]
    public void InventoriesPayFromTheSendersInventoryAloneKeptFromThenOnInPeerOrder()
    {
        var w = Stocked(out var trader).GiveItems(4, ("scrap", Scrap), ("iron", Iron));
        w.GiveItems(2, ("iron", Iron));
        w.Do(new Buy([("scrap", 10), ("iron", 5)]) { From = 4 })
            .Do(new Buy([("iron", 1)]) { From = 2 })
            .Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(new[] { Scrap - 10, Iron - 5, 0 }, Totals(w, 4));
        Assert.Equal(new[] { 0, Iron - 1, 0 }, Totals(w, 2));
        Assert.Equal(new[] { Scrap, Iron, 0 }, Totals(w, 0));
        Assert.Equal(new[] { 0, 2, 4 }, w.Stored().Inventories);
        Assert.Equal(2, trader().Paid.Count);
        Assert.Equal(new[] { ("scrap", 10), ("iron", 5) }, trader().Paid[0]);
        Assert.Equal(new[] { ("iron", 1) }, trader().Paid[1]);
    }

    [Fact]
    public void InventoriesPayFromTheLastStacksFirstSoTheFirstSlotsAreTheLastToEmpty()
    {
        var w = Stocked()
            .Do(new Refund([("scrap", StackOf("scrap"))])).Run()
            .Do(new Buy([("scrap", StackOf("scrap") - 1)])).Run();
        // The refund topped up the first stack and spilled into a slot after the iron.
        Assert.Equal(new Stack?[] { new("scrap", Scrap + 1), new("iron", Iron), null }, w.M.Items.Inventory(0).Take(3));
    }

    [Fact]
    public void InventoriesSpendDownToNothingAndRefuseOneUnitMoreWithoutAChange()
    {
        var w = Stocked()
            .Do(new Buy([("iron", Iron)])).Run()
            .Do(new Buy([("scrap", 1), ("iron", 1)])).Run();
        Assert.Equal(new[] { "items.notEnough" }, Reasons(w));
        Assert.Equal(new[] { Scrap, 0, 0 }, Totals(w));
        Assert.Null(w.M.Items.Inventory(0)[1]);
    }

    [Fact]
    public void InventoriesRefuseACostTheyCanPayOnlyInPart()
    {
        var w = Stocked().Do(new Buy([("scrap", 1), ("canister", 1)])).Run();
        Assert.Equal(new[] { "items.notEnough" }, Reasons(w));
        Assert.Equal(new[] { Scrap, Iron, 0 }, Totals(w));
    }

    [Fact]
    public void InventoriesTakeRefundsOntoStacksOfTheItemFirstThenIntoTheFirstEmptySlots()
    {
        var w = Stocked().Do(new Refund([("canister", StackOf("canister") + 1), ("iron", 3)])).Run();
        Assert.Equal(
            new Stack?[]
            {
                new("scrap", Scrap), new("iron", Iron + 3), new("canister", StackOf("canister")), new("canister", 1), null,
            },
            w.M.Items.Inventory(0).Take(5));
    }

    [Fact]
    public void InventoriesRefuseARefundThatDoesNotFitCountingRoomOnTheStacksThereAre()
    {
        int scrapRoom = StackOf("scrap") - Scrap, ironRoom = StackOf("iron") - Iron, slotsLeft = Inventory.Slots - 2;
        var w = Stocked().Do(new Refund([("canister", slotsLeft * StackOf("canister"))])).Run();
        Assert.Equal("items.full", w.M.Items.FullFor(0, [("canister", 1)]));
        Assert.Null(w.M.Items.FullFor(0, [("scrap", scrapRoom), ("iron", ironRoom)]));
        w.Do(new Refund([("scrap", scrapRoom), ("iron", ironRoom + 1)])).Run();
        Assert.Equal(new[] { "items.full" }, Reasons(w));
        Assert.Equal(new[] { Scrap, Iron, slotsLeft * StackOf("canister") }, Totals(w));
    }

    [Fact]
    public void InventoriesTakeNothingInCreativeModeAndStillGive()
    {
        var w = Stocked(out var trader)
            .Do(new SetCreative(true))
            .Do(new Buy([("iron", Iron + 50), ("canister", 3)]))
            .Do(new Refund([("scrap", 2)]))
            .Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(new[] { Scrap + 2, Iron, 0 }, Totals(w));
        // Nothing was paid, so a refund of what was paid gives nothing back.
        Assert.Empty(Assert.Single(trader().Paid));
        Assert.Null(w.M.Items.ShortOf(0, [("iron", 9999)]));
        w.Do(new SetCreative(false)).Run();
        Assert.Equal("items.notEnough", w.M.Items.ShortOf(0, [("iron", 9999)]));
    }

    [Fact]
    public void InventoriesKeepNoInventoryForAChangeOfNothing()
    {
        var w = Trading(Layout.Empty, out _)
            .Do(new Buy([("scrap", 0)]))
            .Do(new Refund([]))
            .Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.Stored().Inventories);
    }

    /// <summary>A fractional amount cannot be written in C#: an amount is an int, so only a negative one is a bug to catch.</summary>
    [Fact]
    public void InventoriesTreatABrokenAmountAsABugNotARefusalInCreativeModeToo()
    {
        var w = Trading(Layout.Empty, out _);
        Assert.Contains("not a whole amount", Assert.Throws<ArgumentException>(() => w.M.Items.ShortOf(0, [("scrap", -1)])).Message);
        Assert.Contains("not a whole amount", Assert.Throws<ArgumentException>(() => w.M.Items.FullFor(0, [("scrap", -1)])).Message);
        w.Do(new SetCreative(true)).Run();
        Assert.Contains("not a whole amount", Assert.Throws<ArgumentException>(() => w.M.Items.ShortOf(0, [("iron", -2)])).Message);
        Assert.Contains("not a whole amount", Assert.Throws<ArgumentException>(() => w.Do(new Buy([("scrap", -1)])).Run()).Message);
    }

    private sealed record Step(string Type, Cost Cost, int From, bool On);

    private static readonly Gen<Cost> Costs =
        from amounts in Gen.OneOf(Gen.Choose(0, 1500).Select(n => (int?)n), Gen.Constant<int?>(null)).ArrayOf(All.Length)
        select (Cost)All.Zip(amounts).Where(p => p.Second != null).Select(p => (p.First, p.Second!.Value)).ToList();

    [Property(MaxTest = 100)]
    public Property InventoriesKeepEveryStackWithinItsLimitAndEveryChangeExactlyWhatWasPaidOrGiven()
    {
        var step = Gen.OneOf(
            from type in Gen.Elements("buy", "refund")
            from cost in Costs
            from peer in Gen.Choose(0, 2)
            select new Step(type, cost, peer, false),
            from isOn in Gen.Elements(true, false) select new Step("creative", [], 0, isOn));
        var steps = from n in Gen.Choose(0, 60) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = Stocked();
            foreach (var s in all)
            {
                var before = Enumerable.Range(0, 3).Select(peer => Totals(w, peer)).ToArray();
                int refused = w.Refusals.Count;
                Command c = s.Type switch
                {
                    "creative" => new SetCreative(s.On),
                    "buy" => new Buy(s.Cost) { From = s.From },
                    _ => new Refund(s.Cost) { From = s.From },
                };
                w.Do(c).Run();
                bool accepted = w.Refusals.Count == refused;
                for (int peer = 0; peer < 3; peer++)
                {
                    var slots = w.M.Items.Inventory(peer);
                    Assert.Equal(Inventory.Total, slots.Count);
                    foreach (var slot in slots)
                        if (slot != null) Assert.InRange(slot.Count, 1, StackOf(slot.Item));
                    int sign = s.Type == "creative" || !accepted || peer != s.From ? 0
                        : s.Type == "refund" ? 1
                        : w.M.Items.Creative ? 0 : -1;
                    var change = All.Select(i => s.Cost.Where(e => e.Item == i).Sum(e => e.Count) * sign);
                    Assert.Equal(before[peer].Zip(change, (a, b) => a + b), Totals(w, peer));
                }
            }
        });
    }

    // Crates.

    [Fact]
    public void CratesStartEmptyEverySlotOfAMachineThatHoldsItemsAndNoneOfOneThatHoldsNone()
    {
        var w = new Scenario("#KK.TT#\n#...TT#");
        Assert.Equal(Empty(CrateSlots), Crate(w, 1));
        Assert.Empty(Crate(w, 2));
        Assert.Empty(w.Stored().Crates);
    }

    [Fact]
    public void CratesGiveAWholeStackOrPartOfOneToASlotOfAnInventoryAndTakeSomeBack()
    {
        var w = Crates();
        DoMove(w, In(1, 0), new Target(4), 999);
        DoMove(w, In(1, 1), new Target(0), 25, 3);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack("scrap", Scrap), w.M.Items.Inventory(0)[4]);
        Assert.Equal(new Stack("iron", 25), w.M.Items.Inventory(3)[0]);
        Assert.Equal(new Stack?[] { null, new("iron", Iron - 25) }, Crate(w, 1).Take(2));
        DoMove(w, Own(4), new Target(5, 1), 10);
        Assert.Equal(new Stack("scrap", 10), Crate(w, 1)[5]);
        Assert.Equal(new Stack("scrap", Scrap - 10), w.M.Items.Inventory(0)[4]);
    }

    [Fact]
    public void CratesStackOntoTheSameItemUpToItsLimitLeavingTheRestWhereItWas()
    {
        var w = Crates().GiveItems(0, ("scrap", 400));
        DoMove(w, In(1, 0), new Target(0), 999);
        Assert.Equal(new Stack("scrap", StackOf("scrap")), w.M.Items.Inventory(0)[0]);
        Assert.Equal(new Stack("scrap", 400 + Scrap - StackOf("scrap")), Crate(w, 1)[0]);
        DoMove(w, In(1, 0), new Target(0), 1);
        Assert.Equal(new[] { "items.full" }, Reasons(w));
    }

    [Fact]
    public void CratesSwapAWholeStackWithAnotherItemAndRefuseAPartOfOneOntoIt()
    {
        var w = Crates().GiveItems(0, ("canister", 2));
        DoMove(w, In(1, 0), new Target(0), 100);
        Assert.Equal(new[] { "items.full" }, Reasons(w));
        DoMove(w, In(1, 0), new Target(0), 999);
        Assert.Equal(new Stack("scrap", Scrap), w.M.Items.Inventory(0)[0]);
        Assert.Equal(new Stack("canister", 2), Crate(w, 1)[0]);
    }

    [Fact]
    public void CratesWithNoSlotFillTheOtherSidesStacksOfTheItemThenItsEmptySlots()
    {
        var w = Crates().GiveItems(0, ("scrap", 200));
        DoMove(w, Own(0), new Target(Crate: 1), 999);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack("scrap", StackOf("scrap")), Crate(w, 1)[0]);
        Assert.Equal(new Stack("scrap", 200 + Scrap - StackOf("scrap")), Crate(w, 1)[3]);
        Assert.Null(w.M.Items.Inventory(0)[0]);
        DoMove(w, In(1, 2), new Target(), 999, 1);
        Assert.Equal(new Stack("canister", Canisters), w.M.Items.Inventory(1)[0]);
    }

    [Fact]
    public void CratesMoveWithinAnInventoryAndBetweenTwoCrates()
    {
        var w = Crates().GiveItems(0, ("iron", 7));
        DoMove(w, Own(0), new Target(8), 999);
        DoMove(w, In(1, 2), new Target(0, 2), 999);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack("iron", 7), w.M.Items.Inventory(0)[8]);
        Assert.Null(w.M.Items.Inventory(0)[0]);
        Assert.Equal(new Stack("canister", Canisters), Crate(w, 2)[0]);
        Assert.Equal(new[] { 1, 2 }, w.Stored().Crates);
    }

    [Fact]
    public void CratesKeepNoEntryForACrateEmptied()
    {
        var w = Crates();
        foreach (int slot in new[] { 0, 1, 2 }) DoMove(w, In(1, slot), new Target(), 999);
        Assert.Empty(w.Refusals);
        Assert.Empty(w.Stored().Crates);
        Assert.Equal(new[] { Scrap, Iron, Canisters }, Totals(w));
    }

    [Fact]
    public void CratesGiveTheLastStackToTheFirstOfTwoHeroesTakingItOnOneTick()
    {
        var w = Crates()
            .Do(new Move(In(1, 2), new Target(), 999) { From = 2 })
            .Do(new Move(In(1, 2), new Target(), 999) { From = 1 })
            .Run();
        Assert.Equal(new[] { "items.empty" }, Reasons(w));
        Assert.Equal(new[] { Canisters, 0 }, new[] { Count(w, "canister", 2), Count(w, "canister", 1) });
    }

    [Fact]
    public void CratesRefuseAMoveFromNothingOntoNoRoomOfNoCrateOrOfNoSlot()
    {
        var w = Crates().GiveItems(0, ("scrap", 1));
        DoMove(w, In(1, 7), new Target(), 1);
        DoMove(w, In(1, 0), new Target(), 0);
        Assert.Equal(new[] { "items.empty", "items.empty" }, Reasons(w));

        DoMove(w, In(99, 0), new Target(), 1);
        DoMove(w, Own(0), new Target(Crate: 99), 1);
        Assert.Equal(new[] { "items.noCrate", "items.noCrate" }, Reasons(w)[2..]);

        DoMove(w, Own(-1), new Target(1), 1);
        DoMove(w, Own(Inventory.Total), new Target(Crate: 1), 1);
        DoMove(w, Own(0), new Target(CrateSlots, 1), 1);
        DoMove(w, Own(0), new Target(0), 1);
        DoMove(w, Own(0), new Target(), 1);
        Assert.Equal(Enumerable.Repeat("items.noSlot", 5), Reasons(w)[4..]);
        Assert.Equal(new[] { 1, 0, 0 }, Totals(w));

        var full = Crates().GiveItems(0, ("canister", Inventory.Slots * StackOf("canister")));
        DoMove(full, In(1, 0), new Target(), 999);
        Assert.Equal(new[] { "items.full" }, Reasons(full));
        Assert.Equal(new Stack("scrap", Scrap), Crate(full, 1)[0]);
    }

    [Fact]
    public void CratesRefuseACrateThatIsAnotherMachine()
    {
        var w = new Scenario("TT.KK\nTT...").GiveItems(0, ("scrap", 1));
        DoMove(w, Own(0), new Target(Crate: 1), 1);
        DoMove(w, Own(0), new Target(Crate: 2), 1);
        Assert.Equal(new[] { "items.noCrate" }, Reasons(w));
        Assert.Equal(new Stack("scrap", 1), Crate(w, 2)[0]);
    }

    /// <summary>
    /// A slot up to <paramref name="max"/>, -1 too, mostly among the first few, where the stacks are: as
    /// fast-check leans to small numbers, which a uniform draw would rarely reach.
    /// </summary>
    internal static Gen<int> Slots(int max) => Gen.Frequency((3, Gen.Choose(-1, 3)), (1, Gen.Choose(-1, max)));

    /// <summary>A slot of the sender's own inventory or of machines 1 to 3, out of range by one on either side.</summary>
    internal static readonly Gen<Items.Place> Places = Gen.OneOf(
        Slots(Inventory.Total).Select(Own),
        from crate in Gen.Choose(1, 3) from slot in Slots(28) select In(crate, slot));

    private static readonly Gen<Target> Targets = Gen.OneOf(
        Places.Select(p => (Target)p),
        Gen.Constant(new Target()),
        Gen.Choose(1, 3).Select(crate => new Target(Crate: crate)));

    private static readonly Gen<int> Counts = Gen.OneOf(Gen.Constant(999), Gen.Choose(0, 400));

    private static int[] Everything(Scenario w)
    {
        var all = Enumerable.Range(0, 3).SelectMany(p => w.M.Items.Inventory(p)).Concat(Crate(w, 1)).Concat(Crate(w, 2)).ToList();
        return All.Select(item => all.Sum(s => s?.Item == item ? s.Count : 0)).ToArray();
    }

    [Property(MaxTest = 200)]
    public Property CratesNeverMakeOrLoseAnItemNorKeepAnEntryForAnEmptyCrate()
    {
        var step = from src in Places
                   from dst in Targets
                   from count in Counts
                   from peer in Gen.Choose(0, 2)
                   select new Move(src, dst, count) { From = peer };
        var steps = from n in Gen.Choose(0, 80) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), moves =>
        {
            var w = Crates().GiveItems(1, ("canister", 9), ("iron", 600));
            var start = Everything(w);
            foreach (var m in moves)
            {
                w.Do(m).Run();
                Assert.Equal(start, Everything(w));
                foreach (int id in new[] { 1, 2 })
                    foreach (var slot in Crate(w, id))
                        if (slot != null) Assert.InRange(slot.Count, 1, StackOf(slot.Item));
                Assert.All(w.Stored().Crates, id => Assert.Contains(Crate(w, id), s => s != null));
            }
        });
    }

    // Moves a client has in flight.

    [Property(MaxTest = 200)]
    public Property MovesInFlightShowTheSlotsAsTheMovesWouldLeaveThemAndLeaveTheWorldBe()
    {
        var step = from src in Places from dst in Targets from count in Counts select new Move(src, dst, count) { From = 1 };
        var steps = from n in Gen.Choose(0, 30) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), moves =>
        {
            var w = Crates().GiveItems(1, ("canister", 9), ("iron", 600));
            ulong hash = w.World.Hash();
            var shown = w.M.Items.AfterMoves(1, moves);
            Assert.Equal(hash, w.World.Hash());
            var before = new[] { 1, 2, 3 }.Select(id => Crate(w, id)).ToArray();
            foreach (var m in moves) w.Do(m);
            w.Run();
            Assert.Equal(w.M.Items.Inventory(1), shown.Inventory);
            for (int i = 0; i < 3; i++)
            {
                int id = i + 1;
                var crate = shown.Crates.Where(c => c.Crate == id).Select(c => c.Slots).FirstOrDefault() ?? before[i];
                Assert.Equal(Crate(w, id), crate);
            }
        });
    }

    // Beyond items.test.ts: the furnace rules at items' own level, which FurnaceTests prove through a fire.

    [Fact]
    public void TheLifepodTakesNothingInWhicheverSlotIsNamedAndGivesOutWhatItMade()
    {
        var w = new Scenario("OOO\nOOO\nOOO").GiveItems(0, ("scrap", 3));
        DoMove(w, Own(0), new Target(Crate: 1), 1);
        DoMove(w, Own(0), new Target(0, 1), 1);
        DoMove(w, Own(0), new Target(2, 1), 1);
        Assert.Equal(new[] { "items.takeOnly", "items.takeOnly", "items.takeOnly" }, Reasons(w));
        Assert.Equal(new Stack?[3], Crate(w, 1));
        Assert.Null(w.M.Items.OutputFull(1, [("wire", 250)]));
        Assert.Equal("items.full", w.M.Items.OutputFull(1, [("wire", 301)]));
        w.FillCrate(1, ("wire", 2));
        DoMove(w, In(1, 0), new Target(), 2);
        Assert.Equal(3, w.Refusals.Count);
        Assert.Equal(2, Count(w, "wire"));
    }

    [Fact]
    public void FurnacesTakeItemsOnlyIntoTheirInputsAMoveNamingNoSlotGoingIntoTheFirst()
    {
        var w = new Scenario("FF\nFF").GiveItems(0, ("scrapChunk", 3), ("planks", 2));
        DoMove(w, Own(0), new Target(2, 1), 1);
        Assert.Equal(new[] { "items.takeOnly" }, Reasons(w));
        DoMove(w, Own(0), new Target(Crate: 1), 2);
        DoMove(w, Own(1), new Target(1, 1), 1);
        Assert.Equal(new[] { "items.takeOnly" }, Reasons(w));
        Assert.Equal(new Stack?[] { new("scrapChunk", 2), new("planks", 1), null, null, null }, Crate(w, 1));
        // Out of its outputs, as from any crate.
        w.FillCrate(1, ("scrap", 1));
        Assert.Equal(new Stack("scrap", 1), Crate(w, 1)[2]);
        DoMove(w, In(1, 2), new Target(), 1);
        Assert.Single(w.Refusals);
        Assert.Equal(1, Count(w, "scrap"));
    }

    [Fact]
    public void FurnacesSmeltOneOfTheirInputIntoItsYieldsWhileTheyFitAndBurnFuelOneAtATime()
    {
        var w = new Scenario("FF\nFF").GiveItems(0, ("hullPlate", 2), ("planks", 2));
        Assert.False(w.M.Items.CanSmelt(1));
        DoMove(w, Own(0), new Target(Crate: 1), 2);
        DoMove(w, Own(1), new Target(1, 1), 2);
        Assert.True(w.M.Items.CanSmelt(1));
        w.M.Items.Smelt(1, []);
        Assert.Equal(new Stack?[] { new("hullPlate", 1), new("planks", 2), new("iron", 2), new("scrap", 1), null }, Crate(w, 1));
        w.M.Items.Consume(1, 1);
        Assert.Equal(new Stack("planks", 1), Crate(w, 1)[1]);
        Assert.Throws<InvalidOperationException>(() => w.M.Items.Consume(1, 4));

        // Outputs too full for its yields: nothing to smelt.
        var full = new Scenario("FF\nFF").FillCrate(1, ("hullPlate", 1), ("planks", 1), ("canister", 15));
        Assert.Equal(new Stack("canister", 5), Crate(full, 1)[2]);
        Assert.False(full.M.Items.CanSmelt(1));
        Assert.Throws<InvalidOperationException>(() => full.M.Items.Smelt(1, []));
        // Not a furnace, or no salvage in it.
        Assert.False(new Scenario("KK").FillCrate(1, ("scrapChunk", 1)).M.Items.CanSmelt(1));
        Assert.False(new Scenario("FF\nFF").FillCrate(1, ("planks", 1)).M.Items.CanSmelt(1));
    }

    [Fact]
    public void GivesWhatFitsAndLetsTheRestFallStillWhereItWasGivenAsIfDroppedThere()
    {
        var w = new Scenario(".....\n=====").GiveItems(0, ("canister", (Inventory.Slots - 1) * StackOf("canister")));
        var given = w.M.Items.GiveOrDrop(0, [("scrap", StackOf("scrap") + 7), ("iron", 0)], w.At(2, 0));
        Assert.Equal(StackOf("scrap"), Count(w, "scrap"));
        // What it gave is what went into the inventory: none of nothing, and not what fell.
        Assert.Equal([("scrap", StackOf("scrap"))], given);
        var lying = Assert.Single(w.M.Items.Floor);
        Assert.Equal(("scrap", 7, w.At(2, 0), 0, 0, 0), (lying.Item, lying.Count, lying.Cell, lying.Vx, lying.Vy, lying.By));
        // A stack of the limit at most each, and none of nothing.
        w.M.Items.DropAt(3, [("canister", 12), ("iron", 0)], w.At(4, 0));
        Assert.Equal(new[] { 7, 5, 5, 2 }, w.M.Items.Floor.Select(s => s.Count));
        Assert.All(w.M.Items.Floor.Skip(1), s => Assert.Equal(3, s.By));
        // A dropper of its own stack may not take it back at once.
        Assert.Equal("items.tooSoon", w.M.Items.PickUpProblem(3, w.M.Items.Floor[1].Id));
    }

    /// <summary>The generic suites never put an item into the world: a save holding some, loaded, plays on alike.</summary>
    [Fact]
    public void SavesAndLoadsEveryKindOfStack()
    {
        var w = Crates().GiveItems(2, ("iron", 9)).Do(new SetCreative(true)).Run();
        w.M.Items.DropAt(2, [("scrap", 3)], w.At(3, 0));
        w.Do(new Drop(Own(0), 4, w.At(3, 0), 1) { From = 2 }).Run();
        var before = w.World.Save();
        w.Reload();
        Assert.Equal(before, w.World.Save());
        Assert.True(w.M.Items.Creative);
        Assert.Equal(new Stack("iron", 5), w.M.Items.Inventory(2)[0]);
        Assert.Equal(new Stack("canister", Canisters), Crate(w, 1)[2]);
        Assert.Equal(2, w.M.Items.Floor.Count);
    }
}
