using System.Collections.Generic;
using Godot;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>
/// A wall's shape in pixels (0069): which of a cell's sixteen by sixteen pixels its solid half covers, the
/// sim's <see cref="Shape.Solid"/> read at each pixel's middle, so the drawing cuts where the hero stands.
/// The existing wall pictures are cut by it, no picture per shape; their diagonal edged in the colours of
/// the material's own edge.
/// </summary>
public static class ShapeMasks
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>Whether pixel (<paramref name="px"/>, <paramref name="py"/>) of a cell lies in the solid of <paramref name="shape"/>.</summary>
    public static bool Covers(int shape, int px, int py) =>
        Shape.Solid(shape, (2 * px + 1) * Shape.Unit / (2 * Cell), (2 * py + 1) * Shape.Unit / (2 * Cell));

    /// <summary>How far a covered pixel is in from the diagonal, 0 on it; past the cell's size for a whole wall.</summary>
    private static int Depth(int shape, int px, int py) => shape switch
    {
        Shape.BottomRight => px + py - (Cell - 1),
        Shape.BottomLeft => py - px,
        Shape.TopLeft => Cell - 1 - px - py,
        Shape.TopRight => px - py,
        _ => Cell,
    };

    /// <summary>
    /// <paramref name="picture"/>, any number of cells, with every cell cut to <paramref name="shape"/>; the two
    /// pixels along its diagonal take <paramref name="edge"/>'s outer and inner colour, as a side open to the room
    /// is drawn. Without an edge, cut only.
    /// </summary>
    public static Image Cut(Image picture, int shape, Image? edge = null)
    {
        var cut = (Image)picture.Duplicate();
        // The edge picture of a cell open above: its first two rows are a side's outer and inner line.
        Color? outer = edge?.GetPixel(Cell / 2, 0), inner = edge?.GetPixel(Cell / 2, 1);
        for (int y = 0; y < cut.GetHeight(); y++)
            for (int x = 0; x < cut.GetWidth(); x++)
            {
                int px = x % Cell, py = y % Cell;
                if (!Covers(shape, px, py)) cut.SetPixel(x, y, Colors.Transparent);
                else if (Depth(shape, px, py) == 0 && outer is Color o && o.A > 0) cut.SetPixel(x, y, o);
                else if (Depth(shape, px, py) == 1 && inner is Color i && i.A > 0) cut.SetPixel(x, y, i);
            }
        return cut;
    }

    /// <summary>
    /// <paramref name="picture"/>, any number of cells, each cut to the band of <paramref name="depth"/> pixels under its top: a
    /// floor slope's diagonal (<paramref name="shape"/>) or a whole wall's top row. With <paramref name="edge"/>, its outline colour
    /// along the band's foot, as a kick-plate on the hull. <paramref name="below"/>: a whole wall under a floor slope of
    /// <paramref name="shape"/>, cut to where the slope's band runs on into it.
    /// </summary>
    public static Image Lip(Image picture, int shape, int depth, Image? edge = null, bool below = false)
    {
        var lip = (Image)picture.Duplicate();
        Color? foot = edge?.GetPixel(Cell / 2, 0);
        for (int y = 0; y < lip.GetHeight(); y++)
            for (int x = 0; x < lip.GetWidth(); x++)
            {
                int px = x % Cell, py = y % Cell, under = shape == Shape.None ? py : Depth(shape, px, below ? py + Cell : py);
                if (!below && !Covers(shape, px, py) || under >= depth) lip.SetPixel(x, y, Colors.Transparent);
                else if (under == depth - 1 && foot is Color o && o.A > 0) lip.SetPixel(x, y, o);
            }
        return lip;
    }

    private static readonly Dictionary<int, Texture2D> Masks = new();

    /// <summary>A cell's picture of <paramref name="shape"/>'s solid half, white: what the aim outline traces round a shaped wall.</summary>
    public static Texture2D Texture(int shape)
    {
        if (Masks.TryGetValue(shape, out var mask)) return mask;
        var image = Image.CreateEmpty(Cell, Cell, false, Image.Format.Rgba8);
        for (int y = 0; y < Cell; y++)
            for (int x = 0; x < Cell; x++)
                image.SetPixel(x, y, Covers(shape, x, y) ? Colors.White : Colors.Transparent);
        return Masks[shape] = ImageTexture.CreateFromImage(image);
    }
}
