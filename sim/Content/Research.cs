namespace Stillwake.Content;

/// <summary>
/// Knowing blueprints and crafts (ADRs 0024, 0027). Blueprints are named as the build menu names its
/// cards: a wall or a back wall by its kind and material (<c>wallIron</c>, <c>backWallScrap</c>),
/// anything else by the grid's name for it (<c>floor</c>, <c>furnace</c>); a craft by the item it makes
/// (<c>wire</c>). Either is known when granted, or once every item of its cost is analysed. Cables are
/// no blueprint and always known.
/// </summary>
public static class Research
{
    /// <summary>An analyzer analyses an item in this many ticks at full power, slower short of it.</summary>
    public const int Ticks = 200;

    /// <summary>What analysis cannot open yet: the solar panel, until electronics exist.</summary>
    public static readonly IReadOnlyList<string> Locked = ["solarPanel"];

    /// <summary>
    /// What the wreck grants from its start: the furnace and the analyzer, the crate, the scrap walls and
    /// back walls a torn room is closed with, the wire its cables are made of, and the fire axe wreckage
    /// is broken with.
    /// </summary>
    public static readonly IReadOnlyList<string> StartKnown = ["furnace", "analyzer", "crate", "wallScrap", "backWallScrap", "wire", "fireAxe"];
}
