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
/// wall drawn there of the default hull. A negative code is the material without its back wall: a slope under the sky.
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
    public Layout WithMaterials(string map)
    {
        var rows = Trimmed(map);
        var codes = new int[Width * Height];
        Array.Fill(codes, Content.Materials.Get(Content.Materials.DefaultHull).Code);
        for (int y = 0; y < Math.Min(rows.Length, Height); y++)
            for (int x = 0; x < Math.Min(rows[y].Length, Width); x++)
            {
                char c = rows[y][x];
                var id = Legend.MaterialOf(c);
                if (id == null && c is not (' ' or '.')) throw new FormatException($"Material map cell ({x}, {y}): '{c}' is not in the legend.");
                codes[y * Width + x] = Content.Materials.Get(id ?? Content.Materials.DefaultHull).Code;
            }
        return this with { Materials = codes };
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
    public IReadOnlyList<(string Kind, int Cell)> Machines()
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
        return found;
    }
}
