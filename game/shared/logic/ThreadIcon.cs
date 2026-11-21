using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// One thread icon's lines as exported (<see cref="IconInk"/>): its <paramref name="parts"/>, polylines of x, y pairs in a box -1..1;
/// the <paramref name="one"/> thread through them all; the points of it a <paramref name="bridges"/> step reaches.
/// </summary>
public sealed class IconLines(float[][] parts, float[] one, int[] bridges)
{
    /// <summary>The parts, each its own run: what a rim draws.</summary>
    public IconPath Parts { get; } = new(parts, []);

    /// <summary>The one thread, its bridges marked: what an icon draws.</summary>
    public IconPath One { get; } = new([one], bridges);
}

/// <summary>A path of straight steps through runs of points, walked by a share of its length (the pick's <c>mkPath</c>, <c>onPath</c>).</summary>
public sealed class IconPath
{
    /// <summary>A step: from (<see cref="Ax"/>, <see cref="Ay"/>) to (<see cref="Bx"/>, <see cref="By"/>), so long, starting so far along, a bridge or not.</summary>
    public readonly record struct Step(float Ax, float Ay, float Bx, float By, float Length, float From, bool Bridge);

    private readonly List<Step> _steps = [];

    /// <param name="runs">Runs of x, y pairs, each walked on from the last.</param>
    /// <param name="bridges">The points of the first run a step into is a bridge.</param>
    public IconPath(float[][] runs, int[] bridges)
    {
        foreach (var run in runs)
            for (int i = 1; i < run.Length / 2; i++)
            {
                float ax = run[2 * i - 2], ay = run[2 * i - 1], bx = run[2 * i], by = run[2 * i + 1];
                float d = MathF.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
                if (d < 1e-6f) continue;
                _steps.Add(new Step(ax, ay, bx, by, d, Length, ReferenceEquals(run, runs[0]) && Array.IndexOf(bridges, i) >= 0));
                Length += d;
            }
    }

    public IReadOnlyList<Step> Steps => _steps;

    /// <summary>Its whole length.</summary>
    public float Length { get; }

    /// <summary>Where <paramref name="share"/> of the way along it lies (wrapped round), and the way its step runs.</summary>
    public (float X, float Y, float Dx, float Dy) At(float share)
    {
        float d = ((share % 1) + 1) % 1 * Length;
        int lo = 0, hi = _steps.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (_steps[mid].From <= d) lo = mid;
            else hi = mid - 1;
        }
        var s = _steps[lo];
        float e = Math.Clamp((d - s.From) / s.Length, 0, 1);
        return (s.Ax + (s.Bx - s.Ax) * e, s.Ay + (s.By - s.Ay) * e, (s.Bx - s.Ax) / s.Length, (s.By - s.Ay) / s.Length);
    }
}

/// <summary>
/// How a thread icon stands now: how bright (<see cref="Level"/>, an edge key's), how much under the pointer, on, held, waiting, a mode not
/// known yet (<see cref="Shut"/>), how much of a pick's flare is left, a rim's rather than an icon's, its time in seconds and its seed.
/// </summary>
public readonly record struct IconLook(float Level, float Hover = 0, bool On = false, bool Held = false, bool Waiting = false, bool Shut = false,
    float Flare = 0, bool Rim = false, float Time = 0, float Seed = 0);

/// <summary>
/// The thread icons, the pick «Одна нить» (thread-icons.js, candidate oneThread): each one continuous thread, faint bridges between its
/// parts, a soft glow along it. Under the pointer one spark with its tail runs it in 0.9 s, resting 0.3 s; on, it walks it in 2.4 s;
/// held, it stands; picked, the thread flares and the spark runs back as it fades; a mode not known yet is a broken, dashed thread. A
/// waiting key pulses 1.2 times a second. A rim is drawn so too, its spark only under the pointer. Client only.
/// </summary>
public static class ThreadIcon
{
    /// <summary>A shut icon's dashes and gaps, in its thread's px.</summary>
    public const float Dash = 1.6f, Gap = 1.8f;

    /// <summary>How long a wheel takes to come, s; how long a picked side flares after it is let go; how long the rest of it takes to go.</summary>
    public const float WheelIn = 0.12f, FlareS = 0.45f, WheelOut = 0.2f;

    /// <summary>An edge key's icon by its motif (<see cref="EdgeRow.Keys"/>).</summary>
    public static IconLines Named(string motif) => motif switch
    {
        "air" => IconInk.Air,
        "build" => IconInk.Build,
        "research" => IconInk.Research,
        "market" => IconInk.Market,
        "journal" => IconInk.Journal,
        _ => throw new ArgumentException($"No thread icon {motif}", nameof(motif)),
    };

    /// <summary>
    /// <paramref name="icon"/> as it stands (<paramref name="look"/>) into <paramref name="into"/>, its box's middle at
    /// (<paramref name="x"/>, <paramref name="y"/>), <paramref name="r"/> px a unit of it, its thread <paramref name="u"/> px.
    /// </summary>
    public static void Draw(EmberMarks into, IconLines icon, IconLook look, float x, float y, float r, float u)
    {
        var path = look.Rim ? icon.Parts : icon.One;
        float fl = look.Flare, lv = look.Level * Pulse(look) * (look.Shut ? 0.35f : 1);
        Stroke(into, path, x, y, r, 3 * u, 0, (0.06f + 0.1f * fl) * lv, null);
        if (look.Shut)
        {
            Dashed(into, path, x, y, r, 0.9f * u, 0.05f, 0.55f * lv, Dash * u, Gap * u);
            return;
        }
        float a = MathF.Min(1, 0.3f + 0.45f * lv + 0.4f * fl) * (look.Held ? 0.8f : 1);
        Stroke(into, path, x, y, r, (look.Held ? 0.8f : 1) * u, 0.12f + 0.4f * fl, a, false);
        Stroke(into, path, x, y, r, 0.6f * u, 0.12f, a * 0.35f, true);
        if (SparkAt(look) is not float s) return;
        float h = MathF.Min(1, 0.5f + 0.5f * lv) * (look.Held ? 0.6f : 1) * (look.Rim ? 0.6f : 1);
        Tail(into, path, x, y, r, s, look.Rim ? 0.12f : 0.22f, 1.3f * u, h);
        var (qx, qy, dx, dy) = path.At(MathF.Min(0.9999f, s));
        float px = x + qx * r, py = y + qy * r;
        if (!look.Rim) into.Add(new EmberMarks.Halo(px, py, 3.2f * u, 0.4f, 0.45f * h));
        // The pick's spark: three squares nearly on one another, its heat in tenths.
        float heat = Math.Clamp(MathF.Round(h * 10), 1, 10) / 10;
        for (int m = 0; m < 3; m++)
            into.Add(new EmberMarks.Square(px - dx * m * 0.01f, py - dy * m * 0.01f, 1.3f * u, 0.35f * (1 - m / 3f), (0.95f - m * 0.3f) * heat));
    }

    /// <summary>How a waiting one pulses now: 0.7..1, or 1.</summary>
    public static float Pulse(IconLook look) => look.Waiting ? 0.7f + 0.3f * MathF.Sin(look.Time * MathF.Tau * IconInk.Pulse) : 1;

    /// <summary>Where its spark is along its thread, 0..1, or none: running under the pointer, walking while on or waiting, standing held, running back as a pick fades.</summary>
    public static float? SparkAt(IconLook look)
    {
        float t = look.Time, seed = look.Seed;
        if (look.Shut) return null;
        if (look.Rim) return look.Hover > 0.01f ? Frac(t * 0.6f + seed) : null;
        if (look.Flare > 0) return 1 - look.Flare;
        if (look.Held) return 0.55f + 0.2f * (float)Hash(seed);
        if (look.Hover > 0.01f)
        {
            float q = Frac((t + seed) / 1.2f) * 1.2f;
            return q < 0.9f ? q / 0.9f : null;
        }
        if (look.On || look.Waiting) return Frac((t + seed * 3) / 2.4f);
        return null;
    }

    /// <summary>The steps of <paramref name="path"/> (only the bridges, or none of them, or all with null) as runs, each run unbroken.</summary>
    private static void Stroke(EmberMarks into, IconPath path, float x, float y, float r, float width, float pale, float alpha, bool? bridges)
    {
        if (alpha <= 0.004f) return;
        bool open = false;
        (float X, float Y) last = default;
        foreach (var s in path.Steps)
        {
            if (bridges is bool want && s.Bridge != want)
            {
                if (open) into.End(width, pale, alpha);
                open = false;
                continue;
            }
            if (!open || last != (s.Ax, s.Ay))
            {
                if (open) into.End(width, pale, alpha);
                into.Begin();
                into.To(x + s.Ax * r, y + s.Ay * r);
                open = true;
            }
            into.To(x + s.Bx * r, y + s.By * r);
            last = (s.Bx, s.By);
        }
        if (open) into.End(width, pale, alpha);
    }

    /// <summary>The whole path broken into dashes so long with gaps so long, px, each run starting on a dash.</summary>
    private static void Dashed(EmberMarks into, IconPath path, float x, float y, float r, float width, float pale, float alpha, float dash, float gap)
    {
        if (alpha <= 0.004f) return;
        float walked = 0;
        (float X, float Y)? last = null;
        foreach (var s in path.Steps)
        {
            if (last != (s.Ax, s.Ay)) walked = 0;
            float len = s.Length * r;
            for (float d = 0; d < len;)
            {
                float phase = walked % (dash + gap);
                bool on = phase < dash;
                // At least a hair: on a dash's edge the remainder rounds to nothing and would never move on.
                float piece = MathF.Min(len - d, MathF.Max(1e-3f, on ? dash - phase : dash + gap - phase));
                if (on)
                {
                    float e0 = d / len, e1 = (d + piece) / len;
                    into.Segment(x + (s.Ax + (s.Bx - s.Ax) * e0) * r, y + (s.Ay + (s.By - s.Ay) * e0) * r,
                        x + (s.Ax + (s.Bx - s.Ax) * e1) * r, y + (s.Ay + (s.By - s.Ay) * e1) * r, width, pale, alpha);
                }
                d += piece;
                walked += piece;
            }
            last = (s.Bx, s.By);
        }
    }

    /// <summary>The run behind <paramref name="s"/>: <paramref name="len"/> of the path in ten pieces brightening to its head.</summary>
    private static void Tail(EmberMarks into, IconPath path, float x, float y, float r, float s, float len, float width, float h)
    {
        const int N = 10;
        for (int i = 0; i < N; i++)
        {
            float s0 = s - len * (1 - i / (float)N), s1 = MathF.Min(0.9999f, s - len * (1 - (i + 1) / (float)N));
            if (s0 < 0 || s1 <= s0) continue;
            var a = path.At(s0);
            var b = path.At(s1);
            into.Segment(x + a.X * r, y + a.Y * r, x + b.X * r, y + b.Y * r, width, 0.3f + 0.4f * i / N, h * (i + 1) / N);
        }
    }

    private static float Frac(float v) => v - MathF.Floor(v);

    /// <summary>The pick's <c>hsh</c>.</summary>
    private static double Hash(double i)
    {
        double s = Math.Sin(i * 127.1 + 311.7) * 43758.5453;
        return s - Math.Floor(s);
    }
}
