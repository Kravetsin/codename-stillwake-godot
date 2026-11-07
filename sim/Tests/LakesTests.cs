using Stillwake.Content;
using Xunit;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Sim.Tests;

/// <summary>Lakes grown with a planet (docs/plans/liquids.md, L-2).</summary>
public class LakesTests
{
    /// <summary>Each lake of <paramref name="l"/>: its cells, found four ways from the top left.</summary>
    private static List<List<int>> LakesOf(Layout l)
    {
        var lakes = new List<List<int>>();
        if (l.Liquids is not { } water) return lakes;
        var seen = new bool[water.Count];
        for (int start = 0; start < water.Count; start++)
        {
            if (water[start] == L.None || seen[start]) continue;
            var lake = new List<int> { start };
            seen[start] = true;
            for (int k = 0; k < lake.Count; k++)
            {
                int c = lake[k], x = c % l.Width;
                foreach (int n in new[] { x > 0 ? c - 1 : -1, x + 1 < l.Width ? c + 1 : -1, c - l.Width, c + l.Width })
                    if (n >= 0 && n < water.Count && water[n] != L.None && !seen[n])
                    {
                        seen[n] = true;
                        lake.Add(n);
                    }
            }
            lakes.Add(lake);
        }
        return lakes;
    }

    /// <summary>
    /// What must hold of every lake <paramref name="t"/> grew on <paramref name="seed"/>: full water in open cells of the zone,
    /// clear of the ruins, within the big cap, and still: every open cell beside or under it is in it.
    /// </summary>
    private static IEnumerable<string> Wrongs(PlanetType t, uint seed)
    {
        var l = Terrain.Generate(t, seed, out _, out _, out var spires, out var homes);
        var ruins = spires.Select(s => new Box(s.X, s.Y, s.X + Ruins.SpireWide - 1, s.Y + Ruins.Spire.Count - 1))
            .Concat(homes.Select(h => new Box(h.X, h.Y, h.X + h.Home.Wide - 1, h.Y + h.Home.Tall - 1))).ToList();
        foreach (var lake in LakesOf(l))
        {
            if (lake.Count > t.LakeBig) yield return $"{t.Id} {seed}: a lake of {lake.Count} cells";
            foreach (int cell in lake)
            {
                int x = cell % l.Width, y = cell / l.Width;
                if (l.Liquids![cell] != L.Of(L.Water, L.Full)) yield return $"{t.Id} {seed}: {l.Liquids[cell]} at {x},{y}";
                if (l.Rows[y][x] != '.') yield return $"{t.Id} {seed}: water on '{l.Rows[y][x]}' at {x},{y}";
                if (!l.Zone!.Value.Contains(x, y)) yield return $"{t.Id} {seed}: water past the zone at {x},{y}";
                if (ruins.Any(b => b.Contains(x, y))) yield return $"{t.Id} {seed}: water in a ruin at {x},{y}";
                foreach (int n in new[] { x > 0 ? cell - 1 : -1, x + 1 < l.Width ? cell + 1 : -1, y + 1 < l.Height ? cell + l.Width : -1 })
                    if (n >= 0 && l.Rows[n / l.Width][n % l.Width] != '#' && l.Liquids[n] == L.None)
                        yield return $"{t.Id} {seed}: water at {x},{y} can run to {n % l.Width},{n / l.Width}";
            }
        }
    }

    [Fact]
    public void EveryLakeIsFullStillInTheZoneClearOfRuinsAndWithinItsCap()
    {
        var wrongs = Enumerable.Range(1, 50).SelectMany(s => Wrongs(PlanetTypes.Small, (uint)s))
            .Concat(Enumerable.Range(1, 20).SelectMany(s => Wrongs(PlanetTypes.SmallSpire, (uint)s)))
            .Concat(Wrongs(PlanetTypes.First, 1)).Take(10).ToList();
        Assert.True(wrongs.Count == 0, string.Join("\n", wrongs));
    }

    [Fact]
    public void TheFirstPlanetHasBigAndMediumLakesBetweenTheIceAndTheLava()
    {
        var l = PlanetTesting.First(1).Layout;
        var lakes = LakesOf(l);
        Assert.InRange(lakes.Count, 5, 40);
        Assert.Contains(lakes, k => k.Count > PlanetTypes.First.LakeMedium);
        // Each lake's floor (its lowest cell) lies in the garden or the amber deep, or just under it where the floor fell.
        foreach (var lake in lakes)
            Assert.InRange(Terrain.BandOf(PlanetTypes.First, lake.Max() / l.Width), 1, 3);
    }

    [Fact]
    public void TheSmallPlanetHasALakeOnMostSeedsForTheFuzzToMeet()
    {
        int with = Enumerable.Range(1, 50).Count(s => LakesOf(Terrain.Generate(PlanetTypes.Small, (uint)s)).Count > 0);
        Assert.True(with >= 40, $"{with} of 50 seeds grew a lake");
    }

    [Fact]
    public void ALakeLiesStillOnceItsWorldRuns()
    {
        for (uint seed = 1; seed <= 8; seed++)
        {
            var l = Terrain.Generate(PlanetTypes.Small, seed);
            if (l.Liquids == null) continue;
            var w = new Scenario(l).Run(3);
            Assert.True(w.M.Liquids.Layer.SequenceEqual(l.Liquids.ToArray()), $"seed {seed}: the water moved");
            Assert.Equal(0, w.M.Liquids.AwakeChunks);
        }
    }

    [Fact]
    public void LakesGrowTheSameFromTheSameSeedAndNoneWhereTheTypeHasNone()
    {
        Assert.Equal(Terrain.Generate(PlanetTypes.Small, 5).Liquids, Terrain.Generate(PlanetTypes.Small, 5).Liquids);
        Assert.Null(Terrain.Generate(PlanetTypes.Small with { LakeWave = 0 }, 5).Liquids);
    }
}
