using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;
using Stillwake.Sim.Rooms;
using SimCable = Stillwake.Sim.Power.Cable;

namespace Stillwake.Game.Features.Power;

/// <summary>
/// A cable's end carried by hand: the end at <c>Node</c> of every cable there, or of <c>Cable</c> alone,
/// held at <c>At</c> instead of its node.
/// </summary>
public sealed record Held(int Node, Ends? Cable, (double X, double Y) At);

/// <summary>A cable just laid, from node <c>A</c> at <c>From</c> to node <c>B</c> at <c>To</c>, and the rope it was laid as.</summary>
public sealed record Laid(int A, int B, (double X, double Y) From, (double X, double Y) To, Rope Rope);

/// <summary>A cable as drawn this frame: its nodes, its points end to end, and where it goes through the hull.</summary>
public sealed record CableLine(int A, int B, IReadOnlyList<(double X, double Y)> Points, IReadOnlyList<(double X, double Y)> Feeds);

/// <summary>
/// Every cable as ropes (features/power/ropes.ts), a rope for each of its pieces
/// (<see cref="CableShape.PiecesOf"/>), kept between frames by its two nodes: the part aboard hangs and
/// the part outside floats, held where it goes through the hull. The cables found at the start are
/// already at rest; one laid later starts straight and falls into its hang, swinging. A cable's end
/// carried by hand (<see cref="Hold"/>) follows the hand, the rope swinging after it, and a cable
/// carried to a node (<see cref="Land"/>) or laid (<see cref="Adopt"/>) goes on from where it was drawn,
/// moving as it was, however its pieces change on the way: through the hull no longer, or newly.
/// </summary>
public sealed class CableRopes
{
    /// <summary>Aboard: gravity pulls a cable into its hang, and air slows its swing. Cells, steps of 1/60 s.</summary>
    private static readonly RopeStyle Hanging = new(CableShape.Steps, Keep: 0.97, Gravity: 0.004, Drift: 0, DriftSpeed: 0, Wave: 0);
    /// <summary>Out in space: nothing pulls it, and it drifts slowly as the tether does.</summary>
    private static readonly RopeStyle Floating = new(CableShape.Steps, Keep: 0.95, Gravity: 0, Drift: 0.0005, DriftSpeed: 0.01, Wave: 0.35);
    /// <summary>
    /// How much longer than its span a hanging cable is: a parabola <see cref="CableShape.Sag"/> of its
    /// span deep is that much longer, so at rest it hangs where it is picked by. Out in space, a little slack.
    /// </summary>
    private const double Hang = 1 + 8 * CableShape.Sag * CableShape.Sag / 3, Loose = 1.02;
    /// <summary>
    /// How long an end let go on a node stays where it was let go, waiting for the world to move the
    /// cable there: past this, the move was refused, and the end goes back to its node.
    /// </summary>
    private const double LandingMs = 2000;

    /// <summary>How long a hanging cable is for its span, in cells.</summary>
    public static double HangFor(double span) => span * Hang;

    /// <summary>A rope for a cable being laid from <paramref name="from"/>: it hangs as a cable aboard does.</summary>
    public static Rope LayingRope((double X, double Y) from) => new(Hanging, from, from);

    private sealed record Stretch(CablePiece Piece, Rope Rope);

    private sealed record Roped(List<Stretch> Stretches, List<(double X, double Y)> Feeds, List<CablePiece> Shape);

    private sealed class Pending(Laid laid)
    {
        public Laid Laid { get; } = laid;
        public double Ms;
    }

    private sealed class Landing(int node, Held end)
    {
        public int Node { get; } = node;
        public Held End { get; } = end;
        public double Ms;
    }

    private readonly Dictionary<(int A, int B), Roped> _cables = new();
    private readonly Dictionary<(int A, int B), Pending> _pending = new();
    private bool _started;
    private Held? _held;
    /// <summary>An end let go on a node, kept where it was let go until the world has the cable there.</summary>
    private Landing? _landing;

    /// <summary>The points of <paramref name="ropes"/> end to end as drawn, moving as they are, the other way round when <paramref name="turned"/>.</summary>
    private static List<Rope.Knot> Handed(IEnumerable<Rope> ropes, bool turned)
    {
        var points = new List<Rope.Knot>();
        // Each rope after the first begins where the last ended, at a fitting in the hull.
        foreach (var rope in ropes) points.AddRange(rope.Drawn().Skip(points.Count == 0 ? 0 : 1));
        if (turned) points.Reverse();
        return points;
    }

    /// <summary>
    /// The points of the cable <paramref name="c"/> was carried from: the cable between its other end and
    /// the node the hand took it from, as drawn and moving as it was, turned to run as <paramref name="c"/> does.
    /// </summary>
    private List<Rope.Knot>? CarriedPoints(SimCable c, Landing to)
    {
        int other = c.A == to.Node ? c.B : c.A;
        int was = to.End.Node;
        bool fromOther = _cables.TryGetValue((other, was), out var old);
        if (!fromOther && !_cables.TryGetValue((was, other), out old)) return null;
        // The old rope ran from its own A; the new one runs from c.A.
        return Handed(old!.Stretches.Select(s => s.Rope), fromOther != (c.A == other));
    }

    /// <summary>
    /// Where each of <paramref name="pieces"/> starts, taken from <paramref name="line"/>, a cable as it was
    /// drawn: the line itself for one piece of as many points, else each piece's stretch of it between the
    /// points nearest its ends, its links evenly along it, moving as the line did there.
    /// </summary>
    private static List<List<Rope.Knot>> StartsOf(List<Rope.Knot> line, List<CablePiece> pieces)
    {
        if (pieces.Count == 1 && line.Count == CableShape.Steps + 1) return [line];
        var at = new double[line.Count];
        for (int i = 1; i < line.Count; i++) at[i] = at[i - 1] + Math.Sqrt(Squared(line[i - 1], (line[i].X, line[i].Y)));
        var starts = new List<List<Rope.Knot>>(pieces.Count);
        double s = 0;
        foreach (var piece in pieces)
        {
            double from = Nearest(line, at, piece.From, s), to = Nearest(line, at, piece.To, from);
            starts.Add(Enumerable.Range(0, CableShape.Steps + 1).Select(j => Sample(line, at, from + (to - from) * j / CableShape.Steps)).ToList());
            s = to;
        }
        return starts;
    }

    private static double Squared(Rope.Knot p, (double X, double Y) q) => (q.X - p.X) * (q.X - p.X) + (q.Y - p.Y) * (q.Y - p.Y);

    /// <summary>How far along <paramref name="line"/> its point nearest <paramref name="p"/> is, no nearer its start than <paramref name="from"/>.</summary>
    private static double Nearest(List<Rope.Knot> line, double[] at, (double X, double Y) p, double from)
    {
        double best = from, bestD = double.MaxValue;
        for (int i = 0; i + 1 < line.Count; i++)
        {
            if (at[i + 1] < from) continue;
            double dx = line[i + 1].X - line[i].X, dy = line[i + 1].Y - line[i].Y, length = dx * dx + dy * dy;
            double t = length == 0 ? 0 : Math.Clamp(((p.X - line[i].X) * dx + (p.Y - line[i].Y) * dy) / length, 0, 1);
            double along = Math.Max(from, at[i] + t * (at[i + 1] - at[i]));
            double d = Squared(Sample(line, at, along), p);
            if (d < bestD) (best, bestD) = (along, d);
        }
        return best;
    }

    /// <summary>The point <paramref name="along"/> <paramref name="line"/>, moving as the points either side of it.</summary>
    private static Rope.Knot Sample(List<Rope.Knot> line, double[] at, double along)
    {
        int i = 0;
        while (i + 2 < line.Count && at[i + 1] < along) i++;
        int j = Math.Min(i + 1, line.Count - 1);
        var (p, q) = (line[i], line[j]);
        double u = at[j] == at[i] ? 0 : Math.Clamp((along - at[i]) / (at[j] - at[i]), 0, 1);
        double x = p.X + (q.X - p.X) * u, y = p.Y + (q.Y - p.Y) * u;
        double vx = p.X - p.Px + (q.X - q.Px - (p.X - p.Px)) * u, vy = p.Y - p.Py + (q.Y - q.Py - (p.Y - p.Py)) * u;
        return new Rope.Knot { X = x, Y = y, Px = x - vx, Py = y - vy };
    }

    /// <summary>Takes the cables as the world has them now: new ones in, gone ones out.</summary>
    public void Sync(IGridQueries grid, IRoomsQueries rooms, IReadOnlyList<SimCable> cables)
    {
        var seen = new HashSet<(int, int)>();
        bool landed = false;
        foreach (var c in cables)
        {
            var key = (c.A, c.B);
            seen.Add(key);
            var (pieces, feeds) = CableShape.PiecesOf(grid, rooms, c.A, c.B);
            if (_cables.TryGetValue(key, out var kept) && kept.Shape.SequenceEqual(pieces)) continue;
            bool lands = _landing != null && (c.A == _landing.Node || c.B == _landing.Node);
            landed |= lands;
            // A cable carried or laid here goes on as the very rope it was in hand, as it hangs now.
            if (!_pending.TryGetValue((c.A, c.B), out var laid)) _pending.TryGetValue((c.B, c.A), out laid);
            if (laid != null) _pending.Remove((laid.Laid.A, laid.Laid.B));
            var carried = laid != null
                ? Handed([laid.Laid.Rope], laid.Laid.A != c.A)
                : lands && _landing != null ? CarriedPoints(c, _landing) : null;
            var starts = carried != null ? StartsOf(carried, pieces) : null;
            var stretches = pieces.Select((piece, i) =>
            {
                var style = piece.Hangs ? Hanging : Floating;
                var rest = starts != null
                    ? starts[i]
                    : _started && !lands && laid == null
                        ? null
                        : CableShape.CablePoints(piece.From, piece.To, piece.Hangs)
                            .Select(p => new Rope.Knot { X = p.X, Y = p.Y, Px = p.X, Py = p.Y }).ToList();
                return new Stretch(piece, new Rope(style, piece.From, piece.To, c.A + c.B + i, rest));
            }).ToList();
            _cables[key] = new Roped(stretches, feeds, pieces);
        }
        if (landed) _landing = null;
        foreach (var key in _cables.Keys.Where(k => !seen.Contains(k)).ToList()) _cables.Remove(key);
        _started = true;
    }

    /// <summary>Holds a cable's end by hand until the next <see cref="Hold"/>; null lets every end go.</summary>
    public void Hold(Held? end) => _held = end;

    /// <summary>
    /// <paramref name="end"/> is let go on <paramref name="node"/>: it stays there until the world has the
    /// cable there, which then starts in its hang, as it was carried, rather than going back to where it was first.
    /// </summary>
    public void Land(int node, Held end) => _landing = new Landing(node, end);

    /// <summary>A cable laid by hand: drawn as its rope until the world has it, then roped as that.</summary>
    public void Adopt(Laid laid) => _pending[(laid.A, laid.B)] = new Pending(laid);

    /// <summary>Moves every cable on by <paramref name="elapsedMs"/>, brushed by <paramref name="bodies"/> aboard.</summary>
    public void Update(double elapsedMs, IReadOnlyList<Body> bodies)
    {
        if (_landing != null && (_landing.Ms += elapsedMs) > LandingMs) _landing = null;
        foreach (var (key, laid) in _pending.ToList())
        {
            if ((laid.Ms += elapsedMs) > LandingMs) _pending.Remove(key);
            else laid.Laid.Rope.Update(laid.Laid.From, laid.Laid.To, elapsedMs, HangFor, bodies);
        }
        var hand = _held ?? _landing?.End;
        foreach (var ((a, b), c) in _cables)
        {
            var carried = hand != null && (hand.Cable == null || (hand.Cable.A == a && hand.Cable.B == b)) ? hand : null;
            int last = c.Stretches.Count - 1;
            for (int i = 0; i <= last; i++)
            {
                var (piece, rope) = c.Stretches[i];
                double factor = piece.Hangs ? Hang : Loose;
                rope.Update(
                    i == 0 && carried?.Node == a ? carried.At : piece.From,
                    i == last && carried?.Node == b ? carried.At : piece.To,
                    elapsedMs,
                    span => span * factor,
                    piece.Hangs ? bodies : []);
            }
        }
    }

    /// <summary>Each cable's points, end to end, by its two nodes, and where it goes through the hull; then the cables laid and not yet had.</summary>
    public IEnumerable<CableLine> Lines()
    {
        foreach (var ((a, b), c) in _cables)
        {
            var points = c.Stretches.SelectMany((s, i) => Drawn(s.Rope).Skip(i == 0 ? 0 : 1)).ToList();
            yield return new CableLine(a, b, points, c.Feeds);
        }
        foreach (var p in _pending.Values)
            yield return new CableLine(p.Laid.A, p.Laid.B, Drawn(p.Laid.Rope).ToList(), []);
    }

    /// <summary>The points of <paramref name="rope"/> as drawn this frame.</summary>
    public static IEnumerable<(double X, double Y)> Drawn(Rope rope) => Enumerable.Range(0, rope.Points.Count).Select(rope.DrawnAt);
}
