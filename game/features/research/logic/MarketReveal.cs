using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// How far each order's decipher has gone ([market-grid](../../../../docs/decisions/2026-11-03-market-grid.md)): a clock of its own that never touches the sim. Row <c>n</c>
/// opens with order <c>n</c>, which deciphers the goods it opened and the next order's head. Rows opened together start together.
/// </summary>
/// <param name="rows">How many rows the market has.</param>
public sealed class MarketReveal(int rows)
{
    /// <summary>Seconds a row's decipher takes, and how long it holds off as the market comes up: the tablet's rise.</summary>
    public const double Takes = 1.6, Rise = 0.2;

    /// <summary>The part of a decipher the head takes; of a tile's own span, where the first starts and ends, and how much later each starts than the last at most.</summary>
    private const double HeadEnds = 0.3, CardStarts = 0.1, CardEnds = 0.62, CardAfter = 0.08;

    /// <summary>How many steps of garbage a whole decipher goes through.</summary>
    private const int Steps = 50;

    // Seconds into a row's decipher, below 0 while it holds off; null when it is not playing.
    private readonly double?[] _at = new double?[rows];
    private readonly HashSet<string> _cleared = new(StringComparer.Ordinal);
    private double _hold;

    /// <summary>Whether any row is deciphering, or about to.</summary>
    public bool Playing => Array.Exists(_at, at => at != null);

    /// <summary>The market came up: what plays now holds off for its rise, and nothing already playing goes on.</summary>
    public void Came()
    {
        Clear();
        _hold = Rise;
    }

    /// <summary>Everything clear at once: the market put away, or left for another tab.</summary>
    public void Clear()
    {
        Array.Fill(_at, null);
        _cleared.Clear();
        _hold = 0;
    }

    /// <summary>Starts the rows of orders <paramref name="seen"/> + 1 to <paramref name="met"/>; none if the first is not past the second.</summary>
    public void Play(int seen, int met)
    {
        for (int row = Math.Max(seen + 1, 1); row <= met && row < _at.Length; row++) _at[row] = -_hold;
    }

    /// <summary>A frame of <paramref name="seconds"/>.</summary>
    public void Step(double seconds)
    {
        _hold = Math.Max(0, _hold - seconds);
        for (int row = 0; row < _at.Length; row++)
        {
            if (_at[row] is not { } at) continue;
            at += seconds;
            _at[row] = at >= Takes ? null : at;
        }
    }

    /// <summary>Whether <paramref name="row"/> is deciphering, or about to.</summary>
    public bool Plays(int row) => row >= 0 && row < _at.Length && _at[row] != null;

    /// <summary>A good shows clear from now on: picking it clears it, for garbage is only a look.</summary>
    public void ClearCard(string id) => _cleared.Add(id);

    /// <summary>Whether good <paramref name="id"/>, tile <paramref name="index"/> of the <paramref name="count"/> that <paramref name="row"/> deciphers, still has garbage in it.</summary>
    public bool Deciphering(string id, int row, int index, int count) => Of(id, row, index, count) is { } part && part < 1;

    /// <summary>How many of <paramref name="chars"/> characters of that good have cleared.</summary>
    public int CardShown(string id, int row, int index, int count, int chars) => Of(id, row, index, count) is { } part ? (int)(part * chars) : chars;

    /// <summary>Whether a row's head still has garbage in it: the row above is opening it.</summary>
    public bool HeadDeciphering(int row) => row > 0 && Part(row - 1) is { } part && part < HeadEnds;

    /// <summary>How many of <paramref name="count"/> characters of a row's head have cleared.</summary>
    public int HeadShown(int row, int count) => row > 0 && Part(row - 1) is { } part ? (int)(Span(part, 0, HeadEnds) * count) : count;

    /// <summary>Which step of garbage a row's goods are on, the row itself opening them.</summary>
    public int StepOf(int row) => Part(row) is { } part ? (int)(part * Steps) : 0;

    /// <summary>Which step of garbage a row's head is on, the row above opening it.</summary>
    public int HeadStepOf(int row) => row > 0 ? StepOf(row - 1) : 0;

    /// <summary>How far through its own span a good is; null when it shows clear. Each starts a little after the last, all done before the decipher is.</summary>
    private double? Of(string id, int row, int index, int count)
    {
        if (Part(row) is not { } part || _cleared.Contains(id)) return null;
        double after = count > 1 ? Math.Min(CardAfter, (1 - CardEnds - CardStarts) / (count - 1)) : 0;
        return Span(part, CardStarts + index * after, CardEnds + index * after);
    }

    /// <summary>What part of its decipher a row has gone through, 0 while it holds off; null when it is not playing.</summary>
    private double? Part(int row) => row >= 0 && row < _at.Length && _at[row] is { } at ? Math.Max(0, at) / Takes : null;

    private static double Span(double part, double from, double to) => Math.Clamp((part - from) / (to - from), 0, 1);
}
