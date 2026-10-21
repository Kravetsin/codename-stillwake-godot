using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --testbed --arc</c>: the hero by the testbed's electric smelter, smelting rock, an arc module in its bag, for
/// recording the module socket and the arcs (0086). The script opens the smelter, points at its empty socket, its tech's
/// icon in it, and at the module, marked with that icon, its card showing it; shifts it into the socket, flying there;
/// shifts it back out into the bag and in again; the smelter crackles violet-white as it works; then the rock is taken out,
/// and the smelter stands dark with the arc still fitted.
/// </summary>
public partial class ArcDemo : Control
{
    /// <summary>Where the arc module lies in the hero's own slots.</summary>
    public const int ArcSlot = 12;

    /// <summary>Where the rock taken out of the smelter goes.</summary>
    public const int RockSlot = 20;

    /// <summary>The testbed's world with an arc module in peer 0's bag.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == ArcSlot ? new JsonObject { ["item"] = Spec.Modules.Arc.Item, ["count"] = 1 } : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), modules());
    }

    private readonly ItemsView _view;
    private readonly Ui _ui;
    private readonly Func<Registry> _m;
    private readonly List<(double At, Action Do)> _steps = [];
    private Vector2 _pos = new(640, 360), _target = new(640, 360);
    private double _t;
    private int _next;

    public ArcDemo(ItemsView view, Ui ui, Func<Registry> m)
    {
        (_view, _ui, _m) = (view, ui, m);
        MouseFilter = MouseFilterEnum.Ignore;
        At(0.5, () => _ui.OpenCrate.Set(Smelter()));
        At(1.2, () => _target = _view.Panel.SocketAt(Smelter()) ?? _target);
        At(2.6, () => Go(new Place(ArcSlot)));
        At(4.2, () => Click(shift: true));
        At(5.0, () => _target = _view.Panel.SocketAt(Smelter()) ?? _target);
        // Out into the first empty slot, the hotbar's first, and in again from there.
        At(6.2, () => Click(shift: true));
        At(6.8, () => Go(new Place(0)));
        At(7.6, () => Click(shift: true));
        // Away from the panel, so the smelter shows crackling.
        At(8.0, () => _target = new Vector2(1100, 300));
        At(12.0, () => Go(new Place(0, Smelter())));
        At(12.5, () => Click());
        At(12.7, () => Go(new Place(RockSlot)));
        At(13.2, () => Click());
        At(13.6, () => _target = new Vector2(1100, 300));
    }

    private int Smelter() => _m().Grid.MachinesOf("smelter")[0].Id;

    private void At(double t, Action step) => _steps.Add((t, step));

    private void Go(Place place) => _target = _view.Panel.CenterOf(place);

    public override void _Process(double delta)
    {
        _t += delta;
        while (_next < _steps.Count && _steps[_next].At <= _t) _steps[_next++].Do();
        if (_pos != _target)
        {
            _pos = _pos.MoveToward(_target, (float)(3000 * delta));
            Input.ParseInputEvent(new InputEventMouseMotion { Position = _pos, GlobalPosition = _pos });
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2[] arrow = [_pos, _pos + new Vector2(0, 14), _pos + new Vector2(4, 11), _pos + new Vector2(9, 11)];
        DrawColoredPolygon(arrow, Colors.White);
        DrawPolyline([.. arrow, _pos], Colors.Black, 1);
    }

    private void Click(bool shift = false)
    {
        foreach (bool pressed in new[] { true, false })
            Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed, Position = _pos, GlobalPosition = _pos, ShiftPressed = shift });
    }
}
