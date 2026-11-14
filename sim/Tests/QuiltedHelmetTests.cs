using Stillwake.Content;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The quilted helmet (2026-11-14-quilted-helmet-and-traced-art): sewn at the workbench round the basic helmet, holding 70; its room has no power, nor needs it.</summary>
public class QuiltedHelmetTests
{
    private const string Room = """
        ##########
        #........#
        #.OOOII..#
        #.OOOII..#
        #.OOOII..#
        ##########
        """;

    /// <summary>A pod in a vacuum room, never aired.</summary>
    private const string PodRoom = """
        ###########
        #.........#
        #..OOO...S#
        #..OOO....#
        #..OOO....#
        ###########
        """;

    private const int Bench = 2;
    private static readonly string Quilted = Helmet.Quilted;
    private static readonly int Head = Inventory.Slots + Inventory.Equipment.ToList().IndexOf(Helmet.Slot);
    private static Crew.Hero HeroOf(Scenario w) => w.M.Crew.HeroOf(0)!;

    [Fact]
    public void TheWorkbenchSewsOneOfTheBasicHelmetThreeClothTwoPlatesAndTwoCanistersWithNoPower()
    {
        (string, int)[] recipe = [(Helmet.Basic, 1), ("cloth", 3), ("plate", 2), ("canister", 2)];
        var w = new Scenario(Layout.Parse(Room) with { Known = [] }, doubled: true).GiveItems(0, recipe);
        Assert.Contains(Stations.Get("workbench").Rows, r => r.Contains(Quilted));
        Assert.Equal(recipe, Crafts.Get(Quilted).Cost);
        w.M.Knowledge.Grant(Quilted);
        w.Do(new Crafting.Craft(Bench, Quilted, 1)).Run(Crafts.Get(Quilted).Ticks * 1000 / Stations.Get("workbench").Speed + 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack(Quilted, 1), w.M.Items.Crate(Bench)[0]);
        Assert.Empty(w.M.Items.Totals(0));
    }

    [Fact]
    public void WornItHolds70BetweenTheBasicAndTheHeavyAndASwapToTheBasicCutsItTo45()
    {
        var w = new Scenario(Layout.Parse(PodRoom) with { Known = [] }, doubled: true) { Bare = true }.Wear(0, Quilted).Wear(0, Content.Suit.Basic);
        // Brought in past the pod's air: printed full to the helmet's tank.
        w.Do(new Crew.Enter(6 * 22 + 14)).Run();
        Assert.Equal((70, 70), (w.M.Crew.CapacityOf(0), HeroOf(w).Air));
        Assert.InRange(Content.Items.Get(Quilted).Tank, Content.Items.Get(Helmet.Basic).Tank + 1, Content.Items.Get(Helmet.Heavy).Tank - 1);
        w.GiveItems(0, (Helmet.Basic, 1));
        int bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == Helmet.Basic);
        w.Do(new Move(new Items.Place(bag), new Target(Head), 1)).Run();
        Assert.Equal(Scenario.Full, w.M.Crew.CapacityOf(0));
        Assert.True(HeroOf(w).Air <= Scenario.Full);
    }
}
