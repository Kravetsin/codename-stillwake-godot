using System;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water as the shader reads it (<c>WaterLook</c>): a texel a cell of a box of the world, RGBA8 — red the cell's amount
/// (0 to 255 over <c>Liquids.Full</c>), green set where it falls (open under it and not full there), blue where the cell
/// over it holds liquid too, alpha where it holds any.
/// </summary>
public static class WaterCells
{
    /// <summary>
    /// Writes the box (<paramref name="left"/>, <paramref name="top"/>, <paramref name="wide"/> × <paramref name="tall"/> cells) of
    /// <paramref name="layer"/> into <paramref name="texels"/>, four bytes a cell; <paramref name="blocks"/> says which cells stop
    /// liquid. Returns whether any cell of it is wet.
    /// </summary>
    public static bool Write(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int left, int top, int wide, int tall, byte[] texels)
    {
        bool any = false;
        for (int y = 0; y < tall; y++)
            for (int x = 0; x < wide; x++)
            {
                int gx = left + x, gy = top + y, at = (y * wide + x) * 4;
                int cell = gy * width + gx;
                int a = gx >= 0 && gy >= 0 && gx < width && gy < height ? L.AmountOf(layer[cell]) : 0;
                if (a == 0)
                {
                    texels[at] = texels[at + 1] = texels[at + 2] = texels[at + 3] = 0;
                    continue;
                }
                any = true;
                int below = cell + width;
                bool falls = gy + 1 < height && !blocks(below) && L.AmountOf(layer[below]) < L.Full;
                bool covered = gy > 0 && L.AmountOf(layer[cell - width]) > 0;
                texels[at] = (byte)(a * 255 / L.Full);
                texels[at + 1] = falls ? (byte)255 : (byte)0;
                texels[at + 2] = covered ? (byte)255 : (byte)0;
                texels[at + 3] = 255;
            }
        return any;
    }
}
