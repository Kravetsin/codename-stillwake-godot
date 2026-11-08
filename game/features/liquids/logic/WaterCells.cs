using System;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water as the shader reads it (<c>WaterLook</c>), drawn as Terraria draws its liquid: a texel a cell of a box of the world,
/// RGBA8. Red is how high the cell's water stands, 0 to 255: its amount over <c>Liquids.Full</c>, or all of it where water is over
/// it. Green is set where it falls (open under it, short of full there), blue where water is over it, alpha where it is drawn.
/// The sim moves water a cell a tick (<c>Liquids.Fall</c>), so falling water is a whole column of cells, drawn as one.
/// </summary>
public static class WaterCells
{
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
                if (a == 0) continue;
                bool covered = gy > 0 && L.AmountOf(layer[cell - width]) > 0;
                bool falls = gy + 1 < height && !blocks(cell + width) && L.AmountOf(layer[cell + width]) < L.Full;
                texels[at] = covered ? (byte)255 : (byte)(a * 255 / L.Full);
                texels[at + 1] = falls ? (byte)255 : (byte)0;
                texels[at + 2] = covered ? (byte)255 : (byte)0;
                texels[at + 3] = 255;
                any = true;
            }
        return any;
    }
}
