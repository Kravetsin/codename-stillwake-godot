using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// Each item's picture (render/item-art.ts, exported to <c>art/items/*.png</c>), sixteen pixels square:
/// in slots, in the hand, on the floor; and its silhouette in white, the pale rim a stack on the floor
/// stands out by. A tool, an item held as a picture, has none drawn: its icon is its held picture baked
/// turned and fitted to the side it is drawn at (<see cref="ToolIcon"/>, 2026-10-27-tool-icons).
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
    /// The picture of <paramref name="item"/> as the world draws it, loaded once: for a tool its icon
    /// <see cref="HeldLook.IconWorldSide"/> wide with mipmaps, to be filtered at the camera's zoom.
    /// </summary>
    public static Texture2D Icon(string item) => HeldLook.Of(item) != null ? Bake(item, HeldLook.IconWorldSide) : Drawn(item);

    /// <summary>
    /// How the world draws <paramref name="item"/>'s <see cref="Icon(string)"/>: a tool's, larger than its pixels there, filtered
    /// through its mipmaps at any zoom and turn; any other's as its node's parent, pixel for pixel.
    /// </summary>
    public static CanvasItem.TextureFilterEnum WorldFilter(string item) =>
        HeldLook.Of(item) != null ? CanvasItem.TextureFilterEnum.LinearWithMipmaps : CanvasItem.TextureFilterEnum.ParentNode;

    /// <summary>The picture of <paramref name="item"/> where it is drawn <paramref name="side"/> px wide: a tool's icon baked at that side, drawn 1:1; any other item's 16 px picture.</summary>
    public static Texture2D Icon(string item, int side) => HeldLook.Of(item) != null ? Bake(item, side) : Drawn(item);

    /// <summary>
    /// Makes every item's silhouette now, while the game loads: one is read back from the GPU, which waits for
    /// the frames in flight, two refreshes of a 240 Hz screen, if left until a stack first lies on the floor.
    /// A tool's held picture is read back here too, for its icons, and its icon for the world baked.
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
        bool tool = HeldLook.Of(item) != null;
        var image = tool ? BakedImage(item, HeldLook.IconWorldSide) : Readable(Drawn(item));
        if (tool && !Baked.ContainsKey((item, HeldLook.IconWorldSide))) Keep(item, HeldLook.IconWorldSide, (Image)image.Duplicate());
        int lowest = -1;
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
            {
                float a = image.GetPixel(x, y).A;
                if (a > 0) lowest = y;
                image.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        ClearRows[item] = image.GetHeight() - 1 - lowest;
        if (tool) image.GenerateMipmaps();
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

    private static Texture2D Bake(string item, int side) =>
        Baked.TryGetValue((item, side), out var icon) ? icon : Keep(item, side, BakedImage(item, side));

    // Only the world's side is mipmapped: every other is drawn 1:1.
    private static Texture2D Keep(string item, int side, Image image)
    {
        if (side == HeldLook.IconWorldSide) image.GenerateMipmaps();
        return Baked[(item, side)] = ImageTexture.CreateFromImage(image);
    }

    private static Image BakedImage(string item, int side)
    {
        var picture = HeldLook.Of(item)!.Picture;
        if (!Held.TryGetValue(picture, out var held)) Held[picture] = held = Readable(GD.Load<Texture2D>(picture));
        var rgba = ToolIcon.Bake(held.GetData(), held.GetWidth(), held.GetHeight(), side);
        return Image.CreateFromData(side, side, false, Image.Format.Rgba8, rgba);
    }

    private static Image Readable(Texture2D texture)
    {
        var image = texture.GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        return image;
    }
}
