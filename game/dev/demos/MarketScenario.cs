using System.Text.Json.Nodes;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// A room with a trade station, credits and the goods the mafia's first two levels want, with scrap beside them (2026-11-08-mafia-reputation): sell
/// them and watch the market's bar and prices.
/// </summary>
[Dev("--market", "a trade station, credits and the first two levels' goods, to raise the mafia's reputation (MarketScenario)")]
public sealed class MarketScenario : DevScenario
{
    public override Layout? Map { get; } = Layout.Parse(
        """
        ##############
        #.........OOO#
        #...$$....OOO#
        #...$$....OOO#
        ##############
        """).Doubled().InWorld() with
    { Known = Content.Research.StartKnown };

    /// <summary>The credits and the hero's goods, through the save.</summary>
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["knowledge"]!["credits"] = 2000;
        (string Item, int Count)[] goods =
            [("scrapChunk", 40), ("iron", 20), ("planks", 20), ("plate", 30), ("gear", 20), ("wire", 30), ("scrap", 200), ("hide", 20)];
        var slots = new JsonArray();
        for (int i = 0; i < Content.Inventory.Total; i++) slots.Add(null);
        for (int i = 0; i < goods.Length; i++) slots[i] = new JsonObject { ["item"] = goods[i].Item, ["count"] = goods[i].Count };
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), seed.Modules());
    }
}
