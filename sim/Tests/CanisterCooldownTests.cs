using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Xunit;
using CrewSpec = Stillwake.Content.Crew;

namespace Stillwake.Sim.Tests;

/// <summary>The canister's cooldown (food-and-the-body): a hero breathes another only <c>Crew.CanisterCooldown</c> ticks after the last.</summary>
public class CanisterCooldownTests
{
    /// <summary>Open hull with no room: no air, every breath from the suit.</summary>
    private const string Vacuum = "S.......";

    private static Hero HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id)!;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>Heroes 0 and 1 aboard out of any air, each with a low suit and three canisters.</summary>
    private static Scenario Low()
    {
        var w = new Scenario(Vacuum).GiveItems(0, (CrewSpec.Canister, 3)).GiveItems(1, (CrewSpec.Canister, 3));
        w.Do(new Enter(1) { From = 0 }).Do(new Enter(1) { From = 1 }).Run();
        HeroOf(w, 0).Air = 5;
        HeroOf(w, 1).Air = 5;
        return w;
    }

    private static Scenario Breathe(Scenario w, int peer = 0) => w.Do(new Refill { From = peer }).Run();

    [Fact]
    public void RefusesASecondCanisterUntilTheCooldownHasPassedThenTakesIt()
    {
        var w = Breathe(Low());
        int air = HeroOf(w).Air;
        Breathe(w);
        Assert.Equal(["crew.canisterCooling"], Reasons(w));
        Assert.Equal(air, HeroOf(w).Air);
        // The first applied on tick T, its own tick counted: a canister is refused on T + 499 and taken on T + 500.
        w.Run(CrewSpec.CanisterCooldown - 3);
        Breathe(w);
        Assert.Equal(2, w.Refusals.Count);
        Breathe(w);
        Assert.Equal(2, w.Refusals.Count);
        Assert.Equal(CrewSpec.CanisterCooldown - 1, HeroOf(w).CanisterIn);
    }

    [Fact]
    public void KeepsEachHerosCooldownToItself()
    {
        var w = Breathe(Low(), 0);
        Breathe(w, 1);
        Assert.Empty(w.Refusals);
        Breathe(w, 0);
        Assert.Equal(["crew.canisterCooling"], Reasons(w));
    }

    [Fact]
    public void ACreativeHeroBreathesOneAfterAnother()
    {
        var w = new Scenario(Vacuum).Do(new SetCreative(true) { From = 0 });
        w.Do(new Enter(1) { From = 0 }).Run();
        for (int i = 0; i < 3; i++)
        {
            HeroOf(w).Air = 5;
            Breathe(w);
        }
        Assert.Empty(w.Refusals);
        Assert.Equal(0, HeroOf(w).CanisterIn);
    }

    [Fact]
    public void RefusesAFullSuitFirstThenTheCooldownThenNoCanister()
    {
        var w = new Scenario(Vacuum).GiveItems(0, (CrewSpec.Canister, 1));
        w.Do(new Enter(1) { From = 0 }).Run();
        HeroOf(w).Air = 5;
        Breathe(w);
        HeroOf(w).Air = w.M.Crew.CapacityOf(0);
        Breathe(w);
        HeroOf(w).Air = 5;
        Breathe(w);
        HeroOf(w).CanisterIn = 0;
        Breathe(w);
        Assert.Equal(["crew.suitFull", "crew.canisterCooling", "items.notEnough"], Reasons(w));
    }

    [Fact]
    public void ARefusedCanisterStartsNoCooldown()
    {
        var w = new Scenario(Vacuum);
        w.Do(new Enter(1) { From = 0 }).Run();
        HeroOf(w).Air = 5;
        Breathe(w);
        Assert.Equal(["items.notEnough"], Reasons(w));
        Assert.Equal(0, HeroOf(w).CanisterIn);
    }

    [Fact]
    public void APassedOutHeroBreathesNoneAndWakesWithNoCooldown()
    {
        var w = Breathe(Low());
        HeroOf(w).Air = 0;
        w.Run(CrewSpec.BreathEvery + CrewSpec.LastBreath + 1);
        Assert.True(HeroOf(w).Out > 0);
        Assert.Equal(0, HeroOf(w).CanisterIn);
        w.Do(new Refill { From = 0 }).Run();
        Assert.Equal(["crew.noHero"], Reasons(w));
    }

    /// <summary>A hero sends a canister (<c>Ticks</c> 0) or waits that many ticks, its suit drawn low or not.</summary>
    private sealed record Step(int Hero, int Ticks, bool Low);

    private static readonly Gen<Step> Steps =
        from hero in Gen.Choose(0, 1)
        from ticks in Gen.Frequency((3, Gen.Constant(0)), (2, Gen.Choose(1, 40)), (1, Gen.Choose(100, 600)))
        from low in Gen.Elements(true, false)
        select new Step(hero, ticks, low);

    [Property(MaxTest = 60)]
    public Property NeverTakesTwoCanistersOfOneHeroCloserThanTheCooldown()
    {
        var steps = from n in Gen.Choose(0, 40) from s in Steps.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = new Scenario(Vacuum).GiveItems(0, (CrewSpec.Canister, 20)).GiveItems(1, (CrewSpec.Canister, 20));
            w.Do(new Enter(1) { From = 0 }).Do(new Enter(1) { From = 1 }).Run();
            var last = new int?[2];
            int tick = 0;
            void Tick()
            {
                int[] before = [Held(w, 0), Held(w, 1)];
                w.Run();
                tick++;
                for (int peer = 0; peer < 2; peer++)
                {
                    // Passing out leaves what it carried and prints a new person, its cooldown gone with the body.
                    if (HeroOf(w, peer).Out > 0) last[peer] = null;
                    if (HeroOf(w, peer).Out > 0 || Held(w, peer) == before[peer]) continue;
                    if (last[peer] is int at && tick - at < CrewSpec.CanisterCooldown) throw new Exception($"hero {peer}: canisters {tick - at} ticks apart");
                    last[peer] = tick;
                }
            }
            foreach (var s in all)
            {
                if (HeroOf(w, s.Hero) is { Out: 0 } hero && s.Low) hero.Air = 3;
                if (s.Ticks == 0) w.Do(new Refill { From = s.Hero });
                for (int i = 0; i < Math.Max(1, s.Ticks); i++) Tick();
            }
        });
    }

    private static int Held(Scenario w, int peer) => w.M.Items.Inventory(peer).Sum(s => s?.Item == CrewSpec.Canister ? s.Count : 0);

    [Fact]
    public void CarriesTheTicksLeftToAnotherSurfaceWhateverItsClock()
    {
        var w = Breathe(Low());
        w.Run(100);
        var carried = w.M.Crew.Depart(0);
        var there = new Scenario(Vacuum).Run(7);
        there.M.Crew.Arrive(0, carried, null);
        Assert.Equal(CrewSpec.CanisterCooldown - 101, HeroOf(there).CanisterIn);
        there.Run(CrewSpec.CanisterCooldown - 101);
        Assert.Equal(0, HeroOf(there).CanisterIn);
    }
}
