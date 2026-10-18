using System;
using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// What this peer's own catches just put into its inventory, for the pick-up popups over its hero: a grab seen in
/// flight at one read and gone from it at the next has run, and the sim says what it gave (<c>DebrisModule.CaughtFrom</c>);
/// a refused one gave nothing. Read every frame after the hero's grabs are sent, so every own grab is seen in flight
/// before its tick can run. A wreck broken by a hit floats its loot, shown as any floor stack once picked up.
/// </summary>
public sealed class OwnCatches
{
    // By reference, as OwnHits: commands of equal payloads are equal records.
    private HashSet<Command> _flying = new(ReferenceEqualityComparer.Instance), _still = new(ReferenceEqualityComparer.Instance);
    private readonly List<(string Item, int Count)> _gained = [];

    /// <summary>
    /// Takes this peer's commands in flight now (<c>Client.InFlight</c>) and returns what its grabs that ran since the
    /// last read put into its inventory, catch by catch, each by item; <paramref name="caughtFrom"/> is the sim's answer
    /// for a piece. Live until the next read.
    /// </summary>
    public IReadOnlyList<(string Item, int Count)> Read(IReadOnlyList<Command> inFlight, Func<int, IReadOnlyList<(string Item, int Count)>?> caughtFrom)
    {
        _gained.Clear();
        _still.Clear();
        for (int i = 0; i < inFlight.Count; i++)
            if (inFlight[i] is Grab) _still.Add(inFlight[i]);
        foreach (var grab in _flying)
            if (!_still.Contains(grab) && caughtFrom(((Grab)grab).Piece) is { } gave)
                _gained.AddRange(gave);
        (_flying, _still) = (_still, _flying);
        return _gained;
    }
}
