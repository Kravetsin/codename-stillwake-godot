using System.Text.Json.Nodes;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's wreck with its first meteor seen coming at once, not in minutes: flying in and striking.
/// <c>--meteors=N</c> gives it power N (the shower's 2 to 7; 1 shows a held hit on scrap), a blast as wide as that power makes.
/// </summary>
[Dev("--meteors", "the wreck's first meteor seen coming at once, of power N with =N (MeteorScenario)", Shape = DevShape.Either)]
public sealed class MeteorScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    /// <summary>The next meteor drawn on the first tick by the sim's own rules; its power then set as asked, through the save.</summary>
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["hazards"]!["nextIn"] = 1;
        world = World.Load(root.ToJsonString(), seed.Modules());
        if (!int.TryParse(Args.Given("--meteors"), out int power)) return world;
        world.Step([]);
        root = JsonNode.Parse(world.Save())!;
        root["slices"]!["hazards"]!["incoming"]!["power"] = power;
        return World.Load(root.ToJsonString(), seed.Modules());
    }
}
