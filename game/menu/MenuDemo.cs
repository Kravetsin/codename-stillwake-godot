using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Items;
using Stillwake.Game.Render;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Menu;

/// <summary>
/// <c>-- --menu-demo</c>: in <see cref="ItemsDemo"/>'s room, a script played through Godot's own input
/// for recordings: the inventory opened and a hotbar slot and an inventory slot hovered, their names
/// over every panel; Esc closing the inventory, Esc opening the menu; D held and Tab pressed behind it,
/// which the hero ignores; a colour picked in the settings, Esc back, the world saved, Esc closing the
/// menu. It draws the pointer, which a movie does not, and says where the hero stands around the menu.
/// </summary>
public partial class MenuDemo : CanvasLayer
{
    private readonly ItemsView _items;
    private readonly GameMenu _menu;
    private readonly SuitColours _suits;
    private readonly Node2D _arrow = new();
    private readonly List<(double At, Action Do)> _steps = [];
    private Vector2 _pos = new(640, 360), _target = new(640, 360);
    private double _t;
    private int _next;

    public MenuDemo(ItemsView items, GameMenu menu, SuitColours suits)
    {
        (_items, _menu, _suits) = (items, menu, suits);
        Layer = ScreenLayers.Pointer;
        AddChild(_arrow);
        _arrow.Draw += () =>
        {
            Vector2[] arrow = [_pos, _pos + new Vector2(0, 14), _pos + new Vector2(4, 11), _pos + new Vector2(9, 11)];
            _arrow.DrawColoredPolygon(arrow, Colors.White);
            _arrow.DrawPolyline([.. arrow, _pos], Colors.Black, 1);
        };
        At(0.5, () => Press(Key.Tab));
        // A hotbar slot's name, where the suit's gauge lies under it, then an inventory slot's.
        At(0.6, () => _target = _items.Panel.CenterOf(new Place(0)));
        At(1.5, () => _target = _items.Panel.CenterOf(new Place(9)));
        At(2.3, () => Press(Key.Escape));
        At(2.6, () => Press(Key.Escape));
        At(2.7, () => Say("menu open"));
        At(2.8, () => Hold(Key.D, true));
        At(2.9, () => Press(Key.Tab));
        At(3.0, () => _target = _menu.CenterOf("menu.settings"));
        At(3.4, Click);
        At(3.5, () => _target = _suits.CenterOf(2));
        At(3.6, () => Hold(Key.D, false));
        At(3.6, () => Say("D let go"));
        At(3.9, Click);
        At(4.4, () => Press(Key.Escape));
        At(4.6, () => _target = _menu.CenterOf("menu.save"));
        At(4.9, Click);
        At(5.5, () => Press(Key.Escape));
        At(5.6, () => Say("menu closed"));
    }

    private void At(double t, Action step) => _steps.Add((t, step));

    private void Say(string what) =>
        GD.Print($"[menu-demo] {what}: page {_menu.Page}, hero at x {_items.Hero?.X}, inventory {(_items.Panel.Shown ? "open" : "closed")}");

    public override void _Process(double delta)
    {
        _t += delta;
        while (_next < _steps.Count && _steps[_next].At <= _t) _steps[_next++].Do();
        if (_pos != _target)
        {
            _pos = _pos.MoveToward(_target, (float)(3000 * delta));
            Input.ParseInputEvent(new InputEventMouseMotion { Position = _pos, GlobalPosition = _pos });
        }
        _arrow.QueueRedraw();
    }

    private void Click()
    {
        foreach (bool pressed in new[] { true, false })
            Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed, Position = _pos, GlobalPosition = _pos });
    }

    private static void Hold(Key key, bool pressed) =>
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed });

    private static void Press(Key key)
    {
        Hold(key, true);
        Hold(key, false);
    }
}
