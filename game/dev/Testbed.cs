using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Content;
using Stillwake.Sim;
using World = Stillwake.Sim.World;

namespace Stillwake.Game.Dev;

/// <summary>
/// A ready ship for checking things by eye (<c>-- --testbed</c>, or the title's Testbed button in a debug
/// build): every machine once, two rooms aired, an airlock out to space, every user powered, creative
/// mode on, a crate with a full stack of every item but the farm's and a charged battery, the furnace and the burner
/// generator lit, the analyzer at work, a battery charging, the Former pressing plates endlessly from the iron in its
/// inputs, the electric smelter smelting rock, the circuit assembler making circuits, a grow capsule growing seeds beside a crate of
/// the farm's items, seeds, rock and planks; and drones (0062): a full bay of four in room one, the full crate a provider, a storage crate
/// by the pod, the furnace and the Former requesting what they work, so drones carry through the closed door from the start
/// and push what the furnace and the smelter make to storage.
/// </summary>
public static class Testbed
{
    /// <summary>Every item a hero can carry: wreckage is an item only to drift and burst, never held (0041).</summary>
    public static IEnumerable<ItemSpec> Carried => Items.All.Where(i => Wreckage.HpOf(i.Id) == null);

    /// <summary>The farm's items (0060), in the second crate by the grow capsule with rock and planks: the first is full.</summary>
    public static readonly IReadOnlyList<string> Farm = ["seed", "soil", "biomass", "biofuel"];

    /// <summary>What the farm's crate holds: a full stack of each of the farm's items, and of rock and planks for soil.</summary>
    public static IEnumerable<string> FarmCrate => [.. Farm, "rock", "planks"];

    /// <summary>
    /// Room one holds the lifepod, the crates, the drone bay and the air: a tank with a canister and a generator piped to a
    /// vent in each room; room two the furnace, the Former, the electric smelter, the circuit assembler, the grow
    /// capsule and the farm's crate, the analyzer, the charging station and the burner generator; then the airlock, no
    /// vent in it. Five panels on the roof: the first two feed the lamps and the oxygen generator, the third the analyzer
    /// and the charging station, the fourth the smelter, the fifth the assembler and the grow capsule; the burner
    /// generator feeds the Former and room two's right lamp, and through it the airlock's.
    /// Sixty cells of space past the airlock, room for a jetpack to fly.
    /// </summary>
    public static readonly Layout Map = Layout.Parse(
        [
            new string(' ', 96),
            "",
            "       PPP         PPP   PPP PPP PPP",
            "  ###############################################",
            "  #.......L......#....L.................L...#.L.#",
            "  #.OOO......RR..D...............gg.........D...D",
            "  #.OOO..TTGGRR..D...FF.MM.EE.YY.gg....AACBBD...D",
            "  #.OOOKKTTGGVKK.D.V.FF.MM.EE.YY.gg.KK.AACBBD...D",
            "  ###############################################",
        ],
        [
            "",
            "",
            "",
            "",
            "",
            "",
            "         o",
            "         -----------",
        ],
        [
            ((7, 2), (10, 4)), ((7, 2), (11, 6)), ((7, 2), (19, 2)),
            ((19, 2), (22, 4)), ((25, 2), (39, 6)), ((42, 6), (40, 4)), ((40, 4), (46, 4)), ((42, 6), (24, 6)), ((25, 2), (41, 6)),
            ((29, 2), (27, 6)), ((33, 2), (30, 6)), ((33, 2), (33, 6)),
        ]);

    /// <summary>Ticks run before the items go in: a vent airs a cell a tick, and each room has some sixty.</summary>
    public const int AirTicks = 200;

    /// <summary>What the analyzer holds at the start: an item no world has analysed yet.</summary>
    public const string Analysing = "gear";

    /// <summary>What the battery in the charging station holds at the start, in thousandths of full: it charges.</summary>
    public const int Charging = 400;

    /// <summary>What the Former's first input holds at the start: iron for its endless plates, fed from its inputs.</summary>
    public const string Pressing = "iron";

    /// <summary>How much of it: a few plates' worth, so drones soon bring more (0062).</summary>
    public const int Pressed = 6;

    /// <summary>The crates by their top-left cells on the map: the full one, a provider; storage by the pod; the farm's.</summary>
    public static readonly (int X, int Y) Full = (14, 7), Storage = (7, 7), FarmAt = (36, 7);

    /// <summary>What drones bring: the furnace keeps chunks and planks, the Former iron (0062).</summary>
    public static readonly IReadOnlyList<(string Item, int Count)> FurnaceWants = [("scrapChunk", 10), ("planks", 20)], FormerWants = [("iron", 20)];

    /// <summary>What the furnace holds at the start: a chunk and a little fuel, so it burns at once and drones bring the rest.</summary>
    public static readonly IReadOnlyList<(string Item, int Count)> FurnaceHolds = [("scrapChunk", 2), ("planks", 5)];

    /// <summary>The drones docked in the bay at the start: all four, or the furnace's and the Former's top-ups keep two busy and nothing reaches storage.</summary>
    public const int Drones = 4;

    /// <summary>What the electric smelter holds at the start: rock, smelting into silicon.</summary>
    public const string Smelting = "rock";

    /// <summary>The circuits queued at the circuit assembler at the start: some minutes of work, room for seven more crafts.</summary>
    public const int Assembling = 99;

    /// <summary>The seeds in the grow capsule at the start: a crop growing, eight minutes each, the rest to plant.</summary>
    public const int Planted = 10;

    /// <summary>
    /// The testbed's world, played on <paramref name="modules"/>: run until its rooms are aired, its
    /// items put in through the save (no command makes items), loaded, and two ticks on: the furnace and
    /// the generator are lit on the first, the generator's power reaches its lamps and the Former on the
    /// second (it rests while cold), and the analyzer has nearly its whole work ahead. The Former has
    /// <see cref="Pressed"/> <see cref="Pressing"/> in its inputs and an endless entry of plates fed from them, which creative mode
    /// does not make free; the smelter a stack of rock; the assembler <see cref="Assembling"/> circuits queued; the grow capsule <see cref="Planted"/> seeds, the first growing from
    /// nothing at the speed of play. The full crate is made a provider and the requests set on the first tick (0062).
    /// </summary>
    public static World Create(IReadOnlyList<Module> modules)
    {
        var aired = World.Create(1, Map, modules);
        for (int i = 0; i < AirTicks; i++) aired.Step([]);
        var save = JsonNode.Parse(aired.Save())!;
        var machines = save["slices"]!["grid"]!["machines"]!.AsArray();
        int Id(string kind) => (int)machines.First(m => (string?)m!["kind"] == kind)!["id"]!;
        int At((int X, int Y) cell) => (int)machines.First(m => (int)m!["cell"]! == cell.Y * Map.Width + cell.X)!["id"]!;
        static JsonObject Stack(string item, int count) => new() { ["item"] = item, ["count"] = count };
        static JsonObject Battery(int thousandths) =>
            new() { ["item"] = "battery", ["count"] = 1, ["charge"] = Charge.Battery / 1000 * thousandths };
        static JsonArray Slots(string kind, IEnumerable<JsonNode?> stacks)
        {
            var slots = new JsonArray([.. stacks]);
            while (slots.Count < Machines.Get(kind).Slots) slots.Add(null);
            return slots;
        }
        var items = save["slices"]!["items"]!;
        items["creative"] = true;
        var held = new List<JsonObject>
        {
            new() { ["id"] = At(Full), ["slots"] = Slots("crate", [.. Carried.Where(i => !Farm.Contains(i.Id)).Select(i => Stack(i.Id, i.Stack)), Battery(1000)]) },
            new() { ["id"] = At(FarmAt), ["slots"] = Slots("crate", [.. FarmCrate.Select(i => Stack(i, Items.Get(i).Stack))]) },
            new() { ["id"] = Id("growCapsule"), ["slots"] = Slots("growCapsule", [Stack("seed", Planted)]) },
            new()
            {
                ["id"] = Id("furnace"),
                ["slots"] = Slots("furnace", [.. FurnaceHolds.Select(h => Stack(h.Item, h.Count))]),
            },
            new() { ["id"] = Id("analyzer"), ["slots"] = Slots("analyzer", [Stack(Analysing, 1)]) },
            new() { ["id"] = Id("charger"), ["slots"] = Slots("charger", [Battery(Charging)]) },
            new() { ["id"] = Id("burnerGenerator"), ["slots"] = Slots("burnerGenerator", [Stack("planks", Items.Get("planks").Stack)]) },
            new() { ["id"] = Id("smelter"), ["slots"] = Slots("smelter", [Stack(Smelting, Items.Get(Smelting).Stack)]) },
            new() { ["id"] = Id("former"), ["slots"] = Slots("former", [Stack(Pressing, Pressed)]) },
            new() { ["id"] = Id(Logistics.Bay), ["slots"] = Slots(Logistics.Bay, Enumerable.Range(0, Drones).Select(_ => Stack(Logistics.Drone, 1))) },
        };
        // The slice keeps its machines in id order; the lifepod's may hold something already.
        var crates = items["crates"]!.AsArray().Select(c => c!.DeepClone().AsObject()).Concat(held).OrderBy(c => (int)c["id"]!);
        items["crates"] = new JsonArray([.. crates]);
        JsonObject Queue(string kind, string recipe, int count, bool endless = false)
        {
            var entry = new JsonObject { ["id"] = 1, ["recipe"] = recipe, ["count"] = count, ["by"] = 0 };
            if (endless) (entry["fromInputs"], entry["endless"]) = (true, true);
            return new() { ["machine"] = Id(kind), ["next"] = 2, ["progress"] = 0, ["entries"] = new JsonArray(entry) };
        }
        // In machine id order, as the slice keeps them.
        save["slices"]!["crafting"]!["queues"] = new JsonArray([.. new[] { Queue("former", "plate", 1, endless: true), Queue("assembler", "circuit", Assembling) }
            .OrderBy(q => (int)q["machine"]!)]);
        var world = World.Load(save.ToJsonString(), modules);
        static Sim.Logistics.SetRequests Wants(int machine, IReadOnlyList<(string Item, int Count)> wants) =>
            new(machine, [.. wants.Select(w => new Sim.Logistics.Request(w.Item, w.Count))]);
        world.Step([new Sim.Logistics.SetRole(At(Full), Sim.Logistics.Role.Provider), Wants(Id("furnace"), FurnaceWants), Wants(Id("former"), FormerWants)]);
        world.Step([]);
        return world;
    }
}
