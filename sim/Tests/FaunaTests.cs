using Stillwake.Sim.Fauna;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;
using Tune = Stillwake.Content.Fauna;

namespace Stillwake.Sim.Tests;

/// <summary>Creatures (0102): how a loper walks, takes hits and dies, and where and when creatures spawn and go.</summary>
public class FaunaTests
{
    private const int Unit = FaunaModule.Unit;
    private static readonly string Axe = Spec.Weapons.Axe.Item, Drill = Spec.Weapons.Drill.Item;
    private static readonly Tune.Species Loper = Tune.Loper;
    private static readonly int Belt = Spec.Inventory.Slots + 2;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>A room 12 wide, its floor row 4: the side walls taller than a step.</summary>
    private const string Room = """
        ############
        #..........#
        #..........#
        #..........#
        ############
        """;

    /// <summary>A room whose floor is a ledge three rows over the floor at its right: too deep a drop to walk off.</summary>
    private const string Ledge = """
        ##############
        #............#
        #............#
        #######......#
        #######......#
        #######......#
        ##############
        """;

    /// <summary>The same ledge two rows over the floor: walked off.</summary>
    private const string Low = """
        ##############
        #............#
        #............#
        #######......#
        #######......#
        ##############
        """;

    /// <summary>A floor with a step one cell high at column 7, then the room's side wall.</summary>
    private const string Step = """
        ##############
        #............#
        #............#
        #............#
        #......#######
        ##############
        """;

    private static Creature Only(Scenario w) => Assert.Single(w.M.Fauna.Creatures);

    /// <summary>Whether <paramref name="c"/>'s body is clear of every wall, in the grid.</summary>
    private static bool Clear(Scenario w, Creature c) => !w.M.Fauna.Overlaps(Tune.Get(c.Species), c.X, c.Y);

    // Walking.

    [Fact]
    public void ALoperLetOutFallsToTheFloorAndStandsOnIt()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        var c = w.Loper(3, 2).Still();
        w.Run(30);
        Assert.Equal((4 * Unit, 0), (c.Y, c.VY));
        Assert.Equal(3 * Unit + Unit, c.X);
    }

    [Fact]
    public void AWalkerTurnsBackAtAWallItCannotStepUpNeverEnteringIt()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        var c = w.Loper(2, 3).Walking(+1);
        int most = 0;
        bool turned = false;
        for (int t = 0; t < 200; t++)
        {
            w.Run();
            most = Math.Max(most, c.X);
            turned |= c.VX < 0;
            Assert.True(Clear(w, c));
        }
        Assert.True(turned);
        // Flush with the wall at column 11: its right edge at 11 cells.
        Assert.Equal(11 * Unit - Unit, most);
        Assert.Equal(4 * Unit, c.Y);
    }

    [Fact]
    public void AWalkerTurnsBackFromADropOfThreeRowsAtTheLedgesEdge()
    {
        var w = FaunaTesting.Planet(Ledge).HeroAt(8, 5);
        var c = w.Loper(2, 2).Walking(+1);
        int most = 0;
        for (int t = 0; t < 200; t++)
        {
            w.Run();
            most = Math.Max(most, c.X + Loper.Width * Unit / 2);
            Assert.Equal(3 * Unit, c.Y);
        }
        // Its leading edge reaches the ledge's edge at column 7 and goes no farther.
        Assert.True(most <= 7 * Unit, $"its edge reached {most}");
        Assert.True(most > 7 * Unit - Loper.Wander - 1, $"it turned early, at {most}");
    }

    [Fact]
    public void AWalkerWalksOffADropOfTwoRowsAndLandsBelow()
    {
        var w = FaunaTesting.Planet(Low).HeroAt(8, 4);
        var c = w.Loper(2, 2).Walking(+1);
        w.Run(120);
        Assert.Equal(5 * Unit, c.Y);
        Assert.True(c.X > 7 * Unit);
        Assert.True(Clear(w, c));
    }

    [Fact]
    public void AWalkerStepsUpAStepOneCellHighOnItsWay()
    {
        var w = FaunaTesting.Planet(Step).HeroAt(1, 4);
        var c = w.Loper(2, 4).Walking(+1);
        bool up = false;
        for (int t = 0; t < 120; t++)
        {
            w.Run();
            up |= c.Y == 4 * Unit && c.X > 7 * Unit + Unit;
            Assert.True(Clear(w, c));
        }
        Assert.True(up, "it never stood on the step");
    }

    [Fact]
    public void AWalkerStepsDownAStepOneCellDeepWithoutFalling()
    {
        var w = FaunaTesting.Planet("""
            ##############
            #............#
            #............#
            #............#
            #######......#
            ##############
            """).HeroAt(8, 4);
        var c = w.Loper(2, 3).Walking(+1);
        bool down = false;
        for (int t = 0; t < 120; t++)
        {
            w.Run();
            Assert.Equal(0, c.VY);
            down |= c.Y == 5 * Unit;
        }
        Assert.True(down, "it never stood below the step");
    }

    [Fact]
    public void AnIdleLoperTakesToWanderingAndBackOnItsOwnDrawingItsWay()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        var c = w.Loper(4, 3);
        var modes = new HashSet<Mode>();
        for (int t = 0; t < 600; t++)
        {
            w.Run();
            modes.Add(c.Mode);
            Assert.True(c.Mode != Mode.Wander || Math.Abs(c.VX) == Loper.Wander || c.VY != 0);
        }
        Assert.Equal([Mode.Idle, Mode.Wander], modes.Order());
    }

    // Hits.

    [Fact]
    public void AHitKnocksALoperAwayFromTheStrikerWithAHopThenItFleesAwayAndCalmsDown()
    {
        foreach (var (hero, away) in new[] { (1, +1), (10, -1) })
        {
            var w = FaunaTesting.Planet(Room).HeroAt(hero, 3);
            var c = w.Loper(5, 3).Still();
            w.Do(new Strike(c.Id, Axe)).Run();
            Assert.Equal(Mode.Hurt, c.Mode);
            Assert.Equal(away, Math.Sign(c.VX));
            Assert.True(c.VY < 0, "no hop");
            w.Run(Loper.HurtTicks);
            Assert.Equal(Mode.Flee, c.Mode);
            Assert.Equal(away * Loper.Flee, c.VX);
            w.Run(Loper.FleeTicks + 1);
            Assert.Equal(Mode.Idle, c.Mode);
            Assert.Equal(0, c.VX);
            Assert.Equal(Spec.Weapons.Axe.Damage, c.Damage);
        }
    }

    [Fact]
    public void TheDrillsQuickHitsEachKnockItsPacesShareOfTheAxesAndNoHopWhileRising()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3).Do(new SetCreative(true)).Do(new Run(Drill, true)).GiveItems(0, (Drill, 1));
        var c = w.Loper(5, 3).Still();
        w.Do(new Strike(c.Id, Drill)).Run();
        int share = Spec.Weapons.Drill.Pace * 1000 / Tune.KnockPace;
        Assert.Equal(Loper.Knock * share / 1000, c.VX);
        int rising = c.VY;
        Assert.True(rising < 0);
        // Struck again while it rises: knocked as before, its rise not renewed.
        w.Do(new Strike(c.Id, Drill)).Run();
        Assert.Equal(rising + Content.PlanetTypes.First.Fall(Tune.Gravity), c.VY);
    }

    [Fact]
    public void AStruckLoperFleesAwayFromTheNearestHeroNotTheStriker()
    {
        // Struck by hero 1 on its right, it is knocked left; it bolts from hero 0 beside it on the left, the nearer.
        var w = FaunaTesting.Planet(Room).HeroAt(10, 3, peer: 1).HeroAt(4, 3);
        var c = w.Loper(6, 3).Still();
        w.Do(new Strike(c.Id, Axe) { From = 1 }).Run();
        Assert.True(c.VX < 0);
        w.Run(Loper.HurtTicks);
        Assert.Equal(Loper.Flee, c.VX);
    }

    [Fact]
    public void SixHitsOfTheAxeKillALoperAndItsYieldsFallWhereItStood()
    {
        int hits = (Loper.Hp + Spec.Weapons.Axe.Damage - 1) / Spec.Weapons.Axe.Damage;
        Assert.Equal(6, hits);
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        var c = w.Loper(5, 3).Still();
        for (int i = 1; i < hits; i++)
        {
            w.Do(new Strike(c.Id, Axe)).Run(3);
            Assert.Equal(i * Spec.Weapons.Axe.Damage, c.Damage);
            c.Still();
            (c.X, c.Y, c.VY) = (6 * Unit, 4 * Unit, 0);
        }
        Assert.Equal(c, Only(w));
        w.Do(new Strike(c.Id, Axe)).Run();
        Assert.Empty(w.M.Fauna.Creatures);
        Assert.Empty(w.Refusals);
        w.Run(30);
        var biomass = w.M.Items.Floor.Where(s => s.Item == "biomass").ToList();
        Assert.InRange(biomass.Sum(s => s.Count), 2, 3);
        Assert.All(w.M.Items.Floor, s =>
        {
            Assert.Equal(FloorStack.Nobody, s.By);
            Assert.Equal(4 * Unit, s.Y);
            Assert.Equal(6 * Unit + Unit / 2, s.X);
        });
    }

    [Fact]
    public void ALoperGivesASeedNowAndThen()
    {
        const int kills = 40;
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        for (int k = 0; k < kills; k++)
        {
            var c = w.Loper(5, 3).Still();
            c.Damage = Loper.Hp - 1;
            w.Do(new Strike(c.Id, Axe)).Run();
        }
        int seeds = w.M.Items.Floor.Where(s => s.Item == "seed").Sum(s => s.Count);
        // One in four on average: never all, never none in forty.
        Assert.InRange(seeds, 1, kills - 1);
    }

    [Fact]
    public void TheDrillHitsAsItsToolStatsSayPlainOrFittedWhileItRuns()
    {
        foreach (var (drill, damage) in new[] { (new Stack(Drill, 1), 1), (new Stack(Drill, 1, Module: "arcModule"), 2) })
        {
            var w = FaunaTesting.Planet(Room).HeroAt(1, 3).Put(0, drill).Do(new SetCreative(true)).Do(new Run(Drill, true));
            var c = w.Loper(5, 3).Still();
            Assert.Equal(damage, w.M.Items.Damage(0, Drill));
            w.Do(new Strike(c.Id, Drill)).Run();
            Assert.Equal(damage, c.Damage);
            int hits = 1;
            while (w.M.Fauna.Creatures.Count > 0)
            {
                w.Do(new Strike(c.Id, Drill)).Run();
                hits++;
            }
            Assert.Equal((Loper.Hp + damage - 1) / damage, hits);
            Assert.Empty(w.Refusals);
        }
    }

    [Fact]
    public void TheSecondOfTwoKillingBlowsOnOneTickIsRefusedGone()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3).HeroAt(10, 3, peer: 1);
        var c = w.Loper(5, 3).Still();
        c.Damage = Loper.Hp - 2;
        w.Do(new Strike(c.Id, Axe)).Do(new Strike(c.Id, Axe) { From = 1 }).Run();
        Assert.Empty(w.M.Fauna.Creatures);
        Assert.Equal(["fauna.gone"], Reasons(w));
    }

    [Fact]
    public void AStrikeIsRefusedForACreatureGoneAndAnElectricToolFlatOrNotRunning()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3).GiveItems(0, (Drill, 1));
        var c = w.Loper(5, 3).Still();
        Assert.Equal("fauna.gone", w.M.Fauna.StrikeProblem(0, c.Id + 1, Axe));
        Assert.Equal("items.flat", w.M.Fauna.StrikeProblem(0, c.Id, Drill));
        w.Do(new Strike(c.Id + 1, Axe)).Do(new Strike(c.Id, Drill)).Run();
        Assert.Equal(["fauna.gone", "items.flat"], Reasons(w));
        // Charged but not running: the client sends its run first, so a hit without one is refused as it applies.
        w.Put(Belt, new Stack("battery", 1, Spec.Charge.Battery));
        Assert.Null(w.M.Fauna.StrikeProblem(0, c.Id, Drill));
        w.Refusals.Clear();
        w.Do(new Strike(c.Id, Drill)).Run();
        Assert.Equal(["items.notRunning"], Reasons(w));
        Assert.Equal(0, c.Damage);
        Assert.Throws<InvalidOperationException>(() => w.M.Fauna.StrikeProblem(0, c.Id, "iron"));
    }

    // Releases.

    [Fact]
    public void AReleaseIsRefusedAboardIntoAWallOffTheGridAndAtTheCap()
    {
        var ship = new Scenario(Room).HeroAt(1, 3);
        ship.Do(new Release(Loper.Id, ship.At(3, 3))).Run();
        Assert.Equal(["fauna.notHere"], Reasons(ship));
        Assert.Empty(ship.M.Fauna.Creatures);

        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        // In the side wall, its body a cell into the wall on the right, its head in the ceiling, off the grid.
        foreach (int cell in new[] { w.At(0, 3), w.At(10, 3), w.At(4, 1), -1, 12 * 5 })
            w.Do(new Release(Loper.Id, cell));
        w.Run();
        Assert.Equal(Enumerable.Repeat("fauna.blocked", 5), Reasons(w));
        w.Refusals.Clear();
        for (int i = 0; i < Tune.Max + 1; i++) w.Do(new Release(Loper.Id, w.At(1 + i % 9, 3)));
        w.Run();
        Assert.Equal(Tune.Max, w.M.Fauna.Creatures.Count);
        Assert.Equal(["fauna.full"], Reasons(w));
    }

    // Going.

    [Fact]
    public void CreaturesGoWithTheLastHeroAndAtOnceOutOfEveryHardBox()
    {
        var w = FaunaTesting.Planet(Wide(260, 8), FaunaTesting.Lifeless).HeroAt(10, 6);
        w.Loper(12, 6).Still();
        w.Do(new Stillwake.Sim.Crew.Leave()).Run();
        Assert.Empty(w.M.Fauna.Creatures);

        w.HeroAt(10, 6).Run();
        w.Loper(12, 6).Still();
        // Past the hard box across, 111 columns from the cell under its middle.
        w.HeroAt(13 + Tune.Hard.X + 1, 6).Run();
        Assert.Empty(w.M.Fauna.Creatures);
    }

    [Fact]
    public void ACreatureOutOfEveryKeepBoxButInAHardOneLingersThenGoes()
    {
        var w = FaunaTesting.Planet(Wide(260, 8), FaunaTesting.Lifeless).HeroAt(10, 6);
        var c = w.Loper(12, 6).Still();
        w.HeroAt(13 + Tune.Keep.X + 1, 6).Run();
        w.Run(Tune.Linger - 2);
        Assert.Same(c, Only(w));
        w.Run();
        Assert.Empty(w.M.Fauna.Creatures);
    }

    // Spawning.

    /// <summary>Open ground <paramref name="width"/> wide, its floor the bottom row: under the sky, or with <paramref name="open"/> a dot, back walls.</summary>
    private static Layout Wide(int width, int height, char open = ' ') =>
        Layout.Parse([.. Enumerable.Range(0, height).Select(y => new string(y == height - 1 ? '#' : open, width))]);

    [Fact]
    public void LopersSpawnOnTheBeatOnOpenGroundOutOfEveryHerosViewAndInTheKeepBox()
    {
        var w = FaunaTesting.Planet(Wide(400, 12)).Breathing().HeroAt(200, 10).HeroAt(150, 10, peer: 1);
        int seen = 0;
        for (int t = 0; t < 40 * Tune.Beat; t++)
        {
            w.Run();
            foreach (var c in w.M.Fauna.Creatures.Where(c => c.Id > seen))
            {
                Assert.Equal(0, w.World.Tick % Tune.Beat);
                int left = c.X / Unit - 1, top = c.Y / Unit - 2;
                foreach (var hero in new[] { 200, 150 })
                    Assert.False(left + 1 >= hero - Tune.View.X && left <= hero + Tune.View.X, $"creature {c.Id} spawned at {left} in view of {hero}");
                Assert.Equal((11 * Unit, Mode.Idle), (c.Y, c.Mode));
                Assert.True(top >= 0);
                seen = c.Id;
            }
        }
        Assert.True(seen > 0, "nothing spawned");
    }

    [Fact]
    public void NoHerosKeepBoxGetsMoreThanItsShareFromSpawningNorTheSurfaceMoreThanItsCap()
    {
        // Three heroes far apart, their keep boxes apart: each fills its share and no more.
        var w = FaunaTesting.Planet(Wide(1200, 12)).Breathing();
        for (int h = 0; h < 3; h++) w.HeroAt(100 + 200 * h, 10, peer: h);
        void Settle()
        {
            for (int t = 0; t < 100 * Tune.Beat; t++)
            {
                w.Run();
                foreach (var c in w.M.Fauna.Creatures) c.Still();
            }
        }
        int InKeep(int h) => w.M.Fauna.Creatures.Count(c => Math.Abs(c.X / Unit - (100 + 200 * h)) <= Tune.Keep.X);
        Settle();
        Assert.Equal([Tune.PerKeep, Tune.PerKeep, Tune.PerKeep], Enumerable.Range(0, 3).Select(InKeep));
        // Three more: six shares of ten would pass the cap of forty.
        for (int h = 3; h < 6; h++) w.HeroAt(100 + 200 * h, 10, peer: h);
        Settle();
        Assert.Equal(Tune.Max, w.M.Fauna.Creatures.Count);
        Assert.All(Enumerable.Range(0, 6), h => Assert.True(InKeep(h) <= Tune.PerKeep));
    }

    [Fact]
    public void NothingSpawnsUnderBackWallsOnALifelessTypeOrAboard()
    {
        var backed = Wide(400, 12, '.');
        var caves = FaunaTesting.Planet(backed).HeroAt(200, 10);
        var lifeless = FaunaTesting.Planet(Wide(400, 12), FaunaTesting.Lifeless).HeroAt(200, 10);
        var ship = new Scenario(Wide(400, 12)).HeroAt(200, 10);
        foreach (var w in new[] { caves, lifeless, ship }) w.Run(20 * Tune.Beat);
        Assert.Empty(caves.M.Fauna.Creatures);
        Assert.Empty(lifeless.M.Fauna.Creatures);
        Assert.Empty(ship.M.Fauna.Creatures);
    }

    [Fact]
    public void ACaveHabitatSpawnsOnlyOnBackWalledFloorsInItsBand()
    {
        // Rows this near the top are the first planet's band 0, the cold; band 1 is far under them.
        var cold = Spec.PlanetTypes.First with { Fauna = [new(Loper.Id, Tune.Place.Band, 1, Band: 0)] };
        var garden = Spec.PlanetTypes.First with { Fauna = [new(Loper.Id, Tune.Place.Band, 1, Band: 1)] };
        Assert.Equal(0, Terrain.BandOf(cold, 10));
        var caves = FaunaTesting.Planet(Wide(400, 12, '.'), cold).Breathing().HeroAt(200, 10);
        var sky = FaunaTesting.Planet(Wide(400, 12), cold).Breathing().HeroAt(200, 10);
        var deeper = FaunaTesting.Planet(Wide(400, 12, '.'), garden).Breathing().HeroAt(200, 10);
        foreach (var w in new[] { caves, sky, deeper }) w.Run(10 * Tune.Beat);
        Assert.NotEmpty(caves.M.Fauna.Creatures);
        Assert.Empty(sky.M.Fauna.Creatures);
        Assert.Empty(deeper.M.Fauna.Creatures);
    }

    [Fact]
    public void TheShipsFaunaNeverChangesNorDrawsAboardWhateverTheHeroesDo()
    {
        var w = new Scenario(Wide(400, 12)).HeroAt(200, 10).HeroAt(30, 10, peer: 1);
        string before = FaunaSlice(w);
        var rng = new Rng(new RngState { S = 7 });
        for (int t = 0; t < 400; t++)
        {
            if (w.M.Fauna.Fuzz(rng, (int)rng.Below(2)) is { } c) w.Do(c);
            w.Run();
        }
        Assert.Equal(before, FaunaSlice(w));
        Assert.All(w.Refusals.Where(r => r.Command is Release), r => Assert.Equal("fauna.notHere", r.Reason));
    }

    private static string FaunaSlice(Scenario w) => System.Text.Json.JsonSerializer.Serialize(w.M.Fauna.Slice, w.World.Json);

    [Fact]
    public void TheSpeciesAndTheirHabitatsHoldTogether()
    {
        Assert.Equal(Tune.All.Count, Tune.All.Select(s => s.Id).Distinct().Count());
        Assert.All(Tune.All, s =>
        {
            Assert.True(s.Width > 0 && s.Height > 0 && s.Hp > 0 && s.HurtTicks > 0 && s.FleeTicks > 0);
            // A tick moves less than a cell: the next column or row is the only one entered.
            Assert.True(s.Wander < Unit && s.Flee < Unit && s.Knock < Unit && s.Hop < Unit);
            Assert.All(s.Yields, y => Assert.NotNull(Spec.Items.Get(y.Item)));
        });
        Assert.True(Tune.MaxFall < Unit);
        foreach (var type in Spec.PlanetTypes.All)
            Assert.All(type.Fauna, h =>
            {
                Assert.NotNull(Tune.Get(h.Species));
                Assert.True(h.Weight > 0);
                Assert.InRange(h.Band, 0, type.Bands.Count);
            });
        // Spawned past every view, inside the keep box, which lies inside the hard box.
        Assert.True(Tune.RingFrom > Tune.View.X && Tune.RingTo + Tune.All.Max(s => s.Width) <= Tune.Keep.X);
        Assert.True(Tune.Keep.X < Tune.Hard.X && Tune.Keep.Y < Tune.Hard.Y && Tune.View.Y < Tune.Keep.Y);
        Assert.True(Tune.PerKeep <= Tune.Max);
    }

    [Fact]
    public void ACreatureABuildingWallsInRisesOutOfItACellATick()
    {
        var w = FaunaTesting.Planet(Room).HeroAt(1, 3);
        var c = w.Loper(5, 3).Still();
        w.Do(new Place(w.At(5, 3), Grid.Placement.Wall())).Do(new Place(w.At(6, 3), Grid.Placement.Wall())).Run();
        Assert.Equal(3 * Unit, c.Y);
        Assert.True(Clear(w, c));
    }
}
