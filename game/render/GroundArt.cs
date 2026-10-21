using Godot;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>
/// The pictures of a planet's natural ground made here rather than drawn (0077, 0079): the edges of a cell by its open sides,
/// in its material's lines (<see cref="GroundLooks"/>), and the cracks a dig leaves, cut to the cell's shape. Its walls, back
/// walls, details and living edges are drawn, in <c>art/planet</c>. Every pixel is a hash of where it is: the same on every machine and run.
/// </summary>
public static class GroundArt
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>
    /// The tops a living edge is drawn for (<c>art/planet/top_{material}</c>, 0082): whole, and the two floor slopes. Its pictures
    /// are a cell wide and two tall, laid from the cell above; the sheet's top row drawn behind the heroes, its bottom row before them.
    /// </summary>
    public static readonly int[] TopShapes = [Shape.None, Shape.BottomRight, Shape.BottomLeft];

    /// <summary>How many pictures of the living edge a top of <paramref name="shape"/> has, a cell showing one by a hash of where it is.</summary>
    public static int TopVariants(int shape) => shape == Shape.None ? 4 : 2;

    /// <summary>The column of the sheet holding variant <paramref name="variant"/> of a top of <paramref name="shape"/>: four flat, two ◢, two ◣.</summary>
    public static int TopColumn(int shape, int variant) => shape switch { Shape.BottomRight => 4 + variant, Shape.BottomLeft => 6 + variant, _ => variant };

    /// <summary>
    /// The edges of a cell of <paramref name="material"/> on its sides <paramref name="open"/> (up 1, right 2, down 4, left 8),
    /// catching the light (0088): a dark outline, inside it two lit lines on top and one on the other sides; transparent elsewhere.
    /// </summary>
    public static Image Edge(string material, int open)
    {
        var look = GroundLooks.Of[material];
        var image = Image.CreateEmpty(Cell, Cell, false, Image.Format.Rgba8);
        for (int y = 0; y < Cell; y++)
            for (int x = 0; x < Cell; x++)
            {
                int up = (open & 1) != 0 ? y : Cell, right = (open & 2) != 0 ? Cell - 1 - x : Cell;
                int down = (open & 4) != 0 ? Cell - 1 - y : Cell, left = (open & 8) != 0 ? x : Cell;
                int nearest = System.Math.Min(System.Math.Min(up, right), System.Math.Min(down, left));
                if (nearest == 0) image.SetPixel(x, y, Colour(look.Outline));
                else if (up == 1) image.SetPixel(x, y, Colour(look.Lit, GroundLooks.RimTop));
                else if (up == 2) image.SetPixel(x, y, Colour(look.Lit, GroundLooks.RimUnder));
                else if (nearest == 1) image.SetPixel(x, y, Colour(look.Lit, GroundLooks.RimSide));
            }
        return image;
    }

    /// <summary>
    /// The lines along the diagonal of a <paramref name="material"/> wall cut to <paramref name="shape"/>, from the diagonal in:
    /// its outline, then a floor slope's two lit lines as a top's (0088), a ceiling slope's one.
    /// </summary>
    public static Color[] Diagonal(string material, int shape)
    {
        var look = GroundLooks.Of[material];
        return Shape.IsFloor(shape)
            ? [Colour(look.Outline), Colour(look.Lit, GroundLooks.RimTop), Colour(look.Lit, GroundLooks.RimUnder)]
            : [Colour(look.Outline), Colour(look.Lit)];
    }

    /// <summary>
    /// A natural back wall's <paramref name="picture"/> receding behind its ground (0088): paler, cooler and darker, by
    /// <see cref="GroundLooks.BackPale"/>, <see cref="GroundLooks.BackCool"/> and <see cref="GroundLooks.BackDim"/>.
    /// </summary>
    public static Image Recede(Image picture)
    {
        var (cr, cg, cb) = GroundLooks.CoolShift;
        double dim = GroundLooks.BackDim, pale = GroundLooks.BackPale, cool = GroundLooks.BackCool;
        double kr = (1 + cr * cool) * dim, kg = (1 + cg * cool) * dim, kb = (1 + cb * cool) * dim;
        // RGBA8 bytes: a pixel at a time through Godot costs a call each.
        byte[] data = picture.GetData();
        for (int i = 0; i < data.Length; i += 4)
        {
            double r = data[i], g = data[i + 1], b = data[i + 2], grey = 0.3 * r + 0.59 * g + 0.11 * b;
            data[i] = Byte((r + (grey - r) * pale) * kr);
            data[i + 1] = Byte((g + (grey - g) * pale) * kg);
            data[i + 2] = Byte((b + (grey - b) * pale) * kb);
        }
        return Image.CreateFromData(picture.GetWidth(), picture.GetHeight(), false, Image.Format.Rgba8, data);
    }

    /// <summary>
    /// The cracks a dug cell cut to <paramref name="shape"/> shows at <paramref name="stage"/>, 0 to 2 (0077): more lines,
    /// longer, from a dent in its middle; on a slope only over its solid half (0079), its middle moved into it.
    /// </summary>
    public static Image Crack(int stage, int shape = Shape.None)
    {
        var image = Image.CreateEmpty(Cell, Cell, false, Image.Format.Rgba8);
        var dark = Color.Color8(18, 14, 12, 230);
        var lit = Color.Color8(255, 240, 220, 70);
        // A slope's cracks start from the middle of its solid half, a quarter of a cell into it from the cell's middle.
        int mx = Cell / 2 - 1 + shape switch { Shape.BottomRight or Shape.TopRight => 3, Shape.BottomLeft or Shape.TopLeft => -3, _ => 0 };
        int my = Cell / 2 - 1 + shape switch { Shape.BottomRight or Shape.BottomLeft => 3, Shape.TopLeft or Shape.TopRight => -3, _ => 0 };
        int lines = 3 + 2 * stage, steps = 5 + 2 * stage;
        for (int i = 0; i < lines; i++)
        {
            // Each line runs out from near the middle, a step at a time, turning a little at random: a stage grows the last.
            int x = mx + (int)(Hash(i, 0, 4) % 3), y = my + (int)(Hash(i, 0, 5) % 3);
            int dir = (int)(Hash(i, 0, 6) % 8);
            for (int s = 0; s < steps; s++)
            {
                if (x < 0 || y < 0 || x >= Cell || y >= Cell) break;
                image.SetPixel(x, y, dark);
                if (y + 1 < Cell && image.GetPixel(x, y + 1).A == 0) image.SetPixel(x, y + 1, lit);
                dir = (dir + (int)(Hash(i, s, 7) % 3) + 7) % 8;
                x += dir is 1 or 2 or 3 ? 1 : dir is 5 or 6 or 7 ? -1 : 0;
                y += dir is 3 or 4 or 5 ? 1 : dir is 7 or 0 or 1 ? -1 : 0;
            }
        }
        // The dent the blows land in, wider each stage.
        for (int y = my - stage / 2; y <= my + 1 + stage / 2; y++)
            for (int x = mx - stage / 2; x <= mx + 1 + (stage + 1) / 2; x++)
                image.SetPixel(x, y, dark);
        return shape == Shape.None ? image : ShapeMasks.Cut(image, shape);
    }

    private static Color Colour((int R, int G, int B) c, double by = 1) =>
        Color.Color8(Byte(c.R * by), Byte(c.G * by), Byte(c.B * by));

    private static byte Byte(double v) => (byte)System.Math.Clamp((int)System.Math.Round(v), 0, 255);

    private static uint Hash(int x, int y, uint salt)
    {
        unchecked
        {
            uint h = (uint)x * 0x85EBCA77u ^ (uint)y * 0xC2B2AE3Du ^ salt * 0x27D4EB2Fu;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            return h;
        }
    }
}
