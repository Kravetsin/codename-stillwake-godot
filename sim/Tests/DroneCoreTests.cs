using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The drone's core as stamina (drone-abilities, a try): it fills by itself each tick and by loot's worth, its light draws on it, the
/// throw spends it; in creative mode too; nothing unpaired or passed out. It is the player's, kept through death and leaving.
/// </summary>
public class DroneCoreTests
{
    private const string Room = """
        ###########
        #.........#
        #.OOO.....#
        #.OOO.....#
        #.OOO.KK.?#
        ###########
        """;

    private static readonly string Lamp = LightDrone.LampMode, Scan = LightDrone.ScannerMode, Throw = Abilities.Throwing;

    /// <summary>The room with peers 0 and 1 in at the pod out of creative mode, 0 paired, 1 not.</summary>
    private static Scenario Aboard()
    {
        var w = new Scenario(Layout.Parse(Room), doubled: true) { Unpaired = true };
        foreach (int peer in new[] { 0, 1 }) w.Do(new Enter(w.M.Crew.Spawn) { From = peer }).Do(new SetCreative(false) { From = peer });
        w.Run();
        int core = w.M.Grid.MachinesOf(LightDrone.CoreMachine)[0].Id;
        w.Do(new PairDrone(core) { From = 0 }).Run();
        Assert.Empty(w.Refusals);
        // Past the meeting, while which nothing of the hero runs.
        w.M.Crew.HeroOf(0)!.Meets = 0;
        return w;
    }

    private static int Charge(Scenario w, int peer = 0) => w.M.Players.ChargeOf(peer);

    /// <summary>Runs <paramref name="w"/> till peer 0's core is full.</summary>
    private static Scenario Filled(Scenario w)
    {
        w.Run(Abilities.Full / Abilities.Regen + 1);
        Assert.Equal(Abilities.Full, Charge(w));
        return w;
    }

    [Fact]
    public void ItFillsByItselfEachTickUpToFullAndNoFurther()
    {
        var w = Aboard();
        int was = Charge(w);
        w.Run(10);
        Assert.Equal(was + 10 * Abilities.Regen, Charge(w));
        Filled(w).Run(5);
        Assert.Equal(Abilities.Full, Charge(w));
    }

    [Fact]
    public void LootFillsItByItsWorthBesideAFullCatcherMeter()
    {
        var w = Aboard();
        var hero = w.M.Crew.HeroOf(0)!;
        (hero.Need, hero.Wanted) = (Content.Wanted.NeedLeast, Content.Wanted.NeedLeast);
        int was = Charge(w);
        w.M.Crew.Loot(0, [("scrapChunk", 2)]);
        Assert.Equal(was + 2 * Market.WorthOf("scrapChunk") * Abilities.PerWorth, Charge(w));
    }

    [Fact]
    public void NothingFillsUnpairedOrPassedOut()
    {
        var w = Aboard();
        w.Run(20);
        w.M.Crew.Loot(1, [("scrapChunk", 1)]);
        Assert.Equal(0, Charge(w, 1));
        w.M.Crew.HeroOf(0)!.Out = 1000;
        int was = Charge(w);
        w.M.Crew.Loot(0, [("scrapChunk", 1)]);
        w.Run(10);
        Assert.Equal(was, Charge(w));
    }

    [Fact]
    public void ItsLightDrawsOnItTheScannerMoreThanTheLampAndNoneOffAndAnUnknownFormIsRefused()
    {
        var w = Aboard();
        w.Do(new Shine(Lamp)).Run();
        int was = Charge(w);
        w.Run(10);
        Assert.Equal(was + 10 * (Abilities.Regen - Abilities.DrainOf(Lamp)), Charge(w));
        w.Do(new Shine(Scan)).Run();
        was = Charge(w);
        w.Run(10);
        Assert.Equal(was + 10 * (Abilities.Regen - Abilities.DrainOf(Scan)), Charge(w));
        w.Do(new Shine(null)).Do(new Shine("nothing")).Run();
        Assert.Equal("crew.noForm", w.Refusals.Single().Reason);
        Assert.Null(w.M.Crew.HeroOf(0)!.Shining);
    }

    [Fact]
    public void AThrowSpendsItsCostAndIsRefusedTiredUnpairedOrForNoSuchAbility()
    {
        var w = Filled(Aboard());
        w.Do(new Cast(Throw)).Run();
        // The throw on its tick, the tick's filling after it.
        Assert.Equal(Abilities.Full - Abilities.ThrowCost + Abilities.Regen, Charge(w));
        w.Do(new Cast("nothing")).Do(new Cast(Throw) { From = 1 }).Run();
        Assert.Equal(["crew.noAbility", "crew.noDrone"], w.Refusals.Select(r => r.Reason));
        while (w.M.Crew.CastProblem(0, Throw) == null) w.Do(new Cast(Throw)).Run();
        int left = Charge(w);
        Assert.True(left < Abilities.ThrowCost);
        w.Do(new Cast(Throw)).Run();
        Assert.Equal("crew.tired", w.Refusals.Last().Reason);
        Assert.Equal(left + Abilities.Regen, Charge(w));
    }

    [Fact]
    public void AFullCoreHoldsAWholeNumberOfThrowsItsBeads() => Assert.Equal(0, Abilities.Full % Abilities.ThrowCost);

    [Fact]
    public void AThrowEveryTwoSecondsIsPaidByItsOwnFilling()
    {
        var w = Filled(Aboard());
        for (int i = 0; i < 20; i++) w.Do(new Cast(Throw)).Run(2 * World.TicksPerSecond);
        Assert.Empty(w.Refusals);
        Assert.Equal(Abilities.Full, Charge(w));
    }

    [Fact]
    public void InCreativeModeItFillsAndSpendsAsAnywhere()
    {
        var w = Filled(Aboard());
        w.Do(new SetCreative(true)).Do(new Cast(Throw)).Run();
        int spent = Abilities.Full - Abilities.ThrowCost + Abilities.Regen;
        Assert.Equal(spent, Charge(w));
        w.M.Crew.Loot(0, [("scrapChunk", 1)]);
        Assert.Equal(spent + Market.WorthOf("scrapChunk") * Abilities.PerWorth, Charge(w));
    }

    [Fact]
    public void ItIsKeptThroughLeavingAndTwoHerosCoresAreApart()
    {
        var w = Filled(Aboard());
        int core = w.M.Grid.MachinesOf(LightDrone.CoreMachine)[0].Id;
        w.Do(new PairDrone(core) { From = 1 }).Do(new Cast(Throw)).Do(new Leave { From = 0 }).Run();
        w.M.Crew.HeroOf(1)!.Meets = 0;
        int left = Charge(w);
        Assert.Equal(Abilities.Full - Abilities.ThrowCost, left);
        w.Run(10);
        Assert.Equal(left, Charge(w));
        Assert.Equal(10 * Abilities.Regen, Charge(w, 1));
        w.Do(new Enter(w.M.Crew.Spawn) { From = 0 }).Run();
        Assert.Equal(left + Abilities.Regen, Charge(w));
    }

    [Fact]
    public void ASavedCoreOverAFullOneLoadsFull()
    {
        var w = Filled(Aboard());
        string save = w.World.Save().Replace($"\"charge\":{Abilities.Full}", $"\"charge\":{Abilities.Full + 50}");
        Assert.Contains($"\"charge\":{Abilities.Full + 50}", save);
        Assert.Equal(Abilities.Full, w.Reload(save).M.Players.ChargeOf(0));
    }

    /// <summary>Whatever loot, throws, light and time come, to whichever peer, every core stays within 0 and full.</summary>
    [Property]
    public Property EveryCoreStaysWithinNoneAndFull() =>
        Prop.ForAll(Gen.ListOf(Gen.Elements(0, 1).Zip(Gen.Choose(0, 60))).ToArbitrary(), acts =>
        {
            var w = Aboard();
            int core = w.M.Grid.MachinesOf(LightDrone.CoreMachine)[0].Id;
            w.Do(new PairDrone(core) { From = 1 }).Run();
            bool held = true;
            foreach (var (peer, act) in acts)
            {
                if (act < 20) w.M.Crew.Loot(peer, [("opal", act)]);
                else if (act < 40) w.Do(new Cast(Throw) { From = peer }).Run();
                else if (act < 50) w.Do(new Shine(act % 2 == 0 ? Scan : Lamp) { From = peer }).Run(act - 40);
                else w.Run(act - 50);
                held &= new[] { 0, 1 }.All(p => Charge(w, p) is >= 0 and <= Abilities.Full);
            }
            return held;
        });
}
