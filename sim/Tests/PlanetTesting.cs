using System.Collections.Concurrent;
using Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The first planet grown once per seed, for the tests that only read it: a third of a second to grow, read hundreds of times a run.
/// Each stays held until the run ends, 38 MB, so keep to the seeds already asked for, 1 to 50.
/// </summary>
public static class PlanetTesting
{
    /// <summary>A planet as <see cref="Terrain"/> grew it, shared by every test that asks: read-only by its types.</summary>
    public sealed record Grown(Layout Layout, IReadOnlyList<int> Surface, IReadOnlyList<(int X, int Y)> Spires, IReadOnlyList<(int X, int Y, Ruins.Home Home)> Homes);

    private static readonly ConcurrentDictionary<uint, Lazy<Grown>> Firsts = new();

    /// <summary>The first planet of <paramref name="seed"/>: grown by the first test to ask, any other asking meanwhile waiting for it.</summary>
    public static Grown First(uint seed) => Firsts.GetOrAdd(seed, s => new(() =>
    {
        var l = Terrain.Generate(PlanetTypes.First, s, out _, out var surface, out var spires, out var homes);
        return new(l, surface, spires, homes);
    })).Value;

    /// <summary><paramref name="t"/> with no ore pockets: its lode veins alone, the ground, caves and growths as they are.</summary>
    public static PlanetType WithoutPockets(PlanetType t) => t with { Veins = [.. t.Veins.Select(v => v with { Pockets = null })] };

    private static readonly Lazy<Layout> Lodes = new(() => Terrain.Generate(WithoutPockets(PlanetTypes.First), 1));

    /// <summary>The first planet of seed 1 with no ore pockets, for the tests of its lode veins alone.</summary>
    public static Layout FirstLodesOnly => Lodes.Value;
}
