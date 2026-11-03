namespace Stillwake.Game.Render;

/// <summary>A hi-res picture brought down to the world's pixels, for what reads a picture's size or shape rather than draws it: plain C#, tested.</summary>
public static class Reduce
{
    /// <summary>
    /// <paramref name="rgba"/>, <paramref name="w"/> × <paramref name="h"/>, reduced <paramref name="by"/> times to whole pixels: each square's mean
    /// colour weighed by its cover, solid where at least half covered, else clear; or, <paramref name="soft"/>, as covered as its square.
    /// </summary>
    public static byte[] Pixels(byte[] rgba, int w, int h, int by, bool soft = false)
    {
        int rw = w / by, rh = h / by;
        var reduced = new byte[rw * rh * 4];
        for (int y = 0; y < rh; y++)
            for (int x = 0; x < rw; x++)
            {
                int r = 0, g = 0, b = 0, a = 0;
                for (int j = 0; j < by; j++)
                    for (int i = 0; i < by; i++)
                    {
                        int o = ((y * by + j) * w + x * by + i) * 4, k = rgba[o + 3];
                        (r, g, b, a) = (r + rgba[o] * k, g + rgba[o + 1] * k, b + rgba[o + 2] * k, a + k);
                    }
                if (soft ? a == 0 : a * 2 < 255 * by * by) continue;
                int p = (y * rw + x) * 4;
                (reduced[p], reduced[p + 1], reduced[p + 2], reduced[p + 3]) = ((byte)(r / a), (byte)(g / a), (byte)(b / a), soft ? (byte)(a / (by * by)) : (byte)255);
            }
        return reduced;
    }
}
