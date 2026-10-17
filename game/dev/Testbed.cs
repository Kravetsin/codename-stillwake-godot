using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Content;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// A ready ship for checking things by eye (<c>-- --testbed</c>, or the title's Testbed button in a debug
/// build): every machine once, two rooms aired, an airlock out to space, every user powered, creative
/// mode on, a crate with a full stack of every item, the furnace and the burner generator lit and the
/// analyzer at work.
/// </summary>
public static class Testbed
{
    /// <summary>Every item a hero can carry: wreckage is an item only to drift and burst, never held (0041).</summary>
    public static IEnumerable<ItemSpec> Carried => Items.All.Where(i => Wreckage.HpOf(i.Id) == null);

    /// <summary>
    /// Room one holds the lifepod and the air: a tank with a canister and a generator piped to a vent
    /// in each room; room two the furnace, the crate, the analyzer and the burner generator; then the
    /// airlock, no vent in it. Two panels on the roof, every user cabled to the nearer, but room two's
    /// right lamp, cabled to the generator, and the airlock's, cabled to that lamp. Sixty cells of space past
    /// the airlock, room for a jetpack to fly.
    /// </summary>
    public static readonly Layout Map = Layout.Parse(
        [
            new string(' ', 96),
            "",
            "       PPP         PPP",
            "  ###################################",
            "  #.......L......#....L.....L...#.L.#",
            "  #.OOO..........D..............D...D",
            "  #.OOO..TTGG....D...FF....AA.BBD...D",
            "  #.OOO..TTGGV...D.V.FF.KK.AA.BBD...D",
            "  ###################################",
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
            ((19, 2), (22, 4)), ((19, 2), (27, 6)), ((30, 6), (28, 4)), ((28, 4), (34, 4)),
        ]);

    /// <summary>Ticks run before the items go in: a vent airs a cell a tick, and each room has some sixty.</summary>
    public const int AirTicks = 200;

    /// <summary>What the analyzer holds at the start: an item no world has analysed yet.</summary>
    public const string Analysing = "gear";

    /// <summary>
    /// The testbed's world, played on <paramref name="modules"/>: run until its rooms are aired, its
    /// items put in through the save (no command makes items), loaded, and two ticks on: the furnace and
    /// the generator are lit on the first, the generator's power reaches its lamps on the second
    /// (it rests while cold), and the analyzer has nearly its whole work ahead.
    /// </summary>
    public static World Create(IReadOnlyList<Module> modules)
    {
        var aired = World.Create(1, Map, modules);
        for (int i = 0; i < AirTicks; i++) aired.Step([]);
        var save = JsonNode.Parse(aired.Save())!;
        var machines = save["slices"]!["grid"]!["machines"]!.AsArray();
        int Id(string kind) => (int)machines.First(m => (string?)m!["kind"] == kind)!["id"]!;
        static JsonObject Stack(string item, int count) => new() { ["item"] = item, ["count"] = count };
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
            new() { ["id"] = Id("crate"), ["slots"] = Slots("crate", Carried.Select(i => Stack(i.Id, i.Stack))) },
            new()
            {
                ["id"] = Id("furnace"),
                ["slots"] = Slots("furnace", [Stack("scrapChunk", Items.Get("scrapChunk").Stack), Stack("planks", Items.Get("planks").Stack)]),
            },
            new() { ["id"] = Id("analyzer"), ["slots"] = Slots("analyzer", [Stack(Analysing, 1)]) },
            new() { ["id"] = Id("burnerGenerator"), ["slots"] = Slots("burnerGenerator", [Stack("planks", Items.Get("planks").Stack)]) },
        };
        // The slice keeps its machines in id order; the lifepod's may hold something already.
        var crates = items["crates"]!.AsArray().Select(c => c!.DeepClone().AsObject()).Concat(held).OrderBy(c => (int)c["id"]!);
        items["crates"] = new JsonArray([.. crates]);
        var world = World.Load(save.ToJsonString(), modules);
        world.Step([]);
        world.Step([]);
        return world;
    }
}
