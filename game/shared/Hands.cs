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
/// for these features): the plan and its menu, cables, the pipes and air, the air overlay's chip, and
/// the words by the pointer, sharing one <see cref="Shared.Ui"/>. Each frame the building takes the
/// buttons first, then the power tab what is left to it; a middle press on the power tab goes to the
/// cables when one is in reach to cut, else to the building's removal (0030).
/// </summary>
public partial class Hands : Node2D
{
    public Ui Ui { get; } = new();
    public PlanHand Plan { get; }
    public WorldButtons Mouse { get; } = new();
    public BuildMenu Menu { get; }

    private readonly BuildingView _building;
    private readonly PowerView _power;
    private readonly PipesOverlay _pipes = new();
    private readonly AirView _air;
    private readonly PointerTip _tip;
    /// <summary>Whether the middle drag under way began on a cable: its release goes where its press went.</summary>
    private bool _cutting;
    private string? _overlayWas;

    public Hands()
    {
        Plan = new PlanHand(Ui);
        _building = new BuildingView(Ui, Plan);
        _power = new PowerView(Ui);
        _air = new AirView(Ui);
        Menu = new BuildMenu(Plan, Ui);
        _tip = new PointerTip(Ui);
        foreach (var node in (Node[])[Mouse, _air, _power, _pipes, _building, Menu, new OverlayBar(Ui), _tip]) AddChild(node);
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
        // An overlay says what the pointer is on only while it shows; switched, it says nothing.
        string? overlay = Ui.OverlayOn;
        if (overlay != _overlayWas) Ui.WorldTip.Set(null);
        _overlayWas = overlay;
        var removing = new List<ButtonEvent>();
        var cuts = new List<ButtonEvent>();
        foreach (var button in buttons.TakeMiddle())
        {
            if (button.Down) _cutting = overlay == PowerView.Id && _power.NearCable(button.At);
            (_cutting ? cuts : removing).Add(button);
        }
        _building.Frame(client, m, buttons, removing, pointer);
        _power.Frame(client, m, buttons, cuts, pointer, bodies, elapsedMs);
        _pipes.Visible = overlay == PipesOverlay.Id;
        if (_pipes.Visible) _pipes.Frame(client, m, elapsedMs);
        _air.Frame(client, m, pointer, elapsedMs);
        Menu.Frame(m, client.Id, elapsedMs);
        _tip.Frame(pointer);
    }
}
