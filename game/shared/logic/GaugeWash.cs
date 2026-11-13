using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// A canister breathed, on the air row (air-feedback, hud-vitals): the lit stretches whiten and cool from left to right, the
/// stretches it filled glow on a while, and the row shakes a moment.
/// Plain C#: what each part shows at a time since the breath.
/// </summary>
public sealed class GaugeWash
{
    /// <summary>How long the row shakes; how long the new stretches glow; how long the wash lasts.</summary>
    public const double ShakeS = 0.22, GlowS = 1.8, Lasts = 2;
    /// <summary>When the lit stretches cool: each this much after the one left of it, over <see cref="CoolS"/>.</summary>
    public const double CoolEach = 0.028, CoolS = 0.5;

    private double _since = -1;

    /// <summary>Seconds since the breath, or null when it has passed.</summary>
    public double? Since => _since >= 0 ? _since : null;
    /// <summary>The first stretch the breath fills: stretches from it glow on.</summary>
    public int From { get; private set; }

    /// <summary>A canister breathed, the row's stretches lit up to <paramref name="firstNew"/>.</summary>
    public void Breathe(int firstNew) => (_since, From) = (0, firstNew);

    public void Step(double seconds)
    {
        if (_since < 0) return;
        _since += seconds;
        if (_since > Lasts) _since = -1;
    }

    /// <summary>Where the row is thrown, whole pixels: a short shake, then still.</summary>
    public (int X, int Y) Shake => _since is >= 0 and < ShakeS
        ? ((int)Math.Round(Math.Sin(_since * 95)), (int)Math.Round(Math.Cos(_since * 70) * 0.6))
        : (0, 0);

    /// <summary>How white lit stretch <paramref name="stretch"/> burns, 1 till it cools.</summary>
    public double White(int stretch)
    {
        if (_since < 0) return 0;
        double at = stretch * CoolEach;
        return _since < at ? 1 : Math.Max(0, 1 - (_since - at) / CoolS);
    }

    /// <summary>The glow on a stretch the breath filled; none on those lit before.</summary>
    public double Glow(int stretch) => _since < 0 || stretch < From ? 0 : 0.4 * Math.Max(0, 1 - _since / GlowS);
}
