using System;

namespace Stillwake.Game.Features.Energy;

/// <summary>
/// The energy tab's number between reads (energy-ring): a fall glides to the read in one read's span, so a steady drain runs
/// down evenly; a rise eases out over <see cref="RiseS"/>. No flare: the bolt and the ring say that charge flows.
/// </summary>
public sealed class EnergyCount
{
    /// <summary>A fall's span, the energy tab's read; a rise's.</summary>
    public const double FallS = 0.25, RiseS = 0.6;

    private double _from, _to, _t = 1, _span = FallS;
    private bool _read, _rise;

    /// <summary>What is shown now, in power units.</summary>
    public double Value { get; private set; }

    /// <summary>A read: the first is shown at once, any other glided to from what is shown.</summary>
    public void Read(int units)
    {
        if (!_read)
        {
            _read = true;
            Value = _from = _to = units;
            return;
        }
        if (units == _to) return;
        (_from, _to, _t) = (Value, units, 0);
        _rise = units > _from;
        _span = _rise ? RiseS : FallS;
    }

    /// <summary>Moves what is shown on by <paramref name="seconds"/>.</summary>
    public void Step(double seconds)
    {
        _t = Math.Min(1, _t + seconds / _span);
        double k = _rise ? 1 - Math.Pow(1 - _t, 3) : _t;
        Value = _from + (_to - _from) * k;
    }
}
