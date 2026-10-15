using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>What a mote is: drifting in a room's air, or in the lifepod's reach.</summary>
public enum MoteKind { Room, Pod }

/// <summary>One mote of air, in pixels of the world and milliseconds.</summary>
public sealed class Mote
{
    public double X, Y, Vx, Vy, AgeMs, LifeMs;
    /// <summary>The way it wanders, in radians, turning a little at a time.</summary>
    public double Heading;
    public MoteKind Kind;
    /// <summary>A bigger, dimmer speck.</summary>
    public bool Big;
    /// <summary>Blown out through an opening: no longer kept to the air, fading.</summary>
    public bool Out;

    /// <summary>How opaque it is now, of its most: fading in, fading out, twinkling.</summary>
    public double Alpha => Math.Min(1, AgeMs / 400) * Math.Min(1, (LifeMs - AgeMs) / 500) * (0.65 + 0.35 * Math.Sin(AgeMs / 300 + LifeMs));
}

/// <summary>
/// The air made visible without the overlay: sparse motes wandering in every cell holding air, blown out
/// of a filling room's vents to the edge of its air, swept to the opening of a venting room and out; and
/// a few in the lifepod's <c>reach</c> (pixels: left, top, right, bottom). Each glides, its heading turning
/// a little at a time, so none jitters. Purely drawn, each peer its own. <paramref name="cell"/> is a
/// cell's size in pixels; <paramref name="random"/> draws from 0 to 1, tests pass their own.
/// </summary>
public sealed class AirMotes(int cell, Func<double>? random = null)
{
    /// <summary>Motes a cell of air, and the most in rooms at once: a big ship thins out rather than costing frames.</summary>
    public const double PerCell = 0.4;
    public const int Most = 1500;
    /// <summary>Motes in the pod's reach.</summary>
    public const int InReach = 10;
    /// <summary>How fast a mote wanders, in pixels a second, and how much its heading may turn in one.</summary>
    private const double Wander = 6, Turn = 2.5;

    private readonly Func<double> _random = random ?? Random.Shared.NextDouble;
    private readonly List<Mote> _live = [];
    private double _owed;

    public IReadOnlyList<Mote> Live => _live;

    /// <summary>
    /// Moves every mote on by <paramref name="elapsedMs"/> and makes new ones: in <paramref name="field"/>'s
    /// air, in rooms with <paramref name="airedCells"/>, out of <paramref name="fillingVents"/> (pixels), and
    /// in the pod's <paramref name="reach"/> if there is a pod.
    /// </summary>
    public void Step(double elapsedMs, AirField field, IReadOnlyList<int> airedCells, IReadOnlyList<(double X, double Y)> fillingVents,
        (double X0, double Y0, double X1, double Y1)? reach)
    {
        double s = elapsedMs / 1000;
        Make(s, field, airedCells, fillingVents, reach);
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
            if (m.Kind == MoteKind.Room) Drift(m, s, field);
            else Hover(m, s, reach);
        }
    }

    private void Make(double s, AirField field, IReadOnlyList<int> airedCells, IReadOnlyList<(double X, double Y)> fillingVents,
        (double X0, double Y0, double X1, double Y1)? reach)
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
        if (reach is { } r && near < InReach && _random() < s * 6)
            Add(MoteKind.Pod, r.X0 + 3 + _random() * (r.X1 - r.X0 - 6), r.Y0 + 3 + _random() * (r.Y1 - r.Y0 - 6), 0, 0);
    }

    private void Add(MoteKind kind, double x, double y, double vx, double vy) => _live.Add(new Mote
    {
        Kind = kind,
        X = x,
        Y = y,
        Vx = vx,
        Vy = vy,
        Heading = _random() * 2 * Math.PI,
        LifeMs = 5000 + _random() * 5000,
        Big = _random() < 0.2,
    });

    private int CellAt(AirField field, double x, double y)
    {
        int cx = (int)Math.Floor(x / cell), cy = (int)Math.Floor(y / cell);
        return cx < 0 || cy < 0 || cx >= field.Width || cy >= field.Height ? -1 : cy * field.Width + cx;
    }

    /// <summary>Its velocity eased toward its wandering heading, which turns a little at a time.</summary>
    private void Glide(Mote m, double s)
    {
        m.Heading += (_random() - 0.5) * 2 * Turn * s;
        double ease = Math.Min(1, 1.5 * s);
        m.Vx += (Math.Cos(m.Heading) * Wander - m.Vx) * ease;
        m.Vy += (Math.Sin(m.Heading) * Wander - 1.5 - m.Vy) * ease;
    }

    /// <summary>A room's mote: kept to the air, turned back at its edge; swept to the opening while the room vents, and out.</summary>
    private void Drift(Mote m, double s, AirField field)
    {
        int here = CellAt(field, m.X, m.Y);
        if (!m.Out && field.FlowAt(here) is double heading)
        {
            m.Vx += Math.Cos(heading) * 260 * s;
            m.Vy += Math.Sin(heading) * 260 * s;
        }
        else if (!m.Out) Glide(m, s);
        double nx = m.X + m.Vx * s, ny = m.Y + m.Vy * s;
        int next = CellAt(field, nx, ny);
        if (m.Out || next == here || (field.FlowAt(here) != null ? field.InRoom(next) || Leave(m) : field.Aired(next)))
        {
            (m.X, m.Y) = (nx, ny);
            return;
        }
        // Off the edge of the air: turned back the way it came, and gone soon if the air left it behind.
        if (CellAt(field, nx, m.Y) != here) m.Vx = -m.Vx;
        else m.Vy = -m.Vy;
        m.Heading = Math.Atan2(m.Vy, m.Vx);
        if (!field.Aired(here)) m.LifeMs = Math.Min(m.LifeMs, m.AgeMs + 400);
    }

    /// <summary>Out through the opening: free of the room, fading.</summary>
    private static bool Leave(Mote m)
    {
        m.Out = true;
        m.LifeMs = Math.Min(m.LifeMs, m.AgeMs + 700);
        return true;
    }

    /// <summary>A mote in the pod's reach, gliding and kept to it.</summary>
    private void Hover(Mote m, double s, (double X0, double Y0, double X1, double Y1)? reach)
    {
        if (reach is not { } r)
        {
            m.LifeMs = Math.Min(m.LifeMs, m.AgeMs + 300);
            return;
        }
        Glide(m, s);
        if ((m.X < r.X0 + 2 && m.Vx < 0) || (m.X > r.X1 - 3 && m.Vx > 0)) (m.Vx, m.Heading) = (-m.Vx, Math.PI - m.Heading);
        if ((m.Y < r.Y0 + 2 && m.Vy < 0) || (m.Y > r.Y1 - 3 && m.Vy > 0)) (m.Vy, m.Heading) = (-m.Vy, -m.Heading);
        m.X += m.Vx * s;
        m.Y += m.Vy * s;
    }
}
