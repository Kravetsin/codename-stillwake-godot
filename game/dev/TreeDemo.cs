using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Research;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --tree</c>: the wreck with credits to spend, and a script through the tech tree on the tablet: T taking it out on the first
/// thing to do, a drag starting on that node (which must buy nothing), a click on it, the wheel out to the whole map and in again,
/// T putting it away. Played through Godot's own input, for recordings with <c>--write-movie</c>; each step is printed with its time.
/// </summary>
public partial class TreeDemo(Func<Registry> m) : Node
{
    /// <summary>What the world starts with: a few first blocks' worth.</summary>
    private const int Credits = 1500;

    /// <summary><paramref name="world"/> with <see cref="Credits"/> in its save.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["knowledge"]!["credits"] = Credits;
        return World.Load(root.ToJsonString(), modules());
    }

    private double _t;
    private int _step;
    private Vector2 _last;
    private TechPanel _tree = null!;

    public override void _Process(double delta)
    {
        _t += delta;
        _tree ??= GetParent().GetChildren().OfType<TechPanel>().First();
        var away = new Vector2(-120, -60);
        (double At, string What, Action Do)[] script =
        [
            (0.8, "T opens it", () => Press(Key.T)),
            (2.0, "point at the workbench", () => Point(_tree.ScreenOf("workbench"))),
            (3.0, "press on the workbench and drag", () => Mouse(_tree.ScreenOf("workbench"), MouseButton.Left, true)),
            (3.1, "dragging", () => Move(_last + away, MouseButton.Left)),
            (3.2, "let go: nothing bought", () => Mouse(_last, MouseButton.Left, false)),
            (3.6, "drag back", () => Mouse(_tree.ScreenOf("workbench"), MouseButton.Left, true)),
            (3.7, "dragging", () => Move(_last - away, MouseButton.Left)),
            (3.8, "let go: nothing bought", () => Mouse(_last, MouseButton.Left, false)),
            (4.5, "click the workbench", () => Mouse(_tree.ScreenOf("workbench"), MouseButton.Left, true)),
            (4.55, "let go: bought", () => Mouse(_last, MouseButton.Left, false)),
            (5.5, "wheel out", () => Wheel(_tree.ScreenOf("workbench"), MouseButton.WheelDown, 12)),
            (6.5, "point at the furnace, all the way out", () => Point(_tree.ScreenOf("furnace"))),
            (7.5, "wheel in", () => Wheel(_tree.ScreenOf("furnace"), MouseButton.WheelUp, 5)),
            (8.5, "T puts it away", () => Press(Key.T)),
        ];
        while (_step < script.Length && _t >= script[_step].At)
        {
            GD.Print($"{script[_step].At}s: {script[_step].What}; {m().Research.Credits} credits");
            script[_step++].Do();
        }
    }

    private void Point(Vector2 at)
    {
        GetViewport().WarpMouse(at);
        Move(at, null);
    }

    private void Move(Vector2 at, MouseButton? held)
    {
        Input.ParseInputEvent(new InputEventMouseMotion
        {
            Position = at,
            GlobalPosition = at,
            Relative = at - _last,
            ButtonMask = held == null ? 0 : MouseButtonMask.Left,
        });
        _last = at;
    }

    private void Mouse(Vector2 at, MouseButton button, bool pressed)
    {
        _last = at;
        GetViewport().WarpMouse(at);
        Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = button, Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : 0 });
    }

    private void Wheel(Vector2 at, MouseButton button, int notches)
    {
        GetViewport().WarpMouse(at);
        for (int i = 0; i < notches; i++)
        {
            Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = button, Pressed = true });
            Input.ParseInputEvent(new InputEventMouseButton { Position = at, GlobalPosition = at, ButtonIndex = button, Pressed = false });
        }
    }

    private static void Press(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = false });
    }
}
