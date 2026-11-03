using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The creatures' parts as pictures (0102), drawn in ink (<c>art/hd/creatures</c>): each read once and shared by every individual, which
/// differ by a tint at the sprite (<see cref="CreatureVariety.InkTint"/>); a white copy for a blow's flash, its glow, and the spots its
/// glow lights from. The markings an individual wears make their own copies of the part, kept for the run: a few steps of each.
/// </summary>
public static class CreatureParts
{
    /// <summary>What a part is drawn as: its colours, all white (the flash), or its glowing pixels only.</summary>
    public enum Kind { Colour, White, Glow }

    /// <summary>The part an individual wears more or fewer of, and more or less boldly.</summary>
    public const string Marks = "marks";

    /// <summary>What of an individual a part's picture shows: the markings it wears, their opacity.</summary>
    private readonly record struct Worn(int Kept, int Opacity);

    private static readonly Dictionary<(string, string, Kind, Worn), Texture2D> Made = new();
    // Each colour picture's pixels, kept as made: what a creature bursts into is read from them, never back from the GPU.
    private static readonly Dictionary<Texture2D, (int W, int H, byte[] Data)> Pixels = new();
    private static readonly Dictionary<(string, string), (Vector2 At, Color Colour)[]> Spots = new();
    private static readonly Dictionary<string, int[]> Markings = new();
    private static readonly Dictionary<string, (int W, int H, byte[] Data)> Drawings = new();

    /// <summary>Part <paramref name="part"/> of <paramref name="design"/>, as <paramref name="kind"/>, on <paramref name="one"/>.</summary>
    public static Texture2D Of(CreatureArt.Design design, string part, Kind kind, CreatureVariety.Individual one) => Inked(design.Id, part, kind, one);

    /// <summary>Part <paramref name="part"/> of the guard in ink <paramref name="id"/>, as <paramref name="kind"/>: every one of them alike.</summary>
    public static Texture2D Ink(string id, string part, Kind kind) => Inked(id, part, kind, default);

    /// <summary>Part <paramref name="part"/> of design <paramref name="id"/> as drawn; only its markings make copies. Its logical pixels are kept for what it crumbles into.</summary>
    private static Texture2D Inked(string id, string part, Kind kind, CreatureVariety.Individual one)
    {
        bool marked = part == Marks;
        string name = $"creatures/{id}/{part}";
        var (w, h, drawn) = InkPicture(kind == Kind.Glow ? name + "_glow" : name);
        int kept = -1;
        int[]? markings = null;
        if (marked)
        {
            markings = Markings.TryGetValue(name, out var known) ? known : Markings[name] = CreatureVariety.Markings(drawn, w, h);
            int count = markings.Max() + 1;
            kept = count >= CreatureVariety.MostMarkings ? one.Kept : one.Kept & ((1 << count) - 1);
        }
        var worn = new Worn(kept, marked && kind == Kind.Colour ? one.Opacity : 1000);
        var key = (id, part, kind, worn);
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
    /// Where a part's glow lights from, in its own pixels from its pivot, and the colour: its glow reduced to the world's pixels, as a game
    /// light is placed, gathered by squares of <see cref="CreatureLooks.SpotPixels"/>, the biggest first. None for a part with no glow.
    /// </summary>
    public static (Vector2 At, Color Colour)[] SpotsOf(CreatureArt.Design design, string part)
    {
        if (!design.Parts[part].Glow) return [];
        var key = (design.Id, part);
        if (Spots.TryGetValue(key, out var known)) return known;
        var (fw, fh, fine) = InkPicture($"creatures/{design.Id}/{part}_glow");
        var (w, h, data) = (fw / HiRes.Scale, fh / HiRes.Scale, CreatureVariety.Reduced(fine, fw, fh, HiRes.Scale));
        var pivot = new Vector2(design.Parts[part].PivotX, design.Parts[part].PivotY);
        var squares = new Dictionary<(int, int), (Vector2 Sum, Vector3 Colour, int Count)>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int o = (y * w + x) * 4;
                if (data[o + 3] == 0) continue;
                var at = (x / CreatureLooks.SpotPixels, y / CreatureLooks.SpotPixels);
                var (sum, colour, count) = squares.GetValueOrDefault(at);
                squares[at] = (sum + new Vector2(x + 0.5f, y + 0.5f), colour + new Vector3(data[o], data[o + 1], data[o + 2]) / 255f, count + 1);
            }
        return Spots[key] = squares.Values.OrderByDescending(s => s.Count).ThenBy(s => s.Sum.Y)
            .Select(s => (s.Sum / s.Count - pivot, new Color(s.Colour.X / s.Count, s.Colour.Y / s.Count, s.Colour.Z / s.Count, Math.Min(1f, s.Count / 4f))))
            .ToArray();
    }
}
