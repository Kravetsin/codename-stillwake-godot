// InventoryPanel's module sockets (0086): a machine's sockets laid out, lent to other panels, fitted by clicks and drags, and their tips.
using System;
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
    /// <summary>
    /// Machine <paramref name="machine"/>'s module sockets for another feature's panel to lay out (0086), worked here as this
    /// panel's own: the crafting feature's queued stations (wired by <c>Main</c>). Null for a machine that takes no module.
    /// </summary>
    public Control? LendSockets(int machine, string kind)
    {
        if (Sockets(machine, kind, caption: false) is not { } row) return null;
        foreach (var socket in row.GetChildren().OfType<ModuleSocket>()) _lentSockets.Add(socket);
        Refresh();
        return row;
    }

    /// <summary>
    /// A row of machine <paramref name="machine"/>'s sockets, one for each module a machine of <paramref name="kind"/> takes
    /// (<c>Content.Modules</c>), after its caption; null for a machine that takes none (the furnace, the pod).
    /// </summary>
    private Control? Sockets(int machine, string kind, bool caption = true)
    {
        var takes = HostOf(machine, kind) is { } host ? Content.Modules.For(host).ToList() : [];
        if (takes.Count == 0) return null;
        var row = Style.Gap(new HBoxContainer(), Slots.Gap);
        foreach (var module in takes)
        {
            var socket = new ModuleSocket(machine, module, _ui.Slot);
            socket.Pressed += SocketPressed;
            _tipSpots.Add(socket, () => SocketTip(socket));
            _sockets.Add(socket);
            socket.TreeExiting += () =>
            {
                _sockets.Remove(socket);
                _lentSockets.Remove(socket);
            };
            row.AddChild(socket);
        }
        if (!caption) return row;
        var words = new Label
        {
            Text = T("module.socket"),
            LabelSettings = Style.Text(Style.Mono, 10, Style.Slate400),
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        row.AddChild(words);
        row.MoveChild(words, 0);
        return row;
    }

    /// <summary>
    /// The modules fitted into <paramref name="machine"/>: the open machine's as shown, this peer's own fits and takes in
    /// flight made (<see cref="Reading.Fitted"/>); another's as the world has them.
    /// </summary>
    private IReadOnlyList<string> FittedTo(int machine) =>
        machine == Crate && _reading != null ? _reading.Fitted : _items?.FittedTo(machine) ?? [];

    /// <summary>Whether <paramref name="item"/> would go into a socket of open machine <paramref name="machine"/>: a module it takes, not fitted yet.</summary>
    private bool GoesIn(string? item, int machine) =>
        item != null && _grid?.MachineById(machine) is { } m && HostOf(machine, m.Kind) is { } host && Content.Modules.Fits(item, host) && !FittedTo(machine).Contains(item);

    /// <summary>
    /// What machine <paramref name="machine"/>, of <paramref name="kind"/>, takes modules as: its kind, or at an arc bench the tool
    /// on it, as shown for the open one (<see cref="Reading.Host"/>, 0099); none for a bench holding none.
    /// </summary>
    private string? HostOf(int machine, string kind)
    {
        if (!MachineSpecs.TryGet(kind, out var spec) || !spec.Tunes) return kind;
        return machine == Crate && _reading != null ? _reading.Host : _items?.Crate(machine) is { Count: > 0 } slots ? slots[0]?.Item : null;
    }

    /// <summary>
    /// A socket pressed (<see cref="Clicks.OnSocket"/>): a module carried that goes in is fitted; a fitted one begins to
    /// come out, with shift at once into the bag, flying there.
    /// </summary>
    private void SocketPressed(ModuleSocket socket, bool shift)
    {
        if (!Shown || _reading is not { } reading) return;
        var held = Held;
        var result = Clicks.OnSocket(held, new SocketClick(
            socket.Machine, socket.Module, FittedTo(socket.Machine).Contains(socket.Module), shift, held is { } h ? reading.At(h.Place) : null, reading.Inventory));
        if (result.Command != null || result.TakesOut) _ui.Click();
        if (result.Command != null) (_picked, _dragFrom) = (null, null);
        if (result.TakesOut) _unfitting = socket;
        Act(reading, result);
        Refresh();
    }

    /// <summary>Fits what <paramref name="from"/> holds into <paramref name="socket"/>'s machine, the hand let go.</summary>
    private void FitFrom(Place from, ModuleSocket socket)
    {
        _ui.Click();
        (_picked, _dragFrom) = (null, null);
        Send(new Fit(from, socket.Machine));
    }

    /// <summary>
    /// The left button let go over a socket: a module carried or dragged there is fitted; one being taken out, let go
    /// where it was pressed, flies into the bag's first empty slot (<see cref="Clicks.TakeOut"/>).
    /// </summary>
    private void ReleaseOnSocket(ModuleSocket socket)
    {
        var unfitting = _unfitting;
        _unfitting = null;
        if (unfitting == socket && _reading is { } reading) Act(reading, Clicks.TakeOut(socket.Machine, socket.Module, reading.Inventory));
        else if (Held is { } held && _reading?.At(held.Place)?.Item == socket.Module && GoesIn(socket.Module, socket.Machine)) FitFrom(held.Place, socket);
        _dragFrom = null;
        Refresh();
    }

    private ModuleSocket? SocketUnder(Vector2 at) => _ui.MenuOpen.Get() ? null :
        _sockets.FirstOrDefault(s => GodotObject.IsInstanceValid(s) && s.IsVisibleInTree() && s.GetGlobalRect().HasPoint(at));

    /// <summary>
    /// A fitted socket's tip: its module's card, a click taking it out; an empty one's, only its name: the tech icon in it
    /// says what it takes (0086).
    /// </summary>
    private Tip? SocketTip(ModuleSocket socket)
    {
        if (_reading is not { } reading) return null;
        if (FittedTo(socket.Machine).Contains(socket.Module))
        {
            var tip = reading.TipOf(socket.Module);
            return tip with { Card = tip.Card! with { Use = new CardKey("LMB", T("card.takeOut")) } };
        }
        return new Tip(T("module.empty"));
    }
}
