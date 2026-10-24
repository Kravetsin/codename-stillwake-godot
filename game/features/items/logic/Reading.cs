using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;

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

    /// <summary>
    /// The modules fitted into the open machine (0086), its own fits and takes in flight made, as moves are (0014): a stack
    /// shifted into a socket leaves its slot on the click.
    /// </summary>
    public IReadOnlyList<string> Fitted { get; init; } = [];

    /// <summary>
    /// What the open machine's modules go into, as a host's kind: the machine's own kind, or at an arc bench the item of the
    /// tool on it (0099), none while it holds none; none with no machine open.
    /// </summary>
    public string? Host => Kind != null && Content.Machines.TryGet(Kind, out var spec) && spec.Tunes ? Crate is { Count: > 0 } slots ? slots[0]?.Item : null : Kind;

    /// <summary>The modules the open machine has a socket for and holds none of: where a module shifted from the bag goes.</summary>
    public IReadOnlyList<string> Free => Host == null ? [] : Content.Modules.For(Host).Select(m => m.Item).Where(m => !Fitted.Contains(m)).ToList();

    /// <summary>Whether a slot holding <paramref name="item"/> shows the violet "?": in some cost and not analysed yet, never in creative mode.</summary>
    public bool Unknown(string item) => ItemCards.Unknown(item, Creative, Analysed);

    /// <summary>
    /// What the open machine does with <paramref name="item"/>: a fed station smelts or grows it, of its process and
    /// hot enough (<see cref="CraftingModule.Processes"/>), or a machine with a fire burns it, a fuel; it takes it as a
    /// module, its tech's icon (0086); none for the rest, an item too hot for the station too, and with no machine open.
    /// </summary>
    public SlotMark HereOf(string item)
    {
        if (Kind == null) return SlotMark.None;
        if (Host != null && Content.Modules.Fits(item, Host)) return SlotMark.Fits;
        if (Content.Stations.TryGet(Kind, out var station) && station.Fed && CraftingModule.Processes(station, item))
            return station.Process == "grow" ? SlotMark.Grows : SlotMark.Smelts;
        return Content.Machines.TryGet(Kind, out var machine) && machine.FuelSlot != null && Content.Items.Get(item).BurnTicks != null
            ? SlotMark.Burns : SlotMark.None;
    }

    /// <summary>
    /// The mark whose faint ghost slot <paramref name="slot"/> of a <paramref name="kind"/> shows while empty (0097): a fed
    /// station's input what it does (the furnace's and the smelter's crucible, the grow capsule's sprout), a fire's fuel slot
    /// the flame, an analyzer's the "?", a charging station's a battery, a drone bay's a drone; none for any other slot.
    /// </summary>
    public static SlotMark GhostOf(string kind, int slot)
    {
        if (!Content.Machines.TryGet(kind, out var spec)) return SlotMark.None;
        if (spec.FuelSlot == slot) return SlotMark.Burns;
        if (Content.Stations.TryGet(kind, out var station) && station.Fed && slot < (spec.Inputs ?? 0))
            return station.Process == "grow" ? SlotMark.Grows : SlotMark.Smelts;
        if (spec.Is(Content.MachineTrait.Zone)) return SlotMark.Docks;
        if (spec.Charges) return SlotMark.Charges;
        return kind == "analyzer" && slot == 0 ? SlotMark.Unknown : SlotMark.None;
    }

    /// <summary>
    /// Whether <paramref name="item"/>, carried, lights the ghost <paramref name="ghost"/> of the open machine's empty slot:
    /// what wears that mark in the bag with it open, what the fire burns, the "?" of what waits for analysis, what holds a
    /// charge, a drone.
    /// </summary>
    public bool Lights(SlotMark ghost, string item) => ghost switch
    {
        SlotMark.Smelts or SlotMark.Grows => Kind != null && Content.Stations.TryGet(Kind, out var station) && station.Fed && CraftingModule.Processes(station, item),
        SlotMark.Burns => Content.Items.Get(item).BurnTicks != null,
        SlotMark.Unknown => Unknown(item),
        SlotMark.Charges => Content.Items.Get(item).Capacity != null,
        SlotMark.Docks => item == Content.Logistics.Drone,
        _ => false,
    };

    /// <summary>The mark in the corner of an inventory slot holding <paramref name="item"/>: the open machine's (<see cref="HereOf"/>), else the "?" while it is not analysed.</summary>
    public SlotMark MarkOf(string item) =>
        HereOf(item) is var here && here != SlotMark.None ? here : Unknown(item) ? SlotMark.Unknown : SlotMark.None;

    /// <summary>The one tooltip over a stack of <paramref name="item"/>: its card (<see cref="ItemCards"/>), the band while it is not analysed.</summary>
    public Tip TipOf(string item)
    {
        var card = ItemCards.Of(item, Unknown(item));
        return new Tip(card.Name) { Card = card };
    }

    /// <summary>
    /// The one tooltip over a fed station's mark while it is too cold for <paramref name="item"/> (0057): that it
    /// needs more heat, and the first station hot enough for it, by name.
    /// </summary>
    public static Tip TooColdTip(string item)
    {
        int heat = Content.Items.Get(item).Heat;
        var hot = Content.Stations.All.First(s => s.Fed && s.Heat >= heat);
        return new Tip(T("crafting.tooCold", ("station", T($"station.{hot.Machine}"))));
    }

    /// <summary>
    /// The one tooltip over <paramref name="stack"/>: its item's card, with what this one holds of its own, a battery's charge and a
    /// tool's modules (0098), its numbers the sim's fold for them (<see cref="ItemCards.Fitted"/>).
    /// </summary>
    public Tip TipOf(Stack stack)
    {
        var tip = TipOf(stack.Item);
        var card = tip.Card! with { Charge = Charges.MeterOf(stack), Picture = Charges.Picture(stack.Item, stack.Charge) };
        return tip with { Card = ItemCards.Fitted(card, ToolStats.ModulesOf(stack)) };
    }

    /// <summary>
    /// The tooltip over <paramref name="stack"/> in an inventory slot: <see cref="TipOf(Stack)"/>, with what the open machine
    /// does with it (<see cref="HereOf"/>) and, a fed station, what it makes of it there (<see cref="ItemCards.OutputAt"/>).
    /// </summary>
    public Tip HereTipOf(Stack stack)
    {
        var tip = TipOf(stack);
        var here = HereOf(stack.Item);
        var output = here is SlotMark.Smelts or SlotMark.Grows && Content.Stations.TryGet(Kind!, out var station)
            ? ItemCards.OutputAt(stack.Item, station) : null;
        return tip with { Card = tip.Card! with { Here = here, Output = output } };
    }

    /// <summary>
    /// Peer <paramref name="peer"/>'s inventory and crate <paramref name="crate"/> as its commands in flight,
    /// <paramref name="inFlight"/>, will leave them.
    /// </summary>
    public static Reading Of(
        int peer, IReadOnlyList<Command> inFlight, IItemsQueries items, IGridQueries grid, IResearchQueries research, int? crate)
    {
        var moves = inFlight.Where(c => c is Move or Drop or Sort).ToList();
        var shown = items.AfterMoves(peer, moves);
        IReadOnlyList<Stack?>? slots = null;
        string? kind = null;
        if (crate is int id)
        {
            slots = shown.Crates.Where(c => c.Crate == id).Select(c => c.Slots).FirstOrDefault() ?? items.Crate(id);
            kind = grid.MachineById(id)?.Kind;
        }
        // An arc bench's are its tool's (0099), on the stack in its first slot.
        bool tunes = kind != null && Content.Machines.TryGet(kind, out var spec) && spec.Tunes;
        IReadOnlyList<string> modules = tunes ? slots is { Count: > 0 } && slots[0]?.Module is { } on ? [on] : [] : crate is int open ? items.FittedTo(open) : [];
        (var inventory, slots, var fitted) = Fitting(shown.Inventory, slots, modules, inFlight, crate, tunes);
        // Copied: the sim's list is live, and a reading stands for what was read.
        var analysed = research.Analysed.ToList();
        if (kind != "analyzer") return new Reading(inventory, slots, items.Creative, kind, null, analysed) { Fitted = fitted };
        var at = research.At(crate!.Value);
        var item = slots is { Count: > 0 } ? slots[0]?.Item : null;
        return new Reading(inventory, slots, items.Creative, kind, at, analysed, Lessons.Of(research, item, at)) { Fitted = fitted };
    }

    /// <summary>
    /// The slots and machine <paramref name="crate"/>'s modules as this peer's fits and takes in <paramref name="inFlight"/>
    /// will leave them (0086), after its moves: a module fitted leaves the slot it was in, one taken out lands in the slot
    /// it names, empty as shown. One the sim refuses shows back a round trip later, as a refused move does. At an arc bench
    /// (<paramref name="tunes"/>, 0099) the tool on its first slot is shown holding what is fitted.
    /// </summary>
    public static (IReadOnlyList<Stack?> Inventory, IReadOnlyList<Stack?>? Crate, IReadOnlyList<string> Fitted) Fitting(
        IReadOnlyList<Stack?> inventory, IReadOnlyList<Stack?>? crateSlots, IReadOnlyList<string> fitted, IReadOnlyList<Command> inFlight, int? crate,
        bool tunes = false)
    {
        if (!inFlight.Any(c => c is Fit or Unfit)) return (inventory, crateSlots, fitted.ToList());
        var own = inventory.ToArray();
        var open = crateSlots?.ToArray();
        var modules = fitted.ToList();
        foreach (var command in inFlight)
        {
            if (command is Fit fit)
            {
                var side = fit.Module.Crate == null ? own : fit.Module.Crate == crate ? open : null;
                if (side == null || fit.Module.Slot < 0 || fit.Module.Slot >= side.Length || side[fit.Module.Slot] is not { } stack) continue;
                side[fit.Module.Slot] = stack.Count > 1 ? stack with { Count = stack.Count - 1 } : null;
                if (fit.Machine == crate && !modules.Contains(stack.Item)) modules.Add(stack.Item);
            }
            else if (command is Unfit unfit)
            {
                if (unfit.Machine == crate) modules.Remove(unfit.Module);
                if (unfit.Slot is int slot && slot >= 0 && slot < own.Length && own[slot] == null) own[slot] = new Stack(unfit.Module, 1);
            }
        }
        if (tunes && open is { Length: > 0 } && open[0] is { } tool) open[0] = tool with { Module = modules.Count > 0 ? modules[0] : null };
        return (own, open, modules);
    }
}
