using Stillwake.Sim.Creatures;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The caves' lurkers (cave-hostiles): the capback and the geode snail guard the ground they stand on, noticing an awake hero in
/// line with them, striking one within reach, answering a blow; and they spawn by the depth band of the hero they spawn round.
/// </summary>
public class CaveHostilesTests
{
    private static readonly string Axe = Spec.Weapons.Axe.Item;
    private static readonly Tune.Species Capback = Tune.Capback, Snail = Tune.GeodeSnail;
    private static readonly Tune.Guard CapGuard = (Tune.Guard)Capback.Brain[0], SnailGuard = (Tune.Guard)Snail.Brain[0];
    private static readonly Tune.Pounce CapPounce = (Tune.Pounce)Capback.Moves[0];
    private static readonly Tune.Lunge Roll = (Tune.Lunge)Snail.Moves[0];

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
        Assert.Equal(6 * CreaturesModule.Unit, c.X);
        return (w, c);
    }

    private static int Suit(Scenario w, int peer) => w.M.Crew.HeroOf(peer)!.Air;

    [Fact]
    public void ACapbackLurksStillTillAnAwakeHeroComesWithinItsNotice()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(20, 3).Run(3 * CapGuard.AlertTicks);
        Assert.Equal((6 * CreaturesModule.Unit, 0), (c.X, c.VX));
        Assert.False(CreaturesModule.Alerted(c));
        Assert.Null(c.Act);
        // 14 cells off: its notice, just.
        w.HeroAt(10, 3).Run();
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
    public void AlertedItPouncesAtAHeroWithinItsStrikeAndWaitsForOneFarther()
    {
        var (w, c) = Lurker(Capback);
        // 12 cells off: noticed, past its strike.
        w.HeroAt(9, 3).Run(3 * CapGuard.AlertTicks);
        Assert.True(CreaturesModule.Alerted(c));
        Assert.Null(c.Act);
        // 10 cells off: within it.
        w.HeroAt(8, 3).Run(2);
        Assert.NotNull(c.Act);
        w.Run(CapPounce.WindUp);
        Assert.Equal(ActPhase.Active, c.Act!.Phase);
        Assert.True(c.VX > 0);
    }

    [Fact]
    public void ALurkerNeverNoticesAHeroPassedOut()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(6, 3).Run();
        w.M.Crew.HeroOf(0)!.Out = 1000;
        w.Run(3 * CapGuard.AlertTicks);
        Assert.Null(c.Act);
        Assert.False(CreaturesModule.Alerted(c));
    }

    [Fact]
    public void StruckItAnswersTheStrikerAtOnceWhereverTheyStand()
    {
        var (w, c) = Lurker(Capback);
        w.HeroAt(25, 3).Run();
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal(ActPhase.WindUp, c.Act!.Phase);
        Assert.Equal(Ref.Hero(0), c.Target);
    }

    [Fact]
    public void ACapbacksPounceCostsFourOfTheSuitTheSnailsRollSix()
    {
        foreach (var (s, damage, windUp) in new[] { (Capback, 4, CapPounce.WindUp), (Snail, 6, Roll.WindUp) })
        {
            var (w, c) = Lurker(s);
            w.HeroAt(7, 3).HeroAt(7, 3, peer: 1).Do(new Items.SetCreative(false)).Do(new Items.SetCreative(false) { From = 1 });
            w.Do(new Strike(c.Id, Axe)).Run(windUp + 1);
            Assert.Equal(ActPhase.Active, c.Act!.Phase);
            w.Do(new HitBy(c.Id)).Run();
            Assert.Equal(damage, s.Moves[0].Damage);
            Assert.Equal(Suit(w, 1) - damage, Suit(w, 0));
            Assert.Empty(w.Refusals);
        }
    }

    [Fact]
    public void ABlowCutsNeitherTheSnailsCrouchNorItsRoll()
    {
        var (w, c) = Lurker(Snail);
        w.HeroAt(14, 3).Run();
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal(ActPhase.WindUp, c.Act!.Phase);
        int left = c.Act.Left;
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal((ActPhase.WindUp, left - 1), (c.Act!.Phase, c.Act.Left));
        w.Run(left);
        Assert.Equal((ActPhase.Active, Roll.Speed), (c.Act!.Phase, c.VX));
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Equal((ActPhase.Active, Roll.Speed), (c.Act!.Phase, c.VX));
    }

    [Fact]
    public void ALopersRamIsStillCutByABlow()
    {
        var w = CreaturesTesting.Planet(Floor, CreaturesTesting.Lifeless).HeroAt(14, 3);
        var c = w.Loper(2, 3).Charging(1);
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.True(c.Act == null || c.Act.Phase != ActPhase.Active || c.VX != Tune.Loper.Lunge().Speed);
    }

    [Fact]
    public void TheSnailRollsAtTheAwakeHeroNotTheOnePassedOutThatStruckIt()
    {
        var (w, c) = Lurker(Snail);
        // The striker passed out at its left, an awake hero far at its right.
        w.HeroAt(1, 3).HeroAt(20, 3, peer: 1).Run();
        w.M.Crew.HeroOf(0)!.Out = 1000;
        w.Do(new Strike(c.Id, Axe)).Run(Roll.WindUp + 1);
        Assert.Equal((ActPhase.Active, Roll.Speed), (c.Act!.Phase, c.VX));
    }

    [Fact]
    public void WithNoHeroAwakeTheSnailsRollIsLetBe()
    {
        var (w, c) = Lurker(Snail);
        w.HeroAt(10, 3).Run();
        w.M.Crew.HeroOf(0)!.Out = 1000;
        w.Do(new Strike(c.Id, Axe)).Run(Roll.WindUp + 1);
        Assert.Null(c.Act);
        Assert.Equal(0, c.VX);
    }

    [Fact]
    public void TheSnailsRollStopsAtAWall()
    {
        var (w, c) = Lurker(Snail);
        // Past the room's left wall: it rolls at the hero and the wall ends it long before its time.
        w.HeroAt(1, 3).Run();
        w.Do(new Strike(c.Id, Axe)).Run(Roll.WindUp + 1);
        Assert.Equal(-Roll.Speed, c.VX);
        w.Run(10);
        Assert.Null(c.Act);
        Assert.True(c.X >= 2 * CreaturesModule.Unit + Snail.Body.Width * CreaturesModule.Unit / 2);
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
