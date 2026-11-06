using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Energy;

/// <summary>
/// What the energy tab reads four times a second (energy-ring): the belt summed, each open belt slot's store (its charge and
/// what it holds full, in stored units, null for a slot without one) and what the tool running draws a tick, 0 for none.
/// </summary>
public sealed record EnergyRead(Belt Belt, IReadOnlyList<(int Charge, int Capacity)?> Stores, int Draw)
{
    /// <summary>The hero's belt slots, in slot order: the stores an electric tool drains, left to right.</summary>
    public static readonly IReadOnlyList<Place> BeltSlots =
        [.. Enumerable.Range(0, Spec.Inventory.Equipment.Count).Where(i => Spec.Inventory.Equipment[i] == Spec.Charge.Belt).Select(i => new Place(Spec.Inventory.Slots + i))];

    /// <summary>
    /// The read of a hero from its <paramref name="inventory"/>, the belt as <c>BeltCharge</c> sums it, the slots it has open
    /// and the item it is <paramref name="running"/>, as the sim's queries give them.
    /// </summary>
    public static EnergyRead Of(IReadOnlyList<Stack?> inventory, (int Charge, int Capacity) belt, Func<Place, bool> open, string? running)
    {
        var slots = BeltSlots.Where(open).ToList();
        var stores = slots.Select(p => inventory[p.Slot] is { } s && Charges.CapacityOf(s.Item) is int full ? (s.Charge, full) : ((int, int)?)null).ToList();
        int draw = running is null ? 0 : Spec.Items.Get(running).Draw ?? 0;
        return new EnergyRead(Charges.BeltOf(belt, slots.Count), stores, draw);
    }
}
