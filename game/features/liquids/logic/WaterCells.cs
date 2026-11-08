using System;
using System.Buffers;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water as the shader reads it (<c>WaterLook</c>): a texel a cell of a box of the world, RGBA8. Red is the cell's amount
/// (0 to 255 over <c>Liquids.Full</c>), green set where it is part of a stream, blue where lying water is over it, alpha where it
/// is drawn. A stream is water in the air: over an open, empty cell, or alone in its row over water in the air (lumps falling
/// one on another); where such a fall lands; and the empty cells between that a fall of <c>Liquids.Fall</c> a tick skips; each as wide as the
/// water falling past it a tick, so a steady stream reads as one even rope. Lying water is drawn settled in its column, as it
/// will lie by the next tick: water just spread over a pool's surface swells it rather than standing on it.
/// </summary>
public static class WaterCells
{
    /// <summary>
    /// The rows above a cell, and as many from it down, whose falling units make a stream's width there: a whole number of
    /// falls, so a steady stream reads the same at every cell.
    /// </summary>
    public const int Window = L.Fall;

    /// <summary>The rows over and under the box whose cells in the air a stream in it reads: a window, and a fall over that.</summary>
    private const int Margin = Window + L.Fall;

    /// <summary>The world's liquid and walls, and which cells of a box round the drawn one are in the air.</summary>
    private readonly ref struct Cells(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, bool[] air, int left, int top, int wide, int tall)
    {
        private readonly ReadOnlySpan<byte> _layer = layer;

        public int Width => width;

        public int Amount(int cell) => L.AmountOf(_layer[cell]);

        public bool Blocks(int cell) => blocks(cell);

        public bool Inside(int cell) => cell >= 0 && cell < _layer.Length;

        /// <summary>Whether liquid in <paramref name="cell"/> may fall: open under it and not full there.</summary>
        public bool Open(int cell) => cell / width + 1 < height && !blocks(cell + width) && Amount(cell + width) < L.Full;

        /// <summary>Whether <paramref name="cell"/> holds liquid in the air (<see cref="InAir"/>).</summary>
        public bool Air(int cell)
        {
            int x = cell % width - left, y = cell / width - top;
            if (x >= 0 && x < wide && y >= 0 && y < tall) return air[y * wide + x];
            return Amount(cell) > 0 && (Empty(cell + width) || Alone(cell) && Passes(cell + width));
        }

        /// <summary>
        /// Whether liquid falls through <paramref name="cell"/>: open, and empty, or liquid in the air. Liquid is in the air over
        /// an open, empty cell, or alone in its row (nothing beside it) over liquid in the air: a lump falling onto another, not a
        /// pool over the hole it drains through.
        /// </summary>
        public bool Passes(int cell)
        {
            for (; Inside(cell) && !blocks(cell); cell += width)
            {
                if (Amount(cell) == 0) return true;
                if (!Alone(cell)) return Empty(cell + width);
            }
            return false;
        }

        /// <summary>Whether <paramref name="cell"/> is open and holds nothing.</summary>
        public bool Empty(int cell) => Inside(cell) && !blocks(cell) && Amount(cell) == 0;

        /// <summary>Whether no liquid lies beside <paramref name="cell"/> in its row.</summary>
        public bool Alone(int cell)
        {
            int x = cell % width;
            return (x == 0 || Amount(cell - 1) == 0) && (x + 1 == width || Amount(cell + 1) == 0);
        }
    }

    /// <summary>
    /// Writes the box (<paramref name="left"/>, <paramref name="top"/>, <paramref name="wide"/> × <paramref name="tall"/> cells) of
    /// <paramref name="layer"/> into <paramref name="texels"/>, four bytes a cell; <paramref name="blocks"/> says which cells stop
    /// liquid. Returns whether any cell of it is drawn.
    /// </summary>
    public static bool Write(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int left, int top, int wide, int tall, byte[] texels)
    {
        int from = Math.Max(0, top - Margin), to = Math.Min(height, top + tall + Margin), rows = Math.Max(0, to - from);
        var air = ArrayPool<bool>.Shared.Rent(Math.Max(1, wide * rows));
        try
        {
            var cells = new Cells(layer, width, height, blocks, air, left, from, wide, rows);
            FindAir(cells, height, left, from, wide, rows, air);
            for (int y = 0; y < tall; y++)
                for (int x = 0; x < wide; x++)
                {
                    int gx = left + x, gy = top + y, at = (y * wide + x) * 4;
                    texels[at] = texels[at + 1] = texels[at + 2] = texels[at + 3] = 0;
                    if (gx < 0 || gy < 0 || gx >= width || gy >= height) continue;
                    int cell = gy * width + gx, a = cells.Amount(cell);
                    bool stream = a > 0 ? Falls(cells, cell) : !blocks(cell) && Skipped(cells, cell);
                    if (a == 0 && !stream) continue;
                    texels[at] = (byte)Math.Min(255, (stream ? Falling(cells, cell) : a) * 255 / L.Full);
                    texels[at + 1] = stream ? (byte)255 : (byte)0;
                    texels[at + 3] = 255;
                }
            bool any = false;
            for (int x = 0; x < wide; x++)
                if (left + x >= 0 && left + x < width) any |= Settle(layer, width, left + x, top, wide, tall, x, texels);
            return any;
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(air);
        }
    }

    /// <summary>Which cells of the rows from <paramref name="top"/> are in the air, each column from the bottom up.</summary>
    private static void FindAir(Cells cells, int height, int left, int top, int wide, int tall, bool[] air)
    {
        int width = cells.Width;
        for (int x = 0; x < wide; x++)
        {
            int gx = left + x;
            if (gx < 0 || gx >= width)
            {
                for (int y = 0; y < tall; y++) air[y * wide + x] = false;
                continue;
            }
            bool passes = top + tall < height && cells.Passes((top + tall) * width + gx);
            for (int y = tall - 1; y >= 0; y--)
            {
                int cell = (top + y) * width + gx, a = cells.Amount(cell);
                bool inAir = a > 0 && passes && (cells.Empty(cell + width) || cells.Alone(cell));
                air[y * wide + x] = inAir;
                passes = !cells.Blocks(cell) && (a == 0 || inAir);
            }
        }
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

    /// <summary>Whether the liquid in <paramref name="cell"/> falls: in the air, or where water in the air within a fall lands.</summary>
    private static bool Falls(Cells cells, int cell)
    {
        if (cells.Amount(cell) == 0) return false;
        if (cells.Air(cell)) return true;
        if (!cells.Open(cell)) return false;
        for (int k = 1, c = cell - cells.Width; k <= L.Fall && c >= 0 && !cells.Blocks(c); k++, c -= cells.Width)
            if (cells.Air(c)) return true;
        return false;
    }

    /// <summary>
    /// Whether the empty <paramref name="cell"/> lies inside a stream: water falls in an open cell above it, and lies below it,
    /// each within <c>Liquids.Fall</c> open cells.
    /// </summary>
    private static bool Skipped(Cells cells, int cell)
    {
        bool above = false, below = false;
        int width = cells.Width;
        for (int k = 1, c = cell - width; k <= L.Fall && c >= 0 && !cells.Blocks(c) && !above; k++, c -= width)
            above = cells.Amount(c) > 0 && Falls(cells, c);
        for (int k = 1, c = cell + width; k <= L.Fall && cells.Inside(c) && !cells.Blocks(c) && above && !below; k++, c += width)
            below = cells.Amount(c) > 0;
        return above && below;
    }

    /// <summary>
    /// The units falling past <paramref name="cell"/> a tick: those falling in its column within <see cref="Window"/> rows of it,
    /// over the rows there the stream runs through (open, and empty or falling; not the pool it pours from or into), times
    /// the <c>Liquids.Fall</c> cells they fall a tick.
    /// </summary>
    private static int Falling(Cells cells, int cell)
    {
        int units = 0, rows = 0;
        for (int k = -Window; k < Window; k++)
        {
            int c = cell + k * cells.Width;
            if (!cells.Inside(c) || cells.Blocks(c)) continue;
            int a = cells.Amount(c);
            if (a > 0 && !Falls(cells, c)) continue;
            units += a;
            rows++;
        }
        return rows == 0 ? 0 : units * L.Fall / rows;
    }
}
