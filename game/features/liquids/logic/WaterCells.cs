using System;
using System.Buffers;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water as the shader reads it (<c>WaterLook</c>), drawn as Terraria draws its liquid: a texel a cell of a box of the world,
/// RGBA8. Red is how high the cell's water stands, 0 to 255: its amount over <c>Liquids.Full</c>, or all of it where water is over
/// it. Green is set where it falls, blue where water is over it, alpha where it is drawn. The sim moves water a cell a tick, so
/// falling water is a column of cells; where it falls in strips a row or two apart, or in trickles a column apart, the gaps are
/// drawn as falling water too, so it reads whole.
/// </summary>
public static class WaterCells
{
    /// <summary>The most dry rows between falling water and the water over or under it that are drawn as falling water.</summary>
    public const int Gap = 2;

    /// <summary>
    /// How far past the box, down and to each side, falling is traced: a full column falls only over falling water, so whether it
    /// falls is known from its head. A column whose head lies further down, or a sheet wider, is drawn lying till nearer.
    /// </summary>
    public const int Down = 128, Side = 16;

    /// <summary>The world's liquid and walls, and which cells of the rows from <c>From</c> and the columns from <c>Left</c> fall.</summary>
    private readonly ref struct Cells(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, bool[] falls, int left, int from, int wide, int rows)
    {
        private readonly ReadOnlySpan<byte> _layer = layer;

        private bool In(int gx, int gy) => gx >= 0 && gy >= 0 && gx < width && gy < height;

        private int Amount(int gx, int gy) => L.AmountOf(_layer[gy * width + gx]);

        /// <summary>Whether water is in the cell: never in one that blocks, for the sim empties those the tick they close.</summary>
        public bool Wet(int gx, int gy) => In(gx, gy) && Amount(gx, gy) > 0;

        private bool Dry(int gx, int gy) => In(gx, gy) && Amount(gx, gy) == 0 && !blocks(gy * width + gx);

        public bool Falls(int gx, int gy)
        {
            int x = gx - left, y = gy - from;
            return x >= 0 && x < wide && y >= 0 && y < rows && falls[y * wide + x];
        }

        /// <summary>
        /// Whether the dry cell at (<paramref name="gx"/>, <paramref name="gy"/>) lies in falling water: within <see cref="Gap"/>
        /// dry rows of water over it and under it, one of them falling; or between falling water on either side.
        /// </summary>
        public bool Bridged(int gx, int gy) =>
            Between(gx, gy) || Beside(gx - 1, gy) && Beside(gx + 1, gy) && Dry(gx, gy);

        private bool Beside(int gx, int gy) => Wet(gx, gy) && Falls(gx, gy) || Between(gx, gy);

        private bool Between(int gx, int gy)
        {
            if (!In(gx, gy) || Amount(gx, gy) > 0) return false;
            // The water over and under looked for by the bytes alone; the walls, which cost a call, only where both are found.
            int up = gy - 1, down = gy + 1;
            while (up >= gy - Gap && In(gx, up) && Amount(gx, up) == 0) up--;
            while (down <= gy + Gap && In(gx, down) && Amount(gx, down) == 0) down++;
            if (!Wet(gx, up) || !Wet(gx, down) || !Falls(gx, up) && !Falls(gx, down)) return false;
            for (int y = up + 1; y < down; y++)
                if (blocks(y * width + gx)) return false;
            return true;
        }
    }

    /// <summary>
    /// Writes the box (<paramref name="left"/>, <paramref name="top"/>, <paramref name="wide"/> × <paramref name="tall"/> cells) of
    /// <paramref name="layer"/> into <paramref name="texels"/>, four bytes a cell; <paramref name="blocks"/> says which cells stop
    /// liquid. Returns whether any cell of it is drawn.
    /// </summary>
    public static bool Write(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int left, int top, int wide, int tall, byte[] texels)
    {
        int fromX = Math.Max(0, left - Side), toX = Math.Min(width - 1, left + wide - 1 + Side), across = Math.Max(0, toX - fromX + 1);
        int from = Math.Max(0, top - Gap - 1), to = Math.Min(height - 1, top + tall - 1 + Down), rows = Math.Max(0, to - from + 1);
        var falls = ArrayPool<bool>.Shared.Rent(Math.Max(1, across * rows));
        try
        {
            // From the bottom up, so a cell knows whether the water under it falls.
            for (int gy = to; gy >= from; gy--)
                for (int x = 0; x < across; x++)
                    falls[(gy - from) * across + x] = Falls(layer, width, height, blocks, falls, fromX, across, fromX + x, gy, gy < to ? (gy + 1 - from) * across : -1);
            var cells = new Cells(layer, width, height, blocks, falls, fromX, from, across, rows);
            bool any = false;
            for (int y = 0; y < tall; y++)
                for (int x = 0; x < wide; x++)
                {
                    int gx = left + x, gy = top + y, at = (y * wide + x) * 4;
                    texels[at] = texels[at + 1] = texels[at + 2] = texels[at + 3] = 0;
                    bool wet = cells.Wet(gx, gy), bridged = !wet && cells.Bridged(gx, gy);
                    if (!wet && !bridged) continue;
                    bool covered = cells.Wet(gx, gy - 1) || cells.Bridged(gx, gy - 1);
                    texels[at] = covered || bridged ? (byte)255 : (byte)(L.AmountOf(layer[gy * width + gx]) * 255 / L.Full);
                    texels[at + 1] = bridged || cells.Falls(gx, gy) ? (byte)255 : (byte)0;
                    texels[at + 2] = covered ? (byte)255 : (byte)0;
                    texels[at + 3] = 255;
                    any = true;
                }
            return any;
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(falls);
        }
    }

    /// <summary>
    /// Whether the water at (<paramref name="gx"/>, <paramref name="gy"/>) falls: open under it and short of full there, or over
    /// falling water with no water resting beside it (the row under it starts at <paramref name="belowAt"/> in
    /// <paramref name="falls"/>, -1 for none), so a full column falls as one but a pool over the hole it drains through lies.
    /// </summary>
    private static bool Falls(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, bool[] falls, int left, int wide, int gx, int gy, int belowAt)
    {
        if (gx < 0 || gy < 0 || gx >= width || gy + 1 >= height) return false;
        int cell = gy * width + gx;
        if (L.AmountOf(layer[cell]) == 0 || blocks(cell + width)) return false;
        if (L.AmountOf(layer[cell + width]) < L.Full) return true;
        return belowAt >= 0 && Fell(falls, belowAt, wide, gx - left)
            && Lets(layer, width, falls, belowAt, wide, left, cell - gx, gx - 1) && Lets(layer, width, falls, belowAt, wide, left, cell - gx, gx + 1);
    }

    private static bool Fell(bool[] falls, int rowAt, int wide, int x) => x >= 0 && x < wide && falls[rowAt + x];

    /// <summary>Whether the cell at column <paramref name="nx"/> of the row from <paramref name="row"/> holds no water, or water over falling water.</summary>
    private static bool Lets(ReadOnlySpan<byte> layer, int width, bool[] falls, int belowAt, int wide, int left, int row, int nx) =>
        nx < 0 || nx >= width || L.AmountOf(layer[row + nx]) == 0 || Fell(falls, belowAt, wide, nx - left);
}
