using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The board as this machine keeps it, for the world played: what the page and the tablet's key share. A world with id 0 (made before ids, or a
/// dev run) keeps it only while it is played.
/// </summary>
public sealed class BoardKeep(string folder)
{
    private uint _world;
    private bool _pinMoved;
    private string? _pinned;

    /// <summary>What is new on the board.</summary>
    public BoardSeen Seen { get; private set; } = new(null);

    /// <summary>The goal pinned, on this machine only; null for none.</summary>
    public string? Pinned
    {
        get => _pinned;
        set
        {
            if (value == _pinned) return;
            (_pinned, _pinMoved) = (value, true);
        }
    }

    /// <summary>Turns to world <paramref name="world"/>, reading its file; nothing while it is the one already. An older file's marks read as seen.</summary>
    public void Of(uint world)
    {
        if (world == _world) return;
        var kept = BoardFile.Read(BoardFile.PathOf(folder, world));
        var seen = kept.Seen == null && kept.Marked == null ? null : (kept.Seen ?? []).Concat(kept.Marked ?? []);
        (_world, _pinMoved, _pinned, Seen) = (world, false, kept.Pinned, new BoardSeen(seen, kept.Orders, kept.Batches));
    }

    /// <summary>Writes what has changed since it last did; false only if it could not.</summary>
    public bool Write()
    {
        if (_world == 0 || !(_pinMoved || Seen.Unwritten)) return true;
        if (!BoardFile.Write(BoardFile.PathOf(folder, _world), new BoardKept([.. Seen.Seen], Seen.OrdersSeen, null, [.. Seen.Batches], _pinned))) return false;
        (_pinMoved, Seen.Unwritten) = (false, false);
        return true;
    }
}
