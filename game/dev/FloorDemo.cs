using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --floor</c>: stacks on the floor of a dark, aired room with no lamps, for recordings. A stack falls from
/// above a grow capsule and another is thrown over it, both through it onto the floor; one already lies on the
/// floor, and one falls onto a step; two tools lie on the floor, their icons filtered (2026-10-27-tool-icons).
/// </summary>
public static class FloorDemo
{
    public static readonly Layout Map = Layout.Parse(
    [
        "##################",
        "#................#",
        "#................#",
        "#.....gg.........#",
        "#.....gg.........#",
        "#.....gg...S..##.#",
        "##################",
    ]).Doubled();

    /// <summary>The world aired, with the stacks laid in its save: item, column and row of their bottom middle in thousandths of a cell (the map doubled, 0104), speed.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        (string Item, int X, int Y, int Vx, int Vy)[] stacks =
        [
            ("iron", 13000, 4000, 0, 0),
            ("planks", 9000, 10000, 200, -150),
            ("scrap", 19000, 12000, 0, 0),
            ("wire", 29000, 4000, 0, 0),
            ("drill", 5000, 12000, 0, 0),
            (Content.Screwdrivers.Basic.Item, 24000, 12000, 0, 0),
        ];
        var root = JsonNode.Parse(world.Save())!;
        var probe = new Registry();
        World.Create(1, Map, probe.All);
        root["slices"]!["atmosphere"]!["aired"] = new JsonArray([.. Enumerable.Range(0, Map.Width * Map.Height)
            .Where(c => probe.Rooms.RoomAt(c) >= 0).Select(c => (JsonNode)c)]);
        var floor = new JsonArray();
        for (int i = 0; i < stacks.Length; i++)
        {
            var (item, x, y, vx, vy) = stacks[i];
            floor.Add(new JsonObject
            {
                ["id"] = i + 1,
                ["item"] = item,
                ["count"] = HeldLook.Of(item) != null ? 1 : 3,
                ["cell"] = (y - 1) / 1000 * Map.Width + x / 1000,
                ["x"] = x,
                ["y"] = y,
                ["vx"] = vx,
                ["vy"] = vy,
                ["by"] = 99,
                ["at"] = 0,
            });
        }
        var items = root["slices"]!["items"]!;
        items["floor"] = floor;
        items["nextFloorId"] = stacks.Length + 1;
        return World.Load(root.ToJsonString(), modules());
    }
}
