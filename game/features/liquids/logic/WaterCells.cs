using System;
using System.Buffers;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water as the shader reads it (<c>WaterLook</c>): a texel a cell of a box of the world, RGBA8. Red is the cell's amount
/// (0 to 255 over <c>Liquids.Full</c>), green set where it is part of a stream, blue where lying water is over it, alpha where it
/// is drawn. A stream is a lump in the air, the open cells between it and the water it fell from (a lump over it, the pool, the
/// ledge beside), and those down to the water it lands on, as wide as the lump. Everything else lies, drawn settled in its
/// column, as it will lie by the next tick.
/// </summary>
public static class WaterCells
{
    /// <summary>
    /// The most open rows between a lump in the air and the water it fell from that are drawn as its stream: two falls, since a
    /// ledge lets a lump out only each second tick (the sweep turns each tick). Farther, it is a drop.
    /// </summary>
    public const int Trail = 2 * L.Fall;

    /// <summary>
    /// Writes the box (<paramref name="left"/>, <paramref name="top"/>, <paramref name="wide"/> × <paramref name="tall"/> cells) of
    /// <paramref name="layer"/> into <paramref name="texels"/>, four bytes a cell; <paramref name="blocks"/> says which cells stop
    /// liquid. Returns whether any cell of it is drawn.
    /// </summary>
    public static bool Write(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int left, int top, int wide, int tall, byte[] texels)
    {
        Array.Clear(texels, 0, wide * tall * 4);
        // The rows a stream in the box reads: a trail over it, and the row under it.
        int from = Math.Max(0, top - Trail), to = Math.Min(height - 1, top + tall), rows = Math.Max(0, to - from + 1);
        var air = ArrayPool<bool>.Shared.Rent(Math.Max(1, wide * rows));
        try
        {
            Array.Clear(air, 0, wide * rows);
            var box = new Box(width, height, left, from, wide, rows);
            // From the bottom up, the lumps in the air: over an open, empty cell, or over one in the air with nothing beside
            // it resting (lumps falling one on another; not a pool over the hole it drains through).
            for (int gy = to; gy >= from; gy--)
                for (int x = 0; x < wide; x++)
                {
                    int gx = left + x, cell = gy * width + gx;
                    if (gx < 0 || gx >= width || blocks(cell) || L.AmountOf(layer[cell]) == 0) continue;
                    int under = cell + width;
                    bool inAir = gy + 1 < height && !blocks(under)
                        && (L.AmountOf(layer[under]) == 0 || box.Air(air, gx, gy + 1) && !Rests(layer, blocks, box, air, gx - 1, gy) && !Rests(layer, blocks, box, air, gx + 1, gy));
                    air[(gy - from) * wide + x] = inAir;
                    if (gy >= top && gy < top + tall) Put(texels, ((gy - top) * wide + x) * 4, (byte)(L.AmountOf(layer[cell]) * 255 / L.Full), inAir);
                }
            // From the top down, each lump's stream back up to the water it fell from, if that is within a trail.
            for (int x = 0; x < wide; x++)
            {
                int gx = left + x, source = -1;
                if (gx < 0 || gx >= width) continue;
                for (int gy = from; gy <= to && gy < top + tall; gy++)
                {
                    int cell = gy * width + gx;
                    if (blocks(cell))
                        source = -1;
                    else if (L.AmountOf(layer[cell]) > 0)
                    {
                        if (box.Air(air, gx, gy) && source >= 0 && gy - source <= Trail + 1)
                            for (int r = Math.Max(source + 1, top); r < gy; r++)
                                Put(texels, ((r - top) * wide + x) * 4, (byte)(L.AmountOf(layer[cell]) * 255 / L.Full), true);
                        source = gy;
                    }
                    else if (Rests(layer, blocks, box, air, gx - 1, gy) || Rests(layer, blocks, box, air, gx + 1, gy))
                        source = gy - 1;    // beside the pool or ledge it ran off: the stream starts here
                }
            }
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(air);
        }
        // From the top down, a stream drawn on down to the water it lands on within a fall.
        for (int y = 1; y < tall; y++)
            for (int x = 0; x < wide; x++)
            {
                int at = (y * wide + x) * 4, over = at - wide * 4, gx = left + x, gy = top + y;
                if (texels[at + 3] != 0 || texels[over + 1] == 0 || gx < 0 || gx >= width || gy >= height) continue;
                if (blocks(gy * width + gx) || !Lands(layer, width, height, blocks, gy * width + gx)) continue;
                Put(texels, at, texels[over], true);
            }
        bool any = false;
        for (int x = 0; x < wide; x++)
            if (left + x >= 0 && left + x < width) any |= Settle(layer, width, left + x, top, wide, tall, x, texels);
        return any;
    }

    /// <summary>The world's size and the rows the air flags cover, from <c>From</c>, <c>Wide</c> columns from <c>Left</c>.</summary>
    private readonly record struct Box(int Width, int Height, int Left, int From, int Wide, int Rows)
    {
        /// <summary>Whether the cell at (<paramref name="gx"/>, <paramref name="gy"/>) is a lump in the air; false off the rows read.</summary>
        public bool Air(bool[] air, int gx, int gy)
        {
            int x = gx - Left, y = gy - From;
            return x >= 0 && x < Wide && y >= 0 && y < Rows && air[y * Wide + x];
        }
    }

    private static void Put(byte[] texels, int at, byte red, bool stream)
    {
        texels[at] = red;
        texels[at + 1] = stream ? (byte)255 : (byte)0;
        texels[at + 3] = 255;
    }

    /// <summary>Whether liquid lies at (<paramref name="gx"/>, <paramref name="gy"/>): on a wall, or on liquid not in the air.</summary>
    private static bool Rests(ReadOnlySpan<byte> layer, Func<int, bool> blocks, Box box, bool[] air, int gx, int gy)
    {
        if (gx < 0 || gx >= box.Width || L.AmountOf(layer[gy * box.Width + gx]) == 0) return false;
        int under = (gy + 1) * box.Width + gx;
        if (gy + 1 >= box.Height || blocks(under)) return true;
        return L.AmountOf(layer[under]) > 0 && !box.Air(air, gx, gy + 1);
    }

    /// <summary>Whether liquid lies within a fall under the open <paramref name="cell"/>, through open cells.</summary>
    private static bool Lands(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int cell)
    {
        for (int k = 1, c = cell + width; k <= L.Fall && c < width * height && !blocks(c); k++, c += width)
            if (L.AmountOf(layer[c]) > 0) return true;
        return false;
    }

    /// <summary>
    /// The lying water of column <paramref name="x"/> of the box drawn as it settles: each run of lying cells one over another
    /// holds its units from the bottom up, a cell full before the one over it holds any. Whether any cell of the column is drawn.
    /// </summary>
    private static bool Settle(ReadOnlySpan<byte> layer, int width, int gx, int top, int wide, int tall, int x, byte[] texels)
    {
        bool any = false;
        for (int y = tall - 1; y >= 0;)
        {
            int at = (y * wide + x) * 4;
            if (texels[at + 3] == 0 || texels[at + 1] != 0)
            {
                any |= texels[at + 3] != 0;
                y--;
                continue;
            }
            int bottom = y, units = 0;
            for (; y >= 0 && texels[(y * wide + x) * 4 + 3] != 0 && texels[(y * wide + x) * 4 + 1] == 0; y--)
                units += L.AmountOf(layer[(top + y) * width + gx]);
            // Run up to the box's top, it may go on over it: water lying there covers it.
            bool coveredTop = y < 0 && top > 0 && L.AmountOf(layer[(top - 1) * width + gx]) > 0;
            for (int r = bottom, i = 0; r > y; r--, i++)
            {
                int on = (r * wide + x) * 4, u = Math.Clamp(units - i * L.Full, 0, L.Full);
                bool covered = units - (i + 1) * L.Full > 0 || r == y + 1 && coveredTop;
                texels[on] = (byte)(u * 255 / L.Full);
                texels[on + 2] = covered ? (byte)255 : (byte)0;
                texels[on + 3] = u > 0 ? (byte)255 : (byte)0;
                any |= u > 0;
            }
        }
        return any;
    }
}
