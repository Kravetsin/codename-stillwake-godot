using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>An ember flying into the core: where it is, pick px from the core's middle, its heading and how hot it is, 0..1.</summary>
public readonly record struct CoreEmber(float X, float Y, float Dx, float Dy, float Heat);

/// <summary>
/// The drone's core filling (drone-abilities): how full it shows, eased toward its charge, and the embers each gain sends spiralling in from
/// round it, a flare as they land. A stand-in look until the gun form's pick. Pick px, seconds.
/// </summary>
public sealed class CoreEmbers
{
    /// <summary>The most embers in flight; a gain's count, one per <see cref="WorthEach"/> of worth, at least one and at most <see cref="MostEach"/>.</summary>
    public const int Most = 24, MostEach = 8, WorthEach = 15;

    /// <summary>How far out an ember starts, pick px (about three cells), how long it flies, s, and how much of a turn it spirals.</summary>
    public const float From = 60, Flight = 0.5f, Spiral = 0.35f;

    /// <summary>How long the shown fill takes to most of the way to the charge, s; how long a landing's flare lasts, s.</summary>
    public const float FillEase = 0.35f, FlareFor = 0.25f;

    private readonly (double Born, float Angle, float Lead)[] _flying = new (double, float, float)[Most];
    private int _next, _count;
    private int? _charge;
    private double _flareAt = double.NegativeInfinity;

    /// <summary>How full the core shows, 0..1, eased toward its charge.</summary>
    public float Shown { get; private set; }

    /// <summary>How bright a landing's flare is now, 0..1.</summary>
    public float Flare { get; private set; }

    /// <summary>
    /// The core's <paramref name="charge"/> of <paramref name="full"/> at <paramref name="t"/>, after <paramref name="elapsed"/> s: a gain beyond
    /// its own filling sends embers in, the first reading none; a drop or its own filling eases with none.
    /// </summary>
    public void Step(int charge, int full, double t, double elapsed)
    {
        // Its own filling in the frame, a tick more for a frame that saw one tick early: loot is what comes beyond it.
        int own = (int)Math.Ceiling(Content.Abilities.Regen * (Sim.World.TicksPerSecond * elapsed + 1));
        if (_charge is int was && charge - was > own)
        {
            int count = Math.Clamp((charge - was - own) / WorthEach, 1, MostEach);
            for (int i = 0; i < count; i++)
            {
                uint seed = (uint)(_next * 2654435761u);
                float angle = seed % 6283 / 1000f, lead = i * 0.06f;
                _flying[_next % Most] = (t + lead, angle, lead);
                _next++;
                _count = Math.Min(Most, _count + 1);
            }
        }
        _charge = charge;
        float target = full > 0 ? Math.Clamp(charge / (float)full, 0, 1) : 0;
        Shown += (target - Shown) * (float)(1 - Math.Exp(-elapsed / FillEase));
        for (int i = 0; i < _count; i++)
            if (_flying[i].Born + Flight <= t && _flying[i].Born + Flight > t - elapsed) _flareAt = _flying[i].Born + Flight;
        Flare = (float)Math.Clamp(1 - (t - _flareAt) / FlareFor, 0, 1);
    }

    /// <summary>The embers in flight at <paramref name="t"/>, into <paramref name="into"/>; how many.</summary>
    public int At(double t, Span<CoreEmber> into)
    {
        int n = 0;
        for (int i = 0; i < _count && n < into.Length; i++)
        {
            var (born, angle, _) = _flying[i];
            double u = (t - born) / Flight;
            if (u < 0 || u >= 1) continue;
            // Eased in: slow out there, fast as it lands.
            float k = (float)(u * u), r = From * (1 - k), a = angle + Spiral * MathF.Tau * k;
            float x = r * MathF.Cos(a), y = r * MathF.Sin(a);
            float len = MathF.Max(0.001f, MathF.Sqrt(x * x + y * y));
            into[n++] = new CoreEmber(x, y, -x / len, -y / len, (float)Math.Min(1, u * 4));
        }
        return n;
    }
}
