using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Space's food (food-and-the-body, rule 7): a drifting fridge and a derelict's vending machine give rations and the only
/// canned meat, and a seed is eaten as a snack.
/// </summary>
public class SpaceFoodTests
{
    /// <summary>A room on the left, space right of it, as <c>DebrisFoundTests</c>' map.</summary>
    private static readonly Layout Space = Layout.Parse(
    [
        " #####              ",
        " #...#              ",
        " #...#              ",
        " #.S.#              ",
        " #...#              ",
        " #...#              ",
        " #...#              ",
        " #####              ",
    ]);

    private const string Fridge = "fridge";
    private static int Carried(Scenario w, string item) => w.M.Items.Inventory(0).Sum(s => s?.Item == item ? s.Count : 0);

    [Fact]
    public void AFridgeDriftsBesideTheSupplyPodTenOnTheStreamTwelveInEveryFarTierAndTheNetCatchesIt()
    {
        Assert.Equal(10, DebrisSpec.Kinds.Single(k => k.Item == Fridge).Weight);
        Assert.All(DebrisSpec.Far.Tiers, t => Assert.Equal(12, t.Kinds.Single(k => k.Item == Fridge).Weight));
        Assert.Contains(Fridge, DebrisSpec.NetCatches);
        Assert.Equal(ItemGroup.Other, Content.Items.Get(Fridge).Group);
        Assert.Equal(2, Looks.CountOf(Fridge));
    }

    [Fact]
    public void AFridgeHoldsOneToThreeRationsAndUpToTwoCannedMeatTheOnlyCannedMeatThereIs()
    {
        Assert.Equal([new LootEntry(Foods.Ration, 1, 3), new LootEntry(Foods.CannedMeat, 0, 2)], Loot.Of(Fridge));
        Assert.Equal([Fridge], Loot.All.Where(l => l.Entries.Any(e => e.Item == Foods.CannedMeat)).Select(l => l.Container));
        var rng = new Rng(new RngState { S = 5 });
        var rations = new HashSet<int>();
        var meat = new HashSet<int>();
        for (int i = 0; i < 600; i++)
        {
            var loot = DebrisModule.LootOf(Fridge, rng);
            Assert.All(loot, l => Assert.Contains(l.Item, new[] { Foods.Ration, Foods.CannedMeat }));
            rations.Add(loot.Single(l => l.Item == Foods.Ration).Count);
            meat.Add(loot.Where(l => l.Item == Foods.CannedMeat).Sum(l => l.Count));
        }
        Assert.Equal([1, 2, 3], rations.Order());
        Assert.Equal([0, 1, 2], meat.Order());
    }

    [Fact]
    public void AFridgeCaughtByHandOpensIntoItsFood()
    {
        var w = new Scenario(Space).Run(DebrisSpec.Every);
        var piece = w.M.Debris.Pieces[0];
        piece.Item = Fridge;
        w.Do(new Enter(1 * 20 + 12)).Run().Do(new Grab(piece.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.InRange(Carried(w, Foods.Ration), 1, 3);
        Assert.InRange(Carried(w, Foods.CannedMeat), 0, 2);
        Assert.Equal(0, Carried(w, Fridge));
    }

    [Fact]
    public void TheVendingMachineIsAFridgeFiveTimesOverInADerelictsMessInTwoLooksStillGlowing()
    {
        var vending = Vessels.Salvage.Single(s => s.From == Fridge);
        Assert.Equal(("salvageVending", 2, (4, 5)), (vending.Kind, vending.Looks, vending.Size!.Value));
        Assert.Equal([VesselRole.Quarters], vending.Roles);
        Assert.Equal([(Foods.Ration, 10), (Foods.CannedMeat, 5)], Vessels.SalvageCost(vending));
        foreach (var kind in new[] { "salvageVending", "salvageVending2" })
        {
            Assert.True(Machines.Is(kind, MachineTrait.Glow), kind);
            Assert.Null(Machines.Get(kind).Draws);
            Assert.Equal(vending, Vessels.SalvageOf(kind));
        }
        Assert.Contains(Vessels.Interiors, i => i.Role == VesselRole.Quarters && i.Rows.Any(r => r.Contains(vending.Char)));
    }

    /// <summary>A blow of the sledgehammer from inside its top-left cell breaks it for its cost, rations and canned meat.</summary>
    [Fact]
    public void ASledgehammerBreaksAVendingMachineForItsFood()
    {
        string[] rows =
        [
            "##########",
            "#........#",
            "#.6666...#",
            "#.6666...#",
            "#.6666...#",
            "#.6666...#",
            "#S6666...#",
            "##########",
        ];
        var w = new Scenario(string.Join('\n', rows), cells: true);
        const int width = 10;
        var vending = w.M.Grid.MachinesOf("salvageVending").Single();
        string hammer = Weapons.Sledgehammer.Item;
        w.GiveItems(0, (hammer, 1)).Do(new Enter(vending.Cell)).Run();
        w.Do(new Hew(hammer, vending.Cell % width * 1000 + 500, vending.Cell / width * 1000 + 500, HewTesting.Up)).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.MachineById(vending.Id));
        Assert.Equal((10, 5), (Carried(w, Foods.Ration), Carried(w, Foods.CannedMeat)));
    }

    [Fact]
    public void ASeedIsASnackOfFiveMinutesWithNoRatesAndStaysForPlantingAndSale()
    {
        Assert.Equal(new Food(Foods.Seed, 6_000, new()), Foods.Of(Foods.Seed));
        Assert.Equal("grow", Content.Items.Get(Foods.Seed).Process);
        Assert.NotNull(Market.PriceOf(Foods.Seed));
        Assert.All([Foods.Ration, Foods.CannedMeat, Foods.Fruit, Foods.Glowberry], f => Assert.Null(Market.PriceOf(f)));
    }

    /// <summary>A hungry hero eats a seed: no longer hungry for its 6 000 ticks, with no rate, then hungry again.</summary>
    [Fact]
    public void ASeedEndsHungerForItsTicksAndAddsNoRate()
    {
        var w = new Scenario("S.......").GiveItems(0, (Foods.Seed, 2)).Do(new Enter(1)).Run();
        var hero = w.M.Crew.HeroOf(0)!;
        hero.Eaten.Clear();
        Assert.True(w.M.Crew.Hungry(0));
        w.Do(new Eat(Foods.Seed)).Run();
        Assert.Empty(w.Refusals);
        Assert.False(w.M.Crew.Hungry(0));
        Assert.Equal(default, w.M.Crew.RatesOf(0));
        Assert.Equal(1, Carried(w, Foods.Seed));
        for (int left = hero.Meals[0].Left; left > 1; left -= 1000)
        {
            hero.Air = Scenario.Full;
            w.Run(Math.Min(1000, left - 1));
        }
        Assert.False(w.M.Crew.Hungry(0));
        w.Run();
        Assert.True(w.M.Crew.Hungry(0));
    }

    /// <summary>A seed is refused as any food is: the same seed again while it lasts, and none carried.</summary>
    [Fact]
    public void ASecondSeedIsRefusedWhileTheFirstLastsAndNoneCarriedIsRefused()
    {
        var w = new Scenario("S.......").GiveItems(0, (Foods.Seed, 2)).Do(new Enter(1)).Run();
        w.Do(new Eat(Foods.Seed)).Run().Do(new Eat(Foods.Seed)).Run();
        Assert.Equal(["crew.sameFood"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(1, Carried(w, Foods.Seed));
        var empty = new Scenario("S.......").Do(new Enter(1)).Run().Do(new Eat(Foods.Seed)).Run();
        Assert.Equal(["items.notEnough"], empty.Refusals.Select(r => r.Reason));
    }
}
