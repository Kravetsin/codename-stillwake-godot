using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// A salvage drone at work (<c>--salvage</c>, salvage-drones): a room aired by its lifepod, a drone bay with a charged salvage
/// drone docked and canisters in its fuel slot, two empty crates, a lamp, a solar panel on the roof feeding the bay and the
/// lamp, and a shut door in the right wall to space, where scrap, hull plates and a cargo crate drift slowly in the bay's zone.
/// At the bay's first look the drone takes a canister and flies out for the nearest piece. Drawn a character a cell.
/// </summary>
public static class SalvageDemo
{
    /// <summary>The room, the panel over it, and space to the right: the bay's zone reaches column 43.</summary>
    public static readonly Layout Map = Layout.Parse(
        [
            "        PPPPPP",
            "        PPPPPP",
            "##########################",
            "#.......................##",
            "#.......................##",
            "#OOOOOO.......LL........DD",
            "#OOOOOO.......LL........DD",
            "#OOOOOO.RRRR............DD",
            "#OOOOOO.RRRR............DD",
            "#OOOOOO.RRRR.KKKK.KKKK..DD",
            "#OOOOOO.RRRR.KKKK.KKKK..DD",
            "##########################" + new string(' ', 34),
            "",
            "",
            "",
            "",
            "",
            "",
        ],
        ["", "", "", "", "", "", "", "", "", "", "     --"],
        [((8, 0), (8, 7)), ((10, 0), (14, 5))]);

    /// <summary>Ticks run before the items go in, the lifepod airing the room.</summary>
    public const int AirTicks = 1200;

    /// <summary>Canisters in the bay's fuel slot at the start.</summary>
    public const int Canisters = 5;

    /// <summary>What drifts beside the ship, by cell, and its speed leftward in thousandths of a cell a tick: slow, so it stays in the zone.</summary>
    private static readonly (string Item, int X, int Y, int Speed)[] Drifting =
    [
        ("scrapChunk", 31, 6, 4),
        ("hullPlate", 36, 3, 3),
        ("scrapChunk", 40, 9, 5),
        ("cargoCrate", 34, 14, 3),
        ("scrapChunk", 42, 16, 4),
    ];

    /// <summary>The world of <see cref="Map"/>, aired, its bay holding the drone and the canisters, the pieces out, through the save.</summary>
    public static World Create(IReadOnlyList<Module> modules)
    {
        var aired = World.Create(1, Map, modules);
        var air = modules.OfType<AtmosphereModule>().First();
        int inside = 4 * Map.Width + 12;
        for (int i = 0; i < AirTicks && !air.AiredAt(inside); i++) aired.Step([]);
        var save = JsonNode.Parse(aired.Save())!;
        var machines = save["slices"]!["grid"]!["machines"]!.AsArray();
        int bay = (int)machines.First(m => (string?)m!["kind"] == Spec.Logistics.Bay)!["id"]!;
        var spec = Spec.Machines.Get(Spec.Logistics.Bay);
        var slots = new JsonArray();
        for (int i = 0; i < spec.Slots; i++)
            slots.Add(i == 0 ? new JsonObject { ["item"] = Spec.Logistics.SalvageDrone, ["count"] = 1, ["charge"] = Spec.Items.Get(Spec.Logistics.SalvageDrone).Capacity }
                : i == spec.SlotTakes!.Value.Slot ? new JsonObject { ["item"] = Spec.Crew.Canister, ["count"] = Canisters } : null);
        var items = save["slices"]!["items"]!;
        var crates = items["crates"]!.AsArray().Select(c => c!.DeepClone().AsObject()).Where(c => (int)c["id"]! != bay)
            .Append(new JsonObject { ["id"] = bay, ["slots"] = slots }).OrderBy(c => (int)c["id"]!);
        items["crates"] = new JsonArray([.. crates]);
        var debris = save["slices"]!["debris"]!;
        int next = (int?)debris["nextId"] ?? 1;
        if (debris["pieces"] == null) debris["pieces"] = new JsonArray();
        var pieces = debris["pieces"]!.AsArray();
        foreach (var (item, x, y, speed) in Drifting)
            pieces.Add(new JsonObject
            {
                ["id"] = next++,
                ["zone"] = Spec.Debris.Ship.Id,
                ["item"] = item,
                ["x"] = x * 1000 + 500,
                ["y"] = y * 1000 + 500,
                ["speed"] = speed,
                ["start"] = x * 1000 + 500,
                ["end"] = 0,
            });
        debris["nextId"] = next;
        return World.Load(save.ToJsonString(), modules);
    }
}
