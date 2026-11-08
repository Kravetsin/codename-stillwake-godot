using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Research;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --tree</c>: the wreck with credits to spend, and a script through the black market on the tablet: T taking it out, an order met while away deciphering, a click on a
/// tile (which must buy nothing), the strip's button buying its next step twice, a tile too dear, an order filled with the market open, the last two filled with it away, the
/// blueprints' tab, T putting it away, the bag. An order met before T lights the key. Played through Godot's own input, for recordings with <c>--write-movie</c>; each step is
/// printed with its time.
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
            (1.0, "T opens it on the board: the market's key lit, the tablet's dark", () => TabletHands.Press(Key.T)),
            (1.4, "M goes over to the black market: the second row deciphering", () => TabletHands.Press(Key.M)),
            (3.6, "point at the inventory slot: its name", () => _hands.Point(_tree.ScreenOf("bag"))),
            (4.0, "click it: picked, nothing bought", () => _hands.Mouse(_tree.ScreenOf("bag"), MouseButton.Left, true)),
            (4.05, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (4.5, "press the button", () => _hands.Mouse(_tree.ActionAt, MouseButton.Left, true)),
            (4.55, "let go: the first bought, the tile asks for the second", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (5.0, "press it again", () => _hands.Mouse(_tree.ActionAt, MouseButton.Left, true)),
            (5.05, "let go: two bought", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (5.6, "click the belt slot, too dear: the button says so", () => _hands.Mouse(_tree.ScreenOf("belt"), MouseButton.Left, true)),
            (5.65, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (6.4, "the third order sold with the market open: it plays at once", () =>
            {
                foreach (var (item, count) in (ReadOnlySpan<(string, int)>)[("glass", 12), ("silicon", 20), ("frame", 10)]) m().Knowledge.Sell(item, count);
            }),
            (8.6, "M puts it away from the market", () => TabletHands.Press(Key.M)),
            (9.0, "the last two orders sold with it away", () =>
            {
                foreach (var (item, count) in (ReadOnlySpan<(string, int)>)[("circuit", 20), ("copper", 20), ("mica", 10), ("hide", 5)]) m().Knowledge.Sell(item, count);
            }),
            (9.6, "the market's key on the edge opens it on the market: two rows deciphering together", () => _hands.Mouse(_tree.MarketCap.GetGlobalRect().GetCenter(), MouseButton.Left, true)),
            (9.65, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (11.8, "the blueprints' tab: the solar panel alone", () => _hands.Mouse(_tree.ScreenOf("blueprints"), MouseButton.Left, true)),
            (11.85, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (12.3, "click the solar panel, opened by the fourth order: what it is made from", () => _hands.Mouse(_tree.ScreenOf("solarPanel"), MouseButton.Left, true)),
            (12.35, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
            (13.1, "T puts it away", () => TabletHands.Press(Key.T)),
            (13.6, "Tab: the bag, the credits on its metal", () => TabletHands.Press(Key.Tab)),
        ];
        while (_step < script.Length && _t >= script[_step].At)
        {
            GD.Print($"{script[_step].At}s: {script[_step].What}; {m().Research.Credits} credits");
            script[_step++].Do();
        }
    }
}
