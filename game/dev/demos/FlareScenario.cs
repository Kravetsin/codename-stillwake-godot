using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's wreck with a magnetic flare's warning begun at once, not in minutes (2026-10-30-magnetic-flare): the aurora
/// rising, lamps faltering and cables glowing, then the flash and machines broken. <c>--flare=repair</c> skips to after one:
/// its lamps and generator broken, the first lamp's repair paid and half done, as the screwdriver leaves it.
/// </summary>
[Dev("--flare", "the wreck with a magnetic flare's warning begun at once; =repair its lamps and generator broken, a lamp half repaired (FlareScenario)", Shape = DevShape.Either)]
public sealed class FlareScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    /// <summary>The flare set to strike a whole warning on, or what one broke and a repair begun, through the save.</summary>
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        if (Args.Given("--flare") != "repair")
        {
            root["slices"]!["hazards"]!["flareIn"] = Content.MagneticFlares.StartingSpace.Warning;
            return World.Load(root.ToJsonString(), seed.Modules());
        }
        var grid = seed.M.Grid;
        int[] lamps = [.. grid.MachinesOf("lamp").Select(m => m.Id)];
        int[] broken = [.. lamps.Concat(grid.MachinesOf("generator").Select(m => m.Id)).Order()];
        root["slices"]!["power"]!["broken"] = new JsonArray([.. broken.Select(id => (JsonNode)id)]);
        if (lamps.Length > 0)
        {
            int half = (Content.Machines.Get("lamp").BuildTicks * Content.RepairRules.Share / 1000 + 1) / 2;
            root["slices"]!["building"]!["repairs"] = new JsonArray(new JsonObject { ["machine"] = lamps[0], ["progress"] = half });
        }
        return World.Load(root.ToJsonString(), seed.Modules());
    }
}
