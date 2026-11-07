using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Xunit;
using Xunit.Abstractions;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Ore at every depth on the first planet (2026-11-07-ore-pockets): copper and iron pockets through the stone, specks near the
/// surface, more and bigger deeper, the lode veins, the ground and its growths as they were.
/// </summary>
public class PlanetOresTests(ITestOutputHelper output)
{
    private static readonly PlanetType First = PlanetTypes.First;
    private static readonly int Stone = Materials.Get("stone").Code, Copper = Materials.Get("copperVein").Code, Iron = Materials.Get("ironVein").Code;
    private const int Seeds = 16;

    /// <summary>What the measurement found over the seeds: per ore and band, its cells and the rock's; its pockets' sizes in bands 0 and 2.</summary>
    private sealed record Measured(long[] Rock, Dictionary<int, long[]> Ore, Dictionary<int, List<int>> Band0, Dictionary<int, List<int>> Band2);

    private static readonly Lazy<Measured> Measurement = new(Measure);

    /// <summary>The ore share of the rock per band, over the first <see cref="Seeds"/> seeds: rock is solid stone and every vein in it.</summary>
    private static Measured Measure()
    {
        int bands = First.Bands.Count + 1;
        var rock = new long[bands];
        var ore = new Dictionary<int, long[]> { [Copper] = new long[bands], [Iron] = new long[bands] };
        var band0 = new Dictionary<int, List<int>> { [Copper] = [], [Iron] = [] };
        var band2 = new Dictionary<int, List<int>> { [Copper] = [], [Iron] = [] };
        for (uint seed = 1; seed <= Seeds; seed++)
        {
            var l = PlanetTesting.First(seed).Layout;
            int w = l.Width;
            for (int y = 0; y < l.Height; y++)
            {
                int band = Terrain.BandOf(First, y);
                for (int x = 0; x < w; x++)
                {
                    int cell = y * w + x, code = l.Materials![cell];
                    if (l.At(cell) != '#' || code <= 0 || code != Stone && Materials.ByCode(code)?.In == null) continue;
                    rock[band]++;
                    if (ore.TryGetValue(code, out var counts)) counts[band]++;
                }
            }
            // Bands 0 and 2 run no copper or iron lode veins but copper's in band 2: there iron's alone are pockets only.
            foreach (int code in (int[])[Copper, Iron])
                band0[code].AddRange(Pockets(l, code, 0));
            band2[Iron].AddRange(Pockets(l, Iron, 2));
        }
        return new(rock, ore, band0, band2);
    }

    /// <summary>The sizes of the 4-connected blobs of <paramref name="code"/> in band <paramref name="band"/>'s rows, cut at its edges.</summary>
    private static IEnumerable<int> Pockets(Layout l, int code, int band)
    {
        int w = l.Width;
        var seen = new bool[w * l.Height];
        var stack = new Stack<int>();
        bool In(int cell) => cell >= 0 && cell < seen.Length && !seen[cell] && l.Materials![cell] == code && l.At(cell) == '#' && Terrain.BandOf(First, cell / w) == band;
        for (int cell = 0; cell < seen.Length; cell++)
        {
            if (!In(cell)) continue;
            int size = 0;
            seen[cell] = true;
            stack.Push(cell);
            while (stack.Count > 0)
            {
                int at = stack.Pop();
                size++;
                foreach (int next in (int[])[at - w, at + w, at % w > 0 ? at - 1 : -1, at % w < w - 1 ? at + 1 : -1])
                    if (In(next))
                    {
                        seen[next] = true;
                        stack.Push(next);
                    }
            }
            yield return size;
        }
    }

    /// <summary>The plan's targets, percent of the rock per band (veins and pockets): copper, then iron.</summary>
    private static readonly Dictionary<string, double[]> Targets = new()
    {
        ["copperVein"] = [0.15, 1.2, 2.2, 1.6],
        ["ironVein"] = [0.25, 0.7, 0.6, 0.4],
    };

    [Fact]
    public void EachOresShareOfTheRockIsWithinAThirdOfItsTargetInEveryBand()
    {
        var m = Measurement.Value;
        foreach (var (material, targets) in Targets)
        {
            var counts = m.Ore[Materials.Get(material).Code];
            var shares = counts.Select((n, band) => 100.0 * n / m.Rock[band]).ToArray();
            output.WriteLine($"{material}: {string.Join(", ", shares.Select((s, b) => $"band {b} {s:F3} % (target {targets[b]} %)"))}");
            for (int band = 0; band < targets.Length; band++)
                Assert.True(Math.Abs(shares[band] - targets[band]) <= 0.3 * targets[band], $"{material} band {band}: {shares[band]:F3} % for {targets[band]} %");
        }
        // Going down pays: iron, pockets alone, richer in every band under the first.
        var iron = m.Ore[Iron];
        for (int band = 1; band < iron.Length; band++)
            Assert.True(iron[band] * m.Rock[0] > iron[0] * m.Rock[band], $"iron no richer in band {band} than in band 0");
    }

    /// <summary>The largest pocket band 0 holds over the seeds, a few specks joined (measured: copper's 26, iron's 52).</summary>
    private const int LargestNearTheSurface = 60;

    [Fact]
    public void PocketsNearTheSurfaceAreSpecksAndDeeperOnesBigger()
    {
        var m = Measurement.Value;
        foreach (int code in (int[])[Copper, Iron])
        {
            var sizes = m.Band0[code];
            output.WriteLine($"{Materials.ByCode(code)!.Id} band 0: {sizes.Count} pockets, mean {sizes.Average():F2}, largest {sizes.Max()}, "
                + $"over 6: {sizes.Count(s => s > 6)}; sizes {string.Join(" ", Enumerable.Range(1, 12).Select(n => $"{n}:{sizes.Count(s => s == n)}"))}");
        }
        var deep = m.Band2[Iron];
        output.WriteLine($"ironVein band 2: {deep.Count} pockets, mean {deep.Average():F2}, largest {deep.Max()}");
        // Where two of the noise's lattice points both pass the mark their pockets join, so a few run longer: the rest are specks.
        foreach (int code in (int[])[Copper, Iron])
        {
            var sizes = m.Band0[code];
            Assert.True(sizes.Max() <= LargestNearTheSurface, $"{Materials.ByCode(code)!.Id}: a band 0 pocket of {sizes.Max()} cells");
            Assert.True(sizes.Count(s => s > 6) * 100 <= sizes.Count * 5, $"{Materials.ByCode(code)!.Id}: {sizes.Count(s => s > 6)} of {sizes.Count} band 0 pockets over 6 cells");
            Assert.True(sizes.Average() < 2.5, $"{Materials.ByCode(code)!.Id}: band 0 pockets of {sizes.Average():F2} cells on average");
        }
        Assert.True(deep.Average() > 1.25 * m.Band0[Iron].Average(), "band 2's pockets no bigger than band 0's");
    }

    [Fact]
    public void ADigNearTheSpawnFindsCopperAndIronAndACaveShowsSomeOfEach()
    {
        const int Across = 600, Down = 60;
        var short_ = new List<string>();
        var opened = new Dictionary<int, List<int>> { [Copper] = [], [Iron] = [] };
        for (uint seed = 1; seed <= Seeds; seed++)
        {
            var grown = PlanetTesting.First(seed);
            var l = grown.Layout;
            int w = l.Width, spawn = Enumerable.Range(0, l.Height).Select(y => l.Rows[y].IndexOf('S')).First(x => x >= 0);
            var (found, open) = (new Dictionary<int, int> { [Copper] = 0, [Iron] = 0 }, new Dictionary<int, int> { [Copper] = 0, [Iron] = 0 });
            for (int x = Math.Max(1, spawn - Across); x <= Math.Min(w - 2, spawn + Across); x++)
                for (int y = grown.Surface[x]; y < Math.Min(l.Height - 1, grown.Surface[x] + Down); y++)
                {
                    int cell = y * w + x, code = l.Materials![cell];
                    if (l.At(cell) != '#' || !found.ContainsKey(code)) continue;
                    found[code]++;
                    if (l.At(cell - w) != '#' || l.At(cell + w) != '#' || l.At(cell - 1) != '#' || l.At(cell + 1) != '#') open[code]++;
                }
            output.WriteLine($"seed {seed}: copper {found[Copper]}, {open[Copper]} open; iron {found[Iron]}, {open[Iron]} open");
            foreach (int code in (int[])[Copper, Iron])
            {
                if (found[code] < 10) short_.Add($"seed {seed}: {found[code]} {Materials.ByCode(code)!.Id} cells near the spawn");
                opened[code].Add(open[code]);
            }
        }
        Assert.Empty(short_);
        // Open faces are few so near the surface (a band 0 share of about 0.2 %): a seed may show none (measured: one of 16 for each
        // ore), most show some, the seeds 3 or more on average.
        foreach (int code in (int[])[Copper, Iron])
        {
            Assert.True(opened[code].Count(n => n >= 1) >= 12, $"{Materials.ByCode(code)!.Id}: open near the spawn on {opened[code].Count(n => n >= 1)} seeds of {Seeds}");
            Assert.True(opened[code].Average() >= 3, $"{Materials.ByCode(code)!.Id}: {opened[code].Average():F1} cells open near the spawn a seed");
        }
    }

    [Fact]
    public void AMarkOfNoneGrowsNoOreInItsBandAndTheEarlierOreTakesACellBothReach()
    {
        static Vein Ore(string material, params int[] marks) => new(material, 40, 24, [0, 0, 0, 0], new(6, 4, marks));
        var small = PlanetTypes.Small;
        var both = small with { Veins = [Ore("copperVein", 0, 50000, 50000, 50000), Ore("ironVein", 50000, 50000, 50000, 50000)] };
        var copperAlone = small with { Veins = [both.Veins[0], both.Veins[1] with { Pockets = null }] };
        var ironAlone = small with { Veins = [Ore("copperVein", 0, 0, 0, 0), both.Veins[1]] };
        int shared = 0;
        foreach (uint seed in new uint[] { 1, 2, 3 })
        {
            var (l, c, i) = (Terrain.Generate(both, seed), Terrain.Generate(copperAlone, seed), Terrain.Generate(ironAlone, seed));
            for (int cell = 0; cell < l.Width * l.Height; cell++)
            {
                int code = l.Materials![cell];
                if (code == Copper) Assert.NotEqual(0, Terrain.BandOf(small, cell / l.Width));
                if (c.Materials![cell] != Copper) continue;
                // Copper's wherever it would be alone, iron's or not.
                Assert.Equal(Copper, code);
                if (i.Materials![cell] == Iron) shared++;
            }
            Assert.Contains(Iron, l.Materials!);
        }
        Assert.True(shared > 0, "no cell both ores reach");
    }

}

/// <summary>
/// Pockets on the small planet whatever the seed: only plain stone turns to ore. Apart from <see cref="PlanetOresTests"/>, whose
/// output a property beside it would lose.
/// </summary>
public class PlanetOresInvariantTests
{
    private static readonly int Stone = Materials.Get("stone").Code, Copper = Materials.Get("copperVein").Code, Iron = Materials.Get("ironVein").Code;

    private static Arbitrary<uint> AnySeed => Gen.Choose(0, int.MaxValue).Select(s => (uint)s * 2654435761u).ToArbitrary();

    [Property(MaxTest = 40)]
    public Property PocketsChangeOnlyPlainStoneAndNeitherTheGroundNorTheLodeVeinsNorTheGrowths() => Prop.ForAll(AnySeed, seed =>
    {
        var on = Terrain.Generate(PlanetTypes.Small, seed);
        var off = Terrain.Generate(PlanetTesting.WithoutPockets(PlanetTypes.Small), seed);
        Assert.Equal(off.Rows, on.Rows);
        Assert.Equal(off.Flora, on.Flora);
        int w = on.Width;
        var footprint = new bool[w * on.Height];
        foreach (var (cell, kind) in on.Flora!)
            if (Flora.Get(kind) is { On: Flora.Anchor.Rock } rock)
                for (int dx = 0; dx < rock.Across; dx++)
                    for (int i = 0, at = cell + dx; i < rock.Tall; i++, at += w) footprint[at] = true;
        int changed = 0;
        for (int cell = 0; cell < footprint.Length; cell++)
        {
            int was = off.Materials![cell], now = on.Materials![cell];
            if (was == now) continue;
            Assert.True(was == Stone && !footprint[cell] && on.At(cell) == '#' && (now == Copper || now == Iron), $"seed {seed}: cell {cell} {was} -> {now}");
            changed++;
        }
        return changed > 0;
    });
}
