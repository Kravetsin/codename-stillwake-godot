using Stillwake.Sim.Crew;
using Xunit;
using LavaSpec = Stillwake.Content.Lava;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The drone's meeting spares its hero (drone-meeting): from its <see cref="PairDrone"/> on, for <c>Crew.MeetingTicks</c>, nothing
/// drains or harms it, its air, last breath, meals, the body's cold and heat, blows and lava alike; after them all of it does again.
/// </summary>
public class MeetingSparesTests
{
    /// <summary>A sealed room, airless, the alien core on its floor at the left; the lava's pocket beside it, airless too.</summary>
    private const string Vacuum = """
        #######
        #.....#
        #.....#
        #?....#
        #######
        """;

    private const string LavaMap = """
        S......
        .......
        .......
        ?......
        #######
        #######
        """;

    private const string LavaGround = """
        .......
        .......
        .......
        .......
        tllllll
        bbbbbbb
        """;

    private const int Window = Content.Crew.MeetingTicks;

    private static Hero HeroOf(Scenario w) => w.M.Crew.HeroOf(0)!;

    private static int Core(Scenario w) => w.M.Grid.MachinesOf(Content.LightDrone.CoreMachine)[0].Id;

    private static Scenario InVacuum(bool bare)
    {
        var w = new Scenario(Layout.Parse(Vacuum), doubled: true) { Unpaired = true, Bare = bare };
        w.Do(new Enter(w.At(4, 2)) { From = 0 }).Run();
        return w;
    }

    [Fact]
    public void InVacuumWithNoAirThePairedHeroSpendsNoneNeverPassesOutAndGaspsOnAfter()
    {
        var w = InVacuum(bare: true);
        while (HeroOf(w).Air > 0) w.Run();
        w.Run(Content.Crew.BreathEvery);
        int gasp = HeroOf(w).Gasp;
        Assert.InRange(gasp, 1, Content.Crew.LastBreath - 1);
        w.Do(new PairDrone(Core(w)) { From = 0 }).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Crew.Spared(0));
        // The whole window: far longer than the last breath it had left, and it is still on it.
        Assert.True(Window > gasp);
        w.Run(Window - 1);
        Assert.Equal((0, gasp, 0), (HeroOf(w).Air, HeroOf(w).Gasp, HeroOf(w).Out));
        w.Run();
        Assert.False(w.M.Crew.Spared(0));
        Assert.Equal(gasp - 1, HeroOf(w).Gasp);
        w.Run(gasp);
        Assert.True(HeroOf(w).Out > 0);
    }

    [Fact]
    public void ASuitedHeroSpendsNoAirAndItsMealsWaitThroughTheWindowThenBreathesAndEatsOn()
    {
        var w = InVacuum(bare: false).Fed();
        w.Do(new PairDrone(Core(w)) { From = 0 }).Run();
        int air = HeroOf(w).Air, meal = HeroOf(w).Meals[0].Left;
        w.Run(Window - 1);
        Assert.Equal((air, meal), (HeroOf(w).Air, HeroOf(w).Meals[0].Left));
        w.Run(2 * Content.Crew.BreathEvery);
        Assert.True(HeroOf(w).Air < air);
        Assert.True(HeroOf(w).Meals[0].Left < meal);
    }

    [Fact]
    public void BlowsShocksAndTheBodysDeathTakeNothingInTheWindowAndHurtAfter()
    {
        var w = InVacuum(bare: false);
        w.Do(new PairDrone(Core(w)) { From = 0 }).Run();
        int air = HeroOf(w).Air;
        w.M.Crew.Blow(0, 30, Harm.Blow);
        w.M.Crew.Shock(0, 300, Harm.Shock);
        w.Do(new Succumb { From = 0 }).Run(Window - 1);
        Assert.Equal((air, 0, 0, Harm.None), (HeroOf(w).Air, HeroOf(w).Out, HeroOf(w).Hurts, HeroOf(w).Hurt));
        Assert.Equal(["crew.noHero"], w.Refusals.Select(r => r.Reason));
        w.Run();
        w.M.Crew.Blow(0, 3, Harm.Blow);
        Assert.Equal((1, Harm.Blow), (HeroOf(w).Hurts, HeroOf(w).Hurt));
        w.Do(new Succumb { From = 0 }).Run();
        Assert.True(HeroOf(w).Out > 0);
    }

    [Fact]
    public void OnLavaTheWindowBurnsNothingAndTheLavaBurnsAgainAfter()
    {
        var w = new Scenario(Layout.Parse(LavaMap).WithMaterials(LavaGround), doubled: true) { Unpaired = true };
        int onLava = w.At(4, 2);
        w.Do(new Enter(onLava) { From = 0 }).Run();
        w.Do(new PairDrone(Core(w)) { From = 0 }).Run();
        Assert.True(w.M.Crew.Burns(onLava));
        int air = HeroOf(w).Air, hurts = HeroOf(w).Hurts;
        w.Run(Window - 1);
        Assert.Equal((air, hurts, 0), (HeroOf(w).Air, HeroOf(w).Hurts, HeroOf(w).Out));
        w.Run(Content.Crew.BreathEvery + 1);
        Assert.Equal(hurts + 1, HeroOf(w).Hurts);
        Assert.True(HeroOf(w).Air <= air - LavaSpec.Burn);
    }

    [Fact]
    public void TheWindowIsSavedAndLoadedWithTheHero()
    {
        var w = InVacuum(bare: false);
        w.Do(new PairDrone(Core(w)) { From = 0 }).Run(10);
        w.Reload();
        Assert.True(w.M.Crew.Spared(0));
        Assert.Equal(Window - 10, HeroOf(w).Meets);
    }
}
