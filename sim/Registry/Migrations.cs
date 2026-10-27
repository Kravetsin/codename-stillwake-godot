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
/// Format 28 halved the cell (0104): no save before it migrates, its cells and lengths those of another
/// scale and a planet's edits laid on a ground that now grows differently, so its steps went with it. Format 29 made pipes
/// spans (0109): no save before it migrates either, nothing released having held one. Format 30 took the stripper and the
/// wrench out of the game (plan modes): no save before it migrates, every hero having been given both and nothing released.
/// Format 31 grew planets anew (stairs for cliffs, veins of ore, crystals by the lodes): no save before it migrates, a
/// planet's edits laid on ground that now grows differently, and planets reached only from the dev panel. Format 32 added
/// the landing module (the launch platform), its slice empty. Format 33 renamed the fauna module creatures
/// (2026-10-26-creatures-are-composed); format 34 composed its species, dropping the creatures out, which planets reached only
/// from the dev panel held. Format 35 made the suit an item worn in its own slot (2026-10-27-suits-are-worn); format 36 added
/// the cape's slot (2026-10-27-the-cape-is-worn).
/// </summary>
public static class Migrations
{
    /// <summary>The oldest format a save may have and still load: the veined planets' first.</summary>
    public const int Oldest = 31;

    /// <summary>Step <c>i</c> turns a save of format <c>Oldest + i</c> into one of the next format.</summary>
    public static readonly IReadOnlyList<Func<JsonObject, JsonObject>> Steps = [LandingJoins, CreaturesRenamed, CreaturesComposed, SuitsWorn, CapeSlot];

    /// <summary>31 to 32: the landing module (the launch platform) joins every surface, holding nothing yet.</summary>
    private static JsonObject LandingJoins(JsonObject save)
    {
        save["slices"]!.AsObject()["landing"] = new JsonObject();
        if (save["surfaces"] is JsonArray surfaces)
            foreach (var surface in surfaces) surface!["slices"]!.AsObject()["landing"] = new JsonObject();
        return save;
    }

    /// <summary>
    /// 32 to 33: the fauna slice is the creatures slice, on the ship and every surface. Renamed in place, key by key, since
    /// <c>World.Restore</c> reads the slices in the modules' order and a key removed and added again would come last.
    /// </summary>
    private static JsonObject CreaturesRenamed(JsonObject save)
    {
        save["slices"] = Renamed(save["slices"]!.AsObject(), "fauna", "creatures");
        if (save["surfaces"] is JsonArray surfaces)
            foreach (var surface in surfaces) surface!["slices"] = Renamed(surface["slices"]!.AsObject(), "fauna", "creatures");
        return save;
    }

    /// <summary>
    /// 33 to 34: species composed of drives and acts (2026-10-26-creatures-are-composed), a creature's mode gone. The creatures
    /// of every surface are let go, their ids and clock kept: only planets opened from the dev panel held any.
    /// </summary>
    private static JsonObject CreaturesComposed(JsonObject save)
    {
        save["slices"]!["creatures"]!["creatures"] = new JsonArray();
        if (save["surfaces"] is JsonArray surfaces)
            foreach (var surface in surfaces) surface!["slices"]!["creatures"]!["creatures"] = new JsonArray();
        return save;
    }

    /// <summary>
    /// 34 to 35: the suit is a helmet on the head and a body (2026-10-27-suits-are-worn), every hero having worn one, and the
    /// head's lamp has a slot of its own. Every peer's inventory gains the body's and the helmet mount's slots, last, its lamp moved
    /// from the head to its own; a hero's, on its surface, the basic helmet and body, an inventory made for a hero with none
    /// saved; a peer with no hero, its ship's inventory, so it comes back suited. A hero's <c>suit</c> is its <c>air</c>.
    /// </summary>
    private static readonly int Head = Content.Inventory.Slots;

    /// <summary>A peer's inventory's slots in format 35, the body's slot before the lamp's last: later formats add more.</summary>
    private static readonly int SuitsTotal = Content.Inventory.Slots + 7;

    private static JsonObject SuitsWorn(JsonObject save)
    {
        var all = new List<JsonObject> { save["slices"]!.AsObject() };
        if (save["surfaces"] is JsonArray surfaces) all.AddRange(surfaces.Select(s => s!["slices"]!.AsObject()));
        var heroes = all.SelectMany(s => s["crew"]!["heroes"]!.AsArray()).Select(h => h!["id"]!.GetValue<int>()).ToHashSet();
        for (int i = 0; i < all.Count; i++)
        {
            var inventories = all[i]["items"]!["inventories"]!.AsArray();
            foreach (var held in inventories)
            {
                var slots = held!["slots"]!.AsArray();
                var lamp = slots[Head]?.DeepClone();
                slots[Head] = null;
                slots.Add(null);
                slots.Add(lamp);
            }
            var suited = all[i]["crew"]!["heroes"]!.AsArray().Select(h => h!["id"]!.GetValue<int>()).ToList();
            // The ship suits the peers without a hero anywhere whose inventory it keeps.
            if (i == 0) suited.AddRange(inventories.Select(h => h!["id"]!.GetValue<int>()).Where(id => !heroes.Contains(id)));
            foreach (int peer in suited.Order())
            {
                var held = inventories.FirstOrDefault(h => h!["id"]!.GetValue<int>() == peer);
                if (held == null)
                {
                    var slots = new JsonArray();
                    for (int s = 0; s < SuitsTotal; s++) slots.Add(null);
                    held = new JsonObject { ["id"] = peer, ["slots"] = slots };
                    int at = inventories.Count(h => h!["id"]!.GetValue<int>() < peer);
                    inventories.Insert(at, held);
                }
                held["slots"]![Head] = new JsonObject { ["item"] = Content.Helmet.Basic, ["count"] = 1 };
                held["slots"]![SuitsTotal - 2] = new JsonObject { ["item"] = Content.Suit.Basic, ["count"] = 1 };
            }
            foreach (var hero in all[i]["crew"]!["heroes"]!.AsArray().Select(h => h!.AsObject()))
            {
                var air = hero["suit"];
                hero.Remove("suit");
                hero["air"] = air;
            }
        }
        return save;
    }

    /// <summary>35 to 36: every peer's inventory, on the ship and every surface, gains the cape's slot, last and empty.</summary>
    private static JsonObject CapeSlot(JsonObject save)
    {
        var all = new List<JsonObject> { save["slices"]!.AsObject() };
        if (save["surfaces"] is JsonArray surfaces) all.AddRange(surfaces.Select(s => s!["slices"]!.AsObject()));
        foreach (var slices in all)
            foreach (var held in slices["items"]!["inventories"]!.AsArray()) held!["slots"]!.AsArray().Add(null);
        return save;
    }

    /// <summary>A copy of <paramref name="slices"/> in the same order, the slice <paramref name="from"/> named <paramref name="to"/>.</summary>
    private static JsonObject Renamed(JsonObject slices, string from, string to)
    {
        var renamed = new JsonObject();
        foreach (var (key, value) in slices.ToList())
        {
            slices.Remove(key);
            renamed[key == from ? to : key] = value;
        }
        return renamed;
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
