using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;

namespace Stillwake.Game.Render;

/// <summary>A stretch of a cable between two fixed points, in cells: hanging aboard, floating out in space.</summary>
public sealed record CablePiece((double X, double Y) From, (double X, double Y) To, bool Hangs);

/// <summary>
/// A cable as drawn (render/cable.ts): where it meets its nodes, how it hangs, and where it goes through
/// the hull. Only drawn: the sim knows a cable by its two nodes.
/// </summary>
public static class CableShape
{
    /// <summary>How deep a cable hangs inside the ship, as a share of its length.</summary>
    public const double Sag = 0.12;
    /// <summary>Links a drawn cable is made of.</summary>
    public const int Steps = 16;
    /// <summary>How finely the way between a cable's ends is walked to find where it goes through the hull.</summary>
    private const double Probe = 0.05;

    /// <summary>
    /// Where a cable meets node <paramref name="node"/>, in cells: a machine's socket (<c>Plug</c>), or an
    /// anchor's middle. One socket a machine, so every cable to it plugs in at one place.
    /// </summary>
    public static (double X, double Y) EndOf(IGridQueries grid, int node)
    {
        int x = node % grid.Width, y = node / grid.Width;
        var machine = grid.MachineAt(node);
        var plug = machine != null && machine.Cell == node ? Machines.Get(machine.Kind).Plug : null;
        return plug is { } p ? (x + p.X / (double)Camera.Cell, y + p.Y / (double)Camera.Cell) : (x + 0.5, y + 0.5);
    }

    /// <summary>A node's middle, in cells: what the other end of a cable aims at.</summary>
    public static (double X, double Y) MiddleOf(IGridQueries grid, int node)
    {
        var machine = grid.MachineAt(node);
        var spec = machine != null && machine.Cell == node ? Machines.Get(machine.Kind) : null;
        return (node % grid.Width + (spec?.Width ?? 1) / 2.0, node / grid.Width + (spec?.Height ?? 1) / 2.0);
    }

    /// <summary>
    /// The points of a cable from <paramref name="a"/> to <paramref name="b"/>: hanging in a parabola as
    /// deep as <see cref="Sag"/> of its length under gravity, straight with nothing to pull it, out in space.
    /// </summary>
    public static List<(double X, double Y)> CablePoints((double X, double Y) a, (double X, double Y) b, bool hangs)
    {
        double depth = hangs ? Hypot(b.X - a.X, b.Y - a.Y) * Sag : 0;
        var points = new List<(double X, double Y)>(Steps + 1);
        for (int i = 0; i <= Steps; i++)
        {
            double t = (double)i / Steps;
            points.Add((a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t + 4 * depth * t * (1 - t)));
        }
        return points;
    }

    /// <summary>How far <paramref name="p"/> is from the nearest point of a cable drawn through <paramref name="points"/>, in cells.</summary>
    public static double DistanceTo(IReadOnlyList<(double X, double Y)> points, (double X, double Y) p)
    {
        double best = double.PositiveInfinity;
        for (int i = 1; i < points.Count; i++)
        {
            var a = points[i - 1];
            var b = points[i];
            double dx = b.X - a.X, dy = b.Y - a.Y, length = dx * dx + dy * dy;
            double t = length == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / length, 0, 1);
            best = Math.Min(best, Hypot(a.X + dx * t - p.X, a.Y + dy * t - p.Y));
        }
        return best;
    }

    private enum Way { Space, Solid, Aboard }

    /// <summary>
    /// The cable between nodes <paramref name="a"/> and <paramref name="b"/>, each end where it meets its
    /// node, in pieces: where its way passes from space through the hull into the ship it is held, in
    /// the middle of the wall it goes through, so the part aboard hangs and the part outside floats.
    /// <c>Feeds</c> are those points, a fitting drawn on the wall at each.
    /// </summary>
    public static (List<CablePiece> Pieces, List<(double X, double Y)> Feeds) PiecesOf(IGridQueries grid, IRoomsQueries rooms, int a, int b)
    {
        var from = EndOf(grid, a);
        var to = EndOf(grid, b);
        Way KindAt(double t)
        {
            int x = (int)Math.Floor(from.X + (to.X - from.X) * t), y = (int)Math.Floor(from.Y + (to.Y - from.Y) * t);
            if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return Way.Space;
            return rooms.RoomAt(y * grid.Width + x) switch
            {
                RoomsModule.Space => Way.Space,
                null => Way.Solid,
                _ => Way.Aboard,
            };
        }
        int steps = Math.Max(1, (int)Math.Ceiling(Hypot(to.X - from.X, to.Y - from.Y) / Probe));
        var kinds = Enumerable.Range(0, steps + 1).Select(i => KindAt((double)i / steps)).ToArray();
        // Where the way changes between space and the ship: in the middle of the wall between, if any.
        var splits = new List<(double T, bool Feed)>();
        (Way Kind, int At)? last = null;
        for (int i = 0; i <= steps; i++)
        {
            var kind = kinds[i];
            if (kind == Way.Solid) continue;
            if (last is { } was && was.Kind != kind)
            {
                bool wall = i - was.At > 1;
                splits.Add(((wall ? (was.At + 1 + i - 1) / 2.0 : i - 0.5) / steps, wall));
            }
            last = (kind, i);
        }
        (double X, double Y) At(double t) => (from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);
        var ts = new List<double> { 0 };
        ts.AddRange(splits.Select(s => s.T));
        ts.Add(1);
        var pieces = new List<CablePiece>();
        for (int i = 1; i < ts.Count; i++)
        {
            double start = ts[i - 1], end = ts[i];
            int first = (int)Math.Round(start * steps, MidpointRounding.AwayFromZero);
            int stop = (int)Math.Round(end * steps, MidpointRounding.AwayFromZero);
            bool hangs = !kinds.Skip(first).Take(stop + 1 - first).Contains(Way.Space);
            pieces.Add(new CablePiece(At(start), At(end), hangs));
        }
        return (pieces, splits.Where(s => s.Feed).Select(s => At(s.T)).ToList());
    }

    /// <summary>The points of the cable between nodes <paramref name="a"/> and <paramref name="b"/> at rest, piece by piece.</summary>
    public static List<(double X, double Y)> ShapeOf(IGridQueries grid, IRoomsQueries rooms, int a, int b) =>
        PiecesOf(grid, rooms, a, b).Pieces
            .SelectMany((p, i) => CablePoints(p.From, p.To, p.Hangs).Skip(i == 0 ? 0 : 1))
            .ToList();

    private static double Hypot(double x, double y) => Math.Sqrt(x * x + y * y);
}
