using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --die</c>: a quick death to see the body it leaves (0061). The hero comes in out of the pod's reach in the
/// wreck's torn room, open to space, its suit all but empty, carrying resources and its gear; it passes out two
/// breaths later and wakes at the pod with its gear, its body lying where it fell. With <c>--world --edge=N</c>
/// it dies out past the field instead, far enough that the marker points at the body.
/// </summary>
public static class DieDemo
{
    /// <summary>Where the hero comes in: fifteen cells left of the pod's hatch, the wreck's torn room.</summary>
    public static int StartOf(Registry m) => m.Crew.Spawn - 15;

    /// <summary>The world with peer 0's hero aboard on an almost empty suit and its inventory filled, creative mode off.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        var items = root["slices"]!["items"]!;
        items["creative"] = false;
        (string Item, int Count)?[] own = [("fireAxe", 1), ("drill", 1), ("scrap", 120), ("iron", 40), ("canister", 2), ("planks", 12), ("circuit", 3)];
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
        {
            int worn = i - Spec.Inventory.Slots;
            var stack = i < own.Length ? own[i]
                : worn >= 0 && Spec.Inventory.Equipment[worn] == "back" ? (Spec.Jetpack.Item, 1)
                : worn >= 0 && Spec.Inventory.Equipment[worn] == "belt" && (worn == 0 || Spec.Inventory.Equipment[worn - 1] != "belt") ? ("battery", 1)
                : ((string, int)?)null;
            slots.Add(stack is var (item, count) ? new JsonObject { ["item"] = item, ["count"] = count } : null);
        }
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        var spawn = (int)root["slices"]!["crew"]!["spawn"]!;
        root["slices"]!["crew"]!["heroes"] = new JsonArray(new JsonObject
        {
            ["id"] = 0,
            ["cell"] = spawn,
            ["suit"] = 2,
            ["out"] = 0,
            ["exerting"] = false,
            ["thrusting"] = false,
            ["thrustTicks"] = 0,
        });
        return World.Load(root.ToJsonString(), modules());
    }
}
