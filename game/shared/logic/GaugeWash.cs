using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// A canister breathed, on the suit gauge (air-feedback, as picked): the whole tab floods sky after a white flash, its frame's rim
/// lights up, the lit lamps whiten and cool from left to right, the lamps it filled glow on a while, and the tab shakes a moment.
/// Plain C#: what each part shows at a time since the breath.
/// </summary>
public sealed class GaugeWash
{
    /// <summary>How long the tab shakes, flashes white, floods sky and its rim burns; how long the new lamps glow.</summary>
    public const double ShakeS = 0.22, FlashS = 0.09, FloodS = 0.9, RimS = 0.85, GlowS = 1.8, Lasts = 2;
    /// <summary>When the lit lamps cool after the flash: each this much after the one left of it, over <see cref="CoolS"/>.</summary>
    public const double CoolEach = 0.028, CoolS = 0.5;

    private double _since = -1;

    /// <summary>Seconds since the breath, or null when it has passed.</summary>
    public double? Since => _since >= 0 ? _since : null;
    /// <summary>The first lamp the breath fills: lamps from it glow on.</summary>
    public int From { get; private set; }

    /// <summary>A canister breathed, the gauge's lamps lit up to <paramref name="firstNew"/>.</summary>
    public void Breathe(int firstNew) => (_since, From) = (0, firstNew);

    public void Step(double seconds)
    {
        if (_since < 0) return;
        _since += seconds;
        if (_since > Lasts) _since = -1;
    }

    /// <summary>Where the tab is thrown, whole pixels: a short shake, then still.</summary>
    public (int X, int Y) Shake => _since is >= 0 and < ShakeS
        ? ((int)Math.Round(Math.Sin(_since * 95)), (int)Math.Round(Math.Cos(_since * 70) * 0.6))
        : (0, 0);

    /// <summary>The sky the screen is flooded with and the white of its first flash, 0 to 1.</summary>
    public double Flood => _since < 0 ? 0 : 0.62 * Square(1 - Math.Clamp(_since / FloodS, 0, 1));
    public double Flash => _since is >= 0 and < FlashS ? 0.55 * (1 - _since / FlashS) : 0;

    /// <summary>How white lit lamp <paramref name="lamp"/> burns, 1 till it cools.</summary>
    public double White(int lamp)
    {
        if (_since < 0) return 0;
        double at = lamp * CoolEach;
        return _since < at ? 1 : Math.Max(0, 1 - (_since - at) / CoolS);
    }

    /// <summary>The glow on a lamp the breath filled; none on those lit before.</summary>
    public double Glow(int lamp) => _since < 0 || lamp < From ? 0 : 0.4 * Math.Max(0, 1 - _since / GlowS);

    /// <summary>How bright the frame's rim burns, 0 to 1.</summary>
    public double Rim => _since < 0 ? 0 : Square(Math.Max(0, 1 - _since / RimS));

    private static double Square(double x) => x * x;
}
