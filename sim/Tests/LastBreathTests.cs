using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;
using Hero = Stillwake.Sim.Crew.Hero;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The last breath (2026-11-05-the-last-breath): a hero whose air runs dry lives <c>Crew.LastBreath</c> ticks more, spared
/// by any air back in time; lava, shocks and blows past its air cut it, <c>Crew.LastBreathPerUnit</c> ticks a unit.
/// </summary>
public class LastBreathTests
{
    /// <summary>A pod in a vacuum room, never aired.</summary>
    private const string PodRoom = """
        ###########
        #.........#
        #..OOO...S#
        #..OOO....#
        #..OOO....#
        ###########
        """;

    private static int AtPod(int x, int y) => y * 22 + x;
    /// <summary>The last cell right of the pod its air reaches, and the first past it: vacuum, and no pod.</summary>
    private static readonly int Reached = AtPod(13, 6), Beyond = AtPod(14, 6);

    private static Hero HeroOf(Scenario w) => w.M.Crew.HeroOf(0)!;

    /// <summary>Runs whole breaths: from just after one beat to just after the <paramref name="count"/>-th next.</summary>
    private static Scenario Breaths(Scenario w, int count) =>
        w.Run(count * Content.Crew.BreathEvery - w.Crew().Clock % Content.Crew.BreathEvery);

    /// <summary>Hero 0 past the pod's reach, carrying scrap, breathed to just after the breath that empties its suit.</summary>
    private static Scenario Dry(bool bare = false)
    {
        var w = new Scenario(PodRoom) { Bare = bare }.GiveItems(0, ("scrap", 4));
        w.Do(new Enter(Beyond) { From = 0 }).Run();
        var hero = HeroOf(w);
        // Bare, the cold takes a unit a breath besides.
        int breaths = bare ? (hero.Air + 1) / (1 + Content.Crew.BareCold) : hero.Air;
        Breaths(w, breaths);
        Assert.Equal((0, Content.Crew.LastBreath, 0), (HeroOf(w).Air, HeroOf(w).Gasp, HeroOf(w).Out));
        return w;
    }

    [Fact]
    public void ADrySuitLivesOnItsLastBreathThenPassesOutWhereItIsItsBodyLeftThere()
    {
        var w = Dry();
        w.Run(Content.Crew.LastBreath - 1);
        Assert.Equal((1, 0), (HeroOf(w).Gasp, HeroOf(w).Out));
        Assert.Empty(w.M.Items.Floor);
        w.Run();
        Assert.Equal((0, Content.Crew.PassOut), (HeroOf(w).Gasp, HeroOf(w).Out));
        Assert.Equal(("scrap", 4), (Assert.Single(w.M.Items.Floor).Item, w.M.Items.Floor[0].Count));
    }

    [Fact]
    public void BareTheColdCutsNoLastBreath()
    {
        var w = Dry(bare: true);
        w.Run(Content.Crew.LastBreath - 1);
        Assert.Equal((1, 0), (HeroOf(w).Gasp, HeroOf(w).Out));
        w.Run();
        Assert.Equal(Content.Crew.PassOut, HeroOf(w).Out);
    }

    [Fact]
    public void ACanisterBreathedInTimeEndsIt()
    {
        var w = Dry().GiveItems(0, (Content.Crew.Canister, 1));
        w.Run(Content.Crew.LastBreath / 2);
        w.Do(new Refill { From = 0 }).Run();
        Assert.Equal((Content.Crew.CanisterRefill, 0), (HeroOf(w).Air, HeroOf(w).Gasp));
        w.Run(Content.Crew.LastBreath);
        Assert.Equal(0, HeroOf(w).Out);
    }

    [Fact]
    public void ThePodsAirReachedInTimeEndsItAndALaterDrySuitStartsItAnew()
    {
        var w = Dry();
        w.Run(Content.Crew.LastBreath / 2);
        w.Do(new Enter(Reached) { From = 0 }).Run();
        Breaths(w, 1);
        Assert.Equal(0, HeroOf(w).Gasp);
        Assert.True(HeroOf(w).Air > 0);
        w.Run(Content.Crew.LastBreath);
        Assert.Equal(0, HeroOf(w).Out);
        // Back out in vacuum, its few units go and a whole last breath starts again.
        w.Do(new Enter(Beyond) { From = 0 }).Run();
        Breaths(w, HeroOf(w).Air);
        Assert.Equal((0, Content.Crew.LastBreath), (HeroOf(w).Air, HeroOf(w).Gasp));
    }

    [Fact]
    public void AShockCutsItAUnitABreathAndOnePastWhatIsLeftPassesTheHeroOut()
    {
        var w = Dry();
        w.M.Crew.Shock(0, 3, Harm.Shock);
        Assert.Equal((Content.Crew.LastBreath - 3 * Content.Crew.LastBreathPerUnit, 0), (HeroOf(w).Gasp, HeroOf(w).Out));
        w.M.Crew.Shock(0, HeroOf(w).Gasp / Content.Crew.LastBreathPerUnit, Harm.Shock);
        Assert.Equal((0, Content.Crew.PassOut), (HeroOf(w).Gasp, HeroOf(w).Out));
    }

    [Fact]
    public void AShockEmptyingTheAirCutsTheLastBreathByWhatItTookPastIt()
    {
        var w = new Scenario(PodRoom).Do(new Enter(Beyond) { From = 0 }).Run();
        int air = HeroOf(w).Air;
        w.M.Crew.Shock(0, air + 2, Harm.Shock);
        Assert.Equal((0, Content.Crew.LastBreath - 2 * Content.Crew.LastBreathPerUnit), (HeroOf(w).Air, HeroOf(w).Gasp));
        // A blow far past the air passes it out at once.
        w = new Scenario(PodRoom).Do(new Enter(Beyond) { From = 0 }).Run();
        w.M.Crew.Shock(0, 10 * Scenario.Full, Harm.Shock);
        Assert.Equal(Content.Crew.PassOut, HeroOf(w).Out);
    }

    [Fact]
    public void CreativeModeEndsItAndALoadKeepsIt()
    {
        var w = Dry();
        w.Run(7);
        w.Reload();
        Assert.Equal(Content.Crew.LastBreath - 7, HeroOf(w).Gasp);
        w.Do(new SetCreative(true)).Run();
        Assert.Equal((Scenario.Full, 0), (HeroOf(w).Air, HeroOf(w).Gasp));
    }

    [Fact]
    public void TheRegenSuitMakingTheBreathKeepsAHeroWithNoAirAliveAndSwitchedOffItsLastBreathStarts()
    {
        var w = new Scenario(Layout.Parse(PodRoom) with { Known = [] }, doubled: true) { Bare = true }
            .Wear(0, Helmet.Heavy).Wear(0, Suit.Regen);
        w.Put(Inventory.Slots + Inventory.Equipment.ToList().IndexOf(Charge.Belt), new Stack("battery", 1, Charge.Battery));
        w.Do(new Enter(Beyond) { From = 0 }).Run();
        w.Do(new SuitMode(true) { From = 0 }).Run();
        HeroOf(w).Air = 0;
        Breaths(w, 1);
        Breaths(w, Content.Crew.LastBreath / Content.Crew.BreathEvery + 2);
        Assert.Equal((0, 0, 0), (HeroOf(w).Air, HeroOf(w).Gasp, HeroOf(w).Out));
        w.Do(new SuitMode(false) { From = 0 }).Run();
        Breaths(w, 1);
        Assert.Equal((Content.Crew.LastBreath, 0), (HeroOf(w).Gasp, HeroOf(w).Out));
    }
}
