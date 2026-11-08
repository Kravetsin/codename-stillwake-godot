using Stillwake.Sim.Creatures;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The caves' lurkers (cave-hostiles, lurker-attacks): the capback vents gas at a hero about to step on it and, provoked, spits slime
/// at one gone far; the geode snail opens only for a hero close by, bites, and takes harm only open and only from melee. Both notice
/// an awake hero in line with them, and they spawn by the depth band of the hero they spawn round.
/// </summary>
public class CaveHostilesTests
{
    private const int Unit = CreaturesModule.Unit;
    private static readonly string Axe = Spec.Weapons.Axe.Item;
    private static readonly Tune.Species Capback = Tune.Capback, Snail = Tune.GeodeSnail;
    private static readonly Tune.Guard CapGuard = (Tune.Guard)Capback.Brain[0], SnailGuard = (Tune.Guard)Snail.Brain[0];
    private static readonly Tune.Vent Vent = (Tune.Vent)Capback.Moves[0];
    private static readonly Tune.Shoot Spit = (Tune.Shoot)Capback.Moves[1];
    private static readonly Tune.Shell Shell = Snail.Brain.OfType<Tune.Shell>().Single();
    private static readonly Tune.Lunge Bite = (Tune.Lunge)Snail.Moves[0];

    /// <summary>A long cave floor, its floor row 3, and the same floor with a rock two cells high in it at column 6.</summary>
    private const string Floor = """
        ##############################
        #............................#
        #............................#
        #............................#
        ##############################
        """;

    private const string Rock = """
        ##############################
        #............................#
        #............................#
        #.....#......................#
        ##############################
        """;

    /// <summary>
    /// A lurker of <paramref name="s"/> let out at column 2 of <paramref name="map"/>: its middle at cell 6. A hero at column x
    /// is reported in cell 2x, so it is 2x − 6 cells off.
    /// </summary>
    private static (Scenario W, Creature C) Lurker(Tune.Species s, string map = Floor)
    {
        // Far off at first: a release needs a hero on the surface.
        var w = CreaturesTesting.Planet(map, CreaturesTesting.Lifeless).HeroAt(27, 3);
        var c = w.Let(s, 2, 3);
        Assert.Equal(6 * Unit, c.X);
        return (w, c);
    }

    /// <summary>Heroes 0 and 1 out of creative mode, their suits counting.</summary>
    private static Scenario Suits(Scenario w) => w.Do(new Items.SetCreative(false)).Do(new Items.SetCreative(false) { From = 1 });

    private static int Suit(Scenario w, int peer = 0) => w.M.Crew.HeroOf(peer)!.Air;

    private static bool Provoked(Scenario w, Creature c) => GuardDrive.Provoked(w.M.Creatures, c);

    /// <summary>The first arc struck within <paramref name="ticks"/>, run tick by tick: an arc lingers a tick only.</summary>
    private static Arc? FirstArc(Scenario w, int ticks)
    {
        for (int t = 0; t < ticks; t++)
            if (w.Run().M.Creatures.Arcs.SingleOrDefault() is { } arc) return arc;
        return null;
    }

    /// <summary>The ticks run till <paramref name="done"/> holds, at most a thousand.</summary>
    private static int Until(Scenario w, Func<bool> done)
    {
        int ticks = 0;
        for (; !done() && ticks < 1000; ticks++) w.Run();
        return ticks;
    }

    [Fact]
    public void ACapbackLurksStillTillAnAwakeHeroComesWithinItsNotice()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(14, 3).Run(3 * CapGuard.AlertTicks);
        Assert.Equal((6 * Unit, 0), (c.X, c.VX));
        Assert.False(CreaturesModule.Alerted(c));
        // 20 cells off: its notice, just.
        w.HeroAt(13, 3).Run();
        Assert.True(CreaturesModule.Alerted(c));
    }

    [Fact]
    public void RockBetweenHidesTheHeroFromALurker()
    {
        var (w, c) = Lurker(Capback, Rock);
        w.HeroAt(7, 3).Run(3 * CapGuard.AlertTicks);
        Assert.False(CreaturesModule.Alerted(c));
        Assert.Null(c.Act);
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void ACapbackNeitherVentsNorSpitsAtAHeroItNoticesTillTheHeroAlmostStepsOnIt()
    {
        var (w, c) = Lurker(Capback);
        // 12 cells off, then 6: noticed, unprovoked, past its vent.
        w.HeroAt(9, 3).Run(3 * CapGuard.AlertTicks);
        w.HeroAt(6, 3).Run(3 * CapGuard.AlertTicks);
        Assert.True(CreaturesModule.Alerted(c));
        Assert.Empty(w.M.Creatures.Patches);
        Assert.Empty(w.M.Creatures.Arcs);
        // 4 cells off: it vents that tick, no wind-up, its gas round its middle.
        w.HeroAt(5, 3).Run();
        Assert.Equal((0, ActPhase.Active), (c.Act!.Move, c.Act.Phase));
        var gas = w.M.Creatures.Patches.Single();
        Assert.Equal((Tune.Gas.Id, Tune.Gas.Width, Tune.Gas.Height), (gas.Kind, gas.Width, gas.Height));
        int middle = CreaturesModule.Down(c.Y - Capback.Body.Height * Unit / 2, Unit);
        Assert.Equal((6 - (Tune.Gas.Width - 1) / 2, middle - (Tune.Gas.Height - 1) / 2), (gas.Left, gas.Top));
        Assert.True(Provoked(w, c));
    }

    [Fact]
    public void ItVentsNoMoreTillItsCooldownHasPassedAndItsGasThinsAway()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(5, 3).Run(CapGuard.AlertTicks + 1);
        Assert.Single(w.M.Creatures.Patches);
        int vented = c.Recall(VentMove.Vented) - 1;
        // The tick it vents is one of its ticks venting, and of its gas's.
        Assert.Equal(Vent.Ticks - 1, Until(w, () => c.Act == null));
        Assert.Equal(Tune.Gas.Lasts - Vent.Ticks, Until(w, () => w.M.Creatures.Patches.Count == 0));
        w.Run(Vent.Cooldown - 1 - (w.M.Creatures.Clock - vented));
        Assert.Empty(w.M.Creatures.Patches);
        w.Run();
        Assert.Single(w.M.Creatures.Patches);
        Assert.Equal(Vent.Cooldown, w.M.Creatures.Clock - vented);
    }

    [Fact]
    public void AHeroInTheGasPaysItEveryBreathAndNothingOutOfIt()
    {
        var (w, _) = Lurker(Capback);
        Suits(w.HeroAt(5, 3).HeroAt(27, 3, peer: 1)).Run(CapGuard.AlertTicks + 1);
        Assert.Single(w.M.Creatures.Patches);
        int start = Suit(w) - Suit(w, 1);
        w.Run(Spec.Crew.BreathEvery * 3);
        Assert.Equal(3 * Tune.Gas.Damage, start - (Suit(w) - Suit(w, 1)));
        // Past its edge and out of its notice, so out of its spit's reach: nothing more than a suit's own breath.
        w.HeroAt(14, 3).Run(Spec.Crew.BreathEvery);
        int outside = Suit(w) - Suit(w, 1);
        w.Run(Spec.Crew.BreathEvery * 2);
        Assert.Equal(outside, Suit(w) - Suit(w, 1));
    }

    [Fact]
    public void GasDoesNotPassRock()
    {
        foreach (var (map, pays) in new[] { (Floor, true), (Rock, false) })
        {
            var w = Suits(CreaturesTesting.Planet(map, CreaturesTesting.Lifeless).HeroAt(5, 3).HeroAt(27, 3, peer: 1)).Run();
            var (hx, hy) = w.M.Creatures.HeroAt(0)!.Value;
            // Its middle right of the rock, low; the hero left of it, in the box all the same.
            w.M.Creatures.LayPatch(Tune.Gas.Id, 14, 7, floor: false);
            Assert.True(w.M.Creatures.Patches.Single().Holds(hx, hy));
            int start = Suit(w) - Suit(w, 1);
            w.Run(Spec.Crew.BreathEvery * 2);
            Assert.Equal(pays ? 2 * Tune.Gas.Damage : 0, start - (Suit(w) - Suit(w, 1)));
        }
    }

    [Fact]
    public void UnprovokedItNeverSpitsThoughAHeroStandsWithinItsReach()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(9, 3).Run(400);
        Assert.Empty(w.M.Creatures.Arcs);
        Assert.Empty(w.M.Creatures.Patches);
        Assert.Null(c.Act);
    }

    [Fact]
    public void ProvokedItSpitsAtAHeroGoneFarAndItsSlimeLiesOnTheFloorUnderWhereItLanded()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(5, 3).Run(CapGuard.AlertTicks + 1);
        // Gone 12 cells off: past the spit's least, within its reach.
        var arc = FirstArc(w.HeroAt(9, 3), Vent.Ticks + CapGuard.AlertTicks + Spit.WindUp + 4);
        Assert.NotNull(arc);
        Assert.Equal(Spit.Projectile, arc.Projectile);
        var slime = w.M.Creatures.Patches.Single(p => p.Kind == Tune.Slime.Id);
        Assert.Equal((Tune.Slime.Width, Tune.Slime.Height), (slime.Width, slime.Height));
        // On the floor: its bottom row the last open one, row 7 of the doubled map, under where the glob landed.
        Assert.Equal(7, slime.Top + slime.Height - 1);
        Assert.True(slime.Holds(CreaturesModule.Down(arc.X1, Unit), 7));
    }

    [Fact]
    public void ItSpitsAtNoHeroNearerThanItsLeast()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(5, 3).Run(CapGuard.AlertTicks + 1);
        // 6 cells off: past its vent, nearer than its spit's least.
        Assert.Null(FirstArc(w.HeroAt(6, 3), Vent.Ticks + 3 * CapGuard.AlertTicks + Spit.WindUp));
    }

    [Fact]
    public void StruckFromAfarItIsProvokedAndSpitsAtTheStriker()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(10, 3).Run();
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.True(Provoked(w, c));
        // Knocked back and down, alerted, its wind-up: then its glob.
        Assert.Equal(Spit.Projectile, FirstArc(w, 4 * CapGuard.AlertTicks + Spit.WindUp)?.Projectile);
    }

    [Fact]
    public void ItsProvocationPasses()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(10, 3).Run();
        w.Do(new Strike(c.Id, Axe)).Run();
        // Gone out of its notice till its provocation is spent, then back within its spit's reach.
        w.HeroAt(27, 3).Run(CapGuard.Provokes);
        Assert.False(Provoked(w, c));
        Assert.Null(FirstArc(w.HeroAt(10, 3), 200));
    }

    [Fact]
    public void ABlowCutsNoVent()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(5, 3).Run(CapGuard.AlertTicks + 1);
        int left = c.Act!.Left;
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal((0, left - 1), (c.Act!.Move, c.Act.Left));
    }

    [Fact]
    public void ALurkerNeverNoticesAHeroPassedOut()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(5, 3).Run();
        w.M.Crew.HeroOf(0)!.Out = 1000;
        w.Run(3 * CapGuard.AlertTicks);
        Assert.Null(c.Act);
        Assert.Empty(w.M.Creatures.Patches);
        Assert.False(CreaturesModule.Alerted(c));
    }

    [Fact]
    public void AShutSnailTakesNoHarmAndAnswersNoBlow()
    {
        var (w, c) = Lurker(Snail);
        w.HeroAt(9, 3).Run();
        Assert.True(CreaturesModule.Shut(c));
        w.Do(new Strike(c.Id, Axe)).Run(3);
        Assert.Equal(0, c.Damage);
        Assert.Null(c.Act);
        Assert.Equal(Ref.None, c.Target);
    }

    [Fact]
    public void ItOpensForAHeroCloseByAndShutsItsHoldAfterTheyGo()
    {
        var (w, c) = Lurker(Snail);
        // 4 cells off: its reach.
        w.HeroAt(5, 3).Run();
        Assert.False(CreaturesModule.Shut(c));
        w.HeroAt(20, 3).Run(Shell.Hold - 1);
        Assert.False(CreaturesModule.Shut(c));
        w.Run();
        Assert.True(CreaturesModule.Shut(c));
    }

    [Fact]
    public void OpenTheSnailIsHurtByABlowAndNotByAShot()
    {
        var (w, c) = Lurker(Snail);
        w.HeroAt(5, 3).GiveItems(0, (Spec.Shots.Nailer.Item, 1), ("nail", 20)).Run();
        Assert.False(CreaturesModule.Shut(c));
        // From its left, along +x through it.
        w.Do(new Fire(Spec.Shots.Nailer.Item, 3 * Unit, c.Y - Unit, 0)).Run(6);
        Assert.Empty(w.Refusals);
        Assert.Equal(0, c.Damage);
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.True(c.Damage > 0);
    }

    [Fact]
    public void OpenItBitesAHeroWithinItsStrikeAShortSteadyLunge()
    {
        var (w, c) = Lurker(Snail);
        Suits(w.HeroAt(5, 3).HeroAt(27, 3, peer: 1)).Run(SnailGuard.AlertTicks + 1);
        Assert.Equal(ActPhase.WindUp, c.Act!.Phase);
        int left = c.Act.Left;
        // Its crouch is not cut by a blow.
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal((ActPhase.WindUp, left - 1), (c.Act!.Phase, c.Act.Left));
        w.Run(left - 1);
        Assert.Equal((ActPhase.Active, Bite.Speed), (c.Act!.Phase, c.VX));
        int start = Suit(w) - Suit(w, 1);
        w.Do(new HitBy(c.Id)).Run();
        Assert.Equal(Bite.Damage, start - (Suit(w) - Suit(w, 1)));
        w.Run(Bite.Ticks + 2);
        Assert.Null(c.Act);
        Assert.True(c.X - 6 * Unit <= Bite.Ticks * Bite.Speed);
    }

    [Fact]
    public void ALopersRamIsStillCutByABlow()
    {
        var w = CreaturesTesting.Planet(Floor, CreaturesTesting.Lifeless).HeroAt(14, 3);
        var c = w.Loper(2, 3).Charging(1);
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.True(c.Act == null || c.Act.Phase != ActPhase.Active || c.VX != Tune.Loper.Lunge().Speed);
    }

    /// <summary>The first planet's kinds, every row of a test's map in band <paramref name="band"/>.</summary>
    private static Spec.PlanetType InBand(int band) => Spec.PlanetTypes.First with
    {
        Bands = [.. Spec.PlanetTypes.First.Bands.Select((bottom, i) => i < band ? -1000 - 1000 * (band - i) : bottom)],
    };

    private static Layout Wide(int width, int height, char open) =>
        Layout.Parse([.. Enumerable.Range(0, height).Select(y => new string(y == height - 1 ? '#' : open, width))]);

    /// <summary>The kinds spawned round three heroes on <paramref name="map"/> of <paramref name="type"/>, in id order.</summary>
    private static string[] Spawned(Layout map, Spec.PlanetType type)
    {
        var w = CreaturesTesting.Planet(map, type).Breathing();
        for (int h = 0; h < 3; h++) w.HeroAt(100 + 200 * h, 10, peer: h);
        for (int t = 0; t < 60 * Tune.Beat; t++) w.Run();
        return [.. w.M.Creatures.Creatures.Select(c => c.Species).Distinct().Order(StringComparer.Ordinal)];
    }

    [Fact]
    public void AHeroInTheGardenHasCapbacksSpawnedRoundItInTheDeepSnailsAndNoGrazersBelowTheSky()
    {
        Assert.Equal(1, Terrain.BandOf(InBand(1), 20));
        Assert.Equal(2, Terrain.BandOf(InBand(2), 20));
        Assert.Equal([Capback.Id], Spawned(Wide(700, 12, '.'), InBand(1)));
        Assert.Equal([Snail.Id], Spawned(Wide(700, 12, '.'), InBand(2)));
        // Open ground under the sky, but a band down: the surface's grazers are its band's, 0, so none.
        Assert.Empty(Spawned(Wide(700, 12, ' '), InBand(1)));
    }
}
