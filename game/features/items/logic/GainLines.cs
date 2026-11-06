using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// What one place just gave this hero, as a column of lines beside it (2026-11-06-gain-spots): one per item, its count adding up
/// while the item keeps coming, held a while after the last and faded. A line keeps its row while it grows, so the column never
/// shuffles under a drill; a new item opens a line on top, the oldest lowest.
/// </summary>
public sealed class GainLines
{
    /// <summary>How long a line stays whole after its last gain, then how long it fades, in ms; a gain within both adds to it.</summary>
    public const double HoldMs = 1600, FadeMs = 500;
    /// <summary>How long an add's flare lasts, rising then falling, and a new line's fade in, in ms.</summary>
    public const double PopRiseMs = 60, PopMs = 900, InMs = 140;
    /// <summary>How many lines show at most: a new item past them pushes the oldest off.</summary>
    public const int Most = 5;
    /// <summary>How fast a line slides to its row, per second.</summary>
    public const double Slide = 14;
    /// <summary>The key of a body's line: all it gave at once (2026-10-29-body-bag).</summary>
    public const string Body = "items.fromBody";

    /// <summary>One item's line: what it names and how many, when it opened and last grew, and its row.</summary>
    public sealed class Line(string key, int count, double now)
    {
        public string Key { get; } = key;
        public int Count { get; internal set; } = count;
        public double BornMs { get; } = now;
        public double LastMs { get; internal set; } = now;
        /// <summary>When it last grew, ms: what its flare runs from; none while it holds its first count.</summary>
        public double? GrewMs { get; internal set; }
        /// <summary>How many rows above the column's foot it sits, sliding to its row: 0 the oldest.</summary>
        public double Row { get; internal set; } = double.NaN;
    }

    private readonly List<Line> _lines = [];
    private double _now;

    /// <summary>The lines, oldest first: the oldest lowest, the newest on top.</summary>
    public IReadOnlyList<Line> Lines => _lines;

    /// <summary>Adds <paramref name="count"/> of <paramref name="key"/> gained now, to its line while it shows, else to a new one.</summary>
    public void Add(string key, int count)
    {
        if (_lines.Find(l => l.Key == key) is { } line)
        {
            line.Count += count;
            (line.LastMs, line.GrewMs) = (_now, _now);
            return;
        }
        _lines.Add(new Line(key, count, _now));
        if (_lines.Count > Most) _lines.RemoveAt(0);
    }

    /// <summary>Moves <paramref name="elapsedMs"/> on: lines age and go, the rest sliding to their rows.</summary>
    public void Update(double elapsedMs)
    {
        _now += elapsedMs;
        _lines.RemoveAll(l => _now - l.LastMs >= HoldMs + FadeMs);
        double k = 1 - Math.Exp(-Slide * elapsedMs / 1000);
        for (int i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            line.Row = double.IsNaN(line.Row) ? i : line.Row + (i - line.Row) * k;
        }
    }

    /// <summary>How solid <paramref name="line"/> is now, 0 to 1: faded in, held, faded out.</summary>
    public double Alpha(Line line)
    {
        double fin = Math.Clamp((_now - line.BornMs) / InMs, 0, 1), age = _now - line.LastMs;
        return fin * (age < HoldMs ? 1 : Math.Clamp(1 - (age - HoldMs) / FadeMs, 0, 1));
    }

    /// <summary>How much <paramref name="line"/> flares from its last add, 0 to 1: up in a moment, down over most of a second.</summary>
    public double Pop(Line line)
    {
        if (line.GrewMs is not { } grew) return 0;
        double since = _now - grew;
        return since < PopRiseMs ? since / PopRiseMs : Math.Clamp(1 - (since - PopRiseMs) / (PopMs - PopRiseMs), 0, 1);
    }
}
