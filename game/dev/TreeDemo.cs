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
    private TabletHands _hands = null!;
    private ResearchTablet _tree = null!;

    public override void _Process(double delta)
    {
        _t += delta;
        _hands ??= new TabletHands(this);
        _tree ??= GetParent().GetChildren().OfType<ResearchTablet>().First();
        var away = new Vector2(-120, -60);
        (double At, string What, Action Do)[] script =
        [
            (0.8, "T opens it", () => TabletHands.Press(Key.T)),
            (1.4, "from the board to the ship's rings", () => _hands.Mouse(_tree.TabAt(1), MouseButton.Left, true)),
            (1.45, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (2.0, "point at the workbench", () => _hands.Point(_tree.ScreenOf("workbench"))),
            (2.5, "press on the workbench and drag", () => _hands.Mouse(_tree.ScreenOf("workbench"), MouseButton.Left, true)),
            (2.6, "dragging", () => _hands.Move(_hands.Last + away, MouseButton.Left)),
            (2.7, "let go: nothing picked", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (3.0, "drag back", () => _hands.Mouse(_tree.ScreenOf("workbench"), MouseButton.Left, true)),
            (3.1, "dragging", () => _hands.Move(_hands.Last - away, MouseButton.Left)),
            (3.2, "let go: nothing picked", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (3.5, "click the workbench", () => _hands.Mouse(_tree.ScreenOf("workbench"), MouseButton.Left, true)),
            (3.55, "let go: picked, nothing bought", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (4.5, "press the button", () => _hands.Mouse(_tree.ActionAt, MouseButton.Left, true)),
            (4.55, "let go: bought", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (5.0, "wheel out a little: prices stay under their names", () => _hands.Wheel(_tree.ScreenOf("workbench"), MouseButton.WheelDown, 2)),
            (5.1, "further: pictures, no words", () => _hands.Wheel(_tree.ScreenOf("workbench"), MouseButton.WheelDown, 3)),
            (5.2, "point at the furnace: its tip", () => _hands.Point(_tree.ScreenOf("furnace"))),
            (5.5, "far: plain dots", () => _hands.Wheel(_tree.ScreenOf("workbench"), MouseButton.WheelDown, 3)),
            (6.5, "click the plate, not yet open", () => _hands.Mouse(_tree.ScreenOf("plate"), MouseButton.Left, true)),
            (6.55, "let go: its ties lit", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (7.5, "click the hub", () => _hands.Mouse(_tree.HubAt, MouseButton.Left, true)),
            (7.55, "let go: the gate picked", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (8.0, "put in what is carried", () => _hands.Mouse(_tree.ActionAt, MouseButton.Left, true)),
            (8.05, "let go: the iron all in, the scrap part", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (8.5, "wheel in on the hub", () => _hands.Wheel(_tree.HubAt, MouseButton.WheelUp, 8)),
            (9.0, "the other tab", () => _hands.Mouse(_tree.TabAt(2), MouseButton.Left, true)),
            (9.05, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (9.2, "the black market", () => _hands.Mouse(_tree.TabAt(3), MouseButton.Left, true)),
            (9.25, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (9.35, "click the solar panel on sale", () => _hands.Mouse(_tree.ScreenOf("solarPanel"), MouseButton.Left, true)),
            (9.4, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (9.6, "T puts it away", () => TabletHands.Press(Key.T)),
            (10.1, "Tab: the bag, the credits on its metal", () => TabletHands.Press(Key.Tab)),
        ];
        while (_step < script.Length && _t >= script[_step].At)
        {
            GD.Print($"{script[_step].At}s: {script[_step].What}; {m().Research.Credits} credits");
            script[_step++].Do();
        }
    }
}
