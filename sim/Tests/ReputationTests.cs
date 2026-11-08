using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Research;
using Stillwake.Sim.Trade;
using Xunit;
using Tree = Stillwake.Content.Research;

namespace Stillwake.Sim.Tests;

/// <summary>The black market's rows, opened by the mafia's reputation (docs/decisions/2026-11-08-mafia-reputation.md).</summary>
public class ReputationTests
{
    private static Scenario World(int credits = 0)
    {
        var w = new Scenario(Layout.Parse(["##########", "#$$......#", "#$$......#", "##########"], null) with { Known = [] }, doubled: true);
        w.M.Knowledge.Earn(credits);
        return w;
    }

    private static TechNode Gate(string id) => Tree.Node(id)!;

    /// <summary>What <paramref name="id"/>'s level asks, in sevenths: its order at full worth.</summary>
    private static long Target(string id) => Gate(id).Order.Sum(d => (long)d.Count * Market.WorthOf(d.Item)) * Tree.OffWanted;

    private static string[] Reasons(Scenario w) => [.. w.Refusals.Select(r => r.Reason)];

    [Fact]
    public void ALevelOpensByCapsulesAloneWithNoCommandOfItsOwn()
    {
        var w = World();
        int station = w.M.Grid.MachinesOf("tradeStation")[0].Id;
        foreach (var (item, count) in Gate("order1").Order)
        {
            Assert.Equal(0, w.M.Knowledge.Level);
            w.FillCrate(station, (item, count));
            w.Do(new SendCapsule(station)).Run(Market.CapsuleFlight);
        }
        Assert.Empty(w.Refusals);
        Assert.Equal(1, w.M.Knowledge.Level);
        Assert.True(w.M.Research.Done(Gate("order1")));
    }

    [Fact]
    public void AWantedGoodFillsItsShareAtFullWorthAndItsExcessAndAnythingElseAtASeventh()
    {
        var w = World();
        int iron = Market.WorthOf("iron"), hide = Market.WorthOf("hide");
        Mafia.Sell(w, "iron", 25);
        Mafia.Sell(w, "hide", 3);
        var standing = w.M.Research.Standing();
        Assert.Equal(Gate("order1"), standing.Next);
        Assert.Equal(20L * iron * Tree.OffWanted + 5L * iron + 3L * hide, standing.Points);
        Assert.Equal(Target("order1"), standing.Target);
        Assert.Equal(new Want("iron", 25, 20), standing.Wanted.Single(d => d.Item == "iron"));
    }

    [Fact]
    public void ALevelOpensAtItsTargetExactlyAndNotASeventhShort()
    {
        var w = World(credits: 100_000);
        long target = Target("order1");
        w.M.Knowledge.Sell("rock", 1, target - 1);
        w.Run();
        Assert.Equal(0, w.M.Knowledge.Level);
        w.Do(new BuyBlock("bag3")).Run();
        Assert.Equal(["research.reputation"], Reasons(w));
        w.Refusals.Clear();
        w.M.Knowledge.Sell("rock", 1, 1);
        w.Run();
        w.Do(new BuyBlock("bag3")).Run();
        Assert.Equal(1, w.M.Knowledge.Level);
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Research.Done(Tree.Node("bag3")!));
    }

    [Fact]
    public void WhatWasSoldBeforeALevelOpenedCountsNothingTowardTheNext()
    {
        var w = World();
        foreach (var (item, count) in Gate("order2").Order) Mafia.Sell(w, item, count);
        Mafia.Reach(w, 1);
        var standing = w.M.Research.Standing();
        Assert.Equal(Gate("order2"), standing.Next);
        Assert.Equal(0, standing.Points);
        Assert.All(standing.Wanted, d => Assert.Equal(0, d.Sold));
    }

    [Fact]
    public void OneHugeCapsuleOpensOneLevelAndTheNextOnlyBySellingAgain()
    {
        var w = World();
        foreach (var gate in (string[])["order1", "order2", "order3"])
            foreach (var (item, count) in Gate(gate).Order) Mafia.Sell(w, item, count * 10);
        w.Run(5);
        Assert.Equal(1, w.M.Knowledge.Level);
        foreach (var (item, count) in Gate("order2").Order) Mafia.Sell(w, item, count);
        w.Run();
        Assert.Equal(2, w.M.Knowledge.Level);
    }

    [Fact]
    public void WhatElseIsSoldNeverCountsBelowNoneWhenAWantedGoodWasWorthLessThanItIsNow()
    {
        var w = World();
        w.M.Knowledge.Sell("iron", 20, 0);
        Assert.Equal(20L * Market.WorthOf("iron") * Tree.OffWanted, w.M.Research.Standing().Points);
    }

    [Fact]
    public void ALevelPastTheNextIsKeptOutAndEachIsOpenedOnce()
    {
        var w = World();
        w.M.Knowledge.Open(2, [], 0);
        Assert.Equal(0, w.M.Knowledge.Level);
        w.M.Knowledge.Open(1, [], 0);
        w.M.Knowledge.Open(1, [], 0);
        Assert.Equal(1, w.M.Knowledge.Level);
    }

    [Fact]
    public void AWorldWithAPlanetOpensALevelOnce()
    {
        var w = new Scenario(Layout.Parse([" #####   ", " #...#   ", " #.S.#   ", " #...#   ", " #####   "]) with { Known = [] });
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Surfaces.TestStrip.Id)).Run();
        foreach (var gate in (string[])["order1", "order2"])
            foreach (var (item, count) in Gate(gate).Order) Mafia.Sell(w, item, count * 3);
        w.Run(3);
        Assert.Equal(1, w.M.Knowledge.Level);
    }

    [Fact]
    public void ABlockPastTheLevelLacksReputationAndAGateOrNoNodeIsLocked()
    {
        var w = World(credits: 100_000);
        w.Do(new BuyBlock("bag9")).Do(new BuyBlock("order1")).Do(new BuyBlock("nothing")).Run();
        Assert.Equal(["research.reputation", "research.locked", "research.locked"], Reasons(w));
    }

    [Fact]
    public void TheMarketsFirstRowIsOpenFromTheStart()
    {
        var w = World(credits: 100_000);
        w.Do(new BuyBlock("bag1")).Do(new BuyBlock("tradeSlots1")).Run();
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void TheSolarPanelIsSoldOnlyAtTheFourthLevel()
    {
        var w = World(credits: 100_000);
        Mafia.Reach(w, 3);
        w.Do(new BuyBlock("solarPanel")).Run();
        Assert.Equal(["research.reputation"], Reasons(w));
        w.Refusals.Clear();
        Mafia.Reach(w, 4);
        w.Do(new BuyBlock("solarPanel")).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Research.Done(Tree.Node("solarPanel")!));
    }

    [Fact]
    public void PastTheLastLevelNothingIsWanted()
    {
        var w = World();
        int last = Tree.Nodes.Count(n => n.Gate);
        Mafia.Reach(w, last);
        var standing = w.M.Research.Standing();
        Assert.Null(standing.Next);
        Assert.Empty(standing.Wanted);
        Assert.Equal((0L, 0L), (standing.Points, standing.Target));
        Mafia.Sell(w, "iron", 100);
        w.Run();
        Assert.Equal(last, w.M.Knowledge.Level);
    }

    [Fact]
    public void SoldPartWayToALevelSavedAndLoadedPlaysOnAsAWorldThatKeptRunning()
    {
        var kept = World();
        var loaded = World();
        foreach (var w in new[] { kept, loaded })
        {
            Mafia.Reach(w, 1);
            Mafia.Sell(w, "plate", 10);
            Mafia.Sell(w, "hide", 4);
            w.Run();
        }
        loaded.Reload();
        foreach (var w in new[] { kept, loaded })
        {
            foreach (var (item, count) in Gate("order2").Order) Mafia.Sell(w, item, count);
            w.Run();
        }
        Assert.Equal(2, loaded.M.Knowledge.Level);
        Assert.Equal(kept.World.Hash(), loaded.World.Hash());
    }

    [Fact]
    public void OnlyTheMarketsFirstRowHasNoGateAndTheGatesRunInTurn()
    {
        var market = Tree.Tabs.Single(t => t.Id == Tree.Market);
        Assert.Null(market.Rings[0].Gate);
        Assert.All(market.Rings.Skip(1), r => Assert.NotNull(r.Gate));
        Assert.Equal(Enumerable.Range(1, market.Rings.Length - 1), Tree.Nodes.Where(n => n.Gate).Select(n => n.Ring));
    }

    [Fact]
    public void EveryWantedItemHasAPriceAndEveryLevelATarget()
    {
        foreach (var gate in Tree.Nodes.Where(n => n.Gate))
        {
            Assert.All(gate.Order, d => Assert.NotNull(Market.PriceOf(d.Item)));
            Assert.True(Target(gate.Id) > 0, gate.Id);
        }
    }

    /// <summary>Over any sales the level only grows, and after a tick the next level's points are short of its target.</summary>
    [Property(MaxTest = 40)]
    public Property TheLevelOnlyGrowsAndNeverStandsAtItsTarget()
    {
        string[] items = [.. Tree.Nodes.Where(n => n.Gate).SelectMany(n => n.Order.Select(d => d.Item)), "scrap", "rock"];
        var sale = from item in Gen.Elements(items) from count in Gen.Choose(1, 60) select (item, count);
        var sales = from n in Gen.Choose(1, 30) from s in sale.ArrayOf(n) select s;
        return Prop.ForAll(sales.ToArbitrary(), all =>
        {
            var w = World();
            int was = 0;
            foreach (var (item, count) in all)
            {
                Mafia.Sell(w, item, count);
                w.Run();
                int level = w.M.Knowledge.Level;
                if (level < was) return false.Label($"fell to {level} after {item}");
                var standing = w.M.Research.Standing();
                if (standing.Next != null && standing.Points >= standing.Target) return false.Label($"at its target after {item}");
                was = level;
            }
            return true.ToProperty();
        });
    }
}
