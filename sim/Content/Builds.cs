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
        // A cell of a doorway, six cells now (0104): a plate a cell, its six what three cells took; a gear a cell, three more than
        // it took, for a gear cannot be halved; as long to build.
        new("door", [("plate", 1), ("gear", 1)], BuildTicks: 10),
        new("pipe", [("tube", 1)], BuildTicks: 7),
        new("valve", [("tube", 1), ("gear", 1)], BuildTicks: 40),
    ];

    /// <summary>Taking a thing apart is this many times faster than building it; its ticks round up.</summary>
    public const int DeconstructSpeedup = 2;

    /// <summary>
    /// Ticks a crack in natural ground lasts with no hit (0077): ten seconds, then it heals whole, as Terraria's. A dig left
    /// half done is begun again.
    /// </summary>
    public const int CrackHeals = 200;

    private static readonly Dictionary<string, Build> ById = All.ToDictionary(b => b.Id, StringComparer.Ordinal);

    /// <summary>The build by its id; throws for an id that is none.</summary>
    public static Build Get(string id) => ById[id];

    public static bool TryGet(string id, [MaybeNullWhen(false)] out Build build) => ById.TryGetValue(id, out build);
}
