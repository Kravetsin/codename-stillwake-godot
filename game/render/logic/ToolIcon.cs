using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// A tool's icon (2026-10-27-tool-icons): its held picture, its head along +x, turned to point up-right and fitted to a square
/// with <see cref="HeldLook.IconMargin"/> round it, the diagonal of its drawn pixels across the square. Drawn off the pixel grid:
/// each output pixel the mean of sub-samples, each the nearest source pixel, so a source pixel is a crisp turned square
/// with its edges by area. Plain C#: a 56 px bake is some two hundred thousand lookups, once per tool and side.
/// </summary>
public static class ToolIcon
{
    // Sub-samples along each side of an output pixel: the mock page's ×8 nearest upscale before its smooth draw.
    private const int Samples = 8;

    /// <summary>
    /// The icon of <paramref name="rgba"/>, a <paramref name="width"/>×<paramref name="height"/> straight RGBA8 picture rows top
    /// first, as a <paramref name="side"/>×<paramref name="side"/> one. A clear pixel takes its nearest drawn pixel's colour,
    /// so a filtered or mipmapped icon has no dark fringe.
    /// </summary>
    public static byte[] Bake(ReadOnlySpan<byte> rgba, int width, int height, int side)
    {
        var icon = new byte[side * side * 4];
        if (DrawnBox(rgba, width, height) is not var (left, top, w, h)) return icon;
        double scale = ScaleOf(w, h, side), middleX = left + w / 2.0, middleY = top + h / 2.0, half = side / 2.0;
        for (int oy = 0; oy < side; oy++)
            for (int ox = 0; ox < side; ox++)
            {
                long alpha = 0, red = 0, green = 0, blue = 0;
                for (int sy = 0; sy < Samples; sy++)
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        double u = (ox + (sx + 0.5) / Samples - half) / scale, v = (oy + (sy + 0.5) / Samples - half) / scale;
                        // The turn undone: the icon's (u, v) is the picture's +x along (1, −1)/√2 and +y along (1, 1)/√2.
                        int x = (int)Math.Floor(middleX + (u - v) / Math.Sqrt(2)), y = (int)Math.Floor(middleY + (u + v) / Math.Sqrt(2));
                        if (x < 0 || y < 0 || x >= width || y >= height) continue;
                        int at = (y * width + x) * 4, a = rgba[at + 3];
                        alpha += a;
                        red += rgba[at] * a;
                        green += rgba[at + 1] * a;
                        blue += rgba[at + 2] * a;
                    }
                if (alpha == 0) continue;
                int o = (oy * side + ox) * 4;
                icon[o] = (byte)((red + alpha / 2) / alpha);
                icon[o + 1] = (byte)((green + alpha / 2) / alpha);
                icon[o + 2] = (byte)((blue + alpha / 2) / alpha);
                icon[o + 3] = (byte)((alpha + Samples * Samples / 2) / (Samples * Samples));
            }
        Bleed(icon, side);
        return icon;
    }

    /// <summary>The box of the pixels drawn in a picture, any alpha above none; none for a clear one.</summary>
    internal static (int Left, int Top, int Width, int Height)? DrawnBox(ReadOnlySpan<byte> rgba, int width, int height)
    {
        int left = width, top = height, right = -1, bottom = -1;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (rgba[(y * width + x) * 4 + 3] > 0)
                    (left, top, right, bottom) = (Math.Min(left, x), Math.Min(top, y), Math.Max(right, x), Math.Max(bottom, y));
        return right < 0 ? null : (left, top, right - left + 1, bottom - top + 1);
    }

    /// <summary>Icon pixels per picture pixel: a <paramref name="w"/>×<paramref name="h"/> box turned 45° spans (w + h)/√2 each way, fitted inside the margin.</summary>
    internal static double ScaleOf(int w, int h, int side) => (side - 2 * HeldLook.IconMargin) / ((w + h) / Math.Sqrt(2));

    // Each clear pixel takes the colour of the nearest drawn one, spreading outward a ring at a time; its alpha stays none.
    private static void Bleed(byte[] icon, int side)
    {
        var filled = new bool[side * side];
        var next = new Queue<int>();
        for (int i = 0; i < side * side; i++)
            if (icon[i * 4 + 3] > 0)
            {
                filled[i] = true;
                next.Enqueue(i);
            }
        while (next.Count > 0)
        {
            int i = next.Dequeue(), x = i % side, y = i / side;
            if (x > 0) Spread(i, i - 1);
            if (x < side - 1) Spread(i, i + 1);
            if (y > 0) Spread(i, i - side);
            if (y < side - 1) Spread(i, i + side);
        }

        void Spread(int from, int to)
        {
            if (filled[to]) return;
            filled[to] = true;
            Array.Copy(icon, from * 4, icon, to * 4, 3);
            next.Enqueue(to);
        }
    }
}
