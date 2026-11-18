using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where a piece of light reads its columns from the long strip (<c>LightStrip</c>, hud-vitals): its start as baked, then the strip's
/// own end. A piece longer than the strip repeats the strip's body between them, so it is drawn whole however long it grows: the air's
/// bar runs to the edge's keys, past the strip's length on a wide screen.
/// </summary>
public static class StripCuts
{
    /// <summary>The strip's length and the baked margin round it, in block units; how much of the strip's end a piece keeps.</summary>
    public const float Long = 304, Margin = 2, Cap = 8;

    /// <summary>
    /// The stretches of the part from <paramref name="from"/> to <paramref name="to"/> along a piece <paramref name="length"/> long, its
    /// soft margin past either end when the part reaches it: each from <c>A</c> to <c>B</c> along the piece, read from the strip's column
    /// <see cref="Margin"/> + a + <c>Shift</c>.
    /// </summary>
    public static IReadOnlyList<(float A, float B, float Shift)> Of(float length, float from, float to)
    {
        var cuts = new List<(float, float, float)>();
        from = Math.Max(0, from);
        to = Math.Min(length, to);
        if (to - from <= 0.01f || length <= 0.01f) return cuts;
        float a0 = from <= 0.01f ? -Margin : from, a1 = to >= length - 0.01f ? length + Margin : to;
        void Add(float a, float b, float shift)
        {
            a = Math.Max(a, a0);
            b = Math.Min(b, a1);
            if (b - a > 0.01f) cuts.Add((a, b, shift));
        }
        if (length <= Long)
        {
            float cut = Math.Max(0, length - Math.Min(Cap, length / 2));
            Add(-Margin, cut, 0);
            Add(cut, length + Margin, Long - length);
            return cuts;
        }
        // The start as baked; the body again and again; the strip's own end.
        float body = Long - 2 * Cap, x = Long - Cap;
        Add(-Margin, x, 0);
        for (; x < length - Cap; x += body) Add(x, Math.Min(x + body, length - Cap), Cap - x);
        Add(length - Cap, length + Margin, Long - length);
        return cuts;
    }
}
