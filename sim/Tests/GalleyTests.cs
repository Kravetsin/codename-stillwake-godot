using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;
using Craft = Stillwake.Sim.Crafting.Craft;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The galley (food-and-the-body, rule 9): an unpowered queued station cooking the five dishes, each paid by hand from its
/// recipe and eaten as a meal stronger than anything raw in it; the board understands a dish with one of each ingredient
/// and a galley standing.
/// </summary>
public class GalleyTests
{
    private static readonly string[] Dishes = [Foods.Porridge, Foods.Hash, Foods.Broth, Foods.Roast, Foods.Stew];

    /// <summary>A bare room with a galley and a workbench, no power anywhere; knowing everything, or the wreck's start alone.</summary>
    private static Scenario Kitchen(bool wreck = false)
    {
        var layout = Layout.Parse(["##########", "#&&..II..#", "#&&..II..#", "#&&..II..#", "##########"], null);
        return new Scenario(wreck ? layout with { Known = Content.Research.StartKnown } : layout, doubled: true);
    }

    /// <summary>One of each ingredient of every dish, many times over.</summary>
    private static Scenario Stocked(Scenario w) => w.GiveItems(0,
        (Foods.Fruit, 20), (Foods.Ration, 10), (Foods.CannedMeat, 10), (Foods.Glowberry, 20), (Content.Water.Item, 10), ("fat", 20));

    private static int Galley(Scenario w) => w.M.Grid.MachinesOf("galley").Single().Id;
    private static int Bench(Scenario w) => w.M.Grid.MachinesOf("workbench").Single().Id;
    private static int Carried(Scenario w, string item) => w.M.Items.Inventory(0).Sum(s => s?.Item == item ? s.Count : 0);
    private static int Made(Scenario w, string item) => w.M.Items.Crate(Galley(w)).Sum(s => s?.Item == item ? s.Count : 0);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    public static TheoryData<string> EachDish() => [.. Dishes];

    /// <summary>Paid from the bag as it is queued, made in its ticks at full speed with no power, into the galley's slots.</summary>
    [Theory]
    [MemberData(nameof(EachDish))]
    public void TheGalleyCooksEachDishFromItsRecipeWithNoPower(string dish)
    {
        var w = Stocked(Kitchen()).Run();
        var before = Content.Crafts.Get(dish).Cost.ToDictionary(c => c.Item, c => Carried(w, c.Item));
        w.Do(new Craft(Galley(w), dish, 2)).Run();
        Assert.Empty(w.Refusals);
        Assert.All(Content.Crafts.Get(dish).Cost, c => Assert.Equal(before[c.Item] - 2 * c.Count, Carried(w, c.Item)));
        int ticks = Content.Crafts.Get(dish).Ticks * 1000 / Stations.Get("galley").Speed;
        w.Run(ticks - 2);
        Assert.Equal(0, Made(w, dish));
        w.Run(ticks + 1);
        Assert.Equal(2, Made(w, dish));
    }

    [Fact]
    public void TheDishesAreTheTablesEachLastingAsARationStrongerThanAnythingRawInItAndNeverSold()
    {
        Assert.Equal(
            [new(Calm: 50, Work: 400), new(Deep: 300, Work: 200), new(Calm: 300), new(Deep: 500), new Rates(Calm: 150, Deep: 300)],
            Dishes.Select(d => Foods.Of(d)!.Rates));
        Assert.All(Dishes, d => Assert.Equal(18_000, Foods.Of(d)!.Lasts));
        Assert.All(Dishes, d => Assert.Null(Market.PriceOf(d)));
        Assert.All(Dishes, d => Assert.Null(Content.Items.Get(d).Byproducts));
        static int Sum(Rates r) => r.Calm + r.Deep + r.Work;
        foreach (var dish in Dishes)
        {
            var mine = Foods.Of(dish)!.Rates;
            foreach (var raw in Content.Crafts.Get(dish).Cost.Select(c => Foods.Of(c.Item)).OfType<Food>())
            {
                Assert.True(mine.Calm >= raw.Rates.Calm && mine.Deep >= raw.Rates.Deep && mine.Work >= raw.Rates.Work, $"{dish} below {raw.Id}");
                Assert.True(Sum(mine) > Sum(raw.Rates), $"{dish} no stronger than {raw.Id}");
            }
        }
        Assert.Equal(["galley"], Stations.All.Where(s => s.Rows.Any(r => r.Any(Dishes.Contains))).Select(s => s.Machine));
    }

    [Fact]
    public void ADishIsEatenAsAMealGivingItsOwnRates()
    {
        var w = new Scenario("S.......").GiveItems(0, (Foods.Porridge, 1), (Foods.Stew, 1)).Do(new Enter(1)).Run();
        w.Do(new Eat(Foods.Porridge)).Do(new Eat(Foods.Stew)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal([Foods.Paste, Foods.Porridge, Foods.Stew], w.M.Crew.HeroOf(0)!.Meals.Select(m => m.Food));
        Assert.Equal(new Rates(Calm: 200, Deep: 300, Work: 400), w.M.Crew.RatesOf(0));
    }

    /// <summary>A fruit cooked gives no seed back, cooking or eaten (fruit-not-biomass): only a raw one draws.</summary>
    [Fact]
    public void ACookedFruitNeverGivesItsSeed()
    {
        var w = Stocked(Kitchen()).Run();
        w.Do(new Craft(Galley(w), Foods.Porridge, 10)).Run(10 * 200 + 5);
        Assert.Equal(10, Made(w, Foods.Porridge));
        Assert.Equal(0, Carried(w, "seed"));
        Assert.Equal(0, Made(w, "seed"));
        // Eaten, nothing is drawn: a raw fruit eaten on the same seed and tick gives its seed back.
        Scenario Eating(string food) => new Scenario("S.......", seed: 3).GiveItems(0, (food, 20)).Do(new Enter(1)).Run();
        int Seeds(Scenario s) => s.M.Items.Inventory(0).Sum(x => x?.Item == "seed" ? x.Count : 0);
        var cooked = Eating(Foods.Porridge);
        var raw = Eating(Foods.Fruit);
        for (int i = 0; i < 20; i++)
            foreach (var (s, food) in new[] { (cooked, Foods.Porridge), (raw, Foods.Fruit) })
            {
                s.M.Crew.HeroOf(0)!.Eaten.Clear();
                s.Do(new Eat(food)).Run();
            }
        Assert.Empty(cooked.Refusals);
        Assert.Equal(0, Carried(cooked, Foods.Porridge));
        Assert.Equal(0, Seeds(cooked));
        Assert.True(Seeds(raw) > 0);
    }

    [Fact]
    public void RefusesWhatTheGalleyDoesNotCookADishAtTheBenchAndOneShortOfItsRecipe()
    {
        var w = Stocked(Kitchen()).GiveItems(0, ("scrap", 10)).Run();
        w.Do(new Craft(Galley(w), "wire", 1)).Do(new Craft(Bench(w), Foods.Porridge, 1)).Run();
        Assert.Equal(["crafting.notHere", "crafting.notHere"], Reasons(w));
        w.Refusals.Clear();
        w.Do(new Craft(Galley(w), Foods.Hash, 11)).Run();
        Assert.Equal(["items.notEnough"], Reasons(w));
        Assert.Equal(10, Carried(w, Foods.CannedMeat));
    }

    [Fact]
    public void RefusesAFifthDishQueuedAndOneFromInputsItHasNone()
    {
        var w = Stocked(Kitchen()).Run();
        int galley = Galley(w);
        for (int i = 0; i < 4; i++) w.Do(new Craft(galley, Foods.Roast, 1));
        w.Run();
        Assert.Empty(w.Refusals);
        w.Do(new Craft(galley, Foods.Roast, 1)).Run();
        Assert.Equal(["crafting.full"], Reasons(w));
        w.Refusals.Clear();
        var fresh = Stocked(Kitchen()).Run();
        fresh.Do(new Craft(Galley(fresh), Foods.Roast, 1, FromInputs: true)).Run();
        Assert.Equal(["crafting.noInputs"], Reasons(fresh));
    }

    /// <summary>Fruit, a ration and a galley standing: porridge is understood; roast, its fat never had, is not.</summary>
    [Fact]
    public void TheBoardUnderstandsPorridgeWithFruitARationAndAGalleyNotRoast()
    {
        var w = Kitchen(wreck: true).Run();
        w.M.Knowledge.Obtain(Foods.Fruit, 1);
        Assert.False(w.Run(5).M.Research.Knows(Foods.Porridge));
        w.M.Knowledge.Obtain(Foods.Ration, 1);
        Assert.True(w.Run().M.Research.Knows(Foods.Porridge));
        Assert.False(w.M.Research.Knows(Foods.Roast));
        Assert.False(w.M.Research.Knows(Foods.Hash));
    }

    [Fact]
    public void NoGalleyNoDishWhateverIsHad()
    {
        var w = new Scenario(Layout.Parse(["##########", "#........#", "##########"], null) with { Known = Content.Research.StartKnown }, doubled: true).Run();
        w.M.Knowledge.Obtain(Foods.Fruit, 1);
        w.M.Knowledge.Obtain(Foods.Ration, 1);
        Assert.False(w.Run(5).M.Research.Knows(Foods.Porridge));
    }
}
