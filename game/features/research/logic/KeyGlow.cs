using System;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// How lit the lens on the tablet's key is ([key-lens](../../../../docs/decisions/2026-11-02-key-lens.md)): it comes on and goes out over a moment,
/// from wherever it was, for the eye catches the change.
/// </summary>
public sealed class KeyGlow
{
    /// <summary>Seconds it takes to come on, and to go out.</summary>
    public const double ComesOn = 0.9, GoesOut = 0.6;

    private double _along;

    /// <summary>How lit it is, 0 to 1.</summary>
    public double Lit => _along * _along * (3 - 2 * _along);

    /// <summary>Moves it <paramref name="seconds"/> on, towards lit or dark; true if it changed.</summary>
    public bool Step(double seconds, bool lit)
    {
        double was = _along;
        _along = Math.Clamp(_along + (lit ? seconds / ComesOn : -seconds / GoesOut), 0, 1);
        return _along != was;
    }
}
