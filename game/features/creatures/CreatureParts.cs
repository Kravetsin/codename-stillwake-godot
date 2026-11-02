using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The creatures' parts as pictures (0102): each index map of <c>art/creatures</c> recoloured once, at first use, by a palette — its
/// R the ramp, its G the tone — into its colours, a white copy for a blow's flash, and its glow map in the glow ramp a tone up;
/// with the spots its glow lights from. An individual's coat (<see cref="CreatureVariety.Tone"/>) and the markings it wears
/// make their own copies of the parts they touch, kept for the run too: a few steps of each, a dozen small parts a design.
/// </summary>
public static class CreatureParts
{
    /// <summary>What a part is drawn as: its colours, all white (the flash), or its glowing pixels only.</summary>
    public enum Kind { Colour, White, Glow }

    /// <summary>The part an individual wears more or fewer of, and more or less boldly.</summary>
    public const string Marks = "marks";

    /// <summary>What of an individual a part's picture shows: its coat's shade and blend, the markings it wears, their opacity.</summary>
    private readonly record struct Worn(int Shade, int Hue, int Kept, int Opacity);

    private static readonly Dictionary<(string, string, string, Kind, Worn), Texture2D> Made = new();
    // Each colour picture's pixels, kept as made: what a creature bursts into is read from them, never back from the GPU.
    private static readonly Dictionary<Texture2D, (int W, int H, byte[] Data)> Pixels = new();
    private static readonly Dictionary<(string, string, string, int), (Vector2 At, Color Colour)[]> Spots = new();
    private static readonly Dictionary<(string, string), int[]> Markings = new();
    private static readonly Dictionary<string, (int W, int H, byte[] Data)> Drawings = new();

    /// <summary>
    /// Part <paramref name="part"/> of <paramref name="design"/> in <paramref name="palette"/>, as <paramref name="kind"/>, on
    /// <paramref name="one"/> of a species varying by <paramref name="v"/>.
    /// </summary>
    public static Texture2D Of(CreatureArt.Design design, string part, string palette, Kind kind, CreatureLooks.Variety v, CreatureVariety.Individual one)
    {
        if (design.Ink) return Inked(design.Id, part, kind, one);
        bool coat = kind == Kind.Colour && Array.IndexOf(design.Parts[part].Ramps, v.Coat) >= 0;
        bool marked = part == Marks;
        var worn = new Worn(coat ? one.Shade : 0, coat ? one.Hue : 0, marked ? KeptOf(design, part, one) : -1,
            marked && kind == Kind.Colour ? one.Opacity : 1000);
        var key = (design.Id, part, palette, kind, worn);
        if (Made.TryGetValue(key, out var made)) return made;
        var (w, h, data) = Read(kind == Kind.Glow ? CreatureArt.GlowOf(design.Id, part) : CreatureArt.PictureOf(design.Id, part));
        var markings = marked ? MarkingsOf(design, part) : null;
        var ramps = CreatureArt.Palettes[palette];
        byte alpha = (byte)((255 * worn.Opacity + 500) / 1000);
        for (int q = 0; q < w * h; q++)
        {
            int o = q * 4;
            if (data[o + 3] == 0) continue;
            if (markings != null && !CreatureVariety.Wears(one, markings[q]))
            {
                data[o + 3] = 0;
                continue;
            }
            var (r, g, b) = kind switch
            {
                Kind.White => ((byte)255, (byte)255, (byte)255),
                // A glowing pixel shines a tone brighter than it is painted (the page's glow pass).
                Kind.Glow => ramps[design.Glow][Math.Min(5, data[o + 1] + 1)],
                _ => CreatureVariety.Tone(ramps, v, one, data[o], data[o + 1]),
            };
            (data[o], data[o + 1], data[o + 2], data[o + 3]) = (r, g, b, alpha);
        }
        var texture = Made[key] = ImageTexture.CreateFromImage(Image.CreateFromData(w, h, false, Image.Format.Rgba8, data));
        if (kind == Kind.Colour) Pixels[texture] = (w, h, data);
        return texture;
    }

    /// <summary>Part <paramref name="part"/> of the guard in ink <paramref name="id"/>, as <paramref name="kind"/>: every one of them alike.</summary>
    public static Texture2D Ink(string id, string part, Kind kind) => Inked(id, part, kind, default);

    /// <summary>
    /// Part <paramref name="part"/> of a design in ink: the picture as drawn, shared by every individual, which differ by a tint at the sprite
    /// (<see cref="CreatureVariety.InkTint"/>); only its markings make copies. Its logical pixels are kept for what it crumbles into.
    /// </summary>
    private static Texture2D Inked(string id, string part, Kind kind, CreatureVariety.Individual one)
    {
        bool marked = part == Marks;
        string name = $"creatures/{id}/{part}";
        var (w, h, drawn) = InkPicture(kind == Kind.Glow ? name + "_glow" : name);
        int kept = -1;
        int[]? markings = null;
        if (marked)
        {
            markings = Markings.TryGetValue((name, ""), out var known) ? known : Markings[(name, "")] = CreatureVariety.Markings(drawn, w, h);
            int count = markings.Max() + 1;
            kept = count >= CreatureVariety.MostMarkings ? one.Kept : one.Kept & ((1 << count) - 1);
        }
        var worn = new Worn(0, 0, kept, marked && kind == Kind.Colour ? one.Opacity : 1000);
        var key = (id, part, "", kind, worn);
        if (Made.TryGetValue(key, out var made)) return made;
        var data = (byte[])drawn.Clone();
        for (int q = 0; q < w * h; q++)
        {
            int o = q * 4;
            if (data[o + 3] == 0) continue;
            data[o + 3] = markings != null && !CreatureVariety.Wears(one, markings[q]) ? (byte)0 : (byte)((data[o + 3] * worn.Opacity + 500) / 1000);
            if (kind == Kind.White) (data[o], data[o + 1], data[o + 2]) = ((byte)255, (byte)255, (byte)255);
        }
        var texture = Made[key] = HiRes.Texture(Image.CreateFromData(w, h, false, Image.Format.Rgba8, data));
        if (kind == Kind.Colour) Pixels[texture] = (w / HiRes.Scale, h / HiRes.Scale, CreatureVariety.Reduced(data, w, h, HiRes.Scale));
        return texture;
    }

    /// <summary>A part in ink's glow at the world's pixels, as a game light is placed.</summary>
    private static (int W, int H, byte[] Data) InkGlow(CreatureArt.Design design, string part)
    {
        var (w, h, data) = InkPicture($"creatures/{design.Id}/{part}_glow");
        return (w / HiRes.Scale, h / HiRes.Scale, CreatureVariety.Reduced(data, w, h, HiRes.Scale));
    }

    /// <summary>A picture in ink's pixels, read once: a body is dressed each time one comes into view.</summary>
    private static (int W, int H, byte[] Data) InkPicture(string name)
    {
        if (Drawings.TryGetValue(name, out var known)) return known;
        var image = HiRes.Load(name) ?? throw new InvalidOperationException($"No picture {name}");
        return Drawings[name] = (image.GetWidth(), image.GetHeight(), image.GetData());
    }

    /// <summary>The pixels of a colour picture <see cref="Of"/> made, as made: Rgba8, row by row; none for any other.</summary>
    public static (int W, int H, byte[] Data)? PixelsOf(Texture2D texture) => Pixels.TryGetValue(texture, out var p) ? p : null;

    /// <summary>
    /// Where a part's glow lights from, in its own pixels from its pivot, and the colour: its glowing pixels gathered by squares
    /// of <see cref="CreatureLooks.SpotPixels"/>, the biggest first, only of the markings <paramref name="one"/> wears. None for a
    /// part with no glow.
    /// </summary>
    public static (Vector2 At, Color Colour)[] SpotsOf(CreatureArt.Design design, string part, string palette, CreatureVariety.Individual one)
    {
        if (!design.Parts[part].Glow) return [];
        bool marked = part == Marks && !design.Ink;
        var key = (design.Id, part, palette, marked ? KeptOf(design, part, one) : -1);
        if (Spots.TryGetValue(key, out var known)) return known;
        var (w, h, data) = design.Ink ? InkGlow(design, part) : Read(CreatureArt.GlowOf(design.Id, part));
        var markings = marked ? MarkingsOf(design, part) : null;
        var pivot = new Vector2(design.Parts[part].PivotX, design.Parts[part].PivotY);
        var ramp = design.Ink ? [] : CreatureArt.Palettes[palette][design.Glow];
        var squares = new Dictionary<(int, int), (Vector2 Sum, Vector3 Colour, int Count)>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int o = (y * w + x) * 4;
                if (data[o + 3] == 0 || markings != null && !CreatureVariety.Wears(one, markings[y * w + x])) continue;
                var (r, g, b) = design.Ink ? (data[o], data[o + 1], data[o + 2]) : ramp[Math.Min(5, data[o + 1] + 1)];
                var at = (x / CreatureLooks.SpotPixels, y / CreatureLooks.SpotPixels);
                var (sum, colour, count) = squares.GetValueOrDefault(at);
                squares[at] = (sum + new Vector2(x + 0.5f, y + 0.5f), colour + new Vector3(r, g, b) / 255f, count + 1);
            }
        return Spots[key] = squares.Values.OrderByDescending(s => s.Count).ThenBy(s => s.Sum.Y)
            .Select(s => (s.Sum / s.Count - pivot, new Color(s.Colour.X / s.Count, s.Colour.Y / s.Count, s.Colour.Z / s.Count, Math.Min(1f, s.Count / 4f))))
            .ToArray();
    }

    /// <summary>The markings of a part's picture, each pixel's (<see cref="CreatureVariety.Markings"/>); its glow map's pixels lie on them.</summary>
    private static int[] MarkingsOf(CreatureArt.Design design, string part)
    {
        if (Markings.TryGetValue((design.Id, part), out var known)) return known;
        var (w, h, data) = Read(CreatureArt.PictureOf(design.Id, part));
        return Markings[(design.Id, part)] = CreatureVariety.Markings(data, w, h);
    }

    /// <summary>The markings <paramref name="one"/> wears of those the part has, as bits: two individuals wearing the same share a picture.</summary>
    private static int KeptOf(CreatureArt.Design design, string part, CreatureVariety.Individual one)
    {
        int count = MarkingsOf(design, part).Max() + 1;
        return count >= CreatureVariety.MostMarkings ? one.Kept : one.Kept & ((1 << count) - 1);
    }

    /// <summary>An index map's pixels as it was painted: imported lossless, read back byte for byte.</summary>
    private static (int W, int H, byte[] Data) Read(string path)
    {
        var image = GD.Load<Texture2D>(path).GetImage();
        if (image.IsCompressed()) image.Decompress();
        if (image.GetFormat() != Image.Format.Rgba8) image.Convert(Image.Format.Rgba8);
        return (image.GetWidth(), image.GetHeight(), image.GetData());
    }
}
