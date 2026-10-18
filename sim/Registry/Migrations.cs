using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Sim.Rooms;

namespace Stillwake.Sim;

/// <summary>
/// Old saves made loadable (0031): each step turns a save of one format into the next, the JSON of
/// <see cref="World.Save"/> in and out, so a save from any format since <see cref="Oldest"/> loads in
/// this build. Every bump of <see cref="World.SaveFormat"/> ships its step. A module new to a format
/// gets its slice from the step, by hand — empty where a running world allows it, else made from the
/// other slices — never a silent default, so a forgotten step fails loudly in <see cref="World.Load"/>.
/// Only saves read from disk are upgraded: a guest's welcome and a resync come from this very build.
/// </summary>
public static class Migrations
{
    /// <summary>The oldest format a save may have and still load.</summary>
    public const int Oldest = 3;

    /// <summary>Step <c>i</c> turns a save of format <c>Oldest + i</c> into one of the next format.</summary>
    public static readonly IReadOnlyList<Func<JsonObject, JsonObject>> Steps =
    [
        // 3 → 4: every piece of debris has taken no hits yet (0034).
        save =>
        {
            foreach (var piece in save["slices"]!["debris"]!["pieces"]!.AsArray()) piece!["damage"] = 0;
            return save;
        },
        // 4 → 5: the grid's layers saved as runs, so empty space costs nothing.
        save =>
        {
            var grid = save["slices"]!["grid"]!;
            foreach (var layer in new[] { "structure", "material", "conduit", "back", "backMaterial" })
                grid[layer] = Runs.Node([.. grid[layer]!.AsArray().Select(n => (int)n!)]);
            return save;
        },
        // 5 → 6: a solar panel needs the stars (0043): one that made power, out in space, loses the back
        // wall behind it, so it makes power still; one in a room made none and is left as it was.
        save =>
        {
            var grid = save["slices"]!["grid"]!;
            int width = (int)grid["width"]!, height = (int)grid["height"]!;
            var back = Runs.Cells(grid["back"]!);
            var backMaterial = Runs.Cells(grid["backMaterial"]!);
            var (roomOf, _) = RoomsModule.Find(width, height, Runs.Cells(grid["structure"]!), back);
            foreach (var m in grid["machines"]!.AsArray())
            {
                var spec = Content.Machines.Get((string)m!["kind"]!);
                if (spec.Makes == null) continue;
                int cell = (int)m["cell"]!;
                var cells = Enumerable.Range(0, spec.Height)
                    .SelectMany(dy => Enumerable.Range(0, spec.Width).Select(dx => cell + dy * width + dx)).ToList();
                if (cells.All(c => roomOf[c] == RoomsModule.Space))
                    foreach (int c in cells) back[c] = backMaterial[c] = 0;
            }
            grid["back"] = Runs.Node(back);
            grid["backMaterial"] = Runs.Node(backMaterial);
            return save;
        },
        // 6 → 7: no piece of debris is being reeled in by a harpoon yet (0044).
        save =>
        {
            foreach (var piece in save["slices"]!["debris"]!["pieces"]!.AsArray()) piece!["reel"] = null;
            return save;
        },
        // 7 → 8: a fire names its machine, a furnace's or a burner generator's, no longer its furnace.
        save =>
        {
            foreach (var fire in save["slices"]!["furnace"]!["fires"]!.AsArray())
            {
                var o = fire!.AsObject();
                var machine = o["furnace"];
                o.Remove("furnace");
                o["machine"] = machine;
            }
            return save;
        },
        // 8 → 9: every inventory gains its equipment slots, empty (0047); no hero thrusts yet.
        save =>
        {
            foreach (var held in save["slices"]!["items"]!["inventories"]!.AsArray())
            {
                var slots = held!["slots"]!.AsArray();
                while (slots.Count < Content.Inventory.Total) slots.Add(null);
            }
            foreach (var hero in save["slices"]!["crew"]!["heroes"]!.AsArray())
            {
                hero!["thrusting"] = false;
                hero["thrustTicks"] = 0;
            }
            return save;
        },
        // 9 → 10: the world grows to Content.World's size, a build zone in its middle (0048).
        IntoWorld,
        // 10 → 11: debris spawns by zones (0052): every piece out is the stream's, never seen by a hero yet,
        // and the stream's beat is its own; the far field's is given on the first tick. Like every step, it
        // leaves what is already of the new shape as it is: tests relabel a new save as an old one.
        save =>
        {
            var debris = save["slices"]!["debris"]!.AsObject();
            foreach (var piece in debris["pieces"]!.AsArray())
            {
                piece!["zone"] ??= Content.Debris.Ship.Id;
                piece["seenAt"] ??= 0;
            }
            if (debris["nextSpawn"] is { } next)
            {
                debris.Remove("nextSpawn");
                debris["beats"] = new JsonArray(new JsonObject { ["zone"] = Content.Debris.Ship.Id, ["next"] = (int)next });
            }
            return save;
        },
        // 11 → 12: a stack may hold charge (0053), written only when it holds some: no save before held any, so
        // nothing changes but the format, which keeps an older build from loading a battery and losing its charge.
        save => save,
        // 12 → 13: an electric tool draws while it runs, not by the hit (0055); no save before ran one.
        save =>
        {
            save["slices"]!["items"]!["running"] ??= new JsonArray();
            return save;
        },
        // 13 → 14: stations (0057): a fire keeps only its fuel, and what a furnace was smelting moves to crafting.
        Stations,
        // 14 → 15: a floor stack may be part of a body (0061), written only when it is: no save before held one, so
        // its stacks are no one's and nothing changes but the format, which keeps an older build from losing the tag.
        save => save,
        // 15 → 16: the Former and the assembler take inputs (0057); their entries, saved without a feed, load as paid.
        StationInputs,
    ];

    /// <summary>
    /// 15 → 16: what the Former and the circuit assembler hold gains their input slots, empty, in front, so what they
    /// made stays in their outputs. A machine already of its new size is left as it is.
    /// </summary>
    public static JsonObject StationInputs(JsonObject save)
    {
        var slices = save["slices"]!;
        var kinds = slices["grid"]!["machines"]!.AsArray().ToDictionary(m => (int)m!["id"]!, m => (string)m!["kind"]!);
        foreach (var held in slices["items"]!["crates"]!.AsArray())
        {
            if (!kinds.TryGetValue((int)held!["id"]!, out var kind) || kind is not ("former" or "assembler")) continue;
            var slots = held["slots"]!.AsArray();
            while (slots.Count < Content.Machines.Get(kind).Slots) slots.Insert(0, null);
        }
        return save;
    }

    /// <summary>
    /// 13 → 14: each fire's item and progress, in ticks, becomes crafting's smelt, in thousandths of a tick;
    /// a fire with no fuel left, kept only for that, goes. Crafting now runs before building, so its slice does.
    /// </summary>
    private static JsonObject Stations(JsonObject save)
    {
        var slices = save["slices"]!.AsObject();
        var crafting = slices["crafting"]!.AsObject();
        if (crafting["smelts"] is not JsonArray smelts) crafting["smelts"] = smelts = [];
        var fires = new JsonArray();
        foreach (var fire in slices["furnace"]!["fires"]!.AsArray())
        {
            int machine = (int)fire!["machine"]!, fuel = (int)fire["fuel"]!;
            if (fire["item"] is { } item)
                smelts.Add(new JsonObject { ["machine"] = machine, ["item"] = (string)item!, ["progress"] = (int)fire["progress"]! * 1000 });
            if (fuel > 0) fires.Add(new JsonObject { ["machine"] = machine, ["fuel"] = fuel });
        }
        slices["furnace"]!["fires"] = fires;
        // The slices in the modules' order: crafting moved before building.
        var order = slices.Select(p => p.Key).Where(k => k != "crafting").ToList();
        order.Insert(order.IndexOf("building"), "crafting");
        var moved = order.Select(k => (k, slices[k]!.DeepClone())).ToList();
        slices.Clear();
        foreach (var (key, slice) in moved) slices[key] = slice;
        return save;
    }

    /// <summary>
    /// A save's grid centred in a world of <c>Content.World</c>'s size, by the offset its own size gives:
    /// every cell saved moves there, every position in thousandths of a cell with it, and the grid gets
    /// the world's zone. A cell off the old grid, which only the fuzz reports, is off the new one too: -1.
    /// </summary>
    private static JsonObject IntoWorld(JsonObject save)
    {
        var slices = save["slices"]!;
        var grid = slices["grid"]!;
        int w = (int)grid["width"]!, h = (int)grid["height"]!;
        int width = Content.World.Width, height = Content.World.Height;
        var (ox, oy) = Layout.OffsetInWorld(w, h);
        int Cell(int c) => c < 0 || c >= w * h ? -1 : (c / w + oy) * width + c % w + ox;
        void MoveCell(JsonNode o, string key) => o[key] = Cell((int)o[key]!);
        void MoveX(JsonNode o, string key) => o[key] = (int)o[key]! + ox * 1000;
        void MoveY(JsonNode o, string key) => o[key] = (int)o[key]! + oy * 1000;

        foreach (var layer in new[] { "structure", "material", "conduit", "back", "backMaterial" })
        {
            var old = Runs.Cells(grid[layer]!);
            var cells = new int[width * height];
            for (int y = 0; y < h; y++) Array.Copy(old, y * w, cells, (y + oy) * width + ox, w);
            grid[layer] = Runs.Node(cells);
        }
        grid["width"] = width;
        grid["height"] = height;
        var zone = Layout.WorldZone;
        grid["zone"] = new JsonObject { ["left"] = zone.Left, ["top"] = zone.Top, ["right"] = zone.Right, ["bottom"] = zone.Bottom };
        foreach (var m in grid["machines"]!.AsArray()) MoveCell(m!, "cell");

        foreach (var stack in slices["items"]!["floor"]!.AsArray())
        {
            MoveCell(stack!, "cell");
            MoveX(stack!, "x");
            MoveY(stack!, "y");
        }
        foreach (var cable in slices["power"]!["cables"]!.AsArray())
        {
            MoveCell(cable!, "a");
            MoveCell(cable!, "b");
        }
        foreach (var plan in slices["building"]!["plans"]!.AsArray()) MoveCell(plan!, "cell");
        var atmosphere = slices["atmosphere"]!;
        atmosphere["aired"] = new JsonArray([.. atmosphere["aired"]!.AsArray().Select(n => (JsonNode?)Cell((int)n!))]);
        var crew = slices["crew"]!;
        MoveCell(crew, "spawn");
        if (crew["pod"] != null) MoveCell(crew, "pod");
        foreach (var hero in crew["heroes"]!.AsArray()) MoveCell(hero!, "cell");
        foreach (var piece in slices["debris"]!["pieces"]!.AsArray())
        {
            foreach (var x in new[] { "x", "start", "end" }) MoveX(piece!, x);
            MoveY(piece!, "y");
            if (piece!["reel"] is { } reel)
            {
                MoveX(reel, "toX");
                MoveY(reel, "toY");
            }
        }
        return save;
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
