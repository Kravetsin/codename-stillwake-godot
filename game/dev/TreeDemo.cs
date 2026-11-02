using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Research;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --tree</c>: the wreck with credits to spend, and a script through the black market on the tablet: T taking it out, a drag starting on
/// a node (which must pick nothing), a click on it (which must buy nothing), the strip's button buying it, the wheel out to the whole map,
/// a node's ties, the hub with an order part sold, T putting it away, the bag. An order met before T lights the key. Played through Godot's own input, for recordings with
/// <c>--write-movie</c>; each step is printed with its time.
/// </summary>
public partial class TreeDemo(Func<Registry> m) : Node
{
    /// <summary>What the world starts with: a few first slots' worth.</summary>
    private const int Credits = 1500;

    /// <summary><paramref name="world"/> with <see cref="Credits"/> in its save.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["knowledge"]!["credits"] = Credits;
        // Enough of the furnace's parts: a note on the board.
        root["slices"]!["knowledge"]!["obtained"] = new JsonArray(new JsonObject { ["item"] = "scrapChunk", ["count"] = 10 }, new JsonObject { ["item"] = "hullPlate", ["count"] = 5 });
        // The first order sold, the second all but its plates, the third begun.
        root["slices"]!["knowledge"]!["sold"] = new JsonArray(
            new JsonObject { ["item"] = "gear", ["count"] = 20 },
            new JsonObject { ["item"] = "glass", ["count"] = 8 },
            new JsonObject { ["item"] = "iron", ["count"] = 20 },
            new JsonObject { ["item"] = "planks", ["count"] = 20 },
            new JsonObject { ["item"] = "plate", ["count"] = 10 },
            new JsonObject { ["item"] = "scrapChunk", ["count"] = 40 });
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
            (0.5, "the plates sold: an order met, the key lit", () => m().Knowledge.Sell("plate", 20)),
            (1.0, "T opens it: the market's tab lit", () => TabletHands.Press(Key.T)),
            (1.4, "from the board to the black market: its tab no longer lit", () => _hands.Mouse(_tree.TabAt(1), MouseButton.Left, true)),
            (1.45, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (2.0, "point at the first slot", () => _hands.Point(_tree.ScreenOf("bag1"))),
            (2.5, "press on it and drag", () => _hands.Mouse(_tree.ScreenOf("bag1"), MouseButton.Left, true)),
            (2.6, "dragging", () => _hands.Move(_hands.Last + away, MouseButton.Left)),
            (2.7, "let go: nothing picked", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (3.0, "drag back", () => _hands.Mouse(_tree.ScreenOf("bag1"), MouseButton.Left, true)),
            (3.1, "dragging", () => _hands.Move(_hands.Last - away, MouseButton.Left)),
            (3.2, "let go: nothing picked", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (3.5, "click the first slot", () => _hands.Mouse(_tree.ScreenOf("bag1"), MouseButton.Left, true)),
            (3.55, "let go: picked, nothing bought", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (4.5, "press the button", () => _hands.Mouse(_tree.ActionAt, MouseButton.Left, true)),
            (4.55, "let go: bought", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (5.0, "wheel out a little: prices stay under their names", () => _hands.Wheel(_tree.ScreenOf("bag1"), MouseButton.WheelDown, 2)),
            (5.1, "further: pictures, no words", () => _hands.Wheel(_tree.ScreenOf("bag1"), MouseButton.WheelDown, 3)),
            (5.2, "point at the solar panel: its tip", () => _hands.Point(_tree.ScreenOf("solarPanel"))),
            (5.5, "far: plain dots", () => _hands.Wheel(_tree.ScreenOf("bag1"), MouseButton.WheelDown, 3)),
            (6.5, "click the solar panel on sale", () => _hands.Mouse(_tree.ScreenOf("solarPanel"), MouseButton.Left, true)),
            (6.55, "let go: its ties lit", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (7.5, "click the hub: what is sold of the order, no button", () => _hands.Mouse(_tree.HubAt, MouseButton.Left, true)),
            (7.55, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (8.5, "wheel in on the hub", () => _hands.Wheel(_tree.HubAt, MouseButton.WheelUp, 8)),
            (9.5, "T puts it away", () => TabletHands.Press(Key.T)),
            (10.0, "Tab: the bag, the credits on its metal", () => TabletHands.Press(Key.Tab)),
        ];
        while (_step < script.Length && _t >= script[_step].At)
        {
            GD.Print($"{script[_step].At}s: {script[_step].What}; {m().Research.Credits} credits");
            script[_step++].Do();
        }
    }
}
