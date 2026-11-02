using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// What is new on the board for this player (docs/plans/board.md): the notes not yet read, one at a time as they land (<see cref="BoardPile"/>), which light
/// its key while it is away, what landed being marked until it closes; and the market's orders met, which light it until the market is chosen. With nothing
/// kept, what was seen starts as things stand at the first read, so a new world or a late guest has nothing new.
/// </summary>
/// <param name="seen">What was kept; null for nothing.</param>
/// <param name="orders">How many orders met were kept as seen; null for nothing.</param>
public sealed class BoardSeen(IEnumerable<string>? seen, int? orders = null)
{
    private HashSet<string>? _seen = seen?.ToHashSet(StringComparer.Ordinal);
    private HashSet<string> _notes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _landed = new(StringComparer.Ordinal);
    private int? _ordersSeen = orders;
    private int _orders;
    private bool _open;

    /// <summary>What was last seen, to keep.</summary>
    public IReadOnlyCollection<string> Seen => _seen ?? [];

    /// <summary>The notes not yet read.</summary>
    public IReadOnlyCollection<string> Unread => _seen == null ? [] : _notes.Where(n => !_seen.Contains(n)).ToHashSet(StringComparer.Ordinal);

    /// <summary>How many orders met were last seen, to keep; null before a read.</summary>
    public int? OrdersSeen => _ordersSeen;

    /// <summary>Whether what was seen has changed since it was last kept.</summary>
    public bool Unwritten { get; set; }

    /// <summary>Whether the tablet's key is lit: something is new and the tablet is away.</summary>
    public bool Lit => !_open && (MarketNew || (_seen != null && _notes.Any(n => !_seen.Contains(n))));

    /// <summary>Whether an order has been met since the market was last chosen: the market's tab says so.</summary>
    public bool MarketNew => _orders > _ordersSeen;

    /// <summary>A read of the notes on the board.</summary>
    public void Read(IEnumerable<string> notes)
    {
        _notes = notes.ToHashSet(StringComparer.Ordinal);
        if (_seen != null) return;
        _seen = new HashSet<string>(_notes, StringComparer.Ordinal);
        Unwritten = true;
    }

    /// <summary>A read of how many of the market's orders are met.</summary>
    public void ReadOrders(int met)
    {
        _orders = met;
        if (_ordersSeen != null) return;
        _ordersSeen = met;
        Unwritten = true;
    }

    /// <summary>The market shown: its orders met are seen.</summary>
    public void MarketChosen()
    {
        if (!MarketNew) return;
        _ordersSeen = _orders;
        Unwritten = true;
    }

    /// <summary><paramref name="id"/> read: it goes onto the board, marked until the tablet closes.</summary>
    public void Land(string id)
    {
        if (_seen == null || !_seen.Add(id)) return;
        _landed.Add(id);
        Unwritten = true;
    }

    /// <summary>Whether <paramref name="id"/> has landed since the tablet was last put away.</summary>
    public bool Marked(string id) => _landed.Contains(id);

    public void Opened() => _open = true;

    /// <summary>The tablet put away: the marks go; what was not read stays unread.</summary>
    public void Closed()
    {
        _open = false;
        _landed.Clear();
    }
}
