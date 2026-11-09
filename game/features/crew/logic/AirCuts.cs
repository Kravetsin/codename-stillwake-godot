using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using Pens = Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The air a tool works, showing its reach while it is used (tool-reach), drawn in the arms' frame, the shoulder at the origin,
/// in pixels. A stroke's fan, its arc wide and its range long: the swing's dust torn off where the blade passes and carried on
/// round the arc the way it goes, each mote at its own radius out to the range, slowing to a stop by the arc's end; the thrust's
/// carried forward along it to the range («Пыль по ходу удара»). A running drill's or grinder's zone, its working part grown: the
/// bit's point a cone to its range, the wheel's forward half a disc, in a few broken dry-brush strokes flickering round as it
/// turns («Сухая кисть»). Never clipped by walls: it shows how far and how wide, the hits show what.
/// </summary>
public sealed class AirCuts
{
    /// <summary>
    /// A stroke's cut: begun at <paramref name="Start"/> ms, a <paramref name="Thrust"/> or a swing, toward <paramref name="Aim"/>
    /// radians, <paramref name="Half"/> its arc either side, out to <paramref name="Range"/> from <paramref name="Inner"/> pixels, the
    /// blade crossing it in <paramref name="SweepMs"/>, gone <paramref name="FadeMs"/> after.
    /// </summary>
    private readonly record struct Cut(double Start, bool Thrust, double Aim, double Half, double Range, double Inner, double SweepMs, double FadeMs, int Seed);

    /// <summary>
    /// A running tool's zone this frame: a <paramref name="Disc"/>'s forward half round <paramref name="At"/>, <paramref name="Size"/>
    /// its radius, or a cone from <paramref name="At"/> along <paramref name="Toward"/> (a unit), <paramref name="Size"/> long.
    /// </summary>
    public readonly record struct Zone(bool Disc, (double X, double Y) At, (double X, double Y) Toward, double Size);

    private readonly List<Cut> _cuts = [];
    private readonly List<(double X, double Y)> _line = [];
    private double _clock, _ranFor, _off;
    private Zone? _zone;
    private int _seeds;

    /// <summary>Whether anything is drawn now.</summary>
    public bool Live => _cuts.Count > 0 || On > 0;

    /// <summary>How much a running tool's zone shows: eased in over <see cref="WorkLooks.ZoneIn"/> ms, out over <see cref="WorkLooks.ZoneOut"/> after it stops.</summary>
    private double On => _zone == null ? 0
        : _off > 0 ? 1 - Pens.Clamp(_off / WorkLooks.ZoneOut) : Pens.Clamp(_ranFor / WorkLooks.ZoneIn);

    /// <summary>
    /// A stroke begins cutting the air now: a <paramref name="thrust"/> or a swing toward <paramref name="aim"/>, its fan
    /// <paramref name="half"/> radians either side and <paramref name="range"/> pixels long, its blade reaching <paramref name="inner"/>,
    /// crossing the fan in <paramref name="sweepMs"/>, seen <paramref name="fadeMs"/> after.
    /// </summary>
    public void Strike(bool thrust, double aim, double half, double range, double inner, double sweepMs, double fadeMs) =>
        _cuts.Add(new(_clock, thrust, aim, half, range, Math.Min(inner, range), Math.Max(1, sweepMs), fadeMs, ++_seeds));

    /// <summary><paramref name="elapsedMs"/> on, a running tool's <paramref name="zone"/>, none while none runs.</summary>
    public void Step(double elapsedMs, Zone? zone)
    {
        _clock += elapsedMs;
        _cuts.RemoveAll(c => _clock - c.Start > c.SweepMs + c.FadeMs * 1.2);
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

    /// <summary>All of it as it is now, into <paramref name="ink"/>.</summary>
    public void Draw(Ink ink)
    {
        foreach (var cut in _cuts)
            if (cut.Thrust) Dust(ink, cut, _clock - cut.Start);
            else Wave(ink, cut, _clock - cut.Start);
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

    // A swing's wave (axe-wave): a crescent across the fan born at the blade, running out to the range, slowing, and gone there;
    // three strands, the leading one strongest, the others behind it thinner and broken, each widest at the middle.
    private static void Wave(Ink ink, Cut c, double age)
    {
        double p = age / c.SweepMs;
        if (p < 0 || p > 1) return;
        double r = c.Inner + (c.Range - c.Inner) * (1 - (1 - p) * (1 - p)), fade = 1 - p * p;
        const int Pieces = 24;
        for (int strand = 0; strand < 3; strand++)
        {
            double rr = r - strand * 1.1, span = c.Half * (1 - 0.12 * strand);
            var tone = (strand == 0 ? WorkLooks.AirInk : WorkLooks.DustInk).Faded((float)(fade * (strand == 0 ? 0.9 : strand == 1 ? 0.5 : 0.28)));
            (double X, double Y) At(double t)
            {
                // The horns lag the middle a little, so it reads as a crescent thrown out, not a ring.
                double a = c.Aim - span + 2 * span * t, back = 0.9 * (1 - Math.Sin(Math.PI * t));
                return (Math.Cos(a) * (rr - back), Math.Sin(a) * (rr - back));
            }
            var last = At(0);
            for (int i = 1; i <= Pieces; i++)
            {
                double t = i / (double)Pieces;
                var next = At(t);
                if (strand == 0 || Pens.Hash(c.Seed * 31 + strand * 7 + i, 8) > 0.2)
                    ink.Segment(last, next, (0.22 + 0.5 * Math.Sin(Math.PI * (t - 0.5 / Pieces))) * (1 - 0.3 * strand), tone);
                last = next;
            }
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
