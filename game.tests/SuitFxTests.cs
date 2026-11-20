using System;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The suit's frost and heat as their picks draw them (body-temperature): the char's threshold, the edge field, the sparks.</summary>
public class SuitFxTests
{
    [Fact]
    public void TheHashIsThePagesOwn()
    {
        // As the page's JavaScript gives them: fract(sin(x * 127.1 + y * 311.7) * 43758.5453).
        double Page(double x, double y)
        {
            double v = Math.Sin(x * 127.1 + y * 311.7) * 43758.5453;
            return v - Math.Floor(v);
        }
        Assert.Equal(Page(3, 9), SuitFx.Hash(3, 9), 12);
        Assert.InRange(SuitFx.Hash(17, 2.5), 0, 1);
    }

    [Fact]
    public void NoHeatCharsNothingAndTheWholeOfItCharsMostOfTheSuit()
    {
        Assert.True(SuitFx.Scorch(0) < 0);
        Assert.True(SuitFx.Scorch(0.04f) < 0);
        Assert.True(SuitFx.Scorch(0.1f) > 0);
        Assert.Equal(0.81f, SuitFx.Scorch(1), 4);
    }

    [Fact]
    public void TwoPartsOfOnePictureCharApart()
    {
        Assert.NotEqual(SuitFx.Seed(1), SuitFx.Seed(2));
        Assert.Equal((0f, 0f), SuitFx.Seed(0));
    }

    /// <summary>An RGBA picture <paramref name="w"/> wide whose pixels are solid where <paramref name="solid"/> says.</summary>
    private static byte[] Picture(int w, int h, Func<int, int, bool> solid)
    {
        var data = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                data[(y * w + x) * 4 + 3] = solid(x, y) ? (byte)255 : (byte)0;
        return data;
    }

    [Fact]
    public void ThePictureDeepensFromItsEdgeInChamferSteps()
    {
        // A 9 x 9 picture, solid but for its one-pixel rim.
        var d = SuitFx.EdgeDistances(Picture(9, 9, (x, y) => x is > 0 and < 8 && y is > 0 and < 8), 9, 9);
        Assert.Equal(0, d[0]);
        Assert.Equal(1, d[1 * 9 + 4]);
        Assert.Equal(2, d[2 * 9 + 4]);
        Assert.Equal(4, d[4 * 9 + 4]);
        Assert.Equal(1, d[1 * 9 + 1]);
        Assert.Equal(2, d[2 * 9 + 2]);
    }

    [Fact]
    public void ADiagonalStepCostsTheRootOfTwo()
    {
        // One clear pixel in a corner: the pixel diagonal from it is √2 away.
        var d = SuitFx.EdgeDistances(Picture(3, 3, (x, y) => x + y > 0), 3, 3);
        Assert.Equal(MathF.Sqrt(2), d[4], 4);
        Assert.Equal(2 * MathF.Sqrt(2), d[8], 4);
    }

    [Fact]
    public void AHalfClearPixelIsOutside()
    {
        var data = Picture(3, 1, (_, _) => true);
        data[1 * 4 + 3] = 127;
        var d = SuitFx.EdgeDistances(data, 3, 1);
        Assert.Equal([1f, 0f, 1f], d);
    }

    [Fact]
    public void TheShaderReadsTheEdgeWholeAtItsReach()
    {
        var bytes = SuitFx.EdgeBytes([0, SuitFx.EdgeReach / 2, SuitFx.EdgeReach, 100, float.PositiveInfinity]);
        Assert.Equal([0, 128, 255, 255, 255], bytes);
    }

    [Fact]
    public void SparksLeaveOnlyFromInsideThePicture()
    {
        var d = SuitFx.EdgeDistances(Picture(5, 5, (x, y) => x is > 0 and < 4 && y is > 0 and < 4), 5, 5);
        var inside = SuitFx.Inside(d);
        Assert.Equal(9, inside.Length);
        Assert.All(inside, i => Assert.True(d[i] >= 1));
    }

    [Fact]
    public void NoHeatFliesNoSparks()
    {
        for (double t = 0; t < 10; t += 0.1)
            for (int i = 0; i < SuitFx.Sparks; i++)
                Assert.Null(SuitFx.SparkAt(i, t, 0));
    }

    [Fact]
    public void MoreHeatFliesMoreSparks()
    {
        int Flying(double heat)
        {
            int n = 0;
            for (double t = 0; t < 30; t += 0.05)
                for (int i = 0; i < SuitFx.Sparks; i++)
                    if (SuitFx.SparkAt(i, t, heat) != null) n++;
            return n;
        }
        int some = Flying(0.3), all = Flying(1);
        Assert.True(some > 0);
        Assert.True(all > some * 2);
    }

    [Fact]
    public void ASparkRisesFadesAndKeepsItsPlaceThroughItsRise()
    {
        var path = Enumerable.Range(0, 400).Select(k => SuitFx.SparkAt(3, k * 0.01, 1)).ToList();
        var first = path.FindIndex(s => s != null);
        Assert.True(first >= 0);
        var rise = path.Skip(first).TakeWhile(s => s != null).Select(s => s!.Value).ToList();
        Assert.True(rise.Count > 10);
        Assert.All(rise, s => Assert.Equal(rise[0].Cycle, s.Cycle));
        Assert.All(rise, s => Assert.Equal(rise[0].Pick, s.Pick));
        for (int k = 1; k < rise.Count; k++)
        {
            Assert.True(rise[k].Up > rise[k - 1].Up);
            Assert.True(rise[k].Age > rise[k - 1].Age);
            Assert.True(rise[k].Alpha <= rise[k - 1].Alpha);
        }
    }

    [Fact]
    public void ASparkJoinsOrLeavesOnlyAsItStartsANewRise()
    {
        // Through one rise the heat's choice holds: the same cycle is flying at every heat above the share that let it.
        for (int i = 0; i < SuitFx.Sparks; i++)
            for (double t = 0; t < 20; t += 0.25)
                if (SuitFx.SparkAt(i, t, 0.5) is { } half)
                    Assert.Equal(half.Cycle, SuitFx.SparkAt(i, t, 1)!.Value.Cycle);
    }
}
