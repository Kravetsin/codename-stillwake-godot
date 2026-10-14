using System;
using System.Numerics;
using Stillwake.Content;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// Light by cells as the TypeScript game spread it (0019, cells.ts): what each cell is to light, where
/// the stars shine in, and Terraria's sweeps. Starlight runs it over the whole grid when the ship
/// changes; the cells mode (0026) over the screen, every frame. A cell's light is red, green and blue in
/// a <see cref="Vector4"/>, its fourth number free, so each step of a sweep works on the three at once.
/// </summary>
public static class Sweeps
{
    /// <summary>What light does in a cell: crosses air, crosses a machine, goes into a wall or a closed door.</summary>
    public const byte Air = 0, Machine = 1, Wall = 2, Door = 3;
    private const int Kinds = 4;

    /// <summary>Whether a cell of <paramref name="kind"/> blocks light: drawn from the wall side of the lit shader.</summary>
    public static bool Blocks(byte kind) => kind >= Wall;

    /// <summary>
    /// What each cell of the stretch from (<paramref name="x"/>, <paramref name="y"/>), <paramref name="width"/>
    /// by <paramref name="height"/>, is to light, and <paramref name="star"/> where the stars shine in: every
    /// open cell with no back wall and everywhere past the grid's edge; no light elsewhere. A machine dims the
    /// light through every cell of its footprint.
    /// </summary>
    public static void Read(IGridQueries grid, int x, int y, int width, int height, Span<byte> kind, Span<Vector4> light, Vector4 star)
    {
        kind[..(width * height)].Clear();
        light[..(width * height)].Clear();
        int gw = grid.Width, gh = grid.Height;
        var machines = grid.Machines;
        for (int k = 0; k < machines.Count; k++)
        {
            var m = machines[k];
            int mx = m.Cell % gw - x, my = m.Cell / gw - y;
            // Most machines of a big ship lie off screen: passed over before their footprint is looked up.
            if (mx >= width || my >= height || mx + Biggest <= 0 || my + Biggest <= 0) continue;
            var spec = Machines.Get(m.Kind);
            for (int dy = Math.Max(0, -my); dy < spec.Height && my + dy < height; dy++)
                for (int dx = Math.Max(0, -mx); dx < spec.Width && mx + dx < width; dx++)
                    kind[(my + dy) * width + mx + dx] = Machine;
        }
        ReadOnlySpan<int> structure = grid.StructureLayer, back = grid.BackLayer;
        for (int j = 0; j < height; j++)
        {
            int gy = y + j, row = j * width;
            for (int i = 0; i < width; i++)
            {
                int gx = x + i;
                if (gx < 0 || gy < 0 || gx >= gw || gy >= gh)
                {
                    light[row + i] = star;
                    continue;
                }
                int cell = gy * gw + gx;
                int s = structure[cell];
                if (s == Structure.Wall) kind[row + i] = Wall;
                else if (s == Structure.DoorClosed) kind[row + i] = Door;
                else if (back[cell] == Back.None) light[row + i] = star;
            }
        }
    }

    /// <summary>The most cells a machine reaches across or down.</summary>
    private static readonly int Biggest = BiggestOf();

    private static int BiggestOf()
    {
        int most = 1;
        foreach (var spec in Machines.All) most = Math.Max(most, Math.Max(spec.Width, spec.Height));
        return most;
    }

    /// <summary>The share of light going on from a cell of one kind into the next, by <c>from × 4 + to</c>.</summary>
    private static readonly float[] Shares = SharesOf();

    private static float[] SharesOf()
    {
        float[] into = [L.Air, L.Machine];
        var shares = new float[Kinds * Kinds];
        for (int from = 0; from < Kinds; from++)
            for (int to = 0; to < Kinds; to++)
                shares[from * Kinds + to] = Blocks((byte)to)
                    ? Blocks((byte)from) ? L.WallDeep : to == Door ? L.DoorIn : L.WallIn
                    : (Blocks((byte)from) ? L.WallOut : 1) * into[to];
        return shares;
    }


    /// <summary>
    /// Terraria's sweeps (lightOf in cells.ts): along every row each way, then every column each way,
    /// <see cref="L.Rounds"/> times, each cell taking its neighbour's light times what crossing into it
    /// keeps, the brightest way winning, colour by colour.
    /// </summary>
    public static void Spread(Span<Vector4> light, ReadOnlySpan<byte> kind, int w, int h)
    {
        var shares = Shares;
        int n = w * h;
        for (int round = 0; round < L.Rounds; round++)
        {
            for (int row = 0; row < n; row += w)
            {
                for (int to = row + 1; to < row + w; to++)
                    light[to] = Vector4.Max(light[to], light[to - 1] * shares[kind[to - 1] * Kinds + kind[to]]);
                for (int to = row + w - 2; to >= row; to--)
                    light[to] = Vector4.Max(light[to], light[to + 1] * shares[kind[to + 1] * Kinds + kind[to]]);
            }
            // Every column down, then up, a row at a time: the same order within each column, in memory order.
            for (int to = w; to < n; to++)
                light[to] = Vector4.Max(light[to], light[to - w] * shares[kind[to - w] * Kinds + kind[to]]);
            for (int to = n - w - 1; to >= 0; to--)
                light[to] = Vector4.Max(light[to], light[to + w] * shares[kind[to + w] * Kinds + kind[to]]);
        }
    }
}
