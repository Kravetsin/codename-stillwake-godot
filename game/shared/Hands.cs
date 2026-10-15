using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Atmosphere;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Features.Pipes;
using Stillwake.Game.Features.Power;
using Stillwake.Game.Overlays;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Shared;

/// <summary>
/// This player's hands on the ship and the views that explain it (App.tsx's part of the TypeScript game
/// for these features): the plan and its menu, cables, the pipes and air, the overlays and their bar, and
/// the words by the pointer, sharing one <see cref="Shared.Ui"/>. Each frame the building takes the
/// buttons first, then the power overlay what is left to it (0022).
/// </summary>
public partial class Hands : Node2D
{
    public Ui Ui { get; } = new();
    public PlanHand Plan { get; }
    public OverlaySwitch Overlays { get; }
    public WorldButtons Mouse { get; } = new();
    public BuildMenu Menu { get; }

    private readonly BuildingView _building;
    private readonly PowerView _power;
    private readonly PipesOverlay _pipes = new();
    private readonly AirView _air;
    private readonly PointerTip _tip;

    public Hands()
    {
        Plan = new PlanHand(Ui);
        Overlays = new OverlaySwitch(OverlaySwitch.All, Ui.OverlayOn);
        _building = new BuildingView(Ui, Plan);
        _power = new PowerView(Ui);
        _air = new AirView(Ui);
        Menu = new BuildMenu(Plan, Ui);
        _tip = new PointerTip(Ui);
        foreach (var node in (Node[])[Mouse, _air, _power, _pipes, _building, Menu, new OverlayBar(Overlays), _tip]) AddChild(node);
        // An overlay says what the pointer is on only while it is on; switched off, it says nothing.
        Ui.OverlayOn.Subscribe(() => Ui.WorldTip.Set(null));
    }

    /// <summary>
    /// One frame against the world <paramref name="m"/> of <paramref name="client"/>: <paramref name="bodies"/>
    /// are the heroes cables sway out of the way of.
    /// </summary>
    public void Frame(Client client, Registry m, IReadOnlyList<Body> bodies, double elapsedMs)
    {
        // Behind the open menu the hands point at nothing: no ghost, no tip, no work.
        var pointer = Ui.MenuOpen.Get() ? null : Mouse.Pointer();
        var buttons = Mouse.Buttons;
        _building.Frame(client, m, buttons, pointer);
        _power.Frame(client, m, buttons, pointer, bodies, elapsedMs);
        _pipes.Visible = Ui.OverlayOn.Get() == PipesOverlay.Id;
        if (_pipes.Visible) _pipes.Frame(client, m, elapsedMs);
        _air.Frame(client, m, pointer, bodies, elapsedMs);
        Menu.Frame(m, client.Id, elapsedMs);
        _tip.Frame(pointer);
    }
}
