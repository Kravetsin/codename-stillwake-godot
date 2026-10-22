using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --floor</c>: stacks on the floor of a dark, aired room with no lamps, for recordings. A stack falls from
/// above a grow capsule and another is thrown over it, both through it onto the floor; one already lies on the
/// floor, and one falls onto a step.
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
    ]);

    /// <summary>The world aired, with the stacks laid in its save: item, column and row of their bottom middle in thousandths, speed.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        (string Item, int X, int Y, int Vx, int Vy)[] stacks =
        [
            ("iron", 6500, 2000, 0, 0),
            ("planks", 4500, 5000, 200, -150),
            ("scrap", 9500, 6000, 0, 0),
            ("wire", 14500, 2000, 0, 0),
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
                ["count"] = 3,
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
