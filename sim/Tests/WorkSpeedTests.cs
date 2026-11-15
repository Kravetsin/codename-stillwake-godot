using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Xunit;
using Diet = Stillwake.Content.Diet;
using Foods = Stillwake.Content.Foods;
using Hero = Stillwake.Sim.Crew.Hero;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Work speed (food-and-the-body, rule 3): a <c>Work</c> is worth the hero's work rate in thousandths of a tick, a fed one's
/// now and then two ticks, a hungry one's every fourth nothing; a second tick never finishes a plan nor begins an unpaid part.
/// </summary>
public class WorkSpeedTests
{
    private static readonly Placement Wall = Placement.Wall("iron");
    private static readonly Placement Tank = new("machine", "tank");

    /// <summary>Nine characters wide, doubled to 18 cells (0104).</summary>
    private const string Open = """
        #########
        #.......#
        #.......#
        #.......#
        #########
        """;
    private static int At(int x, int y) => 2 * y * 18 + 2 * x;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static Hero HeroOf(Scenario w) => w.M.Crew.HeroOf(0)!;
    private static Plan PlanOf(Scenario w, int id) => w.M.Building.Plans.Single(p => p.Id == id);

    public enum Fed { Hungry, Paste, Ration }

    /// <summary>Hero 0 at (1, 1), carrying <paramref name="items"/>: hungry, on the pod's paste, or on a ration too.</summary>
    private static Scenario Fresh(Fed fed, params (string Item, int Count)[] items)
    {
        var w = new Scenario(Open).GiveItems(0, [.. items, (Foods.Ration, 1)]);
        w.Do(new Enter(At(1, 1))).Run();
        if (fed == Fed.Ration) w.Do(new Eat(Foods.Ration)).Run();
        if (fed == Fed.Hungry) HeroOf(w).Eaten.Clear();
        Assert.Empty(w.Refusals);
        return w;
    }

    /// <summary>How many <c>Work</c>s plan 1 takes to finish.</summary>
    private static int WorksToFinish(Scenario w)
    {
        int works = 0;
        while (w.M.Building.Plans.Count > 0)
        {
            w.Do(new Work(1)).Run();
            works++;
            Assert.Empty(w.Refusals);
        }
        return works;
    }

    [Theory]
    // An iron wall is 14 ticks of one part. Hungry, three ticks every four Works; on paste one a Work; on a ration 1.3.
    [InlineData(Fed.Hungry, 19)]
    [InlineData(Fed.Paste, 14)]
    [InlineData(Fed.Ration, 11)]
    public void AWallTakesTheWorksItsHerosRateMakesOfItsTicks(Fed fed, int works)
    {
        var w = Fresh(fed, ("plate", 1)).Do(new PlacePlan(At(5, 2), Wall)).Run();
        Assert.Equal(works, WorksToFinish(w));
        Assert.Equal("iron", w.M.Grid.WallAt(At(5, 2))?.Id);
    }

    [Theory]
    // Its 160 build ticks are 80 to take apart.
    [InlineData(Fed.Hungry, 107)]
    [InlineData(Fed.Paste, 80)]
    [InlineData(Fed.Ration, 62)]
    public void TakingATankApartScalesAlike(Fed fed, int works)
    {
        var w = Fresh(fed).Do(new Place(At(3, 2), Tank)).Run().Do(new Deconstruct(At(3, 2), "machine")).Run();
        Assert.Equal(works, WorksToFinish(w));
        Assert.Null(w.M.Grid.MachineAt(At(3, 2)));
    }

    [Fact]
    public void AFedHerosSecondTickStopsAtAPartNotPaidFor()
    {
        // Only one plate of the tank's twelve parts: the work stops where the second part begins, refused for want of it.
        var w = Fresh(Fed.Ration, ("plate", 1)).Do(new PlacePlan(At(3, 2), Tank)).Run();
        int build = Costs.BuildOf(Tank).BuildTicks;
        int parts = Costs.PartsOf(Costs.BuildOf(Tank).Cost);
        // A tick at a time up to two short of the second part, then a Work worth two: its second would begin that part.
        while (Costs.PartsBegun(PlanOf(w, 1).Progress + 1, build, parts) < 2)
        {
            HeroOf(w).Worked = 0;
            w.Do(new Work(1)).Run();
        }
        HeroOf(w).Worked = 999;
        w.Do(new Work(1)).Run().Do(new Work(1)).Run();
        Assert.Equal(["items.notEnough"], Reasons(w));
        var plan = PlanOf(w, 1);
        Assert.Equal(1, plan.Delivered.Sum(s => s.Count));
        // The next tick would begin the second part: no tick of it is done.
        Assert.Equal(2, Costs.PartsBegun(plan.Progress, build, parts));
        Assert.Equal(1, Costs.PartsBegun(plan.Progress - 1, build, parts));
    }

    [Fact]
    public void AFedHerosSecondTickNeverFinishesAWallItStandsIn()
    {
        var w = Fresh(Fed.Ration, ("plate", 1)).Do(new PlacePlan(At(1, 1), Wall)).Run();
        int ticks = Costs.BuildOf(Wall).BuildTicks;
        // Worked up to two ticks short, then a Work worth two: the first is not the last, the second would be.
        while (PlanOf(w, 1).Progress < ticks - 2)
        {
            HeroOf(w).Worked = 0;
            w.Do(new Work(1)).Run();
        }
        Assert.Equal(ticks - 2, PlanOf(w, 1).Progress);
        HeroOf(w).Worked = 999;
        w.Do(new Work(1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(ticks - 1, PlanOf(w, 1).Progress);
        Assert.Null(w.M.Grid.WallAt(At(1, 1)));
        w.Do(new Work(1)).Run();
        Assert.Equal(["building.heroInside"], Reasons(w));
    }

    [Fact]
    public void AWorkFromAPeerWithNoHeroIsWorthOneTick()
    {
        var w = new Scenario(Open).Do(new Items.SetCreative(true)).Do(new PlacePlan(At(5, 2), Wall)).Run();
        Assert.Null(w.M.Crew.HeroOf(0));
        Assert.Equal(Costs.BuildOf(Wall).BuildTicks, WorksToFinish(w));
    }

    [Fact]
    public void ACreativeHerosWorkIsWorthOneTickItsWorkedUntouched()
    {
        var w = Fresh(Fed.Hungry).Do(new Items.SetCreative(true)).Do(new PlacePlan(At(5, 2), Wall)).Run();
        Assert.Equal(Costs.BuildOf(Wall).BuildTicks, WorksToFinish(w));
        Assert.Equal(0, HeroOf(w).Worked);
    }

    [Fact]
    public void WorkedIsSavedAndPassingOutClearsIt()
    {
        var w = Fresh(Fed.Ration, ("plate", 1)).Do(new PlacePlan(At(5, 2), Wall)).Run().Do(new Work(1)).Run();
        Assert.Equal(Diet.Work + Foods.Of(Foods.Ration)!.Rates.Work - 1000, HeroOf(w).Worked);
        w.Reload();
        Assert.Equal(300, HeroOf(w).Worked);
        HeroOf(w).Air = 0;
        while (HeroOf(w).Out == 0) w.Run();
        Assert.Equal(0, HeroOf(w).Worked);
    }
}
