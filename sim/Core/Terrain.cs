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
        CavernsFine = 9, ChasmAt = 10, ChasmWander = 11, HallAt = 12, Plants = 13, CaveRoll = 14, CavePatch = 17, MouthFade = 18;

    public static Layout Generate(Ground g, uint seed) => Generate(g, seed, out _, out _);

    /// <param name="hall">Each cell a great hall opened, where the caves' growth is densest (0080): for tests.</param>
    internal static Layout Generate(Ground g, uint seed, out bool[] hall) => Generate(g, seed, out hall, out _);

    /// <param name="hall">Each cell a great hall opened, where the caves' growth is densest (0080): for tests.</param>
    /// <param name="surface">Each column's surface, its first row of ground before caves and landmarks open it: for tests.</param>
    internal static Layout Generate(Ground g, uint seed, out bool[] hall, out int[] surface)
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
        hall = new bool[w * h];
        Landmarks(g, seed, top, kind, caverns, hall);
        // No spikes: a bump one or two cells wide standing up alone, open above, would be cut to a lone ◢◣ peak or left a
        // post; it is taken away, so the ground under it is the top, row by row down, and hills stay walkable.
        for (int y = 1; y < h; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int cell = y * w + x;
                if (!Bump(kind, codes, bedrock, cell, w) || kind[cell - 1] == 1) continue;
                int across = kind[cell + 1] != 1 ? 1 : x + 2 < w && kind[cell + 2] != 1 && Bump(kind, codes, bedrock, cell + 1, w) ? 2 : 0;
                for (int i = 0; i < across; i++)
                {
                    int at = cell + i;
                    bool sky = kind[at - w] == 0;
                    (kind[at], codes[at]) = (sky ? (byte)0 : (byte)2, sky ? 0 : codes[at]);
                }
            }

        // Open ground with nothing solid above it, straight up to the sky — a cave's mouth, a chasm, a pit — is the open sky
        // itself near the surface: no back wall, so it shows the sky as the land round it does, down to a few rows under the
        // surface's line, ragged column by column. Deeper, a shaft keeps its back wall all the way down, as caves and ground
        // under an overhang keep theirs: a hero falling down one sees the rock go by.
        for (int x = 0; x < w; x++)
        {
            int fade = g.MouthFadeLeast + (int)((long)Lattice(seed, MouthFade, x, 0) * (g.MouthFadeMost - g.MouthFadeLeast + 1) / Whole);
            for (int y = 0; y < h && y < top[x] + fade && kind[y * w + x] != 1; y++)
                (kind[y * w + x], codes[y * w + x]) = (0, 0);
        }
        surface = top;

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
                else continue;
                // A slope under the open sky has no back wall: its open half shows the sky. One in a cave keeps it, dark.
                if (kind[cell - w] == 0) codes[cell] = -codes[cell];
            }
        var spawn = SpawnOf(rows, w, h);
        var surfaceTop = TopsOf(rows, w, h);
        var flora = Flora(g, seed, rows, codes, soil, spawn % w, surfaceTop);
        CaveFlora(g, seed, rows, codes, hall, surfaceTop, flora);
        // Found column by column and row by row; kept by cell, as every list of cells is.
        flora.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        rows[spawn / w][spawn % w] = 'S';
        return new Layout(w, h, [.. rows.Select(r => new string(r))], Materials: codes) { Grown = true, Flora = flora };
    }

    /// <summary>
    /// What grows on the surface (0079), column by column from the left: on whole soil with the open sky above it, one draw
    /// a column picks a kind of <see cref="Content.Flora.All"/> by its chances, set where it has room — its ground level with
    /// its own, no ground higher across its crown, the last of its kind far enough, the last plant's level ground not
    /// shared — and nowhere within two columns of the spawn. A column's top is its first solid cell, so nothing grows in a cave.
    /// </summary>
    private static List<(int Cell, string Kind)> Flora(Ground g, uint seed, char[][] rows, int[] codes, int soil, int spawnX, int[] top)
    {
        int w = g.Width, h = g.Height;
        bool Soil(int x, int y) => y < h && rows[y][x] == '#' && codes[y * w + x] == soil;
        var kinds = Content.Flora.All;
        var last = new int[kinds.Count];
        Array.Fill(last, int.MinValue / 2);
        int lastX = int.MinValue / 2, lastLevel = 0;
        var flora = new List<(int, string)>();
        for (int x = g.BedrockSide + 1; x < w - g.BedrockSide - 1; x++)
        {
            int y = top[x];
            if (!Soil(x, y) || Math.Abs(x - spawnX) <= 2) continue;
            int roll = (int)((long)Lattice(seed, Plants, x, 0) * 1000 / Whole), from = 0, k = 0;
            while (k < kinds.Count && roll >= from + kinds[k].Chance) from += kinds[k++].Chance;
            if (k == kinds.Count) continue;
            var kind = kinds[k];
            if (x - last[k] < kind.Apart || x - lastX <= Math.Max(lastLevel, kind.Level) || y - kind.Clear < 0) continue;
            bool room = true;
            int reach = Math.Max(kind.Level, kind.Room);
            for (int dx = -reach; dx <= reach && room; dx++)
            {
                int cx = x + dx;
                room = cx >= 0 && cx < w && top[cx] >= y && (Math.Abs(dx) > kind.Level || top[cx] == y && Soil(cx, y));
            }
            if (!room) continue;
            flora.Add(((y - 1) * w + x, kind.Id));
            (last[k], lastX, lastLevel) = (x, x, kind.Level);
        }
        return flora;
    }

    /// <summary>Each column's top: its first row of ground, open or not, under the sky.</summary>
    private static int[] TopsOf(char[][] rows, int w, int h)
    {
        var top = new int[w];
        for (int x = 0; x < w; x++)
        {
            int y = 0;
            while (y < h && rows[y][x] == ' ') y++;
            top[x] = y;
        }
        return top;
    }

    /// <summary>
    /// The depth band of row <paramref name="row"/> of ground <paramref name="g"/> (0080): 0, the coldest, near and above the
    /// surface's middle, to the last over the bedrock, by <see cref="Content.Flora.Bands"/>' thousandths of the way down.
    /// </summary>
    public static int BandOf(Ground g, int row)
    {
        int span = Math.Max(1, g.Height - g.BedrockRows - g.Surface);
        long down = (long)(row - g.Surface) * 1000 / span;
        int band = 0;
        while (band < Content.Flora.Bands.Count && down >= Content.Flora.Bands[band]) band++;
        return band;
    }

    /// <summary>
    /// What grows in the caves (0080), row by row from the top, each row from the left: in every slot — an open cave cell over a
    /// whole wall (a floor), one under a whole wall (a ceiling), a stone wall beside a cave cell (rock) — one draw under 10000
    /// picks a kind of that anchor by its chance in the row's band, scaled by the patch noise there and in a great hall, set
    /// where it has room (<see cref="Content.Flora.CaveKind"/>). Only in the caves: an open cell under the sky is no slot, nor a
    /// wall beside only the sky.
    /// </summary>
    private static void CaveFlora(Ground g, uint seed, char[][] rows, int[] codes, bool[] hall, int[] top, List<(int, string)> into)
    {
        int w = g.Width, h = g.Height, floor = h - g.BedrockRows, stone = Materials.Get("stone").Code;
        var kinds = Content.Flora.Cave;
        var bands = new int[h];
        for (int y = 0; y < h; y++) bands[y] = BandOf(g, y);
        // Each anchor's kinds, in the list's order, and each kind's chance by band: looked up a slot at a time.
        var byAnchor = new int[3][];
        for (int on = 0; on < 3; on++) byAnchor[on] = Enumerable.Range(0, kinds.Count).Where(k => (int)kinds[k].Holds == on).ToArray();
        var chances = kinds.Select(k => k.Chances.ToArray()).ToArray();
        var last = new int[kinds.Count];
        var lastOn = new int[3];
        var taken = new bool[w * h];
        var patches = new Field(seed, CavePatch, Content.Flora.PatchWave, Content.Flora.PatchWave, w);
        var patch = new int[w];
        bool Open(int x, int y) => rows[y][x] == '.';
        for (int y = 1; y < floor; y++)
        {
            Array.Fill(last, int.MinValue / 2);
            Array.Fill(lastOn, int.MinValue / 2);
            patches.Row(y, patch);
            for (int x = g.BedrockSide; x < w - g.BedrockSide; x++)
            {
                int cell = y * w + x;
                if (Open(x, y))
                {
                    if (rows[y + 1][x] == '#') Try(Content.Flora.Anchor.Floor, x, y, cell);
                    if (rows[y - 1][x] == '#') Try(Content.Flora.Anchor.Ceiling, x, y, cell);
                }
                else if (rows[y][x] == '#' && codes[cell] == stone && (Open(x, y - 1) || Open(x, y + 1) || Open(x - 1, y) || Open(x + 1, y)))
                    Try(Content.Flora.Anchor.Rock, x, y, cell);
            }
        }

        void Try(Content.Flora.Anchor on, int x, int y, int cell)
        {
            if (taken[cell]) return;
            int band = bands[y], roll = (int)((long)Lattice(seed, CaveRoll + (uint)on, x, y) * 10000 / Whole);
            // Percent of the chances here: the patch noise's share of its most, times a hall's — a wall's when it faces one.
            bool inHall = hall[cell] || on == Content.Flora.Anchor.Rock && (hall[cell - w] || hall[cell + w] || hall[cell - 1] || hall[cell + 1]);
            long scale = (long)patch[x] * Content.Flora.PatchMost / Whole * (inHall ? Content.Flora.Hall : 100);
            long from = 0;
            int k = -1;
            foreach (int each in byAnchor[(int)on])
            {
                from += chances[each][band] * scale / 10000;
                if (roll < from)
                {
                    k = each;
                    break;
                }
            }
            if (k < 0) return;
            var kind = kinds[k];
            if (x - last[k] < kind.Apart || (on != Content.Flora.Anchor.Rock && x - lastOn[(int)on] <= 1)) return;
            if (kind.Shallow is int shallow && y - top[x] > shallow) return;
            if (kind.Floors && !Open(x, y - 1)) return;
            int step = on == Content.Flora.Anchor.Ceiling ? w : -w, hold = on == Content.Flora.Anchor.Ceiling ? -1 : 1;
            if (on != Content.Flora.Anchor.Rock)
            {
                for (int i = 0, at = cell; i < kind.Tall; i++, at += step)
                    if (at < 0 || at >= w * h || rows[at / w][x] != '.' || taken[at]) return;
                // Only on whole walls, never a slope, as Terraria's: no growth floats over a slope or stands in one.
                for (int dx = -kind.Wide; dx <= kind.Wide; dx++)
                    if (x + dx < 0 || x + dx >= w || !Open(x + dx, y) || rows[y + hold][x + dx] != '#') return;
                for (int i = 0, at = cell; i < kind.Tall; i++, at += step) taken[at] = true;
            }
            into.Add((cell, kind.Id));
            (last[k], lastOn[(int)on]) = (x, x);
        }
    }

    /// <summary>Whether <paramref name="cell"/> is soil or stone with the open above it: the top of the ground there.</summary>
    private static bool Bump(byte[] kind, int[] codes, int bedrock, int cell, int w) => kind[cell] == 1 && codes[cell] != bedrock && kind[cell - w] != 1;

    /// <summary>
    /// The landmarks, at places the seed chooses, none within <see cref="Ground.Clear"/> of the middle: chasms cut from the
    /// surface down, wandering a little, and great halls with ragged walls. Both are open ground, back-walled, as caves are.
    /// </summary>
    /// <param name="hall">Set for every cell a great hall opens: where the caves' growth is densest (0080).</param>
    private static void Landmarks(Ground g, uint seed, int[] top, byte[] kind, Field ragged, bool[] hall)
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
                    if (dx * dx * ry2 + dy * dy * rx2 <= within && y >= top[x]) (kind[y * w + x], hall[y * w + x]) = (2, true);
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
