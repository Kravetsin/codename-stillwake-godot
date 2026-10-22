using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Stillwake.Game.Features.Fauna;

/// <summary>
/// The creatures' parts as pictures (0102): each index map of <c>art/fauna</c> recoloured once, at first use, by a palette — its
/// R the ramp, its G the tone — into its colours, a white copy for a blow's flash, and its glow map in the glow ramp a tone up;
/// with the spots its glow lights from. Kept for the run: four designs of a dozen small parts each.
/// </summary>
public static class CreatureParts
{
    /// <summary>What a part is drawn as: its colours, all white (the flash), or its glowing pixels only.</summary>
    public enum Kind { Colour, White, Glow }

    private static readonly Dictionary<(string, string, string, Kind), Texture2D> Made = new();
    private static readonly Dictionary<(string, string, string), (Vector2 At, Color Colour)[]> Spots = new();

    /// <summary>Part <paramref name="part"/> of <paramref name="design"/> in <paramref name="palette"/>, as <paramref name="kind"/>.</summary>
    public static Texture2D Of(FaunaArt.Design design, string part, string palette, Kind kind)
    {
        var key = (design.Id, part, palette, kind);
        if (Made.TryGetValue(key, out var made)) return made;
        var (w, h, data) = Read(kind == Kind.Glow ? FaunaArt.GlowOf(design.Id, part) : FaunaArt.PictureOf(design.Id, part));
        var ramps = FaunaArt.Palettes[palette];
        for (int q = 0; q < w * h; q++)
        {
            int o = q * 4;
            if (data[o + 3] == 0) continue;
            var (r, g, b) = kind switch
            {
                Kind.White => ((byte)255, (byte)255, (byte)255),
                // A glowing pixel shines a tone brighter than it is painted (the page's glow pass).
                Kind.Glow => ramps[design.Glow][Math.Min(5, data[o + 1] + 1)],
                _ => ramps[data[o]][data[o + 1]],
            };
            (data[o], data[o + 1], data[o + 2], data[o + 3]) = (r, g, b, 255);
        }
        return Made[key] = ImageTexture.CreateFromImage(Image.CreateFromData(w, h, false, Image.Format.Rgba8, data));
    }

    /// <summary>
    /// Where a part's glow lights from, in its own pixels from its pivot, and the colour: its glowing pixels gathered by squares
    /// of <see cref="FaunaLooks.SpotPixels"/>, the biggest first. None for a part with no glow.
    /// </summary>
    public static (Vector2 At, Color Colour)[] SpotsOf(FaunaArt.Design design, string part, string palette)
    {
        var key = (design.Id, part, palette);
        if (Spots.TryGetValue(key, out var known)) return known;
        if (!design.Parts[part].Glow) return Spots[key] = [];
        var (w, h, data) = Read(FaunaArt.GlowOf(design.Id, part));
        var pivot = new Vector2(design.Parts[part].PivotX, design.Parts[part].PivotY);
        var ramp = FaunaArt.Palettes[palette][design.Glow];
        var squares = new Dictionary<(int, int), (Vector2 Sum, Vector3 Colour, int Count)>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int o = (y * w + x) * 4;
                if (data[o + 3] == 0) continue;
                var (r, g, b) = ramp[Math.Min(5, data[o + 1] + 1)];
                var at = (x / FaunaLooks.SpotPixels, y / FaunaLooks.SpotPixels);
                var (sum, colour, count) = squares.GetValueOrDefault(at);
                squares[at] = (sum + new Vector2(x + 0.5f, y + 0.5f), colour + new Vector3(r, g, b) / 255f, count + 1);
            }
        return Spots[key] = squares.Values.OrderByDescending(s => s.Count).ThenBy(s => s.Sum.Y)
            .Select(s => (s.Sum / s.Count - pivot, new Color(s.Colour.X / s.Count, s.Colour.Y / s.Count, s.Colour.Z / s.Count, Math.Min(1f, s.Count / 4f))))
            .ToArray();
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
