using System;

namespace Stillwake.Game.Features.Energy;

/// <summary>
/// A press of an electric tool the belt cannot pay (no-charge-flash): the energy tab's bolt blinks red <see cref="Blinks"/> times, hard on
/// and nearly off, throwing red light, and jolts as the first lands, so a player pressing a drill that does nothing sees why. Plain C#.
/// </summary>
public sealed class FlatBlink
{
    /// <summary>How many blinks a press throws and how long each lasts; the share of one lit; how long the first jolts the bolt.</summary>
    public const int Blinks = 3;
    public const double BlinkS = 0.22, LitShare = 0.6, JoltS = 0.12;

    private double _since = -1;

    /// <summary>The belt was pressed flat: blinks start again from the first.</summary>
    public void Press() => _since = 0;

    public void Step(double seconds)
    {
        if (_since < 0) return;
        _since += seconds;
        if (_since >= Blinks * BlinkS) _since = -1;
    }

    /// <summary>How red the bolt burns now, 0 to 1: full as a blink lands, easing a little while lit, all but out between.</summary>
    public double Red
    {
        get
        {
            if (_since < 0) return 0;
            double at = _since / BlinkS % 1;
            return at < LitShare ? 1 - 0.3 * at / LitShare : 0.08;
        }
    }

    /// <summary>Where the first blink throws the bolt, whole pixels: a jolt, then still.</summary>
    public (int X, int Y) Jolt => _since is >= 0 and < JoltS ? ((int)Math.Round(Math.Sin(_since * 110)), 0) : (0, 0);
}
