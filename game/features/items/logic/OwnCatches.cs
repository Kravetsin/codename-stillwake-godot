using System;
using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// What this peer's own catches just put into its inventory, for the pick-up popups over its hero and the icons flying home:
/// a <see cref="Grab"/> of a drifting piece, or a <see cref="Collect"/> of every piece a net held (2026-11-03-debris-net), one path
/// for both. A command seen in flight at one read and gone from it at the next has run, and the sim says what each of its pieces
/// gave (<c>DebrisModule.CaughtFrom</c>, a new list a catch): shown when that answer is not the one seen when it set off, or
/// already shown, so a refused grab or collect, or a second E on a net the first emptied, shows nothing. Where each piece was
/// when its command was first seen in flight is kept, for the loot to fly from: a grabbed piece is hidden from then on
/// (<c>Predicted.Grabbing</c>). Read every frame after the hero's commands are sent, so each is seen in flight before its tick can
/// run. A wreck broken by a hit floats its loot, shown as any floor stack once picked up.
/// </summary>
public sealed class OwnCatches
{
    // By reference, as OwnHits: commands of equal payloads are equal records.
    private Dictionary<Command, (int Piece, (double X, double Y)? From)[]> _flying = new(ReferenceEqualityComparer.Instance), _still = new(ReferenceEqualityComparer.Instance);
    /// <summary>For each piece a command in flight is for, the sim's answer last seen or shown: a new answer is a catch run.</summary>
    private readonly Dictionary<int, IReadOnlyList<(string Item, int Count)>?> _known = [];
    private readonly HashSet<int> _wanted = [];
    private readonly List<int> _forget = [];
    private readonly List<(string Item, int Count, (double X, double Y)? From)> _gained = [];

    /// <summary>
    /// Takes this peer's commands in flight now (<c>Client.InFlight</c>) and returns what its grabs and collects that ran since
    /// the last read put into its inventory, piece by piece, each by item, with where its piece was when its command set off.
    /// <paramref name="caughtFrom"/> is the sim's answer for a piece, this peer's catches only; <paramref name="whereIs"/> where a
    /// drifting piece is drawn now, none if it is gone; <paramref name="netHolds"/> the pieces a net holds, in catch order, each
    /// where it is drawn. Live until the next read.
    /// </summary>
    public IReadOnlyList<(string Item, int Count, (double X, double Y)? From)> Read(
        IReadOnlyList<Command> inFlight, Func<int, IReadOnlyList<(string Item, int Count)>?> caughtFrom, Func<int, (double X, double Y)?>? whereIs = null,
        Func<int, IReadOnlyList<(int Piece, (double X, double Y) At)>>? netHolds = null)
    {
        _gained.Clear();
        _still.Clear();
        for (int i = 0; i < inFlight.Count; i++)
        {
            var command = inFlight[i];
            if (!_flying.TryGetValue(command, out var pieces)) pieces = PiecesOf(command, whereIs, netHolds);
            if (pieces == null) continue;
            foreach (var (piece, _) in pieces) _known.TryAdd(piece, caughtFrom(piece));
            _still[command] = pieces;
        }
        foreach (var (command, pieces) in _flying)
        {
            if (_still.ContainsKey(command)) continue;
            foreach (var (piece, from) in pieces)
            {
                if (caughtFrom(piece) is not { } gave || ReferenceEquals(gave, _known.GetValueOrDefault(piece))) continue;
                _known[piece] = gave;
                foreach (var (item, count) in gave) _gained.Add((item, count, from));
            }
        }
        (_flying, _still) = (_still, _flying);
        Forget();
        return _gained;
    }

    /// <summary>The pieces <paramref name="command"/> catches, each where it is drawn as it sets off; null for any other command.</summary>
    private static (int Piece, (double X, double Y)? From)[]? PiecesOf(
        Command command, Func<int, (double X, double Y)?>? whereIs, Func<int, IReadOnlyList<(int Piece, (double X, double Y) At)>>? netHolds)
    {
        switch (command)
        {
            case Grab grab:
                return [(grab.Piece, whereIs?.Invoke(grab.Piece))];
            case Collect collect when netHolds != null:
                var held = netHolds(collect.Machine);
                var pieces = new (int Piece, (double X, double Y)? From)[held.Count];
                for (int i = 0; i < held.Count; i++) pieces[i] = (held[i].Piece, held[i].At);
                return pieces;
            default:
                return null;
        }
    }

    /// <summary>Drops what is known of pieces no command in flight is for.</summary>
    private void Forget()
    {
        if (_known.Count == 0) return;
        _wanted.Clear();
        foreach (var pieces in _flying.Values)
            foreach (var (piece, _) in pieces) _wanted.Add(piece);
        _forget.Clear();
        foreach (int piece in _known.Keys)
            if (!_wanted.Contains(piece)) _forget.Add(piece);
        foreach (int piece in _forget) _known.Remove(piece);
    }
}
