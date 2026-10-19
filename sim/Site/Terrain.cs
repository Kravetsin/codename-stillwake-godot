using Stillwake.Content;

namespace Stillwake.Sim.Site;

/// <summary>
/// SPIKE (planet): a site's ground made from its seed, integers and the world's Rng only, so every peer makes
/// the same: hills of soil over rock, worm caves through the rock, pockets of iron ore. Sky is open space.
/// </summary>
public static class Terrain
{
    /// <summary>The site's map, and the cell where heroes beam down: on the surface in its middle.</summary>
    public static (Layout Layout, int Spawn) Make(uint seed, int width = Content.Site.Width, int height = Content.Site.Height)
    {
        var rng = new Rng(new RngState { S = seed });
        var cells = new char[height][];
        for (int y = 0; y < height; y++) cells[y] = new string(' ', width).ToCharArray();
        var surface = Noise(rng, width, [(64, Content.Site.Hills), (32, Content.Site.Hills / 2), (16, Content.Site.Hills / 4), (8, Content.Site.Hills / 8)]);
        var soil = Noise(rng, width, [(16, Content.Site.SoilVaries / 2)]);
        for (int x = 0; x < width; x++)
        {
            surface[x] = Math.Clamp(Content.Site.Surface + surface[x], 16, height - 40);
            soil[x] = Content.Site.Soil + Content.Site.SoilVaries / 2 + soil[x];
            for (int y = surface[x]; y < height; y++) cells[y][x] = y < surface[x] + soil[x] ? '%' : '&';
        }
        // Ore first, then caves through it, so a cave may open onto a vein.
        for (int i = 0; i < Content.Site.Ores; i++)
        {
            int x = (int)rng.Below((uint)width);
            int top = surface[x] + soil[x] + 4;
            if (top >= height - 3) continue;
            int y = top + (int)rng.Below((uint)(height - 3 - top));
            Disc(cells, x, y, 1 + (int)rng.Below(Content.Site.OreRadius), c => c == '&' ? '*' : c);
        }
        for (int i = 0; i < Content.Site.Caves; i++)
        {
            int x = (int)rng.Below((uint)width);
            // One worm in four starts at the surface: a cave mouth.
            int y = rng.Below(4) == 0 ? surface[x] : surface[x] + soil[x] + 2 + (int)rng.Below((uint)Math.Max(1, height - 8 - surface[x] - soil[x]));
            int dx = rng.Below(2) == 0 ? -1 : 1, dy = 1, r = 2;
            for (int step = 0; step < Content.Site.CaveSteps; step++)
            {
                if (step % 8 == 0) r = 1 + (int)rng.Below(Content.Site.CaveRadius);
                Disc(cells, x, y, r, c => c is '%' or '&' or '*' ? ',' : c);
                uint turn = rng.Below(10);
                if (turn == 0) dx = -dx;
                else if (turn < 3) dy = Math.Clamp(dy + (turn == 1 ? 1 : -1), -1, 1);
                x = Math.Clamp(x + dx, 2, width - 3);
                y = Math.Clamp(y + dy, surface[x] + 1, height - 5);
            }
        }
        int mid = width / 2;
        return (Layout.Parse(cells.Select(r => new string(r)).ToArray()), (surface[mid] - 1) * width + mid);
    }

    /// <summary>Value noise: each octave a point every <c>Every</c> columns, drawn in ±<c>Amp</c>, lines between; summed.</summary>
    private static int[] Noise(Rng rng, int width, (int Every, int Amp)[] octaves)
    {
        var sum = new int[width];
        foreach (var (every, amp) in octaves)
        {
            var points = new int[width / every + 2];
            for (int i = 0; i < points.Length; i++) points[i] = (int)rng.Below((uint)(2 * amp + 1)) - amp;
            // Rounded toward zero, as C# divides: the same on every machine.
            for (int x = 0; x < width; x++)
            {
                int i = x / every, along = x % every;
                sum[x] += points[i] + (points[i + 1] - points[i]) * along / every;
            }
        }
        return sum;
    }

    private static void Disc(char[][] cells, int cx, int cy, int r, Func<char, char> change)
    {
        int height = cells.Length, width = cells[0].Length;
        for (int y = Math.Max(0, cy - r); y <= Math.Min(height - 3, cy + r); y++)
            for (int x = Math.Max(0, cx - r); x <= Math.Min(width - 1, cx + r); x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) cells[y][x] = change(cells[y][x]);
    }
}
