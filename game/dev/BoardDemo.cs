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
/// <c>-- --board</c>: the board on the tablet, a world's counts seeded through its save and nothing obtained during the run (docs/decisions/2026-11-05-the-board.md).
/// Bare it is blank; <c>=first</c>, <c>=near</c>, <c>=burst</c>, <c>=circuit</c> and <c>=late</c> are stills along the way. Script: finds
/// that light the key (with something seeded; 6 with <c>=burst</c>, 15 with <c>=late</c>), T, three entries clicked down, the rest taken at once, a click on the first note, a
/// click on a shape, the wheel out, a click off everything, the first note dragged away, the board itself dragged, T again, a press on the key; each step is printed with its time.
/// <c>=bought</c> has everything found and the solar panel bought: T, a click on it, the wheel all the way out, the first note carried to the edge. <c>=search</c>, the same world: the board's search typed in, a pick by the arrows and Enter, a name not there, Esc.
/// </summary>
public partial class BoardDemo(Func<Registry> m, string? value) : Node
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
        "bought" or "search" => [.. InSpace.Select(i => (i, Plenty))],
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

    /// <summary>The thing first in the board's list that shows as <paramref name="show"/>; null for none.</summary>
    private string? First(BoardShow show)
    {
        var read = BoardPage.ReadOf(m());
        var picture = BoardView.Of(BoardSlots.Of(read.Found, BoardThings.All, read.Granted), BoardThings.All, read, null);
        return picture.Shows.FirstOrDefault(s => s.Value == show).Key;
    }

    /// <summary>Things new found, for the key to light for: what the seed had not got to.</summary>
    private void Find() => FindOn(m().Knowledge, Finds(value));

    /// <summary><paramref name="count"/> things new found in <paramref name="knowledge"/>; none in a world that found nothing yet.</summary>
    public static void FindOn(Sim.Knowledge.KnowledgeModule knowledge, int count)
    {
        if (knowledge.Obtained.Count == 0) return;
        foreach (string item in InSpace.Where(i => knowledge.ObtainedOf(i) == 0).Take(count)) knowledge.Obtain(item, 1);
    }

    /// <summary>A press and release at <paramref name="at"/> while an entry is shown.</summary>
    private void PressWhileReading(Vector2 at)
    {
        if (!_tablet.Reading) return;
        _hands.Mouse(at, MouseButton.Left, true);
        _hands.Mouse(at, MouseButton.Left, false);
    }

    private void Click(string? id)
    {
        if (id == null) return;
        _hands.Mouse(_tablet.ScreenOf(id), MouseButton.Left, true);
    }

    private void Drag(Vector2 by)
    {
        if (First(BoardShow.Note) != null) _hands.Move(_hands.Last + by, MouseButton.Left);
    }

    private void LetGo()
    {
        if (First(BoardShow.Note) != null) _hands.Mouse(_hands.Last, MouseButton.Left, false);
    }

    public override void _Process(double delta)
    {
        _t += delta;
        _hands ??= new TabletHands(this);
        _tablet ??= GetParent().GetChildren().OfType<ResearchTablet>().First();
        (double At, string What, Action Do)[] script =
        [
            (0.5, "a find: the key lit", Find),
            (1.5, "T opens it on the board, the first entry large", () => TabletHands.Press(Key.T)),
            (2.5, "click the entry: it flies to its place", () => PressWhileReading(_tablet.EntryAt)),
            (3.3, "the next", () => PressWhileReading(_tablet.EntryAt)),
            (3.5, "the next, not waiting for the landing", () => PressWhileReading(_tablet.EntryAt)),
            (4.4, "take all: the rest at once", () => PressWhileReading(_tablet.TakeAllAt)),
            (6.0, "click the first note", () => Click(First(BoardShow.Note))),
            (6.05, "let go: its ties lit", LetGo),
            (7.2, "click a shape", () => Click(First(BoardShow.Silhouette))),
            (7.25, "let go: what it takes", LetGo),
            (8.4, "wheel out", () => _hands.Wheel(GetViewport().GetVisibleRect().GetCenter(), MouseButton.WheelDown, 5)),
            (9.0, "click off everything", () => { if (First(BoardShow.Note) is { } note) _hands.Mouse(_tablet.ScreenOf(note) + new Vector2(-300, 120), MouseButton.Left, true); }),
            (9.05, "let go: nothing picked", LetGo),
            (9.6, "take the first note", () => Click(First(BoardShow.Note))),
            (9.7, "drag it", () => Drag(new Vector2(60, 30))),
            (9.8, "on", () => Drag(new Vector2(90, 45))),
            (9.9, "and on", () => Drag(new Vector2(90, 45))),
            (10.0, "let go: it stays, its threads with it", LetGo),
            (10.1, "take the board itself", () => { if (First(BoardShow.Note) is { } note) _hands.Mouse(_tablet.ScreenOf(note) + new Vector2(-300, 120), MouseButton.Left, true); }),
            (10.2, "drag it up and left", () => Drag(new Vector2(-200, -100))),
            (10.3, "on: its corner out of the view, the threads still there", () => Drag(new Vector2(-200, -100))),
            (10.4, "let go", LetGo),
            (10.6, "T puts it away: nothing new, the lamp dark", () => TabletHands.Press(Key.T)),
            (12.0, "press the key: the tablet again", () => _hands.Mouse(_tablet.KeyCovers.GetCenter(), MouseButton.Left, true)),
            (12.05, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
        ];
        if (value == "bought")
            script =
            [
                (0.5, "T opens it on the board", () => TabletHands.Press(Key.T)),
                (1.0, "take all: what the walk just understood", () => PressWhileReading(_tablet.TakeAllAt)),
                (2.5, "click the solar panel", () => Click("solarPanel")),
                (2.55, "let go: its ties lit", LetGo),
                (4.0, "click off and wheel all the way out", () => { LetGo(); _hands.Wheel(GetViewport().GetVisibleRect().GetCenter(), MouseButton.WheelDown, 30); }),
                (5.0, "take the first note", () => Click(First(BoardShow.Note))),
                (5.1, "carry it", () => Drag(new Vector2(400, 200))),
                (5.2, "on, past the edge", () => Drag(new Vector2(400, 200))),
                (5.3, "let go: it stands at the edge", LetGo),
            ];
        if (value == "search")
            script =
            [
                (0.5, "T opens it on the board", () => TabletHands.Press(Key.T)),
                (1.0, "take all", () => PressWhileReading(_tablet.TakeAllAt)),
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
