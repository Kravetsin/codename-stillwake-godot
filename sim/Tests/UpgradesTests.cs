using Stillwake.Content;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Xunit;
using Slot = Stillwake.Sim.Items.Place;
using Tree = Stillwake.Content.Research;

namespace Stillwake.Sim.Tests;

/// <summary>Room bought with credits on the tree (2026-10-30-room-upgrades).</summary>
public class UpgradesTests
{
    private static readonly string[] Rows =
    [
        "##########",
        "#$$......#",
        "#$$......#",
        "##########",
    ];
    private static readonly int Belt = Inventory.Slots + Inventory.Equipment.ToList().IndexOf("belt");

    /// <summary>A world knowing what a new game does, the gates' deposits aside: a test opens each gate it needs.</summary>
    private static Scenario World() => new(Layout.Parse(Rows, null) with { Known = [] }, doubled: true);

    private static int Station(Scenario w) => w.M.Grid.MachinesOf("tradeStation")[0].Id;

    /// <summary>Upgrade <paramref name="id"/> known, as a purchase leaves it: its ring and its price are the tree's tests'.</summary>
    private static void Grant(Scenario w, string id) => w.M.Knowledge.Grant(id);

    private static string[] Reasons(Scenario w) => [.. w.Refusals.Select(r => r.Reason)];

    [Fact]
    public void AnInventoryStartsWithTheHotbarAndTwoRowsAndEachBagUpgradeAddsToIt()
    {
        var w = World().GiveItems(0, ("wire", 1));
        Assert.Equal(9 + 2 * 9, Inventory.Start);
        Assert.True(w.M.Items.SlotOpen(new Slot(Inventory.Start - 1)));
        Assert.False(w.M.Items.SlotOpen(new Slot(Inventory.Start)));
        w.Do(new Move(new Slot(0), new Target(Inventory.Start), 1)).Run();
        Assert.Equal(new[] { "items.noSlot" }, Reasons(w));
        Assert.Equal("wire", w.M.Items.Inventory(0)[0]?.Item);
        Grant(w, "bag1");
        Assert.True(w.M.Items.SlotOpen(new Slot(Inventory.Start)));
        Assert.False(w.M.Items.SlotOpen(new Slot(Inventory.Start + 1)));
        w.Do(new Move(new Slot(0), new Target(Inventory.Start), 1)).Run();
        Assert.Equal("wire", w.M.Items.Inventory(0)[Inventory.Start]?.Item);
    }

    [Fact]
    public void WhatIsGivenFillsOnlyTheOpenSlots()
    {
        var w = World();
        int stack = Content.Items.Get("canister").Stack;
        Assert.Null(w.M.Items.FullFor(0, [("canister", Inventory.Start * stack)]));
        Assert.Equal("items.full", w.M.Items.FullFor(0, [("canister", Inventory.Start * stack + 1)]));
        Grant(w, "bag1");
        Assert.Null(w.M.Items.FullFor(0, [("canister", Inventory.Start * stack + 1)]));
    }

    [Fact]
    public void TheBeltStartsWithOneSlotAndEachUpgradeOpensTheNext()
    {
        var w = World().GiveItems(0, ("battery", 1));
        int bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "battery");
        w.Do(new Move(new Slot(bag), new Target(Belt + 1), 1)).Run();
        Assert.Equal(new[] { "items.noSlot" }, Reasons(w));
        w.Do(new Move(new Slot(bag), new Target(Belt), 1)).Run();
        Assert.Equal("battery", w.M.Items.Inventory(0)[Belt]?.Item);
        Grant(w, "belt1");
        Assert.True(w.M.Items.SlotOpen(new Slot(Belt + 1)));
        Assert.False(w.M.Items.SlotOpen(new Slot(Belt + 2)));
    }

    [Fact]
    public void ATradeStationStartsWithTwoSlotsForHandsAndDronesAlike()
    {
        var w = World().GiveItems(0, ("wire", 3));
        int id = Station(w);
        int bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "wire");
        Assert.True(w.M.Items.SlotOpen(new Slot(1, id)));
        Assert.False(w.M.Items.SlotOpen(new Slot(2, id)));
        w.Do(new Move(new Slot(bag), new Target(2, id), 1)).Run();
        Assert.Equal(new[] { "items.noSlot" }, Reasons(w));
        w.Do(new Move(new Slot(bag), new Target(1, id), 1)).Run();
        Assert.Equal("wire", w.M.Items.Crate(id)[1]?.Item);
        Grant(w, "tradeSlots1");
        Assert.True(w.M.Items.SlotOpen(new Slot(2, id)));
        w.Do(new Move(new Slot(bag), new Target(2, id), 1)).Run();
        Assert.Equal("wire", w.M.Items.Crate(id)[2]?.Item);
    }

    [Fact]
    public void CreativeModeOpensEverything()
    {
        var w = World().Do(new SetCreative(true)).Run();
        Assert.True(w.M.Items.SlotOpen(new Slot(Inventory.Slots - 1)));
        Assert.True(w.M.Items.SlotOpen(new Slot(Belt + 2)));
        Assert.True(w.M.Items.SlotOpen(new Slot(7, Station(w))));
    }

    [Fact]
    public void AnUpgradeIsBoughtWithCreditsAsABlockIs()
    {
        var w = World();
        var node = Tree.Node("bag1")!;
        w.Do(new BuyBlock("bag1")).Run();
        Assert.Equal(new[] { "research.noCredits" }, Reasons(w));
        w.M.Knowledge.Earn(Prices.Of(node));
        w.Do(new BuyBlock("bag1")).Run();
        Assert.Equal(0, w.M.Research.Credits);
        Assert.True(w.M.Items.SlotOpen(new Slot(Inventory.Start)));
        Assert.True(w.M.Research.Done(node));
    }

    [Fact]
    public void EveryRingOfTheTreeOffersMoreBagAndTheSlotsAddUpToTheMost()
    {
        // The black market is a shop, not a stage of the tree.
        foreach (var tab in Tree.Tabs.Where(t => t.Id != "blackMarket"))
            for (int ring = 0; ring < tab.Rings.Length; ring++)
                Assert.Contains(Tree.Nodes, n => n.Tab == tab.Id && n.Ring == ring && Upgrades.Of(n.Id)?.Kind == Upgrades.Bag);
        Assert.All(Upgrades.All, u => Assert.NotNull(Tree.Node(u.Id)));
        Assert.Equal(Inventory.Slots, Inventory.Start + Upgrades.Added(Upgrades.Bag, _ => true));
        Assert.Equal(Inventory.Equipment.Count(e => e == "belt"), Inventory.BeltStart + Upgrades.Added(Upgrades.Belt, _ => true));
        Assert.Equal(Machines.Get("tradeStation").Slots, Upgrades.TradeStart + Upgrades.Added(Upgrades.TradeSlots, _ => true));
    }

    [Fact]
    public void TheBeltGrowsWithTheStoresItCarries()
    {
        static int RingOf(string id) => Tree.Node(id)!.Ring;
        Assert.Equal(RingOf("battery"), RingOf("belt1"));
        Assert.Equal(Tree.Node("accumulator")!.Tab, Tree.Node("belt2")!.Tab);
        Assert.Equal(RingOf("accumulator"), RingOf("belt2"));
    }

    [Fact]
    public void WithinAKindEachUpgradeCostsMoreThanTheLast()
    {
        foreach (var kind in Upgrades.All.GroupBy(u => u.Kind))
        {
            var prices = kind.Select(u => u.Credits).ToList();
            Assert.Equal(prices.Order(), prices);
        }
    }
}
