using Stillwake.Content;
using Stillwake.Sim.Research;
using Stillwake.Sim.Trade;
using Xunit;
using Tree = Stillwake.Content.Research;

namespace Stillwake.Sim.Tests;

/// <summary>The black market's rings, opened by what has been sold (docs/decisions/2026-11-05-the-board.md).</summary>
public class MarketOrdersTests
{
    private static Scenario World(int credits = 0)
    {
        var w = new Scenario(Layout.Parse(["##########", "#$$......#", "#$$......#", "##########"], null) with { Known = [] }, doubled: true);
        w.M.Knowledge.Earn(credits);
        return w;
    }

    private static TechNode Order(string id) => Tree.Node(id)!;

    /// <summary>All of <paramref name="id"/>'s order sold, straight into the count.</summary>
    private static void Sell(Scenario w, string id)
    {
        foreach (var (item, count) in Order(id).Order) w.M.Knowledge.Sell(item, count);
    }

    private static string[] Reasons(Scenario w) => [.. w.Refusals.Select(r => r.Reason)];

    [Fact]
    public void ARingOpensByCapsulesAloneWithNoCommandOfItsOwn()
    {
        var w = World();
        int station = w.M.Grid.MachinesOf("tradeStation")[0].Id;
        var order = Order("order1");
        foreach (var (item, count) in order.Order)
        {
            Assert.False(w.M.Research.Done(order));
            w.FillCrate(station, (item, count));
            w.Do(new SendCapsule(station)).Run(Market.CapsuleFlight);
        }
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Research.Done(order));
    }

    [Fact]
    public void AnOrderNotMetKeepsItsRingShutAndMetLetsItsBlocksBeBought()
    {
        var w = World(credits: 100_000);
        var order = Order("order1");
        w.Do(new BuyBlock("bag3")).Run();
        Assert.Equal(["research.locked"], Reasons(w));
        foreach (var (item, count) in order.Order) w.M.Knowledge.Sell(item, count - 1);
        Assert.False(w.M.Research.Done(order));
        foreach (var (item, _) in order.Order) w.M.Knowledge.Sell(item, 1);
        Assert.True(w.M.Research.Done(order));
        w.Do(new BuyBlock("bag3")).Run();
        Assert.Single(w.Refusals);
        Assert.True(w.M.Research.Knows("bag3"));
    }

    [Fact]
    public void WhatWasSoldBeforeARingCameUpCountsSoALaterRingMayOpenAtOnce()
    {
        var w = World();
        Sell(w, "order2");
        Assert.False(w.M.Research.Done(Order("order2")));
        Sell(w, "order1");
        Assert.True(w.M.Research.Done(Order("order1")));
        Assert.True(w.M.Research.Done(Order("order2")));
    }

    [Fact]
    public void AnOrderMetWithTheRingBeforeShortStaysShut()
    {
        var w = World(credits: 100_000);
        Sell(w, "order4");
        Assert.False(w.M.Research.Done(Order("order4")));
        w.Do(new BuyBlock("bag9")).Run();
        Assert.Equal(["research.locked"], Reasons(w));
    }

    [Fact]
    public void TheMarketsFirstRingIsOpenFromTheStart()
    {
        var w = World(credits: 100_000);
        w.Do(new BuyBlock("bag1")).Do(new BuyBlock("tradeSlots1")).Run();
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void TheSolarPanelIsSoldOnlyOnceTheFourthOrderIsMet()
    {
        var w = World(credits: 100_000);
        foreach (string order in (string[])["order1", "order2", "order3"]) Sell(w, order);
        w.Do(new BuyBlock("solarPanel")).Run();
        Assert.Equal(["research.locked"], Reasons(w));
        w.Refusals.Clear();
        Sell(w, "order4");
        w.Do(new BuyBlock("solarPanel")).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Research.Done(Tree.Node("solarPanel")!));
    }

    [Fact]
    public void OnlyTheMarketsFirstRowHasNoOrder()
    {
        var market = Tree.Tabs.Single(t => t.Id == Tree.Market);
        Assert.Null(market.Rings[0].Gate);
        Assert.All(market.Rings.Skip(1), r => Assert.NotNull(r.Gate));
    }

    [Fact]
    public void EveryOrderedItemHasAPrice()
    {
        foreach (var gate in Tree.Nodes.Where(n => n.Gate))
            Assert.All(gate.Order, d => Assert.NotNull(Market.PriceOf(d.Item)));
    }
}
