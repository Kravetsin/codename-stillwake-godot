using Stillwake.Content;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The perches the sim keeps for the derelicts' pictures (<see cref="Wreckage.Perches"/>) lie on what the pictures show. They were
/// measured from the pixel pictures; the ink ones' tops wander a pixel or two along them, so a perch is held to its picture's top
/// within <see cref="Slack"/> rows rather than measured again.
/// </summary>
public class PerchesTests
{
    private const int Slack = 2;

    /// <summary>Each column's topmost drawn row of picture <paramref name="name"/>, its height where it has none.</summary>
    private static (int W, int H, int[] Tops) TopsOf(string name)
    {
        var (w, h, pixels) = Png.Logical(name);
        var tops = new int[w];
        for (int x = 0; x < w; x++)
        {
            tops[x] = h;
            for (int y = 0; y < h && tops[x] == h; y++)
                if (pixels[y * w + x] >> 24 > 0) tops[x] = y;
        }
        return (w, h, tops);
    }

    [Fact]
    public void EveryGuardedKindsPerchesLieOnItsPicturesTopAndPodClustersHaveNone()
    {
        foreach (var (kind, _) in Creatures.Derelicts.Kinds)
        {
            var (look, count) = DebrisLooks.Of[kind];
            Assert.Equal(count, Wreckage.Perches[kind].Count);
            for (int n = 0; n < count; n++)
            {
                var (w, h, tops) = TopsOf($"debris/{look}_{n + 1:00}");
                foreach (var perch in Wreckage.Perches[kind][n])
                {
                    Assert.True(0 <= perch.Left && perch.Right <= w && perch.Right - perch.Left >= Wreckage.PerchLeast && perch.Top < h, $"{kind} {n}: {perch}");
                    // Never sunk into the drawing, and held by it over half its length at least: an engine's row of pipes, gaps between.
                    var along = Enumerable.Range(perch.Left, perch.Right - perch.Left).ToList();
                    Assert.True(along.All(x => tops[x] >= perch.Top - Slack), $"{kind} {n}: {perch} sunk into its picture");
                    int on = along.Count(x => Math.Abs(tops[x] - perch.Top) <= Slack);
                    Assert.True(on * 2 >= along.Count, $"{kind} {n}: {perch} lies on its top over {on} columns");
                }
            }
        }
        Assert.Empty(Wreckage.PerchesOf("podCluster", 0));
    }
}
