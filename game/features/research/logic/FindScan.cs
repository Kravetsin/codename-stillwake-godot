using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Research;

/// <summary>A record on its way to the research key: the item whose icon folds into it (null: it leaves the drone's eye), how far it has folded, 0..1, and flown, 0..1 eased in; null while it folds.</summary>
public readonly record struct FindRecord(string? Item, double Fold, double? Fly);

/// <summary>What is over the hero's head: the item, how far its icon has come in, 0..1, and how far the scan has swept it, top to bottom, 0..1; null before the sweep.</summary>
public readonly record struct FindOver(string Item, double In, double? Sweep);

/// <summary>Where a lesson is (drone-lessons): the drone going to the thing it learns from, scanning it, then showing the hero the form in a vision (form-vision).</summary>
public enum LessonStage { Go, Study, Vision }

/// <summary>A form being learned (drone-lessons): the form, its stage and how far that stage has gone, 0..1.</summary>
public readonly record struct FindLesson(string Form, LessonStage Stage, double Progress);

/// <summary>
/// The drone scanning a find (find-scan, the pick «Голограмма» made plain), seen by this player only: an item first had shows its icon over the hero's head,
/// the drone's scan sweeps it, it folds into one ember and the ember flies to the research key, which flares as it lands and its number takes the batch.
/// Items had together come one after another, each record leaving after its own scan. A batch with no item (a machine built, a blueprint) is an ember from
/// the eye. A form the drone learned (drone-lessons) is a lesson, a longer beat before the batch's items: the drone goes to the thing it learned from,
/// scans it, shows the hero what the form does in a vision (form-vision), and only then does the form's icon come in over the head as a find's; its
/// landing flares the key longer. A hero that cannot have the vision as it would begin (passed out) skips it. Nothing cuts a lesson short: finds wait
/// behind it, and opening the tablet puts out the finds but not the lessons. With no drone a find lands at once. Times are seconds.
/// </summary>
public sealed class FindScan
{
    /// <summary>
    /// The pick's times: the wait before the first icon, its coming in, the sweep, the fold into an ember, its flight, the key's flare: 1.35 s from a find
    /// to its record in the key. The next of a batch comes in as the one before folds.
    /// </summary>
    public const double Lead = 0.05, Rise = 0.3, Sweep = 0.45, Fold = 0.15, Fly = 0.4, Flare = 0.45;

    /// <summary>
    /// A lesson's stages before its icon comes in: the drone going to the thing, scanning it, the vision (the crew's <c>FormVision.Length</c>,
    /// a test holds them equal); the key's longer flare.
    /// </summary>
    public const double Go = 0.6, Study = 3.0, Vision = 5.0, LessonFlare = 1.2;

    /// <summary>A lesson's stages together, the vision played.</summary>
    public const double Lesson = Go + Study + Vision;

    private sealed class Find(string? item, bool counts, bool lesson = false)
    {
        public string? Item { get; } = item;
        /// <summary>Whether its record brings its batch onto the key's number: the first of the batch. A lesson kept as the tablet opens no longer does.</summary>
        public bool Counts { get; set; } = counts;
        /// <summary>Whether it is a form learned: a lesson before its icon.</summary>
        public bool IsLesson { get; } = lesson;
        /// <summary>When its lesson began, else when it began coming in over the head; once it folds, when its fold began.</summary>
        public double Start;
        public double LessonS, RiseS;
        /// <summary>A lesson's: whether its vision plays, decided as its study ends; null before.</summary>
        public bool? Envisioned;
    }

    private readonly List<Find> _waiting = [];
    private readonly List<Find> _eyes = [], _records = [];
    private Find? _over;
    private double _clock, _landed = double.NegativeInfinity, _lessonLanded = double.NegativeInfinity;
    private object? _seen;
    private BoardBatch? _last;

    /// <summary>
    /// A read of the board's batches, oldest first: <paramref name="seen"/> is whose, so another world's starts over with nothing new. Open, the tablet
    /// shows it all: the finds go and those found meanwhile play nothing, but a lesson plays on, no longer counted, and one learned meanwhile plays too.
    /// </summary>
    public void Read(object seen, IReadOnlyList<BoardBatch> batches, bool open)
    {
        int from = After(batches);
        var last = batches.Count > 0 ? batches[^1] : null;
        if (seen != _seen)
        {
            (_seen, _last) = (seen, last);
            Clear();
            return;
        }
        if (open) PutOutFinds();
        for (int i = from; i < batches.Count; i++)
        {
            var batch = batches[i];
            // The first of a batch counts; a lesson first, then the items found.
            bool counts = !open;
            foreach (var thing in batch.Things)
                if (BoardThings.Form(thing))
                {
                    _waiting.Add(new Find(thing, counts, lesson: true));
                    counts = false;
                }
            if (open) continue;
            if (batch.Cause == BoardSeen.CauseFound)
                foreach (var item in batch.By)
                {
                    _waiting.Add(new Find(item, counts));
                    counts = false;
                }
            if (counts) _eyes.Add(new Find(null, true));
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

    /// <summary>
    /// Moves it <paramref name="seconds"/> on; <paramref name="drone"/>, whether a drone floats by the hero to scan; <paramref name="envisions"/>,
    /// whether its hero can have a lesson's vision now: one whose study ends meanwhile skips it if not.
    /// </summary>
    public void Step(double seconds, bool drone, bool envisions = true)
    {
        // What was read came before this step: it starts from then.
        double read = _clock;
        _clock += seconds;
        if (_over is { IsLesson: true, Envisioned: null } studied && _clock >= studied.Start + Go + Study)
        {
            studied.Envisioned = envisions;
            if (!envisions) studied.LessonS = Go + Study;
        }
        // An ember from the eye waits while the drone is away on a lesson.
        if (!drone || Learning == null)
        {
            foreach (var eye in _eyes)
            {
                if (!drone) Land(eye);
                else
                {
                    eye.Start = read;
                    _records.Add(eye);
                }
            }
            _eyes.Clear();
        }
        if (_over == null) Next(read + Lead, Rise, drone);
        // The one over the head folds as its sweep ends, its record leaving, and the next comes in as it folds.
        while (_over is { } over && _clock >= over.Start + over.LessonS + over.RiseS + Sweep)
        {
            over.Start += over.LessonS + over.RiseS + Sweep;
            _records.Add(over);
            _over = null;
            Next(over.Start, Fold, drone);
        }
        for (int i = _records.Count - 1; i >= 0; i--)
        {
            if (_clock < _records[i].Start + Fold + Fly) continue;
            Land(_records[i]);
            _records.RemoveAt(i);
        }
    }

    /// <summary>The next waiting comes in from <paramref name="at"/> over <paramref name="rise"/> s; with no drone, each lands at once.</summary>
    private void Next(double at, double rise, bool drone)
    {
        while (_waiting.Count > 0)
        {
            var next = _waiting[0];
            _waiting.RemoveAt(0);
            if (!drone)
            {
                Land(next);
                continue;
            }
            // A lesson's icon comes in from the drone back by the head: the whole way.
            (next.Start, next.LessonS, next.RiseS, _over) = (at, next.IsLesson ? Lesson : 0, next.IsLesson ? Rise : rise, next);
            return;
        }
    }

    private void Land(Find find)
    {
        if (find.IsLesson) _lessonLanded = _clock;
        else _landed = _clock;
    }

    /// <summary>The tablet opened: every find goes, the lessons play on, no longer counted.</summary>
    private void PutOutFinds()
    {
        _waiting.RemoveAll(f => !f.IsLesson);
        _records.RemoveAll(f => !f.IsLesson);
        _eyes.Clear();
        if (_over is { IsLesson: false }) _over = null;
        foreach (var find in (ReadOnlySpan<List<Find>>)[_waiting, _records])
            foreach (var kept in find) kept.Counts = false;
        if (_over != null) _over.Counts = false;
        _landed = double.NegativeInfinity;
    }

    private void Clear()
    {
        _waiting.Clear();
        _eyes.Clear();
        _records.Clear();
        _over = null;
        _landed = _lessonLanded = double.NegativeInfinity;
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

    /// <summary>Whether anything shows: a lesson, an icon over the head, a record on its way, the key's flare.</summary>
    public bool Busy => _over != null || _records.Count > 0 || Glow > 0;

    /// <summary>The icon over the head and how far it is scanned; null for none, and through a lesson's stages. The drone looks at it meanwhile.</summary>
    public FindOver? Over => _over is { Item: { } item } over && _clock >= over.Start + over.LessonS
        ? new FindOver(item, Smooth(Math.Clamp((_clock - over.Start - over.LessonS) / over.RiseS, 0, 1)),
            _clock - over.Start - over.LessonS - over.RiseS is var swept and >= 0 ? Math.Min(1, swept / Sweep) : null)
        : null;

    /// <summary>The lesson in its stages before its icon, and how far; null for none.</summary>
    public FindLesson? Learning
    {
        get
        {
            if (_over is not { IsLesson: true, Item: { } form } over) return null;
            double t = Math.Max(0, _clock - over.Start);
            return t >= over.LessonS ? null
                : t < Go ? new FindLesson(form, LessonStage.Go, t / Go)
                : t < Go + Study ? new FindLesson(form, LessonStage.Study, (t - Go) / Study)
                : new FindLesson(form, LessonStage.Vision, (t - Go - Study) / Vision);
        }
    }

    /// <summary>Whether a lesson of <paramref name="form"/> is still to play or playing, its icon not yet landed on the key: the form not open yet.</summary>
    public bool Holds(string form)
    {
        bool Of(Find find) => find.IsLesson && find.Item == form;
        return _over is { } over && Of(over) || _waiting.Exists(Of) || _records.Exists(Of);
    }

    /// <summary>How many records are on their way.</summary>
    public int Records => _records.Count;

    /// <summary>Record <paramref name="i"/> of <see cref="Records"/>.</summary>
    public FindRecord RecordAt(int i)
    {
        var record = _records[i];
        double since = _clock - record.Start;
        return new FindRecord(record.Item, Math.Clamp(since / Fold, 0, 1), since < Fold ? null : EaseIn(Math.Min(1, (since - Fold) / Fly)));
    }

    /// <summary>How brightly the key flares, 1 as a record lands, fading to 0: over <see cref="Flare"/>, over <see cref="LessonFlare"/> for a lesson's.</summary>
    public double Glow => Math.Max(Math.Max(0, 1 - (_clock - _landed) / Flare), Math.Max(0, 1 - (_clock - _lessonLanded) / LessonFlare));

    private static double Smooth(double t) => t * t * (3 - 2 * t);

    // Gathering speed toward the key, so the eye is pulled along to it.
    private static double EaseIn(double t) => t * t;
}
