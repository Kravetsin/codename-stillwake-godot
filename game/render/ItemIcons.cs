using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Each item's picture (render/item-art.ts, exported to <c>art/items/*.png</c>), sixteen pixels square:
/// in slots, on the floor, in space; and its silhouette in white, the pale rim a stack on the floor stands out
/// by. A tool, an item held as a picture, has none drawn: in the world it lies as it is held, its picture at its
/// size; its icon is that picture baked turned and fitted to the side it is drawn at (<see cref="ToolIcon"/>,
/// 2026-10-27-tool-icons).
/// </summary>
public static class ItemIcons
{
    /// <summary>An item picture's side, in pixels.</summary>
    public const int Pixels = 16;

    private static readonly Dictionary<string, Texture2D> Icons = new(), Silhouettes = new();
    private static readonly Dictionary<(string Item, int Side), Texture2D> Baked = new();
    private static readonly Dictionary<string, Image> Held = new();
    private static readonly Dictionary<string, int> ClearRows = new();

    /// <summary>
    /// The picture of <paramref name="item"/> as the world draws it, a pixel of it a pixel of the world, loaded once: a tool's
    /// plain held picture, as large as in the hand; any other item's 16 px picture.
    /// </summary>
    public static Texture2D Icon(string item) => HeldLook.Of(item) is { } look ? GD.Load<Texture2D>(look.Picture) : Drawn(item);

    /// <summary>The picture of <paramref name="item"/> where it is drawn <paramref name="side"/> px wide: a tool's icon baked at that side, drawn 1:1; any other item's 16 px picture.</summary>
    public static Texture2D Icon(string item, int side) => HeldLook.Of(item) != null ? Bake(item, side) : Drawn(item);

    /// <summary>
    /// Makes every item's silhouette now, while the game loads: one is read back from the GPU, which waits for
    /// the frames in flight, two refreshes of a 240 Hz screen, if left until a stack first lies on the floor.
    /// A tool's held picture is read back here too, for its icons.
    /// </summary>
    public static void MakeSilhouettes()
    {
        foreach (var item in Content.Items.All)
        {
            Silhouette(item.Id);
            // An empty battery lies on the floor in its own picture (0053).
            if (item.Capacity != null) Silhouette($"{item.Id}_empty");
        }
    }

    /// <summary>The picture of <paramref name="item"/> as the world draws it with every pixel drawn white, its clear ones clear.</summary>
    public static Texture2D Silhouette(string item)
    {
        if (Silhouettes.TryGetValue(item, out var kept)) return kept;
        var image = HeldLook.Of(item) is { } look ? (Image)HeldImage(look.Picture).Duplicate() : Readable(Drawn(item));
        int lowest = -1;
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
            {
                float a = image.GetPixel(x, y).A;
                if (a > 0) lowest = y;
                image.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        ClearRows[item] = image.GetHeight() - 1 - lowest;
        return Silhouettes[item] = ImageTexture.CreateFromImage(image);
    }

    /// <summary>The clear rows under <paramref name="item"/>'s picture as the world draws it, in that picture's pixels: what a stack on the floor is lowered by to stand on it.</summary>
    public static int ClearBelow(string item)
    {
        if (!ClearRows.ContainsKey(item)) Silhouette(item);
        return ClearRows[item];
    }

    private static Texture2D Drawn(string item)
    {
        if (!Icons.TryGetValue(item, out var icon))
            Icons[item] = icon = GD.Load<Texture2D>($"res://art/items/{item}.png");
        return icon;
    }

    private static Texture2D Bake(string item, int side)
    {
        if (Baked.TryGetValue((item, side), out var icon)) return icon;
        var held = HeldImage(HeldLook.Of(item)!.Picture);
        var rgba = ToolIcon.Bake(held.GetData(), held.GetWidth(), held.GetHeight(), side);
        return Baked[(item, side)] = ImageTexture.CreateFromImage(Image.CreateFromData(side, side, false, Image.Format.Rgba8, rgba));
    }

    private static Image HeldImage(string picture)
    {
        if (!Held.TryGetValue(picture, out var held)) Held[picture] = held = Readable(GD.Load<Texture2D>(picture));
        return held;
    }

    private static Image Readable(Texture2D texture)
    {
        var image = texture.GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        return image;
    }
}
