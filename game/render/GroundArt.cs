using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The pictures of a planet's natural ground made here rather than drawn (0077, 0079): the edges of a cell by its open sides,
/// in its material's lines (<see cref="GroundLooks"/>). Its walls, back walls, details and living edges are drawn, in <c>art/hd</c>;
/// a dug cell's cracks are drawn in ink (<c>BlockFault</c>).
/// </summary>
public static class GroundArt
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>
    /// How many pictures of the living edge a top has (<c>art/planet/top_{material}</c>, 0082), its sheet's first columns, a cell
    /// showing one by a hash of where it is: a cell wide and two tall, laid from the cell above; the sheet's top row drawn behind
    /// the heroes, its bottom row before them. The sheet's last four columns, drawn for slopes, are no longer read (0106).
    /// </summary>
    public const int TopVariants = 4;

    /// <summary>
    /// The edges of a cell of <paramref name="material"/> on its sides <paramref name="open"/> (up 1, right 2, down 4, left 8),
    /// catching the light (0088): a dark outline, inside it two lit lines on top and one on the other sides; transparent elsewhere;
    /// <paramref name="cell"/> pixels square, a 16 px one for a build card (0104).
    /// </summary>
    public static Image Edge(string material, int open, int cell = Cell)
    {
        var look = GroundLooks.Of[material];
        var image = Image.CreateEmpty(cell, cell, false, Image.Format.Rgba8);
        for (int y = 0; y < cell; y++)
            for (int x = 0; x < cell; x++)
            {
                int up = (open & 1) != 0 ? y : cell, right = (open & 2) != 0 ? cell - 1 - x : cell;
                int down = (open & 4) != 0 ? cell - 1 - y : cell, left = (open & 8) != 0 ? x : cell;
                int nearest = System.Math.Min(System.Math.Min(up, right), System.Math.Min(down, left));
                if (nearest == 0) image.SetPixel(x, y, Colour(look.Outline));
                else if (up == 1) image.SetPixel(x, y, Colour(look.Lit, GroundLooks.RimTop));
                else if (up == 2) image.SetPixel(x, y, Colour(look.Lit, GroundLooks.RimUnder));
                else if (nearest == 1) image.SetPixel(x, y, Colour(look.Lit, GroundLooks.RimSide));
            }
        return image;
    }

    private static Color Colour((int R, int G, int B) c, double by = 1) =>
        Color.Color8(Byte(c.R * by), Byte(c.G * by), Byte(c.B * by));

    private static byte Byte(double v) => (byte)System.Math.Clamp((int)System.Math.Round(v), 0, 255);
}
