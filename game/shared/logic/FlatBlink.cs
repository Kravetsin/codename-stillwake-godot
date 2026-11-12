using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// A press the sim would refuse, shown where the reason is (no-charge-flash): <see cref="Blinks"/> red blinks, hard on and
/// nearly off, a jolt as the first lands. The energy tab's bolt for a tool the belt cannot pay; the oxygen sign and the
/// canister's slots for a canister cooling (canister-mark). Plain C#.
/// </summary>
public sealed class FlatBlink
{
    /// <summary>How many blinks a press throws and how long each lasts; the share of one lit; how long the first jolts.</summary>
    public const int Blinks = 3;
    public const double BlinkS = 0.22, LitShare = 0.6, JoltS = 0.12;

    private double _since = -1;

    /// <summary>Pressed again: blinks start again from the first.</summary>
    public void Press() => _since = 0;

    public void Step(double seconds)
    {
        if (_since < 0) return;
        _since += seconds;
        if (_since >= Blinks * BlinkS) _since = -1;
    }

    /// <summary>How red it burns now, 0 to 1: full as a blink lands, easing a little while lit, all but out between.</summary>
    public double Red
    {
        get
        {
            if (_since < 0) return 0;
            double at = _since / BlinkS % 1;
            return at < LitShare ? 1 - 0.3 * at / LitShare : 0.08;
        }
    }

    /// <summary>Where the first blink throws it, whole pixels: a jolt, then still.</summary>
    public (int X, int Y) Jolt => _since is >= 0 and < JoltS ? ((int)Math.Round(Math.Sin(_since * 110)), 0) : (0, 0);
}
