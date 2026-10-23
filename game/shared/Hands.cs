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
/// buttons first, then the power tab what is left to it; the removal card's left press on the power tab
/// goes to the cables when one is in reach to cut, else to the building's removal (0030, 0107). While the
/// right button is held the walls round the pointer fade, so the back wall it works shows (<see cref="Lit.SeeThrough"/>).
/// </summary>
public partial class Hands : Node2D
{
    public Ui Ui { get; } = new();
    public PlanHand Plan { get; }
    public WorldButtons Mouse { get; } = new();
    public BuildMenu Menu { get; }
    public OverlayBar Overlays { get; }

    private readonly BuildingView _building;
    private readonly PowerView _power;
    private readonly PipesOverlay _pipes = new();
    private readonly AirView _air;
    private readonly PointerTip _tip;
    /// <summary>Whether the removal card's drag under way began on a cable: its release goes where its press went.</summary>
    private bool _cutting;
    private string? _overlayWas;
    /// <summary>What this player carries is read this often into <see cref="Ui.Carried"/>: a panel reads on a timer.</summary>
    private const double CarriedEveryMs = 250;
    private double _sinceCarried = CarriedEveryMs;

    public Hands()
    {
        Plan = new PlanHand(Ui);
        _building = new BuildingView(Ui, Plan);
        _power = new PowerView(Ui);
        _air = new AirView(Ui);
        Menu = new BuildMenu(Plan, Ui);
        _tip = new PointerTip(Ui);
        Overlays = new OverlayBar(Ui);
        foreach (var node in (Node[])[Mouse, _air, _power, _pipes, _building, Menu, Overlays, _tip]) AddChild(node);
    }

    /// <summary>
    /// One frame against the world <paramref name="m"/> of <paramref name="client"/>: <paramref name="bodies"/>
    /// are the heroes cables sway out of the way of.
    /// </summary>
    public void Frame(Client client, Registry m, IReadOnlyList<Body> bodies, double elapsedMs)
    {
        // Behind the open menu the hands point at nothing: no ghost, no tip, no work.
        var pointer = Ui.MenuOpen.Get() ? null : Mouse.Pointer();
        if ((_sinceCarried += elapsedMs) >= CarriedEveryMs)
        {
            _sinceCarried = 0;
            Ui.Carried = m.Items.Totals(client.Id);
        }
        var buttons = Mouse.Buttons;
        // An overlay says what the pointer is on only while it shows; switched, it says nothing.
        string? overlay = Ui.OverlayOn;
        if (overlay != _overlayWas) Ui.WorldTip.Set(null);
        _overlayWas = overlay;
        var tool = Plan.Selected.Get();
        // The left button is the card's, but on the power tab with no card to place, where it lays the cable or carries pegs
        // and plugs: the power feature's, which takes it itself.
        bool wiring = overlay == PowerView.Id && tool is not (PlaceTool or RemoveTool);
        var left = new List<ButtonEvent>();
        var cuts = new List<ButtonEvent>();
        foreach (var button in wiring ? [] : buttons.TakeLeft())
        {
            if (button.Down) _cutting = tool is RemoveTool && overlay == PowerView.Id && _power.NearCable(button.At);
            (_cutting ? cuts : left).Add(button);
        }
        _building.Frame(client, m, left, buttons.TakeRight(), buttons.HoldsRight, pointer);
        Lit.SeeThrough(pointer is { } at && Layering.Back(buttons.HoldsLeft, buttons.HoldsRight)
            ? new Vector2((float)at.X, (float)at.Y) * ShipCamera.Cell : null);
        _power.Frame(client, m, buttons, cuts, pointer, bodies, elapsedMs);
        _pipes.Visible = overlay == PipesOverlay.Id;
        if (_pipes.Visible) _pipes.Frame(client, m, elapsedMs);
        _air.Frame(client, m, pointer, elapsedMs);
        Menu.Frame(m);
        _tip.Frame(pointer);
    }
}
