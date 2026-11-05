using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// What is new on the board for this player (docs/decisions/2026-11-05-the-board.md): the notes not yet read, one at a time as they land (<see cref="BoardPile"/>), which light
/// its key while it is away, what landed being marked until the player touches it ([board-thoughts](../../../../docs/decisions/2026-11-02-board-thoughts.md)); and the market's orders met, which light it until the market is chosen. With nothing
/// kept, what was seen starts as things stand at the first read, so a new world or a late guest has nothing new.
/// </summary>
/// <param name="seen">What was kept; null for nothing.</param>
/// <param name="orders">How many orders met were kept as seen; null for nothing.</param>
/// <param name="marked">What was kept as marked; null for nothing.</param>
public sealed class BoardSeen(IEnumerable<string>? seen, int? orders = null, IEnumerable<string>? marked = null)
{
    private HashSet<string>? _seen = seen?.ToHashSet(StringComparer.Ordinal);
    private HashSet<string> _notes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _marked = marked?.ToHashSet(StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
    private readonly HashSet<string> _landed = new(StringComparer.Ordinal);
    private int? _ordersSeen = orders;
    private bool _open;

    /// <summary>What was last seen, to keep.</summary>
    public IReadOnlyCollection<string> Seen => _seen ?? [];

    /// <summary>What landed and was not touched yet, to keep.</summary>
    public IReadOnlyCollection<string> AllMarked => _marked;

    /// <summary>The notes not yet read.</summary>
    public IReadOnlyCollection<string> Unread => _seen == null ? [] : _notes.Where(n => !_seen.Contains(n)).ToHashSet(StringComparer.Ordinal);

    /// <summary>How many orders met were last seen, to keep; null before a read.</summary>
    public int? OrdersSeen => _ordersSeen;

    /// <summary>Whether what was seen has changed since it was last kept.</summary>
    public bool Unwritten { get; set; }

    /// <summary>Whether the board holds any note, read or not: the research key shows from the first.</summary>
    public bool Any => _notes.Count > 0;

    /// <summary>Whether the tablet is out.</summary>
    public bool Open => _open;

    /// <summary>Whether the tablet's key is lit: a note is unread and the tablet is away.</summary>
    public bool Lit => !_open && _seen != null && _notes.Any(n => !_seen.Contains(n));

    /// <summary>A read of the notes on the board.</summary>
    public void Read(IEnumerable<string> notes)
    {
        _notes = notes.ToHashSet(StringComparer.Ordinal);
        if (_seen != null) return;
        _seen = new HashSet<string>(_notes, StringComparer.Ordinal);
        Unwritten = true;
    }

    /// <summary>A read of how many of the market's orders are met: with nothing kept, they are seen as things stand.</summary>
    public void ReadOrders(int met)
    {
        if (_ordersSeen != null) return;
        _ordersSeen = met;
        Unwritten = true;
    }

    /// <summary>
    /// The market on the screen with <paramref name="met"/> orders met, as the sim says now: they are seen from here on. How many were seen before it, for what to decipher; with
    /// nothing kept it starts as things stand, and nothing plays.
    /// </summary>
    public int SeeOrders(int met)
    {
        int was = _ordersSeen ?? met;
        if (_ordersSeen != null && met <= was) return was;
        _ordersSeen = met;
        Unwritten = true;
        return was;
    }

    /// <summary><paramref name="id"/> read: it goes onto the board, marked until the player touches it.</summary>
    public void Land(string id)
    {
        if (_seen == null || !_seen.Add(id)) return;
        _marked.Add(id);
        _landed.Add(id);
        Unwritten = true;
    }

    /// <summary>Whether <paramref name="id"/> has landed and the player has not touched it yet.</summary>
    public bool Marked(string id) => _marked.Contains(id);

    /// <summary>The player took <paramref name="id"/> in hand: it is no longer marked.</summary>
    public void Touch(string id)
    {
        if (_marked.Remove(id)) Unwritten = true;
    }

    /// <summary>Whether <paramref name="id"/> has landed since the tablet was last put away: its threads stand out till then.</summary>
    public bool Fresh(string id) => _landed.Contains(id);

    public void Opened() => _open = true;

    /// <summary>The tablet put away: what landed is no longer fresh; what was not read stays unread.</summary>
    public void Closed()
    {
        _open = false;
        _landed.Clear();
    }
}
