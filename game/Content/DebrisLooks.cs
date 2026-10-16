using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// How a piece of debris looks: each kind of salvage has pictures of its own under
/// <c>art/debris</c>, <c>&lt;look&gt;_NN.png</c>, a pixel of art a pixel of the world, and a piece wears
/// the one its id picks, so every peer sees it the same and pieces in a row differ. A kind with none is
/// drawn as its item.
/// </summary>
public static class DebrisLooks
{
    /// <summary>The pictures of each kind of salvage that drifts: their name, and how many there are.</summary>
    public static readonly IReadOnlyDictionary<string, (string Look, int Count)> Of = new Dictionary<string, (string, int)>
    {
        ["scrapChunk"] = ("scrap", 12),
        ["hullPlate"] = ("hull", 12),
        ["cargoCrate"] = ("crate", 8),
        ["supplyPod"] = ("pod", 4),
    };

    /// <summary>The picture piece <paramref name="id"/> of <paramref name="item"/> wears, or null to draw the item.</summary>
    public static string? PictureOf(string item, int id) =>
        Of.TryGetValue(item, out var look) ? $"res://art/debris/{look.Look}_{id % look.Count + 1:00}.png" : null;
}
