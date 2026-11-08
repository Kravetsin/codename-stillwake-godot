using Stillwake.Content;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The deep caches (blasting-charge): a find in the rock by a cave, one kind a band, appended to the caves' growths so they take only
/// the rolls nothing took; dug or blasted, it gives its band's opals.
/// </summary>
public class PlanetCachesTests
{
    private static readonly PlanetType First = PlanetTypes.First;
    private const int Seeds = 16;
    private static readonly string[] Caches = ["frostCache", "bloomCache", "amberCache", "emberCache"];

    /// <summary>What a planet holds of each band's cache, on average: some 16, 18, 13 and 8 (blasting-charge).</summary>
    private static readonly double[] Aim = [16, 18, 13, 8];

    [Fact]
    public void EachBandHoldsItsOwnCacheAboutAsOftenAsAimedAndEveryPlanetSomeOfEach()
    {
        var totals = new int[Caches.Length];
        for (uint seed = 1; seed <= Seeds; seed++)
        {
            var l = PlanetTesting.First(seed).Layout;
            foreach (var (cell, kind) in l.Flora!)
            {
                int i = Array.IndexOf(Caches, kind);
                if (i < 0) continue;
                totals[i]++;
                Assert.Equal(i, Terrain.BandOf(First, cell / l.Width));
                Assert.Equal(Flora.Anchor.Rock, Flora.Get(kind).On);
            }
            foreach (string kind in Caches) Assert.Contains(l.Flora!, f => f.Kind == kind);
        }
        for (int i = 0; i < Caches.Length; i++)
            Assert.InRange(totals[i] / (double)Seeds, Aim[i] * 0.7, Aim[i] * 1.3);
    }

    [Fact]
    public void TheDeeperTheBandTheMoreOpalsItsCacheGives()
    {
        var opals = Caches.Select(k => Flora.Get(k).Yields.Single()).ToArray();
        Assert.All(opals, y => Assert.Equal("opal", y.Item));
        for (int i = 1; i < opals.Length; i++) Assert.True(opals[i].Count > opals[i - 1].Count);
        // An amber cache is worth a geode's crystals.
        Assert.Equal(Market.WorthOf("crystal") * Flora.Get("geode").Yields.Single().Count, Market.WorthOf("opal") * opals[2].Count);
        Assert.NotNull(Market.PriceOf("opal"));
    }

    [Fact]
    public void AppendedTheyLeaveEveryOtherGrowthAsItWasButTheRockGrowthsRightBesideACacheAndTheGroundWhole()
    {
        var without = First with { Caves = [.. First.Caves.Where(k => !Caches.Contains(k.Id))] };
        foreach (uint seed in new uint[] { 1, 2 })
        {
            var (with, was) = (PlanetTesting.First(seed).Layout, Terrain.Generate(without, seed));
            int w = with.Width;
            var caches = with.Flora!.Where(f => Caches.Contains(f.Kind)).Select(f => (X: f.Cell % w, Y: f.Cell / w)).ToList();
            var others = with.Flora!.Where(f => !Caches.Contains(f.Kind)).ToHashSet();
            var before = was.Flora!.ToHashSet();
            // A cache's cells, taken before the rock growths after it, move a moss or a mica beside it, and that one the next: a
            // few cells at most, the floors' and ceilings' growths never.
            bool Beside(int cell) => caches.Any(c => Math.Abs(c.X - cell % w) <= 4 && Math.Abs(c.Y - cell / w) <= 4);
            foreach (var growth in before.Except(others).Concat(others.Except(before)))
            {
                Assert.Equal(Flora.Anchor.Rock, Flora.Get(growth.Kind).On);
                Assert.True(Beside(growth.Cell), $"seed {seed}: {growth.Kind} at {growth.Cell % w},{growth.Cell / w} moved");
            }
            // The ore pockets paint no growth's cells: under a cache, or a moss it moved, a vein's cell may be stone, and back.
            for (int cell = 0; cell < w * with.Height; cell++)
                if (with.At(cell) != was.At(cell) || with.Materials![cell] != was.Materials![cell])
                    Assert.True(with.At(cell) == was.At(cell) && Beside(cell), $"seed {seed}: the ground at {cell % w},{cell / w} changed");
        }
    }
}
