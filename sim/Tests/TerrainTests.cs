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
    private static readonly Ground Small = Ground.Small, Planet = Ground.Planet;
    private static int Code(string material) => Materials.Get(material).Code;

    private static Arbitrary<uint> Seeds => Gen.Choose(0, int.MaxValue).Select(s => (uint)s * 2654435761u).ToArbitrary();

    [Property(MaxTest = 60)]
    public Property TheSameSeedGrowsTheSameGround() => Prop.ForAll(Seeds, seed =>
    {
        var (a, b) = (Terrain.Generate(Small, seed), Terrain.Generate(Small, seed));
        return a.Rows.SequenceEqual(b.Rows) && a.Materials!.SequenceEqual(b.Materials!);
    });

    [Fact]
    public void AnotherSeedGrowsOtherGround()
    {
        var (a, b) = (Terrain.Generate(Planet, 1), Terrain.Generate(Planet, 2));
        Assert.False(a.Rows.SequenceEqual(b.Rows));
    }

    [Property(MaxTest = 60)]
    public Property TheBottomRowsAndTheSideColumnsAreBedrockTheWholeHeight() => Prop.ForAll(Seeds, seed =>
    {
        var l = Terrain.Generate(Small, seed);
        bool Bedrock(int x, int y) => l.Rows[y][x] == '#' && l.Materials![y * l.Width + x] == Code("bedrock");
        var edge = Enumerable.Range(0, l.Height).SelectMany(y => new[] { (0, y), (l.Width - 1, y) })
            .Concat(Enumerable.Range(l.Height - Small.BedrockRows, Small.BedrockRows).SelectMany(y => Enumerable.Range(0, l.Width).Select(x => (x, y))));
        // Nowhere else: the ground inside is soil and stone.
        var inside = Enumerable.Range(0, l.Height - Small.BedrockRows).SelectMany(y => Enumerable.Range(1, l.Width - 2).Select(x => (x, y)));
        return edge.All(c => Bedrock(c.Item1, c.Item2)) && inside.All(c => l.Materials![c.Item2 * l.Width + c.Item1] != Code("bedrock"));
    });

    [Property(MaxTest = 60)]
    public Property HeroesBeamDownToAnOpenCellOnWholeGroundUnderTheSky() => Prop.ForAll(Seeds, seed =>
    {
        var l = Terrain.Generate(Small, seed);
        int spawn = Assert.Single(l.CellsOf('S'));
        int x = spawn % l.Width, y = spawn / l.Width;
        // Whole ground under it, the sky over it: no back wall, nothing above.
        return l.Rows[y + 1][x] == '#' && l.Materials![spawn] == 0
            && Enumerable.Range(0, y).All(above => l.Rows[above][x] == ' ');
    });

    [Fact]
    public void HeroesBeamDownNearTheMiddleOfThePlanet()
    {
        var l = Terrain.Generate(Planet, 7);
        int spawn = l.CellsOf('S').Single();
        Assert.InRange(spawn % l.Width, l.Width / 2 - 40, l.Width / 2 + 40);
    }

    [Property(MaxTest = 60)]
    public Property SoilLiesOverStoneAtMostEightDeepAndTheSkyHasNoMaterial() => Prop.ForAll(Seeds, seed =>
    {
        var l = Terrain.Generate(Small, seed);
        for (int x = 1; x < l.Width - 1; x++)
        {
            int top = TopOf(l, x), soil = SoilAt(l, x, top);
            if (soil > Small.SoilMost) return false;
            // Stone from there to the bedrock, open or not; nothing but sky above.
            if (Enumerable.Range(top + soil, l.Height - Small.BedrockRows - top - soil).Any(y => Math.Abs(l.Materials![y * l.Width + x]) != Code("stone"))) return false;
            // Above it, the sky, or a cave under an overhang of another column's: open, and of no material but in a cave.
            if (Enumerable.Range(0, top).Any(y => l.Rows[y][x] is not (' ' or 'S' or '.') || l.Rows[y][x] != '.' && l.Materials![y * l.Width + x] != 0)) return false;
        }
        return true;
    });

    [Fact]
    public void TheSoilBandIsAtLeastThreeDeepNearlyEverywhere()
    {
        // Thinner where a spike was taken off the top or the ground carved open to the sky (a mouth, a chasm, a pit): on a
        // planet a few columns in a hundred. A tiny ground may be half chasm, so this is measured on planets.
        foreach (uint seed in Enumerable.Range(1, 6).Select(s => (uint)s))
        {
            var l = Terrain.Generate(Planet, seed);
            int thin = Enumerable.Range(1, l.Width - 2).Count(x => SoilAt(l, x, TopOf(l, x)) < Planet.SoilLeast);
            Assert.True(thin * 10 <= l.Width, $"seed {seed}: {thin} thin columns");
        }
    }

    /// <summary>The top of the ground in column <paramref name="x"/>: its first solid cell; stone where it was carved open to the sky.</summary>
    private static int TopOf(Layout l, int x) => Enumerable.Range(0, l.Height).First(y => l.Rows[y][x] is '#' or '/' or '\\');

    /// <summary>How deep the soil runs in column <paramref name="x"/> from <paramref name="top"/> down.</summary>
    private static int SoilAt(Layout l, int x, int top) =>
        Enumerable.Range(top, l.Height - top).TakeWhile(y => Math.Abs(l.Materials![y * l.Width + x]) == Code("soil")).Count();

    [Property(MaxTest = 60)]
    public Property ASlopeIsAWallOpenAboveWithOneSideOpenAndRisesTowardTheOther() => Prop.ForAll(Seeds, seed =>
    {
        var l = Terrain.Generate(Small, seed);
        bool Solid(int x, int y) => l.Rows[y][x] is '#' or '/' or '\\';
        for (int y = 1; y < l.Height; y++)
            for (int x = 1; x < l.Width - 1; x++)
            {
                char c = l.Rows[y][x];
                if (!Solid(x, y) || l.Materials![y * l.Width + x] == Code("bedrock") || Solid(x, y - 1))
                {
                    if (c is '/' or '\\') return false;
                    continue;
                }
                bool left = Solid(x - 1, y), right = Solid(x + 1, y);
                char want = right && !left ? '/' : left && !right ? '\\' : '#';
                if (c != want) return false;
            }
        return true;
    });

    [Property(MaxTest = 60)]
    public Property NoSpikeStandsUpAndASlopeUnderTheSkyHasNoBackWallWhereOneInACaveKeepsIt() => Prop.ForAll(Seeds, seed =>
    {
        var l = Terrain.Generate(Small, seed);
        static bool Open(char c) => c is ' ' or '.' or 'S';
        for (int y = 1; y < l.Height; y++)
            for (int x = 1; x < l.Width - 1; x++)
            {
                char c = l.Rows[y][x], above = l.Rows[y - 1][x];
                int code = l.Materials![y * l.Width + x];
                // No ◢◣ peak, and no post a cell wide standing in the open.
                if (c == '/' && l.Rows[y][x + 1] == '\\') return false;
                if (c == '#' && Open(above) && Open(l.Rows[y][x - 1]) && Open(l.Rows[y][x + 1]) && code != Code("bedrock")) return false;
                if (c is '/' or '\\' && code < 0 != (above == ' ')) return false;
            }
        return true;
    });

    [Property(MaxTest = 60)]
    public Property OpenGroundStraightUnderTheSkyHasNoBackWallAndUnderAnOverhangKeepsIt() => Prop.ForAll(Seeds, seed =>
    {
        var l = Terrain.Generate(Small, seed);
        for (int x = 0; x < l.Width; x++)
        {
            // Down each column: open sky, with no material, till its first solid cell; open cells under that are caves.
            bool sky = true;
            for (int y = 0; y < l.Height; y++)
            {
                char c = l.Rows[y][x];
                if (c is '#' or '/' or '\\') sky = false;
                else if (sky != (c is ' ' or 'S') || sky != (l.Materials![y * l.Width + x] == 0)) return false;
            }
        }
        return true;
    });

    [Fact]
    public void CavesAreDenserDeeperOnEverySeed()
    {
        foreach (uint seed in Enumerable.Range(1, 12).Select(s => (uint)s))
        {
            var l = Terrain.Generate(Planet, seed);
            int from = Planet.Surface + Planet.CaveFrom, to = Planet.Height - Planet.BedrockRows, third = (to - from) / 3;
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
            var l = Terrain.Generate(Planet, seed);
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
            var l = Terrain.Generate(Planet, seed);
            int w = l.Width, middle = w / 2;
            int TopOf(int x) => Enumerable.Range(0, l.Height).First(y => l.Rows[Math.Clamp(y, 0, l.Height - 1)][Math.Clamp(x, 1, w - 2)] is '#' or '/' or '\\');
            // A chasm: a column open nine cells in ten from the land beside it down half a chasm's greatest depth.
            int LandBy(int x) => Math.Min(TopOf(x - 20), TopOf(x + 20));
            var chasms = Enumerable.Range(1, w - 2).Where(x => Enumerable.Range(LandBy(x), Planet.ChasmDeep / 2).Count(y => l.Rows[y][x] is ' ' or '.') * 10 >= Planet.ChasmDeep / 2 * 9).ToList();
            Assert.NotEmpty(chasms);
            Assert.All(chasms, x => Assert.True(Math.Abs(x - middle) > Planet.Clear, $"seed {seed}: a chasm at {x}"));
            Assert.True(chasms.Max() - chasms.Min() > w / 2, $"seed {seed}: chasms only from {chasms.Min()} to {chasms.Max()}");
            // A hall: a run of open cells along a row, deep down, as wide as the smallest hall.
            int halls = Enumerable.Range(Planet.Surface + Planet.HallFrom, l.Height - Planet.Surface - Planet.HallFrom - Planet.BedrockRows)
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
        var l = Terrain.Generate(Small, 3);
        var grid = new Scenario(l).M.Grid;
        for (int cell = 0; cell < l.Width * l.Height; cell++)
        {
            int code = l.Materials![cell];
            Assert.Equal(code <= 0 ? null : Materials.All.Single(m => m.Code == code), grid.BackAt(cell));
            if (l.At(cell) is '#' or '/' or '\\') Assert.Equal(Math.Abs(code), Materials.All.Single(m => m.Id == grid.WallAt(cell)!.Id).Code);
        }
        Assert.Contains(Enumerable.Range(0, l.Width * l.Height), c => grid.ShapeAt(c) == Shape.BottomRight);
    }
}
