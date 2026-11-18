using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// How the suit shows its wearer's body cold or hot (body-temperature), as the user picked them: frost «Выстуживание» greys the suit
/// paler and bluer, ice specks twinkling on it; heat «Подпалины и искры» chars it in patches from the edges in, their rims smouldering
/// like burning paper, embers flaring inside, sparks rising off it in the screen haze's colours. Strengths 0 to 1; the numbers as the
/// pick page drew them, a part's picture <see cref="Scale"/> times the world's pixels.
/// </summary>
public static class SuitFx
{
    /// <summary>A hero picture's pixels to the world's one (<c>HiRes.Scale</c>): the page's noise and specks are sized in them.</summary>
    public const int Scale = 4;

    // Frost.

    /// <summary>How much of its own colour the suit loses toward grey at full frost.</summary>
    public const float Grey = 0.9f;
    /// <summary>The share of the suit's specks (two picture pixels a side) iced at full frost, and how fast one twinkles, radians a second.</summary>
    public const float Specks = 0.2f, Twinkle = 2.3f;

    // Heat.

    /// <summary>How far the char's threshold climbs at full heat, and what it starts below: no char until heat passes Start / Reach.</summary>
    public const float Reach = 0.85f, Start = 0.04f;
    /// <summary>How far in from a part's edge, in the world's pixels, the edge still draws the char toward it (26 picture pixels).</summary>
    public const float EdgeReach = 6.5f;
    /// <summary>How wide the smouldering rim is, in the threshold's terms, and the share of charred specks that flare as embers.</summary>
    public const float Rim = 0.035f, Embers = 0.025f;

    /// <summary>How many sparks fly at most; each rises for its own life, then starts again from another place on the suit.</summary>
    public const int Sparks = 30;
    /// <summary>A spark's half width, in the world's pixels, and the least it is drawn on screen, in the screen's.</summary>
    public const float SparkRadius = 0.45f, SparkLeast = 0.6f;

    /// <summary>The char's threshold at <paramref name="heat"/>: the suit is charred where its noise, drawn toward its edges, is under it.</summary>
    public static float Scorch(float heat) => heat * Reach - Start;

    /// <summary>The page's hash: a number from 0 to 1 for (<paramref name="x"/>, <paramref name="y"/>), the same each time.</summary>
    public static double Hash(double x, double y)
    {
        double v = Math.Sin(x * 127.1 + y * 311.7) * 43758.5453;
        return v - Math.Floor(v);
    }

    /// <summary>Where part <paramref name="index"/>'s noise starts, in the world's pixels: two legs from one picture char apart.</summary>
    public static (float X, float Y) Seed(int index) => (index * 13.71f, index * 7.37f);

    /// <summary>
    /// How far each pixel of an RGBA picture <paramref name="w"/> by <paramref name="h"/> lies inside it: a chamfer distance (1, √2) to
    /// the nearest pixel under half clear, 0 there; infinite everywhere when none is.
    /// </summary>
    public static float[] EdgeDistances(ReadOnlySpan<byte> rgba, int w, int h)
    {
        int n = w * h;
        var d = new float[n];
        const float Far = float.PositiveInfinity, Diagonal = 1.41421356f;
        for (int i = 0; i < n; i++) d[i] = rgba[i * 4 + 3] < 128 ? 0 : Far;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                float v = d[i];
                if (v == 0) continue;
                if (x > 0) v = Math.Min(v, d[i - 1] + 1);
                if (y > 0)
                {
                    v = Math.Min(v, d[i - w] + 1);
                    if (x > 0) v = Math.Min(v, d[i - w - 1] + Diagonal);
                    if (x < w - 1) v = Math.Min(v, d[i - w + 1] + Diagonal);
                }
                d[i] = v;
            }
        for (int y = h - 1; y >= 0; y--)
            for (int x = w - 1; x >= 0; x--)
            {
                int i = y * w + x;
                float v = d[i];
                if (v == 0) continue;
                if (x < w - 1) v = Math.Min(v, d[i + 1] + 1);
                if (y < h - 1)
                {
                    v = Math.Min(v, d[i + w] + 1);
                    if (x < w - 1) v = Math.Min(v, d[i + w + 1] + Diagonal);
                    if (x > 0) v = Math.Min(v, d[i + w - 1] + Diagonal);
                }
                d[i] = v;
            }
        return d;
    }

    /// <summary>Distances in the world's pixels as the shader reads them: a byte each, whole at <see cref="EdgeReach"/> and beyond.</summary>
    public static byte[] EdgeBytes(float[] distances)
    {
        var bytes = new byte[distances.Length];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)Math.Round(Math.Clamp(distances[i] / EdgeReach, 0, 1) * 255);
        return bytes;
    }

    /// <summary>The pixels sparks leave from: those at least a pixel inside the picture, by index, row by row.</summary>
    public static int[] Inside(float[] distances)
    {
        var inside = new List<int>();
        for (int i = 0; i < distances.Length; i++)
            if (distances[i] >= 1) inside.Add(i);
        return [.. inside];
    }

    /// <summary>
    /// A spark in flight: its rise's <paramref name="Cycle"/> (a new one leaves from a new place), <paramref name="Pick"/> and
    /// <paramref name="Spot"/> to choose that place, 0 to 1; how far it has risen from it, in the world's pixels, x toward the hero's back;
    /// <paramref name="Age"/> through its life, 0 to 1, from the haze's pale to its ember; its alpha.
    /// </summary>
    public readonly record struct Spark(long Cycle, double Pick, double Spot, double Back, double Up, double Age, double Alpha);

    /// <summary>Spark <paramref name="i"/> at <paramref name="seconds"/> in <paramref name="heat"/>; none between its rises or while the heat holds it back.</summary>
    public static Spark? SparkAt(int i, double seconds, double heat)
    {
        if (heat <= 0) return null;
        double life = 1.1 + 0.9 * Hash(i, 9), period = life * (1.2 + Hash(i, 1)), phase = seconds / period + Hash(i, 2);
        long cycle = (long)Math.Floor(phase);
        double age = (phase - cycle) * period / life;
        if (age > 1 || Hash(i, cycle + 0.5) > heat) return null;
        double back = age * 2 - Math.Sin(age * 5 + i) * 1.2 * age, up = age * (10 + 8 * Hash(i, cycle + 4));
        return new Spark(cycle, Hash(i, cycle), Hash(i, cycle + 7), back, up, age, Math.Clamp(heat * 3, 0, 1) * (1 - age * age));
    }
}
