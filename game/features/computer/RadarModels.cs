using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;

namespace Stillwake.Game.Features.Computer;

/// <summary>
/// A class's model on the radar, from the side, stern on the left: one unit its whole length, nozzles to bow, drawn scaled. Its
/// parts behind the hull (bridges, fins, superstructure), the hull, glass lit in the ship's colour, its lines and its rings.
/// </summary>
internal sealed class RadarModel
{
    public RadarModel(Vector2[] hull, Vector2[][] back, Vector2[][]? glass = null, Vector2[]? lines = null, (Vector2 At, float R)[]? rings = null)
    {
        (Hull, Back, Glass, Lines, Rings) = (hull, back, glass ?? [], lines ?? [], rings ?? []);
        HullLine = Closed(hull);
        BackLines = Array.ConvertAll(back, Closed);
        var ys = back.SelectMany(p => p).Concat(hull).Select(p => p.Y).ToList();
        Mid = (ys.Min() + ys.Max()) / 2;
    }

    public Vector2[] Hull { get; }
    public Vector2[] HullLine { get; }
    public Vector2[][] Back { get; }
    public Vector2[][] BackLines { get; }
    public Vector2[][] Glass { get; }
    /// <summary>Segments in pairs, drawn in one call: decks, bulkheads, pipes, masts.</summary>
    public Vector2[] Lines { get; }
    public (Vector2 At, float R)[] Rings { get; }
    /// <summary>The middle of its height: drawn there at the point it is drawn at.</summary>
    public float Mid { get; }

    private static Vector2[] Closed(Vector2[] polygon) => [.. polygon, polygon[0]];
}

/// <summary>
/// The radar's models of the vessel classes and how long each is drawn: by its frame's length, so a shuttle reads small beside a
/// freighter and a liner or a tanker the biggest, within a least that keeps a shuttle readable and a most that keeps the big ones on the map.
/// </summary>
internal static class RadarModels
{
    /// <summary>Pixels a cell of a frame's length on the map, and the least and most a model is drawn there.</summary>
    public const float MapPerCell = 0.2f, MapLeast = 26, MapMost = 76;
    /// <summary>The same in a callout's 60 × 48 picture.</summary>
    public const float CardPerCell = 0.15f, CardLeast = 20, CardMost = 56;

    private static readonly Dictionary<string, int> Lengths = [];

    /// <summary>
    /// Pixels <paramref name="cls"/>'s model is drawn long: its frame's columns at <paramref name="perCell"/>, within
    /// <paramref name="least"/>..<paramref name="most"/>; a class not known as a freighter.
    /// </summary>
    public static float Long(string cls, float perCell, float least, float most)
    {
        if (!Lengths.TryGetValue(cls, out int cells)) Lengths[cls] = cells = (Vessels.Class(cls) ?? Vessels.Class("freighter")!).Frame.Width;
        return Mathf.Clamp(cells * perCell, least, most);
    }

    /// <summary>The model of <paramref name="cls"/>; a freighter's for a class it does not know.</summary>
    public static RadarModel Of(string cls) => cls switch
    {
        "shuttle" => Shuttle,
        "liner" => Liner,
        "tanker" => Tanker,
        _ => Freighter,
    };

    private static Vector2 V(float x, float y) => new(x, y);
    private static Vector2[] Box(float x0, float y0, float x1, float y1) => [V(x0, y0), V(x1, y0), V(x1, y1), V(x0, y1)];

    /// <summary>A dome's top half over (<paramref name="cx"/>, <paramref name="cy"/>).</summary>
    private static Vector2[] Dome(float cx, float cy, float r) =>
        [.. Enumerable.Range(0, 13).Select(i => V(cx + r * Mathf.Cos(Mathf.Pi + Mathf.Pi * i / 12), cy + r * Mathf.Sin(Mathf.Pi + Mathf.Pi * i / 12)))];

    /// <summary>Stubby, two decks, its cockpit's glass in the bow, a fin over its stern and skids under it.</summary>
    private static readonly RadarModel Shuttle = new(
        [V(-0.44f, -0.12f), V(0.12f, -0.17f), V(0.34f, -0.14f), V(0.47f, -0.02f), V(0.5f, 0.05f), V(0.44f, 0.13f), V(-0.4f, 0.15f), V(-0.44f, 0.1f)],
        [
            [V(-0.5f, -0.06f), V(-0.43f, -0.08f), V(-0.43f, 0.07f), V(-0.5f, 0.05f)],
            [V(-0.41f, -0.1f), V(-0.34f, -0.27f), V(-0.25f, -0.27f), V(-0.2f, -0.12f)],
        ],
        [
            [V(0.2f, -0.14f), V(0.33f, -0.12f), V(0.43f, -0.03f), V(0.24f, -0.04f)],
            Box(-0.31f, -0.08f, -0.25f, -0.04f), Box(-0.19f, -0.09f, -0.13f, -0.05f), Box(-0.07f, -0.1f, -0.01f, -0.06f),
        ],
        [
            V(-0.4f, 0.02f), V(0.44f, 0.02f),
            V(0.04f, 0.04f), V(0.04f, 0.14f), V(0.12f, 0.04f), V(0.12f, 0.13f),
            V(-0.28f, 0.15f), V(-0.32f, 0.21f), V(-0.37f, 0.21f), V(-0.2f, 0.21f),
            V(0.24f, 0.13f), V(0.28f, 0.21f), V(0.2f, 0.21f), V(0.36f, 0.21f),
        ]);

    /// <summary>Long and low, three holds, its raised bridge and mast forward, containers on deck and one slung under.</summary>
    private static readonly RadarModel Freighter = new(
        [V(-0.44f, -0.06f), V(0.3f, -0.06f), V(0.4f, -0.03f), V(0.47f, 0.01f), V(0.5f, 0.04f), V(0.45f, 0.08f), V(-0.42f, 0.08f), V(-0.44f, 0.05f)],
        [
            [V(-0.5f, -0.04f), V(-0.44f, -0.05f), V(-0.44f, 0f), V(-0.5f, -0.01f)],
            [V(-0.5f, 0.02f), V(-0.44f, 0.01f), V(-0.44f, 0.06f), V(-0.5f, 0.05f)],
            [V(0.2f, -0.06f), V(0.22f, -0.11f), V(0.33f, -0.11f), V(0.36f, -0.06f)],
            Box(-0.36f, -0.1f, -0.22f, -0.06f), Box(-0.2f, -0.1f, -0.06f, -0.06f), Box(-0.04f, -0.1f, 0.1f, -0.06f),
            Box(-0.16f, 0.08f, 0.06f, 0.12f),
        ],
        [[V(0.23f, -0.1f), V(0.32f, -0.1f), V(0.335f, -0.08f), V(0.225f, -0.08f)]],
        [
            V(-0.42f, 0.01f), V(0.46f, 0.01f),
            V(-0.2f, -0.06f), V(-0.2f, 0.08f), V(0f, -0.06f), V(0f, 0.08f), V(0.2f, -0.06f), V(0.2f, 0.08f),
            V(-0.29f, -0.1f), V(-0.29f, -0.06f), V(-0.13f, -0.1f), V(-0.13f, -0.06f), V(0.03f, -0.1f), V(0.03f, -0.06f),
            V(0.26f, -0.11f), V(0.26f, -0.17f), V(0.235f, -0.15f), V(0.285f, -0.15f),
        ]);

    /// <summary>
    /// Long, its superstructure stepped: the promenade's lit glass, a deck over it with the dome, the bridge forward on top, the fin
    /// over the stern with its emblem; three decks in the hull.
    /// </summary>
    private static readonly RadarModel Liner = new(
        [V(-0.45f, -0.03f), V(0.34f, -0.03f), V(0.44f, 0f), V(0.49f, 0.03f), V(0.5f, 0.06f), V(0.46f, 0.1f), V(-0.42f, 0.1f), V(-0.45f, 0.06f)],
        [
            [V(-0.5f, 0f), V(-0.45f, -0.005f), V(-0.45f, 0.035f), V(-0.5f, 0.03f)],
            [V(-0.5f, 0.045f), V(-0.45f, 0.04f), V(-0.45f, 0.08f), V(-0.5f, 0.075f)],
            [V(-0.45f, -0.03f), V(-0.43f, -0.16f), V(-0.37f, -0.17f), V(-0.34f, -0.07f), V(-0.35f, -0.03f)],
            [V(-0.37f, -0.03f), V(-0.35f, -0.07f), V(0.3f, -0.07f), V(0.34f, -0.03f)],
            [V(-0.28f, -0.07f), V(-0.26f, -0.11f), V(0.18f, -0.11f), V(0.22f, -0.07f)],
            Dome(-0.1f, -0.11f, 0.055f),
            [V(0.09f, -0.11f), V(0.11f, -0.145f), V(0.19f, -0.145f), V(0.24f, -0.11f)],
        ],
        [
            [V(-0.33f, -0.063f), V(0.29f, -0.063f), V(0.315f, -0.04f), V(-0.345f, -0.04f)],
            Box(-0.24f, -0.1f, -0.17f, -0.085f), Box(-0.03f, -0.1f, 0.08f, -0.085f),
            Dome(-0.1f, -0.115f, 0.042f),
            [V(0.175f, -0.138f), V(0.19f, -0.138f), V(0.225f, -0.114f), V(0.2f, -0.114f)],
        ],
        [
            V(-0.44f, 0.015f), V(0.46f, 0.015f), V(-0.43f, 0.045f), V(0.495f, 0.045f), V(-0.42f, 0.075f), V(0.48f, 0.075f),
            V(0.15f, -0.145f), V(0.15f, -0.19f), V(0.13f, -0.175f), V(0.17f, -0.175f),
        ],
        [(V(-0.395f, -0.12f), 0.014f)]);

    /// <summary>
    /// Long and low, its bridge tall over the stern with a mast; two pipes along its back with their risers and a manifold; its hull
    /// in tank bays, a round tank in each; a boom past the bow.
    /// </summary>
    private static readonly RadarModel Tanker = new(
        [V(-0.45f, -0.04f), V(0.38f, -0.04f), V(0.44f, -0.01f), V(0.47f, 0.03f), V(0.44f, 0.07f), V(-0.43f, 0.07f), V(-0.45f, 0.04f)],
        [
            [V(-0.5f, -0.02f), V(-0.45f, -0.03f), V(-0.45f, 0.01f), V(-0.5f, 0f)],
            [V(-0.5f, 0.02f), V(-0.45f, 0.02f), V(-0.45f, 0.06f), V(-0.5f, 0.05f)],
            [V(-0.44f, -0.04f), V(-0.43f, -0.15f), V(-0.31f, -0.15f), V(-0.29f, -0.12f), V(-0.3f, -0.04f)],
            Box(0.02f, -0.085f, 0.07f, -0.04f),
        ],
        [Box(-0.42f, -0.14f, -0.305f, -0.125f), Box(-0.42f, -0.105f, -0.31f, -0.095f)],
        [
            V(-0.29f, -0.055f), V(0.41f, -0.055f), V(-0.29f, -0.07f), V(0.36f, -0.07f),
            V(-0.2f, -0.04f), V(-0.2f, -0.07f), V(-0.06f, -0.04f), V(-0.06f, -0.07f), V(0.16f, -0.04f), V(0.16f, -0.07f), V(0.3f, -0.04f), V(0.3f, -0.07f),
            V(-0.27f, -0.04f), V(-0.27f, 0.07f), V(-0.12f, -0.04f), V(-0.12f, 0.07f), V(0.03f, -0.04f), V(0.03f, 0.07f),
            V(0.18f, -0.04f), V(0.18f, 0.07f), V(0.33f, -0.04f), V(0.33f, 0.07f),
            V(-0.37f, -0.15f), V(-0.37f, -0.2f), V(-0.4f, -0.185f), V(-0.34f, -0.185f),
            V(0.46f, 0.01f), V(0.5f, 0f),
        ],
        [(V(-0.195f, 0.015f), 0.042f), (V(-0.045f, 0.015f), 0.042f), (V(0.105f, 0.015f), 0.042f), (V(0.255f, 0.015f), 0.042f)]);
}
