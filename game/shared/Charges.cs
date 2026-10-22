using System;
using System.Globalization;
using System.Linq;
using System.Text;
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

/// <summary>
/// The energy tab by the suit's gauge (0081): what the belt's stores hold, every one summed, as a <paramref name="Share"/> of
/// <see cref="Charges.BeltMost"/>, a belt full of the largest stores, and in whole power <paramref name="Units"/>;
/// <paramref name="Low"/> under a quarter of what those worn hold full, as a slot's bar is; <paramref name="Worn"/> while any
/// store is on the belt.
/// </summary>
public sealed record Belt(double Share, int Units, bool Low, bool Worn);

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

    /// <summary>
    /// Whether a station's battery has just come full by charging: charging at the read before, short of full then and
    /// full now. Put in full, or swapped for a full one while it stood unpowered, it has not.
    /// </summary>
    public static bool CameFull(bool wasCharging, float wasShare, float share) => wasCharging && wasShare < 1 && share >= 1;

    /// <summary>What a charging station's panel says it does: empty, full, charging, or held without power.</summary>
    public static string StationState(Stack? held, bool charging) =>
        held == null ? "charger.empty"
        : CapacityOf(held.Item) is int full && held.Charge >= full ? "charger.full"
        : charging ? "charger.charging"
        : "charger.noPower";

    /// <summary>How many stores the belt wears at most: batteries, accumulators (0099).</summary>
    public static readonly int BeltSlots = Spec.Inventory.Equipment.Count(e => e == Spec.Charge.Belt);

    /// <summary>
    /// What a belt full of the largest stores worn there holds full, in power units (three accumulators, 16 200): what the
    /// energy tab's share is of and its number counts up to, the same whatever is worn, so wearing another store never moves it.
    /// </summary>
    public static readonly int BeltMost = BeltSlots * Spec.Items.All.Where(i => i.Equip == Spec.Charge.Belt && i.Capacity != null).Max(i => i.Capacity!.Value / Spec.Charge.Scale);

    /// <summary>
    /// The energy tab from the belt's charge and what its stores hold full, <paramref name="belt"/> as
    /// <c>BeltCharge</c> sums it. Shown always, whatever is held, so the gauge row never changes width.
    /// </summary>
    public static Belt BeltOf((int Charge, int Capacity) belt)
    {
        int charge = Math.Max(0, belt.Charge);
        double share = (double)charge / ((long)BeltMost * Spec.Charge.Scale);
        bool worn = belt.Capacity > 0;
        // Units rounded down, as the card's meter: never more than is there.
        return new Belt(Math.Clamp(share, 0, 1), charge / Spec.Charge.Scale, worn && BarOf(charge, belt.Capacity) != ChargeBar.Good, worn);
    }

    /// <summary>The number the energy tab shows for <paramref name="share"/> of <see cref="BeltMost"/>, as its motion trails it.</summary>
    public static int UnitsOf(double share) => (int)Math.Round(share * BeltMost);

    /// <summary>The thin space between each three digits of the energy tab's number: <c>4 300</c>.</summary>
    public const char Thin = ' ';

    /// <summary><paramref name="units"/> with its thousands set apart by a <see cref="Thin"/> space; never negative.</summary>
    public static string Grouped(int units)
    {
        var digits = Math.Max(0, units).ToString(CultureInfo.InvariantCulture);
        var grouped = new StringBuilder();
        for (int i = 0; i < digits.Length; i++)
        {
            if (i > 0 && (digits.Length - i) % 3 == 0) grouped.Append(Thin);
            grouped.Append(digits[i]);
        }
        return grouped.ToString();
    }
}
