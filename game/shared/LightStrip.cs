using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// The vitals' pieces of light (hud-vitals, look D): a wash with a wandering tide line, baked by <c>gauge.js</c> per tone in two
/// variants so neighbours differ. A dial's ring is the strip bent round (<see cref="RingLight"/>); the air's bar reads it whole,
/// its columns by <see cref="StripCuts"/>, in its own shader (<see cref="AirRow"/>).
/// </summary>
public static class LightStrip
{
    private static readonly Dictionary<string, Vector4> Means = new();

    /// <summary>The long strip of <paramref name="tone"/>: sky, red, pale, white, green, paleGreen, amber, darkSky, darkRed, darkGreen.</summary>
    public static Texture2D Of(string tone, int variant) => PixelArt.Picture($"gauge_strip_{tone}_{variant & 1}");

    /// <summary>
    /// The mean colour of <paramref name="tone"/>'s light, premultiplied, over its strip's length and the rows of its light: what
    /// its light looks like blurred, for a glow. Read once, as the HUD is made.
    /// </summary>
    public static Vector4 Mean(string tone)
    {
        if (Means.TryGetValue(tone, out var known)) return known;
        var image = HiRes.Load($"gauge_strip_{tone}_0");
        if (image == null) return Means[tone] = Vector4.One;
        int w = image.GetWidth(), h = image.GetHeight();
        float tall = Of(tone, 0).GetSize().Y;
        int from = (int)(h * StripCuts.Margin / tall), to = (int)(h * (StripCuts.Margin + 10) / tall);
        int left = (int)(w * StripCuts.Margin / (StripCuts.Long + 2 * StripCuts.Margin)), right = w - left;
        byte[] data = image.GetData();
        double r = 0, g = 0, b = 0, a = 0;
        for (int y = from; y < to; y++)
            for (int x = left; x < right; x++)
            {
                int i = 4 * (y * w + x);
                double alpha = data[i + 3] / 255.0;
                (r, g, b, a) = (r + data[i] / 255.0 * alpha, g + data[i + 1] / 255.0 * alpha, b + data[i + 2] / 255.0 * alpha, a + alpha);
            }
        double n = System.Math.Max(1, (to - from) * (right - left));
        return Means[tone] = new Vector4((float)(r / n), (float)(g / n), (float)(b / n), (float)(a / n));
    }
}
