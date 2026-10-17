namespace Stillwake.Content;

/// <summary>
/// Charge, held by an item that declares a <c>Capacity</c> (a battery) and spent by an electric tool, in
/// thousandths of a tick of that tool's work: a charger's share of power, in thousandths, adds whole units.
/// </summary>
public static class Charge
{
    /// <summary>What a tick of an electric tool's work takes: a drill drilling.</summary>
    public const int PerTick = 1000;

    /// <summary>What a battery holds full: 600 ticks of drilling, thirty seconds.</summary>
    public const int Battery = 600 * PerTick;

    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The kind of equipment slot whose batteries an electric tool drains, in slot order.</summary>
    public static readonly string Belt = "belt";
}
