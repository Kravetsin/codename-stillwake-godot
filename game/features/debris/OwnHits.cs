using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// The pieces this peer's own hits have just run on (0042): out in space a hit is heard by its striker
/// alone, through the suit. A hit seen in flight at one read and gone from it at the next has run, landed
/// or refused. Read every frame after the session's ticks and the hero's hits sent, so every own hit is
/// seen in flight before its tick can run.
/// </summary>
public sealed class OwnHits
{
    // By reference: two hits on one piece are equal records, and one may run while the other flies.
    private HashSet<Command> _flying = new(ReferenceEqualityComparer.Instance), _still = new(ReferenceEqualityComparer.Instance);
    /// <summary>Each piece an own hit ran on, and the weapon of the last that did.</summary>
    private readonly Dictionary<int, string> _ran = [];

    /// <summary>Takes this peer's commands in flight now (<c>Client.InFlight</c>).</summary>
    public void Read(IReadOnlyList<Command> inFlight)
    {
        _ran.Clear();
        _still.Clear();
        for (int i = 0; i < inFlight.Count; i++)
            if (inFlight[i] is Hit) _still.Add(inFlight[i]);
        foreach (var hit in _flying)
            if (!_still.Contains(hit)) _ran[((Hit)hit).Piece] = ((Hit)hit).Tool;
        (_flying, _still) = (_still, _flying);
    }

    /// <summary>Whether a hit of this peer's on <paramref name="piece"/> ran since the read before the last.</summary>
    public bool Ran(int piece) => _ran.ContainsKey(piece);

    /// <summary>The weapon of this peer's hit that ran on <paramref name="piece"/> since the read before the last; none for none.</summary>
    public string? ToolOn(int piece) => _ran.GetValueOrDefault(piece);
}
