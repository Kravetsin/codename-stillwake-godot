using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Fruit (fruit-not-biomass): a grow capsule's crops are fruit and glowberries, eaten raw or crushed into biomass, their
/// seed or spore back by chance either way, so a seed's fuel and the seeds it gives back stay what they were.
/// </summary>
public class FruitTests
{
    /// <summary>A solar panel out under the stars cabled to a crusher in a room below, as the crusher's tests.</summary>
    private static Scenario Crushing(uint seed = 1)
    {
        string[] rows = ["PPP       ", "          ", "##########", "#kk......#", "#kk......#", "##########"];
        return new Scenario(Layout.Parse(rows, null, [((0, 0), (1, 3))]), seed, doubled: true)
            .GiveItems(0, (Foods.Fruit, 100), (Foods.Glowberry, 100)).Run(2);
    }

    /// <summary>Hero 0 aboard in open hull, carrying <paramref name="items"/>.</summary>
    private static Scenario Eating(uint seed, params (string Item, int Count)[] items) =>
        new Scenario("S.......", seed).GiveItems(0, items).Do(new Enter(1)).Run();

    private static int Crusher(Scenario w) => w.M.Grid.MachinesOf("crusher").Single().Id;
    private static int Held(Scenario w, int machine, string item) => w.M.Items.Crate(machine).Sum(s => s?.Item == item ? s.Count : 0);
    private static int Carried(Scenario w, string item) => w.M.Items.Inventory(0).Sum(s => s?.Item == item ? s.Count : 0);
    private static int Work(string item) => Content.Items.Get(item).SmeltTicks!.Value * 1000;
    private static readonly int Speed = Stations.Get("crusher").Speed;

    private static Scenario Put(Scenario w, int machine, string item, int count)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
        return w.Do(new Move(new Slot(from), new Target(0, machine), count)).Run();
    }

    [Fact]
    public void FruitAndGlowberriesAreRawFoodsOfTheirRowSoldAsTheirBiomassAndTheOnlyFoodsGivingAnythingBack()
    {
        Assert.Equal(new Food(Foods.Fruit, 12_000, new(Work: 100)), Foods.Of(Foods.Fruit));
        Assert.Equal(new Food(Foods.Glowberry, 12_000, new(Calm: 120)), Foods.Of(Foods.Glowberry));
        Assert.Equal(20, Market.PriceOf(Foods.Fruit));
        Assert.Equal(20, Market.PriceOf(Foods.Glowberry));
        Assert.Equal([Foods.Fruit, Foods.Glowberry], Foods.All.Where(f => Content.Items.TryGet(f.Id, out var i) && i!.Byproducts != null).Select(f => f.Id));
    }

    /// <summary>A fruit crushes in 40 ticks into a biomass and, one time in five, its seed: a hundred on world seed 1 give a seed about twenty times.</summary>
    [Fact]
    public void TheCrusherCrushesAFruitIntoABiomassAndItsSeedOneTimeInFive()
    {
        var w = Crushing();
        int crusher = Crusher(w);
        Assert.Equal((40, "crush"), (Content.Items.Get(Foods.Fruit).SmeltTicks, Content.Items.Get(Foods.Fruit).Process));
        Assert.Equal([new LootEntry("seed", 1, 1, OneIn: 5)], Content.Items.Get(Foods.Fruit).Byproducts);
        Put(w, crusher, Foods.Fruit, 1).Run(Work(Foods.Fruit) / Speed - 2);
        Assert.Equal(0, Held(w, crusher, "biomass"));
        w.Run();
        Assert.Equal((0, 1), (Held(w, crusher, Foods.Fruit), Held(w, crusher, "biomass")));
        Put(w, crusher, Foods.Fruit, 99).Run(99 * Work(Foods.Fruit) / Speed);
        Assert.Equal((0, 100), (Held(w, crusher, Foods.Fruit), Held(w, crusher, "biomass")));
        Assert.InRange(Held(w, crusher, "seed"), 10, 30);
    }

    /// <summary>A glowberry rolls twice, a spore one time in 3 and another one time in 30: a hundred give some 37 spores.</summary>
    [Fact]
    public void TheCrusherCrushesAGlowberryIntoABiomassAndASporeOneTimeInThreeAndAnotherOneInThirty()
    {
        var w = Crushing();
        int crusher = Crusher(w);
        Assert.Equal([new LootEntry("spore", 1, 1, OneIn: 3), new LootEntry("spore", 1, 1, OneIn: 30)], Content.Items.Get(Foods.Glowberry).Byproducts);
        Put(w, crusher, Foods.Glowberry, 100).Run(100 * Work(Foods.Glowberry) / Speed);
        Assert.Equal((0, 100), (Held(w, crusher, Foods.Glowberry), Held(w, crusher, "biomass")));
        Assert.InRange(Held(w, crusher, "spore"), 22, 52);
    }

    /// <summary>The crusher waits for room for a glowberry's most, both spores, whatever the draw will be.</summary>
    [Fact]
    public void TheCrusherWaitsForRoomForBothOfAGlowberrysSpores()
    {
        var w = Crushing();
        int crusher = Crusher(w);
        int spores = Content.Items.Get("spore").Stack, canisters = Content.Items.Get("canister").Stack;
        Stacks.SetCrate((ItemsState)w.M.Items.Slice, crusher, [new Stack(Foods.Glowberry, 1), new Stack("biomass", 1), new Stack("spore", spores - 1), new Stack("canister", canisters)]);
        w.Run(3);
        Assert.False(w.M.Crafting.CanSmelt(crusher));
        Assert.Equal(0, w.M.Crafting.Progress(crusher));
        w.Do(new Move(new Slot(2, crusher), new Target(), 1)).Run(2);
        Assert.True(w.M.Crafting.CanSmelt(crusher));
        Assert.True(w.M.Crafting.Works(crusher));
    }

    /// <summary>Eating a fruit raw draws its seed as crushing it would, one time in five, into the bag; a glowberry its spores.</summary>
    [Fact]
    public void EatingAFruitCanGiveItsSeedAndAGlowberryItsSpores()
    {
        var w = Eating(1, (Foods.Fruit, 100), (Foods.Glowberry, 100));
        for (int i = 0; i < 100; i++)
        {
            w.M.Crew.HeroOf(0)!.Eaten.Clear();
            w.Do(new Eat(Foods.Fruit)).Do(new Eat(Foods.Glowberry)).Run();
        }
        Assert.Empty(w.Refusals);
        Assert.Equal((0, 0), (Carried(w, Foods.Fruit), Carried(w, Foods.Glowberry)));
        Assert.InRange(Carried(w, "seed"), 10, 30);
        Assert.InRange(Carried(w, "spore"), 22, 52);
    }

    /// <summary>Only the apply draws: asking whether a fruit may be eaten, as the client does every frame, moves no world.</summary>
    [Fact]
    public void AskingWhetherAFruitMayBeEatenDrawsNothing()
    {
        var (a, b) = (Eating(7, (Foods.Fruit, 20)), Eating(7, (Foods.Fruit, 20)));
        for (int i = 0; i < 20; i++)
        {
            foreach (var w in (Scenario[])[a, b])
            {
                w.M.Crew.HeroOf(0)!.Eaten.Clear();
                for (int k = 0; k < 10 && w == a; k++) Assert.Null(a.M.Crew.EatProblem(0, Foods.Fruit));
                w.Do(new Eat(Foods.Fruit)).Run();
            }
        }
        Assert.Equal(Carried(a, "seed"), Carried(b, "seed"));
        Assert.Equal(a.World.Hash(), b.World.Hash());
    }

    /// <summary>A seed eaten back from a fruit with the bag full falls at the hero's feet, never lost.</summary>
    [Fact]
    public void ASeedBackWithTheBagFullFallsAtTheHerosFeet()
    {
        var w = Eating(1, (Foods.Fruit, 50));
        var state = (ItemsState)w.M.Items.Slice;
        var scrap = new Stack("scrap", Content.Items.Get("scrap").Stack);
        Stacks.SetInventory(state, 0, [.. Stacks.InventoryOf(state, 0).Select((s, i) => s ?? (i < Content.Inventory.Slots ? scrap : null))]);
        for (int i = 0; i < 40; i++)
        {
            w.M.Crew.HeroOf(0)!.Eaten.Clear();
            w.Do(new Eat(Foods.Fruit)).Run();
        }
        Assert.Empty(w.Refusals);
        Assert.Equal(0, Carried(w, "seed"));
        Assert.InRange(w.M.Items.Floor.Where(s => s.Item == "seed").Sum(s => s.Count), 1, 40);
    }

    /// <summary>
    /// A seed's crop is still worth 4 biomass and 0.8 seeds back on average, a spore's 3 biomass and 1.1 spores: what a crop
    /// gave before its yields were fruit (0060), now through its fruit's rolls, exactly.
    /// </summary>
    [Theory]
    [InlineData("seed", 4, 4, 5)]
    [InlineData("spore", 3, 11, 10)]
    public void ACropsFuelAndWhatItGivesBackAverageWhatTheyWere(string crop, int biomass, int backNumerator, int backDenominator)
    {
        var (fuel, (num, den)) = Expected(crop);
        Assert.Equal(biomass, fuel);
        Assert.Equal(backNumerator * den, num * backDenominator);
    }

    /// <summary>One crop of <paramref name="crop"/>: the biomass its fruit crush into, and the mean of itself their rolls give back, as a fraction.</summary>
    private static (int Biomass, (long Num, long Den) Back) Expected(string crop)
    {
        int biomass = 0;
        (long num, long den) = (0, 1);
        foreach (var (fruit, count) in Content.Items.Get(crop).Yields!)
        {
            var spec = Content.Items.Get(fruit);
            biomass += count * spec.Yields!.Single(y => y.Item == "biomass").Count;
            foreach (var by in spec.Byproducts!.Where(b => b.Item == crop))
            {
                long d = 2L * (by.OneIn ?? 1);
                (num, den) = (num * d + count * (by.Min + by.Max) * den, den * d);
            }
        }
        return (biomass, (num, den));
    }

    /// <summary>
    /// Over many grown seeds and spores, each fruit crushed by the world's Rng as the crusher and eating draw, fuel and seeds
    /// back average today's: 4 biomass and 0.8 seeds a seed, 3 biomass and 1.1 spores a spore, within five standard errors.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property OverManyGrownSeedsAndSporesFuelAndSeedsBackAverageTodays() =>
        Prop.ForAll(Gen.Choose(1, int.MaxValue).ToArbitrary(), s =>
        {
            var rng = new Rng(new RngState { S = (uint)s });
            const int crops = 3000;
            foreach (var (crop, biomass, least, most) in new[] { ("seed", 4, 720, 880), ("spore", 3, 1020, 1180) })
            {
                int fuel = 0, back = 0;
                for (int i = 0; i < crops; i++)
                    foreach (var (fruit, count) in Content.Items.Get(crop).Yields!)
                        for (int k = 0; k < count; k++)
                        {
                            var spec = Content.Items.Get(fruit);
                            fuel += spec.Yields!.Single(y => y.Item == "biomass").Count;
                            back += Loots.Roll(spec.Byproducts!, rng).Where(b => b.Item == crop).Sum(b => b.Count);
                        }
                if (fuel != biomass * crops || back * 1000L < least * (long)crops || back * 1000L > most * (long)crops) return false;
            }
            return true;
        });
}
