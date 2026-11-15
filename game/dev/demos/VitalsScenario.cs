using System;
using System.Text.Json.Nodes;
using Stillwake.Game.Features.Debris;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// The vitals to try by hand (hud-vitals): the hero out in the band by two asteroids as <c>--asteroid --belt</c> seeds it, three
/// stores on the belt and the grinder to drain them, canisters, and on the hotbar the ration, canned meat and a seed to eat (a
/// fourth meal or one too soon is refused), the heavy helmet to swap for the basic one, and a spare accumulator. Not creative:
/// air runs down in the vacuum and meals run out.
/// </summary>
[Dev("--vitals", "the hero in space with three stores on the belt, the grinder, canisters, foods, the heavy helmet and an accumulator to try the vitals (hud-vitals)")]
public sealed class VitalsScenario : DevScenario
{
    /// <summary>What the hotbar's free slots hold: the foods, the heavy helmet, a spare accumulator.</summary>
    private static readonly (int Slot, string Item, int Count)[] Hotbar =
    [
        (4, Spec.Foods.Ration, 5),
        (5, Spec.Foods.CannedMeat, 5),
        (6, Spec.Foods.Seed, 5),
        (7, Spec.Helmet.Heavy, 1),
        (8, "accumulator", 1),
    ];

    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(AsteroidDemo.Seed(world, seed.M, seed.Modules, fullBelt: true).Save())!;
        var slots = root["slices"]!["items"]!["inventories"]![0]!["slots"]!.AsArray();
        foreach (var (slot, item, count) in Hotbar)
        {
            var stack = new JsonObject { ["item"] = item, ["count"] = count };
            if (Spec.Items.Get(item).Capacity is int full) stack["charge"] = full;
            slots[slot] = stack;
        }
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    public override Func<Registry, int>? Start => FarDemo.StartOf;
}
