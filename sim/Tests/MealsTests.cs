using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;
using CrewSpec = Stillwake.Content.Crew;
using Diet = Stillwake.Content.Diet;
using Foods = Stillwake.Content.Foods;

namespace Stillwake.Sim.Tests;

/// <summary>Meals, hunger and the breath's rates (food-and-the-body): what a hero eats changes what its body costs.</summary>
public class MealsTests
{
    /// <summary>Open hull with no room: no air, every breath from the suit.</summary>
    private const string Vacuum = "S.......";
    private const int Breath = CrewSpec.BreathEvery;

    private static Hero HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id)!;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Lasts(string food) => Foods.Of(food)!.Lasts;

    /// <summary>Hero 0 aboard out of any air, carrying <paramref name="items"/>, two ticks before a breath: what it eats next counts from the next.</summary>
    private static Scenario In(params (string Item, int Count)[] items)
    {
        var w = new Scenario(Vacuum);
        if (items.Length > 0) w.GiveItems(0, items);
        w.Do(new Enter(1)).Run(Breath - 2);
        return w;
    }

    private static Scenario Eat(Scenario w, string food, int peer = 0) => w.Do(new Eat(food) { From = peer }).Run();

    [Fact]
    public void APrintedHeroHoldsPodPasteAloneAndItGivesNoRates()
    {
        var w = new Scenario(Vacuum).Do(new Enter(1)).Run();
        Assert.Equal([(Foods.Paste, Lasts(Foods.Paste) - 1)], HeroOf(w).Meals.Select(m => (m.Food, m.Left)));
        Assert.Equal(default, w.M.Crew.RatesOf(0));
        Assert.False(w.M.Crew.Hungry(0));
    }

    [Fact]
    public void ARationsCalmPaysNineteenBreathsInTwentyItsSharesCarried()
    {
        var w = Eat(In((Foods.Ration, 1)), Foods.Ration);
        int air = HeroOf(w).Air;
        w.Run(Breath);
        // 950 thousandths owed after one breath: nothing whole paid yet.
        Assert.Equal((air, 950), (HeroOf(w).Air, HeroOf(w).Owed));
        w.Run(19 * Breath);
        Assert.Equal((air - 19, 0), (HeroOf(w).Air, HeroOf(w).Owed));
    }

    [Fact]
    public void RunningCostsTwoABreathFedAndFiveOverTwoHungry()
    {
        var w = In().Do(new Exert(true)).Run();
        int air = HeroOf(w).Air;
        w.Run(2 * Breath);
        Assert.Equal(air - 2 * CrewSpec.Exerted, HeroOf(w).Air);
        HeroOf(w).Eaten.Clear();
        Assert.True(w.M.Crew.Hungry(0));
        air = HeroOf(w).Air;
        w.Run(2 * Breath);
        Assert.Equal(air - 5, HeroOf(w).Air);
    }

    [Fact]
    public void AHungryHeroAtRestBreathesAsEver()
    {
        var w = In();
        HeroOf(w).Eaten.Clear();
        int air = HeroOf(w).Air;
        w.Run(10 * Breath);
        Assert.Equal(air - 10, HeroOf(w).Air);
    }

    [Fact]
    public void ThePasteRunsOutAfterItsTicksAndTheHeroGoesHungry()
    {
        var w = In();
        for (int left = HeroOf(w).Meals[0].Left; left > 1; left -= 1000)
        {
            HeroOf(w).Air = Scenario.Full;
            w.Run(Math.Min(1000, left - 1));
        }
        Assert.Equal(1, HeroOf(w).Meals[0].Left);
        Assert.False(w.M.Crew.Hungry(0));
        w.Run();
        Assert.Empty(HeroOf(w).Meals);
        Assert.True(w.M.Crew.Hungry(0));
    }

    [Fact]
    public void CannedMeatsDeepBreathGivesACanisterTwentySix()
    {
        var w = Eat(In((Foods.CannedMeat, 1), (CrewSpec.Canister, 1)), Foods.CannedMeat);
        Assert.Equal(CrewSpec.CanisterRefill * 13 / 10, w.M.Crew.RefillOf(0));
        // Past the breath, so the canister alone counts.
        w.Run();
        HeroOf(w).Air = 1;
        w.Do(new Refill()).Run();
        Assert.Equal(1 + 26, HeroOf(w).Air);
    }

    [Fact]
    public void ARationAddsAMealAndItsRatesAndIsTakenFromTheBag()
    {
        var w = Eat(In((Foods.Ration, 2)), Foods.Ration);
        Assert.Empty(w.Refusals);
        Assert.Equal([Foods.Paste, Foods.Ration], HeroOf(w).Meals.Select(m => m.Food));
        Assert.Equal(new Content.Rates(Calm: 50, Work: 300), w.M.Crew.RatesOf(0));
        Assert.Equal(1, w.M.Items.Inventory(0).Sum(s => s?.Item == Foods.Ration ? s.Count : 0));
    }

    [Fact]
    public void RefusesEatingWithNoHeroANonFoodPasteNoneCarriedTheSameFoodAndFullSlots()
    {
        var w = new Scenario(Vacuum).GiveItems(0, (Foods.Ration, 3), (CrewSpec.Canister, 1));
        Eat(w, Foods.Ration);
        w.Do(new Enter(1)).Run();
        Eat(w, CrewSpec.Canister);
        Eat(w, Foods.Paste);
        Eat(w, Foods.CannedMeat);
        Eat(w, Foods.Ration);
        Eat(w, Foods.Ration);
        // Three other meals, as dishes will fill them.
        HeroOf(w).Eaten.RemoveAt(1);
        HeroOf(w).Eaten.AddRange([new Meal { Food = "dish", Left = 100 }, new Meal { Food = "stew", Left = 100 }]);
        Eat(w, Foods.Ration);
        Assert.Equal(["crew.noHero", "crew.notFood", "crew.notFood", "items.notEnough", "crew.sameFood", "crew.mealsFull"], Reasons(w));
    }

    [Fact]
    public void ARefusedMealTakesNothing()
    {
        var w = Eat(In((Foods.Ration, 2)), Foods.Ration);
        Eat(w, Foods.Ration);
        Assert.Equal(["crew.sameFood"], Reasons(w));
        Assert.Equal(1, w.M.Items.Inventory(0).Sum(s => s?.Item == Foods.Ration ? s.Count : 0));
    }

    [Fact]
    public void TheSameFoodInItsLastMinuteIsEatenAgainInItsOwnSlot()
    {
        var w = Eat(Eat(In((Foods.Ration, 2), (Foods.CannedMeat, 1)), Foods.Ration), Foods.CannedMeat);
        var ration = HeroOf(w).Meals[1];
        ration.Left = Diet.RefreshWithin + 1;
        Eat(w, Foods.Ration);
        Assert.Equal(["crew.sameFood"], Reasons(w));
        ration.Left = Diet.RefreshWithin;
        Eat(w, Foods.Ration);
        Assert.Single(w.Refusals);
        Assert.Equal([Foods.Paste, Foods.Ration, Foods.CannedMeat], HeroOf(w).Meals.Select(m => m.Food));
        Assert.Equal(Lasts(Foods.Ration) - 1, HeroOf(w).Meals[1].Left);
    }

    [Fact]
    public void ACreativeHeroEatsTakingNothingAndIsNeverHungry()
    {
        var w = new Scenario(Vacuum).Do(new SetCreative(true)).Do(new Enter(1)).Run();
        Eat(w, Foods.Ration);
        Assert.Empty(w.Refusals);
        Assert.Contains(HeroOf(w).Meals, m => m.Food == Foods.Ration);
        HeroOf(w).Eaten.Clear();
        Assert.False(w.M.Crew.Hungry(0));
    }

    [Fact]
    public void PassingOutClearsTheMealsAndWakingPrintsPasteAlone()
    {
        var w = Eat(In((Foods.Ration, 1)), Foods.Ration);
        w.Run(Breath / 2);
        Assert.NotEqual(0, HeroOf(w).Owed);
        HeroOf(w).Air = 0;
        w.Run(Breath + CrewSpec.LastBreath + 1);
        Assert.True(HeroOf(w).Out > 0);
        Assert.Empty(HeroOf(w).Meals);
        Assert.Equal(0, HeroOf(w).Owed);
        Assert.False(w.M.Crew.Hungry(0));
        w.Run(CrewSpec.PassOut);
        Assert.Equal([Foods.Paste], HeroOf(w).Meals.Select(m => m.Food));
    }

    [Fact]
    public void AMealCrossesToAnotherSurfaceWithItsTicksLeft()
    {
        var w = Eat(In((Foods.Ration, 1)), Foods.Ration);
        w.Run(100);
        var carried = w.M.Crew.Depart(0);
        var there = new Scenario(Vacuum).Run(7);
        there.M.Crew.Arrive(0, carried, null);
        Assert.Equal(Lasts(Foods.Ration) - 101, HeroOf(there).Meals[1].Left);
    }

    [Fact]
    public void AHeroHoldingMealsSavesAndLoadsAlike()
    {
        var w = Eat(In((Foods.Ration, 1)), Foods.Ration).Run(Breath / 2);
        var before = HeroOf(w).Meals.Select(m => (m.Food, m.Left)).ToArray();
        int owed = HeroOf(w).Owed;
        w.Reload();
        Assert.Equal(before, HeroOf(w).Meals.Select(m => (m.Food, m.Left)));
        Assert.Equal(owed, HeroOf(w).Owed);
    }

    /// <summary>A step: a hero eats one of these (<c>Eat</c>), or waits ticks, its last meal sometimes moved to its last minute or ticks.</summary>
    private sealed record Step(int Hero, string? Food, int Ticks, int? Late);

    private static readonly string[] Edible = [Foods.Ration, Foods.CannedMeat, Foods.Paste, CrewSpec.Canister];

    private static readonly Gen<Step> Steps =
        from hero in Gen.Choose(0, 1)
        from food in Gen.Frequency((2, Gen.Elements(Edible).Select(f => (string?)f)), (1, Gen.Constant<string?>(null)))
        from ticks in Gen.Frequency((3, Gen.Choose(1, 40)), (1, Gen.Choose(500, 4000)))
        from late in Gen.Frequency((4, Gen.Constant<int?>(null)), (1, Gen.Elements<int?>(1, 2, Diet.RefreshWithin)))
        select new Step(hero, food, ticks, late);

    [Property(MaxTest = 60)]
    public Property AHeroHoldsAtMostThreeMealsAllDifferentNoneSpentItsRatesCapped()
    {
        var steps = from n in Gen.Choose(0, 30) from s in Steps.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = new Scenario(Vacuum).GiveItems(0, (Foods.Ration, 20), (Foods.CannedMeat, 20)).GiveItems(1, (Foods.Ration, 20));
            w.Do(new Enter(1) { From = 0 }).Do(new Enter(1) { From = 1 }).Run();
            foreach (var s in all)
            {
                foreach (int peer in new[] { 0, 1 })
                    HeroOf(w, peer).Air = Scenario.Full;
                if (s.Late is int late && HeroOf(w, s.Hero).Meals.Count > 0) HeroOf(w, s.Hero).Meals[^1].Left = late;
                if (s.Food != null) w.Do(new Eat(s.Food) { From = s.Hero });
                w.Run(s.Ticks);
                foreach (var hero in w.M.Crew.Heroes)
                {
                    Assert.True(hero.Meals.Count <= Diet.Meals);
                    Assert.Equal(hero.Meals.Count, hero.Meals.Select(m => m.Food).Distinct().Count());
                    Assert.All(hero.Meals, m => Assert.True(m.Left > 0));
                    var rates = w.M.Crew.RatesOf(hero.Id);
                    Assert.True(rates.Calm <= Diet.Caps.Calm && rates.Deep <= Diet.Caps.Deep && rates.Work <= Diet.Caps.Work);
                    Assert.InRange(hero.Owed, 0, 999);
                }
            }
        });
    }
}
