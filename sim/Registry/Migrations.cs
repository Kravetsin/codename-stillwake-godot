using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Stillwake.Sim;

/// <summary>
/// Old saves made loadable (0031): each step turns a save of one format into the next, the JSON of
/// <see cref="World.Save"/> in and out, so a save from any format since <see cref="Oldest"/> loads in
/// this build. Every bump of <see cref="World.SaveFormat"/> ships its step. A module new to a format
/// gets its slice from the step, by hand — empty where a running world allows it, else made from the
/// other slices — never a silent default, so a forgotten step fails loudly in <see cref="World.Load"/>.
/// Only saves read from disk are upgraded: a guest's welcome and a resync come from this very build.
/// Format 37 replaced research by analysis with trade and a tech tree (2026-10-30-trade-and-tech): no save before it
/// migrates, the tree having no way to say what an older world had studied, and nothing released.
/// </summary>
public static class Migrations
{
    /// <summary>The oldest format a save may have and still load: the tech tree's first.</summary>
    public const int Oldest = 39;

    /// <summary>The market's five orders as format 57 had them, in turn.</summary>
    private static readonly (string Item, int Count)[][] OldOrders58 =
    [
        [("scrapChunk", 40), ("iron", 20), ("planks", 20)],
        [("plate", 30), ("gear", 20)],
        [("glass", 20), ("silicon", 20), ("frame", 10)],
        [("circuit", 20)],
        [("copper", 20), ("mica", 10), ("hide", 5)],
    ];

    /// <summary>What each level wants as format 58 first had it: the goods its base counts.</summary>
    private static readonly (string Item, int Count)[][] NewOrders58 =
    [
        [("scrapChunk", 40), ("iron", 20), ("planks", 20)],
        [("plate", 30), ("gear", 20), ("wire", 30)],
        [("glass", 20), ("silicon", 20), ("frame", 10)],
        [("circuit", 15), ("coil", 10), ("tube", 30)],
        [("copper", 40), ("mica", 20), ("hide", 15)],
    ];

    /// <summary>Step <c>i</c> turns a save of format <c>Oldest + i</c> into one of the next format.</summary>
    public static readonly IReadOnlyList<Func<JsonObject, JsonObject>> Steps =
    [
        // 39 → 40: trade by capsule (2026-10-30-trade-capsule); the sales' beat is gone, what stood in a station stays there to send.
        root =>
        {
            root["slices"]!["trade"]!.AsObject().Remove("ticks");
            foreach (var surface in root["surfaces"]!.AsArray()) surface!["slices"]!["trade"]!.AsObject().Remove("ticks");
            return root;
        },
        // 40 → 41: the inventory's room bought by upgrades (2026-10-30-room-upgrades); a world keeps what it had open, its equipment moved up.
        root =>
        {
            var granted = root["slices"]!["knowledge"]!["granted"]!.AsArray();
            var known = granted.Select(g => g!.GetValue<string>()).Concat(["bag1", "bag2", "bag3", "bag4", "bag5", "bag6", "bag7", "bag8", "bag9", "belt1", "belt2", "tradeSlots1", "tradeSlots2"]).Distinct().ToList();
            known.Sort(StringComparer.Ordinal);
            root["slices"]!["knowledge"]!["granted"] = new JsonArray([.. known.Select(k => (JsonNode)k)]);
            foreach (var slices in new[] { root["slices"]! }.Concat(root["surfaces"]!.AsArray().Select(s => s!["slices"]!)))
            {
                foreach (var held in slices["items"]!["inventories"]!.AsArray()) held!["slots"] = InventoryOf41(held["slots"]!.AsArray());
                var stations = slices["grid"]!["machines"]!.AsArray().Where(m => m!["kind"]!.GetValue<string>() is "tradeStation" or "autoTradeStation").Select(m => m!["id"]!.GetValue<int>()).ToHashSet();
                foreach (var held in slices["items"]!["crates"]!.AsArray())
                    if (stations.Contains(held!["id"]!.GetValue<int>()))
                        while (held["slots"]!.AsArray().Count < 8) held["slots"]!.AsArray().Add(null);
            }
            return root;
        },
        // 41 → 42: the fluff is gone from the game (2026-11-01-creatures-in-ink), and so are the ones a world had.
        root =>
        {
            foreach (var surface in root["surfaces"]!.AsArray())
            {
                var creatures = surface!["slices"]!["creatures"]!["creatures"]!.AsArray();
                foreach (var fluff in creatures.Where(c => c!["species"]!.GetValue<string>() == "fluff").ToList()) creatures.Remove(fluff);
            }
            return root;
        },
        // 42 → 43: the tree is gone (docs/decisions/2026-11-05-the-board.md), its gates' deposits with it; what they opened stays granted.
        root =>
        {
            root["slices"]!["knowledge"]!.AsObject().Remove("deposits");
            return root;
        },
        // 43 → 44 (liquid-oxygen): the bytes stay, their meaning changes. A tank's oxygen is now liquid, a generator's
        // store and work its gas and progress; a tank piped straight to a vent no longer airs it, and the player re-pipes.
        root => root,
        // 44 → 45 (water): a module added, its slice empty on the ship and every surface: no cistern held water before it.
        root =>
        {
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
                slices["water"] = new JsonObject { ["cisterns"] = new JsonArray(), ["making"] = new JsonArray() };
            return root;
        },
        // 45 → 46 (ports-left-to-right): a machine takes on its left and gives on its right, so the tank's and the
        // electrolyser's ports swapped sides and the pod's moved right; a pipe ending at a port's old cell ends at its new one.
        root =>
        {
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
                slices["pipes"]!["spans"] = SpansOf46(slices["grid"]!, slices["pipes"]!["spans"]!.AsArray());
            return root;
        },
        // 46 → 47 (2026-11-03-charger-two): the charger holds two batteries; what one held stays in its first slot.
        root =>
        {
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
            {
                var chargers = slices["grid"]!["machines"]!.AsArray().Where(m => m!["kind"]!.GetValue<string>() == "charger").Select(m => m!["id"]!.GetValue<int>()).ToHashSet();
                foreach (var held in slices["items"]!["crates"]!.AsArray())
                    if (chargers.Contains(held!["id"]!.GetValue<int>()))
                        while (held["slots"]!.AsArray().Count < 2) held["slots"]!.AsArray().Add(null);
            }
            return root;
        },
        // 47 → 48 (salvage-drones): a drone bay has a fifth slot, its fuel slot of canisters; its drones stay in the first four.
        root =>
        {
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
            {
                var bays = slices["grid"]!["machines"]!.AsArray().Where(m => m!["kind"]!.GetValue<string>() == "droneBay").Select(m => m!["id"]!.GetValue<int>()).ToHashSet();
                foreach (var held in slices["items"]!["crates"]!.AsArray())
                    if (bays.Contains(held!["id"]!.GetValue<int>()))
                        while (held["slots"]!.AsArray().Count < 5) held["slots"]!.AsArray().Add(null);
            }
            return root;
        },
        // 48 → 49 (2026-11-04-salvage-trips): a salvage drone's net holds several catches, each kept for its look; the piece
        // it caught moves from its target and its kind into the net, a stack it lifted in as its item.
        root =>
        {
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
                foreach (var drone in slices["logistics"]?["drones"]?.AsArray() ?? [])
                {
                    var d = drone!.AsObject();
                    if (d["caught"] is { } caught)
                    {
                        d["net"] = new JsonArray(new JsonObject { ["kind"] = caught.GetValue<string>(), ["piece"] = d["piece"]?.GetValue<int>() ?? 0 });
                        d.Remove("caught");
                        d.Remove("piece");
                    }
                    else if (d["load"] is JsonArray { Count: > 0 } load)
                        d["net"] = new JsonArray(new JsonObject { ["kind"] = load[0]!["item"]!.GetValue<string>(), ["piece"] = 0 });
                }
            return root;
        },
        // 49 → 50 (2026-11-04-footprint-is-the-picture): the workbench stands five cells tall and the arc bench four, their whole
        // pictures, where each stood two with the rest drawn over the cells above; one built or planned keeps its floor, its top up.
        root =>
        {
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
            {
                int width = slices["grid"]!["width"]!.GetValue<int>();
                int Up(string? kind) => kind switch { "workbench" => 3 * width, "arcBench" => 2 * width, _ => 0 };
                foreach (var machine in slices["grid"]!["machines"]!.AsArray())
                    machine!["cell"] = machine["cell"]!.GetValue<int>() - Up(machine["kind"]!.GetValue<string>());
                foreach (var plan in slices["building"]?["plans"]?.AsArray() ?? [])
                    if (plan!["place"] is { } place && place["layer"]?.GetValue<string>() == "machine")
                        plan["cell"] = plan["cell"]!.GetValue<int>() - Up(place["kind"]?.GetValue<string>());
            }
            return root;
        },
        // 50 → 51 (2026-11-06-no-ice-canister): the canister's craft is gone, so are its queued entries and its grant; the ice
        // paid for them goes with them, a planet's ice being all a player could have queued it of.
        root =>
        {
            const string gone = "canister";
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
                foreach (var queue in slices["crafting"]?["queues"]?.AsArray() ?? [])
                {
                    var entries = queue!["entries"]!.AsArray();
                    if (entries.FirstOrDefault()?["recipe"]?.GetValue<string>() == gone) queue["progress"] = 0;
                    foreach (var entry in entries.Where(e => e!["recipe"]!.GetValue<string>() == gone).ToList()) entries.Remove(entry);
                }
            if (root["slices"]!["knowledge"]?["granted"] is JsonArray granted)
                foreach (var grant in granted.Where(g => g!.GetValue<string>() == gone).ToList()) granted.Remove(grant);
            return root;
        },
        // 51 → 52 (2026-11-07-radar-mast): the radar antenna is a mast six cells tall where it stood four; one built or planned
        // keeps the roof it stood on, its top two cells up.
        root =>
        {
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
            {
                int width = slices["grid"]!["width"]!.GetValue<int>();
                int Up(string? kind) => kind switch { "radarAntenna" => 2 * width, _ => 0 };
                foreach (var machine in slices["grid"]!["machines"]!.AsArray())
                    machine!["cell"] = machine["cell"]!.GetValue<int>() - Up(machine["kind"]!.GetValue<string>());
                foreach (var plan in slices["building"]?["plans"]?.AsArray() ?? [])
                    if (plan!["place"] is { } place && place["layer"]?.GetValue<string>() == "machine")
                        plan["cell"] = plan["cell"]!.GetValue<int>() - Up(place["kind"]?.GetValue<string>());
            }
            return root;
        },
        // 52 → 53 (2026-11-07-creative-per-player): creative mode is each peer's; a world in creative mode keeps it for every peer
        // that has an inventory on any surface.
        root =>
        {
            var all = new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)).ToList();
            var peers = all.SelectMany(s => s["items"]!["inventories"]!.AsArray().Select(h => h!["id"]!.GetValue<int>())).Distinct().Order().ToList();
            foreach (var slices in all)
            {
                if (slices["items"]!["creative"] is JsonArray) continue;
                bool on = slices["items"]!["creative"]?.GetValue<bool>() == true;
                slices["items"]!["creative"] = new JsonArray([.. (on ? peers : []).Select(p => (JsonNode)p)]);
            }
            return root;
        },
        // 53 → 54 (2026-11-07-night-chills): the sky, a module added right after the grid; a planet in an older save wakes at the
        // start of its day. Loading reads slices in the modules' order, so each slices object is rebuilt with it in its place.
        root =>
        {
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
            {
                var obj = slices.AsObject();
                var kept = obj.ToList();
                obj.Clear();
                foreach (var (name, slice) in kept)
                {
                    obj[name] = slice;
                    if (name == "grid") obj["sky"] = new JsonObject { ["time"] = 0 };
                }
            }
            return root;
        },
        // 54 → 55 (2026-11-08-lurker-attacks): a capback's first move is a vent where it was a pounce; one mid-pounce stops where it is.
        root =>
        {
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
                foreach (var creature in slices["creatures"]?["creatures"]?.AsArray() ?? [])
                    if (creature!["species"]?.GetValue<string>() == "capback")
                        (creature["act"], creature["vx"], creature["vy"]) = (null, 0, 0);
            return root;
        },
        // 55 → 56 (flowing-liquids): a module added, its slice where it runs, after building, on the ship and every
        // surface: no clock run, no cell changed; a planet grows its lakes again from its seed as it loads.
        root =>
        {
            root["slices"] = Inserted56(root["slices"]!.AsObject());
            foreach (var surface in (root["surfaces"]?.AsArray() ?? []))
                surface!["slices"] = Inserted56(surface["slices"]!.AsObject());
            return root;
        },
        // 56 → 57 (generated-vessels): the places module joins last, on the ship and every surface: no contact found, no vessel out.
        root =>
        {
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
                slices["places"] = new JsonObject { ["contacts"] = new JsonArray(), ["nextContactId"] = 1 };
            return root;
        },
        // 57 → 58 (2026-11-08-mafia-reputation): orders give way to reputation; the world's level is the old orders met in turn, the
        // next counted from now. Both order lists frozen here, so a later change to the market's never changes what this step makes.
        root =>
        {
            var knowledge = root["slices"]!["knowledge"]!.AsObject();
            var sold = (knowledge["sold"]?.AsArray() ?? []).ToDictionary(s => s!["item"]!.GetValue<string>(), s => s!["count"]!.GetValue<int>(), StringComparer.Ordinal);
            int level = 0;
            while (level < OldOrders58.Length && OldOrders58[level].All(d => sold.GetValueOrDefault(d.Item) >= d.Count)) level++;
            var start = level < NewOrders58.Length ? NewOrders58[level] : [];
            knowledge["soldWorth"] = 0;
            knowledge["reputation"] = new JsonObject
            {
                ["level"] = level,
                ["base"] = new JsonArray([.. start.Select(d => d.Item).Order(StringComparer.Ordinal)
                    .Select(item => (JsonNode)new JsonObject { ["item"] = item, ["count"] = sold.GetValueOrDefault(item) })]),
                ["baseWorth"] = 0,
            };
            return root;
        },
        // 58 → 59 (2026-11-08-mafia-deliveries): the mafia's gear is sold as items, its crafts gone, so are their queued entries and their
        // blueprints; the parts paid for them go with them, as the canister's did at 51 (pre-release, no refund).
        root =>
        {
            string[] gone = ["scanner", "powerDriver", "heavyHelmet", "plasmaCutter", "regenSuit"];
            bool Gone(JsonNode? id) => gone.Contains(id!.GetValue<string>(), StringComparer.Ordinal);
            foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
                foreach (var queue in slices["crafting"]?["queues"]?.AsArray() ?? [])
                {
                    var entries = queue!["entries"]!.AsArray();
                    if (entries.FirstOrDefault() is { } first && Gone(first["recipe"])) queue["progress"] = 0;
                    foreach (var entry in entries.Where(e => Gone(e!["recipe"])).ToList()) entries.Remove(entry);
                }
            if (root["slices"]!["knowledge"]?["granted"] is JsonArray granted)
                foreach (var grant in granted.Where(Gone).ToList()) granted.Remove(grant);
            return root;
        },
    ];

    /// <summary>The slices with liquids' empty one after building's, where its module runs: a save holds them in that order.</summary>
    private static JsonObject Inserted56(JsonObject slices)
    {
        var all = slices.ToList();
        slices.Clear();
        var inserted = new JsonObject();
        foreach (var (id, slice) in all)
        {
            inserted[id] = slice;
            if (id == "building") inserted["liquids"] = new JsonObject { ["clock"] = 0, ["cells"] = new JsonArray() };
        }
        return inserted;
    }

    /// <summary><paramref name="old"/> with each end at a moved port's old cell at its new one, in order, a span made twice or made a point gone.</summary>
    private static JsonArray SpansOf46(JsonNode grid, JsonArray old)
    {
        int width = grid["width"]!.GetValue<int>();
        var moved = new Dictionary<int, int>();
        foreach (var machine in grid["machines"]!.AsArray())
        {
            int cell = machine!["cell"]!.GetValue<int>();
            int At(int x, int y) => cell + y * width + x;
            switch (machine["kind"]!.GetValue<string>())
            {
                case "tank": (moved[At(0, 2)], moved[At(3, 2)]) = (At(3, 2), At(0, 2)); break;
                case "electrolyser": (moved[At(3, 5)], moved[At(0, 5)]) = (At(0, 5), At(3, 5)); break;
                case "lifepod": moved[At(0, 5)] = At(5, 5); break;
            }
        }
        int To(JsonNode span, string end) => moved.TryGetValue(span[end]!.GetValue<int>(), out int now) ? now : span[end]!.GetValue<int>();
        var spans = old.Select(s => (A: To(s!, "a"), B: To(s!, "b"), Paid: s!["paid"]!.GetValue<bool>()))
            .Select(s => (A: Math.Min(s.A, s.B), B: Math.Max(s.A, s.B), s.Paid)).Where(s => s.A != s.B)
            .OrderBy(s => s.A).ThenBy(s => s.B).ToList();
        var kept = new JsonArray();
        for (int i = 0; i < spans.Count; i++)
            if (i == 0 || (spans[i].A, spans[i].B) != (spans[i - 1].A, spans[i - 1].B))
                kept.Add(new JsonObject { ["a"] = spans[i].A, ["b"] = spans[i].B, ["paid"] = spans[i].Paid });
        return kept;
    }

    /// <summary>An inventory of 36 own slots, then its equipment, as 38 own slots, then the same equipment.</summary>
    private static JsonArray InventoryOf41(JsonArray old)
    {
        JsonNode?[] Take(int from, int count) => [.. old.Skip(from).Take(count).Select(n => n?.DeepClone())];
        var none = new JsonNode?[1];
        return new JsonArray([.. Take(0, 36), none[0], none[0], .. Take(36, 8)]);
    }

    /// <summary>The format a save says it has.</summary>
    public static int FormatOf(string save) => JsonNode.Parse(save)!["format"]!.GetValue<int>();

    /// <summary>
    /// <paramref name="save"/> in this build's format, every step run from its own; a save of this
    /// format comes back as it is, and one older than <see cref="Oldest"/> or newer than this build as
    /// it is too, for <see cref="World.Load"/> to refuse with its reason.
    /// </summary>
    public static string Upgrade(string save)
    {
        int format = FormatOf(save);
        if (format == World.SaveFormat || format < Oldest || format > World.SaveFormat) return save;
        var root = JsonNode.Parse(save)!.AsObject();
        for (int f = format; f < World.SaveFormat; f++)
        {
            root = Steps[f - Oldest](root);
            root["format"] = f + 1;
        }
        return root.ToJsonString();
    }
}
