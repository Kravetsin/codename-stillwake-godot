using Stillwake.Sim.Fauna;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Fauna;

namespace Stillwake.Sim.Tests;

/// <summary>Rams (0103): a cornered creature winds up and charges the nearest hero, and a charge that hits costs its suit air.</summary>
public class FaunaRamTests
{
    private const int Unit = FaunaModule.Unit;
    private static readonly string Axe = Spec.Weapons.Axe.Item;
    private static readonly Tune.Species Tapir = Tune.Tapir;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>A pen 18 wide, its floor row 4, three rows high: too low for any leap over its side walls.</summary>
    private const string Pen = """
        ##################
        #................#
        #................#
        #................#
        ##################
        """;

    /// <summary>A pen whose floor is a ledge three rows over the floor at its right: a drop no walker or charge goes off.</summary>
    private const string Ledge = """
        ##############
        #............#
        #............#
        #######......#
        #######......#
        #######......#
        ##############
        """;

    private static readonly string Long = string.Join('\n',
        [new string('#', 48), .. Enumerable.Repeat("#" + new string('.', 46) + "#", 3), new string('#', 48)]);

    private static int Suit(Scenario w, int peer) => w.M.Crew.HeroOf(peer)!.Suit;

    private static bool Clear(Scenario w, Creature c) => !w.M.Fauna.Overlaps(Tune.Get(c.Species), c.X, c.Y);

    /// <summary>
    /// <paramref name="c"/> sent fleeing right into the pen's wall again and again: whether, of up to <paramref name="tries"/>
    /// turns there, one became a wind-up rather than a turn.
    /// </summary>
    private static bool WindsUp(Scenario w, Creature c, int tries = 40)
    {
        for (int i = 0; i < tries; i++)
        {
            c.Walking(+1, Mode.Flee);
            for (int t = 0; t < 60 && c.Mode == Mode.Flee && c.VX > 0; t++) w.Run();
            if (c.Mode == Mode.WindUp) return true;
            Assert.True(c.VX < 0, "it neither turned nor wound up");
        }
        return false;
    }

    // The trigger.

    [Fact]
    public void ACorneredTapirWithAHeroWithinReachWindsUpStandingThenChargesAtIt()
    {
        var w = FaunaTesting.Planet(Pen).Breathing().HeroAt(10, 3);
        var c = w.Let(Tapir, 13, 3);
        Assert.True(WindsUp(w, c));
        Assert.Equal(0, c.VX);
        int x = c.X, held = 0;
        while (c.Mode == Mode.WindUp)
        {
            Assert.Equal(x, c.X);
            w.Run();
            held++;
        }
        // Its wind-up's ticks, then at the hero, at the charge's speed, along the ground.
        Assert.Equal(Tune.WindUp, held);
        Assert.Equal(Mode.Charge, c.Mode);
        Assert.Equal(-Tune.Charge, c.VX);
        Assert.Equal(4 * Unit, c.Y);
    }

    [Fact]
    public void ACorneredTapirWithTheHeroBeyondReachTurnsAsEver()
    {
        // Its middle at the wall is in column 15; the hero's cell nine columns off is past RamReach's six.
        var w = FaunaTesting.Planet(Pen).Breathing().HeroAt(6, 3);
        var c = w.Let(Tapir, 13, 3);
        Assert.False(WindsUp(w, c));
    }

    [Fact]
    public void TheFluffNeverRamsNorDoesAWandererThatMeetsAWall()
    {
        Assert.Equal((0, 0), (Tune.Fluff.Ram, Tune.Fluff.RamChance));
        var w = FaunaTesting.Planet(Pen).Breathing().HeroAt(10, 3);
        var fluff = w.Let(Tune.Fluff, 13, 3);
        Assert.False(WindsUp(w, fluff));

        var v = FaunaTesting.Planet(Pen).Breathing().HeroAt(10, 3);
        var tapir = v.Let(Tapir, 13, 3).Walking(+1);
        for (int t = 0; t < 400; t++)
        {
            v.Run();
            Assert.NotEqual(Mode.WindUp, tapir.Mode);
            if (tapir.VX < 0) tapir.Walking(+1);
        }
    }

    [Fact]
    public void EveryWalkerOfTheOpenGroundRamsAndTheTapirHardest()
    {
        Assert.Equal((5, 900), (Tapir.Ram, Tapir.RamChance));
        Assert.Equal((3, 700, 3, 700), (Tune.Loper.Ram, Tune.Loper.RamChance, Tune.Sixleg.Ram, Tune.Sixleg.RamChance));
        Assert.All(Tune.All, s => Assert.InRange(s.RamChance, s.Ram > 0 ? 1 : 0, s.Ram > 0 ? 1000 : 0));
    }

    // Ramming back after a hit.

    /// <summary>
    /// A tapir struck on the long floor by a hero <paramref name="off"/> columns left of it, on a world of <paramref name="seed"/>:
    /// the ticks till it begins its charge back at the hero, none in <paramref name="ticks"/>; and whether that charge passed the hero.
    /// </summary>
    private static (int? ChargedAt, bool Passed) StruckFrom(Tune.Species species, int off, uint seed, int ticks = 60)
    {
        var w = FaunaTesting.Planet(Long, seed: seed).Breathing().HeroAt(8, 3);
        var c = w.Let(species, 8 + off - 1, 3).Still();
        w.Do(new Strike(c.Id, Axe)).Run();
        int? charged = null;
        bool passed = false;
        for (int t = 1; t <= ticks; t++)
        {
            w.Run();
            if (c.Mode == Mode.Charge) charged ??= t;
            passed |= charged != null && c.X < 8 * Unit;
        }
        return (charged, passed);
    }

    [Fact]
    public void AStruckTapirWithTheHeroEightCellsOffRamsItBackWithinTwoSecondsNineTimesInTen()
    {
        var runs = Enumerable.Range(1, 10).Select(seed => StruckFrom(Tapir, 8, (uint)seed)).ToList();
        int rammed = runs.Count(r => r.ChargedAt <= 2 * World.TicksPerSecond);
        Assert.True(rammed >= 9, $"{rammed} of 10 rammed back within 2 s: {string.Join(", ", runs)}");
        // And the charge reaches it: through where it stands.
        Assert.All(runs.Where(r => r.ChargedAt != null), r => Assert.True(r.Passed));
    }

    [Fact]
    public void AStruckLoperRamsBackMostTimesAndTheFluffNever()
    {
        int lopers = Enumerable.Range(1, 10).Count(seed => StruckFrom(Tune.Loper, 3, (uint)seed).ChargedAt != null);
        Assert.InRange(lopers, 5, 10);
        Assert.All(Enumerable.Range(1, 10), seed => Assert.Null(StruckFrom(Tune.Fluff, 3, (uint)seed, ticks: 200).ChargedAt));
    }

    [Fact]
    public void AStruckTapirFleesAMomentThenTurnsWindsUpFacingTheHeroAndCharges()
    {
        var w = FaunaTesting.Planet(Long).Breathing().HeroAt(8, 3);
        var c = w.Let(Tapir, 11, 3).Still();
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal(Mode.Retreat, c.Mode);
        // Its draw, less the tick that ran since.
        Assert.InRange(c.ModeLeft, Tune.RamFleeLeast - 1, Tune.RamFleeMost - 1);
        Assert.True(c.VX > 0, "not fleeing away from the hero");
        while (c.Mode == Mode.Retreat) w.Run();
        Assert.Equal(Mode.WindUp, c.Mode);
        while (c.Mode == Mode.WindUp) w.Run();
        Assert.Equal((Mode.Charge, -Tune.Charge), (c.Mode, c.VX));
    }

    [Fact]
    public void AHeroPastRamBackReachStrikingItIsNotRammedBack()
    {
        // Struck from 14 columns off (the client's word on reach): it flees as ever.
        var w = FaunaTesting.Planet(Long).Breathing().HeroAt(8, 3);
        var c = w.Let(Tapir, 21, 3).Still();
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal(Mode.Flee, c.Mode);
    }

    [Fact]
    public void OneHitOneRamItsChargeSpentItEscapesAndNeverRamsAgainTillStruck()
    {
        // In the pen, the hero by its left wall: the charge back meets the wall missing it, then it flees, cornered again and again.
        var w = FaunaTesting.Planet(Pen).Breathing().HeroAt(3, 3);
        var c = w.Let(Tapir, 6, 3).Still();
        w.Do(new Strike(c.Id, Axe)).Run();
        int windUps = 0;
        var was = c.Mode;
        for (int t = 0; t < 400; t++)
        {
            w.Run();
            if (c.Mode == Mode.WindUp && was != Mode.WindUp) windUps++;
            if (was == Mode.Charge && c.Mode != Mode.Charge) Assert.Equal(Mode.Escape, c.Mode);
            was = c.Mode;
        }
        Assert.Equal(1, windUps);
        // Struck again, it may ram again.
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.True(FaunaModule.Fleeing(c.Mode));
    }

    [Fact]
    public void AStrikeDuringTheWindUpCancelsItsChargeAndKnocksItAway()
    {
        var w = FaunaTesting.Planet(Pen).Breathing().HeroAt(10, 3);
        var c = w.Let(Tapir, 13, 3);
        Assert.True(WindsUp(w, c));
        int left = c.ModeLeft;
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.True(c.Mode is Mode.Flee or Mode.Retreat, $"{c.Mode}");
        Assert.True(c.VX > 0, "not knocked away from the striker");
        Assert.Equal(Spec.Weapons.Axe.Damage, c.Damage);
        // When its wind-up would have ended it does not charge: its cycle begins again from the blow.
        w.Run(left);
        Assert.NotEqual(Mode.Charge, c.Mode);
    }

    // The charge.

    [Fact]
    public void AChargeRunsAtItsSpeedPastTheHeroByRamPastThenEscapesOnItsWay()
    {
        var w = FaunaTesting.Planet(Long).Breathing().HeroAt(20, 3);
        var c = w.Let(Tapir, 33, 3).Charging(-1);
        int ticks = 0;
        while (c.Mode == Mode.Charge)
        {
            int x = c.X;
            w.Run();
            ticks++;
            if (c.Mode == Mode.Charge) Assert.Equal(x - Tune.Charge, c.X);
        }
        Assert.True(ticks < Tune.ChargeTicks, "it ran out its charge");
        Assert.Equal((Mode.Escape, -Tapir.Flee), (c.Mode, c.VX));
        // Turned to escaping the tick its middle was RamPast cells past the hero's, and on a step that tick.
        Assert.InRange(20 * Unit + Unit / 2 - c.X, Tune.RamPast * Unit + Tapir.Flee, Tune.RamPast * Unit + Tune.Charge + Tapir.Flee);
    }

    [Fact]
    public void AChargeEndsAtAWallFlushWithIt()
    {
        var w = FaunaTesting.Planet(Pen).Breathing().HeroAt(16, 3);
        var c = w.Let(Tapir, 8, 3).Charging(+1);
        int ticks = 0, most = 0;
        while (c.Mode == Mode.Charge)
        {
            w.Run();
            ticks++;
            most = Math.Max(most, c.X);
            Assert.True(Clear(w, c));
        }
        Assert.True(ticks < Tune.ChargeTicks, "it ran out its charge rather than meet the wall");
        // Flush with the wall, short of the hero's middle by less than RamPast, then turned from it, escaping.
        Assert.Equal(17 * Unit - Unit, most);
        Assert.Equal((Mode.Escape, -Tapir.Flee), (c.Mode, c.VX));
    }

    [Fact]
    public void AChargeEndsAtADropNeverGoingOffTheLedge()
    {
        var w = FaunaTesting.Planet(Ledge).Breathing().HeroAt(11, 5);
        var c = w.Let(Tapir, 2, 2).Charging(+1);
        int most = 0;
        while (c.Mode == Mode.Charge)
        {
            w.Run();
            most = Math.Max(most, c.X + Tapir.Width * Unit / 2);
            Assert.Equal(3 * Unit, c.Y);
        }
        Assert.Equal((Mode.Escape, -Tapir.Flee), (c.Mode, c.VX));
        Assert.InRange(most, 7 * Unit - Tune.Charge, 7 * Unit);
    }

    // Hit by it.

    [Fact]
    public void AChargeThatHitsTakesItsRamFromTheSuitAndTheCreatureRecoilsFleeing()
    {
        var w = FaunaTesting.Planet(Pen).HeroAt(3, 3).HeroAt(3, 3, peer: 1);
        var c = w.Let(Tapir, 6, 3).Charging(-1);
        Assert.Null(w.M.Fauna.HitByProblem(c.Id));
        int x = c.X;
        w.Do(new HitBy(c.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Tapir.Ram, Suit(w, 1) - Suit(w, 0));
        // Back the way it came, hopping off the hero, its ram spent.
        Assert.Equal((Mode.Escape, Tapir.Flee), (c.Mode, c.VX));
        Assert.True(c.X > x && c.VY < 0, $"at {c.X} from {x}, rising {c.VY}");
    }

    [Fact]
    public void AChargeHitsOnceTheSecondHeroItTouchedPaysNothing()
    {
        var w = FaunaTesting.Planet(Pen).HeroAt(3, 3).HeroAt(3, 3, peer: 1).HeroAt(3, 3, peer: 2);
        var c = w.Let(Tune.Loper, 6, 3).Charging(-1);
        w.Do(new HitBy(c.Id)).Do(new HitBy(c.Id) { From = 1 }).Run();
        w.Do(new HitBy(c.Id)).Run();
        Assert.Equal(["fauna.notAttacking", "fauna.notAttacking"], Reasons(w));
        Assert.Equal(Tune.Loper.Ram, Suit(w, 2) - Suit(w, 0));
        Assert.Equal(Suit(w, 2), Suit(w, 1));
    }

    [Fact]
    public void AHitByACreatureNotChargingOrGoneIsRefused()
    {
        var w = FaunaTesting.Planet(Pen).HeroAt(3, 3).HeroAt(3, 3, peer: 1);
        var c = w.Let(Tapir, 6, 3).Still();
        Assert.Equal("fauna.notAttacking", w.M.Fauna.HitByProblem(c.Id));
        Assert.Equal("fauna.gone", w.M.Fauna.HitByProblem(c.Id + 1));
        w.Do(new HitBy(c.Id)).Do(new HitBy(c.Id + 1)).Run();
        (c.Mode, c.ModeLeft) = (Mode.WindUp, Tune.WindUp);
        w.Do(new HitBy(c.Id)).Run();
        Assert.Equal(["fauna.notAttacking", "fauna.gone", "fauna.notAttacking"], Reasons(w));
        Assert.Equal(Suit(w, 1), Suit(w, 0));
    }

    [Fact]
    public void InCreativeModeAChargeThatHitsTakesNothing()
    {
        var w = FaunaTesting.Planet(Pen).Breathing().HeroAt(3, 3);
        var c = w.Let(Tapir, 6, 3).Charging(-1);
        w.Do(new HitBy(c.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Spec.Crew.Suit, Suit(w, 0));
        Assert.Equal(Mode.Escape, c.Mode);
    }

    [Fact]
    public void ARamThatTakesTheLastUnitPassesTheHeroOutItsBodyLeftWhereItFell()
    {
        var w = FaunaTesting.Planet(Pen).HeroAt(3, 3).GiveItems(0, ("scrap", 4));
        var c = w.Let(Tapir, 6, 3).Charging(-1);
        w.M.Crew.HeroOf(0)!.Suit = Tapir.Ram - 1;
        w.Do(new HitBy(c.Id)).Run();
        var hero = w.M.Crew.HeroOf(0)!;
        Assert.True(hero.Out > 0);
        Assert.Equal(0, hero.Suit);
        var body = Assert.Single(w.M.Items.Floor);
        Assert.Equal(("scrap", 4, 0), (body.Item, body.Count, body.Body!.Peer));
    }
}
