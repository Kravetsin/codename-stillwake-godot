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
/// buttons, but while the stripper is held, when the cables take them (0108), or the wrench, the pipes (0109), by the card of its menu (0110). With the plan away, while the
/// right button digs, the walls round the pointer fade, so the back wall shows (<see cref="Lit.SeeThrough"/>).
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
    private readonly PipesOverlay _pipes;
    private readonly PipesView _pipesBuilt = new();
    private readonly AirView _air;
    private readonly PointerTip _tip;
    private string? _overlayWas;
    /// <summary>What this player carries is read this often into <see cref="Ui.Carried"/>: a panel reads on a timer.</summary>
    private const double CarriedEveryMs = 250;
    private double _sinceCarried = CarriedEveryMs;

    public Hands()
    {
        Plan = new PlanHand(Ui);
        _building = new BuildingView(Ui, Plan);
        _power = new PowerView(Ui);
        _pipes = new PipesOverlay(Ui);
        _air = new AirView(Ui);
        Menu = new BuildMenu(Plan, Ui);
        _tip = new PointerTip(Ui);
        Overlays = new OverlayBar(Ui);
        foreach (var node in (Node[])[Mouse, _air, _power, _pipesBuilt, _pipes, _building, Menu, Overlays, _tip]) AddChild(node);
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
        // The cables show only while the stripper is held (0108), the pipes while the wrench is (0109): then the buttons are
        // that feature's, the left laying, the right taking away; else the card's.
        bool wiring = overlay == PowerView.Id, piping = overlay == PipesOverlay.Id;
        IReadOnlyList<ButtonEvent> cuts = wiring || piping ? buttons.TakeRight() : [];
        IReadOnlyList<ButtonEvent> laid = piping ? buttons.TakeLeft() : [];
        if (wiring || piping) _building.Frame(client, m, [], [], false, pointer);
        else _building.Frame(client, m, buttons.TakeLeft(), buttons.TakeRight(), buttons.HoldsRight, pointer);
        // With the plan put away, aboard, the front fades round the pointer while the right button digs back walls; the open plan fades by its focus.
        // Never away from the ship (0110): the ground would show what is in it.
        bool seeThrough = Plan.SeesThrough(!wiring && !piping && Layering.Back(buttons.HoldsLeft, buttons.HoldsRight));
        Lit.SeeThrough(pointer is { } at && seeThrough ? new Vector2((float)at.X, (float)at.Y) * ShipCamera.Cell : null);
        _power.Frame(client, m, buttons, wiring ? cuts : [], pointer, bodies, elapsedMs);
        _pipesBuilt.Frame(client, m);
        _pipes.Visible = piping;
        if (piping) _pipes.Frame(client, m, laid, Plan.ToolCard.Get() == Tools.Valve, cuts, pointer, elapsedMs);
        else _pipes.Release();
        _air.Frame(client, m, pointer, elapsedMs);
        Menu.Frame(m);
        _tip.Frame(pointer);
    }
}
