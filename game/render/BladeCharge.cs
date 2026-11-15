using System;
using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A charge on a held blade, the user's pick «Срыв разряда» (spear-effects): its area fizzing at rest, and bolts torn off its
/// trailing edge as it moves, each kept where it tore, its shape held, eaten from both ends as it dies (<see cref="ArcBurst"/>).
/// It knows no weapon: its user hands it the area and the hand; how it looks is a <see cref="ChargeLook"/>, the arc spear's or an
/// effect skin's.
/// </summary>
public sealed class BladeCharge
{
    private const double FrameMs = 1000.0 / 60;
    /// <summary>How long a torn bolt's root glows, ms.</summary>
    private const double RootGlowMs = 50;

    /// <summary>How it looks: a charge of another look is made anew.</summary>
    public ChargeLook Look { get; }

    private readonly Random _random = new();
    private readonly List<ArcBurst.Spark> _sparks = [];
    private readonly List<ArcBurst.Torn> _torn = [];
    private readonly List<((double X, double Y) At, double Ms)> _roots = [];
    private EffectArea? _area;
    private ArcBurst? _burst;
    private Func<(double X, double Y), Vector2>? _held;
    private readonly Func<(double X, double Y), (double X, double Y)> _inWorld;

    /// <summary>The charge <paramref name="look"/> says: its colours and numbers.</summary>
    public BladeCharge(ChargeLook look)
    {
        Look = look;
        _inWorld = p => { var w = _held!(p); return (w.X, w.Y); };
    }

    /// <summary>
    /// <paramref name="elapsedMs"/> on: the blade held where <paramref name="held"/> takes its picture into the world, none while
    /// it is not in hand; <paramref name="area"/> where on its picture the charge lives, none for no charge (a new area starts
    /// a new charge); <paramref name="stage"/> the held stage it is drawn on.
    /// </summary>
    public void Step(double elapsedMs, Func<(double X, double Y), Vector2>? held, EffectArea? area, Stage stage)
    {
        if (area != _area)
        {
            _area = area;
            _burst = area != null ? new ArcBurst(area, _random.NextDouble, Look.Numbers) : null;
        }
        _held = held;
        for (int i = _roots.Count - 1; i >= 0; i--)
            if ((_roots[i] = (_roots[i].At, _roots[i].Ms + elapsedMs)).Ms >= RootGlowMs) _roots.RemoveAt(i);
        _sparks.Clear();
        _torn.Clear();
        _burst?.Step(elapsedMs, _held != null ? _inWorld : null, _sparks, _torn);
        if (_burst == null || held == null) return;
        foreach (var s in _sparks)
        {
            var (at, turn, px) = (s.At, s.Turn, s.Px);
            // A spark rides the blade: its root where its pixel is drawn now, its way turned with the picture.
            stage.Bolts.Add(() =>
            {
                var root = held(at);
                var tip = root + (held((at.X + Math.Cos(turn), at.Y + Math.Sin(turn))) - root).Normalized() * (float)px;
                return (root.X, root.Y, tip.X, tip.Y, 0.2);
            }, Frames(s.Ms), s.Alpha, 0.4, tint: Look.Bolts, steady: true);
        }
        foreach (var t in _torn)
        {
            double x0 = t.Root.X, y0 = t.Root.Y, x1 = x0 + t.Way.X * t.Px, y1 = y0 + t.Way.Y * t.Px;
            stage.Bolts.Add(() => (x0, y0, x1, y1, 0.25), Frames(t.Ms), 0.95, 0.7, branchy: t.Forked, tint: Look.Bolts, steady: true);
            _roots.Add((t.Root, 0));
        }
        foreach (var (at, ms) in _roots)
        {
            double fade = 1 - ms / RootGlowMs;
            stage.Flares.Bloom(at.X, at.Y, 2.2, Look.Tint, 0.3 * fade * Fx.Glow);
            stage.Flares.Bloom(at.X, at.Y, 0.6, Look.Core, 0.5 * fade * Fx.Glow);
        }
        var middle = held(_burst.Middle);
        stage.Flares.Bloom(middle.X, middle.Y, 4.5, Look.Tint, (Look.Glow + Look.Charged * _burst.Charged) * Fx.Glow);
    }

    private static int Frames(double ms) => Math.Max(1, (int)Math.Round(ms / FrameMs));
}
