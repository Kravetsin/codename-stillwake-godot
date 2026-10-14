namespace Stillwake.Content;

/// <summary>
/// Knowing blueprints (ADR 0024). Blueprints are named as the build menu names its cards: a wall or a
/// back wall by its kind and material (<c>wallIron</c>, <c>backWallScrap</c>), anything else by the
/// grid's name for it (<c>floor</c>, <c>furnace</c>). Cables are no blueprint and always known.
/// </summary>
public static class Research
{
    /// <summary>An analyzer learns what an item teaches in this many ticks at full power, slower short of it.</summary>
    public const int Ticks = 200;

    /// <summary>What analysing a material teaches: the blueprints it goes into, but those already known.</summary>
    public static readonly IReadOnlyList<(string Item, IReadOnlyList<string> Blueprints)> Unlocks =
    [
        ("scrap", ["floor", "ladder", "pipe"]),
        ("iron", ["wallIron", "backWallIron", "door", "valve"]),
    ];

    /// <summary>The machines schematics teach, in order: each schematic analysed, the first not yet known.</summary>
    public static readonly IReadOnlyList<string> Schematics = ["solarPanel", "vent", "lamp", "tank", "generator"];

    /// <summary>
    /// What the wreck knows from its start: the furnace and the analyzer, the crate, and the scrap walls
    /// and back walls a torn room is closed with.
    /// </summary>
    public static readonly IReadOnlyList<string> StartKnown = ["furnace", "analyzer", "crate", "wallScrap", "backWallScrap"];
}
