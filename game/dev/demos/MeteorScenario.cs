using System.Text.Json.Nodes;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>The new game's wreck with its first meteor due at once, not in minutes: seen coming, striking, its hole planned.</summary>
[Dev("--meteors", "the wreck's first meteor due at once (MeteorScenario)")]
public sealed class MeteorScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    /// <summary>The next meteor seen coming on the first tick, through the save.</summary>
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["hazards"]!["nextIn"] = 1;
        return World.Load(root.ToJsonString(), seed.Modules());
    }
}
