using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Research;

/// <summary>What one read of the board brought, its cause fixed as it came (board-islands).</summary>
/// <param name="Cause"><see cref="BoardSeen.CauseFound"/>, <see cref="BoardSeen.CauseBuilt"/> or <see cref="BoardSeen.CauseOpened"/>.</param>
/// <param name="By">What caused it: the items newly obtained in Obtained order, or the machines newly standing; none for what was opened.</param>
/// <param name="Things">What became known, a cause among them if it did.</param>
/// <param name="Closer">What came closer in it: almost now, with more of its parts had than before; null in a file written before it.</param>
public sealed record BoardBatch(string Cause, List<string> By, List<string> Things, List<string>? Closer = null);

/// <summary>
/// What is new on the board for this player (board-islands): each read that brought something known is a batch, its cause fixed then; a
/// thing is unread while it is in a batch and not seen, and hovering or clicking sees it. The research key counts the batches with something unread
/// while the tablet is away. And the market's orders met, which light it until the market is shown. With nothing kept, the first read sees all
/// that is known, so a new world or a late guest has nothing new.
/// </summary>
/// <param name="seen">What was kept as seen; null for nothing.</param>
/// <param name="orders">How many orders met were kept as seen; null for nothing.</param>
/// <param name="batches">The batches kept, oldest first; null for none.</param>
public sealed class BoardSeen(IEnumerable<string>? seen, int? orders = null, IEnumerable<BoardBatch>? batches = null)
{
    /// <summary>The causes of a batch: items newly obtained, machines newly standing, or neither (a blueprint picked up, a purchase).</summary>
    public const string CauseFound = "found", CauseBuilt = "built", CauseOpened = "opened";

    /// <summary>The most batches kept: an older one goes, what it held seen.</summary>
    public const int Kept = 40;

    private HashSet<string>? _seen = seen?.ToHashSet(StringComparer.Ordinal);
    private readonly List<BoardBatch> _batches = batches?.Where(b => b is { By: not null, Things: not null }).ToList() ?? [];
    private int? _ordersSeen = orders;
    private bool _open;

    /// <summary>What the last read knew, had, saw standing and saw almost: what the next is compared with; null before this run's first read.</summary>
    private HashSet<string>? _known, _had, _standing;

    /// <summary>How many parts each almost thing had at the last read.</summary>
    private Dictionary<string, int>? _almost;

    /// <summary>What was seen, to keep.</summary>
    public IReadOnlyCollection<string> Seen => _seen ?? [];

    /// <summary>Every batch kept, oldest first.</summary>
    public IReadOnlyList<BoardBatch> Batches => _batches;

    /// <summary>How many orders met were last seen, to keep; null before a read.</summary>
    public int? OrdersSeen => _ordersSeen;

    /// <summary>Whether what is kept has changed since it was last written.</summary>
    public bool Unwritten { get; set; }

    /// <summary>Whether anything on the board is known: the research key shows from the first.</summary>
    public bool Any => _known is { Count: > 0 };

    /// <summary>Whether the tablet is out.</summary>
    public bool Open => _open;

    /// <summary>How many batches hold something unread.</summary>
    public int UnreadBatches => _batches.Count(Unread);

    /// <summary>Whether the tablet's key is lit: a batch holds something unread and the tablet is away.</summary>
    public bool Lit => !_open && UnreadBatches > 0;

    /// <summary>Whether <paramref name="id"/> came in a batch and has not been seen.</summary>
    public bool Unread(string id) => _seen != null && !_seen.Contains(id) && _batches.Any(b => b.Things.Contains(id));

    /// <summary>Whether <paramref name="batch"/> holds something not seen.</summary>
    public bool Unread(BoardBatch batch) => _seen != null && batch.Things.Any(t => !_seen.Contains(t));

    /// <summary>A read of the board's things as <paramref name="read"/> says: what became known since the last forms a batch.</summary>
    public void Read(BoardRead read)
    {
        var known = BoardThings.Ids.Where(read.Knows).ToHashSet(StringComparer.Ordinal);
        var standing = BoardThings.Machines.Where(read.Stands).ToHashSet(StringComparer.Ordinal);
        var almost = BoardThings.Ids.Where(id => !known.Contains(id) && BoardView.PencilOf(id, read) > 0).ToDictionary(id => id, id => BoardView.PartsOf(id, read).Had, StringComparer.Ordinal);
        if (_seen == null)
        {
            _seen = known;
            Unwritten = true;
        }
        else if (_known == null)
        {
            // This run's first read with a file: what it knew is what it saw or was told of; what came since was had now.
            var before = _seen.Concat(_batches.SelectMany(b => b.Things)).ToHashSet(StringComparer.Ordinal);
            (_known, _had, _standing, _almost) = (before, read.Found.Where(before.Contains).ToHashSet(StringComparer.Ordinal), standing, almost);
        }
        if (_known != null) Compare(read, known, standing, almost);
        (_known, _had, _standing, _almost) = (known, read.Found.ToHashSet(StringComparer.Ordinal), standing, almost);
    }

    private void Compare(BoardRead read, HashSet<string> known, HashSet<string> standing, Dictionary<string, int> almost)
    {
        var things = BoardThings.Ids.Where(id => known.Contains(id) && !_known!.Contains(id)).ToList();
        if (things.Count == 0) return;
        var had = read.Found.Where(i => !_had!.Contains(i)).ToList();
        var built = BoardThings.Machines.Where(m => standing.Contains(m) && !_standing!.Contains(m)).ToList();
        var closer = BoardThings.Ids.Where(id => almost.TryGetValue(id, out int now) && (!_almost!.TryGetValue(id, out int was) || now > was)).ToList();
        _batches.Add(had.Count > 0 ? new BoardBatch(CauseFound, had, things, closer)
            : built.Count > 0 ? new BoardBatch(CauseBuilt, built, things, closer)
            : new BoardBatch(CauseOpened, [], things, closer));
        while (_batches.Count > Kept)
        {
            _seen!.UnionWith(_batches[0].Things);
            _batches.RemoveAt(0);
        }
        Unwritten = true;
    }

    /// <summary>The player hovered or clicked <paramref name="id"/>: it is read. Answers whether it was unread.</summary>
    public bool See(string id)
    {
        if (!Unread(id)) return false;
        _seen!.Add(id);
        Unwritten = true;
        return true;
    }

    /// <summary>"Read all": everything in a batch is read.</summary>
    public void SeeAll()
    {
        if (_seen == null) return;
        foreach (var batch in _batches) _seen.UnionWith(batch.Things);
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

    public void Opened() => _open = true;

    /// <summary>The tablet put away: what was not read stays unread.</summary>
    public void Closed() => _open = false;
}
