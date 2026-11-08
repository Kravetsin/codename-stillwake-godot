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
        ("parcel", "parcel"), ("deliveryCrate", "deliverycrate"), ("confiscated", "seized"),
    ];

    /// <summary>The pictures of each kind of salvage that drifts: their name, and how many there are.</summary>
    public static readonly IReadOnlyDictionary<string, (string Look, int Count)> Of =
        Names.ToDictionary(n => n.Kind, n => (n.Look, Content.Looks.CountOf(n.Kind)));

    /// <summary>
    /// Wreckage, aimed at by its whole picture rather than near its middle (0034): its picture's size in
    /// pixels of the world, eight to a cell, outline included, the sim's own size of it (<c>Content.Wreckage.Sizes</c>).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (int W, int H)> Wrecks =
        Content.Wreckage.Sizes.ToDictionary(w => w.Item, w => (w.W * PixelsPerCell, w.H * PixelsPerCell));

    private const int PixelsPerCell = 8;

    /// <summary>The picture piece <paramref name="id"/> of <paramref name="item"/> wears (<c>Content.Looks.Of</c>), or null to draw the item; a look's own, by its number from 0, too.</summary>
    public static string? PictureOf(string item, int id) =>
        NameOf(item, id) is { } name ? $"res://art/{name}.png" : null;

    /// <summary>The same picture by the name <c>PixelArt.Picture</c> takes, which picks up its hi-res drawing, or null to draw the item.</summary>
    public static string? NameOf(string item, int id) =>
        Of.TryGetValue(item, out var look) ? $"debris/{look.Look}_{Content.Looks.Of(item, id) + 1:00}" : null;

    /// <summary>A vessel's salvage objects with no picture of their own whole (plan V-1b), wearing their debris piece's first look.</summary>
    public static readonly IReadOnlyList<string> SalvageAsDebris = ["salvageClod"];

    /// <summary>
    /// The picture a vessel's salvage object (<c>machine_&lt;kind&gt;</c>) of <see cref="SalvageAsDebris"/> wears: its debris piece's first
    /// look, as it drifts; null for any other picture, a salvage object drawn whole among them.
    /// </summary>
    public static string? SalvagePicture(string name) =>
        name.StartsWith("machine_", System.StringComparison.Ordinal) && SalvageAsDebris.Contains(name["machine_".Length..])
        && Content.Vessels.SalvageOf(name["machine_".Length..]) is { } salvage
            ? NameOf(salvage.From, 0) : null;

    /// <summary>Every picture of a look by the name <c>PixelArt.Picture</c> takes.</summary>
    public static IEnumerable<string> NamesOf((string Look, int Count) look) =>
        Enumerable.Range(1, look.Count).Select(n => $"debris/{look.Look}_{n:00}");
}
