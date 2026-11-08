using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Research;

/// <summary>A point of a stylus stroke: where it is, how thick there, and whether the stylus lifts after it.</summary>
public readonly record struct InkPoint(float X, float Y, float Thick, bool Lift);

/// <summary>A stroke as triangles (<see cref="BoardInk.Mesh"/>): their corners, how much ink each has, three indices a triangle, and where the stroke's tip is.</summary>
public readonly record struct InkMesh((float X, float Y)[] Points, float[] Alpha, int[] Indices, (float X, float Y) Tip);

/// <summary>How a shape is sketched at a step of its pencil.</summary>
/// <param name="Edge">How strong its outline is.</param>
/// <param name="Side">How much of each side of its box is drawn, from the corners.</param>
/// <param name="Gaps">How much of the breaks in its outline show.</param>
/// <param name="Hatch">How strong its hatching is.</param>
public readonly record struct BoardSketch(float Edge, float Side, float Gaps, float Hatch);

/// <summary>
/// What the board is written with ([the pile](../../../../docs/decisions/2026-11-02-board-pile.md)): strokes of a stylus on the screen, which wander a little,
/// swell and now and then lift. The same thing always gets the same strokes, so nothing shimmers between frames or worlds.
/// </summary>
public static class BoardInk
{
    private static readonly BoardSketch[] Sketches =
    [
        new(0.3f, 0.14f, 1, 0),
        new(0.46f, 0.26f, 1, 0),
        new(0.64f, 0.42f, 0.5f, 0.12f),
        new(0.84f, 0.68f, 0, 0.24f),
        new(1, 1, 0, 0.38f),
    ];

    /// <summary>A number from a thing's id that stays the same on every run: FNV-1a, for <c>GetHashCode</c> changes per process.</summary>
    public static uint Hash(string id)
    {
        uint hash = 2166136261;
        foreach (char c in id) hash = (hash ^ c) * 16777619;
        return hash;
    }

    /// <summary>What a thing's strokes wander by.</summary>
    public static float Seed(string id) => Hash(id) % 997 / 40f;

    /// <summary>A stroke from one point to another, its middle bowed out by <paramref name="sag"/>, a point every <paramref name="step"/>.</summary>
    public static InkPoint[] Stroke(float x0, float y0, float x1, float y1, float seed, float sag = 0, float step = 9)
    {
        float dx = x1 - x0, dy = y1 - y0, length = MathF.Max(MathF.Sqrt(dx * dx + dy * dy), 1), nx = -dy / length, ny = dx / length;
        int n = Math.Max(2, (int)MathF.Ceiling(length / step));
        var points = new InkPoint[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float t = (float)i / n, along = t * length, envelope = MathF.Min(1, MathF.Min(t * 6, (1 - t) * 6)), bow = sag * MathF.Sin(MathF.PI * t);
            float wander = (MathF.Sin(along * 0.021f + seed) * 1.1f + MathF.Sin(along * 0.057f + seed * 1.7f) * 0.6f) * envelope;
            points[i] = new InkPoint(
                x0 + dx * t + nx * (bow + wander),
                y0 + dy * t + ny * (bow + wander),
                0.8f + 0.2f * MathF.Sin(along * 0.04f + seed * 2.3f),
                length > 90 && MathF.Sin(along * 0.031f + seed * 3.1f) > 0.993f);
        }
        return points;
    }

    /// <summary>The four strokes of a box written by hand, running <paramref name="over"/> past its corners.</summary>
    public static InkPoint[][] Box(float x, float y, float wide, float tall, float seed, float over = 3, float step = 9)
    {
        float Lean(int k) => MathF.Sin(seed * 3.1f + k * 2.7f) * 1.6f;
        return
        [
            Stroke(x - over, y + Lean(0), x + wide + over, y + Lean(1), seed, 0, step),
            Stroke(x + wide + Lean(2), y - over, x + wide + Lean(3), y + tall + over, seed + 1, 0, step),
            Stroke(x + wide + over, y + tall + Lean(4), x - over, y + tall + Lean(5), seed + 2, 0, step),
            Stroke(x + Lean(6), y + tall + over, x + Lean(7), y - over, seed + 3, 0, step),
        ];
    }

    /// <summary>What is left of a box at (<paramref name="x"/>, <paramref name="y"/>) when only <paramref name="side"/> of each side is drawn: eight strokes from its corners.</summary>
    public static InkPoint[][] Corners(float x, float y, float wide, float tall, float side, float seed, float step = 12)
    {
        float right = x + wide, bottom = y + tall, across = wide * side, down = tall * side;
        (float, float, float, float)[] from =
        [
            (x, y, x + across, y), (right, y, right - across, y), (right, bottom, right - across, bottom), (x, bottom, x + across, bottom),
            (x, y, x, y + down), (x, bottom, x, bottom - down), (right, y, right, y + down), (right, bottom, right, bottom - down),
        ];
        var strokes = new InkPoint[from.Length][];
        for (int i = 0; i < from.Length; i++) strokes[i] = Stroke(from[i].Item1, from[i].Item2, from[i].Item3, from[i].Item4, seed + i, 0, step);
        return strokes;
    }

    /// <summary>A thread from the edge of one box to the edge of another, with its own lean.</summary>
    /// <param name="key">What it is a thread between, which sets its lean.</param>
    /// <param name="halfWide">Half a box's width.</param>
    /// <param name="halfTall">Half a box's height.</param>
    public static InkPoint[] Thread(string key, float x0, float y0, float x1, float y1, float halfWide, float halfTall)
    {
        (float X, float Y) Edge(float px, float py, float qx, float qy)
        {
            float dx = qx - px, dy = qy - py, k = MathF.Min(1, MathF.Min(halfWide / MathF.Max(MathF.Abs(dx), 1), halfTall / MathF.Max(MathF.Abs(dy), 1)));
            return (px + dx * k, py + dy * k);
        }
        var (sx, sy) = Edge(x0, y0, x1, y1);
        var (ex, ey) = Edge(x1, y1, x0, y0);
        uint hash = Hash(key);
        float length = MathF.Sqrt((ex - sx) * (ex - sx) + (ey - sy) * (ey - sy));
        return Stroke(sx, sy, ex, ey, hash % 997 / 30f, (hash % 2 == 1 ? 1 : -1) * MathF.Min(14, length * 0.05f), 10);
    }

    /// <summary>
    /// A stroke as one list of triangles, <paramref name="drawn"/> of the way along: each piece a band as thick as the stylus pressed (never under
    /// <paramref name="least"/>, thinner at the ends), its edges fading to nothing over <paramref name="feather"/>, which smooths them; lifted pieces left out.
    /// One list draws in one call, where a line a piece cost the renderer a command each, every frame (2.5 ms a frame on the board).
    /// </summary>
    public static InkMesh Mesh(InkPoint[] stroke, float width, float least, float feather, float drawn = 1)
    {
        int last = stroke.Length - 1;
        float upto = drawn * last;
        var points = new List<(float X, float Y)>();
        var alpha = new List<float>();
        var indices = new List<int>();
        var tip = (stroke[0].X, stroke[0].Y);
        for (int i = 0; i < last && i < upto; i++)
        {
            InkPoint a = stroke[i], b = stroke[i + 1];
            float part = MathF.Min(1, upto - i);
            (float X, float Y) to = (a.X + (b.X - a.X) * part, a.Y + (b.Y - a.Y) * part);
            tip = to;
            float dx = to.X - a.X, dy = to.Y - a.Y, length = MathF.Sqrt(dx * dx + dy * dy);
            if (a.Lift || length < 1e-4f) continue;
            float ends = MathF.Min(1, MathF.Min((i + 0.4f) / 2.4f, (last - i + 0.4f) / 2.4f));
            float half = MathF.Max(least, width * a.Thick * ends) / 2, nx = -dy / length, ny = dx / length;
            // The fade straddles the edge, so the band looks as thick as it is; one thinner than the fade is fainter instead.
            float body = MathF.Max(0, half - feather / 2), edge = half + feather / 2, ink = MathF.Min(1, 2 * half / feather);
            int at = points.Count;
            foreach (var (x, y) in (ReadOnlySpan<(float, float)>)[(a.X, a.Y), to])
                foreach (float across in (ReadOnlySpan<float>)[-edge, -body, body, edge])
                {
                    points.Add((x + nx * across, y + ny * across));
                    alpha.Add(MathF.Abs(across) == edge ? 0 : ink);
                }
            // Across the band, its edge, its body and its other edge: two triangles each, from this point to the next.
            for (int k = 0; k < 3; k++) indices.AddRange([at + k, at + k + 1, at + 4 + k, at + k + 1, at + 5 + k, at + 4 + k]);
        }
        return new InkMesh([.. points], [.. alpha], [.. indices], tip);
    }

    /// <summary>A hand-drawn loop round a point, its ends overlapping: what marks a thing just landed.</summary>
    public static (float X, float Y)[] Loop(float seed, float wide, float tall)
    {
        const int Steps = 36;
        var points = new (float, float)[Steps + 1];
        for (int i = 0; i <= Steps; i++)
        {
            float u = (float)i / Steps, angle = -2.1f + u * (MathF.Tau + 0.45f), wobble = 1 + 0.025f * MathF.Sin(angle * 3 + seed) + 0.015f * MathF.Sin(angle * 7 + seed * 2);
            points[i] = (MathF.Cos(angle) * wide * wobble, MathF.Sin(angle) * tall * wobble);
        }
        return points;
    }

    /// <summary>How <paramref name="pencil"/>, from 0 to 1, sketches a shape: five hard steps, not a slide.</summary>
    public static BoardSketch SketchOf(double pencil) => Sketches[pencil < 0.25 ? 0 : pencil < 0.5 ? 1 : pencil < 0.75 ? 2 : pencil < 0.95 ? 3 : 4];

    /// <summary>A smooth start and stop for <paramref name="u"/>, from 0 to 1.</summary>
    public static float Ease(float u) => u * u * (3 - 2 * u);

    /// <summary>The box round <paramref name="spots"/>, of things <paramref name="halfWide"/> by <paramref name="halfTall"/>; null for none.</summary>
    public static (double Left, double Top, double Right, double Bottom)? Around(IEnumerable<(double X, double Y)> spots, double halfWide, double halfTall)
    {
        (double, double, double, double)? box = null;
        foreach (var (x, y) in spots)
            box = box is var (left, top, right, bottom)
                ? (Math.Min(left, x - halfWide), Math.Min(top, y - halfTall), Math.Max(right, x + halfWide), Math.Max(bottom, y + halfTall))
                : (x - halfWide, y - halfTall, x + halfWide, y + halfTall);
        return box;
    }
}
