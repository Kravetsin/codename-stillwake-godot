using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Items;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --skins</c>: the Appearance tab, for recording (hero-skins). The script opens the inventory, turns the equipment's screen
/// to the skins by its key, opens the weapon effect's strip and points at «Пламя» in it; with <c>--own-none</c> it shows locked.
/// </summary>
public partial class SkinsDemo : Control
{
    private readonly List<(double At, Action Do)> _steps = [];
    private Vector2 _pos = new(640, 360), _target = new(640, 360);
    private double _t;
    private int _next;

    public SkinsDemo()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        At(1.0, () => Press(Key.Tab));
        At(1.3, () => _target = CenterOf(All<LooksButton>().First()));
        At(1.8, Click);
        At(2.1, () => _target = CenterOf(All<SkinSlot>().ElementAt(3)));
        At(2.6, Click);
        At(2.9, () => _target = CenterOf(All<SkinSlot>().ElementAt(5)));
    }

    private void At(double t, Action step) => _steps.Add((t, step));

    private IEnumerable<T> All<T>() where T : Control => Under(GetTree().Root).OfType<T>().Where(c => c.IsVisibleInTree());

    private static IEnumerable<Node> Under(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            yield return child;
            foreach (var deeper in Under(child)) yield return deeper;
        }
    }

    private static Vector2 CenterOf(Control c) => c.GetGlobalRect().GetCenter();

    private static void Press(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
    }

    private void Click()
    {
        foreach (bool pressed in (bool[])[true, false])
            Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed, Position = _pos, GlobalPosition = _pos });
    }

    public override void _Process(double delta)
    {
        _t += delta;
        while (_next < _steps.Count && _steps[_next].At <= _t) _steps[_next++].Do();
        if (_pos == _target) return;
        _pos = _pos.MoveToward(_target, (float)(3000 * delta));
        Input.ParseInputEvent(new InputEventMouseMotion { Position = _pos, GlobalPosition = _pos });
    }
}
