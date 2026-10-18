using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game;

/// <summary>
/// How a piece of debris looks: each kind of salvage has pictures of its own under
/// <c>art/debris</c>, <c>&lt;look&gt;_NN.png</c>, a pixel of art a pixel of the world, and a piece wears
/// the one its id picks, so every peer sees it the same and pieces in a row differ. A kind with none is
/// drawn as its item.
/// </summary>
public static class DebrisLooks
{
    /// <summary>The name of each kind's pictures, <c>&lt;name&gt;_NN.png</c>; how many there are is the sim's (<c>Content.Looks</c>).</summary>
    private static readonly (string Kind, string Look)[] Names =
    [
        ("scrapChunk", "scrap"), ("hullPlate", "hull"), ("cargoCrate", "crate"), ("supplyPod", "pod"),
        ("hullSection", "section"), ("cargoContainer", "container"), ("machineBlock", "machine"), ("cableRack", "rack"),
        ("tankCluster", "tanks"), ("shieldSlab", "slab"), ("soilClod", "soilclod"),
        ("asteroid", "asteroid"), ("asteroidSmall", "smallrock"), ("asteroidLarge", "bigrock"), ("asteroidRich", "orerock"),
        ("engineBlock", "engine"), ("cargoHold", "hold"), ("podCluster", "pods"),
    ];

    /// <summary>The pictures of each kind of salvage that drifts: their name, and how many there are.</summary>
    public static readonly IReadOnlyDictionary<string, (string Look, int Count)> Of =
        Names.ToDictionary(n => n.Kind, n => (n.Look, Content.Looks.CountOf(n.Kind)));

    /// <summary>
    /// Wreckage, aimed at by its whole picture rather than near its middle (0034): its picture's size in
    /// pixels of the world, sixteen to a cell, outline included.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (int W, int H)> Wrecks = new Dictionary<string, (int, int)>
    {
        ["hullSection"] = (48, 32),
        ["cargoContainer"] = (64, 32),
        ["machineBlock"] = (48, 32),
        ["cableRack"] = (32, 32),
        ["tankCluster"] = (32, 32),
        ["shieldSlab"] = (64, 32),
        ["soilClod"] = (32, 32),
        ["asteroid"] = (32, 32),
        ["asteroidSmall"] = (24, 24),
        ["asteroidLarge"] = (48, 48),
        ["asteroidRich"] = (40, 40),
        // Derelicts (0056): the biggest pieces out, 5 to 6 cells across.
        ["engineBlock"] = (80, 48),
        ["cargoHold"] = (96, 48),
        ["podCluster"] = (64, 64),
    };

    /// <summary>The picture piece <paramref name="id"/> of <paramref name="item"/> wears (<c>Content.Looks.Of</c>), or null to draw the item; a look's own, by its number from 0, too.</summary>
    public static string? PictureOf(string item, int id) =>
        Of.TryGetValue(item, out var look) ? $"res://art/debris/{look.Look}_{Content.Looks.Of(item, id) + 1:00}.png" : null;
}
