using System;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water as the shader reads it (<c>WaterLook</c>): a texel a cell of a box of the world, RGBA8. Red is the cell's amount
/// (0 to 255 over <c>Liquids.Full</c>), green set where it is part of a stream, blue where lying water is over it, alpha where it
/// is drawn. A stream is water in the air: a cell over an open, empty one (through liquid short of full below it), where such a
/// fall lands, and the empty cells between that a fall of <c>Liquids.Fall</c> a tick skips, each as wide as the water falling past
/// it a tick, so a steady stream reads as one even rope. Lying water is drawn settled in its column, as it will lie by the next
/// tick: water just spread over a pool's surface swells it rather than standing on it.
/// </summary>
public static class WaterCells
{
    /// <summary>
    /// The rows above a cell, and as many from it down, whose falling units make a stream's width there: a whole number of
    /// falls, so a steady stream reads the same at every cell.
    /// </summary>
    public const int Window = L.Fall;

    /// <summary>
    /// Writes the box (<paramref name="left"/>, <paramref name="top"/>, <paramref name="wide"/> × <paramref name="tall"/> cells) of
    /// <paramref name="layer"/> into <paramref name="texels"/>, four bytes a cell; <paramref name="blocks"/> says which cells stop
    /// liquid. Returns whether any cell of it is drawn.
    /// </summary>
    public static bool Write(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int left, int top, int wide, int tall, byte[] texels)
    {
        for (int y = 0; y < tall; y++)
            for (int x = 0; x < wide; x++)
            {
                int gx = left + x, gy = top + y, at = (y * wide + x) * 4;
                texels[at] = texels[at + 1] = texels[at + 2] = texels[at + 3] = 0;
                if (gx < 0 || gy < 0 || gx >= width || gy >= height) continue;
                int cell = gy * width + gx, a = L.AmountOf(layer[cell]);
                bool stream = a > 0 ? Falls(layer, width, height, blocks, cell) : !blocks(cell) && Skipped(layer, width, height, blocks, cell);
                if (a == 0 && !stream) continue;
                texels[at] = (byte)Math.Min(255, (stream ? Falling(layer, width, height, blocks, cell) : a) * 255 / L.Full);
                texels[at + 1] = stream ? (byte)255 : (byte)0;
                texels[at + 3] = 255;
            }
        bool any = false;
        for (int x = 0; x < wide; x++)
            if (left + x >= 0 && left + x < width) any |= Settle(layer, width, left + x, top, wide, tall, x, texels);
        return any;
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

    /// <summary>
    /// Whether the liquid in <paramref name="cell"/> is in the air, falling: open under it and not full there, and either an
    /// open, empty cell under it (through liquid short of full) or water falling through the air onto it within a fall.
    /// </summary>
    private static bool Falls(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int cell)
    {
        if (L.AmountOf(layer[cell]) == 0 || !Open(layer, width, height, blocks, cell)) return false;
        for (int k = 1, c = cell + width; k <= L.Fall && c < layer.Length && !blocks(c); k++, c += width)
        {
            int a = L.AmountOf(layer[c]);
            if (a == 0) return true;
            if (a == L.Full) break;
        }
        for (int k = 1, c = cell - width; k <= L.Fall && c >= 0 && !blocks(c); k++, c -= width)
            if (L.AmountOf(layer[c]) > 0 && Open(layer, width, height, blocks, c) && L.AmountOf(layer[c + width]) == 0) return true;
        return false;
    }

    /// <summary>Whether liquid in <paramref name="cell"/> may fall: open under it and not full there.</summary>
    private static bool Open(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int cell) =>
        cell / width + 1 < height && !blocks(cell + width) && L.AmountOf(layer[cell + width]) < L.Full;

    /// <summary>
    /// Whether the empty <paramref name="cell"/> lies inside a stream: water falls in an open cell above it, and lies below it,
    /// each within <c>Liquids.Fall</c> open cells.
    /// </summary>
    private static bool Skipped(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int cell)
    {
        bool above = false, below = false;
        for (int k = 1, c = cell - width; k <= L.Fall && c >= 0 && !blocks(c) && !above; k++, c -= width)
            above = L.AmountOf(layer[c]) > 0 && Falls(layer, width, height, blocks, c);
        for (int k = 1, c = cell + width; k <= L.Fall && c < layer.Length && !blocks(c) && above && !below; k++, c += width)
            below = L.AmountOf(layer[c]) > 0;
        return above && below;
    }

    /// <summary>
    /// The units falling past <paramref name="cell"/> a tick: those falling in its column within <see cref="Window"/> rows of it,
    /// over the rows there the stream runs through (open, and empty or falling; not the pool it pours from or into), times
    /// the <c>Liquids.Fall</c> cells they fall a tick.
    /// </summary>
    private static int Falling(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int cell)
    {
        int units = 0, rows = 0;
        for (int k = -Window; k < Window; k++)
        {
            int c = cell + k * width;
            if (c < 0 || c >= layer.Length || blocks(c)) continue;
            int a = L.AmountOf(layer[c]);
            if (a > 0 && !Falls(layer, width, height, blocks, c)) continue;
            units += a;
            rows++;
        }
        return rows == 0 ? 0 : units * L.Fall / rows;
    }
}
