using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// Which pixels of a picture give off light (0026): the middles of its glowing pixels, in pixels from its
/// top left, two numbers a pixel. A pixel glows when it is opaque and at least as bright as its emitter's
/// <see cref="Emission.Glowing"/>.
/// </summary>
public sealed class EmitterMask
{
    public int Width { get; }
    public int Height { get; }

    /// <summary>The middle of every glowing pixel, x then y.</summary>
    public float[] Pixels { get; }

    private EmitterMask(int width, int height, float[] pixels) => (Width, Height, Pixels) = (width, height, pixels);

    public int Count => Pixels.Length / 2;

    /// <summary>
    /// The glowing pixels of a picture <paramref name="width"/> by <paramref name="height"/> in RGBA bytes,
    /// those opaque and of a luminance (Rec. 709, as Godot's) at least <paramref name="glowing"/>.
    /// </summary>
    public static EmitterMask Of(ReadOnlySpan<byte> rgba, int width, int height, float glowing)
    {
        var pixels = new List<float>();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                if (rgba[i + 3] < 128) continue;
                float luminance = (0.2126f * rgba[i] + 0.7152f * rgba[i + 1] + 0.0722f * rgba[i + 2]) / 255f;
                if (luminance < glowing) continue;
                pixels.Add(x + 0.5f);
                pixels.Add(y + 0.5f);
            }
        return new EmitterMask(width, height, pixels.ToArray());
    }
}
