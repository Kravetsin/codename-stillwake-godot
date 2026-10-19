using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --hills</c>: a long aired, lit room with ground of slopes to walk (0069). From the left: a hill up three cells,
/// its plateau and down three; a valley; a ladder at the foot of a ramp five cells up to a high plateau with a hole one
/// cell wide right at the ramp's top; down five; a peak; a lone ◢ whose closed side is a cliff; a lone ◣ whose closed
/// side is a wall; ceiling slopes in the room's top corners and on a block hanging from its ceiling. Stacks lie on the
/// slopes, dropped there as the world begins. Lamps under the ceiling and low on the back wall, each pair fed by a solar panel on the roof. With
/// <c>--autopilot</c> the hero walks it (<see cref="Features.Crew.HeroAutopilot.HillsRun"/>).
/// </summary>
public static class HillsDemo
{
    private const int Width = 64, Height = 21, Top = 1, Ground = 19;

    /// <summary>The row of the high plateau's top, where the hole is: the autopilot knows the hero is in it below that.</summary>
    public const int HighPlateau = Ground - 5;

    /// <summary>The columns of the lamps, each under the middle of its panel.</summary>
    private static readonly int[] Lamps = [5, 16, 27, 38, 49, 59];

    /// <summary>The row of the lamps low on the back wall, a cable from each one under the ceiling.</summary>
    private const int LowLamps = Ground - 8;

    /// <summary>The cells above which a stack is dropped as the world begins: each over a slope.</summary>
    private static readonly (int X, int Y)[] Drops = [(7, 16), (14, 15), (20, 17), (27, 13), (39, 14), (45, 16)];

    public static readonly Layout Map = Layout.Parse(Rows(), Under(),
        [.. Lamps.SelectMany(x => new[] { ((x, 0), (x, Top + 1)), ((x, Top + 1), (x, LowLamps)) })]);

    /// <summary>The pipe from the tank's port to the vent beside it, under them.</summary>
    private static string[] Under() => [.. Enumerable.Range(0, Height).Select(y => y == Ground - 1 ? "   --" : "")];

    private static string[] Rows()
    {
        var map = new char[Height, Width];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                map[y, x] = y < Top ? ' ' : x == 0 || x == Width - 1 || y == Top || y >= Ground ? '#' : '.';
        void Set(int x, int y, char c) => map[y, x] = c;
        // Solid from a cell's row down to the ground.
        void Fill(int x, int top)
        {
            for (int y = top; y < Ground; y++) Set(x, y, '#');
        }
        // Up n cells from the ground at x0, a plateau that long at the top, then down n cells.
        void Hill(int x0, int n, int plateau)
        {
            for (int i = 0; i < n; i++)
            {
                Set(x0 + i, Ground - 1 - i, '/');
                Fill(x0 + i, Ground - i);
            }
            for (int x = x0 + n; x < x0 + n + plateau; x++) Fill(x, Ground - n);
            for (int i = 0; i < n; i++)
            {
                int x = x0 + n + plateau + i;
                Set(x, Ground - n + i, '\\');
                Fill(x, Ground - n + i + 1);
            }
        }
        Hill(6, 3, 4);
        // A valley: down into the ground's top row and up again.
        Set(19, Ground, '\\');
        Set(20, Ground, '/');
        // A ladder at the foot of a ramp up five to a high plateau, a floor to step off the ladder onto.
        for (int y = Ground - 6; y < Ground; y++) Set(24, y, 'H');
        Set(22, Ground - 6, '=');
        Set(23, Ground - 6, '=');
        Hill(25, 5, 7);
        // A hole one cell wide at the ramp's top, three deep.
        for (int y = HighPlateau; y < HighPlateau + 3; y++) Set(30, y, '.');
        // A peak; a lone ◢, its closed side a cliff; a lone ◣, its closed side a wall.
        Set(45, Ground - 1, '/');
        Set(46, Ground - 1, '\\');
        Set(50, Ground - 1, '/');
        Set(54, Ground - 1, '\\');
        // Ceiling slopes: the room's top corners, and a block hanging from the ceiling, its lower corners cut.
        Set(1, Top + 1, 'r');
        Set(Width - 2, Top + 1, '7');
        for (int x = 53; x <= 57; x++) Set(x, Top + 1, '#');
        for (int x = 54; x <= 56; x++) Set(x, Top + 2, '#');
        Set(53, Top + 2, '7');
        Set(57, Top + 2, 'r');
        foreach (int x in Lamps)
        {
            Set(x, Top + 1, 'L');
            Set(x, LowLamps, 'L');
            for (int dx = -1; dx <= 1; dx++) Set(x + dx, 0, 'P');
        }
        // A tank and its vent by the start: the room leaks as any does, and the tank, filled as the world begins, makes it up for an hour.
        Set(3, Ground - 1, 'V');
        foreach (var (dx, dy) in (ReadOnlySpan<(int, int)>)[(0, 0), (1, 0), (0, 1), (1, 1)]) Set(4 + dx, Ground - 2 + dy, 'T');
        Set(1, Ground - 1, 'S');
        return [.. Enumerable.Range(0, Height).Select(y => new string([.. Enumerable.Range(0, Width).Select(x => map[y, x])]))];
    }

    /// <summary>
    /// The world with the room's air in every cell of it, and a stack dropped over each slope of <see cref="Drops"/>,
    /// left to settle on it: nothing on the map airs a room this big quickly.
    /// </summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        // A peer no one plays carries what is dropped.
        const int Dropper = 99;
        var rooms = new Registry();
        World.Create(1, Map, rooms.All);
        var root = JsonNode.Parse(world.Save())!;
        var atmosphere = root["slices"]!["atmosphere"]!;
        atmosphere["aired"] = new JsonArray([.. Enumerable.Range(0, Width * Height)
            .Where(c => rooms.Rooms.RoomAt(c) >= 0).Select(c => (JsonNode)c)]);
        int tank = rooms.Grid.MachinesOf("tank").Single().Id;
        atmosphere["tanks"] = new JsonArray(new JsonObject { ["tank"] = tank, ["oxygen"] = Spec.Air.TankCapacity });
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i < Drops.Length ? new JsonObject { ["item"] = i % 2 == 0 ? "scrap" : "planks", ["count"] = 5 } : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = Dropper, ["slots"] = slots });
        var seeded = World.Load(root.ToJsonString(), modules());
        seeded.Step([.. Drops.Select((at, i) => (Command)new Drop(new Place(i), 5, at.Y * Width + at.X, 0) { From = Dropper })]);
        for (int i = 0; i < 60; i++) seeded.Step([]);
        return seeded;
    }
}
