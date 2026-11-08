using Stillwake.Content;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Xunit;
using Slot = Stillwake.Sim.Items.Place;
using Tree = Stillwake.Content.Research;

namespace Stillwake.Sim.Tests;

/// <summary>Room bought with credits on the black market (2026-10-30-room-upgrades).</summary>
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
        Assert.True(w.M.Items.SlotOpen(0, new Slot(Inventory.Start - 1)));
        Assert.False(w.M.Items.SlotOpen(0, new Slot(Inventory.Start)));
        w.Do(new Move(new Slot(0), new Target(Inventory.Start), 1)).Run();
        Assert.Equal(new[] { "items.noSlot" }, Reasons(w));
        Assert.Equal("wire", w.M.Items.Inventory(0)[0]?.Item);
        Grant(w, "bag1");
        Assert.True(w.M.Items.SlotOpen(0, new Slot(Inventory.Start)));
        Assert.False(w.M.Items.SlotOpen(0, new Slot(Inventory.Start + 1)));
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
        Assert.True(w.M.Items.SlotOpen(0, new Slot(Belt + 1)));
        Assert.False(w.M.Items.SlotOpen(0, new Slot(Belt + 2)));
    }

    [Fact]
    public void ATradeStationStartsWithTwoSlotsForHandsAndDronesAlike()
    {
        var w = World().GiveItems(0, ("wire", 3));
        int id = Station(w);
        int bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "wire");
        Assert.True(w.M.Items.SlotOpen(0, new Slot(1, id)));
        Assert.False(w.M.Items.SlotOpen(0, new Slot(2, id)));
        w.Do(new Move(new Slot(bag), new Target(2, id), 1)).Run();
        Assert.Equal(new[] { "items.noSlot" }, Reasons(w));
        w.Do(new Move(new Slot(bag), new Target(1, id), 1)).Run();
        Assert.Equal("wire", w.M.Items.Crate(id)[1]?.Item);
        Grant(w, "tradeSlots1");
        Assert.True(w.M.Items.SlotOpen(0, new Slot(2, id)));
        w.Do(new Move(new Slot(bag), new Target(2, id), 1)).Run();
        Assert.Equal("wire", w.M.Items.Crate(id)[2]?.Item);
    }

    /// <summary>A shift-click names no slot: it filled the closed ones too, unseen, and they sold.</summary>
    [Fact]
    public void AMoveIntoATradeStationAsAWholeFillsItsOpenSlotsAlone()
    {
        int stack = Content.Items.Get("wire").Stack;
        var w = World().GiveItems(0, ("wire", stack), ("wire", stack), ("wire", stack));
        int id = Station(w);
        var inventory = w.M.Items.Inventory(0).ToList();
        foreach (int bag in Enumerable.Range(0, inventory.Count).Where(i => inventory[i]?.Item == "wire"))
            w.Do(new Move(new Slot(bag), new Target(Crate: id), stack)).Run();
        Assert.Equal(new[] { "items.full" }, Reasons(w));
        Assert.Equal(Upgrades.TradeStart, w.M.Items.Crate(id).Count(s => s != null));
    }

    [Fact]
    public void CreativeModeOpensEverySlotOfItsPeerAloneAndNoMachines()
    {
        var w = World().Do(new SetCreative(true)).Run();
        Assert.True(w.M.Items.SlotOpen(0, new Slot(Inventory.Slots - 1)));
        Assert.True(w.M.Items.SlotOpen(0, new Slot(Belt + 2)));
        Assert.False(w.M.Items.SlotOpen(1, new Slot(Inventory.Slots - 1)));
        Assert.False(w.M.Items.SlotOpen(1, new Slot(Belt + 2)));
        // A trade station's slots are the machine's, opened by upgrades only.
        Assert.False(w.M.Items.SlotOpen(0, new Slot(7, Station(w))));
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
        Assert.True(w.M.Research.Done(node));
        Mafia.Receive(w);
        Assert.True(w.M.Items.SlotOpen(0, new Slot(Inventory.Start)));
    }

    [Fact]
    public void EveryRingOfTheMarketOffersMoreBagAndTheSlotsAddUpToTheMost()
    {
        var market = Tree.Tabs.Single(t => t.Id == Tree.Market);
        for (int ring = 0; ring < market.Rings.Length; ring++)
            Assert.Contains(Tree.Nodes, n => n.Tab == market.Id && n.Ring == ring && Upgrades.Of(n.Id)?.Kind == Upgrades.Bag);
        Assert.All(Upgrades.All, u => Assert.Equal(Tree.Market, Tree.Node(u.Id)?.Tab));
        Assert.Equal(Inventory.Slots, Inventory.Start + Upgrades.Added(Upgrades.Bag, _ => true));
        Assert.Equal(Inventory.Equipment.Count(e => e == "belt"), Inventory.BeltStart + Upgrades.Added(Upgrades.Belt, _ => true));
        Assert.Equal(Machines.Get("tradeStation").Slots, Upgrades.TradeStart + Upgrades.Added(Upgrades.TradeSlots, _ => true));
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
