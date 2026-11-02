using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// How far each row of the market has deciphered ([market-decrypt](../../../../docs/decisions/2026-11-02-market-decrypt.md)): a clock of its own that never touches the sim. Row 0 is
/// open from the start; row <c>n</c> opens with order <c>n</c>, which deciphers its cards and the next row's head. Rows opened together start together.
/// </summary>
/// <param name="rows">How many rows the market has.</param>
public sealed class MarketReveal(int rows)
{
    /// <summary>Seconds a row's decipher takes, and how long it holds off as the market comes up: the tablet's rise.</summary>
    public const double Takes = 1.6, Rise = 0.2;

    /// <summary>The part of a decipher the head takes, and the rows' heights move in; of a card's own span, where it starts and ends, and how much later each card starts than the last.</summary>
    private const double HeadEnds = 0.3, CardStarts = 0.1, CardEnds = 0.62, CardAfter = 0.08;

    /// <summary>How many steps of garbage a whole decipher goes through.</summary>
    private const int Steps = 50;

    // Seconds into a row's decipher, below 0 while it holds off; null when it is not playing.
    private readonly double?[] _at = new double?[rows];
    private readonly HashSet<(int Row, int Card)> _cleared = [];
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

    /// <summary>A card shows clear from now on: picking it clears it, for garbage is only a look.</summary>
    public void ClearCard(int row, int card) => _cleared.Add((row, card));

    /// <summary>Whether card <paramref name="card"/> of <paramref name="row"/> still has garbage in it.</summary>
    public bool Deciphering(int row, int card) => Part(row) is { } part && !_cleared.Contains((row, card)) && Span(part, CardStarts + card * CardAfter, CardEnds + card * CardAfter) < 1;

    /// <summary>How many of <paramref name="count"/> characters of a card have cleared.</summary>
    public int CardShown(int row, int card, int count) =>
        Part(row) is { } part && !_cleared.Contains((row, card)) ? (int)(Span(part, CardStarts + card * CardAfter, CardEnds + card * CardAfter) * count) : count;

    /// <summary>Whether a row's head still has garbage in it: the row above is opening it.</summary>
    public bool HeadDeciphering(int row) => row > 0 && Part(row - 1) is { } part && part < HeadEnds;

    /// <summary>How many of <paramref name="count"/> characters of a row's head have cleared.</summary>
    public int HeadShown(int row, int count) => row > 0 && Part(row - 1) is { } part ? (int)(Span(part, 0, HeadEnds) * count) : count;

    /// <summary>Which step of garbage a row's cards are on, the row itself opening them.</summary>
    public int StepOf(int row) => Part(row) is { } part ? (int)(part * Steps) : 0;

    /// <summary>Which step of garbage a row's head is on, the row above opening it.</summary>
    public int HeadStepOf(int row) => row > 0 ? StepOf(row - 1) : 0;

    /// <summary>
    /// How far each row is the tall one, the row of the order being filled, with <paramref name="met"/> orders met: the one whose head is clear and whose order is not, passing to
    /// the next in the first part of a decipher.
    /// </summary>
    public double[] Weights(int met)
    {
        var weights = new double[_at.Length];
        for (int row = 1; row < weights.Length; row++)
        {
            double head = row == 1 ? 1 : row - 1 <= met ? Opened(row - 1) : 0, filled = row <= met ? Opened(row) : 0;
            weights[row] = head * (1 - filled);
        }
        return weights;
    }

    /// <summary>How far a row of an order met has opened: all the way, unless its decipher is still in its first part.</summary>
    private double Opened(int row) => Part(row) is { } part ? Ease(Span(part, 0, HeadEnds)) : 1;

    /// <summary>What part of its decipher a row has gone through, 0 while it holds off; null when it is not playing.</summary>
    private double? Part(int row) => _at[row] is { } at ? Math.Max(0, at) / Takes : null;

    private static double Span(double part, double from, double to) => Math.Clamp((part - from) / (to - from), 0, 1);

    private static double Ease(double part) => 1 - Math.Pow(1 - part, 3);
}
