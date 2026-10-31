using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>A map drawn as ASCII, a character a cell, cells numbered row by row from the top-left.</summary>
/// <param name="Cables">The cells of the two ends of each power cable laid on the map.</param>
/// <param name="Known">The blueprints known at the start (ADR 0024); every one when not given.</param>
/// <param name="DebrisAtStart">Pieces of debris already on their way past the ship when its world begins.</param>
/// <param name="Zone">Where the crew may build (0048); the whole map when not given, as test maps leave it.</param>
/// <param name="Materials">
/// Each cell's material code (<c>Content.Materials</c>), for its wall and its back wall: what one character cannot say (0077).
/// None given, every one is the default hull, as on every map before the planets; 0 is the open sky: no back wall, and a
/// wall drawn there of the default hull.
/// </param>
public sealed record Layout(
    int Width, int Height, IReadOnlyList<string> Rows, IReadOnlyList<string>? Under = null,
    IReadOnlyList<(int A, int B)>? Cables = null, IReadOnlyList<string>? Known = null, int DebrisAtStart = 0,
    Box? Zone = null, IReadOnlyList<int>? Materials = null)
{
    public static readonly Layout Empty = new(0, 0, []);

    /// <summary>
    /// Grown from a seed (0077, <c>Terrain</c>), not drawn: its grid saves only the cells changed since, the ground grown again
    /// from the seed when the save loads.
    /// </summary>
    public bool Grown { get; init; }

    /// <summary>
    /// What grows on its surface (0079): each plant's cell, the open cell over the soil it grows from, and its kind
    /// (<c>Content.Flora</c>), ascending by cell. Grown from the seed with the ground (<c>Terrain</c>); none on a drawn map
    /// but a test's, whose plants a load does not bring back.
    /// </summary>
    public IReadOnlyList<(int Cell, string Kind)>? Flora { get; init; }

    /// <summary>
    /// The kind of surface the ship's own grid is played under (<c>Content.Surfaces</c>), by its id: the ship's, but for a test
    /// or dev world drawn as a planet's ground, played under its rules.
    /// </summary>
    public string Kind { get; init; } = Surface.Ship;

    /// <summary>Whether its surface's meteors fall (2026-10-28-hull-meteors): a new game's wreck; a test's or dev world's map is calm.</summary>
    public bool Meteors { get; init; }

    /// <summary>The build zone of a world of <c>Content.World</c>'s size: <c>ZoneWidth</c> × <c>ZoneHeight</c> in its middle.</summary>
    public static Box WorldZone
    {
        get
        {
            var (left, top) = OffsetInWorld(Content.World.ZoneWidth, Content.World.ZoneHeight);
            return new Box(left, top, left + Content.World.ZoneWidth - 1, top + Content.World.ZoneHeight - 1);
        }
    }

    /// <summary>Where the top-left cell of a map of <paramref name="width"/> × <paramref name="height"/> lands, centred in a world of <c>Content.World</c>'s size; rounded down.</summary>
    public static (int X, int Y) OffsetInWorld(int width, int height) =>
        ((Content.World.Width - width) / 2, (Content.World.Height - height) / 2);

    /// <summary>
    /// This map centred in a world of <c>Content.World</c>'s size, space all round it, its under-map and
    /// cables moved with it, the build zone <see cref="WorldZone"/>: what a new world is made from.
    /// </summary>
    public Layout InWorld()
    {
        int width = Content.World.Width, height = Content.World.Height;
        var (ox, oy) = OffsetInWorld(Width, Height);
        string[] Padded(IReadOnlyList<string> rows)
        {
            var blank = new string(' ', width);
            var padded = new string[height];
            for (int y = 0; y < height; y++)
                padded[y] = y >= oy && y < oy + Height ? new string(' ', ox) + rows[y - oy] + new string(' ', width - ox - Width) : blank;
            return padded;
        }
        int Moved(int cell) => (cell / Width + oy) * width + cell % Width + ox;
        int[]? materials = null;
        if (Materials != null)
        {
            materials = new int[width * height];
            for (int cell = 0; cell < Materials.Count; cell++) materials[Moved(cell)] = Materials[cell];
        }
        return this with
        {
            Width = width,
            Height = height,
            Rows = Padded(Rows),
            Under = Under == null ? null : Padded(Under),
            Cables = Cables?.Select(c => (Moved(c.A), Moved(c.B))).ToArray(),
            Zone = WorldZone,
            Materials = materials,
        };
    }

    /// <summary>
    /// Parses a map as packages/sim/src/core/layout.ts does: a single string is trimmed of blank first
    /// and last lines and common indentation, as tests write maps; rows are read as written. Short rows
    /// are padded with space; a character outside the legend throws.
    /// </summary>
    public static Layout Parse(string map) => Parse(Trimmed(map));

    /// <param name="cables">Power cables laid on the map, each by its two ends, column and row.</param>
    public static Layout Parse(
        IReadOnlyList<string> rows, IReadOnlyList<string>? under = null,
        IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>? cables = null)
    {
        int width = rows.Count == 0 ? 0 : rows.Max(r => r.Length);
        var padded = rows.Select(r => r.PadRight(width)).ToArray();
        Check(padded, Legend.MapChars, "");
        var ends = cables?.Select(c => (c.A.Y * width + c.A.X, c.B.Y * width + c.B.X)).ToArray();
        if (under == null) return new Layout(width, padded.Length, padded, Cables: ends);
        var below = Enumerable.Range(0, padded.Length)
            .Select(y => (y < under.Count ? under[y] : "").PadRight(width)).ToArray();
        Check(below, Legend.UnderChars, " under");
        return new Layout(width, padded.Length, padded, below, ends);
    }

    /// <summary>
    /// This map with the materials of its cells drawn beside it (0077), the same size, in <c>Legend.Materials</c>' characters and
    /// trimmed as <see cref="Parse(string)"/> trims a map: how a test draws natural ground.
    /// </summary>
    public Layout WithMaterials(string map) => WithMaterials(Trimmed(map));

    /// <summary>This map with the materials of its cells drawn beside it, rows as written: how content draws its walls.</summary>
    public Layout WithMaterials(IReadOnlyList<string> rows)
    {
        var codes = new int[Width * Height];
        Array.Fill(codes, Content.Materials.Get(Content.Materials.DefaultHull).Code);
        for (int y = 0; y < Math.Min(rows.Count, Height); y++)
            for (int x = 0; x < Math.Min(rows[y].Length, Width); x++)
            {
                char c = rows[y][x];
                var id = Legend.MaterialOf(c);
                if (id == null && c is not (' ' or '.')) throw new FormatException($"Material map cell ({x}, {y}): '{c}' is not in the legend.");
                codes[y * Width + x] = Content.Materials.Get(id ?? Content.Materials.DefaultHull).Code;
            }
        return this with { Materials = codes };
    }

    /// <summary>
    /// This map at the cell's scale (0104): each character drawn a 16 px cell, as maps are, turned into the 2×2 cells of 8 px
    /// that cover it. Walls, back walls, machines, ladders and doors fill all four, so a doorway is two cells thick, as its
    /// 16 px door was one, and a ladder two wide; a floor and the hero's waking place fill the row a body stands on, the floor its top, <c>S</c> its bottom-left, the rest open;
    /// a conduit its top-left, and the cell right of it or under it where the character there is a conduit too, on either map,
    /// so pipes stay one cell wide and meet the ports, which double with the footprints. The under-map's oxygen marks its
    /// tank's top-left. Materials, cables (by their ends' top-left cells), the zone and the plants of a test's map double
    /// with it: a plant on a floor by its block's bottom-left, any other by its top-left.
    /// </summary>
    public Layout Doubled()
    {
        if (Grown) throw new InvalidOperationException("A grown ground grows at its own scale: it does not double.");
        int w = 2 * Width, h = 2 * Height;
        bool IsConduit(char c) => c is '-' or 'v' or 'x';
        char Main(int x, int y) => x < Width && y < Height ? Rows[y][x] : ' ';
        char Below(int x, int y) => Under != null && x < Width && y < Height ? Under[y][x] : ' ';
        bool Conduit(int x, int y) => IsConduit(Main(x, y)) || Below(x, y) == '-';
        var rows = new char[h][];
        var under = Under == null ? null : new char[h][];
        for (int y = 0; y < h; y++)
        {
            rows[y] = new char[w];
            if (under != null) Array.Fill(under[y] = new char[w], ' ');
        }
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                char c = Rows[y][x];
                // The block's cells: top-left, top-right, bottom-left, bottom-right.
                char tl = c, tr = c, bl = c, br = c;
                if (Structures.OfChar(c) is int code && Structures.Of(code).Flat) (bl, br) = ('.', '.');
                else if (c == 'S') (tl, tr, br) = ('.', '.', '.');
                else if (IsConduit(c))
                {
                    tr = Conduit(x + 1, y) ? '-' : '.';
                    bl = Conduit(x, y + 1) ? '-' : '.';
                    br = '.';
                }
                (rows[2 * y][2 * x], rows[2 * y][2 * x + 1], rows[2 * y + 1][2 * x], rows[2 * y + 1][2 * x + 1]) = (tl, tr, bl, br);
                if (under == null) continue;
                char u = Under![y][x];
                if (u == 'o') under[2 * y][2 * x] = 'o';
                else if (u == '-')
                {
                    under[2 * y][2 * x] = '-';
                    if (Conduit(x + 1, y)) under[2 * y][2 * x + 1] = '-';
                    if (Conduit(x, y + 1)) under[2 * y + 1][2 * x] = '-';
                }
            }
        int Moved(int cell) => cell / Width * 2 * w + cell % Width * 2;
        int[]? materials = null;
        if (Materials != null)
        {
            materials = new int[w * h];
            for (int cell = 0; cell < Materials.Count; cell++)
            {
                int at = Moved(cell);
                materials[at] = materials[at + 1] = materials[at + w] = materials[at + w + 1] = Materials[cell];
            }
        }
        return this with
        {
            Width = w,
            Height = h,
            Rows = [.. rows.Select(r => new string(r))],
            Under = under?.Select(r => new string(r)).ToArray(),
            Cables = Cables?.Select(c => (Moved(c.A), Moved(c.B))).ToArray(),
            Zone = Zone is { } z ? new Box(2 * z.Left, 2 * z.Top, 2 * z.Right + 1, 2 * z.Bottom + 1) : null,
            Materials = materials,
            Flora = Flora?.Select(p => (Moved(p.Cell) + (Content.Flora.Get(p.Kind).On == Content.Flora.Anchor.Floor ? w : 0), p.Kind))
                .OrderBy(p => p.Item1).ToArray(),
        };
    }

    private static void Check(string[] rows, string legend, string which)
    {
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (!legend.Contains(rows[y][x]))
                    throw new FormatException($"Map cell ({x}, {y}){which}: '{rows[y][x]}' is not in the legend.");
    }

    private static string[] Trimmed(string map)
    {
        var lines = map.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        while (lines.Count > 0 && lines[0].Trim().Length == 0) lines.RemoveAt(0);
        while (lines.Count > 0 && lines[^1].Trim().Length == 0) lines.RemoveAt(lines.Count - 1);
        int indent = lines.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length)
            .DefaultIfEmpty(0).Min();
        return lines.Select(l => (l.Length >= indent ? l[indent..] : "").TrimEnd()).ToArray();
    }

    public char At(int cell) => Rows[cell / Width][cell % Width];

    /// <summary>Every cell holding <paramref name="c"/>, in reading order, on the map or the map under it.</summary>
    public IEnumerable<int> CellsOf(char c, bool under = false)
    {
        var rows = under ? Under ?? [] : Rows;
        for (int y = 0; y < rows.Count; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (rows[y][x] == c) yield return y * Width + x;
    }

    /// <summary>
    /// The machines the map draws, in reading order of their top-left cells: each unclaimed machine
    /// character starts one, whose footprint must be drawn in full.
    /// </summary>
    public IReadOnlyList<(string Kind, int Cell)> Machines() => Found.GetValue(this, layout => layout.FindMachines());

    /// <summary>
    /// Each map's machines, found once: every module creating its slice asks, and a planet's map is millions of cells. Kept by
    /// the instance, not in a field, so a copy made by <c>with</c> finds its own and two layouts still compare by what they draw.
    /// </summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Layout, IReadOnlyList<(string Kind, int Cell)>> Found = new();

    private (string Kind, int Cell)[] FindMachines()
    {
        var claimed = new bool[Width * Height];
        var found = new List<(string, int)>();
        for (int cell = 0; cell < Width * Height; cell++)
        {
            char c = Rows[cell / Width][cell % Width];
            // Most cells of a map are no machine: ground, walls, space.
            if (c is ' ' or '#' or '.') continue;
            var kind = Legend.MachineOf(c);
            if (kind == null || claimed[cell]) continue;
            var spec = Content.Machines.Get(kind);
            int x = cell % Width, y = cell / Width;
            for (int dy = 0; dy < spec.Height; dy++)
                for (int dx = 0; dx < spec.Width; dx++)
                {
                    int covered = (y + dy) * Width + x + dx;
                    if (x + dx >= Width || y + dy >= Height || Rows[y + dy][x + dx] != c || claimed[covered])
                        throw new FormatException(
                            $"Map cell ({x}, {y}): a {kind} is {spec.Width}×{spec.Height}; draw its whole footprint with '{c}'.");
                    claimed[covered] = true;
                }
            found.Add((kind, cell));
        }
        return [.. found];
    }
}
