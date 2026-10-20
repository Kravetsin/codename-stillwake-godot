using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --charge</c>: the testbed with batteries on the belt and the hero by the charging station, for
/// recording charge on screen (0053, 0081). The script opens the inventory and points at the belt's first battery,
/// its card showing, closes it, then opens the station charging its battery and points at that one; then it moves
/// the belt's first battery into the bag and back, the energy gauge losing a quarter and filling again.
/// </summary>
public partial class ChargeDemo : Control
{
    /// <summary>What the belt's batteries hold at the start, in thousandths of full: the first well up, the second low.</summary>
    public static readonly int[] Belt = [800, 150];

    /// <summary>Where an empty battery lies in the hero's own slots, and the fire axe on the hotbar.</summary>
    public const int EmptySlot = 9, AxeSlot = 0;

    /// <summary>The testbed's world with the batteries and the axe in peer 0's inventory.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        var items = root["slices"]!["items"]!;
        var slots = new JsonArray();
        var belt = Equipment.Of(Spec.Charge.Belt).Select(p => p.Slot).ToList();
        static JsonObject Battery(int thousandths) =>
            new() { ["item"] = "battery", ["count"] = 1, ["charge"] = Spec.Charge.Battery / 1000 * thousandths };
        for (int i = 0; i < Spec.Inventory.Total; i++)
        {
            int at = belt.IndexOf(i);
            slots.Add(at >= 0 && at < Belt.Length ? Battery(Belt[at])
                : i == EmptySlot ? Battery(0)
                : i == AxeSlot ? new JsonObject { ["item"] = Spec.Weapons.Axe.Item, ["count"] = 1 } : null);
        }
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), modules());
    }

    /// <summary>Where the hero starts: a few steps left of the charging station, which shows past it.</summary>
    public static int StartOf(Registry m) => m.Grid.MachinesOf("charger")[0].Cell + m.Grid.Width - 3;

    private readonly ItemsView _view;
    private readonly Ui _ui;
    private readonly Func<Registry> _m;
    private readonly List<(double At, Action Do)> _steps = [];
    private Vector2 _pos = new(640, 360), _target = new(640, 360);
    private double _t;
    private int _next;

    public ChargeDemo(ItemsView view, Ui ui, Func<Registry> m)
    {
        (_view, _ui, _m) = (view, ui, m);
        MouseFilter = MouseFilterEnum.Ignore;
        At(0.5, () => Press(Key.Key1));
        At(1.0, () => Press(Key.Tab));
        At(1.3, () => Go(Equipment.Of(Spec.Charge.Belt).First()));
        At(3.0, () => Go(new Place(EmptySlot)));
        At(4.5, () => Press(Key.Tab));
        At(4.8, () => _ui.OpenCrate.Set(_m().Grid.MachinesOf("charger")[0].Id));
        At(5.2, () => _target = new Vector2(900, 500));
        At(6.5, () => Go(new Place(0, _m().Grid.MachinesOf("charger")[0].Id)));
        At(8.0, () => Go(Equipment.Of(Spec.Charge.Belt).First()));
        At(8.5, () => Click());
        At(8.7, () => Go(new Place(EmptySlot + 3)));
        At(9.2, () => Click());
        At(10.2, () => Click());
        At(10.4, () => Go(Equipment.Of(Spec.Charge.Belt).First()));
        At(10.9, () => Click());
    }

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

    private void Click()
    {
        foreach (bool pressed in new[] { true, false })
            Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed, Position = _pos, GlobalPosition = _pos });
    }

    private static void Press(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
    }
}
