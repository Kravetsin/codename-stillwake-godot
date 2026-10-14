using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Each item's picture (render/item-art.ts, exported to <c>art/items/*.png</c>), sixteen pixels square:
/// in slots, in the hand, on the floor; and its silhouette in white, the pale rim a stack on the floor
/// stands out by.
/// </summary>
public static class ItemIcons
{
    /// <summary>An item picture's side, in pixels.</summary>
    public const int Pixels = 16;

    private static readonly Dictionary<string, Texture2D> Icons = new(), Silhouettes = new();

    /// <summary>The picture of <paramref name="item"/>, loaded once.</summary>
    public static Texture2D Icon(string item)
    {
        if (!Icons.TryGetValue(item, out var icon))
            Icons[item] = icon = GD.Load<Texture2D>($"res://art/items/{item}.png");
        return icon;
    }

    /// <summary>
    /// Makes every item's silhouette now, while the game loads: one is read back from the GPU, which waits for
    /// the frames in flight, two refreshes of a 240 Hz screen, if left until a piece of debris first shows it.
    /// </summary>
    public static void MakeSilhouettes()
    {
        foreach (var item in Content.Items.All) Silhouette(item.Id);
    }

    /// <summary>The picture of <paramref name="item"/> with every pixel drawn white, its clear ones clear.</summary>
    public static Texture2D Silhouette(string item)
    {
        if (Silhouettes.TryGetValue(item, out var kept)) return kept;
        var image = Icon(item).GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
                image.SetPixel(x, y, new Color(1, 1, 1, image.GetPixel(x, y).A));
        return Silhouettes[item] = ImageTexture.CreateFromImage(image);
    }
}
