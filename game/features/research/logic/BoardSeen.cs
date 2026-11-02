using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// What is new on the board for this player (docs/plans/board.md): the notes since the tablet last closed, which light its key while it is away and
/// are marked until it closes. With nothing kept, what was seen starts as things stand at the first read, so a new world or a late guest has nothing new.
/// </summary>
/// <param name="seen">What was kept; null for nothing.</param>
public sealed class BoardSeen(IEnumerable<string>? seen)
{
    private HashSet<string>? _seen = seen?.ToHashSet(StringComparer.Ordinal);
    private HashSet<string> _notes = new(StringComparer.Ordinal);
    private bool _open;

    /// <summary>What was last seen, to keep.</summary>
    public IReadOnlyCollection<string> Seen => _seen ?? [];

    /// <summary>Whether what was seen has changed since it was last kept.</summary>
    public bool Unwritten { get; set; }

    /// <summary>Whether the tablet's key is lit: something is new and the tablet is away.</summary>
    public bool Lit => !_open && _seen != null && _notes.Any(n => !_seen.Contains(n));

    /// <summary>A read of the notes on the board.</summary>
    public void Read(IEnumerable<string> notes)
    {
        _notes = notes.ToHashSet(StringComparer.Ordinal);
        if (_seen != null) return;
        _seen = new HashSet<string>(_notes, StringComparer.Ordinal);
        Unwritten = true;
    }

    /// <summary>Whether <paramref name="id"/> is a note new since the tablet last closed.</summary>
    public bool Marked(string id) => _seen != null && _notes.Contains(id) && !_seen.Contains(id);

    public void Opened() => _open = true;

    /// <summary>The tablet put away: what it showed is seen.</summary>
    public void Closed()
    {
        _open = false;
        if (_seen == null || _seen.SetEquals(_notes)) return;
        _seen = new HashSet<string>(_notes, StringComparer.Ordinal);
        Unwritten = true;
    }
}
