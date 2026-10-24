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
/// spans (0109): no save before it migrates either, nothing released having held one.
/// </summary>
public static class Migrations
{
    /// <summary>The oldest format a save may have and still load: pipes as spans' first (0109).</summary>
    public const int Oldest = 29;

    /// <summary>Step <c>i</c> turns a save of format <c>Oldest + i</c> into one of the next format.</summary>
    public static readonly IReadOnlyList<Func<JsonObject, JsonObject>> Steps = [LandingJoins];

    /// <summary>29 to 30: the landing module (the launch platform) joins every surface, holding nothing yet.</summary>
    private static JsonObject LandingJoins(JsonObject save)
    {
        save["slices"]!.AsObject()["landing"] = new JsonObject();
        if (save["surfaces"] is JsonArray surfaces)
            foreach (var surface in surfaces) surface!["slices"]!.AsObject()["landing"] = new JsonObject();
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
