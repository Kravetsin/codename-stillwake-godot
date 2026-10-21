using Stillwake.Game.Features.Items;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// Every shift+LMB that sends a stack flies it (0075, 0086): in every panel the inventory opens, from every slot it shows,
/// for every item, and out of every module socket. The panels and items are drawn from content, so a new one is covered.
/// </summary>
public class ShiftFlightsTests
{
    private const int Machine = 7;

    /// <summary>Every panel a shift-click works in: none open, and every machine by its kind.</summary>
    public static TheoryData<string?> Panels() => [null, .. Spec.Machines.All.Select(m => m.Kind)];

    /// <summary>What a panel of <paramref name="kind"/> shows with one <paramref name="item"/> at <paramref name="at"/>, the rest empty.</summary>
    private static Reading Showing(string? kind, string item, Place at, IReadOnlyList<string>? fitted = null)
    {
        var inventory = new Stack?[Spec.Inventory.Total];
        Stack?[]? crate = kind != null ? new Stack?[Spec.Machines.Get(kind).Slots ?? 0] : null;
        (at.Crate == null ? inventory : crate!)[at.Slot] = new Stack(item, 1);
        return new Reading(inventory, crate, false, kind, null, []) { Fitted = fitted ?? [] };
    }

    /// <summary>Every slot a panel of <paramref name="kind"/> shows: the hotbar's, the bag's, the equipment's and the machine's.</summary>
    private static IEnumerable<Place> Places(string? kind)
    {
        foreach (int slot in Enumerable.Range(0, Spec.Inventory.Total)) yield return new Place(slot);
        if (kind == null) yield break;
        foreach (int slot in Enumerable.Range(0, Spec.Machines.Get(kind).Slots ?? 0)) yield return new Place(slot, Machine);
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void EveryShiftThatSendsAStackFliesItFromItsSlot(string? kind)
    {
        int sent = 0;
        foreach (var item in Spec.Items.All.Select(i => i.Id))
            foreach (var place in Places(kind))
            {
                var reading = Showing(kind, item, place);
                var result = Clicks.On(null, SlotClick.Of(reading, place, right: false, shift: true, kind != null ? Machine : null));
                if (result.Command == null) continue;
                sent++;
                Assert.True(result.Flight?.From == new SlotSpot(place), $"{kind}: {item} shifted from {place} sends {result.Command} with no flight");
            }
        // Not vacuous: every panel sends something somewhere with shift.
        Assert.True(sent > 0, kind);
    }

    [Theory]
    [MemberData(nameof(Panels))]
    public void EverySocketShiftedOutOfFliesItsModuleIntoTheBag(string? kind)
    {
        if (kind == null) return;
        foreach (var module in Spec.Modules.For(kind))
        {
            var bag = new Stack?[Spec.Inventory.Total];
            var result = Clicks.OnSocket(null, new SocketClick(Machine, module.Item, Fitted: true, Shift: true, null, bag));
            Assert.Equal(new Unfit(Machine, module.Item, 0), result.Command);
            Assert.Equal(new Flight(new SocketSpot(Machine, module.Item), new SlotSpot(new Place(0))), result.Flight);
        }
    }

    [Fact]
    public void AModuleShiftedFromTheBagIsFittedFlyingIntoItsSocket()
    {
        var arc = Spec.Modules.Arc.Item;
        var result = Clicks.On(null, SlotClick.Of(Showing("smelter", arc, new Place(12)), new Place(12), false, true, Machine));
        Assert.Equal(new Fit(new Place(12), Machine), result.Command);
        Assert.Equal(new Flight(new SlotSpot(new Place(12)), new SocketSpot(Machine, arc)), result.Flight);
        // Fitted already, it goes as any stack would, into the station's input, flying there.
        var fitted = Clicks.On(null, SlotClick.Of(Showing("smelter", arc, new Place(12), [arc]), new Place(12), false, true, Machine));
        Assert.IsType<Move>(fitted.Command);
        Assert.Equal(new Flight(new SlotSpot(new Place(12))), fitted.Flight);
        // A machine that takes none, the furnace: into its input as before.
        Assert.IsType<Move>(Clicks.On(null, SlotClick.Of(Showing("furnace", arc, new Place(12)), new Place(12), false, true, Machine)).Command);
    }

    [Fact]
    public void ASocketTakesACarriedModulePutsItsOwnOutByHandOrWithShiftAndDoesNothingFull()
    {
        var arc = Spec.Modules.Arc.Item;
        var bag = new Stack?[Spec.Inventory.Total];
        var carried = new Picked(new Place(4), 1);
        // A module carried onto the empty socket is put in, as onto a slot: no flight without shift.
        var put = Clicks.OnSocket(carried, new SocketClick(Machine, arc, false, false, new Stack(arc, 1), bag));
        Assert.Equal(new Clicked(null, new Fit(new Place(4), Machine)), put);
        // Anything else carried stays carried; an empty socket pressed with nothing does nothing.
        Assert.Equal(new Clicked(carried), Clicks.OnSocket(carried, new SocketClick(Machine, arc, false, false, new Stack("iron", 1), bag)));
        Assert.Equal(new Clicked(null), Clicks.OnSocket(null, new SocketClick(Machine, arc, false, true, null, bag)));
        // A fitted one pressed begins to come out by hand; with shift, into the first empty slot, the hotbar first.
        Assert.Equal(new Clicked(null, TakesOut: true), Clicks.OnSocket(null, new SocketClick(Machine, arc, true, false, null, bag)));
        bag[0] = new Stack("iron", 3);
        Assert.Equal(new Unfit(Machine, arc, 1), Clicks.OnSocket(null, new SocketClick(Machine, arc, true, true, null, bag)).Command);
        var full = Enumerable.Range(0, Spec.Inventory.Total).Select(_ => (Stack?)new Stack("iron", 1)).ToArray();
        Assert.Equal(new Clicked(null), Clicks.OnSocket(null, new SocketClick(Machine, arc, true, true, null, full)));
    }

    [Fact]
    public void AFitOrATakeInFlightShowsAtOnceAsAMoveDoes()
    {
        var arc = Spec.Modules.Arc.Item;
        Stack?[] bag = [new Stack(arc, 1), new Stack("iron", 2), null];
        // A fit: the module leaves its slot and is shown fitted in the open machine.
        var (inventory, _, fitted) = Reading.Fitting(bag, null, [], [new Fit(new Place(0), Machine)], Machine);
        Assert.Null(inventory[0]);
        Assert.Equal([arc], fitted);
        // Into another machine: it leaves the slot, the open machine's sockets unchanged.
        (inventory, _, fitted) = Reading.Fitting(bag, null, [], [new Fit(new Place(0), 9)], Machine);
        Assert.Null(inventory[0]);
        Assert.Empty(fitted);
        // A take: shown in the slot it names, empty as shown, and no longer fitted; onto a full slot it shows nowhere.
        (inventory, _, fitted) = Reading.Fitting(bag, null, [arc], [new Unfit(Machine, arc, 2)], Machine);
        Assert.Equal(new Stack(arc, 1), inventory[2]);
        Assert.Empty(fitted);
        (inventory, _, _) = Reading.Fitting(bag, null, [arc], [new Unfit(Machine, arc, 1)], Machine);
        Assert.Equal(new Stack("iron", 2), inventory[1]);
        // Nothing in flight: as the world has it.
        Assert.Same(bag, Reading.Fitting(bag, null, [arc], [], Machine).Inventory);
        // Out of the machine's own slot.
        Stack?[] crate = [new Stack(arc, 1)];
        var (_, slots, _) = Reading.Fitting(bag, crate, [], [new Fit(new Place(0, Machine), Machine)], Machine);
        Assert.Null(slots![0]);
    }
}
