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
    ];

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
