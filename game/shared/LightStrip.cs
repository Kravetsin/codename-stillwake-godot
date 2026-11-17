using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// The vitals' pieces of light (hud-vitals, look D): a wash with a wandering tide line, baked by <c>gauge.js</c> per tone in two
/// variants so neighbours differ. A row's piece is the long strip cut to its length, its start then its own end; a dial's ring
/// is the strip bent round (<see cref="RingLight"/>). The glow under a lit part is the strip again, added and spread.
/// </summary>
public static class LightStrip
{
    /// <summary>The strip's length and the baked margin round every picture, in block units; how much of a strip's end a piece keeps.</summary>
    private const float Long = 304, Margin = 2, Cap = 8;

    /// <summary>The long strip of <paramref name="tone"/>: sky, red, pale, white, green, paleGreen, amber, darkSky, darkRed, darkGreen.</summary>
    public static Texture2D Of(string tone, int variant) => PixelArt.Picture($"gauge_strip_{tone}_{variant & 1}");

    /// <summary>
    /// The part from <paramref name="from"/> to <paramref name="to"/> along a piece <paramref name="length"/> long at
    /// (<paramref name="x"/>, <paramref name="top"/>), <paramref name="height"/> tall, drawn in <paramref name="strip"/>: past either
    /// end its soft margin comes too.
    /// </summary>
    public static void Span(CanvasItem on, Texture2D strip, float x, float top, float length, float height, float from, float to, Color modulate)
    {
        from = Math.Max(0, from);
        to = Math.Min(length, to);
        if (modulate.A <= 0.004f || to - from <= 0.01f || length <= 0.01f) return;
        float a0 = from <= 0.01f ? -Margin : from, a1 = to >= length - 0.01f ? length + Margin : to;
        float cap = Math.Min(Cap, length / 2), cut = Math.Max(0, Math.Min(length - cap, Long - cap));
        float ky = height / 10, end = strip.GetSize().X - Margin;
        // Its start as baked, to where its end takes over; then the strip's own last part.
        Part(on, strip, x, top, ky, Math.Max(a0, -Margin), Math.Min(a1, cut), 0, modulate);
        Part(on, strip, x, top, ky, Math.Max(a0, cut), Math.Min(a1, length + Margin), end - Margin - cap - cut, modulate);
    }

    /// <summary>The columns from <paramref name="a"/> to <paramref name="b"/> of a piece, read from the strip <paramref name="shift"/> further on.</summary>
    private static void Part(CanvasItem on, Texture2D strip, float x, float top, float ky, float a, float b, float shift, Color modulate)
    {
        if (b - a <= 0.01f) return;
        float tall = strip.GetSize().Y;
        on.DrawTextureRectRegion(strip, new Rect2(x + a, top - Margin * ky, b - a, tall * ky), new Rect2(Margin + a + shift, 0, b - a, tall), modulate);
    }

    /// <summary>The even glow under a lit part, drawn on an added layer: the strip again, spread above and below it.</summary>
    public static void Glow(CanvasItem on, Texture2D strip, float x, float top, float length, float height, float from, float to, float alpha)
    {
        foreach (var (spread, a) in new[] { (5f, 0.12f), (2.5f, 0.2f) })
            Span(on, strip, x, top - spread, length, height + 2 * spread, from, to, Colors.White with { A = a * alpha });
    }
}
