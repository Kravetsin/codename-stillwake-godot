using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// The vitals' pieces of light bent into a dial's ring (hud-vitals, the pick «dials»): the long strip's body (<see cref="LightStrip"/>)
/// laid round the circle once from its top, an arc of it cut out with round ends. The glow under a lit arc is the strip again,
/// wider, on an added layer. Angles in screen radians, y down, the top at −π/2, clockwise growing.
/// </summary>
public static class RingLight
{
    /// <summary>Where along the strip the ring's top starts, past its baked start, in block units.</summary>
    private const float From = 10;

    /// <summary>How many steps a whole turn is drawn in, and each round end.</summary>
    private const int Turn = 72, End = 5;

    /// <summary>
    /// The arc from <paramref name="from"/> to <paramref name="to"/> of a ring <paramref name="radius"/> round <paramref name="centre"/>,
    /// its light <paramref name="light"/> thick (the strip's ten rows of light, its soft margin round them), drawn in
    /// <paramref name="strip"/> with round ends; a whole turn closes on itself.
    /// </summary>
    public static void Arc(CanvasItem on, Texture2D strip, Vector2 centre, float radius, float light, float from, float to, Color modulate)
    {
        if (modulate.A <= 0.004f || to - from <= 0.003f) return;
        var size = strip.GetSize();
        float half = light * size.Y / 10 / 2, outer = radius + half, span = (to - from) * radius;
        bool whole = to - from >= Mathf.Tau - 1e-3f;
        float cap = whole ? 0 : half;
        // Along the ring from its start, in block units: the round ends' samples, then the body's.
        int body = Math.Max(2, (int)MathF.Ceiling((to - from) / Mathf.Tau * Turn)), ends = whole ? 0 : End;
        int n = body + 1 + 2 * ends;
        var points = new Vector2[2 * n];
        var uvs = new Vector2[2 * n];
        var colours = new Color[2 * n];
        float start = Mathf.PosMod(from + MathF.PI / 2, Mathf.Tau) * radius;
        for (int i = 0; i < n; i++)
        {
            float s = i < ends ? -cap + cap * i / ends
                : i > ends + body ? span + cap * (i - ends - body) / ends
                : span * (i - ends) / body;
            float past = s < 0 ? -s : s > span ? s - span : 0;
            float h = past > 0 ? MathF.Sqrt(Math.Max(0, half * half - past * past)) : half;
            float a = from + s / radius, u = (From + start + s) / size.X;
            var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
            points[2 * i] = centre + dir * (radius + h);
            points[2 * i + 1] = centre + dir * (radius - h);
            uvs[2 * i] = new Vector2(u, (outer - radius - h) / (2 * half));
            uvs[2 * i + 1] = new Vector2(u, (outer - radius + h) / (2 * half));
            colours[2 * i] = colours[2 * i + 1] = modulate;
        }
        var indices = new int[6 * (n - 1)];
        for (int i = 0; i < n - 1; i++)
        {
            int k = 2 * i;
            (indices[6 * i], indices[6 * i + 1], indices[6 * i + 2]) = (k, k + 1, k + 2);
            (indices[6 * i + 3], indices[6 * i + 4], indices[6 * i + 5]) = (k + 1, k + 3, k + 2);
        }
        RenderingServer.CanvasItemAddTriangleArray(on.GetCanvasItem(), indices, points, colours, uvs, texture: strip.GetRid());
    }

    /// <summary>The even glow under a lit arc, drawn on an added layer: the strip's light again, spread both ways, as a piece's glow is.</summary>
    public static void Glow(CanvasItem on, Texture2D strip, Vector2 centre, float radius, float light, float from, float to, float alpha)
    {
        foreach (var (spread, a) in new[] { (5f, 0.12f), (2.5f, 0.2f) })
            Arc(on, strip, centre, radius, light * (10 + 2 * spread) / 10, from, to, Colors.White with { A = a * alpha });
    }

    /// <summary>A soft round light of <paramref name="radius"/> at <paramref name="at"/>.</summary>
    public static void Spot(CanvasItem on, Vector2 at, float radius, Color colour)
    {
        if (colour.A <= 0.003f || radius <= 0) return;
        on.DrawTextureRect(PixelArt.Glow, new Rect2(at - new Vector2(radius, radius), new Vector2(2 * radius, 2 * radius)), false, colour);
    }

    /// <summary>
    /// An arc of a dial's ring in <paramref name="colour"/>, as thick as every dial's: the white strip tinted, for the rings that keep
    /// their own colours (the canister's round the oxygen sign, the purge spot's).
    /// </summary>
    public static void Tinted(CanvasItem on, Vector2 centre, float from, float to, Color colour) =>
        Arc(on, LightStrip.Of("white", 0), centre, Vitals.RingRadius, Vitals.RingLight, from, to, colour);
}
