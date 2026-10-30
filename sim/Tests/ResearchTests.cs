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

/// <summary>The tech tree (2026-10-30-trade-and-tech): tiers opened by deposits, blocks bought with credits, what is bought known.</summary>
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

    /// <summary>What every block <paramref name="id"/> leads through opens, so it is reached: its gates must be filled apart.</summary>
    private static string[] Before(string id)
    {
        var all = new List<string>();
        void Walk(TechNode node)
        {
            foreach (string need in node.Needs)
            {
                Walk(Node(need));
                all.AddRange(Node(need).Opens);
            }
        }
        Walk(Node(id));
        return [.. all.Distinct()];
    }

    /// <summary>Gives peer 0 what <paramref name="gate"/> asks for and puts it in.</summary>
    private static void Fill(Scenario w, TechNode gate)
    {
        w.GiveItems(0, [.. gate.Deposit]);
        w.Do(new Deposit(gate.Id)).Run();
    }

    [Fact]
    public void BuysABlockSpendingItsPriceAndKnowingAllItOpens()
    {
        var floor = Node("floor");
        var w = SetUp(credits: Prices.Of(floor) + 2, known: Before("floor"));
        Assert.False(w.M.Research.Knows("ladder"));
        w.Do(new BuyBlock("floor")).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(2, w.M.Research.Credits);
        Assert.All(floor.Opens, o => Assert.True(w.M.Research.Knows(o), o));
        Assert.True(w.M.Research.Done(floor));
    }

    [Fact]
    public void WhatABlockOpensIsPlannedOnceBought()
    {
        var w = SetUp(credits: 100_000, known: Before("floor"));
        var floor = new Placement("structure", "floor");
        Assert.Equal("building.unknown", w.M.Building.Problem(At(7, 1), floor));
        w.Do(new BuyBlock("floor")).Run();
        Assert.Null(w.M.Building.Problem(At(7, 1), floor));
    }

    /// <summary>The tablet needs no machine (2026-10-30-tablet-tree): a world with none buys and deposits all the same.</summary>
    [Fact]
    public void BuysAndDepositsWithNoMachineInTheWorld()
    {
        var w = new Scenario(Layout.Parse(["##########", "#........#", "#........#", "##########"], null) with { Known = [.. ResearchSpec.StartKnown, .. Before("press")] }, doubled: true);
        w.M.Knowledge.Earn(100_000);
        w.GiveItems(0, ("wire", 5));
        w.Do(new BuyBlock("floor")).Do(new Deposit("press")).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Research.Knows("floor"));
        Assert.Equal(5, w.M.Research.Deposited(Node("press"), "wire"));
    }

    /// <summary>What a map grants may lie behind what is not done: bought, not locked, and nothing is charged for it.</summary>
    [Fact]
    public void ABlockKnownBeyondAShutGateIsBoughtNotLocked()
    {
        var w = SetUp(credits: 100, known: Node("coil").Opens);
        w.Do(new BuyBlock("coil")).Run();
        Assert.Equal(["research.bought"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(100, w.M.Research.Credits);
    }

    [Fact]
    public void RefusesABlockBoughtAlready()
    {
        var w = SetUp(credits: 100_000);
        w.Do(new BuyBlock("workbench")).Run();
        w.Do(new BuyBlock("workbench")).Run();
        Assert.Equal(["research.bought"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(100_000 - Prices.Of(Node("workbench")), w.M.Research.Credits);
    }

    /// <summary>Wire needs both the workbench and the furnace: one is not enough, whichever is bought.</summary>
    [Fact]
    public void RefusesABlockUntilEverythingItNeedsIsDone()
    {
        var w = SetUp(credits: 100_000);
        w.Do(new BuyBlock("wire")).Run();
        w.Do(new BuyBlock("workbench")).Run();
        w.Do(new BuyBlock("wire")).Run();
        Assert.Equal(["research.locked", "research.locked"], w.Refusals.Select(r => r.Reason));
        w.Do(new BuyBlock("furnace")).Run();
        w.Do(new BuyBlock("wire")).Run();
        Assert.Equal(2, w.Refusals.Count);
        Assert.True(w.M.Research.Knows("wire"));
    }

    [Fact]
    public void RefusesABlockBehindAGateNotFullOneThereIsNotAndAGateItself()
    {
        var w = SetUp(credits: 100_000, known: Before("press"));
        w.Do(new BuyBlock("coil")).Run();
        w.Do(new BuyBlock("noSuchBlock")).Run();
        w.Do(new BuyBlock("press")).Run();
        Assert.Equal(["research.locked", "research.locked", "research.locked"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(100_000, w.M.Research.Credits);
    }

    [Fact]
    public void RefusesABlockShortOfCreditsAndLeavesThemAlone()
    {
        int price = Prices.Of(Node("workbench"));
        var w = SetUp(credits: price - 1);
        w.Do(new BuyBlock("workbench")).Run();
        Assert.Equal(["research.noCredits"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(price - 1, w.M.Research.Credits);
        Assert.False(w.M.Research.Knows("workbench"));
    }

    [Fact]
    public void AGateFilledOpensWhatNeedsItTakingOnlyWhatItAsks()
    {
        var w = SetUp(credits: 100_000, known: Before("press"));
        var press = Node("press");
        Assert.False(w.M.Research.Done(press));
        w.GiveItems(0, [.. press.Deposit]);
        w.GiveItems(0, ("wire", 10));
        w.Do(new Deposit("press")).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Research.Done(press));
        Assert.Equal(10, w.M.Items.Totals(0).Where(t => t.Item == "wire").Sum(t => t.Count));
        Assert.DoesNotContain(w.M.Items.Totals(0), t => t.Item is "plate" or "gear");
        w.Do(new BuyBlock("coil")).Run();
        Assert.True(w.M.Research.Knows("coil"));
    }

    /// <summary>Two peers fill one gate piece by piece: what is short stays with no one, what is shared stays shared.</summary>
    [Fact]
    public void TwoPeersFillAGatePartWayEachWhatEachCarries()
    {
        var w = SetUp(known: Before("press"));
        var press = Node("press");
        int wire = press.Deposit.Single(d => d.Item == "wire").Count, plate = press.Deposit.Single(d => d.Item == "plate").Count, gear = press.Deposit.Single(d => d.Item == "gear").Count;
        w.GiveItems(0, ("wire", wire - 8), ("gear", gear));
        w.GiveItems(1, ("wire", 12), ("plate", plate - 1));
        w.Do(new Deposit("press")).Run();
        w.Do(new Deposit("press") { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(wire, w.M.Research.Deposited(press, "wire"));
        Assert.Equal(plate - 1, w.M.Research.Deposited(press, "plate"));
        Assert.Equal(gear, w.M.Research.Deposited(press, "gear"));
        Assert.Equal(4, w.M.Items.Totals(1).Where(t => t.Item == "wire").Sum(t => t.Count));
        Assert.False(w.M.Research.Done(press));
        w.GiveItems(0, ("plate", 1));
        w.Do(new Deposit("press")).Run();
        Assert.True(w.M.Research.Done(press));
    }

    [Fact]
    public void RefusesADepositWithNothingCarried()
    {
        var w = SetUp(known: Before("press"));
        w.Do(new Deposit("press")).Run();
        Assert.Equal(["research.nothingToDeposit"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void RefusesADepositOfAGateNotReachedABlockAndNoSuchGate()
    {
        var w = SetUp();
        w.GiveItems(0, ("wire", 20), ("plate", 15), ("gear", 10));
        w.Do(new Deposit("press")).Run();
        w.Do(new Deposit("workbench")).Run();
        w.Do(new Deposit("noSuchGate")).Run();
        Assert.Equal(["research.locked", "research.locked", "research.locked"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(20, w.M.Items.Totals(0).Single(t => t.Item == "wire").Count);
        Assert.Equal(15, w.M.Items.Totals(0).Single(t => t.Item == "plate").Count);
        Assert.Equal(10, w.M.Items.Totals(0).Single(t => t.Item == "gear").Count);
    }

    [Fact]
    public void RefusesADepositOfAGateFilledWhateverIsStillCarried()
    {
        var w = SetUp(known: Before("press"));
        Fill(w, Node("press"));
        w.GiveItems(0, ("wire", 3));
        w.Do(new Deposit("press")).Run();
        Assert.Equal(["research.gateOpen"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(3, w.M.Items.Totals(0).Single(t => t.Item == "wire").Count);
    }

    /// <summary>The wire is all in and only wire is carried: nothing it asks for still can go in.</summary>
    [Fact]
    public void RefusesADepositOfWhatIsFullAlready()
    {
        var w = SetUp(known: Before("press"));
        int wire = Node("press").Deposit.Single(d => d.Item == "wire").Count;
        w.GiveItems(0, ("wire", wire + 3));
        w.Do(new Deposit("press")).Run();
        Assert.Empty(w.Refusals);
        w.Do(new Deposit("press")).Run();
        Assert.Equal(["research.nothingToDeposit"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(3, w.M.Items.Totals(0).Single(t => t.Item == "wire").Count);
    }

    /// <summary>Two peers' saves must match byte for byte, whichever item went in first.</summary>
    [Fact]
    public void DepositsSaveAlikeWhicheverWentInFirst()
    {
        string Saved(params string[] order)
        {
            var w = SetUp(known: Before("press"));
            foreach (string item in order)
            {
                w.GiveItems(0, (item, 1));
                w.Do(new Deposit("press")).Run();
            }
            return w.World.Save();
        }
        Assert.Equal(Saved("wire", "plate", "gear"), Saved("gear", "plate", "wire"));
    }

    [Fact]
    public void CreativeModeKnowsAndOpensEverythingAndTakesNothing()
    {
        var w = SetUp(credits: 3);
        w.GiveItems(0, ("wire", 20));
        w.Do(new SetCreative(true)).Run();
        Assert.True(w.M.Research.Knows("engine"));
        Assert.All(ResearchSpec.Nodes, n => Assert.True(w.M.Research.Done(n), n.Id));
        w.Do(new Deposit("press")).Run();
        w.Do(new BuyBlock("engine")).Run();
        Assert.Equal(["research.gateOpen", "research.bought"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(3, w.M.Research.Credits);
        Assert.Equal(20, w.M.Items.Totals(0).Single(t => t.Item == "wire").Count);
    }

    [Fact]
    public void KnowsWhatIsGrantedAndNoMore()
    {
        var w = SetUp();
        Assert.True(w.M.Research.Knows("fireAxe"));
        Assert.True(w.M.Research.Knows(new Placement("machine", "tradeStation")));
        Assert.False(w.M.Research.Knows("wire"));
        Assert.False(w.M.Research.Knows(new Placement("machine", "furnace")));
        Assert.Equal(ResearchSpec.StartKnown.Order(StringComparer.Ordinal), w.M.Research.Recipes().Where(r => r.Known).Select(r => r.Id).Order(StringComparer.Ordinal));
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
        Assert.All(w.M.Research.Recipes(), r => Assert.True(r.Known, r.Id));
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
        var w = SetUp();
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
    public void SavesAndLoadsCreditsDepositsAndWhatIsBought()
    {
        int price = Prices.Of(Node("floor"));
        var w = SetUp(credits: price + 45, known: Before("press"));
        w.GiveItems(0, ("wire", 5));
        w.Do(new BuyBlock("floor")).Run();
        w.Do(new Deposit("press")).Run();
        var before = w.World.Save();
        w.Reload();
        Assert.Equal(before, w.World.Save());
        Assert.Equal(45, w.M.Research.Credits);
        Assert.Equal(5, w.M.Research.Deposited(Node("press"), "wire"));
        Assert.True(w.M.Research.Knows("ladder"));
    }

    /// <summary>A buy of any block, a deposit at any gate, the first node not done taken on, or a unit sold.</summary>
    private sealed record Step(string? Block, string? Gate, string? Sold, bool Next = false);

    /// <summary>
    /// The invariant, played through a real station: credits never go negative and move only by a sale's price or a block's,
    /// a gate never holds over what it asks, and what was given is always in the bag or in a gate.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property CreditsMoveOnlyByASaleOrABlockAndDepositsOnlyByWhatTheBagGave()
    {
        var nodes = ResearchSpec.Nodes.ToArray();
        var step = Gen.OneOf(
            Gen.Elements(nodes.Where(n => !n.Gate).ToArray()).Select(b => new Step(b.Id, null, null)),
            Gen.Elements(nodes.Where(n => n.Gate).ToArray()).Select(g => new Step(null, g.Id, null)),
            Gen.Constant(new Step(null, null, null, Next: true)),
            Gen.Constant(new Step(null, null, null, Next: true)),
            Gen.Elements("scrapChunk", "frame", "processor").Select(i => new Step(null, null, i)));
        var steps = from n in Gen.Choose(0, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = SetUp();
            int trade = TradeOf(w), credits = 0;
            var given = new Dictionary<string, int>(StringComparer.Ordinal);
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
                    w.Run(Market.SellEvery);
                    credits += Market.PriceOf(s.Sold)!.Value;
                }
                else if (s.Gate != null) w.Do(new Deposit(s.Gate)).Run();
                else if (s.Block != null) Buy(s.Block);
                else if (ResearchSpec.Nodes.FirstOrDefault(n => !w.M.Research.Done(n)) is { } next)
                {
                    if (next.Gate)
                    {
                        // More than half: two fill it, and the second leaves some carried.
                        foreach (var (item, count) in next.Deposit)
                        {
                            w.GiveItems(0, (item, count / 2 + 1));
                            given[item] = given.GetValueOrDefault(item) + count / 2 + 1;
                        }
                        w.Do(new Deposit(next.Id)).Run();
                    }
                    else
                    {
                        w.M.Knowledge.Earn(Prices.Of(next));
                        credits += Prices.Of(next);
                        Buy(next.Id);
                    }
                }
                if (w.M.Research.Credits != credits || credits < 0) return false;
                if (ResearchSpec.Nodes.Any(n => n.Deposit.Any(d => w.M.Research.Deposited(n, d.Item) > d.Count))) return false;
                var carried = w.M.Items.Totals(0);
                foreach (string item in given.Keys.Order(StringComparer.Ordinal))
                {
                    int put = ResearchSpec.Nodes.Sum(n => n.Deposit.Any(d => d.Item == item) ? w.M.Research.Deposited(n, item) : 0);
                    if (put + carried.Where(t => t.Item == item).Sum(t => t.Count) != given[item]) return false;
                }
            }
            return true;
        });
    }

    /// <summary>The tree opens to its end through the commands alone, each node after what it needs in the order it is listed.</summary>
    [Fact]
    public void TheWholeTreeIsBoughtToTheEngineThroughGatesAndCredits()
    {
        var w = SetUp(credits: 100_000_000);
        foreach (var node in ResearchSpec.Nodes)
        {
            if (node.Gate) Fill(w, node);
            else w.Do(new BuyBlock(node.Id)).Run();
        }
        Assert.Empty(w.Refusals);
        Assert.All(w.M.Research.Recipes(), r => Assert.True(r.Known, r.Id));
    }
}
