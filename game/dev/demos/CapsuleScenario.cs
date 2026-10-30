using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's wreck with its trade station's capsule just gone (2026-10-30-trade-capsule); <c>--capsule=back</c> just about home,
/// a few seconds' worth of ticks left to its landing.
/// </summary>
[Dev("--capsule", "the wreck with its trade station's capsule just gone; =back about to come home (CapsuleScenario)")]
public sealed class CapsuleScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    /// <summary>The flight, through the save.</summary>
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        int station = seed.M.Grid.MachinesOf("tradeStation").First().Id;
        int flight = Content.Market.CapsuleFlight;
        root["slices"]!["trade"]!["flights"] = new JsonArray(new JsonObject
        {
            ["machine"] = station,
            ["left"] = Args.Given("--capsule") == "back" ? CapsulePathSeen : flight,
            ["credits"] = 500,
        });
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    private const int CapsulePathSeen = Features.Research.CapsulePath.Seen;
}
