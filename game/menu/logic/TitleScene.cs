using System;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Menu;

/// <summary>Where the title's hero hangs, its middle in the scene's pixels, and how far it has turned, in radians.</summary>
public readonly record struct Drift(Vec At, double Turn);

/// <summary>
/// The scene behind the title, В«РџСЂРѕС‚РёРІ СЃРѕР»РЅС†Р°В» in its soft light, picked on <c>tools/pick-kit/pages/title-tether.js</c>: a small
/// far sun right of the middle in one soft falloff, a hero hanging limp beside it, turning, on a tether that runs off the frame's
/// foot, and the stars going by to the left. In the page's pixels, <see cref="Wide"/> by <see cref="High"/>, one a screen pixel
/// at 1280 Г— 720; the left third left calm for the menu. Plain arithmetic.
/// </summary>
public static class TitleScene
{
    /// <summary>The scene's size in its pixels: the page's, the title's screen.</summary>
    public const int Wide = 1280, High = 720;

    /// <summary>The menu's third, at the left: nothing moves in it.</summary>
    public const double Third = Wide / 3.0;

    /// <summary>The sun's middle.</summary>
    public static readonly Vec Sun = new(840, 290);

    /// <summary>
    /// The sun's halo the hero reaches into, its hard white-warm disc, and how far its soft light falls off, in pixels.
    /// </summary>
    public const double HaloR = 100, CoreR = 6.5, GlowR = 300;

    /// <summary>The soft light's falloff: its strength at a share of <see cref="GlowR"/> from the sun, straight between.</summary>
    public static readonly (double At, double Alpha)[] Falloff = [(0, 0.95), (0.025, 0.6), (0.08, 0.24), (0.24, 0.08), (0.55, 0.02), (1, 0)];

    /// <summary>How much larger than the rig's pixels the hero is drawn: about 46 pixels, 6.5 % of the frame.</summary>
    public const double HeroScale = 1.05;

    /// <summary>The square, in the scene's pixels, the hero is drawn in about its middle before its light is laid on.</summary>
    public const double HeroBox = 120;

    /// <summary>Where the hero's middle hangs on average, and how long one whole turn takes it, in seconds.</summary>
    public static readonly Vec Base = new(794, 334);

    public const double TurnSeconds = 46;

    /// <summary>The stars' two layers, the far and the near, going by to the left, in pixels a second.</summary>
    public const double FarDrift = 2.2, NearDrift = 4.5;

    /// <summary>The suit's lamp on the chest plate, in the rig's pixels from the hero's middle.</summary>
    public static readonly Vec Lamp = new(2.2, -6.6);

    /// <summary>How long one blink of the lamp takes, how much of it it is lit, and how far into its first it starts, in seconds.</summary>
    public const double LampSeconds = 3.4, LampLit = 0.32, LampFrom = 1.1;

    private const double Tau = 2 * Math.PI;

    /// <summary>The hero <paramref name="seconds"/> into the title: drifting a few pixels, turning whole, against the clock.</summary>
    public static Drift Motion(double seconds) => new(
        new Vec(Base.X + 9 * Math.Sin(Tau * seconds / 57), Base.Y + 7 * Math.Sin(Tau * seconds / 43 + 2)),
        2.4 - Tau * seconds / TurnSeconds + 0.12 * Math.Sin(Tau * seconds / 15));

    /// <summary>A point of the hero, in the rig's pixels from its middle, in the scene's pixels.</summary>
    public static Vec ToScene(Drift drift, Vec point)
    {
        double x = HeroScale * point.X, y = HeroScale * point.Y, c = Math.Cos(drift.Turn), s = Math.Sin(drift.Turn);
        return new Vec(drift.At.X + x * c - y * s, drift.At.Y + x * s + y * c);
    }

    /// <summary>Which way the light comes from at <paramref name="at"/>: toward the sun, a unit vector.</summary>
    public static Vec LightAt(Vec at)
    {
        double x = Sun.X - at.X, y = Sun.Y - at.Y, d = Math.Sqrt(x * x + y * y);
        return d == 0 ? new Vec(1, 0) : new Vec(x / d, y / d);
    }

    /// <summary>How strong the soft light is <paramref name="distance"/> pixels from the sun's middle, 0 to 1.</summary>
    public static double SunAlpha(double distance)
    {
        double u = distance / GlowR;
        for (int i = 1; i < Falloff.Length; i++)
            if (u <= Falloff[i].At)
            {
                var (a, b) = (Falloff[i - 1], Falloff[i]);
                return a.Alpha + (b.Alpha - a.Alpha) * (Math.Max(u, a.At) - a.At) / (b.At - a.At);
            }
        return 0;
    }

    /// <summary>How bright the tether's lit edge is at <paramref name="at"/>: dimming smoothly away from the sun.</summary>
    public static double EdgeAlpha(Vec at) =>
        0.85 * Math.Exp(-Math.Sqrt((at.X - Sun.X) * (at.X - Sun.X) + (at.Y - Sun.Y) * (at.Y - Sun.Y)) / 220) + 0.06;

    /// <summary>Whether the suit's lamp is lit <paramref name="seconds"/> into the title.</summary>
    public static bool LampOn(double seconds) => (seconds + LampFrom) % LampSeconds < LampLit;
}
