using System.Collections.Generic;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Items;

/// <summary>How an equipment slot shows while a stack is carried (0047).</summary>
public enum SlotGlow
{
    /// <summary>Nothing carried, or not an equipment slot: as it is.</summary>
    None,
    /// <summary>It would wear what is carried: lit.</summary>
    Takes,
    /// <summary>It would not: dimmed.</summary>
    Refuses,
}

/// <summary>
/// The hero's equipment slots, after its own in the inventory (0047): which of them is which, and how one
/// shows while a stack is carried. The sim decides what each wears (<c>items.notEquipment</c>); this only
/// says it ahead, from the same content.
/// </summary>
public static class Equipment
{
    /// <summary>Whether <paramref name="place"/> is one of this hero's equipment slots.</summary>
    public static bool Is(Place place) => place.Crate == null && place.Slot >= Spec.Inventory.Slots && place.Slot < Spec.Inventory.Total;

    /// <summary>The kind of equipment slot <paramref name="place"/> is (head, back, belt); none for any other slot.</summary>
    public static string? KindOf(Place place) => Is(place) ? Spec.Inventory.Equipment[place.Slot - Spec.Inventory.Slots] : null;

    /// <summary>
    /// The kinds of equipment slot as the panel lays them out, a row each by where they are worn: the head and what is fixed
    /// onto the helmet; the body, the back and the cape; the belt. Every kind once.
    /// </summary>
    public static readonly IReadOnlyList<IReadOnlyList<string>> Rows =
        [[Spec.Helmet.Slot, Spec.Helmet.Mount], [Spec.Suit.Slot, "back", Spec.Cape.Slot], ["belt"]];

    /// <summary>The equipment slots of one <paramref name="kind"/>, in slot order.</summary>
    public static IEnumerable<Place> Of(string kind)
    {
        for (int i = 0; i < Spec.Inventory.Equipment.Count; i++)
            if (Spec.Inventory.Equipment[i] == kind) yield return new Place(Spec.Inventory.Slots + i);
    }

    /// <summary>
    /// How <paramref name="place"/> shows while <paramref name="carried"/> is in hand or dragged: an equipment
    /// slot of the kind it is worn in lit, any other equipment slot dimmed; nothing carried, or another slot, as it is.
    /// </summary>
    public static SlotGlow GlowOf(Place place, string? carried)
    {
        if (carried == null || KindOf(place) is not { } kind) return SlotGlow.None;
        return Spec.Items.Get(carried).Equip == kind ? SlotGlow.Takes : SlotGlow.Refuses;
    }
}
