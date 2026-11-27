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
/// The drone's core (drone-abilities): a paired hero's loot fills its charge by the loot's worth, up to a full core, beside the catcher
/// meter; nothing unpaired, in creative mode or passed out. It is the player's, kept through death and leaving.
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

    /// <summary>The room with peers 0 and 1 in at the pod out of creative mode, 0 paired, 1 not.</summary>
    private static Scenario Aboard()
    {
        var w = new Scenario(Layout.Parse(Room), doubled: true) { Unpaired = true };
        foreach (int peer in new[] { 0, 1 }) w.Do(new Enter(w.M.Crew.Spawn) { From = peer }).Do(new SetCreative(false) { From = peer });
        w.Run();
        int core = w.M.Grid.MachinesOf(LightDrone.CoreMachine)[0].Id;
        w.Do(new PairDrone(core) { From = 0 }).Run();
        Assert.Empty(w.Refusals);
        return w;
    }

    private static int Worth(string item) => Market.WorthOf(item);

    [Fact]
    public void LootFillsAPairedHerosCoreByItsWorthUpToFullAndNoFurther()
    {
        var w = Aboard();
        Assert.Equal(0, w.M.Players.ChargeOf(0));
        w.M.Crew.Loot(0, [("scrapChunk", 2)]);
        Assert.Equal(2 * Worth("scrapChunk"), w.M.Players.ChargeOf(0));
        w.M.Crew.Loot(0, [("opal", 1000)]);
        Assert.Equal(Abilities.Full, w.M.Players.ChargeOf(0));
        w.M.Crew.Loot(0, [("scrapChunk", 1)]);
        Assert.Equal(Abilities.Full, w.M.Players.ChargeOf(0));
    }

    [Fact]
    public void ItFillsBesideAFullCatcherMeter()
    {
        var w = Aboard();
        var hero = w.M.Crew.HeroOf(0)!;
        (hero.Need, hero.Wanted) = (Content.Wanted.NeedLeast, Content.Wanted.NeedLeast);
        w.M.Crew.Loot(0, [("scrapChunk", 1)]);
        Assert.Equal(Worth("scrapChunk"), w.M.Players.ChargeOf(0));
    }

    [Fact]
    public void NothingFillsUnpairedInCreativeModeOrPassedOut()
    {
        var w = Aboard();
        w.M.Crew.Loot(1, [("scrapChunk", 1)]);
        Assert.Equal(0, w.M.Players.ChargeOf(1));
        w.Do(new SetCreative(true) { From = 0 }).Run();
        w.M.Crew.Loot(0, [("scrapChunk", 1)]);
        Assert.Equal(0, w.M.Players.ChargeOf(0));
        w.Do(new SetCreative(false) { From = 0 }).Run();
        w.M.Crew.HeroOf(0)!.Out = 1000;
        w.M.Crew.Loot(0, [("scrapChunk", 1)]);
        Assert.Equal(0, w.M.Players.ChargeOf(0));
    }

    [Fact]
    public void KillingABreakerFillsItsKillersCore()
    {
        var w = Aboard();
        var cell = w.M.Crew.HeroOf(0)!.Cell;
        var c = BreakerTests.Put(w, cell % w.M.Grid.Width + 3, cell / w.M.Grid.Width - 2);
        string axe = Weapons.Axe.Item;
        w.GiveItems(0, (axe, 1));
        w.Do(new Strike(c.Id, axe) { From = 0 }).Do(new Strike(c.Id, axe) { From = 0 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Worth("scrapChunk"), w.M.Players.ChargeOf(0));
    }

    [Fact]
    public void ItIsKeptThroughLeavingAndComingBackAndTwoHerosCoresAreApart()
    {
        var w = Aboard();
        int core = w.M.Grid.MachinesOf(LightDrone.CoreMachine)[0].Id;
        w.Do(new PairDrone(core) { From = 1 }).Run();
        w.M.Crew.Loot(0, [("scrapChunk", 3)]);
        w.M.Crew.Loot(1, [("scrapChunk", 1)]);
        w.Do(new Leave { From = 0 }).Run();
        Assert.Null(w.M.Crew.HeroOf(0));
        w.Do(new Enter(w.M.Crew.Spawn) { From = 0 }).Run();
        Assert.Equal(3 * Worth("scrapChunk"), w.M.Players.ChargeOf(0));
        Assert.Equal(Worth("scrapChunk"), w.M.Players.ChargeOf(1));
    }

    [Fact]
    public void ASavedCoreOverAFullOneLoadsFull()
    {
        var w = Aboard();
        w.M.Crew.Loot(0, [("opal", 1000)]);
        string save = w.World.Save().Replace($"\"charge\":{Abilities.Full}", $"\"charge\":{Abilities.Full + 50}");
        Assert.Contains($"\"charge\":{Abilities.Full + 50}", save);
        Assert.Equal(Abilities.Full, w.Reload(save).M.Players.ChargeOf(0));
    }

    /// <summary>Whatever loot comes, to whichever peer, every core stays within 0 and full.</summary>
    [Property]
    public Property EveryCoreStaysWithinNoneAndFull() =>
        Prop.ForAll(Gen.ListOf(Gen.Elements(0, 1).Zip(Gen.Choose(0, 40))).ToArbitrary(), loot =>
        {
            var w = Aboard();
            foreach (var (peer, count) in loot) w.M.Crew.Loot(peer, [("opal", count)]);
            return new[] { 0, 1 }.All(p => w.M.Players.ChargeOf(p) is >= 0 and <= Abilities.Full);
        });
}
