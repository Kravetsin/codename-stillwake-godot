using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Sim;
using Stillwake.Sim.Furnace;

namespace Stillwake.Game.Features.Furnace;

/// <summary>
/// For recordings (<c>-- --furnace</c>): the wreck with a furnace in room A, beside the lifepod the hero
/// is made in, holding scrap chunks and planks, played on until its room is aired and its fire lit.
/// </summary>
public static class FurnaceDemo
{
    /// <summary>The furnace's top-left cell in the wreck: in room A, between its vent and the lifepod.</summary>
    private const int X = 49, Y = 21;

    public static World Create(IReadOnlyList<Module> modules)
    {
        var rows = Registry.Wreck.Rows.ToArray();
        for (int y = Y; y < Y + 2; y++) rows[y] = rows[y][..X] + "FF" + rows[y][(X + 2)..];
        var layout = Registry.Wreck with { Rows = rows };
        var save = JsonNode.Parse(World.Create(1, layout, modules).Save())!;
        var furnace = save["slices"]!["grid"]!["machines"]!.AsArray().First(m => (string?)m!["kind"] == "furnace")!;
        // Items come into a world only as the hero catches them: here they are put in through the save.
        save["slices"]!["items"]!["crates"] = new JsonArray(new JsonObject
        {
            ["id"] = (int)furnace["id"]!,
            ["slots"] = new JsonArray(
                new JsonObject { ["item"] = "scrapChunk", ["count"] = 10 },
                new JsonObject { ["item"] = "planks", ["count"] = 50 },
                null, null, null),
        });
        var world = World.Load(save.ToJsonString(), modules);
        var burns = modules.OfType<FurnaceModule>().Single();
        for (int i = 0; i < 5000 && !burns.At((int)furnace["id"]!).Burning; i++) world.Step([]);
        return world;
    }
}
