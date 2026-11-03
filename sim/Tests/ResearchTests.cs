using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Xunit;
using ResearchSpec = Stillwake.Content.Research;

namespace Stillwake.Sim.Tests;

/// <summary>What is known, and the market's blocks bought with credits (docs/plans/board.md); its orders are in <see cref="MarketOrdersTests"/>.</summary>
public class ResearchTests
{
    private const int Width = 10;
    /// <summary>The cell of the map's character at (<paramref name="x"/>, <paramref name="y"/>), doubled (0104).</summary>
    private static int At(int x, int y) => 2 * y * 2 * Width + 2 * x;

    /// <summary>Ten wide, granting <paramref name="known"/> or, with null, everything: a trade station in a room.</summary>
    private static Layout Map(IReadOnlyList<string>? known)
    {
        string[] rows =
        [
            "##########",
            "#$$......#",
            "#$$......#",
            "##########",
        ];
        var layout = Layout.Parse(rows, null);
        return known == null ? layout : layout with { Known = known };
    }

    /// <summary>A world knowing what the wreck does and <paramref name="known"/>, with <paramref name="credits"/> earned.</summary>
    private static Scenario SetUp(int credits = 0, IReadOnlyList<string>? known = null)
    {
        var w = new Scenario(Map([.. ResearchSpec.StartKnown, .. known ?? []]), doubled: true);
        w.M.Knowledge.Earn(credits);
        return w;
    }

    private static int TradeOf(Scenario w) => w.M.Grid.MachineAt(At(1, 1))!.Id;
    private static TechNode Node(string id) => ResearchSpec.Node(id)!;

    [Fact]
    public void BuysABlockSpendingItsPriceAndKnowingWhatItOpensAlone()
    {
        var bag = Node("bag1");
        var w = SetUp(credits: Prices.Of(bag) + 2);
        Assert.False(w.M.Research.Knows("bag1"));
        w.Do(new BuyBlock("bag1")).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(2, w.M.Research.Credits);
        Assert.True(w.M.Research.Knows("bag1"));
        Assert.False(w.M.Research.Knows("bag2"));
        Assert.True(w.M.Research.Done(bag));
    }

    /// <summary>The user's prices (2026-11-03-the-heavy-helmet): a craft's block priced by its worth, as a blueprint's.</summary>
    [Fact]
    public void TheHeavyHelmetCostsFiveThousandAndTheCanistersUpgradeTenAndBoughtTheHelmetIsCraftable()
    {
        Assert.Equal(5000, Prices.Of(Node("heavyHelmet")));
        Assert.Equal(10000, Prices.Of(Node("canister1")));
        var w = SetUp(credits: 100_000);
        foreach (var (item, count) in ResearchSpec.Nodes.Where(n => n.Gate && n.Ring <= Node("heavyHelmet").Ring).SelectMany(n => n.Order)) w.M.Knowledge.Sell(item, count);
        Assert.False(w.M.Research.Knows("heavyHelmet"));
        w.Do(new BuyBlock("heavyHelmet")).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Research.Knows("heavyHelmet"));
        Assert.Equal(100_000 - 5000, w.M.Research.Credits);
    }

    [Fact]
    public void WhatABlockOpensIsPlannedOnceBought()
    {
        var w = SetUp(credits: 100_000);
        var panel = new Placement("machine", "solarPanel");
        Assert.Equal("building.unknown", w.M.Building.Problem(At(5, 1), panel));
        foreach (var (item, count) in ResearchSpec.Nodes.Where(n => n.Gate && n.Ring <= ResearchSpec.Node("solarPanel")!.Ring).SelectMany(n => n.Order)) w.M.Knowledge.Sell(item, count);
        w.Do(new BuyBlock("solarPanel")).Run();
        Assert.Empty(w.Refusals);
        Assert.NotEqual("building.unknown", w.M.Building.Problem(At(5, 1), panel));
    }

    /// <summary>The tablet needs no machine (2026-10-30-tablet-tree): a world with none buys all the same.</summary>
    [Fact]
    public void BuysWithNoMachineInTheWorld()
    {
        var w = new Scenario(Layout.Parse(["##########", "#........#", "#........#", "##########"], null) with { Known = ResearchSpec.StartKnown }, doubled: true);
        w.M.Knowledge.Earn(100_000);
        w.Do(new BuyBlock("bag1")).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Research.Knows("bag1"));
    }

    /// <summary>What a map grants may lie behind what is not done: bought, not locked, and nothing is charged for it.</summary>
    [Fact]
    public void ABlockKnownBeyondAShutGateIsBoughtNotLocked()
    {
        var w = SetUp(credits: 100, known: ["bag3"]);
        w.Do(new BuyBlock("bag3")).Run();
        Assert.Equal(["research.bought"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(100, w.M.Research.Credits);
    }

    [Fact]
    public void RefusesABlockBoughtAlready()
    {
        var w = SetUp(credits: 100_000);
        w.Do(new BuyBlock("bag1")).Run();
        w.Do(new BuyBlock("bag1")).Run();
        Assert.Equal(["research.bought"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(100_000 - Prices.Of(Node("bag1")), w.M.Research.Credits);
    }

    [Fact]
    public void RefusesABlockBehindAGateNotMetOneThereIsNotAndAGateItself()
    {
        var w = SetUp(credits: 100_000);
        w.Do(new BuyBlock("bag3")).Run();
        w.Do(new BuyBlock("noSuchBlock")).Run();
        w.Do(new BuyBlock("order1")).Run();
        Assert.Equal(["research.locked", "research.locked", "research.locked"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(100_000, w.M.Research.Credits);
    }

    /// <summary>A block the rule used to sit behind is sold no more: it is understood, never bought.</summary>
    [Fact]
    public void RefusesToSellWhatTheRuleGrants()
    {
        var w = SetUp(credits: 100_000);
        w.Do(new BuyBlock("furnace")).Run();
        Assert.Equal(["research.locked"], w.Refusals.Select(r => r.Reason));
        Assert.False(w.M.Research.Knows("furnace"));
        Assert.Equal(100_000, w.M.Research.Credits);
    }

    [Fact]
    public void RefusesABlockShortOfCreditsAndLeavesThemAlone()
    {
        int price = Prices.Of(Node("bag1"));
        var w = SetUp(credits: price - 1);
        w.Do(new BuyBlock("bag1")).Run();
        Assert.Equal(["research.noCredits"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(price - 1, w.M.Research.Credits);
        Assert.False(w.M.Research.Knows("bag1"));
    }

    [Fact]
    public void CreativeModeKnowsAndOpensEverythingAndChargesNothing()
    {
        var w = SetUp(credits: 3);
        w.Do(new SetCreative(true)).Run();
        Assert.True(w.M.Research.Knows("engine"));
        Assert.All(ResearchSpec.Nodes, n => Assert.True(w.M.Research.Done(n), n.Id));
        w.Do(new BuyBlock("solarPanel")).Run();
        Assert.Equal(["research.bought"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(3, w.M.Research.Credits);
    }

    [Fact]
    public void KnowsWhatIsGrantedAndNoMore()
    {
        string[] known = ["fireAxe", "tradeStation"];
        var w = SetUp(known: known);
        Assert.True(w.M.Research.Knows("fireAxe"));
        Assert.True(w.M.Research.Knows(new Placement("machine", "tradeStation")));
        Assert.False(w.M.Research.Knows("wire"));
        Assert.False(w.M.Research.Knows(new Placement("machine", "furnace")));
        Assert.Equal(known.Order(StringComparer.Ordinal), w.M.Research.Recipes().Where(r => r.Known).Select(r => r.Id).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TakesTheSolarPanelApartForItsPartsAndBuildsItAgainOnceItsBlueprintIsBought()
    {
        // What it is built of comes back, its photosensitive panel too; its blueprint is the black market's (black-market).
        var w = new Scenario(Layout.Parse(["PPP       ", "          "], null) with { Known = ResearchSpec.StartKnown }, doubled: true);
        var panel = new Placement("machine", "solarPanel");
        var cost = Content.Machines.Get("solarPanel").Cost;
        Assert.False(w.M.Research.Knows(panel));
        w.Do(new Deconstruct(At(0, 0), "machine")).Run();
        int mark = w.M.Building.Plans.Single().Id;
        for (int t = 0; t < 400 && w.M.Grid.MachineAt(At(0, 0)) != null; t++) w.Do(new Work(mark)).Run();
        Assert.Null(w.M.Grid.MachineAt(At(0, 0)));
        Assert.All(cost, c => Assert.Equal(c.Count, w.M.Items.Inventory(0).Where(s => s?.Item == c.Item).Sum(s => s!.Count)));
        Assert.False(w.M.Research.Knows(panel));
        w.Do(new PlacePlan(At(0, 0), panel)).Run();
        Assert.Equal(["building.unknown"], w.Refusals.Select(r => r.Reason));
        w.Refusals.Clear();
        w.Do(new BuyBlock("solarPanel")).Run();
        Assert.Equal(["research.locked"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void ListsEveryBlueprintThenEveryCraftWithItsCost()
    {
        var w = SetUp();
        var recipes = w.M.Research.Recipes();
        Assert.Equal(Blueprints.All.Count + Content.Crafts.All.Count, recipes.Count);
        var coil = recipes.Single(r => r.Id == "coil");
        Assert.Null(coil.Placement);
        Assert.Equal(Content.Crafts.Get("coil").Cost, coil.Cost);
    }

    [Fact]
    public void KnowsEveryBlueprintAndCraftOnAMapThatNamesNone()
    {
        var w = new Scenario(Map(null), doubled: true);
        Assert.All(w.M.Research.Recipes().Where(r => ResearchSpec.Node(r.Id) != null), r => Assert.True(r.Known, r.Id));
        Assert.True(w.M.Research.Knows(new Placement("machine", "solarPanel")));
    }

    [Fact]
    public void RefusesAPlanForABlueprintNotKnownButInCreativeMode()
    {
        var w = SetUp();
        var wall = Placement.Wall("iron");
        w.Do(new PlacePlan(At(6, 1), wall)).Run();
        Assert.Equal(new[] { "building.unknown" }, w.Refusals.Select(r => r.Reason));
        Assert.Equal("building.unknown", w.M.Building.Problem(At(6, 1), wall));
        w.Do(new SetCreative(true)).Run();
        w.Do(new PlacePlan(At(6, 1), wall)).Run();
        Assert.Equal(new[] { "building.unknown" }, w.Refusals.Select(r => r.Reason));
        Assert.Single(w.M.Building.Plans);
    }

    /// <summary>The names building asks <c>Knows</c> by.</summary>
    [Fact]
    public void KnowsAPlacementByItsBlueprintNameAndEveryOneInCreativeMode()
    {
        var w = SetUp(known: ["wallScrap"]);
        Assert.Equal("wallIron", Blueprints.Of(Placement.Wall("iron")));
        Assert.Equal("backWallScrap", Blueprints.Of(Placement.BackWall("scrap")));
        Assert.Equal("floor", Blueprints.Of(new Placement("structure", "floor")));
        Assert.Equal("furnace", Blueprints.Of(new Placement("machine", "furnace")));
        Assert.True(w.M.Research.Knows(Placement.Wall("scrap")));
        Assert.False(w.M.Research.Knows(Placement.Wall("iron")));
        w.Do(new SetCreative(true)).Run();
        Assert.True(w.M.Research.Knows(Placement.Wall("iron")));
        Assert.True(w.M.Research.Knows("rotor"));
    }

    [Fact]
    public void SavesAndLoadsCreditsSalesAndWhatIsBought()
    {
        int price = Prices.Of(Node("bag1"));
        var w = SetUp(credits: price + 45);
        w.M.Knowledge.Sell("iron", 5);
        w.Do(new BuyBlock("bag1")).Run();
        var before = w.World.Save();
        w.Reload();
        Assert.Equal(before, w.World.Save());
        Assert.Equal(45, w.M.Research.Credits);
        Assert.Equal(5, w.M.Knowledge.SoldOf("iron"));
        Assert.True(w.M.Research.Knows("bag1"));
    }

    /// <summary>A buy of any block, the first block not bought taken on with what it costs earned, or a unit sold.</summary>
    private sealed record Step(string? Block, string? Sold, bool Next = false);

    /// <summary>The invariant, played through a real station: credits never go negative and move only by a sale's price or a block's.</summary>
    [Property(MaxTest = 40)]
    public Property CreditsMoveOnlyByASaleOrABlock()
    {
        var step = Gen.OneOf(
            Gen.Elements(ResearchSpec.Nodes.ToArray()).Select(n => new Step(n.Id, null)),
            Gen.Constant(new Step(null, null, Next: true)),
            Gen.Constant(new Step(null, null, Next: true)),
            Gen.Elements("scrapChunk", "frame", "processor").Select(i => new Step(null, i)));
        var steps = from n in Gen.Choose(0, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = SetUp();
            int trade = TradeOf(w), credits = 0;
            void Buy(string block)
            {
                int refused = w.Refusals.Count;
                w.Do(new BuyBlock(block)).Run();
                if (w.Refusals.Count == refused) credits -= Prices.Of(Node(block));
            }
            foreach (var s in all)
            {
                if (s.Sold != null)
                {
                    w.FillCrate(trade, (s.Sold, 1));
                    w.Do(new Stillwake.Sim.Trade.SendCapsule(trade)).Run(Market.CapsuleFlight);
                    credits += Market.PriceOf(s.Sold)!.Value;
                }
                else if (s.Block != null) Buy(s.Block);
                else if (ResearchSpec.Nodes.FirstOrDefault(n => !n.Gate && !w.M.Research.Done(n)) is { } next)
                {
                    w.M.Knowledge.Earn(Prices.Of(next));
                    credits += Prices.Of(next);
                    Buy(next.Id);
                }
                if (w.M.Research.Credits != credits || credits < 0) return false;
            }
            return true;
        });
    }

    /// <summary>The market opens to its end through sales and credits alone, each node after what it needs in the order it is listed.</summary>
    [Fact]
    public void TheWholeMarketIsBoughtThroughOrdersAndCredits()
    {
        var w = SetUp(credits: 100_000_000);
        foreach (var node in ResearchSpec.Nodes)
        {
            foreach (var (item, count) in node.Order) w.M.Knowledge.Sell(item, count);
            if (!node.Gate) w.Do(new BuyBlock(node.Id)).Run();
        }
        Assert.Empty(w.Refusals);
        Assert.All(ResearchSpec.Nodes, n => Assert.True(w.M.Research.Done(n), n.Id));
        Assert.All(Upgrades.All, u => Assert.True(w.M.Research.Knows(u.Id), u.Id));
    }
}
