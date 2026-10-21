using System;
using System.Collections.Generic;
using Anchor = Stillwake.Content.Flora.Anchor;

namespace Stillwake.Game;

/// <summary>What a piece of a home's picture does live (ADR 0090), as the concept page's emitters put it.</summary>
public enum Emits
{
    /// <summary>An arc crackling from (X, Y) to (X2, Y2), or into the air by Reach: Rate a page tick (24 a second), Life page ticks.</summary>
    Arc,
    /// <summary>Its light and glow swelling and sinking by Amp round whole, slowly or flickering.</summary>
    Pulse,
    /// <summary>A broken lamp: dark, then a burst of flashes and short arcs from From to (X2, Y2), dark again.</summary>
    Sputter,
    /// <summary>Four stars of light circling (X, Y), Radius across and Radius2 deep: the cradle's mobile.</summary>
    Mobile,
    /// <summary>A hologram's scanline running down a box Width by Height, now and then the whole frame jumping.</summary>
    Holo,
    /// <summary>One pixel that never went out, blinking.</summary>
    Pixel,
    /// <summary>Beads twinkling at Points, one now and then.</summary>
    Twinkle,
    /// <summary>Spores rising, Count a second or so, in Colour.</summary>
    Spore,
    /// <summary>Embers flying up from a fire Width across.</summary>
    Embers,
    /// <summary>Count motes of dust turning in the light of a box to (X2, Y2).</summary>
    Motes,
    /// <summary>Cloth hanging from (X, Y), swaying (the page left it still; the user asked for it).</summary>
    Sway,
}

/// <summary>A live emitter of a piece's picture, in pixels from its box's corner; the fields its kind reads (<see cref="Emits"/>).</summary>
public sealed record Emitter(Emits Kind, float X, float Y)
{
    public float X2 { get; init; }
    public float Y2 { get; init; }
    public (float X, float Y) From { get; init; }
    public bool ToAir { get; init; }
    public float Reach { get; init; }
    public float Rate { get; init; }
    public float Life { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
    public float Light { get; init; }
    public bool Beat { get; init; }
    public float Radius { get; init; }
    public float Radius2 { get; init; }
    public (int R, int G, int B) Colour { get; init; }
    public float Amp { get; init; }
    public bool Slow { get; init; }
    public int Count { get; init; }
    public (float X, float Y)[] Points { get; init; } = [];
}

/// <summary>A piece's own light, the page's: from (X, Y) of its box, its colour, reaching Reach cells at Strength.</summary>
public sealed record HomeLight(float X, float Y, (float R, float G, float B) Colour, float Reach, float Strength);

public static partial class HomesArt
{
    /// <summary>
    /// A piece's pictures: <paramref name="Variants"/> looks, its box (the page's piece, <paramref name="BoxW"/> by <paramref name="BoxH"/>)
    /// at (<paramref name="OriginX"/>, <paramref name="OriginY"/>) of a picture <paramref name="Width"/> by <paramref name="Height"/>; a dark
    /// look besides where its lamp may be out (<paramref name="Dark"/>).
    /// </summary>
    public sealed record Picture(int Variants, int BoxW, int BoxH, int OriginX, int OriginY, int Width, int Height, bool Dark)
    {
        public HomeLight[] Lights { get; init; } = [];
        public Emitter[] Emitters { get; init; } = [];
    }
}

/// <summary>
/// How the arc makers' homes look (ADR 0090): their pieces' pictures, drawn in <c>art/homes</c> from the concept page's own
/// code (<see cref="HomesArt"/>), where each stands against what holds it, which lamps burn, how their light falls, and how
/// cloth sways. Plain C#, so where a piece stands is tested.
/// </summary>
public static class HomesLooks
{
    private const int Cell = 16;

    /// <summary>The picture of piece <paramref name="kind"/> at <paramref name="cell"/>: its variant by a hash of its cell, lit or dark.</summary>
    public static string PictureOf(string kind, int cell, bool lit, int? variant = null)
    {
        var art = HomesArt.Of[kind];
        int v = variant ?? (int)(Hash(cell) % (uint)art.Variants);
        return lit || !art.Dark ? $"homes/{kind}_{v}" : $"homes/{kind}_{v}_dark";
    }

    /// <summary>
    /// Whether piece <paramref name="kind"/>'s lamp at <paramref name="cell"/> burns (the page's mix by the world's seed): what never
    /// goes out always, every other lamp two in three, by a hash of its cell, so every peer sees the same.
    /// </summary>
    public static bool Lit(string kind, int cell) => !HomesArt.Of[kind].Dark || Hash(cell) / 7 % LitOf != 0;

    /// <summary>One in this many lamps that may go out is out.</summary>
    public const int LitOf = 3;

    /// <summary>
    /// Where piece <paramref name="kind"/>'s box lies, its corner in pixels, standing in its <paramref name="across"/> columns from
    /// cell (<paramref name="x"/>, <paramref name="y"/>), its own: on a floor its bottom on the floor's top in the middle of its
    /// columns, or on the top of the piece it stands on, <paramref name="standsOn"/> pixels up (a table's top, a pixel into it);
    /// from a ceiling its top on its underside; on a back wall in the middle of its cells; in the rock (the fallen beam, the rubble)
    /// on the floor under its walls, its right end at theirs and <see cref="RockOver"/> past it.
    /// </summary>
    public static (int X, int Y) BoxAt(string kind, Anchor on, int x, int y, int across, int tall, int? standsOn = null)
    {
        var art = HomesArt.Of[kind];
        int left = x * Cell + (across * Cell - art.BoxW) / 2;
        return on switch
        {
            Anchor.Floor => (left, (standsOn ?? (y + 1) * Cell) - art.BoxH),
            Anchor.Ceiling => (left, y * Cell),
            Anchor.Back => (left, y * Cell + (tall * Cell - art.BoxH) / 2),
            _ => ((x + across) * Cell + RockOver.GetValueOrDefault(kind) - art.BoxW, (y + 1) * Cell - 1 - art.BoxH),
        };
    }

    /// <summary>How far a piece in the rock reaches past its walls' right end: the beam's high end leans over the room.</summary>
    public static readonly IReadOnlyDictionary<string, int> RockOver = new Dictionary<string, int> { ["fallenBeam"] = 12 };

    /// <summary>How far into the top of a piece another stands on it sinks, in pixels: the page's things on a table sit a pixel in.</summary>
    public static int SinksInto(string under) => under is "table" or "desk" ? 1 : 0;

    /// <summary>
    /// Their lamps burn brighter and colder than ours (the page's scenes widen every arc light a fifth and strengthen it by
    /// three tenths): a light's reach and strength times these; its keep a cell, as the caves' lights.
    /// </summary>
    public const float ArcReach = 1.2f, ArcStrength = 1.3f, Keep = 0.72f, LightSize = 4f;

    /// <summary>The page's arc colour, (0.56, 0.78, 1): the lights it strengthens.</summary>
    public static bool IsArc((float R, float G, float B) c) => Math.Abs(c.R - 0.56f) < 0.01f && Math.Abs(c.B - 1f) < 0.01f;

    /// <summary>The most homes' lights shown at once, the nearest the view's middle: a lit home holds about a dozen.</summary>
    public const int MostLights = 40;

    /// <summary>
    /// Cloth swaying (<see cref="Emits.Sway"/>): its bottom row at most <see cref="SwayMost"/> pixels off, whole pixels, the rows above
    /// less, over a slow breath of <see cref="SwaySeconds"/> seconds, each piece its own phase.
    /// </summary>
    public const float SwayMost = 1.4f, SwaySeconds = 3.6f;

    /// <summary>How many pixels row <paramref name="row"/> of <paramref name="rows"/> under its pivot sways at <paramref name="seconds"/>.</summary>
    public static int SwayAt(int row, int rows, double seconds, double phase) =>
        rows <= 0 || row <= 0 ? 0 : (int)Math.Round(Math.Sin(seconds * 2 * Math.PI / SwaySeconds + phase) * SwayMost * row / rows);

    /// <summary>
    /// The music box's tune (a placeholder): its tine at these semitones over its own note, a short phrase of a lullaby, a note every
    /// <see cref="TuneNoteSeconds"/>, played whenever it is hit.
    /// </summary>
    public static readonly int[] Tune = [0, 4, 7, 12, 7, 9, 4, 0];

    public const double TuneNoteSeconds = 0.19;

    public static uint Hash(int cell)
    {
        uint h = unchecked((uint)cell * 0x9E3779B1u + 0x7F4A7C15u);
        h ^= h >> 15;
        h = unchecked(h * 0x2C1B3C6Du);
        return h ^ (h >> 12);
    }
}
