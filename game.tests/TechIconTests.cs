using System;
using System.Linq;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A module technology's icon (0086): the shared hexagon, its glyph crackling on twos, painted as the page drew it.</summary>
public class TechIconTests
{
    private static readonly TechLooks.Look Arc = TechLooks.Arc;

    [Fact]
    public void StillTheIconIsTheClosedFrameThenTheGlyphAsDrawn()
    {
        var lines = TechLooks.Strokes(Arc, 0);
        Assert.Equal(1 + Arc.Glyph.Count, lines.Count);
        Assert.True(lines[0].Frame);
        Assert.Same(TechLooks.Frame, lines[0].Points);
        Assert.Equal(lines[0].Points[0], lines[0].Points[^1]);
        Assert.Equal(Arc.Glyph[0].Points, lines[1].Points);
        Assert.False(lines[1].Frame);
    }

    [Fact]
    public void ACrackleKinksEachSegmentNearItsLineAndKeepsItsEnds()
    {
        var glyph = Arc.Glyph[0].Points;
        for (int seed = 1; seed <= 500; seed++)
        {
            var bolt = TechLooks.Strokes(Arc, seed)[1].Points;
            Assert.Equal(glyph[0], bolt[0]);
            Assert.Equal(glyph[^1], bolt[^1]);
            int at = 0;
            for (int i = 1; i < glyph.Count; i++)
            {
                var (a, b) = (glyph[i - 1], glyph[i]);
                float length = MathF.Sqrt(Sq(b.U - a.U) + Sq(b.V - a.V));
                int kinks = length > TechLooks.LongSegment ? 2 : 1;
                for (int k = 1; k <= kinks; k++)
                    Assert.True(OffLine(bolt[at + k], a, b) <= TechLooks.Stray * length + 1e-5f, $"seed {seed}");
                at += kinks + 1;
                Assert.Equal(b, bolt[at]);
            }
        }
    }

    [Fact]
    public void ACrackleIsTheSameForItsSeedAndAnotherForTheNext()
    {
        Assert.Equal(TechLooks.Strokes(Arc, 7)[1].Points, TechLooks.Strokes(Arc, 7)[1].Points);
        Assert.NotEqual(TechLooks.Strokes(Arc, 7)[1].Points, TechLooks.Strokes(Arc, 8)[1].Points);
    }

    [Fact]
    public void NowAndThenACrackleBranchesThinnerAndNeverLeavesTheIcon()
    {
        int branched = 0, seeds = 4000;
        for (int seed = 1; seed <= seeds; seed++)
        {
            var lines = TechLooks.Strokes(Arc, seed);
            if (lines.Count > 2)
            {
                branched++;
                Assert.Equal(Arc.Glyph[0].Weight * 0.6f, lines[2].Weight, 4);
                Assert.Contains(lines[2].Points[0], lines[1].Points);
            }
            Assert.All(lines.SelectMany(l => l.Points), p => Assert.True(p.U is >= 0 and <= 1 && p.V is >= 0 and <= 1, $"seed {seed}: {p}"));
        }
        Assert.Equal(TechLooks.BranchChance, branched / (double)seeds, 1);
    }

    [Theory]
    [InlineData(TechShow.Mark, 14)]
    [InlineData(TechShow.Ghost, 32)]
    [InlineData(TechShow.Hot, 32)]
    [InlineData(TechShow.Lit, 12)]
    public void EveryWayItIsShownPaintsTheIconAndLeavesItsCornersClear(TechShow show, int side)
    {
        var canvas = Paint(show, side, 0);
        Assert.Contains(Alphas(canvas), a => a > 100);
        foreach (int p in new[] { 0, side - 1, side * (side - 1), side * side - 1 })
            Assert.True(canvas.Rgba[p * 4 + 3] < 40, $"{show}: corner {p} at {canvas.Rgba[p * 4 + 3]}");
    }

    [Fact]
    public void AGhostIsOnlyTheRimFaintWhileLitTheGlyphBurnsWhiteHot()
    {
        var ghost = Paint(TechShow.Ghost, 32, 0);
        uint rim = Arc.Rim;
        for (int p = 0; p < 32 * 32; p++)
        {
            if (ghost.Rgba[p * 4 + 3] == 0) continue;
            Assert.InRange(ghost.Rgba[p * 4], (rim >> 16 & 0xff) - 1, (rim >> 16 & 0xff) + 1);
            Assert.InRange(ghost.Rgba[p * 4 + 2], (rim & 0xff) - 1, rim & 0xff);
            Assert.True(ghost.Rgba[p * 4 + 3] <= Math.Round(0.48 * 255) + 1);
        }
        var lit = Paint(TechShow.Lit, 32, 0);
        Assert.Contains(Enumerable.Range(0, 32 * 32), p => lit.Rgba[p * 4 + 3] > 240 && lit.Rgba[p * 4 + 1] > 230);
    }

    [Fact]
    public void AMarkSitsOnADarkHaloAndCarriedOverASocketTheIconGlowsSofterThanOnACard()
    {
        var mark = Paint(TechShow.Mark, 14, 3);
        var lit = Paint(TechShow.Lit, 14, 3);
        Assert.True(Alphas(mark).Sum() > Alphas(lit).Sum());
        Assert.Contains(Enumerable.Range(0, 14 * 14), p => mark.Rgba[p * 4 + 3] > 150 && mark.Rgba[p * 4] < 40);
        Assert.True(Alphas(Paint(TechShow.Hot, 32, 3)).Sum() < Alphas(Paint(TechShow.Lit, 32, 3)).Sum());
    }

    private static TechCanvas Paint(TechShow show, int side, int seed)
    {
        var canvas = new TechCanvas(side);
        canvas.Paint(Arc, TechLooks.Strokes(Arc, seed), show, 1);
        return canvas;
    }

    private static int[] Alphas(TechCanvas canvas) => Enumerable.Range(0, canvas.Side * canvas.Side).Select(p => (int)canvas.Rgba[p * 4 + 3]).ToArray();

    private static float Sq(float v) => v * v;

    private static float OffLine((float U, float V) p, (float U, float V) a, (float U, float V) b)
    {
        float du = b.U - a.U, dv = b.V - a.V;
        return MathF.Abs((p.U - a.U) * dv - (p.V - a.V) * du) / MathF.Sqrt(du * du + dv * dv);
    }
}
