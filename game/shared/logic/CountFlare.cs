using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// How a number on screen moves to a new one it reads (credit-counter, as picked): it counts there quickly and flares, a rise
/// toward white, a fall toward ember, the digits that changed glowing on a moment after. Plain C#: stepped by its view each frame.
/// </summary>
public sealed class CountFlare
{
    /// <summary>How long a count takes, and the least it is cut to when reads come quicker, so a steady drain never stutters.</summary>
    public const double CountS = 0.4, LeastS = 0.14;
    /// <summary>How long the flare takes to come up, and how long it glows on after the count.</summary>
    public const double RiseS = 0.06, GlowS = 0.52;
    /// <summary>The weakest flare, and the share of the number a change must reach to flare whole.</summary>
    public const double Faint = 0.3, Whole = 0.08;

    private int _from, _to;
    private double _since = double.PositiveInfinity, _sinceRead = double.PositiveInfinity, _span = CountS;
    private bool _read;

    /// <summary>A read: the first is shown at once, any other counted to.</summary>
    public void Read(int value)
    {
        if (!_read)
        {
            _read = true;
            _from = _to = value;
            return;
        }
        if (value == _to) return;
        _from = Shown;
        _span = Math.Min(CountS, Math.Max(LeastS, _sinceRead * 0.9));
        _to = value;
        _since = _sinceRead = 0;
    }

    /// <summary>Moves it on by <paramref name="seconds"/>.</summary>
    public void Step(double seconds)
    {
        _since += seconds;
        _sinceRead += seconds;
    }

    /// <summary>How far the count has gone, 0 to 1.</summary>
    private double Gone => Math.Clamp(_since / _span, 0, 1);

    /// <summary>The number as shown now.</summary>
    public int Shown => (int)Math.Round(_from + (_to - _from) * EaseOut(Gone));

    /// <summary>Whether the last change was a rise.</summary>
    public bool Up => _to >= _from;

    /// <summary>How hot the flare is now, 0 to 1: up over <see cref="RiseS"/>, down over the count and <see cref="GlowS"/>, weaker for a small change.</summary>
    public double Heat => Pop * Math.Clamp(Math.Abs(_to - _from) / (Whole * Math.Max(1, Math.Max(Math.Abs(_from), Math.Abs(_to)))), Faint, 1);

    /// <summary>The flare's shape whatever the change's size, 0 to 1: what a readout that must be noticed on every change swells by.</summary>
    public double Pop => double.IsInfinity(_since) ? 0 : _since < RiseS ? _since / RiseS : Math.Clamp(1 - (_since - RiseS) / (_span + GlowS), 0, 1);

    /// <summary>How hot digit <paramref name="column"/> is, 0 the units: all of them while counting, then only those that changed.</summary>
    public double HeatOf(int column) => Gone < 1 || DigitAt(_from, column) != DigitAt(_to, column) ? Heat : 0;

    private static int DigitAt(int value, int column)
    {
        string text = Math.Abs(value).ToString();
        int at = text.Length - 1 - column;
        return at >= 0 ? text[at] : -1;
    }

    private static double EaseOut(double t) => 1 - Math.Pow(1 - t, 3);
}
