using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A held tool's pictures with its modules' overlays laid over them (<see cref="HeldParts"/>, 0098), put together once a
/// picture and set of layers and kept: the hand and the card draw the same texture, whatever is fitted. The areas its
/// effects live in are read from the same picture (<see cref="EffectArea"/>).
/// </summary>
public static class HeldArt
{
    private static readonly Dictionary<string, Texture2D> Made = new();
    private static readonly Dictionary<string, Image> Laid = new();
    private static readonly Dictionary<(string, (double, double, double, double)), EffectArea> Areas = new();

    /// <summary><paramref name="picture"/> with <paramref name="layers"/> laid over it in order; the picture itself for none.</summary>
    public static Texture2D Of(string picture, IReadOnlyList<HeldLayer> layers)
    {
        string key = KeyOf(picture, layers);
        if (Made.TryGetValue(key, out var kept)) return kept;
        return Made[key] = HiRes.Texture(LaidOver(picture, layers, key));
    }

    /// <summary>
    /// The area an effect lives in on <paramref name="picture"/> with <paramref name="layers"/> laid over it: its drawn pixels in
    /// <paramref name="box"/>, in pixels of the picture. Read once a picture, set of layers and box.
    /// </summary>
    public static EffectArea AreaOf(string picture, IReadOnlyList<HeldLayer> layers, (double X0, double Y0, double X1, double Y1) box)
    {
        string key = KeyOf(picture, layers);
        if (Areas.TryGetValue((key, box), out var kept)) return kept;
        var image = LaidOver(picture, layers, key);
        return Areas[(key, box)] = EffectArea.Of(image.GetWidth(), image.GetHeight(), (x, y) => image.GetPixel(x, y).A > 0.5f, HiRes.Scale, box);
    }

    private static string KeyOf(string picture, IReadOnlyList<HeldLayer> layers) =>
        layers.Count == 0 ? picture : picture + "|" + string.Join("|", layers.Select(l => $"{l.Picture}@{l.X},{l.Y}"));

    /// <summary>The hi-res picture with the layers laid over, made once a key.</summary>
    private static Image LaidOver(string picture, IReadOnlyList<HeldLayer> layers, string key)
    {
        if (Laid.TryGetValue(key, out var kept)) return kept;
        var fine = Fine(picture);
        foreach (var layer in layers)
        {
            var over = Fine(layer.Picture);
            fine.BlendRect(over, new Rect2I(Vector2I.Zero, over.GetSize()), new Vector2I(layer.X, layer.Y) * HiRes.Scale);
        }
        return Laid[key] = fine;
    }

    /// <summary>The hi-res picture standing for the one at <paramref name="path"/>.</summary>
    private static Image Fine(string path) => HiRes.Load(path["res://art/".Length..^4]) ?? throw new System.InvalidOperationException($"No picture {path}");
}
