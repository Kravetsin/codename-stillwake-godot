using System;
using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// The pieces this peer's own hits have just run on (0042): out in space a hit is heard by its striker
/// alone, through the suit. A hit seen in flight at one read and gone from it at the next has run, landed
/// or refused, on the wrecks its fan struck when first seen (the fan of rays). Read every frame after the session's ticks
/// and the hero's hits sent, so every own hit is seen in flight before its tick can run.
/// </summary>
public sealed class OwnHits
{
    // By reference: two hits alike are equal records, and one may run while the other flies. Each with the pieces it struck.
    private Dictionary<Command, IReadOnlyList<int>> _flying = new(ReferenceEqualityComparer.Instance), _still = new(ReferenceEqualityComparer.Instance);
    /// <summary>Each piece an own hit ran on, and the weapon of the last that did.</summary>
    private readonly Dictionary<int, string> _ran = [];

    /// <summary>
    /// Takes this peer's commands in flight now (<c>Client.InFlight</c>), each wreck hit new among them striking the pieces
    /// <paramref name="struck"/> says, by id (<c>DebrisModule.Struck</c>).
    /// </summary>
    public void Read(IReadOnlyList<Command> inFlight, Func<HewWrecks, IReadOnlyList<int>> struck)
    {
        _ran.Clear();
        _still.Clear();
        for (int i = 0; i < inFlight.Count; i++)
            if (inFlight[i] is HewWrecks hew) _still[hew] = _flying.TryGetValue(hew, out var pieces) ? pieces : struck(hew);
        foreach (var (hit, pieces) in _flying)
            if (!_still.ContainsKey(hit))
                foreach (int piece in pieces) _ran[piece] = ((HewWrecks)hit).Tool;
        (_flying, _still) = (_still, _flying);
    }

    /// <summary>Whether a hit of this peer's on <paramref name="piece"/> ran since the read before the last.</summary>
    public bool Ran(int piece) => _ran.ContainsKey(piece);

    /// <summary>The weapon of this peer's hit that ran on <paramref name="piece"/> since the read before the last; none for none.</summary>
    public string? ToolOn(int piece) => _ran.GetValueOrDefault(piece);
}
