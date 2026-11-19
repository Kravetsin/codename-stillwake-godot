using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The drone's soot spread round a window's outline (drone-frame, the pick «Угли»'s <c>frameOf</c>).</summary>
public class SootFrameTests
{
    /// <summary>The bag, the build menu, the board and a small window, screen px.</summary>
    private static readonly (float W, float H)[] Sizes = [(380, 200), (1120, 470), (1800, 1000), (40, 30)];

    private static (float X, float Y)[] Rect(float x, float y, float w, float h) => [(x, y), (x + w, y), (x + w, y + h), (x, y + h)];

    private static float Apart((float X, float Y) a, (float X, float Y) b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    [Fact]
    public void ASpotSitsInEveryCorner()
    {
        foreach (var (w, h) in Sizes)
            for (int near = 0; near < 4; near++)
            {
                var outline = Rect(20, 30, w, h);
                var spots = SootFrame.Spots(outline, near);
                foreach (var corner in outline)
                    Assert.Contains(Enumerable.Range(0, spots.Count), i => Apart(spots.Spot(i), corner) < 1e-3f);
            }
    }

    [Fact]
    public void GapsAreEvenAndWithin14To32()
    {
        foreach (var (w, h) in Sizes)
        {
            var spots = SootFrame.Spots(Rect(0, 0, w, h), 2);
            var gaps = Enumerable.Range(0, spots.Count).Select(i => Apart(spots.Spot(i), spots.Spot((i + 1) % spots.Count))).ToArray();
            Assert.All(gaps, g => Assert.InRange(g, SootRun.Tune.GapMin - 1e-3f, SootRun.Tune.GapMax + 1e-3f));
            // Along one edge every gap is the same: the spots split it evenly.
            for (int i = 0; i + 1 < gaps.Length; i++)
            {
                var (a, b, c) = (spots.Spot(i), spots.Spot(i + 1), spots.Spot((i + 2) % spots.Count));
                bool straight = MathF.Abs((b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X)) < 1e-2f;
                if (straight) Assert.Equal(gaps[i], gaps[i + 1], 2);
            }
        }
    }

    [Fact]
    public void OrderStartsAtTheCornerNearestTheDrone()
    {
        var outline = Rect(100, 50, 380, 200);
        // The drone low and to the right: the bottom-right corner is the nearest.
        int near = SootFrame.NearCorner(outline, (700, 400));
        Assert.Equal(2, near);
        var spots = SootFrame.Spots(outline, near);
        Assert.Equal(outline[2], spots.Spot(0));
        Assert.Equal(outline[2], spots.Corner);
        // Rank 0 both ways sits at the near corner: the corner itself one way, its neighbour back along the outline the other.
        Assert.Equal((0, 1), (spots.Rank(0), spots.Way(0)));
        Assert.Equal((0, -1), (spots.Rank(spots.Count - 1), spots.Way(spots.Count - 1)));
        Assert.True(Apart(spots.Spot(spots.Count - 1), outline[2]) < SootRun.Tune.GapMax + 1e-3f);
        // Each way, the rank rises with the distance along the outline from the corner.
        foreach (int way in (int[])[1, -1])
        {
            var ranked = Enumerable.Range(0, spots.Count).Where(i => spots.Way(i) == way).OrderBy(spots.Rank).ToArray();
            Assert.Equal(Enumerable.Range(0, ranked.Length), ranked.Select(spots.Rank));
            for (int k = 1; k < ranked.Length; k++) Assert.True(spots.Along(ranked[k]) > spots.Along(ranked[k - 1]));
        }
        Assert.Equal(spots.Perimeter / 2, Enumerable.Range(0, spots.Count).Max(spots.Along), 2);
    }

    /// <summary>How far <paramref name="p"/> is from the rectangle's outline at nought, <paramref name="w"/> by <paramref name="h"/>.</summary>
    private static float OffOutline((float X, float Y) p, float w, float h)
    {
        float dx = MathF.Max(MathF.Max(-p.X, p.X - w), 0), dy = MathF.Max(MathF.Max(-p.Y, p.Y - h), 0);
        if (dx > 0 || dy > 0) return MathF.Sqrt(dx * dx + dy * dy);
        return MathF.Min(MathF.Min(p.X, w - p.X), MathF.Min(p.Y, h - p.Y));
    }

    [Fact]
    public void EachNodeRestsInABandAlongTheOutline()
    {
        foreach (var (w, h) in Sizes)
        {
            var spots = SootFrame.Spots(Rect(0, 0, w, h), 2);
            for (int i = 0; i < spots.Count; i++)
                Assert.True(OffOutline(spots.Home(i), w, h) <= SootRun.Tune.NetSpread + 1e-3f, $"{w}×{h}: node {i} off the band");
        }
    }

    [Fact]
    public void EveryNodeIsLinkedOnceToEachNeighbourNearBy()
    {
        foreach (var (w, h) in Sizes)
        {
            var spots = SootFrame.Spots(Rect(0, 0, w, h), 2);
            var links = Enumerable.Range(0, spots.Links).Select(spots.Link).ToArray();
            Assert.Equal(links.Length, links.Distinct().Count());
            Assert.All(links, l => Assert.True(l.A < l.B));
            Assert.All(links, l => Assert.True(Apart(spots.Home(l.A), spots.Home(l.B)) < SootRun.Tune.NetSearch));
            for (int i = 0; i < spots.Count; i++) Assert.Contains(links, l => l.A == i || l.B == i);
        }
    }

    [Fact]
    public void TheNetLiesFlatAlongTheOutline()
    {
        // Picked 2026-11-19, trimmed for fewer steep lines: 9 in 10 links run within 30° of their edge (the pick's own net, 78 %).
        foreach (var (w, h) in Sizes.Take(3))
        {
            var spots = SootFrame.Spots(Rect(0, 0, w, h), 2);
            int flat = 0;
            for (int j = 0; j < spots.Links; j++)
            {
                var (a, b) = (spots.Home(spots.Link(j).A), spots.Home(spots.Link(j).B));
                float dx = MathF.Abs(a.X - b.X), dy = MathF.Abs(a.Y - b.Y);
                if (MathF.Min(dx, dy) <= MathF.Tan(MathF.PI / 6) * MathF.Max(dx, dy)) flat++;
            }
            Assert.True(flat >= 0.9f * spots.Links, $"{w}×{h}: only {flat} of {spots.Links} links lie flat");
        }
    }

    [Fact]
    public void ABigBoardTakesMoreThan170Spots()
    {
        var spots = SootFrame.Spots(Rect(0, 0, 1800, 1000), 0);
        Assert.True(spots.Count > 170, $"only {spots.Count}");
    }
}
