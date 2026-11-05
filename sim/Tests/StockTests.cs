using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crafting;
using Xunit;
using Crafts = Stillwake.Content.Crafts;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The stock (craft-from-crates): a paid craft takes from what the sender carries, then from every crate on the ship by
/// id, however far; a cancel gives back to the sender, then to the crates.
/// </summary>
public class StockTests
{
    /// <summary>A workbench and a crate beside it, and a second crate behind a wall with no door.</summary>
    private const string Ship = """
        ##############################
        #.............#..............#
        #.II..........#..............#
        #.II..........#..............#
        #.IIKK........#............KK#
        ##############################
        """;
    private const int Bench = 1, Near = 2, Far = 3;
    private static readonly int WireScrap = Crafts.Get("wire").Cost.Single().Count;
    private static readonly int CanisterStack = Content.Items.Get("canister").Stack;
    private static readonly int CrateSlots = Content.Machines.Get("crate").Slots!.Value;

    private static Scenario World() => new(Layout.Parse(Ship), doubled: true);

    private static int Carried(Scenario w, string item, int peer = 0) =>
        w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);

    private static int Held(Scenario w, int crate, string item) =>
        w.M.Items.Crate(crate).Sum(s => s?.Item == item ? s.Count : 0);

    private static Scenario Craft(Scenario w, string recipe, int count = 1, int from = 0) =>
        w.Do(new Craft(Bench, recipe, count) { From = from }).Run();

    private static Scenario FullBag(Scenario w, int peer = 0) =>
        w.GiveItems(peer, ("canister", Content.Inventory.Slots * CanisterStack));

    private static Scenario FullCrate(Scenario w, int crate) => w.FillCrate(crate, ("canister", CrateSlots * CanisterStack));

    [Fact]
    public void TheMapHasTheBenchAndBothCratesWhereTheTestsSay()
    {
        var w = World();
        Assert.Equal(["workbench", "crate", "crate"], w.M.Grid.Machines.Select(m => m.Kind));
        Assert.Equal([Bench, Near, Far], w.M.Grid.Machines.Select(m => m.Id));
    }

    [Fact]
    public void PaysFromACrateBehindAWallWhenTheHeroCarriesNothing()
    {
        var w = World().FillCrate(Far, ("scrap", 10));
        Assert.Equal([("scrap", 10)], w.M.Items.StockTotals(0));
        Craft(w, "wire", 2);
        Assert.Empty(w.Refusals);
        Assert.Equal([new Entry(1, "wire", 2, 0)], w.M.Crafting.Queue(Bench));
        Assert.Equal(10 - 2 * WireScrap, Held(w, Far, "scrap"));
    }

    [Fact]
    public void TakesWhatTheHeroCarriesFirstThenEachCrateByIdLeavingTheRest()
    {
        var w = World().GiveItems(0, ("scrap", 1)).FillCrate(Near, ("scrap", 1)).FillCrate(Far, ("scrap", 20));
        Craft(w, "wire", 3);
        Assert.Empty(w.Refusals);
        Assert.Equal((0, 0, 20 - (3 * WireScrap - 2)), (Carried(w, "scrap"), Held(w, Near, "scrap"), Held(w, Far, "scrap")));
        // A crate emptied by a craft keeps no entry, as one emptied by hand.
        Assert.DoesNotContain(Near, w.Stored().Crates);
    }

    [Fact]
    public void PaysOnlyFromCratesNotAStationsSlots()
    {
        var w = World().FillCrate(Bench, ("scrap", 10));
        Craft(w, "wire");
        Assert.Equal(["items.notEnough"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(10, Held(w, Bench, "scrap"));
    }

    [Fact]
    public void RefusesWhenTheHeroAndEveryCrateTogetherHoldTooLittleTakingNothing()
    {
        var w = World().GiveItems(0, ("scrap", WireScrap)).FillCrate(Near, ("scrap", WireScrap)).FillCrate(Far, ("scrap", WireScrap - 1));
        Craft(w, "wire", 3);
        Assert.Equal(["items.notEnough"], w.Refusals.Select(r => r.Reason));
        Assert.Equal((WireScrap, WireScrap, WireScrap - 1), (Carried(w, "scrap"), Held(w, Near, "scrap"), Held(w, Far, "scrap")));
        Assert.Empty(w.M.Crafting.Queue(Bench));
    }

    [Fact]
    public void ACancelGivesBackToTheHeroThenToTheFirstCrateWithRoom()
    {
        var w = World().FillCrate(Far, ("scrap", 10));
        Craft(w, "wire", 2);
        w.Do(new Cancel(Bench, 1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((2 * WireScrap, 10 - 2 * WireScrap), (Carried(w, "scrap"), Held(w, Far, "scrap")));

        // The bag full: what does not fit goes to the near crate, the first by id with room.
        Craft(w, "wire", 2);
        FullBag(w);
        w.Do(new Cancel(Bench, 2)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(2 * WireScrap, Held(w, Near, "scrap"));
    }

    [Fact]
    public void RefusesACancelWhoseRefundFitsNeitherTheBagNorAnyCrate()
    {
        var w = World().GiveItems(0, ("scrap", 2 * WireScrap));
        Craft(w, "wire", 2);
        FullBag(FullCrate(FullCrate(w, Near), Far));
        w.Do(new Cancel(Bench, 1)).Run();
        Assert.Equal("items.full", w.Refusals[^1].Reason);
        Assert.Single(w.M.Crafting.Queue(Bench));
    }

    /// <summary>
    /// Whatever the hero and the two crates hold, a paid craft takes exactly its cost or nothing, the hero's first, then the
    /// near crate's, then the far one's; a cancel gives exactly that back.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TakesExactlyTheCostInOrderOrNothingAndACancelGivesItBack() =>
        Prop.ForAll(
            (from hero in Gen.Choose(0, 30)
             from near in Gen.Choose(0, 30)
             from far in Gen.Choose(0, 30)
             from count in Gen.Choose(1, 9)
             select (hero, near, far, count)).ToArbitrary(),
            held =>
            {
                var (hero, near, far, count) = held;
                var w = World();
                if (hero > 0) w.GiveItems(0, ("scrap", hero));
                if (near > 0) w.FillCrate(Near, ("scrap", near));
                if (far > 0) w.FillCrate(Far, ("scrap", far));
                int cost = count * WireScrap, all = hero + near + far;
                Craft(w, "wire", count);
                (int, int, int) now = (Carried(w, "scrap"), Held(w, Near, "scrap"), Held(w, Far, "scrap"));
                if (cost > all) return w.Refusals.Count == 1 && now == (hero, near, far);
                int fromHero = Math.Min(hero, cost), fromNear = Math.Min(near, cost - fromHero);
                if (now != (hero - fromHero, near - fromNear, far - (cost - fromHero - fromNear))) return false;
                w.Do(new Cancel(Bench, 1)).Run();
                return w.Refusals.Count == 0 && Carried(w, "scrap") + Held(w, Near, "scrap") + Held(w, Far, "scrap") == all;
            });
}
