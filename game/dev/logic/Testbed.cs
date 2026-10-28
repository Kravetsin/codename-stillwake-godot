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
/// the farm's items, seeds, rock and planks, and what a planet's caves give; and drones (0062): two in a powered bay in room one beside a supply crate of
/// twenty stacks of chunks, planks, iron and seeds, the farm's crate switched off. The furnace already holds chunks and
/// planks, the Former iron and the capsule seeds, each under half a stack, so from the start drones carry through the
/// closed door to keep them going for ten minutes and more, and carry what they make back to the crates.
/// </summary>
public static class Testbed
{
    /// <summary>
    /// Every item a hero can carry but the kit it comes aboard with (0107) and suits' helmets and bodies, free at the pod: wreckage is an item only
    /// to drift and burst, never held (0041).
    /// </summary>
    public static IEnumerable<ItemSpec> Carried => Items.All.Where(i => Wreckage.HpOf(i.Id) == null && !Inventory.Kit.Any(k => k.Item == i.Id) && i.Equip != Suit.Slot && i.Equip != Helmet.Slot);

    /// <summary>The farm's items (0060), in the second crate by the grow capsule with rock and planks: the first is full.</summary>
    public static readonly IReadOnlyList<string> Farm = ["seed", "soil", "biomass", "biofuel"];

    /// <summary>What a planet's caves and ruins give (0080, 0085, 0091), the motor made of their copper, a grazer's hide and fat, and the cape of cloth and hide, in the farm's crate too: the first is full.</summary>
    public static readonly IReadOnlyList<string> Planet = ["copperOre", "copper", "ice", "crystal", "spore", "mica", "magnetite", "motor", "alloyScrap", "arcShard", "cloth", "hide", "fat", "arcLamp", Cape.Item];

    /// <summary>What the farm's crate holds: a full stack of each of the farm's items, of rock and planks for soil, and of what a planet gives.</summary>
    public static IEnumerable<string> FarmCrate => [.. Farm, "rock", "planks", .. Planet];

    /// <summary>Whether <paramref name="item"/> goes in the farm's crate rather than the full one.</summary>
    public static bool InFarmCrate(string item) => Farm.Contains(item) || Planet.Contains(item);

    /// <summary>
    /// What the supply crate by the door holds besides its supplies, the first crate being full: the helmet lamp (0077), and an
    /// arc module for each machine and tool it fits (0086, 0098, 0100), which takes a spire's relic to make, and an empty accumulator
    /// (0099), a part of each of the computer's (<see cref="ComputerParts"/>), a launch platform's deck drive and the engine's parts
    /// (<see cref="EngineParts"/>), the nailer and its nails, the full crate having no slot left. A drill with the arc fitted lies there too (0098); a plain one is on the arc bench, to be fitted there (0099).
    /// </summary>
    public static IEnumerable<string> InSupply => [HelmetLamp.Item, .. Modules.Arc.Hosts.Select(_ => Modules.Arc.Item), "accumulator", .. ComputerParts, "deckDrive", .. EngineParts, Shots.Nailer.Item, Shots.Railgun.Item, Shots.Nail.Id];

    /// <summary>The ship computer's and the radar's parts, one each in the supply crate: the full crate has no slot left for them.</summary>
    public static readonly IReadOnlyList<string> ComputerParts = ["processor", "display", "transceiver"];

    /// <summary>The engine's parts, one each in the supply crate too.</summary>
    public static readonly IReadOnlyList<string> EngineParts = ["nozzle", "fuelPump", "igniter"];

    /// <summary>What the engine's tank holds at the start: biofuel, so it draws its power.</summary>
    public const int Fuelled = 10;

    /// <summary>What the drop pod's slot holds at the start: biofuel for two descents (the descent's <c>DescentFuel</c> each).</summary>
    public const int PodFuel = 2 * Landing.DescentFuel;

    /// <summary>A drill with <paramref name="module"/> fitted, as a save writes its stack (0098): a dev world's, the bench to come.</summary>
    public static JsonObject FittedDrill(string module) => new() { ["item"] = Weapons.Drill.Item, ["count"] = 1, ["module"] = module };

    /// <summary>
    /// Room one holds the lifepod, the crates, the drone bay and the air: a tank with a canister and a generator piped to a
    /// vent in each room, and a capacitor bank on the supply crate by the pod; room two the arc bench by its door, the
    /// furnace, the Former, the electric smelter, the circuit assembler, the grow capsule and the farm's crate, the analyzer, the charging station
    /// and the burner generator; then the airlock, no vent in it. The ship's computer hangs over room one's generator, the radar
    /// antenna stands on the roof at the bow. The drop pod stands in the airlock on a launch platform laid in its floor over space,
    /// cabled to the panel over the airlock, biofuel for two descents in its slot. Twelve panels on the roof: the first and the third feed the
    /// lamps and the oxygen generator, their spare filling the bank (0101), the second the drone bay, the one beside it the
    /// computer and the antenna, the fourth the analyzer,
    /// the fifth the smelter, the sixth the assembler and the grow capsule, the seventh and eighth the two oxygen generators in
    /// room two, and the last three, over the airlock and past the stern, the charging station: 15, what it draws with the arc
    /// fitted (0100); the burner generator feeds the Former and room
    /// two's right lamp, and through it the airlock's. The grow capsule stands a cell left of the assembler, inside the
    /// bay's zone. Three oxygen generators, piped with the tank to both vents, make more than the rooms leak and the two
    /// fires breathe, so the air holds however long a session runs; one alone ran room two dry in three minutes.
    /// Under room one, through an open hatch, the engine's bay: the engine against the stern's wall, its nozzle out into space,
    /// biofuel in its tank, cabled to the bank, and a lamp cabled to it. Sixty cells of space past the airlock, room for a jetpack to fly. Drawn a character a 16 px cell and doubled (0104).
    /// </summary>
    public static readonly Layout Map = Layout.Parse(
        [
            new string(' ', 96),
            "   NN",
            "   NN  PPP  PPPPPP PPP   PPP PPP PPP PPP PPP PPP PPP PPP",
            "  ###############################################",
            "  #.......LUUU...#....L.........J.......L...#..L#",
            "  #.OOOQQ..UUU.RRD..............gg...GG.....DZZ.D",
            "  #.OOOQQTTGG..RRD...FF.MM.EE.YYggGG.GGAACBBDZZ.D",
            "  #.OOOKKTTGGVKK.DWWVFF.MM.EE.YYggGGKK.AACBBDZZ.D",
            "  #o#########################################__##",
            "  #XXXL#",
            "  #XXX.#",
            "  ######",
        ],
        [
            "",
            "",
            "",
            "",
            "",
            "",
            "         o                          --",
            "         ----------------------------",
        ],
        [
            ((7, 2), (10, 4)), ((7, 2), (11, 6)), ((7, 2), (19, 2)),
            ((19, 2), (22, 4)), ((25, 2), (39, 6)), ((42, 6), (40, 4)), ((40, 4), (47, 4)), ((42, 6), (24, 6)), ((45, 2), (41, 6)),
            ((29, 2), (27, 6)), ((33, 2), (30, 6)), ((33, 2), (33, 6)), ((12, 2), (15, 5)), ((37, 2), (34, 6)), ((41, 2), (37, 5)),
            ((37, 2), (32, 4)), ((45, 2), (49, 2)), ((49, 2), (53, 2)), ((7, 2), (7, 5)), ((15, 2), (4, 2)), ((15, 2), (13, 4)),
            ((7, 5), (3, 9)), ((3, 9), (6, 9)), ((45, 2), (45, 8)),
        ]).Doubled();

    /// <summary>Ticks run before the items go in: a vent airs a cell a tick, and each room has some sixty.</summary>
    public const int AirTicks = 200;

    /// <summary>The arc bench by room two's door (0099) and what it holds at the start: a plain drill in its vise.</summary>
    public static readonly (string Kind, string Tool) BenchHolds = ("arcBench", Weapons.Drill.Item);

    /// <summary>What the analyzer holds at the start: an item no world has analysed yet.</summary>
    public const string Analysing = "gear";

    /// <summary>What the battery in the charging station holds at the start, in thousandths of full: it charges.</summary>
    public const int Charging = 400;

    /// <summary>What the Former's first input holds at the start: iron for its endless plates, fed from its inputs.</summary>
    public const string Pressing = "iron";

    /// <summary>How much of it: a few plates' worth, under half a stack, so drones bring a stack at once (0062).</summary>
    public const int Pressed = 6;

    /// <summary>The crates by their top-left cells on the map, its drawing doubled (0104): the full one; the supply crate by the pod; the farm's, switched off.</summary>
    public static readonly (int X, int Y) Full = (28, 14), Supply = (14, 14), FarmAt = (72, 14);

    /// <summary>What the supply crate holds at the start: what keeps the furnace, the Former and the grow capsule going (0062).</summary>
    public static readonly IReadOnlyList<(string Item, int Count)> Supplies = [.. Enumerable.Repeat(("scrapChunk", 50), 4), ("planks", 50), ("iron", 500), ("seed", 10), (Shots.Nail.Id, 100)];

    /// <summary>What the furnace holds at the start: a chunk and a little fuel, each under half a stack, so it burns at once and drones bring the rest.</summary>
    public static readonly IReadOnlyList<(string Item, int Count)> FurnaceHolds = [("scrapChunk", 2), ("planks", 5)];

    /// <summary>The drones docked in the bay at the start, charged full: two keep the room going (0062).</summary>
    public const int Drones = 2;

    /// <summary>What the electric smelter holds at the start: rock, smelting into silicon.</summary>
    public const string Smelting = "rock";

    /// <summary>The circuits queued at the circuit assembler at the start: some minutes of work, room for seven more crafts.</summary>
    public const int Assembling = 99;

    /// <summary>The seeds in the grow capsule at the start: a crop growing, eight minutes each, under half a stack, so drones top them up.</summary>
    public const int Planted = 4;

    /// <summary>
    /// The testbed's world, played on <paramref name="modules"/>: run until its rooms are aired, its
    /// items put in through the save (no command makes items), loaded, and two ticks on: the furnace and
    /// the generator are lit on the first, the generator's power reaches its lamps and the Former on the
    /// second (it rests while cold), and the analyzer has nearly its whole work ahead. The Former has
    /// <see cref="Pressed"/> <see cref="Pressing"/> in its inputs and an endless entry of plates fed from them, which creative mode
    /// does not make free; the smelter a stack of rock; the assembler <see cref="Assembling"/> circuits queued; the grow capsule <see cref="Planted"/> seeds, the first growing from
    /// nothing at the speed of play. The supply crate and the bay's two drones go in with the rest; the farm's crate is
    /// switched off on the first tick, to show the badge crossed (0062).
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
        static JsonObject Drone() => new() { ["item"] = Logistics.Drone, ["count"] = 1, ["charge"] = Charge.Drone };
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
            new() { ["id"] = At(Full), ["slots"] = Slots("crate", [.. Carried.Where(i => !InFarmCrate(i.Id) && !InSupply.Contains(i.Id)).Select(i => Stack(i.Id, i.Stack)), Battery(1000)]) },
            new() { ["id"] = At(FarmAt), ["slots"] = Slots("crate", [.. FarmCrate.Select(i => Stack(i, Items.Get(i).Stack))]) },
            new() { ["id"] = At(Supply), ["slots"] = Slots("crate", [.. Supplies.Select(s => Stack(s.Item, s.Count)), .. InSupply.Select(f => Stack(f, 1)), FittedDrill(Modules.Arc.Item)]) },
            new() { ["id"] = Id("growCapsule"), ["slots"] = Slots("growCapsule", [Stack("seed", Planted)]) },
            new()
            {
                ["id"] = Id("furnace"),
                ["slots"] = Slots("furnace", [.. FurnaceHolds.Select(h => Stack(h.Item, h.Count))]),
            },
            new() { ["id"] = Id("analyzer"), ["slots"] = Slots("analyzer", [Stack(Analysing, 1)]) },
            new() { ["id"] = Id(BenchHolds.Kind), ["slots"] = Slots(BenchHolds.Kind, [Stack(BenchHolds.Tool, 1)]) },
            new() { ["id"] = Id("charger"), ["slots"] = Slots("charger", [Battery(Charging)]) },
            new() { ["id"] = Id("engine"), ["slots"] = Slots("engine", [Stack("biofuel", Fuelled)]) },
            new() { ["id"] = Id("dropPod"), ["slots"] = Slots("dropPod", [Stack("biofuel", PodFuel)]) },
            new() { ["id"] = Id("burnerGenerator"), ["slots"] = Slots("burnerGenerator", [Stack("planks", Items.Get("planks").Stack)]) },
            new() { ["id"] = Id("smelter"), ["slots"] = Slots("smelter", [Stack(Smelting, Items.Get(Smelting).Stack)]) },
            new() { ["id"] = Id("former"), ["slots"] = Slots("former", [Stack(Pressing, Pressed)]) },
            new() { ["id"] = Id(Logistics.Bay), ["slots"] = Slots(Logistics.Bay, Enumerable.Range(0, Drones).Select(_ => Drone())) },
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
        world.Step([new Sim.Logistics.SetServed(At(FarmAt), false)]);
        world.Step([]);
        return world;
    }
}
