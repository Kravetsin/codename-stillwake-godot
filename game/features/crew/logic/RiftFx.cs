using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using Pens = Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// «Разлом» (<see cref="RiftLook"/>): a dark seam breathing along the edge at rest; on a swing a tear in space along its path,
/// bright-rimmed, far specks in it, each stretch closing toward the edge's middle as it ages, so it shuts from its tail.
/// </summary>
public sealed class RiftFx(RiftLook look) : BladeFx(look)
{
    /// <summary>The world's cells a speck may sit in, px.</summary>
    private const double SpeckCell = 3;

    private readonly List<(double X, double Y)[]> _run = [];
    private readonly List<(double X, double Y)> _outline = [], _was = [], _now = [], _side = [], _other = [], _lens = [];

    public override void Draw(Ink under, Ink dark, Ink over, Func<(double X, double Y), (double X, double Y)> local)
    {
        var samples = Trail.Samples;
        // Runs of stretches that leave something, each from the sample before its first.
        for (int i = 1; i < samples.Count; i++)
        {
            if (samples[i].Shown <= 0) continue;
            int from = i - 1;
            while (i + 1 < samples.Count && samples[i + 1].Shown > 0) i++;
            Tear(under, dark, over, local, from, i);
        }
        if (Now != null) Seam(under, dark, local, Now.Edge);
    }

    // A run of the tear from sample `from` to `to`: its void as bands between its squeezed edges, its rim round them, specks inside.
    private void Tear(Ink under, Ink dark, Ink over, Func<(double X, double Y), (double X, double Y)> local, int from, int to)
    {
        var samples = Trail.Samples;
        _run.Clear();
        for (int i = from; i <= to; i++) _run.Add(Squeezed(samples[i], i == from ? samples[from + 1].Shown : samples[i].Shown));
        _outline.Clear();
        foreach (var e in _run) _outline.Add(e[0]);
        var (oldest, newest) = (_run[0], _run[^1]);
        for (int j = 1; j < newest.Length; j++) _outline.Add(newest[j]);
        for (int i = _run.Count - 2; i >= 0; i--) _outline.Add(_run[i][^1]);
        for (int j = oldest.Length - 2; j >= 1; j--) _outline.Add(oldest[j]);
        double pixel = under.Feather;
        for (int k = 0; k < _outline.Count; k++)
        {
            var (a, b) = (_outline[k], _outline[(k + 1) % _outline.Count]);
            (a, b) = (local(a), local(b));
            under.Segment(a, b, look.RimPx * 2.6, Tone(look.Rim, look.RimAlpha * 0.35));
            under.Segment(a, b, Math.Max(look.RimPx, 0.6 * pixel), Tone(look.Rim, look.RimAlpha));
        }
        for (int i = 1; i < _run.Count; i++)
        {
            Local(_run[i - 1], local, _was);
            Local(_run[i], local, _now);
            dark.Band(_was, _now, Tone(look.Void, look.VoidAlpha));
        }
        Specks(over, local);
    }

    // A sample's edge drawn toward its middle as it ages: open for the first OpenShare of its life, then shut; a slow sweep opens less.
    private (double X, double Y)[] Squeezed(BladeTrail.Sample sample, double shown)
    {
        double age = 1 - Trail.Left(sample);
        double open = (age < look.OpenShare ? 1 : 1 - Smooth((age - look.OpenShare) / (1 - look.OpenShare))) * Math.Min(1, shown * look.ShownToOpen + 0.35);
        var mid = sample.Edge[sample.Edge.Length / 2];
        var edge = new (double X, double Y)[sample.Edge.Length];
        for (int j = 0; j < edge.Length; j++) edge[j] = Lerp(mid, sample.Edge[j], open);
        return edge;
    }

    // Far specks seen through the tear: fixed cells of the world, each holding one by chance, drawn where inside.
    private void Specks(Ink ink, Func<(double X, double Y), (double X, double Y)> local)
    {
        double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
        foreach (var p in _outline) (x0, y0, x1, y1) = (Math.Min(x0, p.X), Math.Min(y0, p.Y), Math.Max(x1, p.X), Math.Max(y1, p.Y));
        for (int gx = (int)Math.Floor(x0 / SpeckCell); gx <= (int)Math.Ceiling(x1 / SpeckCell); gx++)
            for (int gy = (int)Math.Floor(y0 / SpeckCell); gy <= (int)Math.Ceiling(y1 / SpeckCell); gy++)
            {
                if (Pens.Hash(gx, gy) > look.SpecksPerCell) continue;
                (double X, double Y) at = ((gx + Pens.Hash(gy, gx)) * SpeckCell, (gy + Pens.Hash(gx + 7, gy)) * SpeckCell);
                if (Inside(_outline, at)) Disc(ink, local(at), 0.22, Tone(look.Specks, 0.75));
            }
    }

    // The seam at rest: a lens along the edge, its rim faint, the whole breathing.
    private void Seam(Ink under, Ink dark, Func<(double X, double Y), (double X, double Y)> local, IReadOnlyList<(double X, double Y)> edge)
    {
        double b = 0.5 + 0.5 * Math.Sin(ClockMs / look.BreathMs * Math.Tau);
        double half = (look.SeamPx.From + (look.SeamPx.To - look.SeamPx.From) * b) / 2;
        Lens(edge, half + 0.35, local);
        under.Fill(_lens, local(edge[edge.Count / 2]), Tone(look.Rim, look.SeamRim.From + (look.SeamRim.To - look.SeamRim.From) * b));
        Lens(edge, half, local);
        dark.Fill(_lens, local(edge[edge.Count / 2]), Tone(look.Void, 0.95));
    }

    private void Lens(IReadOnlyList<(double X, double Y)> e, double half, Func<(double X, double Y), (double X, double Y)> local)
    {
        int n = e.Count;
        _side.Clear();
        _other.Clear();
        for (int j = 0; j < n; j++)
        {
            var (prev, next) = (e[Math.Max(0, j - 1)], e[Math.Min(n - 1, j + 1)]);
            var d = Unit(next.X - prev.X, next.Y - prev.Y);
            double w = half * Math.Pow(Math.Sin(Math.PI * (0.06 + 0.88 * j / (n - 1))), 0.6);
            _side.Add((e[j].X - d.Y * w, e[j].Y + d.X * w));
            _other.Add((e[j].X + d.Y * w, e[j].Y - d.X * w));
        }
        _lens.Clear();
        _lens.Add(local(Lerp(e[0], e[1], -0.25)));
        foreach (var p in _side) _lens.Add(local(p));
        _lens.Add(local(Lerp(e[n - 1], e[n - 2], -0.25)));
        for (int j = _other.Count - 1; j >= 0; j--) _lens.Add(local(_other[j]));
    }

    private static void Local((double X, double Y)[] world, Func<(double X, double Y), (double X, double Y)> local, List<(double X, double Y)> into)
    {
        into.Clear();
        foreach (var p in world) into.Add(local(p));
    }

    private static bool Inside(List<(double X, double Y)> shape, (double X, double Y) p)
    {
        bool inside = false;
        for (int i = 0, j = shape.Count - 1; i < shape.Count; j = i++)
            if ((shape[i].Y > p.Y) != (shape[j].Y > p.Y) && p.X < (shape[j].X - shape[i].X) * (p.Y - shape[i].Y) / (shape[j].Y - shape[i].Y) + shape[i].X)
                inside = !inside;
        return inside;
    }
}
