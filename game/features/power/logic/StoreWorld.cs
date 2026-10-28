using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Power;

/// <summary>
/// The world of <c>-- --store</c> (0101), plain C# so it is tested: an aired room with a capacitor bank holding
/// <see cref="Holds"/>, cabled to a solar panel out in space, two lamps and an electric smelter with the arc module fitted,
/// standing empty; the hero, in creative mode, carries quartz. The panel's 5 against the lamps' 2 fills the bank by 3 a tick;
/// with quartz in it the smelter draws 8, and the bank gives the 5 the network is short.
/// </summary>
public static class StoreWorld
{
    /// <summary>What the bank holds at the start, in power-ticks.</summary>
    public const int Holds = 3800;

    /// <summary>Where the hero carries the quartz, and how much.</summary>
    public const int QuartzSlot = 20, Quartz = 50;

    /// <summary>The room, drawn a character a 16 px cell and doubled (0104).</summary>
    public static readonly Layout Map = Layout.Parse(
        [
            "                              ",
            "         PPP                  ",
            "                              ",
            "##############################",
            "#.....L.....L................#",
            "#.OOO.TT..QQ.EE..............#",
            "#.OOO.TTV.QQ.EE..............#",
            "#.OOO........................#",
            "##############################",
        ],
        ["", "", "", "", "", "      o", "      ---"],
        [((9, 1), (10, 5)), ((6, 4), (10, 5)), ((12, 4), (10, 5)), ((13, 5), (10, 5))]).Doubled();

    /// <summary>The world, its room aired, the bank holding <see cref="Holds"/>, the smelter fitted and the hero carrying quartz.</summary>
    public static World Create(IReadOnlyList<Module> modules)
    {
        var aired = World.Create(1, Map, modules);
        for (int i = 0; i < 400; i++) aired.Step([]);
        var save = JsonNode.Parse(aired.Save())!;
        var machines = save["slices"]!["grid"]!["machines"]!.AsArray();
        int Id(string kind) => (int)machines.First(m => (string?)m!["kind"] == kind)!["id"]!;
        var items = save["slices"]!["items"]!;
        items["creative"] = true;
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++) slots.Add(i == QuartzSlot ? new JsonObject { ["item"] = "quartz", ["count"] = Quartz } : null);
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        items["fitted"] = new JsonArray(new JsonObject { ["machine"] = Id("smelter"), ["module"] = Spec.Modules.Arc.Item });
        save["slices"]!["power"]!["stores"] = new JsonArray(new JsonObject { ["machine"] = Id("capacitorBank"), ["energy"] = Holds });
        return World.Load(save.ToJsonString(), modules);
    }
}
