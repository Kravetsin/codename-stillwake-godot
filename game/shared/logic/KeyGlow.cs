using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// How lit a hologram on the tablet's edge is, or how amber (hologram-keys): it comes on and goes out over a moment, from wherever it was,
/// for the eye catches the change.
/// </summary>
/// <param name="comesOn">Seconds it takes to come on.</param>
/// <param name="goesOut">Seconds it takes to go out.</param>
public sealed class KeyGlow(double comesOn = KeyGlow.ComesOn, double goesOut = KeyGlow.GoesOut)
{
    /// <summary>The research key's amber: seconds it takes to come on, and to go out.</summary>
    public const double ComesOn = 0.9, GoesOut = 0.6;

    private double _along;

    /// <summary>How lit it is, 0 to 1.</summary>
    public double Lit => _along * _along * (3 - 2 * _along);

    /// <summary>Moves it <paramref name="seconds"/> on, towards lit or dark; true if it changed.</summary>
    public bool Step(double seconds, bool lit)
    {
        double was = _along;
        _along = Math.Clamp(_along + (lit ? seconds / comesOn : -seconds / goesOut), 0, 1);
        return _along != was;
    }
}
