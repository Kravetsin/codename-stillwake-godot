using System;

namespace Stillwake.Game.Menu;

/// <summary>
/// The title's stars (<see cref="TitleScene"/>), in two layers going by to the left: most faint, some brighter, a handful bright
/// and a few of those with a tiny four-point glint. Made in screen pixels, as many as the scene covers, so a star stays a pixel
/// or two at any size; the same stars for the same size.
/// </summary>
public static class TitleStars
{
    /// <summary>The far layer's opacity, one byte a pixel, <paramref name="width"/> by <paramref name="height"/>, a scene pixel <paramref name="scale"/> screen pixels.</summary>
    public static byte[] Far(int width, int height, double scale) => Layer(width, height, scale, 7, 330, 0, 0.08, 0.45, 0, 0);

    /// <summary>The near layer's: fewer, brighter, some two pixels, a dozen bright, four of them glinting.</summary>
    public static byte[] Near(int width, int height, double scale) => Layer(width, height, scale, 9, 80, 0.2, 0.25, 0.8, 12, 4);

    private static byte[] Layer(int width, int height, double scale, uint seed, int count, double big, double faint, double bright, int brights, int glints)
    {
        var alpha = new float[width * height];
        var next = Rng(seed);
        void Put(int x, int y, int w, int h, double a)
        {
            float over = (float)Math.Round(a, 3);
            for (int yy = Math.Max(0, y); yy < Math.Min(height, y + h); yy++)
                for (int xx = Math.Max(0, x); xx < Math.Min(width, x + w); xx++)
                {
                    ref float under = ref alpha[yy * width + xx];
                    under = over + under * (1 - over);
                }
        }
        for (int i = 0; i < count; i++)
        {
            int size = next() < big ? 2 : 1;
            double u = next(), a = faint + u * u * (bright - faint);
            int x = (int)Math.Floor(next() * (width - 2)), y = (int)Math.Floor(next() * (height - 2));
            Put(x, y, size, size, a);
        }
        for (int i = 0; i < brights; i++)
        {
            int x = (int)Math.Floor(next() * (width - 20)) + 10, y = (int)Math.Floor(next() * (height - 20)) + 10;
            Put(x, y, 2, 2, 0.95);
            if (i >= glints) continue;
            int l = (int)Math.Round(3.5 * scale, MidpointRounding.AwayFromZero);
            Put(x - l, y, 2 * l + 2, 1, 0.32);
            Put(x, y - l, 1, 2 * l + 2, 0.32);
            Put(x - 1, y, 4, 1, 0.5);
            Put(x, y - 1, 1, 4, 0.5);
        }
        var bytes = new byte[alpha.Length];
        for (int i = 0; i < alpha.Length; i++) bytes[i] = (byte)Math.Round(alpha[i] * 255);
        return bytes;
    }

    /// <summary>The pick page's random numbers (<c>tools/pick-kit/kit.js</c>, <c>rng</c>): xorshift on a seed, 0 to 1.</summary>
    public static Func<double> Rng(uint seed)
    {
        uint s = unchecked(seed * 2654435761u);
        if (s == 0) s = 1;
        return () =>
        {
            s ^= s << 13;
            s ^= s >> 17;
            s ^= s << 5;
            return s / 4294967296.0;
        };
    }
}
