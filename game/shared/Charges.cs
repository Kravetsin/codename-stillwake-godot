using System;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Shared;

/// <summary>How a slot's thin bar shows what a battery holds (0053): green, amber under a quarter, empty at none.</summary>
public enum ChargeBar
{
    Good,
    Low,
    Flat,
}

/// <summary>
/// A battery's CHARGE meter on its card: the share of full in whole percent, and what it holds and holds
/// full in power-ticks, the units machines draw in. Never how long any one tool would run on it.
/// </summary>
public sealed record ChargeMeter(int Percent, int Stored, int Capacity, float Share);

/// <summary>The energy bar by the suit's: what the belt's batteries hold of full, in the colour of the energy what is held runs on.</summary>
public sealed record Energy(float Share, string Colour);

/// <summary>
/// What the client shows of charge (0053), from the stacks the sim keeps and content: a slot's bar, the
/// empty battery's picture, the card's meter, a charging station's state and the energy bar. No rule of
/// its own: the sim decides what holds charge and how much.
/// </summary>
public static class Charges
{
    /// <summary>Charge's colour, "rrggbb": a slot's bar, the meter, the station's, electric energy's.</summary>
    public const string Green = "7ee36b";

    /// <summary>A slot's bar under a quarter of full.</summary>
    public const string Amber = "f2b84b";

    /// <summary>The most a battery of <paramref name="item"/> holds; null for an item that holds no charge.</summary>
    public static int? CapacityOf(string item) => Spec.Items.Get(item).Capacity;

    /// <summary>What <paramref name="stack"/> holds of full, 0 to 1; 0 for an item that holds no charge.</summary>
    public static float Share(Stack stack) =>
        CapacityOf(stack.Item) is int full ? Math.Clamp((float)stack.Charge / full, 0, 1) : 0;

    /// <summary>How a slot's bar shows <paramref name="charge"/> of <paramref name="capacity"/>: amber below a quarter, empty at none.</summary>
    public static ChargeBar BarOf(int charge, int capacity) =>
        charge <= 0 ? ChargeBar.Flat : charge * 4 < capacity ? ChargeBar.Low : ChargeBar.Good;

    /// <summary>The bar of <paramref name="stack"/>; null for an item that holds no charge, which has none.</summary>
    public static ChargeBar? BarOf(Stack stack) => CapacityOf(stack.Item) is int full ? BarOf(stack.Charge, full) : null;

    /// <summary>The picture of <paramref name="item"/> holding <paramref name="charge"/>: an empty battery shows <c>battery_empty</c>.</summary>
    public static string Picture(string item, int charge) => CapacityOf(item) != null && charge <= 0 ? $"{item}_empty" : item;

    /// <summary>The card's meter for <paramref name="stack"/>: whole percent and whole power-ticks, rounded down; null with no charge to hold.</summary>
    public static ChargeMeter? MeterOf(Stack stack)
    {
        if (CapacityOf(stack.Item) is not int full) return null;
        int charge = Math.Clamp(stack.Charge, 0, full);
        // Both are never negative, so integer division rounds down: 100% only when full.
        return new ChargeMeter(
            (int)(100L * charge / full),
            charge / Spec.Charge.Scale,
            full / Spec.Charge.Scale,
            (float)charge / full);
    }

    /// <summary>What a charging station's panel says it does: empty, full, charging, or held without power.</summary>
    public static string StationState(Stack? held, bool charging) =>
        held == null ? "charger.empty"
        : CapacityOf(held.Item) is int full && held.Charge >= full ? "charger.full"
        : charging ? "charger.charging"
        : "charger.noPower";

    /// <summary>The colour of the energy <paramref name="item"/> runs on, held: charge's green for an electric tool; null for one that runs on none.</summary>
    public static string? ColourOf(string item, Func<string, bool>? electric = null) =>
        (electric ?? (i => Spec.Items.Get(i).Draw != null))(item) ? Green : null;

    /// <summary>
    /// The energy bar while <paramref name="held"/> runs on energy, null otherwise: the belt's charge,
    /// <paramref name="belt"/> as <c>BeltCharge</c> sums it, of what it holds full; empty with no battery worn.
    /// <paramref name="electric"/> says what is electric, what has a <c>Draw</c> in content when not given.
    /// </summary>
    public static Energy? EnergyOf(string? held, (int Charge, int Capacity) belt, Func<string, bool>? electric = null)
    {
        if (held == null || ColourOf(held, electric) is not { } colour) return null;
        float share = belt.Capacity > 0 ? Math.Clamp((float)belt.Charge / belt.Capacity, 0, 1) : 0;
        return new Energy(share, colour);
    }
}
