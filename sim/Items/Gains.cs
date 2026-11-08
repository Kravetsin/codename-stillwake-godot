using System.Collections.Generic;

namespace Stillwake.Sim.Items;

/// <summary>
/// <paramref name="Count"/> of <paramref name="Item"/> that went into peer <paramref name="Peer"/>'s inventory at
/// <paramref name="Cell"/>, the <paramref name="Seq"/>th such gain since the module was made or loaded (2026-11-06-head-tally).
/// </summary>
public sealed record Gain(long Seq, int Peer, string Item, int Count, int Cell);

public sealed partial class ItemsModule
{
    /// <summary>How many of the latest gains <see cref="Gains"/> keeps: more than four heroes digging make between two frames.</summary>
    public const int GainsKept = 32;

    private readonly List<Gain> _gains = [];
    private long _gainsSeen;

    /// <summary>
    /// The latest gains, oldest first, for the client to show: born into an inventory from a dig, a felling or a refund. Never
    /// saved, and empty after a load, so peers' differ: nothing in the sim reads it.
    /// </summary>
    public IReadOnlyList<Gain> Gains => _gains;

    public override void Rebuild()
    {
        _gains.Clear();
        _gainsSeen = 0;
    }

    public void NoteDelivered(int peer, string good, int cell) => Note(peer, [(good, 1)], cell);

    /// <summary>Notes <paramref name="given"/>, what went into <paramref name="peer"/>'s inventory, as gains at <paramref name="cell"/>.</summary>
    private void Note(int peer, IReadOnlyList<(string Item, int Count)> given, int cell)
    {
        foreach (var (item, count) in given)
        {
            if (_gains.Count == GainsKept) _gains.RemoveAt(0);
            _gains.Add(new Gain(++_gainsSeen, peer, item, count, cell));
        }
    }
}
