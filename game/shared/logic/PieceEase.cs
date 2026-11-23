using System;
using System.Collections.Generic;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Shared;

/// <summary>
/// Pieces as a guest draws them (debris-physics): where the sim has them, carried on between ticks, plus a correction that dies away.
/// When the sim's pose of a piece jumps against where it was drawn the frame before carried on by its motion (the host player's feed
/// set it, its motion changed), the difference goes into the piece's correction, so the screen keeps it where it was and eases it over
/// <see cref="EaseMs"/>; plain motion between ticks is no jump. One past <see cref="SnapAt"/> is no correction (a load, a resync): it
/// snaps. Only the drawing eases: aim, hits and the scan read the sim's pose. Off on the host player's client, which draws its bodies.
/// </summary>
public sealed class PieceEase
{
    /// <summary>How long a correction takes to die away, in ms: nineteen twentieths of it gone by then. A feel number.</summary>
    public const double EaseMs = 120;

    /// <summary>A jump farther than this, in thousandths of a cell, is no correction: three cells.</summary>
    public const double SnapAt = 3 * DebrisModule.Unit;

    private sealed class Entry
    {
        /// <summary>Where the sim had it drawn at its last look, its motion then a tick, and when that was, in ticks.</summary>
        public PiecePose Was;
        public double Vx, Vy, Spin, At;
        /// <summary>The correction drawn over the sim's pose, in thousandths of a cell and 65536ths of a turn.</summary>
        public double X, Y, Angle;
        /// <summary>The frame it was last looked at in, and the ms clock then.</summary>
        public long Frame;
        public double Ms;
    }

    private readonly Dictionary<int, Entry> _entries = [];
    private readonly List<int> _stale = [];
    private bool _on;
    private int _tick;
    private double _drift, _ms;
    private long _frame;

    /// <summary>How many pieces it keeps a correction for: those looked at in the last frame or this.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// A new frame, before anything is drawn: the world at tick <paramref name="tick"/>, pieces drawn <paramref name="drift"/> ticks on,
    /// <paramref name="elapsedMs"/> since the last frame; off (<paramref name="on"/> false) everything is drawn as the sim has it. Once a
    /// tick, the pieces not looked at last frame are forgotten: gone, or out of every view.
    /// </summary>
    public void Frame(bool on, int tick, double drift, double elapsedMs)
    {
        bool ticked = tick != _tick;
        (_on, _tick, _drift) = (on, tick, drift);
        _ms += Math.Max(0, elapsedMs);
        _frame += 1;
        if (!on)
        {
            _entries.Clear();
            return;
        }
        if (!ticked) return;
        _stale.Clear();
        foreach (var (id, entry) in _entries)
            if (entry.Frame < _frame - 1) _stale.Add(id);
        foreach (int id in _stale) _entries.Remove(id);
    }

    /// <summary>Where <paramref name="piece"/>, which the sim has at <paramref name="sim"/> as its reader carries it on, is drawn: there, plus its correction.</summary>
    public PiecePose Drawn(Piece piece, PiecePose sim)
    {
        if (!_on) return sim;
        if (!_entries.TryGetValue(piece.Id, out var entry))
        {
            _entries[piece.Id] = entry = new Entry();
            Look(entry, piece);
            return sim;
        }
        if (entry.Frame != _frame)
        {
            // Where it was drawn last frame, carried on by its motion then, against where the sim has it now: a jump, kept off the screen.
            var now = Carried(piece, _drift);
            double ticks = _tick + _drift - entry.At, decay = Math.Exp(-3 * (_ms - entry.Ms) / EaseMs);
            entry.X = entry.X * decay + entry.Was.X + entry.Vx * ticks - now.X;
            entry.Y = entry.Y * decay + entry.Was.Y + entry.Vy * ticks - now.Y;
            entry.Angle = entry.Angle * decay + Wrapped(entry.Was.Angle + entry.Spin * ticks - now.Angle);
            if (entry.X * entry.X + entry.Y * entry.Y > SnapAt * SnapAt) (entry.X, entry.Y, entry.Angle) = (0, 0, 0);
            // Gone to less than a thousandth of a pixel's worth: none, so a piece at rest is drawn exactly as the sim has it.
            if (Math.Abs(entry.X) < 0.5 && Math.Abs(entry.Y) < 0.5 && Math.Abs(entry.Angle) < 0.5) (entry.X, entry.Y, entry.Angle) = (0, 0, 0);
            Look(entry, piece);
        }
        return new PiecePose(sim.X + entry.X, sim.Y + entry.Y, sim.Angle + entry.Angle);
    }

    private void Look(Entry entry, Piece piece)
    {
        (entry.Was, entry.Vx, entry.Vy, entry.Spin, entry.At) = (Carried(piece, _drift), piece.Vx, piece.Vy, piece.Spin, _tick + _drift);
        (entry.Frame, entry.Ms) = (_frame, _ms);
    }

    private static PiecePose Carried(Piece piece, double drift) =>
        new(piece.X + piece.Vx * drift, piece.Y + piece.Vy * drift, piece.Angle + piece.Spin * drift);

    /// <summary>A turn in 65536ths, the short way round: from half a turn back to half a turn on.</summary>
    private static double Wrapped(double angle) => angle - 65536 * Math.Round(angle / 65536);
}
