using Stillwake.Sim.Crew;
using Xunit;
using LavaSpec = Stillwake.Content.Lava;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The drone's meeting spares its hero (drone-meeting), and so does a form's vision (form-vision): from its <see cref="PairDrone"/> on, for
/// <c>Crew.MeetingTicks</c>, or its <see cref="Envision"/>, for <c>Crew.VisionTicks</c>, nothing drains or harms it, its air, last breath,
/// meals, the body's cold and heat, blows and lava alike; after them all of it does again. Each rule is one test over both.
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

    private static Hero HeroOf(Scenario w) => w.M.Crew.HeroOf(0)!;

    private static int Core(Scenario w) => w.M.Grid.MachinesOf(Content.LightDrone.CoreMachine)[0].Id;

    /// <summary>What spares the hero: its drone's meeting, else a form's vision.</summary>
    private static Command Spare(Scenario w, bool vision) => vision ? new Envision { From = 0 } : new PairDrone(Core(w)) { From = 0 };

    /// <summary>How long it spares the hero, ticks.</summary>
    private static int Window(bool vision) => vision ? Content.Crew.VisionTicks : Content.Crew.MeetingTicks;

    private static Scenario InVacuum(bool bare)
    {
        var w = new Scenario(Layout.Parse(Vacuum), doubled: true) { Unpaired = true, Bare = bare };
        w.Do(new Enter(w.At(4, 2)) { From = 0 }).Run();
        return w;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InVacuumWithNoAirTheSparedHeroSpendsNoneNeverPassesOutAndGaspsOnAfter(bool vision)
    {
        var w = InVacuum(bare: true);
        while (HeroOf(w).Air > 0) w.Run();
        w.Run(Content.Crew.BreathEvery);
        int gasp = HeroOf(w).Gasp;
        Assert.InRange(gasp, 1, Content.Crew.LastBreath - 1);
        w.Do(Spare(w, vision)).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Crew.Spared(0));
        w.Run(Window(vision) - 1);
        Assert.Equal((0, gasp, 0), (HeroOf(w).Air, HeroOf(w).Gasp, HeroOf(w).Out));
        w.Run();
        Assert.False(w.M.Crew.Spared(0));
        Assert.Equal(gasp - 1, HeroOf(w).Gasp);
        w.Run(gasp);
        Assert.True(HeroOf(w).Out > 0);
    }

    /// <summary>The meeting is longer than the last breath: a hero on its last breath as it touches the core is still on it after.</summary>
    [Fact]
    public void TheMeetingOutlastsTheLastBreath() => Assert.True(Content.Crew.MeetingTicks > Content.Crew.LastBreath);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASuitedHeroSpendsNoAirAndItsMealsWaitThroughTheWindowThenBreathesAndEatsOn(bool vision)
    {
        var w = InVacuum(bare: false).Fed();
        w.Do(Spare(w, vision)).Run();
        int air = HeroOf(w).Air, meal = HeroOf(w).Meals[0].Left;
        w.Run(Window(vision) - 1);
        Assert.Equal((air, meal), (HeroOf(w).Air, HeroOf(w).Meals[0].Left));
        w.Run(2 * Content.Crew.BreathEvery);
        Assert.True(HeroOf(w).Air < air);
        Assert.True(HeroOf(w).Meals[0].Left < meal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BlowsShocksAndTheBodysDeathTakeNothingInTheWindowAndHurtAfter(bool vision)
    {
        var w = InVacuum(bare: false);
        w.Do(Spare(w, vision)).Run();
        int air = HeroOf(w).Air;
        w.M.Crew.Blow(0, 30, Harm.Blow);
        w.M.Crew.Shock(0, 300, Harm.Shock);
        w.Do(new Succumb { From = 0 }).Run(Window(vision) - 1);
        Assert.Equal((air, 0, 0, Harm.None), (HeroOf(w).Air, HeroOf(w).Out, HeroOf(w).Hurts, HeroOf(w).Hurt));
        Assert.Equal(["crew.noHero"], w.Refusals.Select(r => r.Reason));
        w.Run();
        w.M.Crew.Blow(0, 3, Harm.Blow);
        Assert.Equal((1, Harm.Blow), (HeroOf(w).Hurts, HeroOf(w).Hurt));
        w.Do(new Succumb { From = 0 }).Run();
        Assert.True(HeroOf(w).Out > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnLavaTheWindowBurnsNothingAndTheLavaBurnsAgainAfter(bool vision)
    {
        var w = new Scenario(Layout.Parse(LavaMap).WithMaterials(LavaGround), doubled: true) { Unpaired = true };
        int onLava = w.At(4, 2);
        w.Do(new Enter(onLava) { From = 0 }).Run();
        w.Do(Spare(w, vision)).Run();
        Assert.True(w.M.Crew.Burns(onLava));
        int air = HeroOf(w).Air, hurts = HeroOf(w).Hurts;
        w.Run(Window(vision) - 1);
        Assert.Equal((air, hurts, 0), (HeroOf(w).Air, HeroOf(w).Hurts, HeroOf(w).Out));
        w.Run(Content.Crew.BreathEvery + 1);
        Assert.Equal(hurts + 1, HeroOf(w).Hurts);
        Assert.True(HeroOf(w).Air <= air - LavaSpec.Burn);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheWindowIsSavedAndLoadedWithTheHero(bool vision)
    {
        var w = InVacuum(bare: false);
        w.Do(Spare(w, vision)).Run(10);
        w.Reload();
        Assert.True(w.M.Crew.Spared(0));
        Assert.Equal(Window(vision) - 10, HeroOf(w).Meets);
    }

    /// <summary>Two forms learned at once play two visions back to back: the second's spare starts over, never adding up past one vision's.</summary>
    [Fact]
    public void ASecondVisionNeverSparesLongerThanOne()
    {
        var w = InVacuum(bare: false);
        w.Do(new Envision { From = 0 }).Run(50);
        w.Do(new Envision { From = 0 }).Run();
        Assert.Equal(Content.Crew.VisionTicks - 1, HeroOf(w).Meets);
        w.Do(new Envision { From = 0 }).Do(new Envision { From = 0 }).Run();
        Assert.Equal(Content.Crew.VisionTicks - 1, HeroOf(w).Meets);
        Assert.Empty(w.Refusals);
    }

    /// <summary>A vision in the meeting's window keeps what is left of the meeting's: a vision never cuts a spare short.</summary>
    [Fact]
    public void AVisionKeepsALongerSpare()
    {
        var w = InVacuum(bare: false);
        w.Do(new PairDrone(Core(w)) { From = 0 }).Run(10);
        w.Do(new Envision { From = 0 }).Run();
        Assert.Equal(Content.Crew.MeetingTicks - 11, HeroOf(w).Meets);
    }

    /// <summary>No hero, or one passed out, has no vision: refused, nothing spared.</summary>
    [Fact]
    public void AVisionIsRefusedWithNoHeroOrOnePassedOut()
    {
        var none = new Scenario(Layout.Parse(Vacuum), doubled: true) { Unpaired = true };
        none.Do(new Envision { From = 3 }).Run();
        Assert.Equal(["crew.noHero"], none.Refusals.Select(r => r.Reason));
        var w = InVacuum(bare: false);
        w.Do(new Succumb { From = 0 }).Run();
        Assert.True(HeroOf(w).Out > 0);
        w.Do(new Envision { From = 0 }).Run();
        Assert.Equal(["crew.noHero"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(0, HeroOf(w).Meets);
    }
}
