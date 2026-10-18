using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Debris;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --handbook</c>: the wreck, its world having found ten kinds of debris (a supply pod with no seeds out of it yet,
/// a cargo container with no gear), and a script through the Handbook (0066): J opening it, the container chosen, the
/// pointer on a silhouetted gear, on planks, on a kind not found, on the pod, then the pod chosen and its "caught by hand";
/// the crate chosen, a look of it not found pointed at, another found shown big, the counter, the scrap chunk; Esc closing it, and again opening the menu with its Handbook line. Played through Godot's own input, for recordings
/// with <c>--write-movie</c>; each step is printed with its time.
/// </summary>
public partial class HandbookDemo(HandbookPage page) : Node
{
    /// <summary>The kinds found, the one item of each not seen come out of it yet, and the looks found of each.</summary>
    private static readonly (string Kind, string? Unseen, int[] Looks)[] Found =
    [
        ("cargoCrate", null, [2, 0, 5]), ("scrapChunk", null, [1, 3, 4, 7, 10]), ("hullPlate", null, [0, 1, 2, 3, 4, 5]),
        ("supplyPod", "seed", [1, 2]), ("hullSection", null, [0]), ("cargoContainer", "gear", [0]), ("cableRack", null, [0]),
        ("tankCluster", null, [0]), ("asteroidSmall", null, [2]), ("asteroid", null, [0, 3]),
    ];

    /// <summary><paramref name="world"/>, its record of debris found put into its save.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        var found = new JsonArray();
        foreach (var (kind, unseen, looks) in Found)
        {
            var items = Finds.SlotsOf(kind, null).Select(s => s.Item).Where(i => i != unseen).Select(i => (JsonNode?)JsonValue.Create(i));
            found.Add(new JsonObject
            {
                ["kind"] = kind,
                ["items"] = new JsonArray([.. items]),
                ["looks"] = new JsonArray([.. looks.Select(l => (JsonNode?)JsonValue.Create(l))]),
            });
        }
        root["slices"]!["debris"]!["found"] = found;
        return World.Load(root.ToJsonString(), modules());
    }

    private double _t;
    private int _step;

    public override void _Process(double delta)
    {
        _t += delta;
        (double At, string What, Action Do)[] script =
        [
            (0.8, "J opens it", () => Press(Key.J)),
            (1.6, "choose cargoContainer", () => Click(page.CenterOfKind("cargoContainer"))),
            (2.4, "point at its gear", () => Point(page.CenterOfLoot("gear"))),
            (3.2, "point at its planks", () => Point(page.CenterOfLoot("planks"))),
            (4.0, "point at engineBlock, not found", () => Point(page.CenterOfKind("engineBlock"))),
            (4.8, "point at supplyPod", () => Point(page.CenterOfKind("supplyPod"))),
            (5.6, "choose supplyPod", () => Click(page.CenterOfKind("supplyPod"))),
            (6.4, "point at caught by hand", () => Point(page.CenterOfHow())),
            (7.2, "point at its seed", () => Point(page.CenterOfLoot("seed"))),
            (8.0, "choose cargoCrate", () => Click(page.CenterOfKind("cargoCrate"))),
            (8.8, "point at its look 2, not found", () => Point(page.CenterOfLook(1))),
            (9.6, "click its look 6", () => Click(page.CenterOfLook(5))),
            (10.4, "point at the counter", () => Point(page.CenterOfTally())),
            (11.2, "choose scrapChunk", () => Click(page.CenterOfKind("scrapChunk"))),
            (12.0, "Esc closes it", () => Press(Key.Escape)),
            (12.6, "Esc opens the menu, its Handbook line", () => Press(Key.Escape)),
        ];
        if (_step >= script.Length || _t < script[_step].At) return;
        GD.Print($"[demo] {script[_step].What} at {_t:0.00} s");
        script[_step++].Do();
    }

    private static void Press(Key key)
    {
        foreach (bool down in (bool[])[true, false])
            Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = down });
    }

    private static void Point(Vector2? at)
    {
        if (at is not { } p) return;
        Input.ParseInputEvent(new InputEventMouseMotion { Position = p, GlobalPosition = p });
    }

    private static void Click(Vector2? at)
    {
        if (at is not { } p) return;
        Point(p);
        foreach (bool down in (bool[])[true, false])
            Input.ParseInputEvent(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = down });
    }
}
