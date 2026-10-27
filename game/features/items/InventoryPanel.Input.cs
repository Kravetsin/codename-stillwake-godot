// InventoryPanel's input: keys, clicks, drags and drops on the slots, the moves sent and flown, and where the panel lies on screen.
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Furnace;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using static Stillwake.Game.I18n.En;
using MachineSpecs = Stillwake.Content.Machines;

namespace Stillwake.Game.Features.Items;

public partial class InventoryPanel
{
    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false } key)
        {
            switch (key.PhysicalKeycode)
            {
                case Key.Tab:
                    if (Shown) Close();
                    else
                    {
                        // One hand (0022): the inventory, a stack carried from it, takes it.
                        _ui.Hand.Set(Holding.Free);
                        (_open, _picked) = (true, null);
                    }
                    break;
                case >= Key.Key1 and <= Key.Key9:
                    _ui.Hand.Set(Holding.Free);
                    int digit = Hotbar.SlotOfDigit((int)(key.PhysicalKeycode - Key.Key0))!.Value;
                    if (digit != _chosen) _ui.Click();
                    _chosen = digit;
                    break;
                case Key.Q:
                    // A card in hand, not the chosen slot: Q drops nothing it does not show.
                    if (!HandFree) return;
                    if (_reading?.Inventory[_chosen] is { } stack)
                    {
                        _ui.Click();
                        DropFrom(new Place(_chosen), stack.Count);
                    }
                    break;
                default:
                    return;
            }
            GetViewport().SetInputAsHandled();
            Refresh();
        }
        // Over the world only: a panel under the pointer takes the wheel before it gets here. With Ctrl it zooms the camera.
        else if (e is InputEventMouseButton { Pressed: true, CtrlPressed: false, ButtonIndex: MouseButton.WheelDown or MouseButton.WheelUp } wheel)
        {
            _ui.Hand.Set(Holding.Free);
            _chosen = Hotbar.WheelSlot(_chosen, wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1);
            Refresh();
        }
    }

    /// <summary>
    /// The pointer and the left button let go, wherever they are: Godot sends a release to the control
    /// that took the press, so the slot under the pointer is found here. A stack in hand let go over the
    /// world is thrown.
    /// </summary>
    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseMotion motion) _mouse = motion.Position;
        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } up) return;
        _mouse = up.Position;
        // Sockets first (0086): a module let go there is fitted; one taken out goes to the bag slot it is let go over.
        if (SocketUnder(_mouse) is { } socket)
        {
            ReleaseOnSocket(socket);
            return;
        }
        if (_unfitting is { } from)
        {
            _unfitting = null;
            if (SlotUnder(_mouse) is { Crate: null } bag && bag.Slot < Content.Inventory.Slots) Send(new Unfit(from.Machine, from.Module, bag.Slot));
            Refresh();
            return;
        }
        if (SlotUnder(_mouse) is { } place)
        {
            ReleaseAt(place);
            return;
        }
        _dragFrom = null;
        if (Held is { } inHand && !OverPanel(_mouse))
        {
            _ui.Click();
            DropFrom(inHand.Place, inHand.Count);
            _picked = null;
            Refresh();
        }
    }

    /// <summary>The middle of the bag's sort key, or of the open crate's; null with none shown: for scripted runs.</summary>
    public Vector2? SortKeyAt(bool crate) => (crate ? _crateKey : _bagKey) is { } key && key.IsVisibleInTree() ? key.GetGlobalRect().GetCenter() : null;

    /// <summary>The middle of machine <paramref name="machine"/>'s first module socket on screen while it shows, for scripted runs (0086).</summary>
    public Vector2? SocketAt(int machine) =>
        _sockets.FirstOrDefault(s => GodotObject.IsInstanceValid(s) && s.Machine == machine && s.IsVisibleInTree())?.GetGlobalRect().GetCenter();

    /// <summary>The middle of the slot at <paramref name="place"/> on screen, for scripted runs.</summary>
    public Vector2 CenterOf(Place place) => _slots.First(s => s.Place == place).GetGlobalRect().GetCenter();

    // Under the open menu no slot is pointed at: its name would show over the menu.
    private Place? SlotUnder(Vector2 at) => _ui.MenuOpen.Get() ? null :
        _slots.FirstOrDefault(s => s.IsVisibleInTree() && s.GetGlobalRect().HasPoint(at))?.Place;

    // Any panel under the pointer counts, another feature's too: a click on the crafts throws nothing.
    private bool OverPanel(Vector2 at) => GetViewport().GuiGetHoveredControl() != null || Covers().Any(r => r.HasPoint(at));

    /// <summary>
    /// Where the hotbar, the open inventory with the machine under it and the equipment lie on the screen, their metal
    /// round them: what the crew's markers keep out of.
    /// </summary>
    public IEnumerable<Rect2> Covers()
    {
        yield return _hotbar.Outer;
        if (_body.Visible) yield return _body.Outer;
        if (!_shown.Visible) yield break;
        foreach (var panel in _shown.GetChildren().OfType<Control>())
            if (panel.IsVisibleInTree()) yield return panel is WristScreen screen ? screen.Outer : panel.GetGlobalRect();
    }

    /// <summary>Throws a stack from the hero the way it faces.</summary>
    private void DropFrom(Place src, int count)
    {
        if (_hero is not { } spot) return;
        Send(new Drop(src, count, spot.Cell, spot.Facing));
    }

    /// <summary>
    /// A stack picked up by a press, carried while the button is held: let go over another slot, it is
    /// put down there, as a second click would; let go where it was picked, it stays in hand.
    /// </summary>
    private void ReleaseAt(Place place)
    {
        var from = _dragFrom;
        _dragFrom = null;
        if (from == null || from == place || _reading == null) return;
        if (Held != null)
        {
            ClickAt(place, false, false);
            return;
        }
        // With the inventory closed a press only chooses a slot, and a drag along the hotbar chooses the slot
        // it is let go over, as Terraria does: nothing is moved where the player cannot see it carried.
        if (!Shown && place.Crate == null && place.Slot < Hotbar.Row)
        {
            // A drag along the hotbar clicks as it is let go, on the slot it chose.
            _ui.Click();
            _chosen = place.Slot;
            Refresh();
        }
    }

    private void ClickAt(Place place, bool right, bool shift)
    {
        if (!right && !shift) _dragFrom = Held != null ? null : place;
        if (!Shown)
        {
            if (place.Crate == null && place.Slot < Hotbar.Row)
            {
                _ui.Click();
                _chosen = place.Slot;
            }
            Refresh();
            return;
        }
        if (_reading == null) return;
        var before = _reading;
        var held = Held;
        // A queued station's inputs share what is shift-clicked in, onto its kind first; a module goes into its free socket.
        var result = Clicks.On(held, SlotClick.Of(_reading, place, right, shift, Crate));
        (_picked, _pickedItem) = (result.Picked, result.Picked is { } p ? _reading.At(p.Place)?.Item : null);
        if (result.Breathes)
        {
            _breathedFrom = (place, Engine.GetProcessFrames());
            _ui.Breathe();
        }
        // Every action clicks as it is made, a shift's on launch; a canister's when the crew breathes it.
        if (!result.Breathes && (result.Command != null || result.Picked != held)) _ui.Click();
        if (result.Command != null) Act(before, result);
        else
        {
            if (shift && !right && before.At(place) != null) BoxAt(place)?.Shake();
            Refresh();
        }
    }

    /// <summary>
    /// A stack shift-clicked out of a slot that is not one of this panel's, at <paramref name="from"/> on screen: a queued
    /// station's output, the crafting feature's (wired by <c>Main</c>). Taken as a shift-click on its slot is, flying.
    /// </summary>
    public void ShiftOut(Place place, Rect2 from)
    {
        if (_reading is not { } reading) return;
        Act(reading, Clicks.On(null, SlotClick.Of(reading, place, false, true, Crate)), from);
    }

    /// <summary>
    /// The one way a click's command goes out (0075, 0086): sent, the slots read again at once, and a stack sent with shift
    /// flying from where it was (<paramref name="from"/> for a slot not this panel's) to where it went (<see cref="Fly"/>).
    /// </summary>
    private void Act(Reading before, Clicked result, Rect2? from = null)
    {
        if (result.Command is not { } command) return;
        Send(command);
        if (result.Flight is { } flight) Fly(before, flight, from);
    }

    private SlotBox? BoxAt(Place place) => _slots.FirstOrDefault(s => s.Place == place && s.IsVisibleInTree());

    private ModuleSocket? SocketAt(SocketSpot spot) =>
        _sockets.FirstOrDefault(s => GodotObject.IsInstanceValid(s) && s.Machine == spot.Machine && s.Module == spot.Module && s.IsVisibleInTree());

    /// <summary>Where <paramref name="spot"/> is on screen while it shows: a slot's box or a socket.</summary>
    private Rect2? RectOf(Spot spot) => spot switch
    {
        SlotSpot slot => BoxAt(slot.Place)?.GetGlobalRect(),
        SocketSpot socket => SocketAt(socket)?.GetGlobalRect(),
        _ => null,
    };

    /// <summary>
    /// A stack sent with shift flying from where it was to where it went, as the slots show it now, the command made at
    /// once (0014): into a socket, into the slot named, or each slot it landed in; a move the sim would refuse lands
    /// nowhere, and its slot shakes.
    /// </summary>
    private void Fly(Reading before, Flight flight, Rect2? from)
    {
        if (_reading is not { } after || (from ?? RectOf(flight.From)) is not { } src) return;
        var stack = flight.From is SlotSpot { Place: var place } ? before.At(place) : flight.From is SocketSpot s ? new Stack(s.Module, 1) : null;
        if (stack == null) return;
        var picture = Charges.Picture(stack.Item, stack.Charge);
        var start = src.GetCenter();
        switch (flight.To)
        {
            case SocketSpot socket when SocketAt(socket) is { } to:
                _flights.Fly(picture, start, to.GetGlobalRect(), socket, true);
                return;
            case SlotSpot slot when BoxAt(slot.Place) is { } to:
                _flights.Fly(picture, start, to.GetGlobalRect(), slot, before.At(slot.Place) == null);
                return;
            case null when flight.From is SlotSpot { Place: var at }:
                var shown = _slots.Where(b => b.IsVisibleInTree()).Select(b => b.Place).ToList();
                var landed = Clicks.Landed(at, stack.Item, before.At, after.At, shown).ToList();
                if (landed.Count == 0) BoxAt(at)?.Shake();
                for (int i = 0; i < landed.Count; i++)
                    if (BoxAt(landed[i]) is { } box) _flights.Fly(picture, start, box.GetGlobalRect(), new SlotSpot(landed[i]), before.At(landed[i]) == null, i);
                return;
        }
    }
}
