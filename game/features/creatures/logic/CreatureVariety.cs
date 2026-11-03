using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// How one creature differs from the next of its kind (0102), drawn only: plain C#, tested. All of it from its id and its
/// surface's seed, integers only, so every peer draws the same individual and nothing is drawn from the world's random stream:
/// its size (a golden-ratio step an id) and its tint (by turns an id), so neighbours in spawn order differ, its shade, now and
/// then a morph, and which of its markings it wears and how boldly.
/// </summary>
public static class CreatureVariety
{
    /// <summary>
    /// One creature's looks: <paramref name="Size"/> thousandths of its design's, <paramref name="Shade"/> quarter tones lighter
    /// (darker below 0), <paramref name="Hue"/> steps warmer (cooler below 0), whether it is a morph, a bit for each marking it
    /// wears, in the order <see cref="Markings"/> finds them, and their opacity in thousandths.
    /// </summary>
    public readonly record struct Individual(int Size, int Shade, int Hue, bool Morph, int Kept, int Opacity);

    /// <summary>The steps of its markings' opacity from the faintest to whole.</summary>
    public const int OpacitySteps = 2;

    /// <summary>The tints a coat takes in turn, an id each: toward one neighbour ramp and the other by turns, a step or two; never none.</summary>
    public static readonly int[] Tints = [-2, 1, -1, 2];

    /// <summary>The most markings a part is told apart by: any past them are always worn.</summary>
    public const int MostMarkings = 16;

    /// <summary>A plain individual: its design's size and colours, every marking.</summary>
    public static readonly Individual Plain = new(1000, 0, 0, false, (1 << MostMarkings) - 1, 1000);

    /// <summary>Creature <paramref name="id"/>'s looks on a surface grown from <paramref name="seed"/>, by <paramref name="v"/>.</summary>
    public static Individual Of(CreatureLooks.Variety v, uint seed, int id)
    {
        uint h = unchecked(Mix(Mix(seed) + (uint)id * 0x9E3779B9u));
        // Its size steps on by the golden ratio an id from where the seed starts it: any few in spawn order spread wide.
        uint step = unchecked(Mix(seed ^ 0x5bd1e995u) + (uint)id * 2654435769u);
        int size = v.Least + (int)((ulong)step * (ulong)(v.Most - v.Least + 1) >> 32);
        bool morph = v.MorphOneIn > 0 && Draw(h, 1) % (uint)v.MorphOneIn == 0;
        int shade = morph ? v.Morph : Between(h, 2, -v.Shade, v.Shade);
        int kept = 0;
        for (int k = 0; k < MostMarkings; k++)
            if (Draw(h, 16u + (uint)k) % 1000 >= (uint)v.Drop) kept |= 1 << k;
        int opacity = v.Faint + (1000 - v.Faint) * Between(h, 4, 0, OpacitySteps) / OpacitySteps;
        // Its tint turns with the id, cool and warm by turns: any few in spawn order wear each their own.
        int hue = Tints[(int)(unchecked(Mix(seed ^ 0x27d4eb2fu) + (uint)id) % (uint)Tints.Length)];
        return new(size, shade, hue, morph, kept, opacity);
    }

    /// <summary>What <paramref name="one"/>'s picture in ink is multiplied by, its ink staying ink: lighter or darker by its shade, warmer or cooler by its tint.</summary>
    public static (float R, float G, float B) InkTint(Individual one)
    {
        float light = 1 + one.Shade * CreatureLooks.InkShade, warm = one.Hue * CreatureLooks.InkHue;
        return (light * (1 + warm), light, light * (1 - warm));
    }

    /// <summary>
    /// <paramref name="rgba"/>, <paramref name="w"/> × <paramref name="h"/>, reduced <paramref name="by"/> times to whole pixels: each square's mean
    /// colour weighed by its cover, solid where at least half covered, else clear. What a picture in ink crumbles into.
    /// </summary>
    public static byte[] Reduced(byte[] rgba, int w, int h, int by)
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
                if (a * 2 < 255 * by * by) continue;
                int p = (y * rw + x) * 4;
                (reduced[p], reduced[p + 1], reduced[p + 2], reduced[p + 3]) = ((byte)(r / a), (byte)(g / a), (byte)(b / a), 255);
            }
        return reduced;
    }

    /// <summary>
    /// Every marking of a part's picture (<paramref name="rgba"/>, <paramref name="w"/> × <paramref name="h"/>): each painted
    /// pixel's marking, its pixels touching side or corner, numbered in the order their first pixel is met row by row; -1
    /// where nothing is painted. A stripe, a saddle or a spot each one marking.
    /// </summary>
    public static int[] Markings(byte[] rgba, int w, int h)
    {
        var of = new int[w * h];
        Array.Fill(of, -1);
        var open = new Stack<int>();
        int next = 0;
        for (int q = 0; q < w * h; q++)
        {
            if (rgba[q * 4 + 3] == 0 || of[q] >= 0) continue;
            of[q] = next;
            open.Push(q);
            while (open.Count > 0)
            {
                int p = open.Pop(), x = p % w, y = p / w;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int n = ny * w + nx;
                        if (rgba[n * 4 + 3] == 0 || of[n] >= 0) continue;
                        of[n] = next;
                        open.Push(n);
                    }
            }
            next++;
        }
        return of;
    }

    /// <summary>Whether <paramref name="one"/> wears marking <paramref name="marking"/>.</summary>
    public static bool Wears(Individual one, int marking) => marking >= MostMarkings || (one.Kept >> marking & 1) != 0;

    private static int Between(uint h, uint salt, int least, int most) => least + (int)(Draw(h, salt) % (uint)(most - least + 1));

    private static uint Draw(uint h, uint salt) => Mix(unchecked(h + salt * 0x9E3779B9u));

    /// <summary>A 32-bit finaliser (MurmurHash3's): every bit of the input stirred into every bit out.</summary>
    private static uint Mix(uint h)
    {
        unchecked
        {
            h ^= h >> 16;
            h *= 0x85EBCA6Bu;
            h ^= h >> 13;
            h *= 0xC2B2AE35u;
            h ^= h >> 16;
            return h;
        }
    }
}
