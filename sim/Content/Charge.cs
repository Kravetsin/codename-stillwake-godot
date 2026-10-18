namespace Stillwake.Content;

/// <summary>
/// Charge, in the units machines draw power in (0053): a power-tick is one unit of power drawn for a
/// tick, so a machine drawing 3 for 600 ticks uses 1800. A store declares a <c>Capacity</c> (a battery),
/// a consumer a <c>Draw</c> (an electric tool), both in these units; how long a store lasts any one
/// consumer is never shown.
/// </summary>
public static class Charge
{
    /// <summary>
    /// Stored charge is in thousandths of a power-tick: a charging station's share of power, in
    /// thousandths, adds whole numbers.
    /// </summary>
    public const int Scale = 1000;

    /// <summary>What a battery holds full, stored: 1800 power-ticks, a draw of 3 for 600 ticks.</summary>
    public const int Battery = 1800 * Scale;

    /// <summary>What a drone holds full, stored: 1800 power-ticks, its own battery's, flown at <c>Logistics.Draw</c> a tick (0062).</summary>
    public const int Drone = 1800 * Scale;

    // Not a const: every constant of this table is a whole number (ContentTests).
    /// <summary>The kind of equipment slot whose batteries an electric tool drains, in slot order.</summary>
    public static readonly string Belt = "belt";
}
