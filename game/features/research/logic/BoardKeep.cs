using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The board as this machine keeps it, for the world played: what the page and the tablet's key share. A world with id 0 (made before ids, or a
/// dev run) keeps it only while it is played.
/// </summary>
public sealed class BoardKeep(string folder)
{
    private uint _world;
    private bool _moved;

    /// <summary>Where things were moved to.</summary>
    public Dictionary<string, BoardSpot> Spots { get; private set; } = new(StringComparer.Ordinal);

    /// <summary>What is new on the board.</summary>
    public BoardSeen Seen { get; private set; } = new(null);

    /// <summary>Turns to world <paramref name="world"/>, reading its file; nothing while it is the one already.</summary>
    public void Of(uint world)
    {
        if (world == _world) return;
        var kept = BoardFile.Read(BoardFile.PathOf(folder, world));
        (_world, _moved, Spots, Seen) = (world, false, kept.Moved ?? new(StringComparer.Ordinal), new BoardSeen(kept.Seen));
    }

    /// <summary>Stands <paramref name="id"/> at <paramref name="spot"/>.</summary>
    public void Move(string id, BoardSpot spot)
    {
        Spots[id] = spot;
        _moved = true;
    }

    /// <summary>Writes what has changed since it last did; false only if it could not.</summary>
    public bool Write()
    {
        if (_world == 0 || !(_moved || Seen.Unwritten)) return true;
        if (!BoardFile.Write(BoardFile.PathOf(folder, _world), new BoardKept(Spots, Seen.Seen.ToList()))) return false;
        (_moved, Seen.Unwritten) = (false, false);
        return true;
    }
}
