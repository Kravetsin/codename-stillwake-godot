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
/// B-5). Bare it is blank; <c>=first</c>, <c>=near</c>, <c>=burst</c>, <c>=circuit</c> and <c>=late</c> are stills along the way. Script: T, a click
/// on the first note, a click on a shape, the wheel out; each step is printed with its time.
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

    private void Click(string? id)
    {
        if (id == null) return;
        _hands.Mouse(_tablet.ScreenOf(id), MouseButton.Left, true);
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
            (0.8, "T opens it on the board", () => TabletHands.Press(Key.T)),
            (2.0, "click the first note", () => Click(First(BoardShow.Note))),
            (2.05, "let go: its ties lit", LetGo),
            (3.2, "click a shape", () => Click(First(BoardShow.Silhouette))),
            (3.25, "let go: what it takes", LetGo),
            (4.4, "wheel out", () => _hands.Wheel(GetViewport().GetVisibleRect().GetCenter(), MouseButton.WheelDown, 5)),
        ];
        while (_step < script.Length && _t >= script[_step].At)
        {
            GD.Print($"{script[_step].At}s: {script[_step].What}");
            script[_step++].Do();
        }
    }
}
