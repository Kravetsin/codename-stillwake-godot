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
/// <c>-- --board</c>: the board on the tablet, a world's counts seeded through its save and nothing obtained during the run (docs/plans/board-plan.md,
/// B-5 to B-7). Bare it is blank; <c>=first</c>, <c>=near</c>, <c>=burst</c>, <c>=circuit</c> and <c>=late</c> are stills along the way. Script: a find
/// that lights the key (with something seeded), T, a click on the first note, a click on a shape, the wheel out, a click off everything, the first note dragged
/// away, T again, a press on the key; each step is printed with its time.
/// </summary>
public partial class BoardDemo(Func<Registry> m) : Node
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
        "late" => [.. InSpace.Select(i => (i, Plenty))],
        _ => [],
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

    /// <summary>Something new found, for the key to light for: what the seed had not got to.</summary>
    private void Find()
    {
        var knowledge = m().Knowledge;
        if (knowledge.Obtained.Count > 0 && InSpace.FirstOrDefault(i => knowledge.ObtainedOf(i) == 0) is { } item) knowledge.Obtain(item, 1);
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
            (1.5, "T opens it on the board, a loop round each new note", () => TabletHands.Press(Key.T)),
            (3.0, "click the first note", () => Click(First(BoardShow.Note))),
            (3.05, "let go: its ties lit", LetGo),
            (4.2, "click a shape", () => Click(First(BoardShow.Silhouette))),
            (4.25, "let go: what it takes", LetGo),
            (5.4, "wheel out", () => _hands.Wheel(GetViewport().GetVisibleRect().GetCenter(), MouseButton.WheelDown, 5)),
            (6.0, "click off everything", () => { if (First(BoardShow.Note) is { } note) _hands.Mouse(_tablet.ScreenOf(note) + new Vector2(-300, 120), MouseButton.Left, true); }),
            (6.05, "let go: nothing picked", LetGo),
            (6.6, "take the first note", () => Click(First(BoardShow.Note))),
            (6.7, "drag it", () => Drag(new Vector2(60, 30))),
            (6.8, "on", () => Drag(new Vector2(90, 45))),
            (6.9, "and on", () => Drag(new Vector2(90, 45))),
            (7.0, "let go: it stays, its threads with it", LetGo),
            (7.6, "T puts it away: nothing new, the lamp dark", () => TabletHands.Press(Key.T)),
            (9.0, "press the key: the tablet again", () => _hands.Mouse(_tablet.KeyCovers.GetCenter(), MouseButton.Left, true)),
            (9.05, "let go", () => _hands.Mouse(_hands.Last, MouseButton.Left, false)),
        ];
        while (_step < script.Length && _t >= script[_step].At)
        {
            GD.Print($"{script[_step].At}s: {script[_step].What}");
            script[_step++].Do();
        }
    }
}
