using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Research;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --board</c>: the board on the tablet, a world's counts seeded through its save and nothing obtained during the run (board-islands).
/// Bare it is all «?»; <c>=first</c>, <c>=near</c>, <c>=burst</c>, <c>=circuit</c> and <c>=late</c> are stills along the way. Script: finds that light the
/// key (with something seeded; 6 with <c>=burst</c>, 15 with <c>=late</c>), T, a frame pointed at, the newest find clicked, a silhouette clicked, the wheel
/// out, the board dragged, T again, a press on the key; each step is printed with its time. <c>=bought</c> has everything found and the solar panel bought:
/// T, a click on it, the wheel all the way out. <c>=search</c>, the same world: the board's search typed in, a pick by the arrows and Enter, a name not
/// there, Esc. <c>=tools</c>, the same world wheeled all the way in over the screwdriver and it clicked: tool icons at the board's most zoom.
/// </summary>
public partial class BoardDemo(Func<Registry> m, Func<World?> world, string? value) : Node
{
    private const int Plenty = 40;

    /// <summary>What can be had in space, in the order a world might find it.</summary>
    private static readonly string[] InSpace =
    [
        "scrapChunk", "hullPlate", "planks", "scrap", "iron", "nail", "wire", "plate", "gear", "tube", "rotor", "coil", "frame", "canister",
        "cargoCrate", "rock", "quartz", "ice", "gravel", "ironOre", "asteroidOre", "silicon", "glass", "solarCell", "photoPanel", "circuit",
        "deckDrive", "battery", "drone", "processor", "display", "transceiver", "nozzle", "screwdriver", "fireAxe", "harpoon", "nailer",
        "helmetLamp", "jetpack", "drill",
    ];

    /// <summary>What each value seeds: what was obtained, in order, and how many.</summary>
    private static IReadOnlyList<(string Item, int Count)> Found(string? value) => value switch
    {
        "first" => [("scrapChunk", 3)],
        "near" => [("scrapChunk", 9), ("hullPlate", 4)],
        "burst" => [("scrap", Plenty), ("iron", Plenty)],
        "circuit" => [("frame", 2), ("solarCell", 6), ("wire", 4), ("circuit", 1), ("photoPanel", 1)],
        "late" => [.. InSpace.SkipLast(Finds(value)).Select(i => (i, Plenty))],
        "bought" or "search" or "tools" => [.. InSpace.Select(i => (i, Plenty))],
        _ => [],
    };

    /// <summary>How many new things the script finds with each value.</summary>
    private static int Finds(string? value) => value switch
    {
        "burst" => 6,
        "late" => 15,
        _ => 1,
    };

    /// <summary><paramref name="world"/> with what <paramref name="value"/> found in its save, and with <c>=burst</c> a workbench standing.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules, string? value)
    {
        if (value is "burst" or "late")
        {
            var grid = Registry.On(world, 0).Grid;
            var bench = new Placement("machine", "workbench");
            grid.Place(Enumerable.Range(0, grid.Width * grid.Height).First(c => grid.PlacementProblem(c, bench) == null), bench);
        }
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["knowledge"]!["obtained"] = new JsonArray([.. Found(value).Select(f => (JsonNode)new JsonObject { ["item"] = f.Item, ["count"] = f.Count })]);
        if (value is "bought" or "search") root["slices"]!["knowledge"]!["granted"] = new JsonArray("solarPanel");
        return World.Load(root.ToJsonString(), modules());
    }

    private double _t;
    private int _step;
    private TabletHands _hands = null!;
    private ResearchTablet _tablet = null!;

    /// <summary>The thing first on the board that shows as <paramref name="state"/>; null for none.</summary>
    private string? First(BoardState state)
    {
        if (world() is not { } w) return null;
        var read = BoardRead.Of(w);
        return BoardThings.Ids.FirstOrDefault(id => BoardView.StateOf(id, read) == state);
    }

    /// <summary>Things new found, for the key to light for: what the seed had not got to.</summary>
    private void Find() => FindOn(m().Knowledge, Finds(value));

    /// <summary><paramref name="count"/> things new found in <paramref name="knowledge"/>; none in a world that found nothing yet.</summary>
    public static void FindOn(Sim.Knowledge.KnowledgeModule knowledge, int count)
    {
        if (knowledge.Obtained.Count == 0) return;
        foreach (string item in InSpace.Where(i => knowledge.ObtainedOf(i) == 0).Take(count)) knowledge.Obtain(item, 1);
    }

    private void Click(string? id)
    {
        if (id == null) return;
        _hands.Mouse(_tablet.ScreenOf(id), MouseButton.Left, true);
    }

    private void Drag(Vector2 by) => _hands.Move(_hands.Last + by, MouseButton.Left);

    private void LetGo() => _hands.Mouse(_hands.Last, MouseButton.Left, false);

    public override void _Process(double delta)
    {
        _t += delta;
        _hands ??= new TabletHands(this);
        _tablet ??= GetParent().GetChildren().OfType<ResearchTablet>().First();
        (double At, string What, Action Do)[] script =
        [
            (0.5, "a find: the key lit, its count a batch", Find),
            (1.5, "T opens it on the board at the newest find, its frames glowing", () => TabletHands.Press(Key.T)),
            (2.5, "point at a frame: its ties lit, its glow out", () => { if (First(BoardState.Known) is { } id) _hands.Point(_tablet.ScreenOf(id)); }),
            (3.5, "click the newest line of finds: the board flies there", () => _hands.Mouse(_tablet.FirstFindAt, MouseButton.Left, true)),
            (3.55, "let go", LetGo),
            (4.5, "click a silhouette: what it lacks in the strip", () => Click(First(BoardState.Almost))),
            (4.55, "let go", LetGo),
            (5.5, "wheel out: the whole board, the districts' captions over them", () => _hands.Wheel(GetViewport().GetVisibleRect().GetCenter(), MouseButton.WheelDown, 8)),
            (6.5, "take the board", () => _hands.Mouse(GetViewport().GetVisibleRect().GetCenter(), MouseButton.Left, true)),
            (6.6, "drag it", () => Drag(new Vector2(-200, -100))),
            (6.7, "let go", LetGo),
            (7.5, "T puts it away", () => TabletHands.Press(Key.T)),
            (9.0, "press the key: the tablet again", () => _hands.Mouse(_tablet.KeyCovers.GetCenter(), MouseButton.Left, true)),
            (9.05, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
        ];
        if (value == "bought")
            script =
            [
                (0.5, "T opens it on the board", () => TabletHands.Press(Key.T)),
                (2.5, "click the solar panel", () => Click("solarPanel")),
                (2.55, "let go: its ties lit", LetGo),
                (4.0, "click off and wheel all the way out", () => { LetGo(); _hands.Wheel(GetViewport().GetVisibleRect().GetCenter(), MouseButton.WheelDown, 30); }),
            ];
        if (value == "tools")
            script =
            [
                (0.5, "T opens it on the board", () => TabletHands.Press(Key.T)),
                (4.0, "wheel all the way in over the screwdriver: the tools drawn large", () => _hands.Wheel(_tablet.ScreenOf("screwdriver"), MouseButton.WheelUp, 10)),
                (5.0, "click the screwdriver: its card's picture too", () => Click("screwdriver")),
                (5.05, "let go", LetGo),
            ];
        if (value == "search")
            script =
            [
                (0.5, "T opens it on the board", () => TabletHands.Press(Key.T)),
                (2.5, "wheel all the way out", () => _hands.Wheel(GetViewport().GetVisibleRect().GetCenter(), MouseButton.WheelDown, 30)),
                (3.0, "/ puts the keys in the board's search", () => TabletHands.Press(Key.Slash)),
                (3.3, "type iron's name: iron, then what it ties to, the board lit by the list", () => TabletHands.Type(I18n.En.T("item.iron").ToLowerInvariant())),
                (4.2, "down", () => TabletHands.Press(Key.Down)),
                (4.8, "down again", () => TabletHands.Press(Key.Down)),
                (5.6, "Enter: picked, the board glides to it", () => TabletHands.Press(Key.Enter)),
                (7.5, "/ and a name not on the board", () => { TabletHands.Press(Key.Slash); TabletHands.Type("zzz"); }),
                (8.5, "Esc empties the search, the tablet stays out", () => TabletHands.Press(Key.Escape)),
            ];
        while (_step < script.Length && _t >= script[_step].At)
        {
            GD.Print($"{script[_step].At}s: {script[_step].What}");
            script[_step++].Do();
        }
    }
}
