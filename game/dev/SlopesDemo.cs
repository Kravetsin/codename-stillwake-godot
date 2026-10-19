using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --slopes</c>: walls cut to shapes, built and lit (0069, part 2), in an aired room with lamps (<c>--lit</c> lights
/// them). On the left a hull with bevelled corners being built in creative mode, part of it up, a wall half done, the rest
/// planned; a hill under a lamp; on the right a strut of ◤ with a lamp under it, and a corridor three cells high, its far
/// top corner cut to ◥, where the hero
/// walks in under it with <c>--autopilot</c> from beside the hill (<see cref="Features.Crew.HeroAutopilot.SlopesRun"/>). A second
/// in, the plan comes out with the scrap wall's card, R turns it to ◢ and a line of them is dragged out, held.
/// </summary>
public partial class SlopesDemo(Hands hands) : Node
{
    private const int Width = 34, Height = 16;

    public static readonly Layout Map = Layout.Parse(Rows());

    private static string[] Rows()
    {
        var map = new char[Height, Width];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                map[y, x] = x == 0 || x == Width - 1 || y == 0 || y == Height - 1 ? '#' : '.';
        void Set(int x, int y, char c) => map[y, x] = c;
        // The room's top corners bevelled.
        Set(1, 1, 'r');
        Set(Width - 2, 1, '7');
        // A hill under a lamp.
        Set(11, 8, 'L');
        Set(13, 14, '/');
        Set(14, 13, '/');
        foreach (int x in new[] { 14, 15, 16, 17 }) Set(x, 14, '#');
        Set(15, 13, '#');
        Set(16, 13, '#');
        Set(17, 13, '\\');
        Set(18, 14, '\\');
        // A corridor three cells high under a roof, its lintel and far top corner cut, a lamp in it.
        for (int x = 22; x < Width - 1; x++) Set(x, 11, '#');
        Set(22, 11, '7');
        Set(Width - 2, 12, '7');
        Set(27, 12, 'L');
        // Over the roof, a strut of ◤ down a diagonal with a lamp under it: its shadow above it runs along the diagonal.
        for (int i = 0; i < 5; i++) Set(30 - i, 3 + i, 'r');
        Set(30, 7, 'L');
        Set(11, 14, 'S');
        return [.. Enumerable.Range(0, Height).Select(y => new string([.. Enumerable.Range(0, Width).Select(x => map[y, x])]))];
    }

    /// <summary>The bevelled hull's cells in the order planned, each with its shape, and how many of them are built at the start.</summary>
    private static readonly (int X, int Y, int Shape)[] Hull =
    [
        (2, 10, Shape.None), (2, 9, Shape.None), (2, 8, Shape.None), (2, 7, Shape.BottomRight), (3, 6, Shape.BottomRight),
        (4, 5, Shape.None), (5, 5, Shape.None), (6, 5, Shape.None), (7, 5, Shape.None),
        (8, 6, Shape.BottomLeft), (9, 7, Shape.BottomLeft), (9, 8, Shape.None), (9, 9, Shape.None), (9, 10, Shape.None),
    ];
    private const int Built = 7;

    /// <summary>The world aired, in creative mode, the hull planned, the first of it built and the next wall half done.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var rooms = new Registry();
        World.Create(1, Map, rooms.All);
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["atmosphere"]!["aired"] = new JsonArray([.. Enumerable.Range(0, Width * Height)
            .Where(c => rooms.Rooms.RoomAt(c) >= 0).Select(c => (JsonNode)c)]);
        var seeded = World.Load(root.ToJsonString(), modules());
        seeded.Step([new SetCreative(true), .. Hull.Select(h => (Command)new PlacePlan(h.Y * Width + h.X, Placement.Wall() with { Shape = h.Shape }))]);
        int ticks = Content.Materials.Get("scrap").BuildTicks;
        for (int plan = 1; plan <= Built + 1; plan++)
            for (int i = 0; i < (plan <= Built ? ticks : ticks / 2); i++) seeded.Step([new Work(plan)]);
        return seeded;
    }

    private const double OpenAt = 1.0, DragFor = 0.8;
    /// <summary>The line dragged out, in cells: under the hull, left to right.</summary>
    private static readonly (double X, double Y) From = (3.5, 12.5), To = (8.5, 12.5);
    private double _t;
    private bool _open, _pressed;

    public override void _Process(double delta)
    {
        _t += delta;
        if (_t < OpenAt) return;
        if (!_open)
        {
            _open = true;
            hands.Plan.Toggle();
            hands.Plan.Pick(Tools.All.First(t => t.Id == "wallScrap"));
            hands.Plan.Turn(1);
            hands.Mouse.Script = () => Along(Math.Clamp((_t - OpenAt) / DragFor, 0, 1));
        }
        var at = Along(Math.Clamp((_t - OpenAt) / DragFor, 0, 1));
        hands.Mouse.Buttons.OnPointer(!_pressed, Buttons.Left, at);
        _pressed = true;
    }

    private static (double X, double Y) Along(double t) => (From.X + (To.X - From.X) * t, From.Y + (To.Y - From.Y) * t);
}
