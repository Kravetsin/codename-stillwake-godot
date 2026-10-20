using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim.Items;
using CrewSpec = Stillwake.Content.Crew;
using InventorySpec = Stillwake.Content.Inventory;
using ItemSpecs = Stillwake.Content.Items;

namespace Stillwake.Game.Features.Items;

/// <summary>A stack picked up to be put elsewhere: only on screen, the items stay in their slot till then.</summary>
public readonly record struct Picked(Place Place, int Count);

/// <summary>A click on a slot: what it holds now, the hero's slots as shown, and the crate open under the inventory, if any.</summary>
/// <param name="Inventory">The hero's slots as shown, its equipment after its own: where a shift-click wears or takes off.</param>
/// <param name="FuelSlot">The slot the open crate burns fuel in, when it is a furnace.</param>
/// <param name="Charger">Whether the open crate is a charging station: a battery shift-clicked goes there to charge.</param>
/// <param name="Inputs">
/// The open machine's input slots a stack shift-clicked spreads over, its slots as shown in <paramref name="CrateSlots"/>:
/// a queued station's (the Former's two, the assembler's three); 0 for any other, which takes it into its first slot.
/// </param>
public sealed record SlotClick(
    Place Place, Stack? Stack, bool Right, bool Shift, int? Crate, IReadOnlyList<Stack?> Inventory,
    int? FuelSlot = null, bool Charger = false, int Inputs = 0, IReadOnlyList<Stack?>? CrateSlots = null);

/// <summary>What a click leaves picked, the move it sends, if any, and whether it breathes a canister (0047).</summary>
public readonly record struct Clicked(Picked? Picked, Move? Command = null, bool Breathes = false);

/// <summary>The rules of clicks on slots (apps/game/src/features/items/click.ts).</summary>
public static class Clicks
{
    /// <summary>
    /// Terraria's way: a left click picks a whole stack and puts it on the next slot clicked, which swaps
    /// it with another item there; a right click picks half, rounded up, or with a stack picked puts one
    /// of it; a shift-click wears an item of the hero's own that is worn, takes off what is worn, and sends
    /// any other stack to the other side of an open crate, or with none between the bag and the hotbar
    /// (<see cref="Shifted"/>). A click on the picked
    /// slot puts it back. A right click on a canister of the hero's own, nothing picked, breathes it into
    /// the suit (0047) rather than picking half.
    /// </summary>
    public static Clicked On(Picked? picked, SlotClick click)
    {
        var (place, stack) = (click.Place, click.Stack);
        if (click.Shift && !click.Right)
        {
            return stack != null && Shifted(click, stack) is { } to
                ? new Clicked(null, new Move(place, to, stack.Count))
                : new Clicked(picked);
        }
        if (click.Right)
        {
            if (picked is { } p && p.Place != place)
            {
                int left = p.Count - 1;
                return new Clicked(left > 0 ? p with { Count = left } : null, new Move(p.Place, place, 1));
            }
            if (picked == null && place.Crate == null && stack?.Item == CrewSpec.Canister) return new Clicked(null, Breathes: true);
            return new Clicked(stack != null ? new Picked(place, (stack.Count + 1) / 2) : null);
        }
        if (picked is not { } held) return new Clicked(stack != null ? new Picked(place, stack.Count) : null);
        if (held.Place == place) return new Clicked(null);
        return new Clicked(null, new Move(held.Place, place, held.Count));
    }

    /// <summary>
    /// <paramref name="picked"/> while its slot, holding <paramref name="now"/>, still holds the <paramref name="item"/> it was
    /// picked as; emptied by a friend at a crate, or holding another item after a sort or a swap, it is let go.
    /// </summary>
    public static Picked? StillHeld(Picked? picked, string? item, Stack? now) => picked is { } p && now != null && now.Item == item ? p : null;

    /// <summary>
    /// Where a shift-click sends a stack, or nowhere. Quick equip, as Terraria's: an item of the hero's own
    /// that is worn goes onto the first free equipment slot of its kind, or swaps with the first worn there;
    /// what is worn goes onto the first free own slot, the hotbar first. A battery goes into an open charging
    /// station instead. Anything else, and anything in a crate, goes to the other side of an open crate, a
    /// fuel into a furnace's fuel slot, and into a queued station's inputs onto its own kind there, else the first empty
    /// one, else the first, as the sim puts a move naming none. With no machine open, the bag and the hotbar are the
    /// two sides (<see cref="Across"/>).
    /// </summary>
    private static Target? Shifted(SlotClick click, Stack stack)
    {
        var spec = ItemSpecs.Get(stack.Item);
        bool own = click.Place.Crate == null;
        bool charges = click.Charger && spec.Capacity != null;
        if (own && !charges && Equipment.Is(click.Place))
        {
            int free = Enumerable.Range(0, InventorySpec.Slots).FirstOrDefault(i => click.Inventory[i] == null, -1);
            return free >= 0 ? new Target(free) : null;
        }
        if (own && !charges && spec.Equip is string kind)
        {
            var slots = Equipment.Of(kind).ToList();
            var slot = slots.FirstOrDefault(s => click.Inventory[s.Slot] == null) ?? slots.FirstOrDefault();
            return slot != null ? new Target(slot.Slot) : null;
        }
        if (click.Crate is not int crate) return own ? Across(click, stack) : null;
        if (!own) return new Target();
        if (spec.BurnTicks != null && click.FuelSlot != null) return new Target(click.FuelSlot, crate);
        return new Target(InputFor(click, stack), crate);
    }

    /// <summary>
    /// The input slot of the open queued station <paramref name="stack"/> goes to: the first holding its kind with room,
    /// else the first empty one; none, the sim's first slot, for a machine without several or with no room in them.
    /// </summary>
    private static int? InputFor(SlotClick click, Stack stack) =>
        click.Inputs < 2 || click.CrateSlots is not { } slots ? null : RoomFor(slots, 0, click.Inputs, stack);

    /// <summary>
    /// With no machine open, a stack in the bag goes onto the hotbar and one on the hotbar into the bag: onto its own kind
    /// with room there, else the first empty slot; nowhere when that side has neither.
    /// </summary>
    private static Target? Across(SlotClick click, Stack stack)
    {
        bool hotbar = click.Place.Slot < Hotbar.Row;
        int from = hotbar ? Hotbar.Row : 0, to = hotbar ? InventorySpec.Slots : Hotbar.Row;
        return RoomFor(click.Inventory, from, to, stack) is int slot ? new Target(slot) : null;
    }

    /// <summary>The first of <paramref name="slots"/> from <paramref name="from"/> up to <paramref name="to"/> holding a stack alike with room, else the first empty; none for neither.</summary>
    private static int? RoomFor(IReadOnlyList<Stack?> slots, int from, int to, Stack stack)
    {
        int limit = ItemSpecs.Get(stack.Item).Stack;
        for (int i = from; i < to; i++)
            if (slots[i] is { } s && s.Item == stack.Item && s.Charge == stack.Charge && s.Count < limit) return i;
        for (int i = from; i < to; i++)
            if (slots[i] == null) return i;
        return null;
    }

    /// <summary>
    /// Where a stack of <paramref name="item"/> moved out of <paramref name="src"/> landed, as the slots showed it
    /// <paramref name="before"/> and show it <paramref name="after"/> the move: each of <paramref name="shown"/> but the source
    /// that holds more of that item now, in their order. None for a move the sim would refuse: it changes nothing shown.
    /// </summary>
    public static IEnumerable<Place> Landed(
        Place src, string item, Func<Place, Stack?> before, Func<Place, Stack?> after, IEnumerable<Place> shown)
    {
        static int Of(Stack? stack, string item) => stack?.Item == item ? stack.Count : 0;
        return shown.Where(p => p != src && Of(after(p), item) > Of(before(p), item));
    }
}
