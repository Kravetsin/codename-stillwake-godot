using System;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water as the shader reads it (<c>WaterLook</c>): a texel a cell of a box of the world, RGBA8. Red is the cell's amount
/// (0 to 255 over <c>Liquids.Full</c>), green set where it is part of a stream, blue where lying water is over it, alpha where it
/// is drawn. A stream is a lump in the air and its trail: the open cells it fell through over it, up to <see cref="Trail"/>
/// rows or the water it left, and those down to the water it lands on, as wide as the lump. Everything else lies, drawn
/// settled in its column, as it will lie by the next tick.
/// </summary>
public static class WaterCells
{
    /// <summary>
    /// The rows a lump in the air trails over it: two falls, since a ledge lets a lump out only each second tick (the sweep
    /// turns each tick), so a steady stream's trails meet.
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
        Span<bool> air = wide <= 1024 ? stackalloc bool[wide] : new bool[wide];
        Span<bool> airUnder = wide <= 1024 ? stackalloc bool[wide] : new bool[wide];
        Span<byte> lump = wide <= 1024 ? stackalloc byte[wide] : new byte[wide];
        Span<int> trail = wide <= 1024 ? stackalloc int[wide] : new int[wide];
        // From the bottom up, so a lump's trail runs on up its column; from a trail under the box, so one rising into it is there.
        for (int gy = Math.Min(height - 1, top + tall - 1 + Trail); gy >= Math.Max(0, top); gy--)
        {
            int y = gy - top, row = gy * width;
            for (int x = 0; x < wide; x++)
            {
                int gx = left + x, at = (y * wide + x) * 4;
                air[x] = false;
                if (gx < 0 || gx >= width || blocks(row + gx))
                {
                    trail[x] = 0;
                    continue;
                }
                int cell = row + gx, a = L.AmountOf(layer[cell]);
                if (a > 0)
                {
                    // In the air: over an open, empty cell, or over water in the air with nothing beside it resting (lumps
                    // falling one on another; not a pool over the hole it drains through).
                    int under = cell + width;
                    air[x] = gy + 1 < height && !blocks(under)
                        && (L.AmountOf(layer[under]) == 0 || airUnder[x] && !Rests(layer, width, height, blocks, airUnder, left, row, gx - 1) && !Rests(layer, width, height, blocks, airUnder, left, row, gx + 1));
                    (lump[x], trail[x]) = air[x] ? ((byte)(a * 255 / L.Full), Trail) : ((byte)0, 0);
                    if (y < tall) Put(texels, at, (byte)(a * 255 / L.Full), air[x]);
                }
                else if (trail[x] > 0)
                {
                    // The trail ends where the water left from: the pool or ledge beside it.
                    trail[x] = Rests(layer, width, height, blocks, airUnder, left, row, gx - 1) || Rests(layer, width, height, blocks, airUnder, left, row, gx + 1) ? 0 : trail[x] - 1;
                    if (y < tall) Put(texels, at, lump[x], true);
                }
            }
            var done = air;
            air = airUnder;
            airUnder = done;
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

    private static void Put(byte[] texels, int at, byte red, bool stream)
    {
        texels[at] = red;
        texels[at + 1] = stream ? (byte)255 : (byte)0;
        texels[at + 3] = 255;
    }

    /// <summary>
    /// Whether liquid lies at column <paramref name="gx"/> of the row from <paramref name="row"/>: on a wall, or on liquid not
    /// in the air (<paramref name="airUnder"/>, the row under it, from the box's <paramref name="left"/>).
    /// </summary>
    private static bool Rests(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, ReadOnlySpan<bool> airUnder, int left, int row, int gx)
    {
        if (gx < 0 || gx >= width || L.AmountOf(layer[row + gx]) == 0) return false;
        int under = row + width + gx, x = gx - left;
        if (row / width + 1 >= height || blocks(under)) return true;
        return L.AmountOf(layer[under]) > 0 && !(x >= 0 && x < airUnder.Length && airUnder[x]);
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
