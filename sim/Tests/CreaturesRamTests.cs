using Stillwake.Sim.Creatures;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>Rams (0103): a cornered creature winds up and charges the nearest hero, and a charge that hits costs its suit air.</summary>
public class CreaturesRamTests
{
    /// <summary>A cell, and a character of a doubled map: two cells across and down (0104).</summary>
    private const int Unit = CreaturesModule.Unit, Cell = 2 * Unit;
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

    private static int Suit(Scenario w, int peer) => w.M.Crew.HeroOf(peer)!.Air;

    private static bool Clear(Scenario w, Creature c) => !w.M.Creatures.Overlaps(Tune.Get(c.Species).Body, c.X, c.Y);

    /// <summary>
    /// <paramref name="c"/> sent fleeing right into the pen's wall again and again: whether, of up to <paramref name="tries"/>
    /// turns there, one became a wind-up rather than a turn.
    /// </summary>
    private static bool WindsUp(Scenario w, Creature c, int tries = 40)
    {
        for (int i = 0; i < tries; i++)
        {
            c.Walking(+1, Posture.Flee);
            for (int t = 0; t < 60 && c.Posture == Posture.Flee && c.VX > 0; t++) w.Run();
            if (c.Posture == Posture.WindUp) return true;
            Assert.True(c.VX < 0, "it neither turned nor wound up");
        }
        return false;
    }

    // The trigger.

    [Fact]
    public void ACorneredTapirWithAHeroWithinReachWindsUpStandingThenChargesAtIt()
    {
        var w = CreaturesTesting.Planet(Pen).Breathing().HeroAt(10, 3);
        var c = w.Let(Tapir, 13, 3);
        Assert.True(WindsUp(w, c));
        Assert.Equal(0, c.VX);
        int x = c.X, held = 0;
        while (c.Posture == Posture.WindUp)
        {
            Assert.Equal(x, c.X);
            w.Run();
            held++;
        }
        // Its wind-up's ticks, then at the hero, at the charge's speed, along the ground.
        Assert.Equal(Tapir.Lunge().WindUp, held);
        Assert.Equal(Posture.Charge, c.Posture);
        Assert.Equal(-Tapir.Lunge().Speed, c.VX);
        Assert.Equal(4 * Cell, c.Y);
    }

    [Fact]
    public void ACorneredTapirWithTheHeroBeyondReachTurnsAsEver()
    {
        // Its middle at the wall is in column 32; the hero's cell 20 columns off is past RamReach's 12.
        var w = CreaturesTesting.Planet(Pen).Breathing().HeroAt(6, 3);
        var c = w.Let(Tapir, 13, 3);
        Assert.False(WindsUp(w, c));
    }

    [Fact]
    public void TheFluffNeverRamsNorDoesAWandererThatMeetsAWall()
    {
        Assert.Equal((0, 0), (Tune.Fluff.Ram(), Tune.Fluff.RamChance()));
        var w = CreaturesTesting.Planet(Pen).Breathing().HeroAt(10, 3);
        var fluff = w.Let(Tune.Fluff, 13, 3);
        Assert.False(WindsUp(w, fluff));

        var v = CreaturesTesting.Planet(Pen).Breathing().HeroAt(10, 3);
        var tapir = v.Let(Tapir, 13, 3).Walking(+1);
        for (int t = 0; t < 400; t++)
        {
            v.Run();
            Assert.NotEqual(Posture.WindUp, tapir.Posture);
            if (tapir.VX < 0) tapir.Walking(+1);
        }
    }

    [Fact]
    public void EveryWalkerOfTheOpenGroundRamsAndTheTapirHardest()
    {
        Assert.Equal((5, 900), (Tapir.Ram(), Tapir.RamChance()));
        Assert.Equal((3, 700, 3, 700), (Tune.Loper.Ram(), Tune.Loper.RamChance(), Tune.Sixleg.Ram(), Tune.Sixleg.RamChance()));
        Assert.All(Tune.All.Where(s => s.Body is Tune.Walker), s => Assert.InRange(s.RamChance(), s.Ram() > 0 ? 1 : 0, s.Ram() > 0 ? 1000 : 0));
    }

    // Ramming back after a hit.

    /// <summary>
    /// A tapir struck on the long floor by a hero <paramref name="off"/> columns left of it, on a world of <paramref name="seed"/>:
    /// the ticks till it begins its charge back at the hero, none in <paramref name="ticks"/>; and whether that charge passed the hero.
    /// </summary>
    private static (int? ChargedAt, bool Passed) StruckFrom(Tune.Species species, int off, uint seed, int ticks = 60)
    {
        var w = CreaturesTesting.Planet(Long, seed: seed).Breathing().HeroAt(8, 3);
        var c = w.Let(species, 8 + off - 1, 3).Still();
        w.Do(new Strike(c.Id, Axe)).Run();
        int? charged = null;
        bool passed = false;
        for (int t = 1; t <= ticks; t++)
        {
            w.Run();
            if (c.Posture == Posture.Charge) charged ??= t;
            passed |= charged != null && c.X < 8 * Cell;
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
        var w = CreaturesTesting.Planet(Long).Breathing().HeroAt(8, 3);
        var c = w.Let(Tapir, 11, 3).Still();
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal(Posture.Retreat, c.Posture);
        // Its draw, less the tick that ran since.
        Assert.InRange(c.Left, Tapir.Retaliation().FleeLeast - 1, Tapir.Retaliation().FleeMost - 1);
        Assert.True(c.VX > 0, "not fleeing away from the hero");
        while (c.Posture == Posture.Retreat) w.Run();
        Assert.Equal(Posture.WindUp, c.Posture);
        while (c.Posture == Posture.WindUp) w.Run();
        Assert.Equal((Posture.Charge, -Tapir.Lunge().Speed), (c.Posture, c.VX));
    }

    [Fact]
    public void AHeroPastRamBackReachStrikingItIsNotRammedBack()
    {
        // Struck from 28 columns off (the client's word on reach): it flees as ever.
        var w = CreaturesTesting.Planet(Long).Breathing().HeroAt(8, 3);
        var c = w.Let(Tapir, 21, 3).Still();
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal(Posture.Flee, c.Posture);
    }

    [Fact]
    public void OneHitOneRamItsChargeSpentItEscapesAndNeverRamsAgainTillStruck()
    {
        // In the pen, the hero by its left wall: the charge back meets the wall missing it, then it flees, cornered again and again.
        var w = CreaturesTesting.Planet(Pen).Breathing().HeroAt(3, 3);
        var c = w.Let(Tapir, 6, 3).Still();
        w.Do(new Strike(c.Id, Axe)).Run();
        int windUps = 0;
        var was = c.Posture;
        for (int t = 0; t < 400; t++)
        {
            w.Run();
            if (c.Posture == Posture.WindUp && was != Posture.WindUp) windUps++;
            if (was == Posture.Charge && c.Posture != Posture.Charge) Assert.Equal(Posture.Escape, c.Posture);
            was = c.Posture;
        }
        Assert.Equal(1, windUps);
        // Struck again, it may ram again: retaliating or fleeing, never left escaping.
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Contains(c.Posture, new[] { Posture.Flee, Posture.Retreat });
    }

    [Fact]
    public void StruckWhileEscapingAfterItsRamItFleesOrTurnsBackAnewNeverStillEscaping()
    {
        // The usual answer to a ram: the hero rammed swings back at the creature recoiling off it.
        var w = CreaturesTesting.Planet(Pen).Breathing().HeroAt(3, 3);
        var c = w.Let(Tapir, 6, 3).Charging(-1);
        w.Do(new HitBy(c.Id)).Run();
        Assert.Equal(Posture.Escape, c.Posture);
        w.Run(5);
        Assert.Equal(Posture.Escape, c.Posture);
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Contains(c.Posture, new[] { Posture.Flee, Posture.Retreat });
        int longest = c.Posture == Posture.Flee ? Tapir.FleeTicks() : Tapir.Retaliation().FleeMost;
        Assert.InRange(c.Left, longest - 9, longest - 1);
        Assert.Null(c.Act);
    }

    [Fact]
    public void AStrikeDuringTheWindUpCancelsItsChargeAndKnocksItAway()
    {
        var w = CreaturesTesting.Planet(Pen).Breathing().HeroAt(10, 3);
        var c = w.Let(Tapir, 13, 3);
        Assert.True(WindsUp(w, c));
        int left = c.Left;
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.True(c.Posture is Posture.Flee or Posture.Retreat, $"{c.Posture}");
        Assert.True(c.VX > 0, "not knocked away from the striker");
        Assert.Equal(Spec.Weapons.Axe.Damage, c.Damage);
        // When its wind-up would have ended it does not charge: its cycle begins again from the blow.
        w.Run(left);
        Assert.NotEqual(Posture.Charge, c.Posture);
    }

    // The charge.

    [Fact]
    public void AChargeRunsAtItsSpeedPastTheHeroByRamPastThenEscapesOnItsWay()
    {
        var w = CreaturesTesting.Planet(Long).Breathing().HeroAt(20, 3);
        var c = w.Let(Tapir, 33, 3).Charging(-1);
        int ticks = 0;
        while (c.Posture == Posture.Charge)
        {
            int x = c.X;
            w.Run();
            ticks++;
            if (c.Posture == Posture.Charge) Assert.Equal(x - Tapir.Lunge().Speed, c.X);
        }
        Assert.True(ticks < Tapir.Lunge().Ticks, "it ran out its charge");
        Assert.Equal((Posture.Escape, -Tapir.Run()), (c.Posture, c.VX));
        // Turned to escaping the tick its middle was RamPast cells past the hero's, and on a step that tick.
        Assert.InRange(w.HeroMiddle(20) - c.X, Tapir.Lunge().Past * Unit + Tapir.Run(), Tapir.Lunge().Past * Unit + Tapir.Lunge().Speed + Tapir.Run());
    }

    [Fact]
    public void AChargeEndsAtAWallFlushWithIt()
    {
        var w = CreaturesTesting.Planet(Pen).Breathing().HeroAt(16, 3);
        var c = w.Let(Tapir, 8, 3).Charging(+1);
        int ticks = 0, most = 0;
        while (c.Posture == Posture.Charge)
        {
            w.Run();
            ticks++;
            most = Math.Max(most, c.X);
            Assert.True(Clear(w, c));
        }
        Assert.True(ticks < Tapir.Lunge().Ticks, "it ran out its charge rather than meet the wall");
        // Flush with the wall, short of the hero's middle by less than RamPast, then turned from it, escaping.
        Assert.Equal(17 * Cell - Cell, most);
        Assert.Equal((Posture.Escape, -Tapir.Run()), (c.Posture, c.VX));
    }

    [Fact]
    public void AChargeEndsAtADropNeverGoingOffTheLedge()
    {
        var w = CreaturesTesting.Planet(Ledge).Breathing().HeroAt(11, 5);
        var c = w.Let(Tapir, 2, 2).Charging(+1);
        int most = 0;
        while (c.Posture == Posture.Charge)
        {
            w.Run();
            most = Math.Max(most, c.X + Tapir.Body.Width * Unit / 2);
            Assert.Equal(3 * Cell, c.Y);
        }
        Assert.Equal((Posture.Escape, -Tapir.Run()), (c.Posture, c.VX));
        Assert.InRange(most, 7 * Cell - Tapir.Lunge().Speed, 7 * Cell);
    }

    // Hit by it.

    [Fact]
    public void AChargeThatHitsTakesItsRamFromTheSuitAndTheCreatureRecoilsFleeing()
    {
        var w = CreaturesTesting.Planet(Pen).HeroAt(3, 3).HeroAt(3, 3, peer: 1);
        var c = w.Let(Tapir, 6, 3).Charging(-1);
        Assert.Null(w.M.Creatures.HitByProblem(c.Id));
        int x = c.X;
        w.Do(new HitBy(c.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Tapir.Ram(), Suit(w, 1) - Suit(w, 0));
        // Back the way it came, hopping off the hero, its ram spent.
        Assert.Equal((Posture.Escape, Tapir.Run()), (c.Posture, c.VX));
        Assert.True(c.X > x && c.VY < 0, $"at {c.X} from {x}, rising {c.VY}");
    }

    [Fact]
    public void ACapePutOnTheTickARamLandsSparesAFifthOfIt()
    {
        // Commands apply in order: the cape moved on first, the ram after it (2026-10-27-the-cape-is-worn). The tapir's 5 less a fifth.
        var w = CreaturesTesting.Planet(Pen).HeroAt(3, 3).HeroAt(3, 3, peer: 1).GiveItems(0, (Spec.Cape.Item, 1));
        var c = w.Let(Tapir, 6, 3).Charging(-1);
        int bag = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == Spec.Cape.Item);
        int slot = Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(Spec.Cape.Slot);
        w.Do(new Items.Move(new Items.Place(bag), new Items.Target(slot), 1)).Do(new HitBy(c.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(5, Tapir.Ram());
        Assert.Equal(Tapir.Ram() - 1, Suit(w, 1) - Suit(w, 0));
        Assert.Equal(0, w.M.Crew.HeroOf(0)!.Spared);
    }

    [Fact]
    public void AChargeHitsOnceTheSecondHeroItTouchedPaysNothing()
    {
        var w = CreaturesTesting.Planet(Pen).HeroAt(3, 3).HeroAt(3, 3, peer: 1).HeroAt(3, 3, peer: 2);
        var c = w.Let(Tune.Loper, 6, 3).Charging(-1);
        w.Do(new HitBy(c.Id)).Do(new HitBy(c.Id) { From = 1 }).Run();
        w.Do(new HitBy(c.Id)).Run();
        Assert.Equal(["creatures.notAttacking", "creatures.notAttacking"], Reasons(w));
        Assert.Equal(Tune.Loper.Ram(), Suit(w, 2) - Suit(w, 0));
        Assert.Equal(Suit(w, 2), Suit(w, 1));
    }

    [Fact]
    public void AHitByACreatureNotChargingOrGoneIsRefused()
    {
        var w = CreaturesTesting.Planet(Pen).HeroAt(3, 3).HeroAt(3, 3, peer: 1);
        var c = w.Let(Tapir, 6, 3).Still();
        Assert.Equal("creatures.notAttacking", w.M.Creatures.HitByProblem(c.Id));
        Assert.Equal("creatures.gone", w.M.Creatures.HitByProblem(c.Id + 1));
        w.Do(new HitBy(c.Id)).Do(new HitBy(c.Id + 1)).Run();
        (c.Drive, c.Act) = (CreaturesTesting.Place<Tune.Retaliate>(Tapir), new Act { Move = 0, Phase = ActPhase.WindUp, Left = Tapir.Lunge().WindUp });
        w.Do(new HitBy(c.Id)).Run();
        Assert.Equal(["creatures.notAttacking", "creatures.gone", "creatures.notAttacking"], Reasons(w));
        Assert.Equal(Suit(w, 1), Suit(w, 0));
    }

    [Fact]
    public void InCreativeModeAChargeThatHitsTakesNothing()
    {
        var w = CreaturesTesting.Planet(Pen).Breathing().HeroAt(3, 3);
        var c = w.Let(Tapir, 6, 3).Charging(-1);
        w.Do(new HitBy(c.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Scenario.Full, Suit(w, 0));
        Assert.Equal(Posture.Escape, c.Posture);
    }

    [Fact]
    public void ARamThatTakesTheLastUnitPassesTheHeroOutItsBodyLeftWhereItFell()
    {
        var w = CreaturesTesting.Planet(Pen).HeroAt(3, 3).GiveItems(0, ("scrap", 4));
        var c = w.Let(Tapir, 6, 3).Charging(-1);
        w.M.Crew.HeroOf(0)!.Air = Tapir.Ram() - 1;
        w.Do(new HitBy(c.Id)).Run();
        var hero = w.M.Crew.HeroOf(0)!;
        Assert.True(hero.Out > 0);
        Assert.Equal(0, hero.Air);
        var body = Assert.Single(w.M.Items.Floor);
        Assert.Equal(("scrap", 4, 0), (body.Item, body.Count, body.Body!.Peer));
    }

    /// <summary>A field 80 wide, its floor row 4: room for a whole charge and a hero past its run.</summary>
    private static readonly string Field = string.Join('\n',
        [new string('#', 80), .. Enumerable.Repeat("#" + new string('.', 78) + "#", 3), new string('#', 80)]);

    /// <summary>A tapir at column <paramref name="x"/> of the field, its wind-up one tick from its end.</summary>
    private static Creature WindingUp(Scenario w, int x)
    {
        var c = w.Let(Tapir, x, 3).Still();
        (c.Drive, c.Act) = (CreaturesTesting.Place<Tune.Retaliate>(Tapir), new Act { Move = 0, Phase = ActPhase.WindUp, Left = 1 });
        return c;
    }

    [Fact]
    public void AWindUpEndingWithTheNearestHeroPastItsRunLetsItBeAndEscapesAwayFromThatHero()
    {
        // The nearest hero 70 cells right, past the 56 of a charge; a farther one 76 cells left: it escapes left, from the nearest.
        var w = CreaturesTesting.Planet(Field, CreaturesTesting.Lifeless).HeroAt(75, 3).HeroAt(2, 3, peer: 1);
        var c = WindingUp(w, 40);
        w.Run();
        Assert.Equal((Posture.Escape, -Tapir.Run()), (c.Posture, c.VX));
    }

    [Fact]
    public void AWindUpEndingWithTheNearestHeroWithinItsRunChargesIt()
    {
        var w = CreaturesTesting.Planet(Field, CreaturesTesting.Lifeless).HeroAt(60, 3);
        var c = WindingUp(w, 40);
        w.Run();
        Assert.Equal((Posture.Charge, Tapir.Lunge().Speed), (c.Posture, c.VX));
    }

    [Fact]
    public void TwoHeroesAsNearChargeTheFirstInIdOrderOnWhicheverSide()
    {
        foreach (var (right, left, way) in new[] { (0, 1, +1), (1, 0, -1) })
        {
            var w = CreaturesTesting.Planet(Field, CreaturesTesting.Lifeless).HeroAt(52, 3, peer: right).HeroAt(30, 3, peer: left);
            var c = WindingUp(w, 40);
            w.Run();
            Assert.Equal((Posture.Charge, way * Tapir.Lunge().Speed), (c.Posture, c.VX));
        }
    }

    [Fact]
    public void AChargeMeetingNothingEndsWhenItsTicksRunOutAndEscapes()
    {
        // The hero far ahead, past the whole run: neither a wall nor the hero ends it, only its time.
        var w = CreaturesTesting.Planet(Field, CreaturesTesting.Lifeless).HeroAt(78, 3);
        var c = w.Let(Tapir, 6, 3).Charging(+1);
        w.Run(Tapir.Lunge().Ticks - 1);
        Assert.Equal(Posture.Charge, c.Posture);
        w.Run();
        Assert.Equal((Posture.Escape, -Tapir.Run()), (c.Posture, c.VX));
    }

    [Fact]
    public void AHitByACreatureKilledEarlierInTheTickIsRefusedGoneAndCostsNothing()
    {
        // One hero lands the killing blow as another's client reports its ram, on one tick.
        var w = CreaturesTesting.Planet(Pen).HeroAt(3, 3).HeroAt(3, 3, peer: 1);
        var c = w.Let(Tapir, 6, 3).Charging(-1);
        c.Damage = Tapir.Life.Hp - 1;
        int suit = Suit(w, 1);
        w.Do(new Strike(c.Id, Axe)).Do(new HitBy(c.Id) { From = 1 }).Run();
        Assert.Empty(w.M.Creatures.Creatures);
        Assert.Equal(["creatures.gone"], Reasons(w));
        Assert.Equal(suit, Suit(w, 1));
    }
}
