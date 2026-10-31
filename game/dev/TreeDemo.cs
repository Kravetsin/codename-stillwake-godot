using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Research;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --tree</c>: the wreck with credits to spend, and a script through the tech tree on the tablet: T taking it out, a drag starting on
/// a node (which must pick nothing), a click on it (which must buy nothing), the strip's button buying it, a far node's ties, the wheel
/// out to the whole map, the hub, the other tab, T putting it away, the bag. Played through Godot's own input, for recordings with
/// <c>--write-movie</c>; each step is printed with its time.
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
        // Enough of the furnace's parts: it shows done, unbought.
        root["slices"]!["knowledge"]!["obtained"] = new JsonArray(new JsonObject { ["item"] = "scrapChunk", ["count"] = 10 }, new JsonObject { ["item"] = "hullPlate", ["count"] = 5 });
        var slots = new JsonArray();
        for (int i = 0; i < Content.Inventory.Total; i++) slots.Add(null);
        slots[0] = new JsonObject { ["item"] = "scrap", ["count"] = 12 };
        slots[1] = new JsonObject { ["item"] = "iron", ["count"] = 30 };
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
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
            (2.5, "press on the workbench and drag", () => Mouse(_tree.ScreenOf("workbench"), MouseButton.Left, true)),
            (2.6, "dragging", () => Move(_last + away, MouseButton.Left)),
            (2.7, "let go: nothing picked", () => Mouse(_last, MouseButton.Left, false)),
            (3.0, "drag back", () => Mouse(_tree.ScreenOf("workbench"), MouseButton.Left, true)),
            (3.1, "dragging", () => Move(_last - away, MouseButton.Left)),
            (3.2, "let go: nothing picked", () => Mouse(_last, MouseButton.Left, false)),
            (3.5, "click the workbench", () => Mouse(_tree.ScreenOf("workbench"), MouseButton.Left, true)),
            (3.55, "let go: picked, nothing bought", () => Mouse(_last, MouseButton.Left, false)),
            (4.5, "press the button", () => Mouse(_tree.ActionAt, MouseButton.Left, true)),
            (4.55, "let go: bought", () => Mouse(_last, MouseButton.Left, false)),
            (5.0, "wheel out a little: prices stay under their names", () => Wheel(_tree.ScreenOf("workbench"), MouseButton.WheelDown, 2)),
            (5.1, "further: pictures, no words", () => Wheel(_tree.ScreenOf("workbench"), MouseButton.WheelDown, 3)),
            (5.2, "point at the furnace: its tip", () => Point(_tree.ScreenOf("furnace"))),
            (5.5, "far: plain dots", () => Wheel(_tree.ScreenOf("workbench"), MouseButton.WheelDown, 3)),
            (6.5, "click the plate, not yet open", () => Mouse(_tree.ScreenOf("plate"), MouseButton.Left, true)),
            (6.55, "let go: its ties lit", () => Mouse(_last, MouseButton.Left, false)),
            (7.5, "click the hub", () => Mouse(_tree.HubAt, MouseButton.Left, true)),
            (7.55, "let go: the gate picked", () => Mouse(_last, MouseButton.Left, false)),
            (8.0, "put in what is carried", () => Mouse(_tree.ActionAt, MouseButton.Left, true)),
            (8.05, "let go: the iron all in, the scrap part", () => Mouse(_last, MouseButton.Left, false)),
            (8.5, "wheel in on the hub", () => Wheel(_tree.HubAt, MouseButton.WheelUp, 8)),
            (9.0, "the other tab", () => Mouse(_tree.TabAt(1), MouseButton.Left, true)),
            (9.05, "let go", () => Mouse(_last, MouseButton.Left, false)),
            (9.2, "the black market", () => Mouse(_tree.TabAt(2), MouseButton.Left, true)),
            (9.25, "let go", () => Mouse(_last, MouseButton.Left, false)),
            (9.35, "click the solar panel on sale", () => Mouse(_tree.ScreenOf("solarPanel"), MouseButton.Left, true)),
            (9.4, "let go", () => Mouse(_last, MouseButton.Left, false)),
            (9.6, "T puts it away", () => Press(Key.T)),
            (10.1, "Tab: the bag, the credits on its metal", () => Press(Key.Tab)),
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
