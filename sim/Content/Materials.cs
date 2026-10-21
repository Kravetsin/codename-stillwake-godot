using System.Diagnostics.CodeAnalysis;

namespace Stillwake.Content;

/// <summary>What a wall or a back wall is made of: a hull material, or a planet's natural ground (0077).</summary>
/// <param name="Code">Stored in saves instead of the name: never reuse or renumber a code.</param>
/// <param name="Strength">
/// A natural wall's hit points: the damage of weapons' hits (0054) that breaks it out (0077). <see cref="Unbreakable"/>
/// for one nothing breaks, bedrock. Unused for hull, which is taken apart with marks.
/// </param>
/// <param name="Leak">
/// Oxygen units lost every <see cref="Air.LeakEvery"/> ticks (ten minutes) through one cell of it that an
/// aired room's air touches: a back wall behind the room, or a wall of its hull.
/// </param>
/// <param name="Cost">
/// Whole units of items one cell of it takes: scrap itself, iron as plates; what a natural wall yields when it breaks.
/// None for one never built, bedrock.
/// </param>
/// <param name="BuildTicks">Ticks of a hero's work to build one cell; 0 for one never built.</param>
/// <param name="Natural">
/// A planet's ground (0077): dug by weapons, never marked to take apart; a wall of it may be planned, a back wall not, for
/// nothing would take one down again.
/// </param>
public sealed record Material(
    string Id, int Code, int Strength, int Leak, IReadOnlyList<(string Item, int Count)> Cost, int BuildTicks, bool Natural = false)
{
    /// <summary>The <see cref="Strength"/> of a material nothing breaks.</summary>
    public const int Unbreakable = 0;

    /// <summary>Whether weapons break a wall of it (0077): natural and not <see cref="Unbreakable"/>.</summary>
    public bool Breaks => Natural && Strength != Unbreakable;

    /// <summary>Whether a wall of it may be planned and built: it costs something.</summary>
    public bool Built => Cost.Count > 0;
}

/// <summary>Hull materials, weakest first, then the natural ones.</summary>
public static class Materials
{
    public static readonly IReadOnlyList<Material> All =
    [
        new("scrap", Code: 1, Strength: 2, Leak: 30, Cost: [("scrap", 2)], BuildTicks: 13),
        new("iron", Code: 2, Strength: 5, Leak: 24, Cost: [("plate", 1)], BuildTicks: 27),
        // A planet's ground (0077), leakier than any hull. Soil breaks in two swings of the axe, stone in six; a shovelful
        // of soil and a lump of rock, the asteroids' own, come out of it, and build it back.
        new("soil", Code: 3, Strength: 4, Leak: 40, Cost: [("soil", 1)], BuildTicks: 10, Natural: true),
        new("stone", Code: 4, Strength: 12, Leak: 36, Cost: [("rock", 1)], BuildTicks: 20, Natural: true),
        new("bedrock", Code: 5, Strength: Material.Unbreakable, Leak: 36, Cost: [], BuildTicks: 0, Natural: true),
        // The ruins' metal (0085): five times stone, four seconds of the arc drill, six of the drill; a scrap of it a cell.
        new("alloy", Code: 6, Strength: 60, Leak: 36, Cost: [("alloyScrap", 1)], BuildTicks: 40, Natural: true),
    ];

    /// <summary>What a <c>#</c> on a map is built of.</summary>
    public const string DefaultHull = "scrap";

    private static readonly Dictionary<string, Material> ById = All.ToDictionary(m => m.Id, StringComparer.Ordinal);

    /// <summary>The material by its id; throws for an id that is none.</summary>
    public static Material Get(string id) => ById[id];

    public static bool TryGet(string id, [MaybeNullWhen(false)] out Material material) => ById.TryGetValue(id, out material);
}
