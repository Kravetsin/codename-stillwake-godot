using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// A tech icon's pixels (<see cref="TechLooks"/>), painted as the icon page's canvas strokes them: each line in passes,
/// a dark halo for a mark, then a glow with a blurred shadow, then the rim, then the white-hot core, every stroke laid
/// over the last. A line's cover is its round-capped band's share of each pixel; a shadow, that band blurred. Plain C#:
/// some tens of thousands of sums for a 32 px paint, twelve a second.
/// </summary>
public sealed class TechCanvas
{
    // The page's numbers for variant C, render() in its script: alphas and widths by pass.
    private const float HaloWiden = 2.1f, HaloAlpha = 0.82f, HaloShadow = 0.95f, HaloBlur = 1.6f;
    private const float GlowWiden = 2.4f, GlowFrame = 0.16f, GlowGlyph = 0.34f;
    private const float GhostFrame = 0.36f, GhostGlyph = 0.48f;
    private static readonly (float R, float G, float B) Halo = (5 / 255f, 8 / 255f, 14 / 255f);

    public int Side { get; }

    /// <summary>The last paint, straight RGBA8, rows top first.</summary>
    public byte[] Rgba { get; }

    private readonly float[] _paint, _distance;

    public TechCanvas(int side)
    {
        Side = side;
        Rgba = new byte[side * side * 4];
        _paint = new float[side * side * 4];
        _distance = new float[side * side];
    }

    /// <summary>Paints <paramref name="lines"/> of <paramref name="look"/> as <paramref name="show"/> asks, at <paramref name="intensity"/> (1 still).</summary>
    public void Paint(TechLooks.Look look, IReadOnlyList<TechLooks.Line> lines, TechShow show, float intensity)
    {
        Array.Clear(_paint);
        float s = Side, i = intensity, baseWidth = MathF.Max(TechLooks.LeastWidth, s * TechLooks.WidthShare);
        var (core, rim, glow) = (Colour(look.Core), Colour(look.Rim), Colour(look.Glow));
        bool hot = show == TechShow.Hot;
        float WidthOf(TechLooks.Line l) => baseWidth * l.Weight * (l.Frame ? TechLooks.FrameWeight : 1);
        if (show == TechShow.Ghost)
        {
            foreach (var l in lines) Stroke(l, rim, l.Frame ? GhostFrame : GhostGlyph, WidthOf(l));
            Finish();
            return;
        }
        if (show == TechShow.Mark)
            foreach (var l in lines) Stroke(l, Halo, HaloAlpha, WidthOf(l) + HaloWiden, (Halo, HaloShadow, HaloBlur));
        float glowShadow = (hot ? 0.6f : 0.85f) * i, blur = s * (hot ? 0.12f : 0.17f) * MathF.Min(1.4f, i);
        foreach (var l in lines) Stroke(l, glow, (l.Frame ? GlowFrame : GlowGlyph) * i, WidthOf(l) * GlowWiden, (glow, glowShadow, blur));
        float rimAlpha = (hot ? 0.85f : 1) * MathF.Min(1, 0.35f + 0.65f * i);
        foreach (var l in lines) Stroke(l, rim, (l.Frame ? 0.7f : 1) * rimAlpha, WidthOf(l));
        foreach (var l in lines)
        {
            float w = WidthOf(l);
            if (l.Frame) Stroke(l, core, 0.28f * i, MathF.Max(0.5f, w * 0.3f));
            else Stroke(l, core, (hot ? 0.75f : 1) * MathF.Min(1, i), MathF.Max(0.55f, w * 0.42f));
        }
        Finish();
    }

    /// <summary>
    /// One canvas stroke of <paramref name="line"/>: its blurred shadow first, if any (blur <c>b</c> a Gaussian of
    /// σ = b / 2, as canvas has it), then the line itself, <paramref name="width"/> wide, laid over what is painted.
    /// </summary>
    private void Stroke(TechLooks.Line line, (float R, float G, float B) colour, float alpha, float width,
        ((float R, float G, float B) Colour, float Alpha, float Blur)? shadow = null)
    {
        alpha = Math.Clamp(alpha, 0, 1);
        Measure(line);
        float half = width / 2;
        if (shadow is { } sh && sh.Blur > 0)
        {
            float k = 1 / (sh.Blur / 2 * MathF.Sqrt(2)), a = Math.Clamp(sh.Alpha, 0, 1) * alpha;
            for (int p = 0; p < _distance.Length; p++)
            {
                float d = _distance[p];
                Over(p, sh.Colour, a * 0.5f * (Erf((half - d) * k) + Erf((half + d) * k)));
            }
        }
        for (int p = 0; p < _distance.Length; p++)
        {
            float d = _distance[p];
            // The band's share of a pixel's width round its centre: a line thinner than a pixel covers less of it.
            float cover = MathF.Min(d + 0.5f, half) - MathF.Max(d - 0.5f, -half);
            if (cover > 0) Over(p, colour, alpha * MathF.Min(1, cover));
        }
    }

    /// <summary>Each pixel centre's distance to <paramref name="line"/>, in pixels.</summary>
    private void Measure(TechLooks.Line line)
    {
        float s = Side;
        Array.Fill(_distance, float.MaxValue);
        for (int n = 1; n < line.Points.Count; n++)
        {
            float ax = line.Points[n - 1].U * s, ay = line.Points[n - 1].V * s, bx = line.Points[n].U * s, by = line.Points[n].V * s;
            float dx = bx - ax, dy = by - ay, lengthSq = MathF.Max(dx * dx + dy * dy, 1e-6f);
            for (int y = 0; y < Side; y++)
                for (int x = 0; x < Side; x++)
                {
                    float px = x + 0.5f - ax, py = y + 0.5f - ay;
                    float t = Math.Clamp((px * dx + py * dy) / lengthSq, 0, 1);
                    float ex = px - dx * t, ey = py - dy * t, d = MathF.Sqrt(ex * ex + ey * ey);
                    int p = y * Side + x;
                    if (d < _distance[p]) _distance[p] = d;
                }
        }
    }

    /// <summary>Lays <paramref name="colour"/> at <paramref name="alpha"/> over pixel <paramref name="p"/>, premultiplied.</summary>
    private void Over(int p, (float R, float G, float B) colour, float alpha)
    {
        if (alpha <= 0) return;
        int at = p * 4;
        float keep = 1 - alpha;
        _paint[at] = colour.R * alpha + _paint[at] * keep;
        _paint[at + 1] = colour.G * alpha + _paint[at + 1] * keep;
        _paint[at + 2] = colour.B * alpha + _paint[at + 2] * keep;
        _paint[at + 3] = alpha + _paint[at + 3] * keep;
    }

    private void Finish()
    {
        for (int p = 0; p < _distance.Length; p++)
        {
            int at = p * 4;
            float a = _paint[at + 3];
            float un = a > 0 ? 1 / a : 0;
            Rgba[at] = Byte(_paint[at] * un);
            Rgba[at + 1] = Byte(_paint[at + 1] * un);
            Rgba[at + 2] = Byte(_paint[at + 2] * un);
            Rgba[at + 3] = Byte(a);
        }
    }

    private static byte Byte(float v) => (byte)Math.Clamp(MathF.Round(v * 255), 0, 255);

    private static (float R, float G, float B) Colour(uint rgb) =>
        (((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f);

    /// <summary>The error function, to 1.5e-7 (Abramowitz and Stegun 7.1.26).</summary>
    private static float Erf(float x)
    {
        float sign = MathF.Sign(x), ax = MathF.Abs(x), t = 1 / (1 + 0.3275911f * ax);
        float y = 1 - ((((1.061405429f * t - 1.453152027f) * t + 1.421413741f) * t - 0.284496736f) * t + 0.254829592f) * t * MathF.Exp(-ax * ax);
        return sign * y;
    }
}
