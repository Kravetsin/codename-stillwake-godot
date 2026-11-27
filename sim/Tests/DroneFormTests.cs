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
/// The drone's nail form (drone-abilities): it fires from the lens on the core's charge, a tenth of a full core a shot; a shot opens a
/// full core and nine more empty it, filling none meanwhile; known in creative, its charge spent there too.
/// </summary>
public class DroneFormTests
{
    private const int Unit = CreaturesModule.Unit, Right = 0;
    private static readonly Gun Form = Shots.DroneNailer;
    private static readonly int Cost = Form.CoreCost;

    /// <summary>Open space, a cell a character: peer 0 in, out of creative mode, paired unless <paramref name="unpaired"/>, the form known if <paramref name="knows"/>.</summary>
    private static Scenario Space(bool knows = true, bool unpaired = false)
    {
        var w = new Scenario(string.Join('\n', Enumerable.Repeat(new string('.', 120), 20)), cells: true) { Unpaired = unpaired };
        w.Do(new Enter(10 * 120 + 2)).Do(new SetCreative(false)).Run();
        if (knows) w.M.Knowledge.Grant(0, Abilities.Nailer);
        return w;
    }

    private static Fire Shot(int peer = 0) => new(Form.Item, 10 * Unit + Unit / 2, 8 * Unit + Unit / 2, Right) { From = peer };

    private static void Fill(Scenario w, int peer = 0) => w.M.Crew.Loot(peer, [("opal", 1000)]);

    [Fact]
    public void ItIsRefusedUnpairedUnknownPassedOutAndBelowAFullCore()
    {
        var unpaired = Space(unpaired: true);
        unpaired.Do(Shot()).Run();
        var unknown = Space(knows: false);
        Fill(unknown);
        unknown.Do(Shot()).Run();
        Assert.Equal(["creatures.noForm", "creatures.noForm"], unpaired.Refusals.Concat(unknown.Refusals).Select(r => r.Reason));

        var w = Space();
        w.M.Crew.Loot(0, [("scrapChunk", 1)]);
        w.Do(Shot()).Run();
        Assert.Equal("creatures.noCharge", w.Refusals.Single().Reason);
        Fill(w);
        w.M.Crew.HeroOf(0)!.Out = 1000;
        w.Do(Shot()).Run();
        Assert.Equal("creatures.out", w.Refusals.Last().Reason);
        Assert.Empty(w.M.Creatures.Shots);
        Assert.Equal(Abilities.Full, w.M.Players.ChargeOf(0));
    }

    [Fact]
    public void AShotOpensAFullCoreANailATenthAndTenEmptyItThenLootFillsItAgain()
    {
        var w = Space();
        Fill(w);
        w.Do(Shot()).Run();
        Assert.Empty(w.Refusals);
        var shot = w.M.Creatures.Shots.Single();
        Assert.Equal((Ref.Hero(0), Form.Item, Shots.Ember.Id), (shot.By, shot.Gun, shot.Projectile));
        Assert.Equal((Abilities.Full - Cost, true), (w.M.Players.ChargeOf(0), w.M.Players.DrainingOf(0)));
        w.M.Crew.Loot(0, [("scrapChunk", 1)]);
        Assert.Equal(Abilities.Full - Cost, w.M.Players.ChargeOf(0));
        for (int i = 1; i < Abilities.Full / Cost; i++) w.Do(Shot()).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((0, false), (w.M.Players.ChargeOf(0), w.M.Players.DrainingOf(0)));
        w.Do(Shot()).Run();
        Assert.Equal("creatures.noCharge", w.Refusals.Single().Reason);
        w.M.Crew.Loot(0, [("scrapChunk", 1)]);
        Assert.Equal(Market.WorthOf("scrapChunk"), w.M.Players.ChargeOf(0));
    }

    [Fact]
    public void InCreativeItIsKnownAndItsChargeSpentAsAnywhere()
    {
        var w = Space(knows: false);
        w.Do(new SetCreative(true)).Do(Shot()).Run();
        Assert.Equal("creatures.noCharge", w.Refusals.Single().Reason);
        Fill(w);
        w.Do(Shot()).Run();
        Assert.Single(w.M.Creatures.Shots);
        Assert.Equal((Abilities.Full - Cost, true), (w.M.Players.ChargeOf(0), w.M.Players.DrainingOf(0)));
    }

    [Fact]
    public void ItsKillIsTheShootersButFillsNoneOfTheCoreItDrains()
    {
        var w = Space();
        Fill(w);
        // The catcher meter emptied, so the kill's loot shows there, the shooter's.
        w.M.Crew.HeroOf(0)!.Wanted = 0;
        var c = BreakerTests.Put(w, 20, 7);
        w.Do(Shot()).Run(30);
        Assert.Empty(w.Refusals);
        Assert.DoesNotContain(w.M.Creatures.Creatures, x => x.Id == c.Id);
        Assert.True(w.M.Crew.HeroOf(0)!.Wanted > 0);
        Assert.Equal(Abilities.Full - Cost, w.M.Players.ChargeOf(0));
    }

    [Fact]
    public void TwoHerosCoresAreApart()
    {
        var w = Space();
        w.Do(new Enter(10 * 120 + 6) { From = 1 }).Do(new SetCreative(false) { From = 1 }).Run();
        w.M.Knowledge.Grant(1, Abilities.Nailer);
        Fill(w);
        Fill(w, 1);
        w.Do(Shot()).Run();
        Assert.Equal((Abilities.Full - Cost, Abilities.Full), (w.M.Players.ChargeOf(0), w.M.Players.ChargeOf(1)));
        Assert.False(w.M.Players.DrainingOf(1));
    }

    [Fact]
    public void ADrainingCoreSavedLoadsDrainingAndABrokenOneDoesNot()
    {
        var w = Space();
        Fill(w);
        w.Do(Shot()).Run();
        var back = w.Reload(w.World.Save());
        Assert.Equal((Abilities.Full - Cost, true), (back.M.Players.ChargeOf(0), back.M.Players.DrainingOf(0)));
        string full = w.World.Save().Replace($"\"charge\":{Abilities.Full - Cost}", $"\"charge\":{Abilities.Full}");
        Assert.False(w.Reload(full).M.Players.DrainingOf(0));
    }

    /// <summary>Whatever loot and shots come, every core stays within 0 and full, draining only while part spent.</summary>
    [Property]
    public Property EveryCoreDrainsOnlyPartSpent() =>
        Prop.ForAll(Gen.ListOf(Gen.Choose(0, 40)).ToArbitrary(), acts =>
        {
            var w = Space();
            bool held = true;
            foreach (int act in acts)
            {
                if (act < 20) w.M.Crew.Loot(0, [("scrapChunk", act)]);
                else if (w.M.Players.CanDischarge(0, Cost)) w.M.Players.Discharge(0, Cost);
                int charge = w.M.Players.ChargeOf(0);
                held &= charge is >= 0 and <= Abilities.Full && (!w.M.Players.DrainingOf(0) || charge is > 0 and < Abilities.Full);
            }
            return held;
        });
}
