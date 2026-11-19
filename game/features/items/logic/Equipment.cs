using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
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
    public static bool Is(Place place) => place.Crate == null && place.Slot >= Spec.Inventory.Slots && place.Slot < Spec.Inventory.Trash;

    /// <summary>The kind of equipment slot <paramref name="place"/> is (head, back, belt); none for any other slot.</summary>
    public static string? KindOf(Place place) => Is(place) ? Spec.Inventory.Equipment[place.Slot - Spec.Inventory.Slots] : null;

    /// <summary>The equipment slots of one <paramref name="kind"/>, in slot order.</summary>
    public static IEnumerable<Place> Of(string kind)
    {
        for (int i = 0; i < Spec.Inventory.Equipment.Count; i++)
            if (Spec.Inventory.Equipment[i] == kind) yield return new Place(Spec.Inventory.Slots + i);
    }

    /// <summary>Every equipment slot, in slot order.</summary>
    public static IEnumerable<Place> All()
    {
        for (int i = 0; i < Spec.Inventory.Equipment.Count; i++) yield return new Place(Spec.Inventory.Slots + i);
    }

    /// <summary>
    /// Where equipment slot <paramref name="place"/> is by the doll (gear-tabs): each kind by the part it dresses, the belt's
    /// slots in a row in slot order.
    /// </summary>
    public static DollSpot SpotOf(Place place)
    {
        var kind = KindOf(place) ?? throw new System.ArgumentException($"{place} is no equipment slot", nameof(place));
        if (kind == "belt") return DollSpot.Belt1 + Of(kind).TakeWhile(p => p != place).Count();
        return kind == Spec.Helmet.Slot ? DollSpot.Head
            : kind == Spec.Suit.Slot ? DollSpot.Suit
            : kind == Spec.Cape.Slot ? DollSpot.Cape
            : kind == "back" ? DollSpot.Back
            : throw new System.ArgumentException($"No place by the doll for {kind}", nameof(place));
    }

    /// <summary>
    /// Whether the equipment's screen shows the Appearance tab (gear-tabs): while it was chosen, until a stack is lifted from any
    /// slot, which turns it to the gear so a suit is never carried into the cosmetics.
    /// </summary>
    public static bool LooksShown(bool chosen, bool carrying) => chosen && !carrying;

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
