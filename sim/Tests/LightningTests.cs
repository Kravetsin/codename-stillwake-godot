using System.Text.Json;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;
using Stillwake.Sim.Sky;
using Xunit;
using Spec = Stillwake.Content.Stretches;

namespace Stillwake.Sim.Tests;

/// <summary>
/// A storm's one bolt of lightning (docs/plans/surface-storms.md): drawn as the storm starts, aimed at the highest open-sky wall near one
/// hero drawn, glowing its <c>Glow</c>, then a crater and a shock to every hero under the open sky in its zone; drawn from a fork.
/// </summary>
public class LightningTests
{
    private static readonly Content.Surfaces.Kind Strip = Content.Surfaces.TestStrip;
    private static readonly Spec.Stretch Dust = Spec.DustStorm, Rain = Spec.RainStorm;
    private static readonly Spec.BoltRule Bolt = Dust.Bolt!;

    /// <summary>A soil ground of the strip's rules, a character a cell: <c>W</c> wide, its top at row <c>Ground</c>, the open sky above.</summary>
    private const int W = 64, H = 14, Ground = 10;

    /// <summary>The pillar most maps hold: one soil cell over the ground at column 30, the highest thing near.</summary>
    private const int PillarX = 30;
    private static readonly int Top = Cell(PillarX, Ground - 1);

    private static int Cell(int x, int y) => y * W + x;

    /// <summary>Where a hero stands on the flat at column <paramref name="x"/>: the open cell over the ground.</summary>
    private static int Stand(int x) => Cell(x, Ground - 1);

    private static char? Pillar(int x, int y) => x == PillarX && y == Ground - 1 ? '#' : null;

    /// <summary>
    /// The ground, <paramref name="paint"/> drawing over it (<c>#</c> soil, <c>.</c> an open cell with a back wall, space the open sky),
    /// stirred unless <paramref name="calm"/>, under the strip's rules: its storms and their bolts.
    /// </summary>
    private static Scenario Land(Func<int, int, char?>? paint = null, bool calm = false, IReadOnlyList<(int Cell, string Kind)>? flora = null,
        Box? zone = null, uint seed = 1)
    {
        var rows = new List<string>();
        var looks = new List<string>();
        for (int y = 0; y < H; y++)
        {
            var row = new char[W];
            var look = new char[W];
            for (int x = 0; x < W; x++)
            {
                row[x] = paint?.Invoke(x, y) ?? (y >= Ground ? '#' : ' ');
                look[x] = row[x] == '#' ? 's' : '.';
            }
            rows.Add(new string(row));
            looks.Add(new string(look));
        }
        var layout = Layout.Parse(rows).WithMaterials(looks) with { Kind = Strip.Id, Meteors = !calm, Flora = flora ?? [], Zone = zone };
        return new Scenario(layout, seed);
    }

    private static HazardsState S(Scenario w) => (HazardsState)w.M.Hazards.Slice;
    private static Strike? Glowing(Scenario w) => w.M.Hazards.Strike;
    private static Hero HeroOf(Scenario w, int peer, int surface = 0) => w.On(surface).Crew.HeroOf(peer)!;

    /// <summary><paramref name="storm"/> brought and run to its first tick: running, its bolt's wait drawn.</summary>
    private static Scenario Storm(Scenario w, Spec.Stretch storm)
    {
        w.Do(new StretchNow(storm.Id)).Run(storm.Warning);
        Assert.Equal((storm.Id, 0), (w.M.Sky.Now.Stretch, w.M.Sky.Now.Ran));
        return w;
    }

    /// <summary>Runs until <paramref name="until"/> holds, at most <paramref name="most"/> ticks; the ticks run.</summary>
    private static int RunUntil(Scenario w, Func<bool> until, int most = 10_000)
    {
        for (int t = 1; t <= most; t++)
        {
            w.Run();
            if (until()) return t;
        }
        throw new Xunit.Sdk.XunitException($"not within {most} ticks");
    }

    /// <summary>Runs the strike glowing to its end, every hero's air topped up first: the tick it strikes.</summary>
    private static void Strikes(Scenario w)
    {
        foreach (var hero in w.M.Crew.Heroes) hero.Air = Scenario.Full;
        RunUntil(w, () => Glowing(w) == null, Bolt.Glow);
    }

    /// <summary>The world's own random state, as its save holds it.</summary>
    private static uint WorldRng(Scenario w) => JsonDocument.Parse(w.World.Save()).RootElement.GetProperty("rng").GetUInt32();

    [Fact]
    public void EachStormBoltIsDrawnInItsSpanItsGlowShortEnoughToStrikeBeforeAnyStormEndsAndTheShipHasNone()
    {
        foreach (var storm in Spec.FirstPlanet)
        {
            var b = storm.Bolt!;
            Assert.True(b.At.Most + b.Glow < storm.Duration.Least, storm.Id);
            Assert.Equal((200, 1000, 60, 24, 2, 40), (b.At.Least, b.At.Most, b.Glow, b.Columns, b.Reach, b.Shock));
        }
        Assert.Equal((10, 20), (Dust.Bolt!.Spread, Rain.Bolt!.Spread));
        Assert.All(Spec.Ship, s => Assert.Null(s.Bolt));
    }

    // One a storm.

    [Fact]
    public void AStormStrikesOnceSeenGlowingItsGlowFirstAndItsCellHoldsWhenTheHeroWalksAway()
    {
        var w = Storm(Land(Pillar).Do(new Enter(Stand(10))).Run(), Rain);
        int wait = S(w).BoltIn;
        Assert.InRange(wait, Bolt.At.Least, Bolt.At.Most);
        Assert.Equal(wait, RunUntil(w, () => Glowing(w) != null));
        Assert.Equal(new Strike(Top, Bolt.Glow, Rain.Bolt!.Spread), Glowing(w));
        w.Do(new Enter(Stand(60))).Run(Bolt.Glow - 1);
        Assert.Equal(new Strike(Top, 1, Rain.Bolt!.Spread), Glowing(w));
        Assert.NotNull(w.M.Grid.WallAt(Top));
        w.Run();
        Assert.Null(Glowing(w));
        Assert.Null(w.M.Grid.WallAt(Top));
        // Out of the zone when it struck: not shocked.
        Assert.Equal(0, HeroOf(w, 0).Hurts);
        while (w.M.Sky.Now.Stretch != null)
        {
            w.Run();
            Assert.Null(Glowing(w));
        }
        Assert.Equal(0, S(w).BoltIn);
    }

    [Fact]
    public void TheCraterBreaksTheStruckSoilAndCracksTheSoilBesideAndBelowAtFullDamage()
    {
        var w = Land(Pillar).Do(new Enter(Stand(10))).Run().Do(new BoltNow()).Run();
        Assert.Equal(Top, Glowing(w)!.Cell);
        Strikes(w);
        Assert.Null(w.M.Grid.WallAt(Top));
        int Crack(int cell) => w.M.Building.Cracks.SingleOrDefault(c => c.Cell == cell)?.Damage ?? 0;
        // 60 less the pillar's 40, then a step and the next cell's: 18 below it, 16 beside that through the open air; nothing more breaks.
        Assert.Equal((16, 18, 16), (Crack(Cell(PillarX - 1, Ground)), Crack(Cell(PillarX, Ground)), Crack(Cell(PillarX + 1, Ground))));
        Assert.All(new[] { -1, 0, 1 }, dx => Assert.NotNull(w.M.Grid.WallAt(Cell(PillarX + dx, Ground))));
    }

    // The target.

    [Fact]
    public void AHilltopWithinTwentyFourColumnsBeatsLowerGroundNearerTheHeroAndOneFartherIsNotSeen()
    {
        var w = Land((x, y) => x == 12 && y == Ground - 1 || x == 33 && y >= Ground - 3 || x == 35 && y >= Ground - 5 ? '#' : null);
        w.Do(new Enter(Stand(10))).Run().Do(new BoltNow()).Run();
        Assert.Equal(Cell(33, Ground - 3), Glowing(w)!.Cell);
    }

    [Fact]
    public void ABuiltRoofHigherThanTheGroundIsStruckAndItsWallBreaksInFullWhileTheHeroUnderItIsSheltered()
    {
        // Soil walls over a back-walled room, columns 18 to 22: the nearest cell of the roof to the hero is struck.
        var w = Land((x, y) => x is >= 18 and <= 22 && y >= Ground - 4 && y < Ground ? y == Ground - 4 ? '#' : '.' : null);
        w.Do(new Enter(Stand(10))).Do(new Enter(Stand(20)) { From = 1 }).Run().Do(new BoltNow()).Run();
        int roof = Cell(18, Ground - 4);
        Assert.Equal(roof, Glowing(w)!.Cell);
        Strikes(w);
        // The bolt is no player's: soil's 40 paid in full by 60, where a tenth would only crack it.
        Assert.Null(w.M.Grid.WallAt(roof));
        Assert.NotNull(w.M.Grid.BackAt(Stand(20)));
        Assert.Equal(0, HeroOf(w, 1).Hurts);
    }

    [Fact]
    public void AColumnWithABackWallAboveItsTopWallIsSkipped()
    {
        var w = Land((x, y) => x == 25 && y == Ground - 4 ? '.' : x == 25 && y >= Ground - 3 || x == 27 && y >= Ground - 2 ? '#' : null);
        w.Do(new Enter(Stand(20))).Run().Do(new BoltNow()).Run();
        Assert.Equal(Cell(27, Ground - 2), Glowing(w)!.Cell);
    }

    [Fact]
    public void TiesGoToTheColumnNearestTheHeroThenTheLeftOne()
    {
        var flat = Land().Do(new Enter(Stand(30))).Run().Do(new BoltNow()).Run();
        Assert.Equal(Cell(30, Ground), Glowing(flat)!.Cell);
        var bumps = Land((x, y) => x is 27 or 33 && y == Ground - 1 ? '#' : null).Do(new Enter(Stand(30))).Run().Do(new BoltNow()).Run();
        Assert.Equal(Cell(27, Ground - 1), Glowing(bumps)!.Cell);
    }

    [Fact]
    public void NothingToStrikeWithinItsColumnsOrOnlyInTheFogNoBolt()
    {
        // No ground within 24 columns of the hero, floating at column 5.
        var bare = Land((x, y) => x < 40 && y >= Ground ? ' ' : null).Do(new Enter(Stand(5))).Run().Do(new BoltNow()).Run();
        Assert.Empty(bare.Refusals);
        Assert.Null(Glowing(bare));
        // A tower past the zone's edge is never aimed at: the flat in the zone is, under the hero.
        var fog = Land((x, y) => x == 45 && y >= Ground - 5 ? '#' : null, zone: new Box(0, 0, 40, H - 1));
        fog.Do(new Enter(Stand(30))).Run().Do(new BoltNow()).Run();
        Assert.Equal(Cell(30, Ground), Glowing(fog)!.Cell);
    }

    // The shock.

    [Fact]
    public void ARainStormsBoltShocksAHeroInTheOpenTwentyColumnsAwayNotTwentyOneNorTwentyUnderABackWall()
    {
        var w = Land((x, y) => x == 50 && y == Ground - 1 ? '.' : Pillar(x, y));
        w.Do(new Enter(Stand(10))).Do(new Enter(Stand(51)) { From = 1 }).Do(new Enter(Stand(50)) { From = 2 }).Run();
        Storm(w, Rain).Do(new BoltNow()).Run();
        Assert.Equal(new Strike(Top, Bolt.Glow - 1, 20), Glowing(w));
        Strikes(w);
        Hero near = HeroOf(w, 0), far = HeroOf(w, 1), sheltered = HeroOf(w, 2);
        Assert.Equal((1, Harm.Shock), (near.Hurts, near.Hurt));
        Assert.Equal((0, 0), (far.Hurts, sheltered.Hurts));
        // Both out in the storm's cold alike: the shock is the difference.
        Assert.Equal(far.Air - Bolt.Shock, near.Air);
    }

    [Fact]
    public void ADustStormsBoltReachesTenColumns()
    {
        var w = Land(Pillar).Do(new Enter(Stand(20))).Do(new Enter(Stand(41)) { From = 1 }).Run();
        Storm(w, Dust).Do(new BoltNow()).Run();
        Assert.Equal(new Strike(Top, Bolt.Glow - 1, 10), Glowing(w));
        Strikes(w);
        Assert.Equal((1, 0), (HeroOf(w, 0).Hurts, HeroOf(w, 1).Hurts));
        Assert.Equal(HeroOf(w, 1).Air - Bolt.Shock, HeroOf(w, 0).Air);
    }

    [Fact]
    public void ACreativeHeroAndOneOutLoseNothingAndAHeroOnAnotherSurfaceIsNeverCounted()
    {
        var w = Land(Pillar);
        w.Do(new Enter(Stand(10))).Do(new Enter(Stand(28)) { From = 1 }).Do(new Enter(Stand(32)) { From = 2 }).Do(new Enter(Stand(34)) { From = 3 })
            .Do(new Enter(Stand(26)) { From = 4 }).Do(new SetCreative(true) { From = 1 }).Do(new OpenSurface(Strip.Id)).Run();
        // Hero 4 on the strip, out in its open at a column within the zone's.
        w.Do(new Transfer(1) { From = 4 }).Run();
        w.Do(new Enter(59 * 2 * Strip.Rows[0].Length + PillarX) { From = 4 }).Run();
        Assert.True(w.On(1).Crew.OpenSky(HeroOf(w, 4, 1).Cell));
        HeroOf(w, 2).Out = 100_000;
        w.Do(new BoltNow()).Run();
        Strikes(w);
        Assert.Equal((0, 0, 0, 1), (HeroOf(w, 0).Hurts, HeroOf(w, 1).Hurts, HeroOf(w, 2).Hurts, HeroOf(w, 3).Hurts));
        Assert.Equal(0, HeroOf(w, 4, 1).Hurts);
    }

    // Who is drawn, and when nothing is.

    [Fact]
    public void WithNoHeroWhenItsWaitIsUpNoHeroIsDrawnAndNothingStrikes()
    {
        var w = Storm(Land(Pillar), Dust);
        int wait = S(w).BoltIn;
        Assert.Equal(1, S(w).Drawn);
        w.Run(wait);
        Assert.Equal((0, 1, null), (S(w).BoltIn, S(w).Drawn, Glowing(w)));
        while (w.M.Sky.Now.Stretch != null) Assert.Null(Glowing(w.Run()));
    }

    [Fact]
    public void ACalmWorldDrawsNoBoltAndCalmingDropsOneGlowing()
    {
        var calm = Storm(Land(Pillar, calm: true).Do(new Enter(Stand(10))).Run(), Rain);
        Assert.Equal((0, 0), (S(calm).BoltIn, S(calm).Drawn));
        while (calm.M.Sky.Now.Stretch != null) Assert.Null(Glowing(calm.Run()));

        var w = Storm(Land(Pillar).Do(new Enter(Stand(10))).Run(), Rain).Do(new BoltNow()).Run();
        Assert.NotNull(Glowing(w));
        w.Do(new SetCalm(true)).Run();
        Assert.Equal((0, null), (S(w).BoltIn, Glowing(w)));
        w.Run(Bolt.Glow);
        Assert.NotNull(w.M.Grid.WallAt(Top));
    }

    [Fact]
    public void InCoopEveryHeroInTheZoneIsShockedAndTheBoltFallsNearTheOneDrawn()
    {
        var w = Land((x, y) => x == 25 && y == Ground - 1 ? '#' : null).Do(new Enter(Stand(10))).Do(new Enter(Stand(14)) { From = 1 }).Run();
        Storm(w, Rain);
        RunUntil(w, () => Glowing(w) != null);
        Assert.Equal(Cell(25, Ground - 1), Glowing(w)!.Cell);
        Strikes(w);
        Assert.Equal((1, 1), (HeroOf(w, 0).Hurts, HeroOf(w, 1).Hurts));

        // Two heroes far apart, each by a pillar of its own: over seeds, each is drawn.
        var struck = new HashSet<int>();
        for (uint seed = 1; seed <= 12; seed++)
        {
            var apart = Land((x, y) => x is 6 or 57 && y == Ground - 1 ? '#' : null, seed: seed);
            apart.Do(new Enter(Stand(3))).Do(new Enter(Stand(60)) { From = 1 }).Run();
            Storm(apart, Dust);
            RunUntil(apart, () => Glowing(apart) != null);
            struck.Add(Glowing(apart)!.Cell);
        }
        Assert.Equal(new[] { Cell(6, Ground - 1), Cell(57, Ground - 1) }, struck.Order().ToArray());
    }

    // Saves.

    [Fact]
    public void SavedWithItsWaitOrItsGlowItStrikesTheSameCellOnTheSameTick()
    {
        var w = Storm(Land(Pillar).Do(new Enter(Stand(10))).Run(), Rain).Run(50);
        var waiting = Land(Pillar).Reload(w.World.Save());
        Assert.Equal(S(w).BoltIn, S(waiting).BoltIn);
        int glows = RunUntil(w, () => Glowing(w) != null);
        Assert.Equal(glows, RunUntil(waiting, () => Glowing(waiting) != null));
        w.Run(10);
        var glowing = Land(Pillar).Reload(w.World.Save());
        Assert.Equal(Glowing(w), Glowing(glowing));
        Assert.Equal(w.World.Hash(), glowing.World.Hash());
        int strikes = RunUntil(w, () => Glowing(w) == null);
        Assert.Equal(strikes, RunUntil(glowing, () => Glowing(glowing) == null));
        Assert.Null(glowing.M.Grid.WallAt(Top));
        Assert.Equal(w.World.Hash(), glowing.World.Hash());
    }

    // What else it touches.

    [Fact]
    public void APlantOnTheStruckCellIsFelledByNobodyItsYieldsDroppedThere()
    {
        // A bush two cells across on a pillar as wide: the struck half breaks under it, the other only cracks.
        var w = Land((x, y) => x is PillarX or PillarX + 1 && y == Ground - 1 ? '#' : null, flora: [(Cell(PillarX, Ground - 2), "bush")]);
        w.Do(new Enter(Stand(10))).Run();
        Assert.Contains(w.M.Building.Plants, p => p.Kind == "bush");
        Assert.Empty(w.M.Items.Floor);
        w.Do(new BoltNow()).Run();
        Assert.Equal(Top, Glowing(w)!.Cell);
        Strikes(w);
        Assert.NotNull(w.M.Grid.WallAt(Top + 1));
        Assert.DoesNotContain(w.M.Building.Plants, p => p.Kind == "bush");
        Assert.Contains(w.M.Items.Floor, s => s.Item == "seed");
    }

    [Fact]
    public void BoltNowIsRefusedWhereNoStretchHasABoltWhileOneGlowsAndWithNoHeroOfTheSender()
    {
        var ship = new Scenario(Layout.Parse(["      ", "######"]));
        ship.Do(new Enter(1)).Run().Do(new BoltNow()).Run();
        Assert.Equal("hazards.noBolt", Assert.Single(ship.Refusals).Reason);

        var w = Land(Pillar).Do(new Enter(Stand(10))).Run();
        w.Do(new BoltNow { From = 1 }).Run();
        Assert.Equal("hazards.noHero", Assert.Single(w.Refusals).Reason);
        w.Refusals.Clear();
        w.Do(new BoltNow()).Run().Do(new BoltNow()).Run();
        Assert.Equal("hazards.boltGlowing", Assert.Single(w.Refusals).Reason);
        Assert.Equal(Bolt.Glow - 2, Glowing(w)!.Left);
    }

    [Fact]
    public void ItsWaitAndItsHeroAreDrawnFromItsOwnForkLeavingTheWorldsRngAsItWas()
    {
        var w = Land(Pillar).Do(new Enter(Stand(10))).Run();
        w.Do(new StretchNow(Rain.Id)).Run(Rain.Warning - 1);
        uint before = WorldRng(w);
        w.Run();
        Assert.Equal(Rain.Id, w.M.Sky.Now.Stretch);
        Assert.Equal((before, 1), (WorldRng(w), S(w).Drawn));
        w.Run(S(w).BoltIn - 1);
        before = WorldRng(w);
        w.Run();
        Assert.NotNull(Glowing(w));
        Assert.Equal((before, 2), (WorldRng(w), S(w).Drawn));
    }

    // The invariant.

    private const string Ship = """
        ............
        .##########.
        .#..D..S..#.
        .##########.
        ............
        """;

    private static Arbitrary<uint> Seeds => Gen.Choose(0, int.MaxValue).Select(s => (uint)s * 2654435761u).ToArbitrary();

    /// <summary>
    /// On a small planet grown from the world's seed, a hero landed and creative: a storm brought strikes at most once, however its
    /// wait, its hero and its target fall.
    /// </summary>
    [Property(MaxTest = 8)]
    public Property AStormStrikesAtMostOnce() => Prop.ForAll(Seeds, seed =>
    {
        var w = new Scenario(Ship, seed);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Content.PlanetTypes.Small.Id)).Run();
        w.Do(new Transfer(1)).Run().Do(new SetCreative(true)).Run();
        var planet = w.On(1);
        var storm = seed % 2 == 0 ? Dust : Rain;
        w.Do(new StretchNow(storm.Id)).Run(storm.Warning);
        // A glow just begun has its whole Glow left: one aimed on the tick another struck is counted too.
        int strikes = 0;
        while (planet.Sky.Now.Stretch != null || planet.Hazards.Strike != null)
            if (w.Run().On(1).Hazards.Strike?.Left == storm.Bolt!.Glow) strikes++;
        return (strikes <= 1).Label($"{strikes} strikes in one storm");
    });
}
