using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Game.Features.Research;
using Stillwake.Sim.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The market's goods and what their tiles say ([market-grid](../docs/decisions/2026-11-03-market-grid.md)).</summary>
public class MarketGoodsTests
{
    private static readonly TechTab Tab = Content.Research.Tabs.Single(t => t.Id == Content.Research.Market);
    private static readonly IReadOnlyList<MarketGood> Goods = MarketGoods.Of(Tab);

    private static MarketGood Good(string id) => Goods.Single(g => g.Id == id);

    /// <summary>What the sim says with levels up to <paramref name="met"/> reached and <paramref name="bought"/> had.</summary>
    private static MarketState State(string id, int met, string[] bought, string? problem = null)
    {
        var had = new HashSet<string>(bought, StringComparer.Ordinal);
        return MarketGoods.StateOf(Good(id), n => had.Contains(n.Id), n => n.Ring <= met, _ => problem);
    }

    private static Func<TechNode, bool> Met(int met) => n => n.Ring <= met;

    [Fact]
    public void EachKindOfUpgradeIsOneGoodItsBlocksInOrderThenTheRest()
    {
        Assert.Equal(["bag", "tradeSlots", "belt", "canister", "scanner", "powerDriver", "heavyHelmet", "solarPanel", "salvageDrone", "plasmaCutter", "regenSuit"], Goods.Select(g => g.Id));
        Assert.Equal(Enumerable.Range(1, 11).Select(i => "bag" + i), Good("bag").Steps.Select(s => s.Id));
        Assert.Equal(MarketShelf.Upgrades, Good("belt").Shelf);
        Assert.Equal(MarketShelf.Blueprints, Good("solarPanel").Shelf);
        Assert.Equal(MarketShelf.Blueprints, Good("powerDriver").Shelf);
    }

    [Fact]
    public void EveryLaddersPriceRisesStepByStep()
    {
        foreach (var good in Goods.Where(g => g.Shelf == MarketShelf.Upgrades))
        {
            var prices = good.Steps.Select(Prices.Of).ToArray();
            Assert.True(prices.Zip(prices.Skip(1)).All(p => p.First < p.Second), $"{good.Id}: {string.Join(", ", prices)}");
        }
    }

    [Fact]
    public void EveryGoodFitsTheGridAtTheReferenceWindow() =>
        Assert.True(Goods.Count <= MarketGrid.Capacity(1188, 472), $"{Goods.Count} goods: the grid needs to scroll");

    [Fact]
    public void ALadderOffersItsFirstOpenedStepNotBought()
    {
        var state = State("bag", 1, ["bag1"]);
        Assert.Equal(new MarketState(MarketTile.Affordable, Content.Research.Node("bag2"), 1, 4), state);
    }

    [Fact]
    public void ShortOfCreditsItIsDear() =>
        Assert.Equal(MarketTile.Dear, State("bag", 1, ["bag1"], "research.noCredits").Tile);

    [Fact]
    public void WithEveryOpenedStepBoughtItIsLockedOnTheNextStep()
    {
        var state = State("bag", 0, ["bag1", "bag2"]);
        Assert.Equal(new MarketState(MarketTile.Locked, Content.Research.Node("bag3"), 2, 2), state);
        Assert.Equal(new MarketState(MarketTile.Locked, Content.Research.Node("solarPanel"), 0, 0), State("solarPanel", 3, []));
    }

    [Fact]
    public void BoughtOutItIsOwned()
    {
        Assert.Equal(MarketTile.Owned, State("belt", 5, ["belt1", "belt2"]).Tile);
        Assert.Equal(MarketTile.Owned, State("solarPanel", 4, ["solarPanel"]).Tile);
    }

    [Fact]
    public void AHoleInAnOldSavesLadderIsOfferedFirst()
    {
        var state = State("bag", 1, ["bag1", "bag3"]);
        Assert.Equal("bag2", state.Next!.Id);
        Assert.Equal((2, 4), (state.Bought, state.Opened));
    }

    [Fact]
    public void EveryGoodIsShownWhatTheMafiaDoesNotSellYetLastNearestFirst() =>
        Assert.Equal(["bag", "tradeSlots", "belt", "canister", "scanner", "powerDriver", "heavyHelmet", "plasmaCutter", "solarPanel", "salvageDrone", "regenSuit"],
            MarketGoods.Sorted(Goods, Met(1), null).Select(g => g.Id));

    [Fact]
    public void UpgradesComeFirstThenBlueprintsNewestFirst()
    {
        var goods = Goods.Append(new MarketGood("older", MarketShelf.Blueprints, [Content.Research.Node("bag1")!]));
        Assert.Equal(["bag", "tradeSlots", "belt", "canister", "regenSuit", "plasmaCutter", "solarPanel", "salvageDrone", "heavyHelmet", "powerDriver", "scanner", "older"], MarketGoods.Sorted(goods, Met(5), null).Select(g => g.Id));
        Assert.Equal(["regenSuit", "plasmaCutter", "solarPanel", "salvageDrone", "heavyHelmet", "powerDriver", "scanner", "older"], MarketGoods.Sorted(goods, Met(5), MarketShelf.Blueprints).Select(g => g.Id));
    }

    [Fact]
    public void AGoodWithAStepInTheRowsDecipheringDeciphersItsPrice()
    {
        Func<int, bool> second = row => row == 2;
        Assert.Equal(MarketFresh.More, MarketGoods.FreshOf(Good("belt"), Met(2), second));
        Assert.Equal(MarketFresh.More, MarketGoods.FreshOf(Good("bag"), Met(2), second));
        Assert.Equal(MarketFresh.None, MarketGoods.FreshOf(Good("solarPanel"), Met(2), second));
        Assert.Equal(MarketFresh.None, MarketGoods.FreshOf(Good("bag"), Met(2), _ => false));
    }
}
