using Stillwake.Content;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The perches the sim keeps for the derelicts' pictures (<see cref="Wreckage.Perches"/>) are those the pictures show.</summary>
public class PerchesTests
{
    private static readonly string Debris = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "art", "debris");

    /// <summary>
    /// The stretches of a picture's top, from the left, each as long as its columns' topmost pixels stay within one row of each
    /// other, those <see cref="Wreckage.PerchLeast"/> across or more, at their highest row.
    /// </summary>
    private static List<Wreckage.Perch> Measured(string png)
    {
        var (w, h, pixels) = CreatureArtTests.Pixels(png);
        var tops = new int[w];
        for (int x = 0; x < w; x++)
        {
            tops[x] = h;
            for (int y = 0; y < h && tops[x] == h; y++)
                if (pixels[(y * w + x) * 4 + 3] > 0) tops[x] = y;
        }
        var perches = new List<Wreckage.Perch>();
        for (int x = 0; x < w;)
        {
            int lo = tops[x], hi = tops[x], end = x + 1;
            while (end < w && tops[end] < h && Math.Max(hi, tops[end]) - Math.Min(lo, tops[end]) <= 1)
                (lo, hi, end) = (Math.Min(lo, tops[end]), Math.Max(hi, tops[end]), end + 1);
            if (tops[x] < h && end - x >= Wreckage.PerchLeast) perches.Add(new(x, end, lo));
            x = end;
        }
        return perches;
    }

    [Fact]
    public void EveryGuardedKindsPerchesAreThoseItsPicturesShowAndPodClustersHaveNone()
    {
        foreach (var (kind, _) in Creatures.Derelicts.Kinds)
        {
            var (look, count) = DebrisLooks.Of[kind];
            Assert.Equal(count, Wreckage.Perches[kind].Count);
            for (int n = 0; n < count; n++)
                Assert.Equal(Measured(Path.Combine(Debris, $"{look}_{n + 1:00}.png")), Wreckage.Perches[kind][n]);
        }
        for (int n = 1; n <= Looks.CountOf("podCluster"); n++) Assert.Empty(Measured(Path.Combine(Debris, $"pods_{n:00}.png")));
    }
}
