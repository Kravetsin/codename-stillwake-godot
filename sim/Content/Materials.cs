using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>A hull material.</summary>
/// <param name="Code">Stored in saves instead of the name: never reuse or renumber a code.</param>
/// <param name="Strength">Hits from meteors a hull cell takes before it breaks.</param>
/// <param name="Leak">
/// Oxygen units lost every <see cref="Air.LeakEvery"/> ticks (ten minutes) through one cell of it that an
/// aired room's air touches: a back wall behind the room, or a wall of its hull.
/// </param>
/// <param name="Cost">Whole units of items one cell of it takes: scrap itself, iron as plates.</param>
/// <param name="BuildTicks">Ticks of a hero's work to build one cell.</param>
public sealed record Material(string Id, int Code, int Strength, int Leak, IReadOnlyList<(string Item, int Count)> Cost, int BuildTicks);

/// <summary>Hull materials, weakest first.</summary>
public static class Materials
{
    public static readonly IReadOnlyList<Material> All =
    [
        new("scrap", Code: 1, Strength: 2, Leak: 30, Cost: [("scrap", 2)], BuildTicks: 13),
        new("iron", Code: 2, Strength: 5, Leak: 24, Cost: [("plate", 1)], BuildTicks: 27),
    ];

    /// <summary>What a <c>#</c> on a map is built of.</summary>
    public const string DefaultHull = "scrap";

    private static readonly Dictionary<string, Material> ById = All.ToDictionary(m => m.Id, StringComparer.Ordinal);

    /// <summary>The material by its id; throws for an id that is none.</summary>
    public static Material Get(string id) => ById[id];

    public static bool TryGet(string id, [MaybeNullWhen(false)] out Material material) => ById.TryGetValue(id, out material);
}
