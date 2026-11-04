using System;

namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// How what a salvage drone's net holds lies in it (salvage-trips), apart from Godot: its pictures in catch order, in rows of
/// two from the net's bottom, a row alone centred, each row resting part way down into the one under it; all drawn smaller
/// the more share the net. In the world's pixels from the net's bottom middle, y down.
/// </summary>
public static class NetPile
{
    /// <summary>How much of a row's height the row over it sinks into it, and of two side by side each other's width.</summary>
    private const float Nest = 0.4f, Overlap = 0.1f;

    /// <summary>How big each picture is drawn with <paramref name="count"/> in the net: whole alone, smaller as more share it.</summary>
    public static float ScaleOf(int count) => count <= 1 ? 1f : count == 2 ? 0.8f : 0.65f;

    /// <summary>
    /// Each of <paramref name="sizes"/>, the pictures' own, laid into <paramref name="into"/> as drawn, its top-left and size;
    /// the pile's height, its top that far over the net's bottom.
    /// </summary>
    public static float Lay(ReadOnlySpan<(float W, float H)> sizes, Span<(float X, float Y, float W, float H)> into)
    {
        float scale = ScaleOf(sizes.Length), foot = 0, top = 0;
        for (int row = 0; row * 2 < sizes.Length; row++)
        {
            int first = row * 2, count = Math.Min(2, sizes.Length - first);
            float tall = 0;
            for (int i = first; i < first + count; i++)
            {
                var (w, h) = (sizes[i].W * scale, sizes[i].H * scale);
                // Alone, centred; of two, the first left and the second right, overlapping a little.
                float x = count == 1 ? -w / 2 : i == first ? -w * (1 - Overlap) : -w * Overlap;
                into[i] = (x, -foot - h, w, h);
                tall = Math.Max(tall, h);
                top = Math.Max(top, foot + h);
            }
            foot += tall * (1 - Nest);
        }
        return top;
    }
}
