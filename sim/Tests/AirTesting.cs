using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crew;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/testing/air.ts, and the slices TypeScript's tests read directly.</summary>
public static class AirTesting
{
    /// <summary>
    /// What a room leaking <paramref name="leak"/> every <c>Air.LeakEvery</c> ticks uses on the air ticks
    /// after clock <paramref name="from"/>, up to <paramref name="to"/>: the leak spread over the period,
    /// what is due by the end of a tick less what was by its start.
    /// </summary>
    public static int Leaked(int leak, int from, int to)
    {
        long sum = 0;
        for (long clock = from + 1; clock <= to; clock++)
        {
            long beat = clock % Content.Air.LeakEvery;
            sum += leak * (beat + 1) / Content.Air.LeakEvery - leak * beat / Content.Air.LeakEvery;
        }
        return (int)sum;
    }

    /// <summary>The cell of the character drawn at (<paramref name="x"/>, <paramref name="y"/>) of a map <paramref name="width"/> wide, doubled: its block's top-left (0104).</summary>
    public static int Cell(int width, int x, int y) => 2 * y * 2 * width + 2 * x;

    /// <summary>
    /// The cells from the block of the character drawn at (<paramref name="x0"/>, <paramref name="y0"/>) to that at
    /// (<paramref name="x1"/>, <paramref name="y1"/>), along their top-left cells: a pipe one cell wide, as a doubled map lays one.
    /// </summary>
    public static IEnumerable<int> Line(int width, int x0, int y0, int x1, int y1)
    {
        if (x0 != x1 && y0 != y1) throw new ArgumentException("A line runs along a row or a column.");
        int from = Cell(width, x0, y0), to = Cell(width, x1, y1);
        int step = (x0 != x1 ? 1 : 2 * width) * Math.Sign(to - from);
        for (int cell = from; ; cell += step)
        {
            yield return cell;
            if (cell == to) yield break;
        }
    }

    /// <summary>
    /// <paramref name="map"/> doubled (0104), its hull one cell thick as a player builds one at 8 px: every wall cell beside
    /// space or the grid's edge and no open cell is taken away. A wall two cells thick leaks nothing (Rooms README).
    /// </summary>
    public static Layout OneThick(string map)
    {
        var doubled = Layout.Parse(map).Doubled();
        int w = doubled.Width, h = doubled.Height;
        char At(int x, int y) => x < 0 || y < 0 || x >= w || y >= h ? '\0' : doubled.Rows[y][x];
        var rows = doubled.Rows.Select(r => r.ToCharArray()).ToArray();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (At(x, y) != '#') continue;
                char[] near = [At(x - 1, y), At(x + 1, y), At(x, y - 1), At(x, y + 1)];
                if (near.Any(c => c is '\0' or ' ') && near.All(c => c is '\0' or ' ' or '#')) rows[y][x] = ' ';
            }
        return doubled with { Rows = [.. rows.Select(r => new string(r))] };
    }

    /// <summary>
    /// A map drawn before liquid oxygen, a tank <c>TT</c> piped along its bottom row rightwards to vents, made the chain
    /// (liquid-oxygen), every drawn cell and machine id kept: the tank's cells become a vaporizer, its id the tank's; its
    /// gas leaves the output on its top row's right and joins the old run past it; under the hull, in space, a tank
    /// holding the old <c>o</c> and a solar panel cabled to the vaporizer, ids after every other, the liquid drawn up
    /// through the floor to the vaporizer's input. The rows are doubled as the test's own are (0104).
    /// </summary>
    public static Layout Vaporized(
        IReadOnlyList<string> rows, IReadOnlyList<string>? under = null, IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>? cables = null)
    {
        int width = rows.Max(r => r.Length), height = rows.Count;
        var main = rows.Select(r => r.PadRight(width).ToCharArray()).Concat(Enumerable.Range(0, 3).Select(_ => new string(' ', width).ToCharArray())).ToArray();
        var below = Enumerable.Range(0, height + 3).Select(y => (under != null && y < under.Count ? under[y] : "").PadRight(width).ToCharArray()).ToArray();
        var wires = cables?.ToList() ?? [];
        for (int ty = 0; ty < height; ty++)
            for (int tx = 0; tx < width; tx++)
            {
                if (main[ty][tx] != 'T' || (tx > 0 && main[ty][tx - 1] == 'T') || (ty > 0 && main[ty - 1][tx] == 'T')) continue;
                if (below[ty + 1][tx + 2] != '-') throw new ArgumentException($"The tank at ({tx}, {ty}) is not piped rightwards along its bottom row: draw its chain by hand.");
                bool filled = below[ty][tx] == 'o' || below[ty][tx + 1] == 'o';
                main[ty][tx] = main[ty][tx + 1] = main[ty + 1][tx] = main[ty + 1][tx + 1] = 'G';
                below[ty][tx] = ' ';
                below[ty][tx + 1] = below[ty][tx + 2] = '-';
                below[ty + 1][tx + 1] = ' ';
                for (int y = ty + 1; y <= height + 2; y++) below[y][tx] = '-';
                main[height + 1][tx] = main[height + 1][tx + 1] = main[height + 2][tx] = main[height + 2][tx + 1] = 'T';
                if (filled) below[height + 1][tx + 1] = 'o';
                main[height + 1][tx + 2] = main[height + 1][tx + 3] = main[height + 1][tx + 4] = 'P';
                wires.Add(((tx + 2, height + 1), (tx, ty)));
            }
        return Layout.Parse([.. main.Select(r => new string(r))], [.. below.Select(r => new string(r))], wires);
    }

    /// <summary>The atmosphere's slice, to read what TypeScript's tests read of <c>w.sim.world.slices.atmosphere</c>.</summary>
    internal static AtmosphereState Air(this Scenario w) => (AtmosphereState)w.M.Atmosphere.Slice;

    /// <summary>The crew's slice, as TypeScript's tests read <c>w.sim.world.slices.crew</c>.</summary>
    internal static CrewState Crew(this Scenario w) => (CrewState)w.M.Crew.Slice;
}
