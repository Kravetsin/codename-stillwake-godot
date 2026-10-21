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
        CavernsFine = 9, ChasmAt = 10, ChasmWander = 11, HallAt = 12, Plants = 13, CaveRoll = 14, CavePatch = 17, MouthFade = 18,
        SpireAt = 19, HomeAt = 20;

    public static Layout Generate(Ground g, uint seed) => Generate(g, seed, out _, out _, out _);

    /// <param name="hall">Each cell a great hall opened, where the caves' growth is densest (0080): for tests.</param>
    internal static Layout Generate(Ground g, uint seed, out bool[] hall) => Generate(g, seed, out hall, out _, out _);

    /// <param name="hall">Each cell a great hall opened, where the caves' growth is densest (0080): for tests.</param>
    /// <param name="surface">Each column's surface, its first row of ground before caves and landmarks open it: for tests.</param>
    internal static Layout Generate(Ground g, uint seed, out bool[] hall, out int[] surface) => Generate(g, seed, out hall, out surface, out _);

    /// <param name="hall">Each cell a great hall opened, where the caves' growth is densest (0080): for tests.</param>
    /// <param name="surface">Each column's surface, its first row of ground before caves and landmarks open it: for tests.</param>
    /// <param name="spires">Each spire stamped (0085), its left column and its top row: for tests.</param>
    internal static Layout Generate(Ground g, uint seed, out bool[] hall, out int[] surface, out List<(int X, int Y)> spires) =>
        Generate(g, seed, out hall, out surface, out spires, out _);

    /// <param name="hall">Each cell a great hall opened, where the caves' growth is densest (0080): for tests.</param>
    /// <param name="surface">Each column's surface, its first row of ground before caves and landmarks open it: for tests.</param>
    /// <param name="spires">Each spire stamped (0085), its left column and its top row: for tests.</param>
    /// <param name="homes">Each home stamped (0091), its left column, its top row and its scheme: for tests.</param>
    internal static Layout Generate(
        Ground g, uint seed, out bool[] hall, out int[] surface, out List<(int X, int Y)> spires, out List<(int X, int Y, Ruins.Home Home)> homes)
    {
        int w = g.Width, h = g.Height, floor = h - g.CoreRows;
        int soil = Materials.Get("soil").Code, stone = Materials.Get("stone").Code, bedrock = Materials.Get("bedrock").Code;
        int lava = Materials.Get(Content.Lava.Material).Code;
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
        int span = Math.Max(1, h - g.CoreRows - g.Surface - g.CaveFrom);
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
                bool side = x < g.BedrockSide || x >= w - g.BedrockSide;
                if (side || y >= floor)
                {
                    // The core: lava over the bedrock, between the bedrock's columns at the sides.
                    (kind[cell], codes[cell]) = (1, !side && y < floor + g.LavaRows ? lava : bedrock);
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
        // post; it is taken away, so the ground under it is the top, row by row down, and hills stay walkable. Out in the
        // open air it becomes sky; a post in a pit or a cave leaves ground with its back wall, sky only as far as the fade below.
        for (int y = 1; y < h; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int cell = y * w + x;
                if (!Bump(kind, codes, soil, stone, cell, w) || kind[cell - 1] == 1) continue;
                int across = kind[cell + 1] != 1 ? 1 : x + 2 < w && kind[cell + 2] != 1 && Bump(kind, codes, soil, stone, cell + 1, w) ? 2 : 0;
                bool air = kind[cell - 1] == 0 || kind[cell + across] == 0;
                for (int i = 0; i < across; i++)
                {
                    int at = cell + i;
                    bool sky = air && kind[at - w] == 0;
                    (kind[at], codes[at]) = (sky ? (byte)0 : (byte)2, sky ? 0 : codes[at]);
                }
            }

        // Open ground with nothing solid above it, straight up to the sky — a cave's mouth, a chasm, a pit — is the open sky
        // itself near the surface: no back wall, so it shows the sky as the land round it does, down to a few rows under the
        // surface's line, varying smoothly along it. Deeper, a shaft keeps its back wall all the way down, as caves and ground
        // under an overhang keep theirs: a hero falling down one sees the rock go by.
        for (int x = 0; x < w; x++)
        {
            int fade = g.MouthFadeLeast + (int)((long)Noise(seed, MouthFade, x, 6) * (g.MouthFadeMost - g.MouthFadeLeast + 1) / Whole);
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
                if (kind[cell] != 1 || codes[cell] != soil && codes[cell] != stone || kind[cell - w] == 1) continue;
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
        spires = Spires(g, seed, top, rows, codes, flora);
        homes = Homes(g, seed, top, rows, codes, flora, spires);
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
    /// surface's middle, to the last over the core, by <see cref="Content.Flora.Bands"/>' thousandths of the way down.
    /// </summary>
    public static int BandOf(Ground g, int row)
    {
        int span = Math.Max(1, g.Height - g.CoreRows - g.Surface);
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
        int w = g.Width, h = g.Height, floor = h - g.CoreRows, stone = Materials.Get("stone").Code;
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
                    // Nothing grows on the lava under the caves' last row.
                    if (y + 1 < floor && rows[y + 1][x] == '#') Try(Content.Flora.Anchor.Floor, x, y, cell);
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

    /// <summary>
    /// The spires (0085), stamped last over all that grew: <see cref="Ground.Spires"/> at columns the seed chooses, each in a
    /// band of its own so they spread and never overlap, every column of each at least <see cref="Ground.RuinClear"/> from the
    /// middle; each anchored at the highest surface across it (<paramref name="top"/>), <see cref="Ground.SpireRise"/> of its rows
    /// above that, moved down or up only where the grid is too small to hold it. Its cells overwrite the rows and the materials
    /// but where it keeps the ground; what grew in its box, or lay in the rock a cell round it, goes, and its own growths are
    /// set. Slopes and the other growths beside it were found before it and stay as they are.
    /// </summary>
    /// <returns>Each spire's left column and top row.</returns>
    private static List<(int X, int Y)> Spires(Ground g, uint seed, int[] top, char[][] rows, int[] codes, List<(int Cell, string Kind)> flora)
    {
        var spires = new List<(int X, int Y)>();
        var prefab = Content.Ruins.Spire;
        int w = g.Width, wide = Content.Ruins.SpireWide, tall = prefab.Count, rise = Math.Clamp(g.SpireRise, 0, tall);
        int floor = g.Height - g.BedrockRows, middle = w / 2, from = g.BedrockSide + 1;
        // The left columns a spire may start at, the left side's then the right's, counted as one run the bands split.
        int left = Math.Max(0, middle - g.RuinClear - wide + 1 - from), rightFrom = middle + g.RuinClear;
        int right = Math.Max(0, w - g.BedrockSide - 1 - wide + 1 - rightFrom);
        int lowest = floor - (tall - rise);
        if (g.Spires <= 0 || left + right == 0 || lowest < rise) return spires;
        int band = (left + right) / g.Spires;
        if (band < wide + 1) return spires;
        int alloy = Materials.Get("alloy").Code, soil = Materials.Get("soil").Code;
        for (int k = 0; k < g.Spires; k++)
        {
            // Within its band, a spire's width short of its end: the next one starts past it.
            int at = k * band + (int)((long)Lattice(seed, SpireAt, k, 0) * (band - wide) / Whole);
            int x0 = at < left ? from + at : rightFrom + (at - left);
            int anchor = int.MaxValue;
            for (int x = x0; x < x0 + wide; x++) anchor = Math.Min(anchor, top[x]);
            int y0 = Math.Clamp(anchor, rise, lowest) - rise;
            // What grew in its box goes, and what lay in the rock round it, which the spire's alloy may now close off from its cave.
            bool In(int cell, int by) => cell % w >= x0 - by && cell % w < x0 + wide + by && cell / w >= y0 - by && cell / w < y0 + tall + by;
            flora.RemoveAll(p => Stamped(p, w, x0, x0 + wide - 1, In));
            for (int dy = 0; dy < tall; dy++)
                for (int dx = 0; dx < wide; dx++)
                {
                    char c = prefab[dy][dx];
                    if (c == Content.Ruins.Keep) continue;
                    int x = x0 + dx, y = y0 + dy, cell = y * w + x;
                    bool open = c is Content.Ruins.Open or Content.Ruins.Cache;
                    rows[y][x] = open ? '.' : '#';
                    codes[cell] = c == Content.Ruins.Plug ? soil : alloy;
                    string? kind = c switch
                    {
                        Content.Ruins.Cache => Content.Ruins.SpireCache,
                        Content.Ruins.Shrine => Content.Ruins.RelicShrine,
                        Content.Ruins.Lamp => Content.Ruins.SpireLamp,
                        _ => null,
                    };
                    if (kind != null) flora.Add((cell, kind));
                }
            foreach (var (kind, px, py) in Content.Ruins.SpireFurnishing) flora.Add(((y0 + py) * w + x0 + px, kind));
            spires.Add((x0, y0));
        }
        return spires;
    }

    /// <summary>
    /// Whether growth <paramref name="p"/> goes with a ruin stamped at <paramref name="inBox"/> (a cell, a margin): one standing,
    /// hanging or reaching into its box by any of its <c>Tall</c> cells, or held by a wall in it, which is the ruin's now; or one
    /// in the rock a cell round it, which its walls may close off from its cave. Looked up only for those in its columns or beside them.
    /// </summary>
    private static bool Stamped((int Cell, string Kind) p, int w, int left, int right, Func<int, int, bool> inBox)
    {
        int x = p.Cell % w;
        if (x < left - 1 || x > right + 1) return false;
        var kind = Content.Flora.Get(p.Kind);
        if (kind.On == Content.Flora.Anchor.Rock) return inBox(p.Cell, 1);
        int step = kind.On == Content.Flora.Anchor.Floor ? -w : w;
        if (inBox(p.Cell - step, 0)) return true;
        for (int i = 0, cell = p.Cell; i < kind.Tall; i++, cell += step)
            if (inBox(cell, 0)) return true;
        return false;
    }

    /// <summary>How many columns either side of a home's box no spire may come into.</summary>
    private const int HomeMargin = 2;

    /// <summary>
    /// The arc makers' homes (0091), stamped after the spires, last of all: <see cref="Ground.Homes"/> of them, their schemes in
    /// <see cref="Content.Ruins.Homes"/>' order from one the seed picks, each in a band of its own along the columns a home may
    /// start at — inside the bedrock's sides, none within <see cref="Ground.HomeClear"/> of the middle — at the start nearest the
    /// seed's pick in its band whose box, <see cref="HomeMargin"/> wider each side, meets no spire's columns; every band but the
    /// last a home's width short of its end, so the next starts past it. Its ceiling <see cref="Ground.HomeDeepLeast"/> to
    /// <see cref="Ground.HomeDeepMost"/> rows (by the seed) under the lowest surface across it, moved up only where the grid is too
    /// small. Its cells overwrite the rows and the materials but where it keeps the ground; what grew in its box, or lay in the
    /// rock a cell round it, goes, and its own pieces are set. A band with no start clear of the spires holds none.
    /// </summary>
    /// <returns>Each home's left column, top row and scheme.</returns>
    private static List<(int X, int Y, Content.Ruins.Home Home)> Homes(
        Ground g, uint seed, int[] top, char[][] rows, int[] codes, List<(int Cell, string Kind)> flora, List<(int X, int Y)> spires)
    {
        var homes = new List<(int X, int Y, Content.Ruins.Home Home)>();
        var schemes = Content.Ruins.Homes;
        if (g.Homes <= 0) return homes;
        int w = g.Width, floor = g.Height - g.CoreRows, middle = w / 2, wide = schemes.Max(s => s.Wide);
        var starts = new List<int>();
        for (int x = g.BedrockSide + 1; x + wide <= w - g.BedrockSide - 1; x++)
            if (g.HomeClear <= 0 || x + wide - 1 < middle - g.HomeClear || x >= middle + g.HomeClear) starts.Add(x);
        int band = starts.Count / g.Homes;
        if (band < 1) return homes;
        int first = (int)((long)Lattice(seed, HomeAt, 0, 1) * schemes.Count / Whole);
        int alloy = Materials.Get("alloy").Code, soil = Materials.Get("soil").Code, stone = Materials.Get("stone").Code;
        int plaster = Materials.Get("plaster").Code, spireWide = Content.Ruins.SpireWide;
        for (int k = 0; k < g.Homes; k++)
        {
            var home = schemes[(first + k) % schemes.Count];
            int from = k * band, room = k == g.Homes - 1 ? starts.Count - from : band - wide;
            if (room < 1) continue;
            int pick = from + (int)((long)Lattice(seed, HomeAt, k, 0) * room / Whole);
            bool Clear(int i) => !spires.Any(s => starts[i] - HomeMargin < s.X + spireWide && s.X < starts[i] + home.Wide + HomeMargin);
            int at = -1;
            for (int d = 0; d < room && at < 0; d++)
            {
                if (pick - d >= from && Clear(pick - d)) at = pick - d;
                else if (pick + d < from + room && Clear(pick + d)) at = pick + d;
            }
            if (at < 0) continue;
            int x0 = starts[at], lowest = 0;
            for (int x = x0; x < x0 + home.Wide; x++) lowest = Math.Max(lowest, top[x]);
            int deep = g.HomeDeepLeast + (int)((long)Lattice(seed, HomeAt, k, 2) * (g.HomeDeepMost - g.HomeDeepLeast + 1) / Whole);
            int y0 = Math.Min(lowest + deep, floor - home.Tall - 1);
            if (y0 < 1) continue;
            int tall = home.Tall;
            bool In(int cell, int by) => cell % w >= x0 - by && cell % w < x0 + home.Wide + by && cell / w >= y0 - by && cell / w < y0 + tall + by;
            flora.RemoveAll(p => Stamped(p, w, x0, x0 + home.Wide - 1, In));
            for (int dy = 0; dy < tall; dy++)
                for (int dx = 0; dx < home.Wide; dx++)
                {
                    char c = home.Rows[dy][dx];
                    if (c == Content.Ruins.Keep) continue;
                    int cell = (y0 + dy) * w + x0 + dx;
                    rows[y0 + dy][x0 + dx] = c is Content.Ruins.Alloy or Content.Ruins.Plug or Content.Ruins.Stone ? '#' : '.';
                    codes[cell] = c switch
                    {
                        Content.Ruins.Plug => soil,
                        Content.Ruins.Stone or Content.Ruins.OpenStone => stone,
                        Content.Ruins.Plaster => plaster,
                        _ => alloy,
                    };
                }
            foreach (var (kind, px, py) in home.Pieces) flora.Add(((y0 + py) * w + x0 + px, kind));
            homes.Add((x0, y0, home));
        }
        return homes;
    }

    /// <summary>Whether <paramref name="cell"/> is soil or stone with the open above it: the top of the ground there.</summary>
    private static bool Bump(byte[] kind, int[] codes, int soil, int stone, int cell, int w) =>
        kind[cell] == 1 && (codes[cell] == soil || codes[cell] == stone) && kind[cell - w] != 1;

    /// <summary>
    /// The landmarks, at places the seed chooses, none within <see cref="Ground.Clear"/> of the middle: chasms cut from the
    /// surface down, wandering a little, and great halls with ragged walls. Both are open ground, back-walled, as caves are.
    /// </summary>
    /// <param name="hall">Set for every cell a great hall opens: where the caves' growth is densest (0080).</param>
    private static void Landmarks(Ground g, uint seed, int[] top, byte[] kind, Field ragged, bool[] hall)
    {
        int w = g.Width, h = g.Height, floor = h - g.CoreRows;
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
