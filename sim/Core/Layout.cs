using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>A map drawn as ASCII, a character a cell, cells numbered row by row from the top-left.</summary>
public sealed record Layout(int Width, int Height, IReadOnlyList<string> Rows, IReadOnlyList<string>? Under = null)
{
    public static readonly Layout Empty = new(0, 0, []);

    /// <summary>
    /// Parses a map as packages/sim/src/core/layout.ts does: a single string is trimmed of blank first
    /// and last lines and common indentation, as tests write maps; rows are read as written. Short rows
    /// are padded with space; a character outside the legend throws.
    /// </summary>
    public static Layout Parse(string map) => Parse(Trimmed(map));

    public static Layout Parse(IReadOnlyList<string> rows, IReadOnlyList<string>? under = null)
    {
        int width = rows.Count == 0 ? 0 : rows.Max(r => r.Length);
        var padded = rows.Select(r => r.PadRight(width)).ToArray();
        Check(padded, Data.Legend, "");
        if (under == null) return new Layout(width, padded.Length, padded);
        var below = Enumerable.Range(0, padded.Length)
            .Select(y => (y < under.Count ? under[y] : "").PadRight(width)).ToArray();
        Check(below, Data.LegendUnder, " under");
        return new Layout(width, padded.Length, padded, below);
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
            char c = At(cell);
            var kind = Data.MachineOf(c);
            if (kind == null || claimed[cell]) continue;
            var spec = Data.Machine(kind)!;
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
