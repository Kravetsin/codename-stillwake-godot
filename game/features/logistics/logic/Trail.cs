using System;

namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// Where a flying drone is drawn between ticks, in thousandths of a cell: its path through the sim's places at the last
/// ticks, straight from one to the next, averaged over the last <see cref="DroneLook.TrailTicks"/> ticks of it up to a
/// tick behind the sim. It flies at an even speed through each cell's centre and turns a corner along a curve, its speed
/// swinging round smoothly, where the sim turns it at once. It runs that many ticks behind the sim, and a half more.
/// </summary>
public sealed class Trail
{
    // The sim's places at the last ticks, the newest last: one a tick of path, the window's ticks and the tick behind.
    private readonly (int X, int Y)[] _at = new (int, int)[DroneLook.TrailTicks + 2];
    private double _progress;
    private bool _seen;

    /// <summary>The sim's place now, <paramref name="progress"/> of the way to the next tick: a new tick when it moved or progress went back.</summary>
    public void See(int x, int y, double progress)
    {
        if (!_seen) Array.Fill(_at, (x, y));
        else if ((x, y) != _at[^1] || progress < _progress)
        {
            Array.Copy(_at, 1, _at, 0, _at.Length - 1);
            _at[^1] = (x, y);
        }
        (_progress, _seen) = (Math.Clamp(progress, 0, 1), true);
    }

    /// <summary>Where it is drawn, thousandths of a cell: the path's average over the window ending a tick behind.</summary>
    public (double X, double Y) At
    {
        get
        {
            double from = _progress, to = from + DroneLook.TrailTicks;
            return (Mean(from, to, p => p.X), Mean(from, to, p => p.Y));
        }
    }

    /// <summary>How far it goes in a tick as drawn, thousandths of a cell: the window's head less its tail, over its length.</summary>
    public (double X, double Y) PerTick
    {
        get
        {
            double from = _progress, to = from + DroneLook.TrailTicks;
            return ((On(to, p => p.X) - On(from, p => p.X)) / DroneLook.TrailTicks, (On(to, p => p.Y) - On(from, p => p.Y)) / DroneLook.TrailTicks);
        }
    }

    /// <summary>The path at <paramref name="s"/> ticks from the oldest place, on the line between two.</summary>
    private double On(double s, Func<(int X, int Y), int> axis)
    {
        int i = Math.Min((int)s, _at.Length - 2);
        return axis(_at[i]) + (axis(_at[i + 1]) - axis(_at[i])) * (s - i);
    }

    /// <summary>The path's mean between <paramref name="from"/> and <paramref name="to"/> ticks, a trapezoid a leg.</summary>
    private double Mean(double from, double to, Func<(int X, int Y), int> axis)
    {
        double sum = 0;
        for (double a = from; a < to;)
        {
            double b = Math.Min(to, Math.Floor(a) + 1);
            sum += (On(a, axis) + On(b, axis)) / 2 * (b - a);
            a = b;
        }
        return sum / (to - from);
    }
}
