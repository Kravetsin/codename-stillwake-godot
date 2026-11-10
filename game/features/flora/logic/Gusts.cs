using System;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// The wind on a planet's surface (plant-stirs), a look only: calm for a while, then a gust one way that rises, flutters and
/// dies, its strength rolling along the ground with it. Each peer blows its own. Plain C#.
/// </summary>
public sealed class Gusts
{
    private readonly FloraLooks.Wind _wind;
    private readonly Func<double> _random;
    private double _seconds, _start, _length;
    private int _way = 1;

    /// <summary>The wind of <paramref name="wind"/>, its waits and gusts drawn from <paramref name="random"/> (0 to 1).</summary>
    public Gusts(FloraLooks.Wind wind, Func<double> random)
    {
        _wind = wind;
        _random = random;
        _start = Between(wind.Calm);
        _length = Between(wind.Gust);
    }

    /// <summary>Seconds blown so far: what the gust's roll along the ground follows.</summary>
    public double Seconds => _seconds;

    /// <summary>1 blowing to the right, -1 to the left.</summary>
    public int Way => _way;

    /// <summary>How hard it blows now, 0 calm to 1 a full gust, signed by its way.</summary>
    public float Strength { get; private set; }

    /// <summary>Whether a gust blows now.</summary>
    public bool Blowing { get; private set; }

    /// <summary>Blows on for <paramref name="seconds"/>.</summary>
    public void Advance(double seconds)
    {
        _seconds += seconds;
        double t = _seconds - _start;
        if (t >= _length)
        {
            _start = _seconds + Between(_wind.Calm);
            _length = Between(_wind.Gust);
            _way = _random() < 0.5 ? -1 : 1;
            t = _seconds - _start;
        }
        Blowing = t >= 0;
        if (!Blowing)
        {
            Strength = 0;
            return;
        }
        // Rising over its first second and a half, dying over its last two, fluttering between.
        double rise = Math.Clamp(t / 1.5, 0, 1), fall = Math.Clamp((_length - t) / 2, 0, 1);
        double flutter = 0.8 + 0.2 * Math.Sin(t * 5.3) * Math.Sin(t * 2.1 + 1);
        Strength = (float)(_way * Smooth(rise) * Smooth(fall) * flutter);
    }

    /// <summary>Set by the dev room's F9: a gust wanted now, taken by the surface's wind (<see cref="Now"/>).</summary>
    public static bool Asked { get; set; }

    /// <summary>A gust now, whatever the wait: the dev room's F9.</summary>
    public void Now()
    {
        _start = _seconds;
        _length = Between(_wind.Gust);
    }

    /// <summary>The gust's strength at <paramref name="x"/> pixels along the ground, rolling with it: from 0.6 to 1 of <see cref="Strength"/>.</summary>
    public float At(float x) => Strength * (0.8f + 0.2f * MathF.Sin(x * Roll - (float)_seconds * _way * 3));

    /// <summary>How fast the gust's strength rolls along the ground, in radians a pixel: the same in the shader (<c>Bend</c>).</summary>
    public const float Roll = 0.045f;

    private double Between((double From, double To) span) => span.From + (span.To - span.From) * _random();

    private static double Smooth(double t) => t * t * (3 - 2 * t);
}
