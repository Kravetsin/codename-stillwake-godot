using System;
using System.Collections.Generic;
using Stillwake.Content;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// Starlight cell by cell (0019, 0070): the stars shine in every open cell with no back wall and everywhere
/// past the grid's edge, as brightly as the sky round the cell is open — whole in open space, faint in a
/// lone hole in a back wall (<see cref="L.StarHole"/>) — and spread from there in sweeps along every row and
/// column, keeping <see cref="L.StarKeep"/> a cell, so the light through a hole or a doorway falls off within
/// a few cells. Worked out over <see cref="Region"/> only, what is built and <see cref="Margin"/> cells of
/// space round it: past what is built every cell is open and holds the stars whole, the light of the
/// region's edge, as it did over the whole world. After a change it is worked out again only around the
/// cells changed (<see cref="Follow"/>), within <see cref="Reach"/> and the sky's reach of them: past it,
/// what a change does to the light rounds to nothing. Plain C#, so it is tested.
/// </summary>
public sealed class StarField
{
    /// <summary>Cells of open space worked out round what is built, so its edge is lit as the space beyond it: its sky open whole.</summary>
    public const int Margin = 4;
    /// <summary>How far round a cell its sky is counted: <see cref="L.StarSky"/>, within the margin.</summary>
    private const int Sky = L.StarSky < Margin ? L.StarSky : Margin;
    private const byte Air = 0, Machine = 1, Wall = 2;

    /// <summary>The colour of the light through the open sky: the stars', or a planet's daylight (0076).</summary>
    private readonly (float R, float G, float B) _sky;

    /// <param name="sky">The light through the open sky, by <see cref="L.SkyOf"/> its surface's kind; the stars' when not given.</param>
    public StarField((float R, float G, float B)? sky = null) => _sky = sky ?? L.Star;

    /// <summary>The cells worked out, in the grid's cells, past its edge too.</summary>
    public Box Region { get; private set; } = Box.None;

    /// <summary>
    /// Four bytes a cell of <see cref="Region"/>, row by row: the stars' colour by the cell's share of
    /// their light, a wall's that of its face, and an alpha whole in a wall's or closed door's cell.
    /// </summary>
    public byte[] Texels { get; private set; } = [];

    private byte[] _kind = [];
    /// <summary>Scratch: running sums of the cells round the box the stars shine into, to count each cell's sky.</summary>
    private int[] _skySum = [];
    /// <summary>Each cell's share of the stars' light: the stars are the one colour it comes in.</summary>
    private float[] _light = [];

    /// <summary>Light this faint, or fainter, is 0 in every byte of a texel; a hundredth under, against rounding.</summary>
    private static readonly double Faint = 0.99 * Math.Min(1, L.WallIn) / (L.BrightestSky * 255);

    /// <summary>The share of light going on from a cell of one kind into the next, by <c>from × 3 + to</c>.</summary>
    private static readonly float[] Shares = SharesOf();

    /// <summary>A cell's share of the stars' light by how many cells of its sky, itself among them, the stars shine into.</summary>
    private static readonly float[] SkyLight = SkyLightOf();

    /// <summary>
    /// How many cells from a change its light may still show: past that, light that crossed the changed
    /// cell keeps under <see cref="Faint"/> of itself, so every texel there rounds as it did. Light keeps
    /// at most the brightest way through cells of any kinds, step by step; no texel shows light below
    /// <see cref="Faint"/>, a wall's face, which shows light at its strongest, included. No reach when a
    /// way keeps it all.
    /// </summary>
    public static readonly int Reach = ReachOf();

    private static int ReachOf()
    {
        // A float a step keeps a ten-millionth more than the share at most; the margin covers it many times.
        double[] best = [1, 1, 1];
        int reach = 0;
        while (Math.Max(best[0], Math.Max(best[1], best[2])) >= Faint * 0.999)
        {
            if (++reach > 10_000) return int.MaxValue;
            var next = new double[3];
            for (int from = 0; from < 3; from++)
                for (int to = 0; to < 3; to++) next[to] = Math.Max(next[to], best[from] * Shares[from * 3 + to]);
            best = next;
        }
        return reach;
    }

    private static float[] SkyLightOf()
    {
        int most = (2 * Sky + 1) * (2 * Sky + 1);
        var light = new float[most + 1];
        for (int open = 1; open <= most; open++)
            light[open] = L.StarHole + (1 - L.StarHole) * Math.Min(1f, (open - 1f) / Math.Max(1, L.StarFullSky - 1));
        return light;
    }

    /// <summary>Works the starlight out anew for the grid as it stands, over what is built and the margin; with nothing built, over a little open space.</summary>
    public void Work(IGridQueries grid) => Work(grid, RegionOf(grid));

    private static Box RegionOf(IGridQueries grid)
    {
        var b = grid.Built;
        return new Box(b.Left - Margin, b.Top - Margin, b.Right + Margin, b.Bottom + Margin);
    }

    /// <summary>Works the starlight out over <paramref name="region"/>, which must hold everything built and a cell of space round it.</summary>
    public void Work(IGridQueries grid, Box region)
    {
        Region = region;
        int n = region.Width * region.Height;
        if (Texels.Length != n * 4) Texels = new byte[n * 4];
        Compute(grid, region, region, machinesListed: true);
    }

    /// <summary>
    /// Brings the starlight up to the grid after <paramref name="cells"/> changed what stops or dims light or
    /// lets the stars in (structure, back walls, machines), and what is built with them: worked out again
    /// only within <see cref="Reach"/> and the sky's reach of them, from what lies within <see cref="Reach"/>
    /// more, where all light that can reach them starts. The texels are the same as <see cref="Work"/> gives.
    /// </summary>
    public void Follow(IGridQueries grid, IReadOnlyList<int> cells)
    {
        var region = RegionOf(grid);
        if (Texels.Length == 0 || Reach == int.MaxValue)
        {
            Work(grid, region);
            return;
        }
        if (region != Region) Move(region);
        // A change alters the sky of cells within Sky of it, whose light shows within Reach of them; that light
        // starts within Reach again. Changes near each other are worked out together: a box each, grown by each cell within that spread.
        var boxes = new List<Box>();
        int w = grid.Width, shows = Reach + Sky, spread = 2 * Reach + Sky;
        foreach (int cell in cells)
        {
            int x = cell % w, y = cell / w, near = -1;
            for (int i = 0; i < boxes.Count && near < 0; i++)
            {
                var b = boxes[i];
                if (x >= b.Left - spread && x <= b.Right + spread && y >= b.Top - spread && y <= b.Bottom + spread) near = i;
            }
            if (near < 0) boxes.Add(new Box(x, y, x, y));
            else boxes[near] = boxes[near].Union(new Box(x, y, x, y));
        }
        long worked = 0;
        foreach (var b in boxes) worked += (long)Within(Grown(b, spread), region).Width * Within(Grown(b, spread), region).Height;
        if (worked >= (long)region.Width * region.Height)
        {
            Work(grid, region);
            return;
        }
        foreach (var b in boxes)
        {
            var write = Within(Grown(b, shows), region);
            if (!write.IsEmpty) Compute(grid, Within(Grown(b, spread), region), write, machinesListed: false);
        }
    }

    private static Box Grown(Box b, int by) => new(b.Left - by, b.Top - by, b.Right + by, b.Bottom + by);

    private static Box Within(Box b, Box region)
    {
        var r = new Box(Math.Max(b.Left, region.Left), Math.Max(b.Top, region.Top), Math.Min(b.Right, region.Right), Math.Min(b.Bottom, region.Bottom));
        return r.IsEmpty ? Box.None : r;
    }

    /// <summary>
    /// The texels laid over <paramref name="region"/> now, kept where it overlaps the last one, and every other
    /// cell open stars at full: a cell not built then or since, which the stars light whole.
    /// </summary>
    private void Move(Box region)
    {
        var old = Region;
        var was = Texels;
        int n = region.Width * region.Height;
        var texels = new byte[n * 4];
        var open = OpenTexel();
        for (int i = 0; i < n; i++) open.CopyTo(texels, i * 4);
        var both = Within(old, region);
        for (int y = both.Top; y <= both.Bottom; y++)
            Array.Copy(was, ((y - old.Top) * old.Width + both.Left - old.Left) * 4,
                texels, ((y - region.Top) * region.Width + both.Left - region.Left) * 4, both.Width * 4);
        (Region, Texels) = (region, texels);
    }

    /// <summary>An open cell's texel with the stars whole in it.</summary>
    private byte[] OpenTexel()
    {
        var bytes = new byte[4];
        Texel(bytes, 0, 1, Air);
        return bytes;
    }

    /// <summary>
    /// Works the light out over <paramref name="box"/>, a part of <see cref="Region"/>, as if nothing lay past
    /// its sides, and writes the texels of the cells in <paramref name="write"/>, which it holds.
    /// </summary>
    private void Compute(IGridQueries grid, Box box, Box write, bool machinesListed)
    {
        int w = box.Width, h = box.Height, n = w * h;
        if (_kind.Length < n) (_kind, _light) = (new byte[n], new float[n]);
        var kind = _kind;
        var light = _light;
        Array.Clear(kind, 0, n);
        Array.Clear(light, 0, n);
        ReadOnlySpan<int> structure = grid.StructureLayer, back = grid.BackLayer, shape = grid.ShapeLayer;
        if (machinesListed)
        {
            // A machine dims the light through every cell of its footprint; over the region, every machine is in it.
            foreach (var m in grid.Machines)
            {
                var spec = Machines.Get(m.Kind);
                for (int dy = 0; dy < spec.Height; dy++)
                    for (int dx = 0; dx < spec.Width; dx++)
                        kind[(m.Cell / grid.Width + dy - box.Top) * w + m.Cell % grid.Width + dx - box.Left] = Machine;
            }
        }
        // The sky: the cells the stars shine into, counted round each cell of the box from the grid itself, past the box too.
        int sw = w + 2 * Sky + 1, sh = h + 2 * Sky + 1, gw = grid.Width, gh = grid.Height;
        if (_skySum.Length < sw * sh) _skySum = new int[sw * sh];
        var sum = _skySum;
        Array.Clear(sum, 0, sw);
        for (int y = 1; y < sh; y++)
        {
            int gy = y - 1 + box.Top - Sky, row = 0;
            sum[y * sw] = 0;
            for (int x = 1; x < sw; x++)
            {
                int gx = x - 1 + box.Left - Sky;
                row += Starry(structure, back, shape, gw, gh, gx, gy) ? 1 : 0;
                sum[y * sw + x] = sum[(y - 1) * sw + x] + row;
            }
        }
        int side = 2 * Sky + 1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x, gx = x + box.Left, gy = y + box.Top;
                bool inside = gx >= 0 && gy >= 0 && gx < grid.Width && gy < grid.Height;
                int cell = gy * grid.Width + gx;
                if (!machinesListed && inside && grid.MachineAt(cell) != null) kind[i] = Machine;
                if (inside && structure[cell] is Structure.Wall or Structure.DoorClosed) kind[i] = Wall;
                // Its own cell's count, from the sums: whether the stars shine into it.
                int c = (y + Sky + 1) * sw + x + Sky + 1;
                if (sum[c] - sum[c - 1] - sum[c - sw] + sum[c - sw - 1] == 0) continue;
                int open = sum[(y + side) * sw + x + side] - sum[y * sw + x + side] - sum[(y + side) * sw + x] + sum[y * sw + x];
                light[i] = SkyLight[open];
            }
        Spread(light, kind, w, h);
        var bytes = Texels;
        var region = Region;
        for (int y = write.Top; y <= write.Bottom; y++)
        {
            int i = (y - box.Top) * w + write.Left - box.Left, at = ((y - region.Top) * region.Width + write.Left - region.Left) * 4;
            for (int x = write.Left; x <= write.Right; x++, i++, at += 4) Texel(bytes, at, light[i], kind[i]);
        }
    }

    /// <summary>
    /// Whether the stars shine into a cell: an open one with no back wall, any past the grid's edge, and a shaped
    /// wall's open half with no back wall behind it, a wall still for what crosses it (0069).
    /// </summary>
    private static bool Starry(ReadOnlySpan<int> structure, ReadOnlySpan<int> back, ReadOnlySpan<int> shape, int width, int height, int gx, int gy)
    {
        if (gx < 0 || gy < 0 || gx >= width || gy >= height) return true;
        int cell = gy * width + gx;
        return back[cell] == Back.None && (structure[cell] is not (Structure.Wall or Structure.DoorClosed) || shape[cell] != Shape.None);
    }

    private void Texel(byte[] bytes, int at, float light, byte kind)
    {
        var (starR, starG, starB) = _sky;
        // Walls are drawn taking WallIn of the light on their face, so a wall cell holds its face's light.
        float face = kind == Wall ? light / L.WallIn : light;
        bytes[at] = (byte)(Math.Min(1, starR * face) * 255);
        bytes[at + 1] = (byte)(Math.Min(1, starG * face) * 255);
        bytes[at + 2] = (byte)(Math.Min(1, starB * face) * 255);
        // Its alpha says whether it is a wall's, so the shader blends it only with its own side.
        bytes[at + 3] = kind == Wall ? (byte)255 : (byte)0;
    }

    private static float[] SharesOf()
    {
        float[] into = [L.StarKeep, L.StarMachine];
        var shares = new float[9];
        for (int from = 0; from < 3; from++)
            for (int to = 0; to < 3; to++)
                shares[from * 3 + to] = to == Wall
                    ? from == Wall ? L.WallDeep : L.WallIn
                    : (from == Wall ? L.WallOut : 1) * into[to];
        return shares;
    }

    /// <summary>
    /// Terraria's sweeps (lightOf in cells.ts): each cell takes its neighbour's light times what crossing
    /// keeps, the brightest way winning. One channel: every share is the same for red, green and blue.
    /// Open space stays whole, so a cell beside the region's edge sees what it would beside more space.
    /// </summary>
    private static void Spread(float[] light, byte[] kind, int w, int h)
    {
        var shares = Shares;
        int n = w * h;
        for (int round = 0; round < L.Rounds; round++)
        {
            for (int row = 0; row < n; row += w)
            {
                for (int to = row + 1; to < row + w; to++)
                {
                    float v = light[to - 1];
                    if (v == 0) continue;
                    v *= shares[kind[to - 1] * 3 + kind[to]];
                    if (v > light[to]) light[to] = v;
                }
                for (int to = row + w - 2; to >= row; to--)
                {
                    float v = light[to + 1];
                    if (v == 0) continue;
                    v *= shares[kind[to + 1] * 3 + kind[to]];
                    if (v > light[to]) light[to] = v;
                }
            }
            // Every column down, then up, a row at a time: the same order within each column, in memory order.
            for (int to = w; to < n; to++)
            {
                float v = light[to - w];
                if (v == 0) continue;
                v *= shares[kind[to - w] * 3 + kind[to]];
                if (v > light[to]) light[to] = v;
            }
            for (int to = n - w - 1; to >= 0; to--)
            {
                float v = light[to + w];
                if (v == 0) continue;
                v *= shares[kind[to + w] * 3 + kind[to]];
                if (v > light[to]) light[to] = v;
            }
        }
    }
}
