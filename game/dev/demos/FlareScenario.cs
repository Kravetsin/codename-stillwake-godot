using System.Text.Json.Nodes;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's wreck with a magnetic flare's warning begun at once, not in minutes (2026-10-30-magnetic-flare): the aurora
/// rising, lamps faltering and cables glowing, then the flash and machines broken.
/// </summary>
[Dev("--flare", "the wreck with a magnetic flare's warning begun at once (FlareScenario)")]
public sealed class FlareScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    /// <summary>The flare set to strike a whole warning on, through the save, as the dev panel's button sets it.</summary>
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["hazards"]!["flareIn"] = Content.MagneticFlares.StartingSpace.Warning;
        return World.Load(root.ToJsonString(), seed.Modules());
    }
}
