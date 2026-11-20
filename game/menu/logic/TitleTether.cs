using System;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Menu;

/// <summary>
/// The title hero's tether (<see cref="TitleScene"/>): a chain of points stepped by Verlet, its near end on the hero's belt, its
/// far end pinned off the frame's foot, a little slack; a weak pull down and to the right, a sway travelling along it, damped.
/// Settled for <see cref="Settle"/> seconds before the title opens, so it never shows straight.
/// </summary>
public sealed class TitleTether
{
    /// <summary>How many points it is, and where its far end is pinned, below the frame.</summary>
    public const int Count = 36;

    public static readonly Vec Anchor = new(1100, 960);

    /// <summary>Its length over the straight line it starts on.</summary>
    public const double Slack = 1.025;

    /// <summary>The pull on each point, in pixels a second squared; the sway's strength, period in seconds and lag per point.</summary>
    public const double PullX = 3, PullY = 4, Sway = 4, SwaySeconds = 9.5, SwayLag = 0.22;

    /// <summary>How much of its speed a point keeps each step; the step, in seconds; how long it settles before the title.</summary>
    public const double Damp = 0.996, Step = 1.0 / 120, Settle = 20;

    /// <summary>The constraint passes each step; the bend's rest over two links and how hard it pushes a fold open.</summary>
    private const int Passes = 24;
    private const double Bend = 1.94, BendStiffness = 0.25, Tau = 2 * Math.PI;

    private readonly double[] _x = new double[Count], _y = new double[Count], _px = new double[Count], _py = new double[Count];
    private readonly Vec[] _chain = new Vec[Count], _once = new Vec[2 * Count];

    /// <summary>One link's rest length, in pixels.</summary>
    public double Link { get; }

    /// <summary>How far into the title it has been stepped, in seconds.</summary>
    public double Seconds { get; private set; } = -Settle;

    public TitleTether()
    {
        var a = Attach(-Settle);
        Link = Math.Sqrt((Anchor.X - a.X) * (Anchor.X - a.X) + (Anchor.Y - a.Y) * (Anchor.Y - a.Y)) * Slack / (Count - 1);
        for (int i = 0; i < Count; i++)
        {
            double u = i / (Count - 1.0);
            _x[i] = _px[i] = a.X + (Anchor.X - a.X) * u;
            _y[i] = _py[i] = a.Y + (Anchor.Y - a.Y) * u;
        }
        RunTo(0);
    }

    /// <summary>Point <paramref name="i"/>, from the belt (0) to the pin, in the scene's pixels.</summary>
    public Vec this[int i] => new(_x[i], _y[i]);

    /// <summary>Steps it on to <paramref name="seconds"/> into the title, in fixed steps.</summary>
    public void RunTo(double seconds)
    {
        while (Seconds + Step <= seconds + 1e-9) StepAt(Seconds += Step);
    }

    /// <summary>Where the tether is clipped to the hero <paramref name="seconds"/> into the title.</summary>
    public static Vec Attach(double seconds) => TitleScene.ToScene(TitleScene.Motion(seconds), TitleHero.Attach);

    /// <summary>
    /// The chain smoothed twice by Chaikin's corners, its ends kept, into <paramref name="into"/>: <see cref="Smoothed"/> points.
    /// </summary>
    public void Smooth(Vec[] into)
    {
        for (int i = 0; i < Count; i++) _chain[i] = this[i];
        Corners(_chain, _once);
        Corners(_once, into);
    }

    /// <summary>How many points <see cref="Smooth"/> gives.</summary>
    public const int Smoothed = 2 + 2 * (2 * Count - 1);

    private static void Corners(ReadOnlySpan<Vec> from, Span<Vec> into)
    {
        int n = from.Length;
        into[0] = from[0];
        for (int i = 0; i < n - 1; i++)
        {
            Vec p = from[i], q = from[i + 1];
            into[1 + 2 * i] = new Vec(0.75 * p.X + 0.25 * q.X, 0.75 * p.Y + 0.25 * q.Y);
            into[2 + 2 * i] = new Vec(0.25 * p.X + 0.75 * q.X, 0.25 * p.Y + 0.75 * q.Y);
        }
        into[2 * n - 1] = from[n - 1];
    }

    private void StepAt(double seconds)
    {
        var a = Attach(seconds);
        double dx = Anchor.X - a.X, dy = Anchor.Y - a.Y, dl = Math.Sqrt(dx * dx + dy * dy);
        if (dl == 0) dl = 1;
        double perpX = -dy / dl, perpY = dx / dl;
        for (int i = 1; i < Count - 1; i++)
        {
            double s = Sway * Math.Sin(Tau * seconds / SwaySeconds - i * SwayLag);
            double fx = PullX + perpX * s, fy = PullY + perpY * s;
            double vx = (_x[i] - _px[i]) * Damp, vy = (_y[i] - _py[i]) * Damp;
            (_px[i], _py[i]) = (_x[i], _y[i]);
            _x[i] += vx + fx * Step * Step;
            _y[i] += vy + fy * Step * Step;
        }
        Pin(a);
        for (int pass = 0; pass < Passes; pass++)
        {
            for (int i = 0; i < Count - 1; i++) Hold(i, i + 1, Link, 1);
            for (int i = 0; i < Count - 2; i++) Hold(i, i + 2, Link * Bend, BendStiffness);
            Pin(a);
        }
    }

    private void Pin(Vec a)
    {
        (_x[0], _y[0], _px[0], _py[0]) = (a.X, a.Y, a.X, a.Y);
        (_x[Count - 1], _y[Count - 1]) = (Anchor.X, Anchor.Y);
    }

    /// <summary>Pulls points <paramref name="i"/> and <paramref name="j"/> to <paramref name="rest"/> apart; a weak one only pushes a fold open.</summary>
    private void Hold(int i, int j, double rest, double stiffness)
    {
        double dx = _x[j] - _x[i], dy = _y[j] - _y[i], d = Math.Sqrt(dx * dx + dy * dy);
        if (d == 0) d = 1e-6;
        if (stiffness < 1 && d > rest) return;
        int wa = i == 0 || i == Count - 1 ? 0 : 1, wb = j == 0 || j == Count - 1 ? 0 : 1;
        if (wa + wb == 0) return;
        double m = (d - rest) / d * stiffness / (wa + wb);
        _x[i] += dx * m * wa;
        _y[i] += dy * m * wa;
        _x[j] -= dx * m * wb;
        _y[j] -= dy * m * wb;
    }
}
