namespace Stillwake.Content;

/// <summary>
/// Liquid in the cells of a surface (docs/plans/liquids.md): each cell holds one kind, up to <see cref="Full"/> units. A cell
/// of the layer is a byte, its amount in the low four bits and its kind in the three over them (<see cref="Of"/>).
/// </summary>
public static class Liquids
{
    /// <summary>No liquid: a cell holding none is of no kind.</summary>
    public const byte None = 0;

    /// <summary>Water, found in lakes in the caves.</summary>
    public const byte Water = 1;

    /// <summary>
    /// What a full cell holds: a unit a pixel of its 8. More levels never settle: a body spreading at 240 a cell crept on
    /// for 30,000 ticks in the measure (the plan's Scale).
    /// </summary>
    public const int Full = 8;

    /// <summary>The most cells liquid falls in a tick through an empty shaft: 80 cells a second.</summary>
    public const int Fall = 4;

    /// <summary>
    /// The most units a body moves a tick to level itself (<c>LiquidsModule.Press</c>): a cell a tick, 20 a second, up a U-tube's far side.
    /// </summary>
    public const int Press = 8;

    /// <summary>The side of a square of cells that sleeps or wakes as one, when none of it can move.</summary>
    public const int Chunk = 16;

    /// <summary>A cell's byte: <paramref name="amount"/> of <paramref name="kind"/>, or none.</summary>
    public static byte Of(byte kind, int amount) => amount <= 0 ? None : (byte)(amount | kind << 4);

    /// <summary>The amount a cell's byte holds.</summary>
    public static int AmountOf(byte cell) => cell & 15;

    /// <summary>The kind a cell's byte holds, <see cref="None"/> for none.</summary>
    public static byte KindOf(byte cell) => (byte)(cell >> 4 & 7);
}
