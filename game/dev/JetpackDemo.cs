using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Items;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --jetpack</c>: the testbed with a jetpack and canisters carried, for recording the equipment and the
/// flight (0047). Its panel script opens the inventory, drags the jetpack onto the back, the back slot lit
/// while it is carried, and closes it; the hero's autopilot (<c>HeroAutopilot.Jetpack</c>) then leaves by the
/// airlock and flies; at the end a canister is breathed from the hotbar with the right button.
/// </summary>
public partial class JetpackDemo : Control
{
    /// <summary>Where the jetpack and the canisters lie in the hero's inventory at the start.</summary>
    public const int JetpackSlot = 9, CanisterSlot = 0;

    /// <summary>The testbed's world with the jetpack and canisters in peer 0's inventory and creative mode off, so a canister is spent.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        var items = root["slices"]!["items"]!;
        items["creative"] = false;
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == JetpackSlot ? new JsonObject { ["item"] = Spec.Jetpack.Item, ["count"] = 1 }
                : i == CanisterSlot ? new JsonObject { ["item"] = Spec.Crew.Canister, ["count"] = 3 } : null);
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), modules());
    }

    private readonly ItemsView _view;
    private readonly List<(double At, Action Do)> _steps = [];
    private Vector2 _pos = new(640, 360), _target = new(640, 360);
    private double _t;
    private int _next;

    public JetpackDemo(ItemsView view)
    {
        _view = view;
        MouseFilter = MouseFilterEnum.Ignore;
        At(0.4, () => Press(Key.Tab));
        At(0.6, () => Go(new Place(JetpackSlot)));
        At(1.1, () => Button(MouseButton.Left, true));
        At(1.2, () => Go(Equipment.Of("back").First()));
        At(2.2, () => Button(MouseButton.Left, false));
        At(2.5, () => Press(Key.Tab));
        At(2.6, () => _target = new Vector2(1100, 200));
        // Back in after the flight: the canisters chosen on the hotbar, one breathed into the suit.
        At(15.0, () => Press(Key.Key1));
        At(15.4, () => Click(MouseButton.Right));
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
            Input.ParseInputEvent(new InputEventMouseMotion { Position = _pos, GlobalPosition = _pos, ButtonMask = _held });
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2[] arrow = [_pos, _pos + new Vector2(0, 14), _pos + new Vector2(4, 11), _pos + new Vector2(9, 11)];
        DrawColoredPolygon(arrow, Colors.White);
        DrawPolyline([.. arrow, _pos], Colors.Black, 1);
    }

    private MouseButtonMask _held;

    private void Click(MouseButton button)
    {
        Button(button, true);
        Button(button, false);
    }

    private void Button(MouseButton button, bool pressed)
    {
        var mask = button == MouseButton.Left ? MouseButtonMask.Left : MouseButtonMask.Right;
        _held = pressed ? _held | mask : _held & ~mask;
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = button,
            Pressed = pressed,
            ButtonMask = _held,
            Position = _pos,
            GlobalPosition = _pos,
        });
    }

    private static void Press(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
    }
}
