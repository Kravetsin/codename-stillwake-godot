using System;
using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// The pieces this peer's own hits have just been sent at and have just run on (0042): out in space a hit is heard by its
/// striker alone, through the suit, and felt as it is sent (0014). A hit seen in flight at one read and gone from it at the
/// next has run, landed or refused, on the wrecks it struck when first seen: its fan's (the fan of rays), or those it names (tool-edge). Read every frame after
/// the session's ticks and the hero's hits sent, so every own hit is seen in flight before its tick can run.
/// </summary>
public sealed class OwnHits
{
    // By reference: two hits alike are equal records, and one may run while the other flies. Each with the pieces it struck.
    private Dictionary<Command, (IReadOnlyList<int> Pieces, string Tool)> _flying = new(ReferenceEqualityComparer.Instance), _still = new(ReferenceEqualityComparer.Instance);
    /// <summary>Each piece an own hit ran on, and the weapon of the last that did.</summary>
    private readonly Dictionary<int, string> _ran = [];
    /// <summary>Each piece an own hit first seen at this read strikes, and the weapon of the last that does.</summary>
    private readonly Dictionary<int, string> _sent = [];

    /// <summary>
    /// Takes this peer's commands in flight now (<c>Client.InFlight</c>), each wreck hit new among them striking the pieces
    /// <paramref name="struck"/> says a fan strikes, by id (<c>DebrisModule.Struck</c>), or those a <see cref="HewPieces"/> names.
    /// </summary>
    public void Read(IReadOnlyList<Command> inFlight, Func<HewWrecks, IReadOnlyList<int>> struck)
    {
        _ran.Clear();
        _sent.Clear();
        _still.Clear();
        for (int i = 0; i < inFlight.Count; i++)
        {
            var hit = inFlight[i];
            if (hit is not (HewWrecks or HewPieces)) continue;
            if (_flying.TryGetValue(hit, out var seen)) _still[hit] = seen;
            else
            {
                _still[hit] = seen = hit is HewWrecks hew ? (struck(hew), hew.Tool) : (((HewPieces)hit).Pieces, ((HewPieces)hit).Tool);
                foreach (int piece in seen.Pieces) _sent[piece] = seen.Tool;
            }
        }
        foreach (var (hit, seen) in _flying)
            if (!_still.ContainsKey(hit))
                foreach (int piece in seen.Pieces) _ran[piece] = seen.Tool;
        (_flying, _still) = (_still, _flying);
    }

    /// <summary>Whether a hit of this peer's on <paramref name="piece"/> ran since the read before the last.</summary>
    public bool Ran(int piece) => _ran.ContainsKey(piece);

    /// <summary>The weapon of this peer's hit that ran on <paramref name="piece"/> since the read before the last; none for none.</summary>
    public string? ToolOn(int piece) => _ran.GetValueOrDefault(piece);

    /// <summary>The weapon of this peer's hit first seen in flight at the last read striking <paramref name="piece"/>; none for none.</summary>
    public string? SentOn(int piece) => _sent.GetValueOrDefault(piece);
}
