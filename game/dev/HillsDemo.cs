using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --hills</c>: a long aired, lit room with ground of one-cell steps to walk (0106). From the left: a hill up three
/// steps, its plateau and down three; a valley two cells wide and one deep; a ladder at the foot of a stair five steps up to
/// a high plateau with a hole one cell wide right at the stair's top; down five; a bump; a block two cells high, which a walk
/// does not step; past it a stair of blocks with air under its steps, up to a floor. Stacks lie on the steps, dropped there as
/// the world begins. Lamps under the ceiling and low on the back wall, each pair fed by a solar panel on the roof. With
/// <c>--autopilot</c> the hero walks it (<see cref="Features.Crew.HeroAutopilot.HillsRun"/>).
/// </summary>
public static class HillsDemo
{
    private const int Width = 64, Height = 22, Top = 2, Ground = 20;

    /// <summary>The row of the high plateau's top, where the hole is: the autopilot knows the hero is in it below that.</summary>
    public const int HighPlateau = Ground - 5;

    /// <summary>The columns of the lamps, each under the middle of its panel.</summary>
    private static readonly int[] Lamps = [5, 16, 27, 38, 49, 59];

    /// <summary>The row of the lamps low on the back wall, a cable from each one under the ceiling.</summary>
    private const int LowLamps = Ground - 8;

    /// <summary>The cells above which a stack is dropped as the world begins: each over a step.</summary>
    private static readonly (int X, int Y)[] Drops = [(7, 17), (14, 16), (20, 18), (27, 14), (39, 15), (45, 17)];

    /// <summary>
    /// A character a cell, for its steps are a cell (0106), its machines drawn at their whole footprints: a panel six by two on the
    /// roof, a lamp two by two, the tank four by four up the left wall beside its vent.
    /// </summary>
    public static readonly Layout Map = Layout.Parse(Rows(), Under(),
        [.. Lamps.SelectMany(x => new[] { ((x, 0), (x, Top + 1)), ((x, Top + 1), (x, LowLamps)) })]);

    /// <summary>The pipe from the tank's port, its left column's third cell, along under it and up under the vent beside it.</summary>
    private static string[] Under() =>
        [.. Enumerable.Range(0, Height).Select(y => y == TankTop + 2 ? "  -----" : y == TankTop || y == TankTop + 1 ? "      -" : "")];

    /// <summary>The tank's top row, hung on the left wall under the first lamp: machines need no floor.</summary>
    private const int TankTop = Top + 4;

    private static string[] Rows()
    {
        var map = new char[Height, Width];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                map[y, x] = y < Top ? ' ' : x == 0 || x == Width - 1 || y == Top || y >= Ground ? '#' : '.';
        void Set(int x, int y, char c) => map[y, x] = c;
        void Block(int x0, int y0, int w, int h, char c)
        {
            for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++) Set(x, y, c);
        }
        // Solid from a cell's row down to the ground.
        void Fill(int x, int top)
        {
            for (int y = top; y < Ground; y++) Set(x, y, '#');
        }
        // Up n steps from the ground at x0, a cell a column, a plateau that long at the top, then down n steps.
        void Hill(int x0, int n, int plateau)
        {
            for (int i = 0; i < n; i++) Fill(x0 + i, Ground - 1 - i);
            for (int x = x0 + n; x < x0 + n + plateau; x++) Fill(x, Ground - n);
            for (int i = 0; i < n; i++) Fill(x0 + n + plateau + i, Ground - n + i);
        }
        Hill(6, 3, 4);
        // A valley: two cells of the ground's top row taken away, a step down and a step up.
        Set(19, Ground, '.');
        Set(20, Ground, '.');
        // A ladder at the foot of a stair up five to a high plateau, a floor to step off the ladder onto.
        for (int y = Ground - 6; y < Ground; y++) Set(24, y, 'H');
        Set(22, Ground - 6, '=');
        Set(23, Ground - 6, '=');
        Hill(25, 5, 7);
        // A hole one cell wide at the stair's top, three deep.
        for (int y = HighPlateau; y < HighPlateau + 3; y++) Set(30, y, '.');
        // A bump a cell high; a block two high, which a walk does not step up.
        Fill(45, Ground - 1);
        Fill(46, Ground - 1);
        Block(50, Ground - 2, 2, 2, '#');
        // Past it, a stair of blocks as a player builds one, air under every step, up to a floor by the room's wall.
        for (int i = 0; i < 6; i++) Set(56 + i, Ground - 1 - i, '#');
        Set(62, Ground - 6, '=');
        foreach (int x in Lamps)
        {
            Block(x, Top + 1, 2, 2, 'L');
            Block(x, LowLamps, 2, 2, 'L');
            Block(x - 2, 0, 6, 2, 'P');
        }
        // A tank and its vent by the start: the room leaks as any does, and the tank, filled as the world begins, makes it up for an hour.
        Block(2, TankTop, 4, 4, 'T');
        Block(6, TankTop, 2, 2, 'V');
        Set(1, Ground - 1, 'S');
        return [.. Enumerable.Range(0, Height).Select(y => new string([.. Enumerable.Range(0, Width).Select(x => map[y, x])]))];
    }

    /// <summary>
    /// The world with the room's air in every cell of it, and a stack dropped over each step of <see cref="Drops"/>,
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
