using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>Something a hero builds, kept by the name the grid gives it.</summary>
/// <param name="Cost">
/// Whole units of items it takes from its builder, and gives back when taken apart: materials and
/// parts, salvage or planks for what is built before there are any.
/// </param>
/// <param name="BuildTicks">Ticks of a hero's work to build it.</param>
public sealed record Build(string Id, IReadOnlyList<(string Item, int Count)> Cost, int BuildTicks);

/// <summary>
/// What a hero builds besides walls and back walls, which cost their material (<see cref="Materials"/>),
/// and machines (<see cref="Machines"/>).
/// </summary>
public static class Builds
{
    public static readonly IReadOnlyList<Build> All =
    [
        new("floor", [("scrap", 1)], BuildTicks: 7),
        new("ladder", [("scrap", 1)], BuildTicks: 7),
        new("door", [("plate", 2), ("gear", 1)], BuildTicks: 20),
        new("pipe", [("scrap", 1)], BuildTicks: 7),
        new("valve", [("scrap", 1), ("gear", 1)], BuildTicks: 40),
    ];

    /// <summary>Taking a thing apart is this many times faster than building it; its ticks round up.</summary>
    public const int DeconstructSpeedup = 2;

    private static readonly Dictionary<string, Build> ById = All.ToDictionary(b => b.Id, StringComparer.Ordinal);

    /// <summary>The build by its id; throws for an id that is none.</summary>
    public static Build Get(string id) => ById[id];

    public static bool TryGet(string id, [MaybeNullWhen(false)] out Build build) => ById.TryGetValue(id, out build);
}
