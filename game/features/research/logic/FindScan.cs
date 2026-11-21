using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Research;

/// <summary>A record on its way to the research key: the item whose icon folds into it (null: it leaves the drone's eye), how far it has folded, 0..1, and flown, 0..1 eased in; null while it folds.</summary>
public readonly record struct FindRecord(string? Item, double Fold, double? Fly);

/// <summary>What is over the hero's head: the item, how far its icon has come in, 0..1, and how far the scan has swept it, top to bottom, 0..1; null before the sweep.</summary>
public readonly record struct FindOver(string Item, double In, double? Sweep);

/// <summary>
/// The drone scanning a find (find-scan, the pick «Голограмма» made plain), seen by this player only: an item first had shows its icon over the hero's head,
/// the drone's scan sweeps it, it folds into one ember and the ember flies to the research key, which flares as it lands and its number takes the batch.
/// Items had together come one after another, each record leaving after its own scan. A batch with no item (a machine built, a blueprint) is an ember from
/// the eye. With no drone a find lands at once. Times are seconds.
/// </summary>
public sealed class FindScan
{
    /// <summary>
    /// The pick's times: the wait before the first icon, its coming in, the sweep, the fold into an ember, its flight, the key's flare: 1.35 s from a find
    /// to its record in the key. The next of a batch comes in as the one before folds.
    /// </summary>
    public const double Lead = 0.05, Rise = 0.3, Sweep = 0.45, Fold = 0.15, Fly = 0.4, Flare = 0.45;

    private sealed class Find(string? item, bool counts)
    {
        public string? Item { get; } = item;
        /// <summary>Whether its record brings its batch onto the key's number: the first of the batch.</summary>
        public bool Counts { get; } = counts;
        /// <summary>When it began coming in over the head; once it folds, when its fold began.</summary>
        public double Start;
        public double RiseS;
    }

    private readonly Queue<Find> _waiting = new();
    private readonly List<Find> _eyes = [], _records = [];
    private Find? _over;
    private double _clock, _landed = double.NegativeInfinity;
    private object? _seen;
    private BoardBatch? _last;

    /// <summary>
    /// A read of the board's batches, oldest first: <paramref name="seen"/> is whose, so another world's starts over with nothing new. Open, the tablet
    /// shows it all: nothing plays and what played goes.
    /// </summary>
    public void Read(object seen, IReadOnlyList<BoardBatch> batches, bool open)
    {
        int from = After(batches);
        var last = batches.Count > 0 ? batches[^1] : null;
        if (seen != _seen || open)
        {
            (_seen, _last) = (seen, last);
            Clear();
            return;
        }
        for (int i = from; i < batches.Count; i++)
        {
            var batch = batches[i];
            if (batch.Cause == BoardSeen.CauseFound && batch.By.Count > 0)
                for (int k = 0; k < batch.By.Count; k++) _waiting.Enqueue(new Find(batch.By[k], k == 0));
            else _eyes.Add(new Find(null, true));
        }
        _last = last;
    }

    /// <summary>Where the batches not read yet begin: after the last one read, all of them if it is gone.</summary>
    private int After(IReadOnlyList<BoardBatch> batches)
    {
        for (int i = batches.Count - 1; i >= 0; i--)
            if (ReferenceEquals(batches[i], _last)) return i + 1;
        return 0;
    }

    /// <summary>Moves it <paramref name="seconds"/> on; <paramref name="drone"/>, whether a drone floats by the hero to scan.</summary>
    public void Step(double seconds, bool drone)
    {
        // What was read came before this step: it starts from then.
        double read = _clock;
        _clock += seconds;
        foreach (var eye in _eyes)
        {
            if (!drone) Land();
            else
            {
                eye.Start = read;
                _records.Add(eye);
            }
        }
        _eyes.Clear();
        if (_over == null) Next(read + Lead, Rise, drone);
        // The one over the head folds as its sweep ends, its record leaving, and the next comes in as it folds.
        while (_over is { } over && _clock >= over.Start + over.RiseS + Sweep)
        {
            over.Start += over.RiseS + Sweep;
            _records.Add(over);
            _over = null;
            Next(over.Start, Fold, drone);
        }
        for (int i = _records.Count - 1; i >= 0; i--)
        {
            if (_clock < _records[i].Start + Fold + Fly) continue;
            Land();
            _records.RemoveAt(i);
        }
    }

    /// <summary>The next item waiting comes in from <paramref name="at"/> over <paramref name="rise"/> s; with no drone, each lands at once.</summary>
    private void Next(double at, double rise, bool drone)
    {
        while (_waiting.TryDequeue(out var next))
        {
            if (!drone)
            {
                Land();
                continue;
            }
            (next.Start, next.RiseS, _over) = (at, rise, next);
            return;
        }
    }

    private void Land() => _landed = _clock;

    private void Clear()
    {
        _waiting.Clear();
        _eyes.Clear();
        _records.Clear();
        _over = null;
        _landed = double.NegativeInfinity;
    }

    /// <summary>The batches not yet on the key's number: the key's number is the unread less these.</summary>
    public int Holding
    {
        get
        {
            int held = _over is { Counts: true } ? 1 : 0;
            foreach (var find in _waiting) held += find.Counts ? 1 : 0;
            foreach (var find in _eyes) held += find.Counts ? 1 : 0;
            foreach (var find in _records) held += find.Counts ? 1 : 0;
            return held;
        }
    }

    /// <summary>Whether anything shows: an icon over the head, a record on its way, the key's flare.</summary>
    public bool Busy => _over != null || _records.Count > 0 || Glow > 0;

    /// <summary>The icon over the head and how far it is scanned; null for none. The drone looks at it meanwhile.</summary>
    public FindOver? Over => _over is { Item: { } item } over
        ? new FindOver(item, Smooth(Math.Clamp((_clock - over.Start) / over.RiseS, 0, 1)),
            _clock - over.Start - over.RiseS is var swept and >= 0 ? Math.Min(1, swept / Sweep) : null)
        : null;

    /// <summary>How many records are on their way.</summary>
    public int Records => _records.Count;

    /// <summary>Record <paramref name="i"/> of <see cref="Records"/>.</summary>
    public FindRecord RecordAt(int i)
    {
        var record = _records[i];
        double since = _clock - record.Start;
        return new FindRecord(record.Item, Math.Clamp(since / Fold, 0, 1), since < Fold ? null : EaseIn(Math.Min(1, (since - Fold) / Fly)));
    }

    /// <summary>How brightly the key flares, 1 as a record lands, fading to 0.</summary>
    public double Glow => Math.Max(0, 1 - (_clock - _landed) / Flare);

    private static double Smooth(double t) => t * t * (3 - 2 * t);

    // Gathering speed toward the key, so the eye is pulled along to it.
    private static double EaseIn(double t) => t * t;
}
