using System;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water as the shader reads it (<c>WaterLook</c>): a texel a cell of a box of the world, RGBA8. Red is the cell's amount
/// (0 to 255 over <c>Liquids.Full</c>), green set where it is part of a stream, blue where liquid lies over it (on a stream's
/// cell: where it is the trail over a drop), alpha where it is drawn. A stream is drawn whole: the cells water falls through
/// (open under them, not full there) and the empty cells between that a fall of <c>Liquids.Fall</c> a tick skips, each as wide
/// as the water falling past it a tick, so a steady stream reads as one even rope. A drop falling alone leaves a trail over
/// the cells it may have fallen through this tick, as wide as itself.
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
        bool any = false;
        for (int y = 0; y < tall; y++)
            for (int x = 0; x < wide; x++)
            {
                int gx = left + x, gy = top + y, at = (y * wide + x) * 4;
                texels[at] = texels[at + 1] = texels[at + 2] = texels[at + 3] = 0;
                if (gx < 0 || gy < 0 || gx >= width || gy >= height) continue;
                int cell = gy * width + gx, a = L.AmountOf(layer[cell]);
                bool stream = a > 0 ? Falls(layer, width, height, blocks, cell) : !blocks(cell) && Skipped(layer, width, height, blocks, cell);
                int? drop = a == 0 && !stream && !blocks(cell) ? DropUnder(layer, width, height, blocks, cell) : null;
                if (a == 0 && !stream && drop == null) continue;
                any = true;
                int units = drop is int under ? Falling(layer, width, height, blocks, under) : stream ? Falling(layer, width, height, blocks, cell) : a;
                texels[at] = (byte)Math.Min(255, units * 255 / L.Full);
                texels[at + 1] = stream || drop != null ? (byte)255 : (byte)0;
                texels[at + 2] = drop != null || !stream && gy > 0 && L.AmountOf(layer[cell - width]) > 0 ? (byte)255 : (byte)0;
                texels[at + 3] = 255;
            }
        return any;
    }

    /// <summary>Whether the liquid in <paramref name="cell"/> falls: open under it and not full there.</summary>
    private static bool Falls(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int cell) =>
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

    /// <summary>The falling cell within <c>Liquids.Fall</c> open cells under the empty <paramref name="cell"/>: whose trail it is.</summary>
    private static int? DropUnder(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int cell)
    {
        for (int k = 1, c = cell + width; k <= L.Fall && c < layer.Length && !blocks(c); k++, c += width)
            if (L.AmountOf(layer[c]) > 0) return Falls(layer, width, height, blocks, c) ? c : null;
        return null;
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
