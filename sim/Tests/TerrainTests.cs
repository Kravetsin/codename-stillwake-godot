using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>A planet's ground grown from its seed (0077): what <see cref="Terrain"/> guarantees whatever the seed.</summary>
public class TerrainTests
{
    private static readonly Ground Small = PlanetTypes.Small.Ground, Planet = PlanetTypes.First.Ground;

    /// <summary>The small ground with no ruins: what it grows of itself, before a spire is stamped over it (0085).</summary>
    private static readonly PlanetType BareType = PlanetTypes.Small with { Ground = PlanetTypes.Small.Ground with { Spires = 0, Homes = 0 } };
    private static int Code(string material) => Materials.Get(material).Code;

    private static Arbitrary<uint> Seeds => Gen.Choose(0, int.MaxValue).Select(s => (uint)s * 2654435761u).ToArbitrary();

    [Property(MaxTest = 60)]
    public Property TheSameSeedGrowsTheSameGround() => Prop.ForAll(Seeds, seed =>
    {
        var (a, b) = (Terrain.Generate(PlanetTypes.Small, seed), Terrain.Generate(PlanetTypes.Small, seed));
        return a.Rows.SequenceEqual(b.Rows) && a.Materials!.SequenceEqual(b.Materials!);
    });

    [Fact]
    public void AnotherSeedGrowsOtherGround()
    {
        var (a, b) = (Terrain.Generate(PlanetTypes.First, 1), Terrain.Generate(PlanetTypes.First, 2));
        Assert.False(a.Rows.SequenceEqual(b.Rows));
    }

    [Property(MaxTest = 60)]
    public Property TheBottomIsLavaOverBedrockAndTheSideColumnsBedrockTheWholeHeight() => Prop.ForAll(Seeds, seed =>
    {
        var l = Terrain.Generate(PlanetTypes.Small, seed);
        bool Is(int x, int y, string material) => l.Rows[y][x] == '#' && l.Materials![y * l.Width + x] == Code(material);
        int floor = l.Height - Small.CoreRows, side = Small.BedrockSide;
        var sides = Enumerable.Range(0, l.Height).SelectMany(y => Enumerable.Range(0, side).SelectMany(x => new[] { (x, y), (l.Width - 1 - x, y) }));
        var bedrock = Enumerable.Range(floor + Small.LavaRows, Small.BedrockRows).SelectMany(y => Enumerable.Range(0, l.Width).Select(x => (x, y)));
        // The lava under every column between the sides, solid, the bedrock under it: nothing falls out of the bottom.
        var lava = Enumerable.Range(floor, Small.LavaRows).SelectMany(y => Enumerable.Range(side, l.Width - 2 * side).Select(x => (x, y)));
        // Nowhere else between the fog bands (fog-edges), which are bedrock: the ground inside is soil and stone.
        var inside = Enumerable.Range(0, floor).SelectMany(y => Enumerable.Range(Small.Fog, l.Width - 2 * Small.Fog).Select(x => (x, y)));
        return Small.LavaRows > 0 && Small.BedrockRows > 0
            && sides.Concat(bedrock).All(c => Is(c.Item1, c.Item2, "bedrock")) && lava.All(c => Is(c.Item1, c.Item2, "lava"))
            && inside.All(c => l.Materials![c.Item2 * l.Width + c.Item1] is var m && m != Code("bedrock") && m != Code("lava"));
    });

    [Fact]
    public void NothingGrowsOnTheLava()
    {
        // The deepest caves of the first planet reach the lava: their floors there grow nothing.
        var l = Terrain.Generate(PlanetTypes.First, 1);
        int floor = l.Height - Planet.CoreRows;
        Assert.Contains(Enumerable.Range(0, l.Width), x => l.Rows[floor - 1][x] == '.');
        Assert.DoesNotContain(l.Flora!, f => f.Cell / l.Width == floor - 1 && Content.Flora.Cave.Any(k => k.Id == f.Kind && k.Holds == Content.Flora.Anchor.Floor));
    }

    [Property(MaxTest = 60)]
    public Property HeroesBeamDownToAnOpenCellOnWholeGroundUnderTheSky() => Prop.ForAll(Seeds, seed =>
    {
        var l = Terrain.Generate(PlanetTypes.Small, seed);
        int spawn = Assert.Single(l.CellsOf('S'));
        int x = spawn % l.Width, y = spawn / l.Width;
        // Whole ground under it, the sky over it: no back wall, nothing above.
        return l.Rows[y + 1][x] == '#' && l.Materials![spawn] == 0
            && Enumerable.Range(0, y).All(above => l.Rows[above][x] == ' ');
    });

    [Fact]
    public void HeroesBeamDownInTheClearMiddleNeverInARuinWhereLittleGroundIsFlat()
    {
        // A small planet whose spawn once went out past its cave mouths, to a spire's columns and into its alloy.
        var l = Terrain.Generate(PlanetTypes.Small, 205198277);
        int spawn = Assert.Single(l.CellsOf('S'));
        Assert.Equal(0, l.Materials![spawn]);
        Assert.InRange(spawn % l.Width, l.Width / 2 - Small.Clear, l.Width / 2 + Small.Clear);
    }

    [Fact]
    public void HeroesBeamDownNearTheMiddleOfThePlanet()
    {
        var l = Terrain.Generate(PlanetTypes.First, 7);
        int spawn = l.CellsOf('S').Single();
        Assert.InRange(spawn % l.Width, l.Width / 2 - 40, l.Width / 2 + 40);
    }

    [Property(MaxTest = 60)]
    public Property SoilLiesOverStoneAtMostSixteenDeepAndTheSkyHasNoMaterial() => Prop.ForAll(Seeds, seed =>
    {
        var l = Terrain.Generate(BareType, seed);
        for (int x = Small.Fog; x < l.Width - Small.Fog; x++)
        {
            int top = TopOf(l, x), soil = SoilAt(l, x, top);
            if (soil > Small.SoilMost) return false;
            // Stone from there to the bedrock, open or not, veins of ore in it; nothing but sky above.
            if (Enumerable.Range(top + soil, l.Height - Small.CoreRows - top - soil).Any(y => Materials.ByCode(l.Materials![y * l.Width + x]) is not ({ Id: "stone" } or { In: "stone" }))) return false;
            // Above it, the sky, or a cave under an overhang of another column's: open, and of no material but in a cave.
            if (Enumerable.Range(0, top).Any(y => l.Rows[y][x] is not (' ' or 'S' or '.') || l.Rows[y][x] != '.' && l.Materials![y * l.Width + x] != 0)) return false;
        }
        return true;
    });

    [Fact]
    public void TheSoilBandIsAtLeastSixDeepNearlyEverywhere()
    {
        // Thinner where a spike was taken off the top or the ground carved open to the sky (a mouth, a chasm, a pit): on a
        // planet a few columns in a hundred. A tiny ground may be half chasm, so this is measured on planets.
        foreach (uint seed in Enumerable.Range(1, 6).Select(s => (uint)s))
        {
            var l = Terrain.Generate(PlanetTypes.First, seed);
            int thin = Enumerable.Range(Planet.Fog, l.Width - 2 * Planet.Fog).Count(x => SoilAt(l, x, TopOf(l, x)) < Planet.SoilLeast);
            Assert.True(thin * 10 <= l.Width, $"seed {seed}: {thin} thin columns");
        }
    }

    /// <summary>The top of the ground in column <paramref name="x"/>: its first solid cell; stone where it was carved open to the sky.</summary>
    private static int TopOf(Layout l, int x) => Enumerable.Range(0, l.Height).First(y => l.Rows[y][x] == '#');

    /// <summary>How deep the soil runs in column <paramref name="x"/> from <paramref name="top"/> down.</summary>
    private static int SoilAt(Layout l, int x, int top) =>
        Enumerable.Range(top, l.Height - top).TakeWhile(y => l.Materials![y * l.Width + x] == Code("soil")).Count();

    /// <summary>A grown ground has no shapes (0106): every cell whole or open, every material one a wall or a back wall is made of.</summary>
    [Property(MaxTest = 60)]
    public Property AGrownGroundIsWholeCellsOnly() => Prop.ForAll(Seeds, seed =>
    {
        var l = Terrain.Generate(PlanetTypes.Small, seed);
        return l.Rows.All(r => r.All(c => c is ' ' or '.' or '#' or 'S')) && l.Materials!.All(code => code >= 0);
    });

    [Fact]
    public void TheSurfaceIsAStairAHeroWalksWithNoCliffAnywhere()
    {
        // The first planet's terraces and hills are steeper than a row a column wherever they rise; the stair fills them up.
        foreach (uint seed in new uint[] { 1, 2 }) AStair(PlanetTypes.First, seed);
    }

    [Property(MaxTest = 60)]
    public Property TheSmallSurfaceIsAStairWhateverTheSeed() => Prop.ForAll(Seeds, seed => AStair(PlanetTypes.Small, seed));

    private static void AStair(PlanetType type, uint seed)
    {
        Terrain.Generate(type, seed, out _, out var surface);
        for (int x = 1; x < surface.Length; x++)
            Assert.True(Math.Abs(surface[x] - surface[x - 1]) <= 1, $"seed {seed}: {surface[x] - surface[x - 1]} rows at column {x}");
    }

    [Property(MaxTest = 60)]
    public Property NoSpikeStandsUp() => Prop.ForAll(Seeds, seed =>
    {
        var l = Terrain.Generate(BareType, seed);
        static bool Open(char c) => c is ' ' or '.' or 'S';
        for (int y = 1; y < l.Height; y++)
            for (int x = 1; x < l.Width - 1; x++)
            {
                char c = l.Rows[y][x], above = l.Rows[y - 1][x];
                // No post a cell wide standing in the open.
                if (c == '#' && Open(above) && Open(l.Rows[y][x - 1]) && Open(l.Rows[y][x + 1]) && l.Materials![y * l.Width + x] != Code("bedrock")) return false;
            }
        return true;
    });

    [Property(MaxTest = 60)]
    public Property OpenGroundStraightUnderTheSkyHasNoBackWallNearTheSurfaceAndDeeperOrUnderAnOverhangKeepsIt() =>
        Prop.ForAll(Seeds, SkyFadesUnderTheSurface);

    [Fact]
    public void APostCutFromAPitKeepsItsBackWallBelowTheFade() =>
        // A post standing in a pit, taken away as a spike, once showed the sky all the way down it.
        Assert.True(SkyFadesUnderTheSurface(1231727561u));

    private static bool SkyFadesUnderTheSurface(uint seed)
    {
        var l = Terrain.Generate(BareType, seed, out _, out var surface);
        for (int x = 0; x < l.Width; x++)
        {
            // Down each column: open sky, with no material, till its first solid cell or a few rows under its surface's line,
            // whichever comes first; every other open cell keeps its back wall, a shaft's deep down too.
            bool sky = true;
            for (int y = 0; y < l.Height; y++)
            {
                char c = l.Rows[y][x];
                if (c == '#')
                {
                    sky = false;
                    continue;
                }
                bool open = c is ' ' or 'S', bare = l.Materials![y * l.Width + x] == 0;
                if (open != bare) return false;
                if (open && (!sky || y >= surface[x] + Small.MouthFadeMost)) return false;
                if (!open && sky && y < surface[x] + Small.MouthFadeLeast) return false;
                if (!open) sky = false;
            }
        }
        return true;
    }

    [Fact]
    public void AShaftKeepsItsBackWallAllTheWayDownUnderItsMouth()
    {
        // The first planet's chasms go down hundreds of rows straight under the sky: only their first few rows show it.
        var l = Terrain.Generate(PlanetTypes.First, 1, out _, out var surface);
        int deepest = 0;
        for (int x = 0; x < l.Width; x++)
        {
            int y = 0;
            while (y < l.Height && l.Rows[y][x] is ' ' or 'S') y++;
            Assert.True(y <= surface[x] + Planet.MouthFadeMost, $"column {x}: sky down to row {y}, its surface at {surface[x]}");
            int down = y;
            while (down < l.Height && l.Rows[down][x] == '.') down++;
            deepest = Math.Max(deepest, down - y);
        }
        Assert.True(deepest > 40, $"the deepest open run straight under the sky {deepest} rows, a back wall in each");
    }

    [Fact]
    public void CavesAreDenserDeeperOnEverySeed()
    {
        foreach (uint seed in Enumerable.Range(1, 12).Select(s => (uint)s))
        {
            var l = Terrain.Generate(PlanetTypes.First, seed);
            int from = Planet.Surface + Planet.CaveFrom, to = Planet.Height - Planet.CoreRows, third = (to - from) / 3;
            double Open(int top, int bottom) =>
                Enumerable.Range(top, bottom - top).Sum(y => l.Rows[y].Count(c => c == '.')) / (double)((bottom - top) * l.Width);
            Assert.True(Open(to - third, to) > Open(from, from + third) * 1.15, $"seed {seed}: {Open(from, from + third):P0} above, {Open(to - third, to):P0} below");
        }
    }

    [Fact]
    public void TheFirstCavesAreAFewRowsUnderTheSoilAndSomeOpenToTheSky()
    {
        // Every planet has caves within its first rows of stone, and on most a cave mouth reaches the surface.
        int mouths = 0;
        foreach (uint seed in Enumerable.Range(1, 12).Select(s => (uint)s))
        {
            var l = Terrain.Generate(PlanetTypes.First, seed);
            int near = Enumerable.Range(Planet.Surface, 2 * Planet.CaveFrom).Sum(y => l.Rows[y].Count(c => c == '.'));
            Assert.True(near > 100, $"seed {seed}: {near} cave cells near the surface");
            // A cave breaking the surface: open under the open sky.
            if (Enumerable.Range(l.Width, l.Width * (l.Height - 2)).Any(c => l.At(c) == '.' && (l.At(c - l.Width) == ' ' || l.At(c - 1) == ' ' || l.At(c + 1) == ' '))) mouths++;
        }
        Assert.True(mouths >= 6, $"{mouths} of 12 planets with a cave open to the sky");
    }

    [Fact]
    public void ChasmsAndHallsLieAtPlacesTheSeedChoosesSpreadOutAndClearOfTheMiddle()
    {
        foreach (uint seed in new uint[] { 1, 2, 3 })
        {
            var l = Terrain.Generate(PlanetTypes.First, seed);
            int w = l.Width, middle = w / 2;
            int TopOf(int x) => Enumerable.Range(0, l.Height).First(y => l.Rows[Math.Clamp(y, 0, l.Height - 1)][Math.Clamp(x, 1, w - 2)] == '#');
            // A chasm: a column open nine cells in ten from the land beside it down half a chasm's greatest depth.
            int LandBy(int x) => Math.Min(TopOf(x - 20), TopOf(x + 20));
            var chasms = Enumerable.Range(1, w - 2).Where(x => Enumerable.Range(LandBy(x), Planet.ChasmDeep / 2).Count(y => l.Rows[y][x] is ' ' or '.') * 10 >= Planet.ChasmDeep / 2 * 9).ToList();
            Assert.NotEmpty(chasms);
            Assert.All(chasms, x => Assert.True(Math.Abs(x - middle) > Planet.Clear, $"seed {seed}: a chasm at {x}"));
            Assert.True(chasms.Max() - chasms.Min() > w / 2, $"seed {seed}: chasms only from {chasms.Min()} to {chasms.Max()}");
            // A hall: a run of open cells along a row, deep down, as wide as the smallest hall.
            int halls = Enumerable.Range(Planet.Surface + Planet.HallFrom, l.Height - Planet.Surface - Planet.HallFrom - Planet.CoreRows)
                .Count(y => l.Rows[y].Contains(new string('.', Planet.HallWide)));
            Assert.True(halls > 0, $"seed {seed}: no hall");
        }
    }

    [Fact]
    public void AWalkAcrossThePlanetTakesMinutes()
    {
        // At the hero's walking pace, steps of a sixtieth of a second: the user's "a small world kills exploration".
        double seconds = Planet.Width * 1000.0 / Content.Hero.Walk / 60;
        Assert.True(seconds > 240, $"{seconds:F0} s across");
    }

    [Fact]
    public void AGrownGroundIsBuiltOfItsMaterialsWithBackWallsFromTheSurfaceDown()
    {
        var l = Terrain.Generate(PlanetTypes.Small, 3);
        var grid = new Scenario(l).M.Grid;
        for (int cell = 0; cell < l.Width * l.Height; cell++)
        {
            int code = l.Materials![cell];
            // A vein's back wall the stone it runs through.
            var back = code <= 0 ? null : Materials.ByCode(code)!;
            Assert.Equal(back?.In is { } rock ? Materials.Get(rock) : back, grid.BackAt(cell));
            if (l.At(cell) == '#') Assert.Equal(code, Materials.All.Single(m => m.Id == grid.WallAt(cell)!.Id).Code);
        }
    }

    /// <summary>The first planet grown from seed 1, once: the veins' tests read it.</summary>
    private static readonly Lazy<Layout> FirstOne = new(() => Terrain.Generate(PlanetTypes.First, 1));

    private static bool IsVein(int code) => Materials.ByCode(code)?.In != null;

    [Fact]
    public void VeinsRunOnlyThroughSolidStoneInTheBandsTheyRunIn()
    {
        var (l, t) = (FirstOne.Value, PlanetTypes.First);
        var seen = new HashSet<string>();
        for (int cell = 0; cell < l.Width * l.Height; cell++)
        {
            if (!IsVein(l.Materials![cell])) continue;
            var vein = t.Veins.Single(v => Materials.Get(v.Material).Code == l.Materials[cell]);
            Assert.Equal('#', l.At(cell));
            Assert.True(vein.Widths[Terrain.BandOf(t, cell / l.Width)] > 0, $"{vein.Material} at row {cell / l.Width}");
            seen.Add(vein.Material);
        }
        Assert.Equal(t.Veins.Select(v => v.Material).Order(StringComparer.Ordinal), seen.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AVeinIsALineOfCellsNotScatteredDots()
    {
        // Nearly every vein cell has another of its vein beside it, across or corner to corner.
        var l = FirstOne.Value;
        int w = l.Width, cells = 0, joined = 0;
        for (int y = 1; y < l.Height - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int code = l.Materials![y * w + x];
                if (!IsVein(code)) continue;
                cells++;
                bool beside = false;
                for (int dy = -1; dy <= 1 && !beside; dy++)
                    for (int dx = -1; dx <= 1 && !beside; dx++)
                        beside = (dx, dy) != (0, 0) && l.Materials[(y + dy) * w + x + dx] == code;
                if (beside) joined++;
            }
        Assert.True(cells > 1000, $"{cells} vein cells");
        Assert.True(joined * 100 >= cells * 97, $"{joined} of {cells} joined");
    }

    [Fact]
    public void WithoutLodesThereAreNoVeinsNorCrystalsButTheRestOfTheCavesStillGrow()
    {
        var bare = PlanetTypes.Small with { LodeOver = 65535 };
        foreach (uint seed in new uint[] { 1, 2, 3, 4 })
        {
            var l = Terrain.Generate(bare, seed);
            Assert.DoesNotContain(l.Materials!, IsVein);
            Assert.DoesNotContain(l.Flora!, p => Flora.Cave.Any(k => k.Id == p.Kind && k.Lode));
        }
        Assert.Contains(FirstOne.Value.Flora!, p => Flora.Cave.Any(k => k.Id == p.Kind && k.Lode));
        Assert.Contains(FirstOne.Value.Flora!, p => Flora.Cave.Any(k => k.Id == p.Kind && !k.Lode));
    }

    [Fact]
    public void CrystalsGatherByTheVeinsInRichCavesWithBareOnesBetween()
    {
        // Within a lode's reach of a vein cell: a crystal or a geode far more often than any growth that grows by the patches.
        var l = FirstOne.Value;
        int w = l.Width, reach = PlanetTypes.First.LodeWave / 2;
        var near = new bool[l.Width * l.Height];
        for (int cell = 0; cell < near.Length; cell++)
            if (IsVein(l.Materials![cell]))
                for (int dy = -reach; dy <= reach; dy += 4)
                    for (int dx = -reach; dx <= reach; dx += 4)
                    {
                        int x = cell % w + dx, y = cell / w + dy;
                        if (x >= 0 && x < w && y >= 0 && y < l.Height) near[y * w + x] = true;
                    }
        double Share(bool lode)
        {
            var kinds = l.Flora!.Where(p => Flora.Cave.Any(k => k.Id == p.Kind && k.Lode == lode && k.Chances.Any(c => c > 0))).ToList();
            return (double)kinds.Count(p => near[p.Cell]) / kinds.Count;
        }
        Assert.True(Share(true) > 0.95, $"{Share(true)} of the crystals by a vein");
        Assert.True(Share(false) < 0.6, $"{Share(false)} of the rest by a vein");
    }

    [Fact]
    public void PlainsFlattenTheLandAndHighlandsLiftIt()
    {
        // Against the same seeds grown with no regions: the ranges and hills of every column at their most, the land unlifted.
        var flat = PlanetTypes.First with { Ground = PlanetTypes.First.Ground with { RegionLeast = 100, RegionLift = 0 } };
        foreach (uint seed in new uint[] { 1, 2 })
        {
            Terrain.Generate(PlanetTypes.First, seed, out _, out var regions);
            Terrain.Generate(flat, seed, out _, out var none);
            // The rows climbed over each eighth of the planet: the plains' the flattest.
            static long[] Climbs(int[] s) => [.. s.Chunk(s.Length / 8).Select(c => c.Zip(c.Skip(1)).Sum(p => (long)Math.Abs(p.First - p.Second)))];
            Assert.True(Climbs(regions).Min() * 2 < Climbs(none).Min(), $"seed {seed}: {Climbs(regions).Min()} rows climbed on the plains, {Climbs(none).Min()} with none");
            Assert.True(Climbs(regions).Sum() < Climbs(none).Sum(), $"seed {seed}");
            Assert.True(regions.Average() < none.Average() - PlanetTypes.First.Ground.RegionLift / 4, $"seed {seed}: {regions.Average()} against {none.Average()}");
        }
    }
}
