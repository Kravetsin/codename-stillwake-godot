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
    private static readonly int Station = At(1, 1);

    private static readonly string[] Press = ["wire", "plate", "gear"];

    /// <summary>Ten wide, granting <paramref name="known"/> or, with null, everything: a tech station in a room.</summary>
    private static Layout Map(IReadOnlyList<string>? known)
    {
        string[] rows =
        [
            "##########",
            "#AA......#",
            "#AA......#",
            "##########",
        ];
        var layout = Layout.Parse(rows, null);
        return known == null ? layout : layout with { Known = known };
    }

    /// <summary>A world knowing what the wreck does, with <paramref name="credits"/> earned, and its tech station's id.</summary>
    private static (Scenario W, int Id) SetUp(int credits = 0, IReadOnlyList<string>? known = null)
    {
        var w = new Scenario(Map(known ?? ResearchSpec.StartKnown), doubled: true);
        w.M.Knowledge.Earn(credits);
        return (w, w.M.Grid.MachineAt(Station)?.Id ?? 0);
    }

    private static TechTier Tier(string id) => ResearchSpec.Tiers.Single(t => t.Id == id);
    private static TechBlock BlockOf(string id) => ResearchSpec.Block(id)!.Value.Block;

    /// <summary>A world knowing what the wreck does, a tech station and a trade station side by side.</summary>
    private static (Scenario W, int Tech, int Trade) WithTrade(int credits = 0)
    {
        string[] rows =
        [
            "##########",
            "#AA$$....#",
            "#AA$$....#",
            "##########",
        ];
        var w = new Scenario(Layout.Parse(rows, null) with { Known = ResearchSpec.StartKnown }, doubled: true);
        w.M.Knowledge.Earn(credits);
        return (w, w.M.Grid.MachineAt(At(1, 1))!.Id, w.M.Grid.MachineAt(At(3, 1))!.Id);
    }

    /// <summary>Fills every deposit up to and with <paramref name="tier"/>, from what peer 0 is given for it.</summary>
    private static void OpenUpTo(Scenario w, int id, string tier)
    {
        foreach (var t in ResearchSpec.Tiers)
        {
            if (t.Deposit.Count > 0)
            {
                w.GiveItems(0, t.Deposit.ToArray());
                w.Do(new Deposit(id, t.Id)).Run();
            }
            if (t.Id == tier) return;
        }
    }

    [Fact]
    public void BuysABlockOfAnOpenTierSpendingItsPriceAndKnowingAllItOpens()
    {
        var (w, id) = SetUp(credits: 7);
        Assert.False(w.M.Research.Knows("ladder"));
        w.Do(new BuyBlock(id, "floor")).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(2, w.M.Research.Credits);
        Assert.All(BlockOf("floor").Opens, o => Assert.True(w.M.Research.Knows(o), o));
        Assert.True(w.M.Research.Bought(BlockOf("floor")));
    }

    [Fact]
    public void WhatABlockOpensIsPlannedOnceBought()
    {
        var (w, id) = SetUp(credits: 100);
        var floor = new Placement("structure", "floor");
        Assert.Equal("building.unknown", w.M.Building.Problem(At(5, 1), floor));
        w.Do(new BuyBlock(id, "floor")).Run();
        Assert.Null(w.M.Building.Problem(At(5, 1), floor));
    }

    [Fact]
    public void RefusesABuyAtWhatIsNoTechStation()
    {
        var (w, _) = SetUp(credits: 100);
        w.Do(new BuyBlock(999, "floor")).Run();
        Assert.Equal(["research.noMachine"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(100, w.M.Research.Credits);
    }

    [Fact]
    public void RefusesABuyAndADepositAtAMachineOfAnotherKind()
    {
        var (w, _, trade) = WithTrade(credits: 100);
        w.GiveItems(0, ("wire", 5));
        w.Do(new BuyBlock(trade, "floor")).Run();
        w.Do(new Deposit(trade, "press")).Run();
        Assert.Equal(["research.noMachine", "research.noMachine"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(100, w.M.Research.Credits);
        Assert.Equal(5, w.M.Items.Totals(0).Single(t => t.Item == "wire").Count);
    }

    /// <summary>What a map grants may lie in a tier still shut: bought, not locked, and nothing is charged for it.</summary>
    [Fact]
    public void ABlockKnownOfAShutTierIsBoughtNotLocked()
    {
        var (w, id) = SetUp(credits: 100, known: [.. ResearchSpec.StartKnown, .. BlockOf("burnerGenerator").Opens]);
        w.Do(new BuyBlock(id, "burnerGenerator")).Run();
        Assert.Equal(["research.bought"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(100, w.M.Research.Credits);
    }

    [Fact]
    public void RefusesABlockBoughtAlready()
    {
        var (w, id) = SetUp(credits: 100);
        w.Do(new BuyBlock(id, "floor")).Run();
        w.Do(new BuyBlock(id, "floor")).Run();
        Assert.Equal(["research.bought"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(95, w.M.Research.Credits);
    }

    [Fact]
    public void RefusesABlockBeforeTheOneBeforeItInItsBranch()
    {
        var (w, id) = SetUp(credits: 100);
        w.Do(new BuyBlock(id, "door")).Run();
        Assert.Equal(["research.locked"], w.Refusals.Select(r => r.Reason));
        w.Do(new BuyBlock(id, "floor")).Run();
        w.Do(new BuyBlock(id, "door")).Run();
        Assert.Single(w.Refusals);
        Assert.True(w.M.Research.Knows("door"));
    }

    [Fact]
    public void RefusesABlockOfATierStillShutAndOneThereIsNot()
    {
        var (w, id) = SetUp(credits: 1000);
        w.Do(new BuyBlock(id, "burnerGenerator")).Run();
        w.Do(new BuyBlock(id, "noSuchBlock")).Run();
        Assert.Equal(["research.locked", "research.locked"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(1000, w.M.Research.Credits);
    }

    [Fact]
    public void RefusesABlockShortOfCreditsAndLeavesThemAlone()
    {
        var (w, id) = SetUp(credits: 4);
        w.Do(new BuyBlock(id, "floor")).Run();
        Assert.Equal(["research.noCredits"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(4, w.M.Research.Credits);
        Assert.False(w.M.Research.Knows("floor"));
    }

    [Fact]
    public void ADepositFilledOpensItsTierTakingOnlyWhatItNeeds()
    {
        var (w, id) = SetUp(credits: 60);
        var press = Tier("press");
        Assert.False(w.M.Research.Open(press));
        w.GiveItems(0, [.. press.Deposit]);
        w.GiveItems(0, ("wire", 10));
        w.Do(new Deposit(id, "press")).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Research.Open(press));
        Assert.Equal(10, w.M.Items.Totals(0).Where(t => t.Item == "wire").Sum(t => t.Count));
        Assert.DoesNotContain(w.M.Items.Totals(0), t => t.Item is "plate" or "gear");
        w.Do(new BuyBlock(id, "burnerGenerator")).Run();
        Assert.True(w.M.Research.Knows("burnerGenerator"));
    }

    /// <summary>Two peers fill one deposit piece by piece: what is short stays with no one, what is shared stays shared.</summary>
    [Fact]
    public void TwoPeersFillADepositPartWayEachWhatEachCarries()
    {
        var (w, id) = SetUp();
        var press = Tier("press");
        int wire = press.Deposit.Single(d => d.Item == "wire").Count, plate = press.Deposit.Single(d => d.Item == "plate").Count, gear = press.Deposit.Single(d => d.Item == "gear").Count;
        w.GiveItems(0, ("wire", wire - 8), ("gear", gear));
        w.GiveItems(1, ("wire", 12), ("plate", plate - 1));
        w.Do(new Deposit(id, "press")).Run();
        w.Do(new Deposit(id, "press") { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(wire, w.M.Research.Deposited(press, "wire"));
        Assert.Equal(plate - 1, w.M.Research.Deposited(press, "plate"));
        Assert.Equal(gear, w.M.Research.Deposited(press, "gear"));
        Assert.Equal(4, w.M.Items.Totals(1).Where(t => t.Item == "wire").Sum(t => t.Count));
        Assert.False(w.M.Research.Open(press));
        w.GiveItems(0, ("plate", 1));
        w.Do(new Deposit(id, "press")).Run();
        Assert.True(w.M.Research.Open(press));
    }

    [Fact]
    public void RefusesADepositAtWhatIsNoTechStationAndNothingCarried()
    {
        var (w, id) = SetUp();
        w.Do(new Deposit(999, "press")).Run();
        w.Do(new Deposit(id, "press")).Run();
        Assert.Equal(["research.noMachine", "research.nothingToDeposit"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void RefusesADepositOfATierOpenAndOfOneBeyondTheNextShut()
    {
        var (w, id) = SetUp();
        w.GiveItems(0, ("frame", 10), ("gear", 20), ("coil", 20));
        w.Do(new Deposit(id, "salvage")).Run();
        w.Do(new Deposit(id, "processing")).Run();
        w.Do(new Deposit(id, "noSuchTier")).Run();
        Assert.Equal(["research.tierOpen", "research.locked", "research.locked"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(10, w.M.Items.Totals(0).Single(t => t.Item == "frame").Count);
        Assert.Equal(20, w.M.Items.Totals(0).Single(t => t.Item == "gear").Count);
        Assert.Equal(20, w.M.Items.Totals(0).Single(t => t.Item == "coil").Count);
    }

    [Fact]
    public void RefusesADepositOfATierFilledWhateverIsStillCarried()
    {
        var (w, id) = SetUp();
        var press = Tier("press");
        w.GiveItems(0, [.. press.Deposit]);
        w.Do(new Deposit(id, "press")).Run();
        w.GiveItems(0, ("wire", 3));
        w.Do(new Deposit(id, "press")).Run();
        Assert.Equal(["research.tierOpen"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(3, w.M.Items.Totals(0).Single(t => t.Item == "wire").Count);
    }

    /// <summary>The wire is all in and only wire is carried: nothing it asks for still can go in.</summary>
    [Fact]
    public void RefusesADepositOfWhatIsFullAlready()
    {
        var (w, id) = SetUp();
        var press = Tier("press");
        int wire = press.Deposit.Single(d => d.Item == "wire").Count;
        w.GiveItems(0, ("wire", wire + 3));
        w.Do(new Deposit(id, "press")).Run();
        Assert.Empty(w.Refusals);
        w.Do(new Deposit(id, "press")).Run();
        Assert.Equal(["research.nothingToDeposit"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(3, w.M.Items.Totals(0).Single(t => t.Item == "wire").Count);
    }

    /// <summary>One unit short keeps the next tier shut.</summary>
    [Fact]
    public void RefusesADepositOfTheNextTierWhileThisOneIsAUnitShort()
    {
        var (w, id) = SetUp();
        var press = Tier("press");
        w.GiveItems(0, [.. press.Deposit.Select(d => d.Item == "plate" ? (d.Item, d.Count - 1) : d)]);
        w.Do(new Deposit(id, "press")).Run();
        w.GiveItems(0, ("frame", 1));
        w.Do(new Deposit(id, "processing")).Run();
        Assert.Equal(["research.locked"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(1, w.M.Items.Totals(0).Single(t => t.Item == "frame").Count);
    }

    /// <summary>Two peers' saves must match byte for byte, whichever item went in first.</summary>
    [Fact]
    public void DepositsSaveAlikeWhicheverWentInFirst()
    {
        string Saved(params string[] order)
        {
            var (w, id) = SetUp();
            foreach (string item in order)
            {
                w.GiveItems(0, (item, 1));
                w.Do(new Deposit(id, "press")).Run();
            }
            return w.World.Save();
        }
        Assert.Equal(Saved("wire", "plate", "gear"), Saved("gear", "plate", "wire"));
    }

    [Fact]
    public void CreativeModeKnowsAndOpensEverythingAndTakesNothing()
    {
        var (w, id) = SetUp(credits: 3);
        w.GiveItems(0, ("wire", 20));
        w.Do(new SetCreative(true)).Run();
        Assert.True(w.M.Research.Knows("engine"));
        Assert.All(ResearchSpec.Tiers, t => Assert.True(w.M.Research.Open(t)));
        w.Do(new Deposit(id, "press")).Run();
        w.Do(new BuyBlock(id, "engine")).Run();
        Assert.Equal(["research.tierOpen", "research.bought"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(3, w.M.Research.Credits);
        Assert.Equal(20, w.M.Items.Totals(0).Single(t => t.Item == "wire").Count);
    }

    [Fact]
    public void KnowsWhatIsGrantedTheWireCraftAmongIt()
    {
        var (w, _) = SetUp();
        Assert.True(w.M.Research.Knows("wire"));
        Assert.True(w.M.Research.Knows(new Placement("machine", "tradeStation")));
        Assert.False(w.M.Research.Knows("coil"));
        Assert.Equal(ResearchSpec.StartKnown.Order(StringComparer.Ordinal), w.M.Research.Recipes().Where(r => r.Known).Select(r => r.Id).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ListsEveryBlueprintThenEveryCraftWithItsCost()
    {
        var (w, _) = SetUp();
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
        var (w, _) = SetUp();
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
        var (w, _) = SetUp();
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
        var (w, id) = SetUp(credits: 50);
        w.GiveItems(0, ("wire", 5));
        w.Do(new BuyBlock(id, "floor")).Run();
        w.Do(new Deposit(id, "press")).Run();
        var before = w.World.Save();
        w.Reload();
        Assert.Equal(before, w.World.Save());
        Assert.Equal(45, w.M.Research.Credits);
        Assert.Equal(5, w.M.Research.Deposited(Tier("press"), "wire"));
        Assert.True(w.M.Research.Knows("ladder"));
    }

    /// <summary>A buy of any block, a deposit at any tier (the next shut one, with what it asks for given, for an empty tier), or a unit sold.</summary>
    private sealed record Step(string? Block, string? Tier, string? Sold);

    /// <summary>
    /// The invariant, played through a real station: credits never go negative and move only by a sale's price or a block's,
    /// a deposit never goes over what it asks, and what was given is always in the bag or in a deposit.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property CreditsMoveOnlyByASaleOrABlockAndDepositsOnlyByWhatTheBagGave()
    {
        var blocks = ResearchSpec.Tiers.SelectMany(t => t.Branches).SelectMany(b => b.Blocks).ToList();
        var step = Gen.OneOf(
            Gen.Elements(blocks.ToArray()).Select(b => new Step(b.Id, null, null)),
            Gen.Elements(ResearchSpec.Tiers.Select(t => t.Id).ToArray()).Select(t => new Step(null, t, null)),
            Gen.Constant(new Step(null, "", null)),
            Gen.Elements("scrapChunk", "frame", "processor", "deckDrive").Select(i => new Step(null, null, i)));
        var steps = from n in Gen.Choose(0, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var (w, tech, trade) = WithTrade();
            int credits = 0;
            var given = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var s in all)
            {
                if (s.Sold != null)
                {
                    w.FillCrate(trade, (s.Sold, 1));
                    w.Run(Market.SellEvery);
                    credits += Market.PriceOf(s.Sold)!.Value;
                }
                else if (s.Tier != null)
                {
                    var tier = s.Tier == "" ? ResearchSpec.Tiers.FirstOrDefault(t => !w.M.Research.Open(t)) : Tier(s.Tier);
                    if (tier == null) continue;
                    if (s.Tier == "")
                        foreach (var (item, count) in tier.Deposit)
                        {
                            // More than half: two fill it, and the second leaves some carried.
                            w.GiveItems(0, (item, count / 2 + 1));
                            given[item] = given.GetValueOrDefault(item) + count / 2 + 1;
                        }
                    w.Do(new Deposit(tech, tier.Id)).Run();
                }
                else
                {
                    int refused = w.Refusals.Count;
                    w.Do(new BuyBlock(tech, s.Block!)).Run();
                    if (w.Refusals.Count == refused) credits -= BlockOf(s.Block!).Price;
                }
                if (w.M.Research.Credits != credits || credits < 0) return false;
                if (ResearchSpec.Tiers.Any(t => t.Deposit.Any(d => w.M.Research.Deposited(t, d.Item) > d.Count))) return false;
                var carried = w.M.Items.Totals(0);
                foreach (string item in given.Keys.Order(StringComparer.Ordinal))
                {
                    int put = ResearchSpec.Tiers.Sum(t => t.Deposit.Any(d => d.Item == item) ? w.M.Research.Deposited(t, item) : 0);
                    if (put + carried.Where(t => t.Item == item).Sum(t => t.Count) != given[item]) return false;
                }
            }
            return true;
        });
    }

    /// <summary>The tree opens to its end through the commands alone, tier by tier.</summary>
    [Fact]
    public void TheWholeTreeIsBoughtToTheEngineThroughDepositsAndCredits()
    {
        var (w, id) = SetUp(credits: 1_000_000);
        OpenUpTo(w, id, "arcAndEngine");
        foreach (var tier in ResearchSpec.Tiers)
            foreach (var branch in tier.Branches)
                foreach (var block in branch.Blocks)
                    w.Do(new BuyBlock(id, block.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.All(w.M.Research.Recipes(), r => Assert.True(r.Known, r.Id));
    }
}
