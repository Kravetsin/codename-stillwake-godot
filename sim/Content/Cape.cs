namespace Stillwake.Content;

/// <summary>
/// The cape (2026-10-27-the-cape-is-worn), worn in a slot of its own beside the jetpack: it spares <see cref="Guard"/>
/// thousandths of every enemy's blow, the rest of a unit kept for the next, and lowers the coldest its wearer withstands by
/// <see cref="Warmth"/> degrees (2026-11-14-temperature-in-degrees).
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

    /// <summary>The degrees it adds to the cold its wearer withstands; nothing off the field's edge, which is no weather.</summary>
    public const int Warmth = 5;
}
