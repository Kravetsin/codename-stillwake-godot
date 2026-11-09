using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using Pens = Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The air a tool works, showing its reach while it is used (tool-reach), drawn in the arms' frame, the shoulder at the origin,
/// in pixels. A swing's blur: dry-brush arcs round the shoulder near the range, left where they were swept, over the angles the blade
/// has swept in this cut, their front on the blade, the inner ones shorter, broken afresh every step as the grinder's are; a
/// thrust's dust carried forward along it to the range («Пыль по ходу удара»). A running drill's or grinder's zone, its working part grown: the
/// bit's point a cone to its range, the wheel's forward half a disc, in a few broken dry-brush strokes flickering round as it
/// turns («Сухая кисть»). Never clipped by walls: it shows how far and how wide, the hits show what.
/// </summary>
public sealed class AirCuts
{
    /// <summary>
    /// A thrust's cut: begun at <paramref name="Start"/> ms toward <paramref name="Aim"/> radians, <paramref name="Half"/> its arc
    /// either side, out to <paramref name="Range"/> from <paramref name="Inner"/> pixels, thrown out over <paramref name="SweepMs"/>,
    /// gone <paramref name="FadeMs"/> after.
    /// </summary>
    private readonly record struct Cut(double Start, double Aim, double Half, double Range, double Inner, double SweepMs, double FadeMs, int Seed);

    /// <summary>
    /// A swing's cut, entered at <paramref name="from"/> radians: where the blade has been, each frame the shoulder's place and the
    /// blade's way out from it in the world, so it stays where it was swept as the body moves on; how far the blade has turned to each.
    /// </summary>
    private sealed class Swish(double from, int seed)
    {
        public double From { get; } = from;
        public int Seed { get; } = seed;
        public double To { get; set; }
        public double Range { get; set; }
        public double Last { get; set; }
        public List<(double Along, (double X, double Y) At, (double X, double Y) Out)> Samples { get; } = [];
    }

    /// <summary>
    /// A running tool's zone this frame: a <paramref name="Disc"/>'s forward half round <paramref name="At"/>, <paramref name="Size"/>
    /// its radius, or a cone from <paramref name="At"/> along <paramref name="Toward"/> (a unit), <paramref name="Size"/> long.
    /// </summary>
    public readonly record struct Zone(bool Disc, (double X, double Y) At, (double X, double Y) Toward, double Size);

    private readonly List<Cut> _cuts = [];
    private readonly List<Swish> _swishes = [];
    private readonly List<(double X, double Y)> _line = [];
    private double _clock, _ranFor, _off;
    private Zone? _zone;
    private int _seeds;

    /// <summary>Whether anything is drawn now.</summary>
    public bool Live => _cuts.Count > 0 || _swishes.Count > 0 || On > 0;

    /// <summary>How much a running tool's zone shows: eased in over <see cref="WorkLooks.ZoneIn"/> ms, out over <see cref="WorkLooks.ZoneOut"/> after it stops.</summary>
    private double On => _zone == null ? 0
        : _off > 0 ? 1 - Pens.Clamp(_off / WorkLooks.ZoneOut) : Pens.Clamp(_ranFor / WorkLooks.ZoneIn);

    /// <summary>
    /// A thrust begins cutting the air now toward <paramref name="aim"/>, its fan <paramref name="half"/> radians either side and
    /// <paramref name="range"/> pixels long, its tip reaching <paramref name="inner"/>, thrown out over <paramref name="sweepMs"/>;
    /// seen <paramref name="fadeMs"/> after.
    /// </summary>
    public void Thrust(double aim, double half, double range, double inner, double sweepMs, double fadeMs) =>
        _cuts.Add(new(_clock, aim, half, range, Math.Min(inner, range), Math.Max(1, sweepMs), fadeMs, ++_seeds));

    /// <summary>
    /// Whether a swing's blade, its edge at <paramref name="edge"/> and <paramref name="was"/> a frame ago, is cutting the air: within
    /// its fan, <paramref name="half"/> either side of <paramref name="aim"/>, and moving, not held on a bite.
    /// </summary>
    public static bool Cutting((double X, double Y) edge, (double X, double Y) was, double aim, double half) =>
        Math.Abs(Math.IEEERemainder(Math.Atan2(edge.Y, edge.X) - aim, 2 * Math.PI)) <= half
        && (edge.X - was.X) * (edge.X - was.X) + (edge.Y - was.Y) * (edge.Y - was.Y) >= WorkLooks.CutMoving * WorkLooks.CutMoving;

    /// <summary>
    /// A swing's blade is cutting now, at <paramref name="to"/> radians, having entered its fan at <paramref name="from"/>: the same
    /// <paramref name="from"/> goes on the cut drawn, another begins one; out to <paramref name="range"/>. In the world, the shoulder is
    /// <paramref name="at"/> and a pixel out toward the blade <paramref name="out"/>. Let go, it fades out where it was.
    /// </summary>
    public void Swing(double from, double to, double range, (double X, double Y) at, (double X, double Y) @out)
    {
        if (_swishes.Count == 0 || _swishes[^1].From != from) _swishes.Add(new(from, ++_seeds) { To = from });
        var w = _swishes[^1];
        double along = w.Samples.Count == 0 ? 0 : w.Samples[^1].Along + Math.Abs(to - w.To);
        (w.To, w.Range, w.Last) = (to, range, _clock);
        w.Samples.Add((along, at, @out));
    }

    /// <summary><paramref name="elapsedMs"/> on, a running tool's <paramref name="zone"/>, none while none runs.</summary>
    public void Step(double elapsedMs, Zone? zone)
    {
        _clock += elapsedMs;
        _cuts.RemoveAll(c => _clock - c.Start > c.SweepMs + c.FadeMs * 1.2);
        _swishes.RemoveAll(w => _clock - w.Last > WorkLooks.ZoneOut);
        if (zone is { } z)
        {
            _ranFor = _off > 0 ? 0 : _ranFor + elapsedMs;
            (_zone, _off) = (z, 0);
        }
        else if (_zone != null)
        {
            _off += elapsedMs;
            _ranFor += elapsedMs;
            if (_off >= WorkLooks.ZoneOut) (_zone, _ranFor, _off) = (null, 0, 0);
        }
    }

    /// <summary>All of it as it is now, into <paramref name="ink"/>; a swing's cut from the world by <paramref name="local"/>.</summary>
    public void Draw(Ink ink, Func<(double X, double Y), (double X, double Y)> local)
    {
        foreach (var cut in _cuts) Dust(ink, cut, _clock - cut.Start);
        foreach (var swish in _swishes) Swept(ink, swish, 1 - Pens.Clamp((_clock - swish.Last) / WorkLooks.ZoneOut), local);
        if (_zone is { } zone && On > 0) Brush(ink, zone, _ranFor, On);
    }

    // The dust a thrust tears off, each mote carried out along it to its range.
    private void Dust(Ink ink, Cut c, double age)
    {
        const int n = 16;
        var (dx, dy) = (Math.Cos(c.Aim), Math.Sin(c.Aim));
        for (int j = 0; j < n; j++)
        {
            int key = j + 97 * c.Seed;
            double life = c.FadeMs * (0.7 + 0.5 * Pens.Hash(key, 3)), h = Pens.Hash(key, 1);
            double v = (Pens.Hash(key, 5) - 0.5) * 1.6, tip = c.Inner, inner = tip - 16;
            double u0 = inner + 2 + (tip - inner - 2) * Pens.Hash(key, 2), wide = Math.Tan(c.Half);
            double born = c.SweepMs * (0.2 + 0.6 * h);
            (double X, double Y) at(double tau)
            {
                double u = u0 + (c.Range - u0) * (1 - Math.Exp(-Math.Max(0, tau) / 55)), off = v * u * wide;
                return (dx * u - dy * off, dy * u + dx * off);
            }
            double t = age - born;
            if (t < 0 || t > life) continue;
            double q = t / life, fade = Math.Pow(1 - q, 0.7);
            var tone = j % 3 != 0 ? WorkLooks.AirInk.Faded((float)(0.9 * fade)) : WorkLooks.DustInk.Faded((float)(0.75 * fade));
            double width = 0.36 + 0.22 * Pens.Hash(key, 6);
            var last = at(t - 30);
            for (int i = 1; i <= 5; i++)
            {
                var next = at(t - 30 * (1 - i / 5.0));
                ink.Segment(last, next, width, tone);
                last = next;
            }
        }
    }

    // A swing's blur, as the grinder's face blurred in turning: two arcs in from the range over where the blade has been, each
    // ending on the blade, the outer longer; stepped as the zones are.
    private void Swept(Ink ink, Swish w, double k, Func<(double X, double Y), (double X, double Y)> local)
    {
        var samples = w.Samples;
        double swept = samples.Count > 1 ? samples[^1].Along : 0;
        if (k <= 0 || swept < 0.02) return;
        int step = (int)Math.Floor(_clock / WorkLooks.BrushStepMs);
        (double R, double Length)[] arcs = [(w.Range - 0.4, 1), (w.Range - 4, 0.8)];
        for (int s = 0; s < arcs.Length; s++)
        {
            var (r, len) = arcs[s];
            if (r <= 0) continue;
            Bristles(ink, (q, off) =>
            {
                double along = swept * (1 - len * (1 - q));
                int i = 1;
                while (i < samples.Count - 1 && samples[i].Along < along) i++;
                var (a0, at0, out0) = samples[i - 1];
                var (a1, at1, out1) = samples[i];
                double t = a1 > a0 ? Math.Clamp((along - a0) / (a1 - a0), 0, 1) : 1, rr = r + off;
                double x = at0.X + (at1.X - at0.X) * t + (out0.X + (out1.X - out0.X) * t) * rr;
                double y = at0.Y + (at1.Y - at0.Y) * t + (out0.Y + (out1.Y - out0.Y) * t) * rr;
                return local((x, y));
            }, Math.Max(4, (int)Math.Ceiling(swept * len * r / 2.5)), w.Seed * 5 + s, step, k);
        }
    }

    // The working part grown and blurred: a few broken strokes of a dry brush, their bristles gapped, a step round every BrushStepMs.
    private void Brush(Ink ink, Zone z, double clock, double k)
    {
        int step = (int)Math.Floor(clock / WorkLooks.BrushStepMs);
        var (dx, dy) = z.Toward;
        var (nx, ny) = (-dy, dx);
        if (!z.Disc)
        {
            double length = z.Size, turn = step * WorkLooks.BrushStepMs * 2 * Math.PI / WorkLooks.DrillTurnMs;
            double Wide(double x) => WorkLooks.ConeBase * Pens.Clamp(1 - x / length);
            // Five slanted strokes round the cone, as the flutes of a bit spinning too fast to see.
            for (int s = 0; s < 5; s++)
            {
                double x0 = length * s / 5 + turn * 9 / (2 * Math.PI) % (length / 5);
                Bristles(ink, (q, off) =>
                {
                    double x = Math.Min(length - 0.2, x0 + q * 5.5), y = Wide(x) * 0.9 * Math.Sin(-Math.PI / 2 + q * Math.PI) + off;
                    return (z.At.X + dx * x + nx * y, z.At.Y + dy * x + ny * y);
                }, 12, s + 1, step, k);
            }
            return;
        }
        double r0 = z.Size, wheel = step * WorkLooks.BrushStepMs * 2 * Math.PI / WorkLooks.DiscTurnMs;
        // Arcs round the centre at four radii, as the disc's face blurred in turning, each a stretch of the forward half.
        (double R, double Phase, double Length)[] arcs = [(r0 - 0.4, 0, 1.3), (r0 - 4, 1.4, 1.0), (r0 - 8.5, 0.6, 0.9), (r0 - 13, 2.3, 0.7)];
        for (int s = 0; s < arcs.Length; s++)
        {
            var (r, phase, len) = arcs[s];
            if (r <= 0) continue;
            double a0 = -Math.PI / 2 - len + (phase + wheel) % (Math.PI + len);
            Bristles(ink, (q, off) =>
            {
                double a = a0 + q * len;
                if (Math.Abs(a) >= Math.PI / 2 - 0.05) return null;
                double rr = r + off, c = Math.Cos(a), sn = Math.Sin(a);
                return (z.At.X + (dx * c + nx * sn) * rr, z.At.Y + (dy * c + ny * sn) * rr);
            }, 14, s + 7, step, k);
        }
    }

    // Three bristles along a path(share, offset across), each broken where it runs dry this step, inked as a thin pen.
    private void Bristles(Ink ink, Func<double, double, (double X, double Y)?> path, int n, int seed, int step, double k)
    {
        for (int b = 0; b < 3; b++)
        {
            _line.Clear();
            var tone = WorkLooks.AirInk.Faded((float)((0.42 + 0.18 * Pens.Hash(b, seed)) * k));
            double width = 0.26 + 0.1 * Pens.Hash(seed, b);
            for (int i = 0; i < n; i++)
            {
                var p = path(i / (double)(n - 1), (b - 1) * 0.32);
                bool dry = p == null || Pens.Hash(seed * 31 + b * 7 + i, step) < 0.22;
                if (!dry) _line.Add(p!.Value);
                if ((dry || i == n - 1) && _line.Count > 1) ink.Pen(_line, width, tone, seed + b, 0.2, 0.3);
                if (dry || i == n - 1) _line.Clear();
            }
        }
    }
}
