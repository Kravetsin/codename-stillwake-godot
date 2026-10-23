using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>Something a hero builds, kept by the name the grid gives it.</summary>
/// <param name="Cost">
/// Whole units of items it takes from its builder, and gives back when taken apart: materials and
/// parts, salvage or planks for what is built before there are any.
/// </param>
/// <param name="BuildTicks">Ticks of a hero's work to build it.</param>
/// <param name="Across">Its piece's cells across, built and taken apart as one (0104): a ladder's two, a door segment's two; 1 for a cell.</param>
/// <param name="Tall">Its piece's cells down: a ladder's two, a door segment's two; 1 for a cell.</param>
public sealed record Build(string Id, IReadOnlyList<(string Item, int Count)> Cost, int BuildTicks, int Across = 1, int Tall = 1);

/// <summary>
/// What a hero builds besides walls and back walls, which cost their material (<see cref="Materials"/>),
/// and machines (<see cref="Machines"/>).
/// </summary>
public static class Builds
{
    public static readonly IReadOnlyList<Build> All =
    [
        new("floor", [("scrap", 1)], BuildTicks: 7),
        // A piece the size of a 16 px ladder cell (0104), costing and taking what one did.
        new("ladder", [("scrap", 1)], BuildTicks: 7, Across: 2, Tall: 2),
        // A door segment, a 16 px door cell (0104), costing and taking what one did; a run of them is one doorway.
        new("door", [("plate", 2), ("gear", 1)], BuildTicks: 20, Across: 2, Tall: 2),
    ];

    /// <summary>Taking a thing apart is this many times faster than building it; its ticks round up.</summary>
    public const int DeconstructSpeedup = 2;

    /// <summary>
    /// Ticks a crack in natural ground lasts with no hit (0077): ten seconds, then it heals whole, as Terraria's. A dig left
    /// half done is begun again.
    /// </summary>
    public const int CrackHeals = 200;

    /// <summary>
    /// The side, in cells, of the square a strike or a build stroke covers (the client's brush): 2 at 8 px cells, so
    /// digging and building go as fast per area as one 16 px cell did; 1 at 16 px. Ctrl held strikes or builds one cell.
    /// </summary>
    public const int Brush = 2;

    /// <summary>
    /// The cells across and down of a piece of <paramref name="kind"/>, placed and taken apart as one (0104): a ladder's 2×2, a
    /// door segment's 2×2; null for what is placed a cell at a time.
    /// </summary>
    public static (int Across, int Tall)? PieceOf(string kind)
    {
        if (!ById.TryGetValue(kind, out var build) || build.Across * build.Tall == 1) return null;
        return (build.Across, build.Tall);
    }

    private static readonly Dictionary<string, Build> ById = All.ToDictionary(b => b.Id, StringComparer.Ordinal);

    /// <summary>The build by its id; throws for an id that is none.</summary>
    public static Build Get(string id) => ById[id];

    public static bool TryGet(string id, [MaybeNullWhen(false)] out Build build) => ById.TryGetValue(id, out build);
}
