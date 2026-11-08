using Stillwake.Game.Features.Backdrop;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>A planet's land composed from its seed and its own pictures (composed-backdrops), and the ground line it stands on.</summary>
public class BackdropPlanTests
{
    private static (int W, int H)? Size(string name) => Png.Has(name) ? Png.SizeOf(name) : null;

    private static readonly (int R, int G, int B) Grass =
        BackdropPlan.GrassOf(BackdropPlan.GrassPictures(Spec.PlanetTypes.First).Select(n => Png.Pixels(Png.Of(n)).Pixels));

    private static IReadOnlyList<BackdropPlan.Layer> Of(uint seed, Spec.PlanetType? type = null) =>
        BackdropPlan.Compose(type ?? Spec.PlanetTypes.First, seed, Size, Grass);

    /// <summary>Every draw as text, so two plans compare whole.</summary>
    private static List<string> Drawn(IReadOnlyList<BackdropPlan.Layer> plan) =>
        plan.SelectMany(l => l.Draws.Select(d => d switch
        {
            BackdropPlan.Fill f => $"{l.Depth.Name} fill {f.Sheet} {f.Wash} {f.Shade} {string.Join(',', f.Ridge.Up)}",
            BackdropPlan.Picture p => $"{l.Depth.Name} {p.Name} {p.X:F3} {p.Y:F3} {p.W:F3} {p.H:F3} {p.Flip} {p.Shade}",
            _ => throw new InvalidOperationException(),
        })).ToList();

    [Fact]
    public void TheSameSeedComposesTheSameLand() => Assert.Equal(Drawn(Of(7)), Drawn(Of(7)));

    [Fact]
    public void AnotherSeedComposesOtherLand()
    {
        Assert.NotEqual(Drawn(Of(7)), Drawn(Of(8)));
        // Not only the plants: the ridges themselves.
        Assert.NotEqual(Of(7)[1].Draws.OfType<BackdropPlan.Fill>().First().Ridge.Up, Of(8)[1].Draws.OfType<BackdropPlan.Fill>().First().Ridge.Up);
    }

    [Fact]
    public void ItHasItsDepthsFarToNearWithTreesInThem()
    {
        var plan = Of(1);
        Assert.Equal(BackdropLooks.First.Depths, plan.Select(l => l.Depth));
        Assert.All(plan, l => Assert.Contains(l.Draws.OfType<BackdropPlan.Picture>(), p => p.Name.StartsWith("planet/acacia_", StringComparison.Ordinal)));
    }

    [Fact]
    public void APlanetWithNoTallPlantPlacesNoTree()
    {
        var tall = Spec.Flora.All.Where(k => k.Tall >= BackdropLooks.First.TreeTall).ToList();
        Assert.NotEmpty(tall);
        var low = Spec.PlanetTypes.First with { Plants = Spec.Flora.All.Except(tall).ToList() };
        var trees = tall.SelectMany(k => FloraLooks.Pictures(k.Id)).ToHashSet();
        foreach (uint seed in new uint[] { 1, 2, 3 })
        {
            var plan = Of(seed, low);
            Assert.DoesNotContain(plan.SelectMany(l => l.Draws).OfType<BackdropPlan.Picture>(), p => trees.Contains(p.Name));
            // Its shrubs take the trees' place, so the land is not bare.
            Assert.Contains(plan.SelectMany(l => l.Draws).OfType<BackdropPlan.Picture>(), p => p.Name.StartsWith("planet/bush_", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void EveryPictureItDrawsIsOnDisk()
    {
        foreach (uint seed in new uint[] { 1, 2, 3, 0xdeadbeef })
            foreach (var draw in Of(seed).SelectMany(l => l.Draws))
            {
                string name = draw switch { BackdropPlan.Fill f => f.Sheet, BackdropPlan.Picture p => p.Name, _ => "" };
                Assert.True(Png.Has(name), name);
            }
    }

    [Fact]
    public void RidgesWrapAtTheTileAndKeepTheOneCellStair()
    {
        foreach (uint seed in new uint[] { 1, 2, 3, 4, 5, 0xdeadbeef })
            foreach (var layer in Of(seed))
                foreach (var ridge in layer.Draws.OfType<BackdropPlan.Fill>().Select(f => f.Ridge))
                {
                    Assert.Equal(BackdropLooks.First.Tile, ridge.N * ridge.Cx, 6);
                    for (int i = 0; i < ridge.N; i++)
                        Assert.InRange(ridge.Up[i] - ridge.Up[(i + 1) % ridge.N], -1, 1);
                    // A column past the tile is the tile's own.
                    Assert.Equal(ridge.Top(0), ridge.Top(ridge.N));
                    Assert.Equal(ridge.Top(ridge.N - 1), ridge.Top(-1));
                }
    }

    /// <summary>The most of a layer's face, in its pixels, a crest or a shelf may leave bare above the next one.</summary>
    private const double BareMost = 80;

    [Fact]
    public void TheNearFaceIsShelvedDownThroughItsHeight()
    {
        foreach (uint seed in new uint[] { 1, 2, 3, 4, 5, 7, 8, 0xdeadbeef })
        {
            var near = Of(seed)[^1];
            var ridges = near.Draws.OfType<BackdropPlan.Fill>().Select(f => f.Ridge).ToList();
            Assert.True(ridges.Count > 3, $"seed {seed}: {ridges.Count} ridges");
            for (int i = 0; i < ridges[0].N; i++)
                for (int k = 0; k < ridges.Count; k++)
                {
                    double top = ridges[k].Top(i), under = k + 1 < ridges.Count ? Math.Min(ridges[k + 1].Top(i), near.Depth.Tall) : near.Depth.Tall;
                    Assert.True(under - top <= BareMost, $"seed {seed}, column {i}: {under - top:F0} px bare under ridge {k}");
                }
        }
    }

    [Fact]
    public void ShelvesDarkenAsTheyComeNearer()
    {
        var fills = Of(1)[^1].Draws.OfType<BackdropPlan.Fill>().ToList();
        Assert.Equal(1, fills[0].Shade);
        Assert.All(fills.Zip(fills.Skip(1)), p => Assert.True(p.Second.Shade < p.First.Shade && p.Second.Wash.A >= p.First.Wash.A));
    }

    [Fact]
    public void TheGrassGreenIsReadFromItsTops()
    {
        Assert.True(Grass.G > Grass.R && Grass.G >= Grass.B, $"{Grass}");
        Assert.Equal(BackdropLooks.First.Grass, BackdropPlan.GrassOf([]));
    }

    [Fact]
    public void TheSurfaceRowIsTheFirstNotOpenToTheSky()
    {
        // Three columns, four rows: a wall at row 2, a back wall at row 1, nothing.
        int[] structure = [0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0], back = [0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0];
        Assert.Equal([2, 1, 4], GroundLine.Tops(structure, back, 3, 4));
    }

    [Fact]
    public void TheGroundLineMeanWrapsAtTheWorldsEdge()
    {
        var tops = Enumerable.Range(0, 50).Select(x => x < 5 || x >= 45 ? 100 : 10).ToArray();
        // Over column 0, two either side: 48, 49, 0, 1, 2, all of the edge's high ground.
        Assert.Equal(100, GroundLine.Mean(tops, 0.5, 2));
        Assert.Equal(GroundLine.Mean(tops, 0.5, 2), GroundLine.Mean(tops, 50.5, 2));
        Assert.Equal(GroundLine.Mean(tops, 49, 3), GroundLine.Mean(tops, -1, 3));
        Assert.Equal(10, GroundLine.Mean(tops, 25, 3));
    }
}
