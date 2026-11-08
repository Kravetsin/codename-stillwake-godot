using System;
using L = Stillwake.Content.Liquids;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water as the shader reads it (<c>WaterLook</c>), drawn as Terraria draws its liquid: a texel a cell of a box of the world,
/// RGBA8. Red is how high the cell's water stands, 0 to 255: its amount over <c>Liquids.Full</c>, or all of it where water is over
/// it. Green is set where it falls, blue where water is over it, alpha where it is drawn. The sim moves water a cell a tick
/// (<c>Liquids.Fall</c>), so falling water is a whole column of cells, drawn as one.
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
        Span<bool> under = wide <= 1024 ? stackalloc bool[wide] : new bool[wide];
        Span<bool> here = wide <= 1024 ? stackalloc bool[wide] : new bool[wide];
        // From the bottom up, so a cell knows whether the water under it falls; the row under the box by its own cells alone.
        for (int x = 0; x < wide; x++) under[x] = Falls(layer, width, height, blocks, left + x, top + tall, under, left);
        for (int y = tall - 1; y >= 0; y--)
        {
            for (int x = 0; x < wide; x++)
            {
                int gx = left + x, gy = top + y, at = (y * wide + x) * 4;
                texels[at] = texels[at + 1] = texels[at + 2] = texels[at + 3] = 0;
                here[x] = Falls(layer, width, height, blocks, gx, gy, under, left);
                if (gx < 0 || gy < 0 || gx >= width || gy >= height) continue;
                int cell = gy * width + gx, a = L.AmountOf(layer[cell]);
                if (a == 0) continue;
                bool covered = gy > 0 && L.AmountOf(layer[cell - width]) > 0;
                texels[at] = covered ? (byte)255 : (byte)(a * 255 / L.Full);
                texels[at + 1] = here[x] ? (byte)255 : (byte)0;
                texels[at + 2] = covered ? (byte)255 : (byte)0;
                texels[at + 3] = 255;
                any = true;
            }
            here.CopyTo(under);
        }
        return any;
    }

    /// <summary>
    /// Whether the water at (<paramref name="gx"/>, <paramref name="gy"/>) falls: open under it and short of full there, or over
    /// falling water (<paramref name="under"/>, the row under it from <paramref name="left"/>) with no water resting beside it,
    /// so a full column falls as one but a pool over the hole it drains through lies.
    /// </summary>
    private static bool Falls(ReadOnlySpan<byte> layer, int width, int height, Func<int, bool> blocks, int gx, int gy, ReadOnlySpan<bool> under, int left)
    {
        if (gx < 0 || gy < 0 || gx >= width || gy + 1 >= height) return false;
        int cell = gy * width + gx;
        if (L.AmountOf(layer[cell]) == 0 || blocks(cell + width)) return false;
        if (L.AmountOf(layer[cell + width]) < L.Full) return true;
        return Fell(under, gx - left) && Lets(layer, width, under, left, cell, gx - 1) && Lets(layer, width, under, left, cell, gx + 1);
    }

    private static bool Fell(ReadOnlySpan<bool> under, int x) => x >= 0 && x < under.Length && under[x];

    /// <summary>Whether the cell beside, at column <paramref name="nx"/>, holds no water or water over falling water.</summary>
    private static bool Lets(ReadOnlySpan<byte> layer, int width, ReadOnlySpan<bool> under, int left, int cell, int nx)
    {
        if (nx < 0 || nx >= width) return true;
        return L.AmountOf(layer[cell - cell % width + nx]) == 0 || Fell(under, nx - left);
    }
}
