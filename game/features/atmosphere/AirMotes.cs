using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>What a mote is: drifting in a room's air, in the lifepod's reach, or on its way from the pod to a suit.</summary>
public enum MoteKind { Room, Pod, Feed }

/// <summary>One mote of air, in pixels of the world and milliseconds.</summary>
public sealed class Mote
{
    public double X, Y, Vx, Vy, AgeMs, LifeMs;
    public MoteKind Kind;
    /// <summary>A bigger, dimmer speck.</summary>
    public bool Big;
    /// <summary>Blown out through an opening: no longer kept to the air, fading.</summary>
    public bool Out;
    /// <summary>Which suit a feeding mote heads for, of the feeds of the frame.</summary>
    public int Target;

    /// <summary>How opaque it is now, of its most: fading in, fading out, twinkling.</summary>
    public double Alpha => Math.Min(1, AgeMs / 400) * Math.Min(1, (LifeMs - AgeMs) / 500) * (0.65 + 0.35 * Math.Sin(AgeMs / 300 + X));
}

/// <summary>The lifepod's air as motes see it: its reach in pixels (left, top, right, bottom), its valve, and each suit it fills now.</summary>
public readonly record struct PodAir((double X0, double Y0, double X1, double Y1) Reach, (double X, double Y) Valve, IReadOnlyList<(double X, double Y)> Feeds);

/// <summary>
/// The air made visible without the overlay: sparse motes drifting in every cell holding air, blown out
/// of a filling room's vents to the edge of its air, swept to the opening of a venting room and out;
/// a few in the lifepod's reach, and a stream from its valve to each suit it fills. Purely drawn, each
/// peer its own. <paramref name="cell"/> is a cell's size in pixels; <paramref name="random"/> draws from
/// 0 to 1, tests pass their own.
/// </summary>
public sealed class AirMotes(int cell, Func<double>? random = null)
{
    /// <summary>Motes a cell of air, and the most in rooms at once: a big ship thins out rather than costing frames.</summary>
    public const double PerCell = 0.4;
    public const int Most = 1500;
    /// <summary>Motes in the pod's reach, and how many a second stream to each suit it fills.</summary>
    public const int InReach = 10;
    public const double FeedPerSecond = 22;

    private readonly Func<double> _random = random ?? Random.Shared.NextDouble;
    private readonly List<Mote> _live = [];
    private double _owed, _fed;

    public IReadOnlyList<Mote> Live => _live;

    /// <summary>
    /// Moves every mote on by <paramref name="elapsedMs"/> and makes new ones: in <paramref name="field"/>'s
    /// air, in rooms with <paramref name="airedCells"/>, out of <paramref name="fillingVents"/> (pixels), and
    /// about the <paramref name="pod"/> if there is one.
    /// </summary>
    public void Step(double elapsedMs, AirField field, IReadOnlyList<int> airedCells, IReadOnlyList<(double X, double Y)> fillingVents, PodAir? pod)
    {
        double s = elapsedMs / 1000;
        Make(s, field, airedCells, fillingVents, pod);
        for (int i = _live.Count - 1; i >= 0; i--)
        {
            var m = _live[i];
            m.AgeMs += elapsedMs;
            if (m.AgeMs >= m.LifeMs)
            {
                _live[i] = _live[^1];
                _live.RemoveAt(_live.Count - 1);
                continue;
            }
            switch (m.Kind)
            {
                case MoteKind.Room: Drift(m, s, field); break;
                case MoteKind.Pod: Hover(m, s, pod); break;
                case MoteKind.Feed: Feed(m, s, pod); break;
            }
        }
    }

    private void Make(double s, AirField field, IReadOnlyList<int> airedCells, IReadOnlyList<(double X, double Y)> fillingVents, PodAir? pod)
    {
        int rooms = 0, near = 0;
        foreach (var m in _live)
            if (m.Kind == MoteKind.Room && !m.Out) rooms++;
            else if (m.Kind == MoteKind.Pod) near++;
        double want = Math.Min(Most, airedCells.Count * PerCell);
        // Owed in proportion to what is missing, so a room fills with motes in about half a second.
        _owed = rooms < want ? _owed + (want - rooms) * Math.Min(1, s * 2) : 0;
        for (; _owed >= 1; _owed--)
        {
            if (fillingVents.Count > 0 && _random() < 0.5)
            {
                var (vx, vy) = fillingVents[(int)(_random() * fillingVents.Count)];
                double a = -Math.PI / 2 + (_random() - 0.5) * 2.2, speed = 25 + _random() * 30;
                Add(MoteKind.Room, vx + (_random() - 0.5) * cell / 2, vy, Math.Cos(a) * speed, Math.Sin(a) * speed);
            }
            else if (airedCells.Count > 0)
            {
                int c = airedCells[(int)(_random() * airedCells.Count)];
                Add(MoteKind.Room, (c % field.Width + _random()) * cell, (c / field.Width + _random()) * cell, 0, 0);
            }
        }
        if (pod is not { } p) return;
        if (near < InReach && _random() < s * 6)
            Add(MoteKind.Pod, p.Reach.X0 + 3 + _random() * (p.Reach.X1 - p.Reach.X0 - 6), p.Reach.Y0 + 3 + _random() * (p.Reach.Y1 - p.Reach.Y0 - 6), 0, 0);
        _fed = p.Feeds.Count > 0 ? _fed + FeedPerSecond * p.Feeds.Count * s : 0;
        for (; _fed >= 1; _fed--)
        {
            var m = Add(MoteKind.Feed, p.Valve.X, p.Valve.Y + (_random() - 0.5) * 2, (_random() - 0.5) * 20, -12 - _random() * 10);
            m.LifeMs = 1600;
            m.Target = (int)(_random() * p.Feeds.Count);
        }
    }

    private Mote Add(MoteKind kind, double x, double y, double vx, double vy)
    {
        var m = new Mote { Kind = kind, X = x, Y = y, Vx = vx, Vy = vy, LifeMs = 5000 + _random() * 5000, Big = _random() < 0.2 };
        _live.Add(m);
        return m;
    }

    private int CellAt(AirField field, double x, double y)
    {
        int cx = (int)Math.Floor(x / cell), cy = (int)Math.Floor(y / cell);
        return cx < 0 || cy < 0 || cx >= field.Width || cy >= field.Height ? -1 : cy * field.Width + cx;
    }

    /// <summary>A room's mote: kept to the air, bouncing off its edge; swept to the opening while the room vents, and out.</summary>
    private void Drift(Mote m, double s, AirField field)
    {
        int here = CellAt(field, m.X, m.Y);
        if (!m.Out && field.FlowAt(here) is double heading)
        {
            m.Vx += Math.Cos(heading) * 260 * s;
            m.Vy += Math.Sin(heading) * 260 * s;
        }
        else if (!m.Out)
        {
            m.Vx = (m.Vx + (_random() - 0.5) * 60 * s) * (1 - 1.2 * s);
            m.Vy = (m.Vy + (_random() - 0.5) * 60 * s - 1.5 * s) * (1 - 1.2 * s);
        }
        double nx = m.X + m.Vx * s, ny = m.Y + m.Vy * s;
        int next = CellAt(field, nx, ny);
        if (m.Out || next == here || (field.FlowAt(here) != null ? field.InRoom(next) || Leave(m) : field.Aired(next)))
        {
            (m.X, m.Y) = (nx, ny);
            return;
        }
        // Off the edge of the air: back the way it came, and gone soon if the air left it behind.
        if (CellAt(field, nx, m.Y) != here) m.Vx *= -0.6;
        else m.Vy *= -0.6;
        if (!field.Aired(here)) m.LifeMs = Math.Min(m.LifeMs, m.AgeMs + 400);
    }

    /// <summary>Out through the opening: free of the room, fading.</summary>
    private static bool Leave(Mote m)
    {
        m.Out = true;
        m.LifeMs = Math.Min(m.LifeMs, m.AgeMs + 700);
        return true;
    }

    /// <summary>A mote in the pod's reach, drifting and kept to it.</summary>
    private void Hover(Mote m, double s, PodAir? pod)
    {
        if (pod is not { } p)
        {
            m.LifeMs = Math.Min(m.LifeMs, m.AgeMs + 300);
            return;
        }
        m.Vx = (m.Vx + (_random() - 0.5) * 40 * s) * (1 - s);
        m.Vy = (m.Vy + (_random() - 0.5) * 40 * s - 2 * s) * (1 - s);
        if ((m.X < p.Reach.X0 + 2 && m.Vx < 0) || (m.X > p.Reach.X1 - 3 && m.Vx > 0)) m.Vx = -m.Vx;
        if ((m.Y < p.Reach.Y0 + 2 && m.Vy < 0) || (m.Y > p.Reach.Y1 - 3 && m.Vy > 0)) m.Vy = -m.Vy;
        m.X += m.Vx * s;
        m.Y += m.Vy * s;
    }

    /// <summary>A mote on its way to a suit: steered to it, gone on arrival or once the suit is no longer fed.</summary>
    private static void Feed(Mote m, double s, PodAir? pod)
    {
        if (pod is not { } p || m.Target >= p.Feeds.Count)
        {
            m.LifeMs = Math.Min(m.LifeMs, m.AgeMs + 120);
            return;
        }
        var (tx, ty) = p.Feeds[m.Target];
        double dx = tx - m.X, dy = ty - m.Y, d = Math.Sqrt(dx * dx + dy * dy);
        if (d < 3)
        {
            m.LifeMs = Math.Min(m.LifeMs, m.AgeMs + 120);
            return;
        }
        m.Vx = (m.Vx + dx / d * 220 * s) * (1 - 2.2 * s);
        m.Vy = (m.Vy + dy / d * 220 * s) * (1 - 2.2 * s);
        m.X += m.Vx * s;
        m.Y += m.Vy * s;
    }
}
