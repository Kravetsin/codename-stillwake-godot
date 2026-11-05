using System;
using System.Collections.Generic;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The trail a swung tool's downstroke leaves (damage-looks, "flicks"): three pale dry speed lines drawn after the blade, the
/// outer longest, tapering off behind it, fading after the stroke. Built from where the blade's edge really was over the last
/// few milliseconds, kept frame by frame, never from an angle that could wrap: it stays on the blade aimed any way, straight up
/// too. Points are in the arms' frame, the shoulder at the origin; a frame between two seen is turned round the shoulder.
/// </summary>
public sealed class SwingTrail
{
    private readonly List<(double Ms, (double X, double Y) From, (double X, double Y) To)> _seen = [];
    private readonly List<((double X, double Y) From, (double X, double Y) To, double Q)> _sweep = [];
    private readonly List<(double X, double Y)> _line = [];
    private double _clock, _start = double.NaN, _down;
    private bool _striking;

    /// <summary>How long ago the downstroke began, in ms; none before the first.</summary>
    public double? Since => double.IsNaN(_start) ? null : _clock - _start;

    /// <summary>How brightly it shows: whole through the downstroke, dying away over <see cref="WorkLooks.FlickFade"/> of it after.</summary>
    public double Light => Since is { } since ? 1 - Math.Clamp((since - _down) / (WorkLooks.FlickFade * _down), 0, 1) : 0;

    /// <summary>
    /// <paramref name="elapsedMs"/> on, the blade's edge from <paramref name="from"/> to <paramref name="to"/> (none while no tool
    /// is swung), on its downstroke of <paramref name="downMs"/> while <paramref name="striking"/>; a new downstroke starts afresh.
    /// </summary>
    public void Step(double elapsedMs, (double X, double Y)? from, (double X, double Y)? to, bool striking, double downMs)
    {
        _clock += elapsedMs;
        if (striking && !_striking)
        {
            (_start, _down) = (_clock, downMs);
            _seen.Clear();
        }
        _striking = striking;
        if (Since == null || from is not { } f || to is not { } t) return;
        _seen.Add((_clock, f, t));
        // Kept no further back than the trail reaches, and one more to turn from.
        double oldest = _clock - WorkLooks.FlickLength * _down;
        while (_seen.Count > 2 && _seen[1].Ms < oldest) _seen.RemoveAt(0);
    }

    /// <summary>The edge <paramref name="ago"/> ms back, as last seen then; none if nothing was.</summary>
    public ((double X, double Y) From, (double X, double Y) To)? Edge(double ago)
    {
        if (_seen.Count == 0) return null;
        double at = _clock - ago;
        if (at <= _seen[0].Ms) return (_seen[0].From, _seen[0].To);
        for (int i = 1; i < _seen.Count; i++)
        {
            if (_seen[i].Ms < at) continue;
            var (a, b) = (_seen[i - 1], _seen[i]);
            double q = (at - a.Ms) / Math.Max(1e-9, b.Ms - a.Ms);
            return (Turned(a.From, b.From, q), Turned(a.To, b.To, q));
        }
        return (_seen[^1].From, _seen[^1].To);
    }

    // Between two places of a point swung round the shoulder: turned the short way and stretched, not cut across the arc.
    private static (double X, double Y) Turned((double X, double Y) a, (double X, double Y) b, double q)
    {
        double ra = Math.Sqrt(a.X * a.X + a.Y * a.Y), rb = Math.Sqrt(b.X * b.X + b.Y * b.Y);
        double from = Math.Atan2(a.Y, a.X), angle = from + (SwingArc.Near(from, Math.Atan2(b.Y, b.X)) - from) * q, r = ra + (rb - ra) * q;
        return (Math.Cos(angle) * r, Math.Sin(angle) * r);
    }

    /// <summary>The flicks as they are now, into <paramref name="ink"/>; nothing once they have died away.</summary>
    public void Draw(Ink ink)
    {
        double k = Light;
        if (k <= 0 || Since is not { } since) return;
        double back = Math.Min(since, WorkLooks.FlickLength * _down);
        _sweep.Clear();
        for (int i = 0; i <= WorkLooks.FlickSamples; i++)
        {
            double q = (double)i / WorkLooks.FlickSamples;
            if (Edge(back * (1 - q)) is var (f, t)) _sweep.Add((f, t, q));
        }
        var tone = WorkLooks.FlickInk.Faded((float)k);
        foreach (var (u, share, width) in WorkLooks.Flicks)
        {
            _line.Clear();
            foreach (var (f, t, q) in _sweep)
                if (q >= 1 - share) _line.Add((f.X + (t.X - f.X) * u, f.Y + (t.Y - f.Y) * u));
            ink.Pen(_line, width, tone, (int)(u * 10), 0.85, 0.04);
        }
    }
}
