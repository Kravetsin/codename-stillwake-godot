using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The slots as this peer shows them (<c>read</c> in Inventory.tsx): the world with its own moves still
/// in flight made, so a stack lands on the click rather than a round trip later, and the next click acts
/// on what is seen (0014). <see cref="Crate"/> is the open crate's slots, <see cref="Kind"/> its kind;
/// an analyzer shows what is in its slot, why it stands still and its <see cref="Items.Lessons"/>.
/// <see cref="Analysed"/> are the items analysed so far, which no slot marks.
/// </summary>
public sealed record Reading(
    IReadOnlyList<Stack?> Inventory, IReadOnlyList<Stack?>? Crate, bool Creative, string? Kind, Analyzing? Analyzer,
    IReadOnlyList<string> Analysed, Lessons? Lessons = null)
{
    /// <summary>What the slot at <paramref name="place"/> shows: null when empty or not shown.</summary>
    public Stack? At(Place place)
    {
        var slots = place.Crate == null ? Inventory : Crate;
        return slots != null && place.Slot >= 0 && place.Slot < slots.Count ? slots[place.Slot] : null;
    }

    /// <summary>Whether a slot holding <paramref name="item"/> shows the violet "?": not analysed yet, and never in creative mode.</summary>
    public bool Unknown(string item) => !Creative && !Analysed.Contains(item);

    /// <summary>The one tooltip over a stack of <paramref name="item"/>: its card (<see cref="ItemCards"/>), the band while it is not analysed.</summary>
    public Tip TipOf(string item)
    {
        var card = ItemCards.Of(item, Unknown(item));
        return new Tip(card.Name) { Card = card };
    }

    /// <summary>The one tooltip over <paramref name="stack"/>: its item's card, with what this one holds of its own, a battery's charge.</summary>
    public Tip TipOf(Stack stack)
    {
        var tip = TipOf(stack.Item);
        return tip with { Card = tip.Card! with { Charge = Charges.MeterOf(stack), Picture = Charges.Picture(stack.Item, stack.Charge) } };
    }

    /// <summary>
    /// Peer <paramref name="peer"/>'s inventory and crate <paramref name="crate"/> as its commands in flight,
    /// <paramref name="inFlight"/>, will leave them.
    /// </summary>
    public static Reading Of(
        int peer, IReadOnlyList<Command> inFlight, IItemsQueries items, IGridQueries grid, IResearchQueries research, int? crate)
    {
        var moves = inFlight.Where(c => c is Move or Drop).ToList();
        var shown = items.AfterMoves(peer, moves);
        IReadOnlyList<Stack?>? slots = null;
        string? kind = null;
        if (crate is int id)
        {
            slots = shown.Crates.Where(c => c.Crate == id).Select(c => c.Slots).FirstOrDefault() ?? items.Crate(id);
            kind = grid.MachineById(id)?.Kind;
        }
        // Copied: the sim's list is live, and a reading stands for what was read.
        var analysed = research.Analysed.ToList();
        if (kind != "analyzer") return new Reading(shown.Inventory, slots, items.Creative, kind, null, analysed);
        var at = research.At(crate!.Value);
        var item = slots is { Count: > 0 } ? slots[0]?.Item : null;
        return new Reading(shown.Inventory, slots, items.Creative, kind, at, analysed, Lessons.Of(research, item, at));
    }
}
