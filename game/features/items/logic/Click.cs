using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;
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
/// <param name="Free">The modules the open machine has an empty socket for (0086): one shifted from the hero's slots is fitted.</param>
/// <param name="Open">Whether a slot of the hero's is open yet (2026-10-30-room-upgrades); none given, all are.</param>
public sealed record SlotClick(
    Place Place, Stack? Stack, bool Right, bool Shift, int? Crate, IReadOnlyList<Stack?> Inventory,
    int? FuelSlot = null, bool Charger = false, int Inputs = 0, IReadOnlyList<Stack?>? CrateSlots = null, IReadOnlyList<string>? Free = null,
    Func<Place, bool>? Open = null)
{
    /// <summary>
    /// A click on the slot at <paramref name="place"/> of what <paramref name="reading"/> shows, the machine it shows open as
    /// <paramref name="crate"/>: its fuel slot, whether it charges, the inputs a queued station spreads over, its free sockets.
    /// </summary>
    public static SlotClick Of(Reading reading, Place place, bool right, bool shift, int? crate)
    {
        var spec = reading.Kind is string kind && Content.Machines.TryGet(kind, out var s) ? s : null;
        int inputs = reading.Kind is string queued && Content.Stations.TryGet(queued, out var station) && !station.Fed ? spec?.Inputs ?? 0 : 0;
        return new SlotClick(
            place, reading.At(place), right, shift, crate, reading.Inventory, spec?.FuelSlot, spec?.Charges == true, inputs, reading.Crate,
            crate != null ? reading.Free : null, reading.Open);
    }
}

/// <summary>A press on machine <paramref name="Machine"/>'s socket for module <paramref name="Module"/> (0086), as shown.</summary>
/// <param name="Fitted">Whether the module is fitted there.</param>
/// <param name="Carried">What the stack picked holds, as shown; null with none picked.</param>
/// <param name="Inventory">The hero's slots as shown: where a module taken out goes.</param>
public sealed record SocketClick(int Machine, string Module, bool Fitted, bool Shift, Stack? Carried, IReadOnlyList<Stack?> Inventory, Func<Place, bool>? Open = null);

/// <summary>One end of a stack's flight (<see cref="Flight"/>): a slot, or a machine's module socket.</summary>
public abstract record Spot;

/// <summary>The slot at <paramref name="Place"/>.</summary>
public sealed record SlotSpot(Place Place) : Spot;

/// <summary>Machine <paramref name="Machine"/>'s socket for module <paramref name="Module"/> (0086).</summary>
public sealed record SocketSpot(int Machine, string Module) : Spot;

/// <summary>
/// A stack sent with shift flying from where it was to where it went (0075): <paramref name="To"/> a slot or a socket,
/// or null for wherever the slots show more of it after the move (<see cref="Clicks.Landed"/>).
/// </summary>
public sealed record Flight(Spot From, Spot? To = null);

/// <summary>
/// What a click leaves picked, the command it sends, if any: a move, a module fitted or taken out (0086); whether it
/// breathes a canister (0047); where a stack sent with shift flies (<see cref="Flight"/>), which every shift that sends one
/// has; whether it begins taking a socket's module out by hand.
/// </summary>
public readonly record struct Clicked(Picked? Picked, Command? Command = null, bool Breathes = false, Flight? Flight = null, bool TakesOut = false);

/// <summary>The rules of clicks on slots (apps/game/src/features/items/click.ts).</summary>
public static class Clicks
{
    /// <summary>
    /// Terraria's way: a left click picks a whole stack and puts it on the next slot clicked, which swaps
    /// it with another item there; a right click picks half, rounded up, or with a stack picked puts one
    /// of it; a shift-click fits a module into a free socket of the open machine (0086), wears an item of the hero's own
    /// that is worn, takes off what is worn, and sends any other stack to the other side of an open crate, or with none
    /// between the bag and the hotbar (<see cref="Shifted"/>), the stack flying there. A click on the picked
    /// slot puts it back. A right click on a canister of the hero's own, nothing picked, breathes it into
    /// the suit (0047) rather than picking half.
    /// </summary>
    public static Clicked On(Picked? picked, SlotClick click) => Flying(click.Shift && !click.Right, Click(picked, click), click.Stack?.Item);

    /// <summary>
    /// A press on a socket (0086): a module carried that it takes is fitted, put down as on a slot (with shift flying from the
    /// slot it was picked from, as every shift that sends a stack does); a fitted one is taken out
    /// with shift at once into the first empty slot of the hero's own, the hotbar first, flying there, and without it
    /// begins to come out by hand (<see cref="Clicked.TakesOut"/>).
    /// </summary>
    public static Clicked OnSocket(Picked? picked, SocketClick click) => Flying(click.Shift, Socket(picked, click), click.Carried?.Item ?? click.Module);

    private static Clicked Socket(Picked? picked, SocketClick click)
    {
        if (picked is { } held)
            return click.Carried?.Item == click.Module && !click.Fitted ? new Clicked(null, new Fit(held.Place, click.Machine)) : new Clicked(picked);
        if (!click.Fitted) return new Clicked(null);
        return click.Shift ? TakeOut(click.Machine, click.Module, click.Inventory, click.Open) : new Clicked(null, TakesOut: true);
    }

    /// <summary>
    /// Module <paramref name="module"/> taken out of machine <paramref name="machine"/> into the first empty slot of
    /// <paramref name="inventory"/>, the hotbar first, flying there: a socket shift-clicked, or clicked and let go on itself.
    /// Nothing with no slot empty.
    /// </summary>
    public static Clicked TakeOut(int machine, string module, IReadOnlyList<Stack?> inventory, Func<Place, bool>? open = null)
    {
        int free = Enumerable.Range(0, InventorySpec.Slots).FirstOrDefault(i => inventory[i] == null && open?.Invoke(new Place(i)) != false, -1);
        return free < 0 ? new Clicked(null) : Flying(true, new Clicked(null, new Unfit(machine, module, free)), module);
    }

    /// <summary>
    /// <paramref name="clicked"/> with the flight of the stack of <paramref name="item"/> its command sends when
    /// <paramref name="shifted"/>: the one place a shift's flight is decided, so no shift sends a stack that does not fly.
    /// </summary>
    private static Clicked Flying(bool shifted, Clicked clicked, string? item) =>
        shifted && clicked.Command is { } command ? clicked with { Flight = FlightOf(command, item) } : clicked;

    /// <summary>Where the stack a shift's <paramref name="command"/> sends flies; null for a command that sends none.</summary>
    private static Flight? FlightOf(Command command, string? item) => command switch
    {
        Move move => new Flight(new SlotSpot(move.Src)),
        Fit fit when item != null => new Flight(new SlotSpot(fit.Module), new SocketSpot(fit.Machine, item)),
        Unfit unfit => new Flight(new SocketSpot(unfit.Machine, unfit.Module), unfit.Slot is int slot ? new SlotSpot(new Place(slot)) : null),
        _ => null,
    };

    private static Clicked Click(Picked? picked, SlotClick click)
    {
        var (place, stack) = (click.Place, click.Stack);
        if (click.Shift && !click.Right)
        {
            if (stack == null) return new Clicked(picked);
            if (place.Crate == null && click.Crate is int machine && click.Free?.Contains(stack.Item) == true) return new Clicked(null, new Fit(place, machine));
            return Shifted(click, stack) is { } to ? new Clicked(null, new Move(place, to, stack.Count)) : new Clicked(picked);
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
            int free = Enumerable.Range(0, InventorySpec.Slots).FirstOrDefault(i => click.Inventory[i] == null && click.Open?.Invoke(new Place(i)) != false, -1);
            return free >= 0 ? new Target(free) : null;
        }
        if (own && !charges && spec.Equip is string kind)
        {
            var slots = Equipment.Of(kind).Where(p => click.Open?.Invoke(p) != false).ToList();
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
        // Slots open from the first on, so the bag ends at the first closed one.
        while (to > from && click.Open?.Invoke(new Place(to - 1)) == false) to--;
        return RoomFor(click.Inventory, from, to, stack) is int slot ? new Target(slot) : null;
    }

    /// <summary>The first of <paramref name="slots"/> from <paramref name="from"/> up to <paramref name="to"/> holding a stack alike with room, else the first empty; none for neither.</summary>
    private static int? RoomFor(IReadOnlyList<Stack?> slots, int from, int to, Stack stack)
    {
        int limit = ItemSpecs.Get(stack.Item).Stack;
        for (int i = from; i < to; i++)
            if (slots[i] is { } s && s.Alike(stack) && s.Count < limit) return i;
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
