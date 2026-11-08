using System.Text.Json.Nodes;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// A room with a trade station, credits and the goods the mafia's next level wants, with scrap beside them (2026-11-08-mafia-reputation), the mafia at
/// its third level: buy a parcel, a crate and the seized container, step out of the door into space and catch or break each as it drifts in along
/// the station's row (2026-11-08-mafia-deliveries); sell the goods and watch the bar and prices.
/// </summary>
[Dev("--market", "a trade station, credits and the mafia at its third level: buy, then catch the deliveries out of the door (MarketScenario)")]
public sealed class MarketScenario : DevScenario
{
    public override Layout? Map { get; } = Layout.Parse(
        """
        ##############
        #.........OOOD
        #...$$....OOOD
        #...$$....OOOD
        ##############
        """).Doubled().InWorld() with
    { Known = Content.Research.StartKnown };

    /// <summary>The credits, the mafia's level and the hero's goods, through the save.</summary>
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        var knowledge = root["slices"]!["knowledge"]!;
        knowledge["credits"] = 20_000;
        knowledge["reputation"] = new JsonObject { ["level"] = 3, ["base"] = new JsonArray(), ["baseWorth"] = 0 };
        (string Item, int Count)[] goods =
            [("circuit", 15), ("coil", 10), ("tube", 30), ("scrapChunk", 40), ("iron", 20), ("plate", 30), ("scrap", 200), ("hide", 20)];
        var slots = new JsonArray();
        for (int i = 0; i < Content.Inventory.Total; i++) slots.Add(null);
        for (int i = 0; i < goods.Length; i++) slots[i] = new JsonObject { ["item"] = goods[i].Item, ["count"] = goods[i].Count };
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), seed.Modules());
    }
}
