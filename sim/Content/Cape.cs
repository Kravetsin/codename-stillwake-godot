namespace Stillwake.Content;

/// <summary>
/// The cape (2026-10-27-the-cape-is-worn), worn in a slot of its own beside the jetpack: it spares <see cref="Guard"/>
/// thousandths of every enemy's blow and <see cref="Warmth"/> thousandths of the cold, the rest of a unit kept for the next.
/// Made of cloth at the Former.
/// </summary>
public static class Cape
{
    // Not consts: every constant of this table is a whole number (ContentTests).
    /// <summary>The item that is the cape.</summary>
    public static readonly string Item = "cape";

    /// <summary>The kind of equipment slot it is worn in, of <see cref="Inventory.Equipment"/>.</summary>
    public static readonly string Slot = "cape";

    /// <summary>Thousandths of a creature's ram, pounce or bolt it spares: a fifth.</summary>
    public const int Guard = 200;

    /// <summary>Thousandths of the cold it spares, the edge's and the bare cold alike: a fifth.</summary>
    public const int Warmth = 200;
}
