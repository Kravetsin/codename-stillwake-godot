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
    ];

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
