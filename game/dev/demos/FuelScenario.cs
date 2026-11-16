using System;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Pipes;

namespace Stillwake.Game.Dev;

/// <summary>
/// Fuel as a liquid (fuel-liquid), to play: an aired room with a fermenter fed biomass on a panel, piped to a fuel tank holding 60 l,
/// piped on to a drop pod on a powered launch platform; a biofuel generator cabled to a lamp, its fuel input left for the hero to
/// pipe. The hero is creative, carrying cans, biomass and tubes.
/// </summary>
[Dev("--fuel", "fuel as a liquid: a fermenter piped to a 60 l fuel tank piped on to a drop pod; a biofuel generator to pipe by hand")]
public sealed class FuelScenario : DevScenario
{
    public override Layout? Map => Layout.Parse(
        [
            "                              ",
            "  PPP    PPP       PPP        ",
            "                              ",
            "############################# ",
            "#..L....L....L......L.......# ",
            "#.........ZZ................# ",
            "#.uu..!!..ZZ..S.qqq.........# ",
            "#.uu..!!..ZZ....qqq.........# ",
            "##########__################# ",
            "                              ",
        ],
        [],
        [((2, 1), (2, 6)), ((2, 1), (3, 4)), ((9, 1), (10, 8)), ((9, 1), (8, 4)), ((16, 6), (13, 4)), ((19, 1), (20, 4))]).Doubled();

    /// <summary>Cells, doubled: the fermenter's output, the tank's input and output, the pod's input.</summary>
    private static readonly int[] Ports = [14 * 60 + 7, 14 * 60 + 12, 14 * 60 + 15, 14 * 60 + 20];

    /// <summary>What the hero carries besides its kit.</summary>
    private static readonly (string Item, int Count)[] Carried = [(Content.Fuel.Item, 20), ("biomass", 50), ("tube", 40)];

    /// <summary>Every room's cells aired; the hero creative with its bag filled; the fermenter's biomass; the tank's 60 l.</summary>
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        var m = seed.M;
        var roomOf = m.Rooms.RoomOfCells.ToArray();
        root["slices"]!["atmosphere"]!["aired"] = new JsonArray([.. Enumerable.Range(0, roomOf.Length).Where(c => roomOf[c] >= 0).Select(c => (JsonNode)c)]);
        var items = root["slices"]!["items"]!;
        var slots = new JsonArray([.. Enumerable.Range(0, Content.Inventory.Total).Select(i => i < Content.Inventory.Kit.Count
            ? new JsonObject { ["item"] = Content.Inventory.Kit[i].Item, ["count"] = Content.Inventory.Kit[i].Count } : (JsonNode?)null)]);
        int free = Content.Inventory.Hotbar;
        foreach (var (item, count) in Carried)
        {
            while (slots[free] != null) free++;
            slots[free] = new JsonObject { ["item"] = item, ["count"] = count };
        }
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        items["creative"] = new JsonArray(0);
        int width = m.Grid.Width;
        int fermenter = m.Grid.MachineAt(12 * width + 4)!.Id, tank = m.Grid.MachineAt(12 * width + 12)!.Id;
        items["crates"] = new JsonArray(new JsonObject { ["id"] = fermenter, ["slots"] = new JsonArray(new JsonObject { ["item"] = "biomass", ["count"] = 50 }) });
        root["slices"]!["fuel"]!["stores"] = new JsonArray(new JsonObject { ["machine"] = tank, ["amount"] = 60 * Content.Fuel.Litre });
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    /// <summary>The fermenter piped to the tank and the tank to the pod, as the session starts.</summary>
    public override void Started(Client client)
    {
        client.Send(new LayPipe(Ports[0], Ports[1]));
        client.Send(new LayPipe(Ports[2], Ports[3]));
    }
}
