using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;
using JetpackSpec = Stillwake.Content.Jetpack;

namespace Stillwake.Sim.Tests;

/// <summary>The oxygen jetpack and the canister breathed into the suit (0047), in crew.</summary>
public class JetpackTests
{
    /// <summary>Two cells of open hull with no room: no air, every breath from the suit.</summary>
    private const string Vacuum = "S.......";

    private static Hero HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id)!;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Canisters(Scenario w, int peer = 0) => w.M.Items.Inventory(peer).Sum(s => s?.Item == "canister" ? s.Count : 0);

    /// <summary>Heroes 0 and 1 aboard side by side out of any air, hero 0 wearing a jetpack: 1 breathes as 0 does, but never thrusts.</summary>
    private static Scenario Pair()
    {
        var w = new Scenario(Vacuum).Wear(0, JetpackSpec.Item);
        return w.Do(new Enter(1) { From = 0 }).Do(new Enter(1) { From = 1 }).Run();
    }

    /// <summary>What hero 0's thrust has cost its suit: the units it has below hero 1's.</summary>
    private static int Thrusted(Scenario w) => HeroOf(w, 1).Suit - HeroOf(w, 0).Suit;

    [Fact]
    public void ThrustVentsAUnitOfTheSuitEveryTenTicksOfThrust()
    {
        var w = Pair().Do(new Thrust(true) { From = 0 }).Run(JetpackSpec.TicksPerUnit * 3);
        Assert.True(HeroOf(w).Thrusting);
        Assert.Equal(3, Thrusted(w));
        w.Run(JetpackSpec.TicksPerUnit - 1);
        Assert.Equal(3, Thrusted(w));
        w.Run();
        Assert.Equal(4, Thrusted(w));
        w.Do(new Thrust(false) { From = 0 }).Run(JetpackSpec.TicksPerUnit * 3);
        Assert.False(HeroOf(w).Thrusting);
        Assert.Equal(4, Thrusted(w));
    }

    [Fact]
    public void KeepsTheTicksOfShortTapsSoTheyAddUp()
    {
        var w = Pair();
        for (int tap = 0; tap < 2; tap++)
        {
            Assert.Equal(0, Thrusted(w));
            w.Do(new Thrust(true) { From = 0 }).Run(JetpackSpec.TicksPerUnit / 2);
            w.Do(new Thrust(false) { From = 0 }).Run(30);
        }
        Assert.Equal(1, Thrusted(w));
        Assert.Equal(0, HeroOf(w).ThrustTicks);
    }

    [Fact]
    public void RefusesThrustWithNoJetpackWornOrNoHeroButStopsAnyway()
    {
        var w = Pair();
        w.Do(new Thrust(true) { From = 1 }).Do(new Thrust(true) { From = 5 }).Run();
        Assert.Equal(["crew.noJetpack", "crew.noHero"], Reasons(w));
        Assert.False(HeroOf(w, 1).Thrusting);
        w.Do(new Thrust(false) { From = 1 }).Run();
        Assert.Equal(2, w.Refusals.Count);
    }

    [Fact]
    public void StopsThrustingOnceTheJetpackComesOff()
    {
        var w = Pair().Do(new Thrust(true) { From = 0 }).Run();
        w.Do(new Move(new Items.Place(Content.Inventory.Slots + 1), new Target(0), 1) { From = 0 }).Run();
        Assert.False(HeroOf(w).Thrusting);
        w.Run(JetpackSpec.TicksPerUnit * 2);
        Assert.Equal(0, Thrusted(w));
    }

    [Fact]
    public void PassingOutStopsTheThrustAndAThrustLandingOnAHeroOutDoesNothing()
    {
        var w = Pair().Do(new Thrust(true) { From = 0 });
        for (int i = 0; i < 2000 && HeroOf(w).Out == 0; i++) w.Run();
        Assert.True(HeroOf(w).Out > 0);
        Assert.False(HeroOf(w).Thrusting);
        w.Do(new Thrust(true) { From = 0 }).Run();
        Assert.False(HeroOf(w).Thrusting);
        Assert.Equal(0, HeroOf(w).Suit);
    }

    // Canisters.

    /// <summary>Hero 0 aboard out of any air with a suit of <paramref name="suit"/>, carrying <paramref name="canisters"/>; the next tick no breath.</summary>
    private static Scenario Breathing(int suit, int canisters)
    {
        var w = new Scenario(Vacuum);
        if (canisters > 0) w.GiveItems(0, ("canister", canisters));
        w.Do(new Enter(1) { From = 0 }).Run();
        HeroOf(w).Suit = suit;
        if (w.Crew().Clock % Content.Crew.BreathEvery == Content.Crew.BreathEvery - 1) w.Run();
        return w;
    }

    [Fact]
    public void BreathesACanisterIntoTheSuitTwentyUnitsUpToFull()
    {
        var w = Breathing(10, 2).Do(new Refill { From = 0 }).Run();
        Assert.Equal(10 + Content.Crew.CanisterRefill, HeroOf(w).Suit);
        Assert.Equal(1, Canisters(w));
        w = Breathing(Content.Crew.Suit - 5, 2).Do(new Refill { From = 0 }).Run();
        Assert.Equal(Content.Crew.Suit, HeroOf(w).Suit);
        Assert.Equal(1, Canisters(w));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void RefusesACanisterIntoAFullSuitWithNoneCarriedOrWithNoHero()
    {
        var w = Breathing(Content.Crew.Suit, 2).Do(new Refill { From = 0 }).Do(new Refill { From = 4 }).Run();
        Assert.Equal(["crew.suitFull", "crew.noHero"], Reasons(w));
        Assert.Equal(2, Canisters(w));
        w = Breathing(10, 0).Do(new Refill { From = 0 }).Run();
        Assert.Equal(["items.notEnough"], Reasons(w));
        Assert.Equal(10, HeroOf(w).Suit);
        Assert.Equal("items.notEnough", w.M.Crew.RefillProblem(0));
    }

    [Fact]
    public void NeedsNoCanisterInCreativeModeWhereTheSuitIsFullAndStaysSo()
    {
        var w = Breathing(10, 0).Do(new SetCreative(true)).Do(new Refill { From = 0 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Content.Crew.Suit, HeroOf(w).Suit);
        w.Do(new Exert(true) { From = 0 }).Run(Content.Crew.BreathEvery * 100);
        Assert.Equal((Content.Crew.Suit, 0), (HeroOf(w).Suit, HeroOf(w).Out));
        w.Do(new Refill { From = 0 }).Run();
        Assert.Equal(["crew.suitFull"], Reasons(w));
        w.Do(new SetCreative(false)).Run(Content.Crew.BreathEvery);
        Assert.True(HeroOf(w).Suit < Content.Crew.Suit);
    }
}
