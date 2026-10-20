using System;
using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// What this peer's own catches just put into its inventory, for the pick-up popups over its hero and the icons flying home:
/// a grab seen in flight at one read and gone from it at the next has run, and the sim says what it gave
/// (<c>DebrisModule.CaughtFrom</c>); a refused one gave nothing. Where the piece was when its grab was first seen in flight is
/// kept, for the loot to fly from: the piece is hidden from then on (<c>Predicted.Grabbing</c>). Read every frame after the
/// hero's grabs are sent, so every own grab is seen in flight before its tick can run. A wreck broken by a hit floats its
/// loot, shown as any floor stack once picked up.
/// </summary>
public sealed class OwnCatches
{
    // By reference, as OwnHits: commands of equal payloads are equal records.
    private Dictionary<Command, (double X, double Y)?> _flying = new(ReferenceEqualityComparer.Instance), _still = new(ReferenceEqualityComparer.Instance);
    private readonly List<(string Item, int Count, (double X, double Y)? From)> _gained = [];

    /// <summary>
    /// Takes this peer's commands in flight now (<c>Client.InFlight</c>) and returns what its grabs that ran since the
    /// last read put into its inventory, catch by catch, each by item, with where its piece was when the grab set off;
    /// <paramref name="caughtFrom"/> is the sim's answer for a piece, <paramref name="whereIs"/> where a piece is drawn now,
    /// none if it is gone. Live until the next read.
    /// </summary>
    public IReadOnlyList<(string Item, int Count, (double X, double Y)? From)> Read(
        IReadOnlyList<Command> inFlight, Func<int, IReadOnlyList<(string Item, int Count)>?> caughtFrom, Func<int, (double X, double Y)?>? whereIs = null)
    {
        _gained.Clear();
        _still.Clear();
        for (int i = 0; i < inFlight.Count; i++)
            if (inFlight[i] is Grab grab)
                _still[grab] = _flying.TryGetValue(grab, out var seen) ? seen : whereIs?.Invoke(grab.Piece);
        foreach (var (grab, from) in _flying)
            if (!_still.ContainsKey(grab) && caughtFrom(((Grab)grab).Piece) is { } gave)
                foreach (var (item, count) in gave) _gained.Add((item, count, from));
        (_flying, _still) = (_still, _flying);
        return _gained;
    }
}
