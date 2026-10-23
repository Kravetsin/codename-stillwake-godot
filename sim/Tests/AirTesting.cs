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

    /// <summary>The atmosphere's slice, to read what TypeScript's tests read of <c>w.sim.world.slices.atmosphere</c>.</summary>
    internal static AtmosphereState Air(this Scenario w) => (AtmosphereState)w.M.Atmosphere.Slice;

    /// <summary>The crew's slice, as TypeScript's tests read <c>w.sim.world.slices.crew</c>.</summary>
    internal static CrewState Crew(this Scenario w) => (CrewState)w.M.Crew.Slice;
}
