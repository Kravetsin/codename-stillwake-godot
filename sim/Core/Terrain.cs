using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>
/// A planet's ground grown from its seed (0077): a pure function of a <see cref="Ground"/> and the seed, filling a
/// <see cref="Layout"/> directly with the legend's characters and each cell's material in <see cref="Layout.Materials"/>.
/// Integer value noise only, an unchecked hash of the seed and the cell, weights never negative, so every peer grows the
/// same ground. It runs when the surface opens and again when a save of it loads, which holds only what was changed
/// since (Grid's edits), so it is worked out a row at a time: each noise's lattice blended down once a row, then along.
/// </summary>
public static class Terrain
{
    /// <summary>Noise values run from 0 below <see cref="Whole"/>; weights from 0 to it.</summary>
    private const int Whole = 65536;

    /// <summary>Each noise and draw of a ground hashed apart from the others.</summary>
    private const uint Ranges = 1, Hills = 2, Bumps = 3, Soil = 4, Mouths = 5, Tunnels = 6, TunnelsFine = 7, Caverns = 8,
        CavernsFine = 9, ChasmAt = 10, ChasmWander = 11, HallAt = 12;

    public static Layout Generate(Ground g, uint seed)
    {
        int w = g.Width, h = g.Height, floor = h - g.BedrockRows;
        int soil = Materials.Get("soil").Code, stone = Materials.Get("stone").Code, bedrock = Materials.Get("bedrock").Code;
        var codes = new int[w * h];
        // 0 sky, 1 solid, 2 open below the surface.
        var kind = new byte[w * h];
        var top = new int[w];
        var soilDepth = new int[w];
        var mouth = new int[w];
        var caveFrom = new int[w];
        for (int x = 0; x < w; x++)
        {
            int range = Swell(Noise(seed, Ranges, x, g.RangeWave), g.RangeRise);
            // Terraced: the range rises in steps, so its sides are cliffs.
            if (g.CliffStep > 1) range = (range >= 0 ? range : range - g.CliffStep + 1) / g.CliffStep * g.CliffStep;
            int rise = range + Swell(Noise(seed, Hills, x, g.HillWave), g.HillRise) + Swell(Noise(seed, Bumps, x, g.BumpWave), g.BumpRise);
            top[x] = Math.Clamp(g.Surface + rise, 1, floor - 1);
            soilDepth[x] = g.SoilLeast + (int)((long)Noise(seed, Soil, x, g.SoilWave) * (g.SoilMost - g.SoilLeast + 1) / Whole);
            // Where the mouths' noise passes its mark caves begin nearer the surface and open as if deeper, the more the
            // further past: at the surface itself from halfway past, as open as at the bedrock at its peak.
            mouth[x] = (int)((long)Math.Max(0, Noise(seed, Mouths, x, g.MouthWave) - g.MouthOver) * Whole / (Whole - g.MouthOver));
            caveFrom[x] = Math.Max(0, g.CaveFrom - (int)((long)mouth[x] * 2 * (g.CaveFrom + 1) / Whole));
        }

        var tunnels = new Field(seed, Tunnels, g.TunnelWave, g.TunnelTall, w);
        var tunnelsFine = new Field(seed, TunnelsFine, Math.Max(1, g.TunnelWave / 3), Math.Max(1, g.TunnelTall / 3), w);
        var caverns = new Field(seed, Caverns, g.CavernWave, g.CavernWave, w);
        var cavernsFine = new Field(seed, CavernsFine, Math.Max(1, g.CavernWave / 2), Math.Max(1, g.CavernWave / 2), w);
        var (t1, t2, c1, c2) = (new int[w], new int[w], new int[w], new int[w]);
        // By depth, how far down from where caves begin to the bedrock (0 to Whole), and the tunnels' width and the caverns'
        // mark there: worked out once, not a cell at a time.
        int span = Math.Max(1, h - g.BedrockRows - g.Surface - g.CaveFrom);
        var deeperAt = new int[h];
        var widthAt = new int[h];
        var markAt = new int[h];
        for (int d = 0; d < h; d++)
        {
            deeperAt[d] = (int)Math.Clamp((long)(d - g.CaveFrom) * Whole / span, 0, Whole);
            (widthAt[d], markAt[d]) = (Between(g.TunnelTop, g.TunnelBottom, deeperAt[d]), Between(g.CavernTop, g.CavernBottom, deeperAt[d]));
        }
        for (int y = 0; y < h; y++)
        {
            tunnels.Row(y, t1);
            tunnelsFine.Row(y, t2);
            caverns.Row(y, c1);
            cavernsFine.Row(y, c2);
            for (int x = 0; x < w; x++)
            {
                int cell = y * w + x;
                if (x < g.BedrockSide || x >= w - g.BedrockSide || y >= floor)
                {
                    (kind[cell], codes[cell]) = (1, bedrock);
                    continue;
                }
                if (y < top[x]) continue;
                int depth = y - top[x];
                codes[cell] = depth < soilDepth[x] ? soil : stone;
                kind[cell] = 1;
                if (depth < caveFrom[x]) continue;
                // A mouth opens as if deeper.
                int width = widthAt[depth], mark = markAt[depth];
                if (mouth[x] > deeperAt[depth]) (width, mark) = (Between(g.TunnelTop, g.TunnelBottom, mouth[x]), Between(g.CavernTop, g.CavernBottom, mouth[x]));
                int tunnel = (3 * t1[x] + t2[x]) >> 2, cavern = (2 * c1[x] + c2[x]) / 3;
                if (Math.Abs(tunnel - Whole / 2) < width || cavern > mark) kind[cell] = 2;
            }
        }
        Landmarks(g, seed, top, kind, caverns);

        var rows = new char[h][];
        for (int y = 0; y < h; y++)
        {
            var row = rows[y] = new char[w];
            for (int x = 0; x < w; x++) row[x] = kind[y * w + x] switch { 1 => '#', 2 => '.', _ => ' ' };
        }
        // A cell of soil or stone open above, with one side open and the other solid, is cut to a slope rising toward the solid side.
        for (int y = 1; y < h; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int cell = y * w + x;
                if (kind[cell] != 1 || codes[cell] == bedrock || kind[cell - w] == 1) continue;
                bool left = kind[cell - 1] == 1, right = kind[cell + 1] == 1;
                if (right && !left) rows[y][x] = '/';
                else if (left && !right) rows[y][x] = '\\';
            }
        var spawn = SpawnOf(rows, w, h);
        rows[spawn / w][spawn % w] = 'S';
        return new Layout(w, h, [.. rows.Select(r => new string(r))], Materials: codes) { Grown = true };
    }

    /// <summary>
    /// The landmarks, at places the seed chooses, none within <see cref="Ground.Clear"/> of the middle: chasms cut from the
    /// surface down, wandering a little, and great halls with ragged walls. Both are open ground, back-walled, as caves are.
    /// </summary>
    private static void Landmarks(Ground g, uint seed, int[] top, byte[] kind, Field ragged)
    {
        int w = g.Width, h = g.Height, floor = h - g.BedrockRows;
        for (int k = 0; k < g.Chasms; k++)
        {
            int cx = Column(g, seed, ChasmAt, k, g.Chasms, g.BedrockSide + g.ChasmWide + 3);
            int deep = g.ChasmDeep / 2 + (int)((long)Lattice(seed, ChasmAt, k, 1) * (g.ChasmDeep / 2 + 1) / Whole);
            int wide = Math.Max(2, g.ChasmWide / 2 + (int)((long)Lattice(seed, ChasmAt, k, 2) * (g.ChasmWide / 2 + 1) / Whole));
            int bottom = Math.Min(floor - 1, top[cx] + deep);
            for (int y = 0; y <= bottom; y++)
            {
                // It wanders a cell or two either way as it goes down, and narrows toward its foot.
                int at = cx + Swell(Noise(seed + (uint)k, ChasmWander, y, 9), 2);
                int across = Math.Max(1, wide * (bottom - y + deep / 2) / (deep + deep / 2));
                for (int x = Math.Max(g.BedrockSide, at - across / 2); x < Math.Min(w - g.BedrockSide, at - across / 2 + across); x++)
                    if (y >= top[x]) kind[y * w + x] = 2;
            }
        }
        var row = new int[w];
        for (int k = 0; k < g.Halls; k++)
        {
            int hx = Column(g, seed, HallAt, k, g.Halls, g.BedrockSide + g.HallWide + 1);
            int rx = g.HallWide / 2 + (int)((long)Lattice(seed, HallAt, k, 1) * (g.HallWide / 2 + 1) / Whole);
            int ry = g.HallTall / 2 + (int)((long)Lattice(seed, HallAt, k, 2) * (g.HallTall / 2 + 1) / Whole);
            int highest = top[hx] + g.HallFrom, lowest = floor - 2 - ry;
            if (lowest < highest) continue;
            int hy = highest + (int)((long)Lattice(seed, HallAt, k, 3) * (lowest - highest + 1) / Whole);
            long rx2 = (long)rx * rx, ry2 = (long)ry * ry;
            int reachY = ry + ry / 4 + 1, reachX = rx + rx / 4 + 1;
            for (int y = Math.Max(0, hy - reachY); y <= Math.Min(floor - 1, hy + reachY); y++)
            {
                ragged.Row(y, row);
                for (int x = Math.Max(g.BedrockSide, hx - reachX); x <= Math.Min(w - 1 - g.BedrockSide, hx + reachX); x++)
                {
                    long dx = x - hx, dy = y - hy;
                    // Inside the ellipse stretched by the noise, from three quarters of it to five quarters.
                    long within = rx2 * ry2 * (3 * Whole / 4 + row[x] / 2) / Whole;
                    if (dx * dx * ry2 + dy * dy * rx2 <= within && y >= top[x]) kind[y * w + x] = 2;
                }
            }
        }
    }

    /// <summary>
    /// The column the seed chooses for landmark <paramref name="k"/> of <paramref name="count"/> of <paramref name="which"/>:
    /// <paramref name="margin"/> in from either side, out of the middle's clear columns, each in a band of its own so they spread.
    /// </summary>
    private static int Column(Ground g, uint seed, uint which, int k, int count, int margin)
    {
        int middle = g.Width / 2, left = Math.Max(0, middle - g.Clear - margin), right = Math.Max(0, g.Width - margin - middle - g.Clear);
        int band = Math.Max(1, (left + right) / Math.Max(1, count));
        int at = k * band + (int)((long)Lattice(seed, which, k, 0) * band / Whole);
        return at < left ? margin + at : Math.Min(g.Width - margin - 1, middle + g.Clear + (at - left));
    }

    /// <summary>
    /// Where heroes beam down: on the flat column nearest the middle, the ground whole under it and beside it level,
    /// the two cells above it open; to the left of the middle first at a tie. The middle's column when none is flat.
    /// </summary>
    private static int SpawnOf(char[][] rows, int w, int h)
    {
        int TopOf(int x)
        {
            for (int y = 0; y < h; y++)
                if (rows[y][x] != ' ') return y;
            return h;
        }
        bool Flat(int x)
        {
            if (x < 1 || x >= w - 1) return false;
            int y = TopOf(x);
            return y >= 3 && y < h && rows[y][x] == '#' && TopOf(x - 1) == y && TopOf(x + 1) == y;
        }
        int middle = w / 2;
        for (int d = 0; d < w; d++)
        {
            if (Flat(middle - d)) return (TopOf(middle - d) - 1) * w + middle - d;
            if (Flat(middle + d)) return (TopOf(middle + d) - 1) * w + middle + d;
        }
        return Math.Max(0, TopOf(middle) - 1) * w + middle;
    }

    /// <summary><paramref name="from"/> to <paramref name="to"/> by <paramref name="t"/> of <see cref="Whole"/>, rounded toward <paramref name="from"/>.</summary>
    private static int Between(int from, int to, int t) => from + (int)((long)(to - from) * t / Whole);

    /// <summary>A noise value as a rise from -<paramref name="most"/> to <paramref name="most"/>.</summary>
    private static int Swell(int noise, int most) => (int)((long)noise * (2 * most + 1) / Whole) - most;

    /// <summary>1D value noise along <paramref name="x"/>, a lattice point every <paramref name="wave"/> cells, eased between.</summary>
    private static int Noise(uint seed, uint which, int x, int wave)
    {
        int i = x / wave, t = Ease(x % wave, wave);
        return Mix(Lattice(seed, which, i, 0), Lattice(seed, which, i + 1, 0), t);
    }

    /// <summary>
    /// 2D value noise a row at a time, a lattice point every <c>across</c> columns and <c>down</c> rows, eased between:
    /// each lattice column blended down to the row once, then each cell along it.
    /// </summary>
    private sealed class Field
    {
        private readonly uint _seed, _which;
        private readonly int _down;
        private readonly int[] _lattice, _ease, _column;

        public Field(uint seed, uint which, int across, int down, int width)
        {
            (_seed, _which, _down) = (seed, which, down);
            _lattice = new int[width];
            _ease = new int[width];
            for (int x = 0; x < width; x++) (_lattice[x], _ease[x]) = (x / across, Ease(x % across, across));
            _column = new int[(width - 1) / across + 2];
        }

        public void Row(int y, int[] values)
        {
            int j = y / _down, t = Ease(y % _down, _down);
            for (int i = 0; i < _column.Length; i++) _column[i] = Mix(Lattice(_seed, _which, i, j), Lattice(_seed, _which, i, j + 1), t);
            for (int x = 0; x < values.Length; x++)
            {
                int i = _lattice[x];
                values[x] = Mix(_column[i], _column[i + 1], _ease[x]);
            }
        }
    }

    /// <summary>a and b weighed by <paramref name="t"/> of <see cref="Whole"/>: both weights non-negative, rounded down.</summary>
    private static int Mix(int a, int b, int t) => (int)(((long)a * (Whole - t) + (long)b * t) >> 16);

    /// <summary>The smoothstep of <paramref name="at"/> of <paramref name="span"/>, 0 to <see cref="Whole"/>.</summary>
    private static int Ease(int at, int span)
    {
        long u = (long)at * Whole / span;
        return (int)(u * u * (3L * Whole - 2 * u) / ((long)Whole * Whole));
    }

    /// <summary>The noise at a lattice point: the top 16 bits of a hash of the seed, the noise and the point.</summary>
    private static int Lattice(uint seed, uint which, int i, int j)
    {
        unchecked
        {
            uint h = seed * 0x9E3779B1u ^ which * 0x85EBCA77u ^ (uint)i * 0xC2B2AE3Du ^ (uint)j * 0x27D4EB2Fu;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return (int)(h >> 16);
        }
    }
}
