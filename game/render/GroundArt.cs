using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The pictures of a planet's natural ground (0077), made here rather than drawn, placeholders till its art goes
/// through an audition page: a wall's 8 × 4 cell tile speckled in its material's colour (<see cref="GroundLooks"/>), the
/// edges of a cell by its open sides, and a darker, flatter back wall. Every pixel is a hash of where it is, so a
/// picture is the same on every machine and every run.
/// </summary>
public static class GroundArt
{
    private const int Cell = ShipCamera.Cell, Across = 8, Down = 4;

    /// <summary>A wall of <paramref name="material"/>, the whole tile, as <c>wall_{material}.png</c> would be.</summary>
    public static Image Wall(string material) => Tile(GroundLooks.Of[material], back: false);

    /// <summary>The back wall behind <paramref name="material"/>, cut from a tile as a scrap back wall is.</summary>
    public static Image Back(string material) => Tile(GroundLooks.Of[material], back: true);

    /// <summary>
    /// The edges of a cell of <paramref name="material"/> on its sides <paramref name="open"/> (up 1, right 2, down 4, left 8):
    /// a dark outer line, and inside it a line lit on top and shaded on the other sides; transparent elsewhere.
    /// </summary>
    public static Image Edge(string material, int open)
    {
        var look = GroundLooks.Of[material];
        var image = Image.CreateEmpty(Cell, Cell, false, Image.Format.Rgba8);
        var outer = Shade(look.Colour, 0.32);
        for (int y = 0; y < Cell; y++)
            for (int x = 0; x < Cell; x++)
            {
                int up = (open & 1) != 0 ? y : Cell, right = (open & 2) != 0 ? Cell - 1 - x : Cell;
                int down = (open & 4) != 0 ? Cell - 1 - y : Cell, left = (open & 8) != 0 ? x : Cell;
                int nearest = System.Math.Min(System.Math.Min(up, right), System.Math.Min(down, left));
                if (nearest == 0) image.SetPixel(x, y, outer);
                else if (nearest == 1) image.SetPixel(x, y, Shade(look.Colour, up == 1 ? 1.3 : 0.75));
            }
        return image;
    }

    /// <summary>The cracks a dug cell shows at <paramref name="stage"/>, 0 to 2 (0077): more lines, longer, from a dent in its middle.</summary>
    public static Image Crack(int stage)
    {
        var image = Image.CreateEmpty(Cell, Cell, false, Image.Format.Rgba8);
        var dark = Color.Color8(18, 14, 12, 230);
        var lit = Color.Color8(255, 240, 220, 70);
        int lines = 3 + 2 * stage, steps = 5 + 2 * stage;
        for (int i = 0; i < lines; i++)
        {
            // Each line runs out from near the middle, a step at a time, turning a little at random: a stage grows the last.
            int x = Cell / 2 - 1 + (int)(Hash(i, 0, 4) % 3), y = Cell / 2 - 1 + (int)(Hash(i, 0, 5) % 3);
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
        for (int y = Cell / 2 - 1 - stage / 2; y <= Cell / 2 + stage / 2; y++)
            for (int x = Cell / 2 - 1 - stage / 2; x <= Cell / 2 + (stage + 1) / 2; x++)
                image.SetPixel(x, y, dark);
        return image;
    }

    private static Image Tile(GroundLooks.Look look, bool back)
    {
        int w = Across * Cell, h = Down * Cell;
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Blotches four pixels across, then each pixel's own stray, and now and then a fleck or a dark grain.
                int blotch = (int)(Hash(x / 4, y / 4, 1) % 7) - 3;
                int stray = (int)(Hash(x, y, 2) % (uint)(2 * look.Stray + 1)) - look.Stray + blotch * look.Stray / 3;
                uint roll = Hash(x, y, 3) % 100;
                var c = roll < 4 ? look.Fleck : look.Colour;
                double shade = roll >= 92 ? 0.7 : 1;
                if (back) (stray, shade) = (stray / 2, shade * look.Back);
                image.SetPixel(x, y, Shade((c.R + stray, c.G + stray, c.B + stray), shade));
            }
        return image;
    }

    private static Color Shade((int R, int G, int B) c, double by) =>
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
