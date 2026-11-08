using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The capback's attacks as drawn (lurker-attacks), the user's picks: its gas, «Клубы со спорами» (eight puffs thrown out of its caps,
/// drifting and swelling as they thin, with lit spores each on its own slow loop, breathing in brightness); its slime, «Споровая»
/// (a glob with two drops behind it, a splat, a puddle that glows and breathes, spores rising off it). Each a list of sprites of
/// <see cref="CreatureArt.LurkerFx"/>'s parts and the lights they give, in pixels of the world, y down; times in seconds. Plain C#, tested.
/// </summary>
public static class LurkerFx
{
    /// <summary>A part placed: its middle, turn (radians), scale, alpha; a light mask shines in <see cref="Shine"/>, the rest is lit as the world.</summary>
    public readonly record struct Sprite(string Part, double X, double Y, double R, double Sx, double Sy, double A, TickArt.Rgb? Shine = null);

    /// <summary>How long a patch takes to go at its end: its last second.</summary>
    public const double GoS = 1.0;

    /// <summary>How far gone a patch is with <paramref name="leftS"/> to go, 0..1: nothing till its last second, all at its end.</summary>
    public static double Going(double leftS) => 1 - Smooth(leftS / GoS);

    // The puffs: their part, where each settles from the capback's feet, its size, and how late it is thrown.
    private static readonly (string Part, double X, double Y, double Size, double Late)[] Puffs =
    [
        ("gasPuff", -26, -22, .85, .06), ("gasPuffSmall", -10, -26, .95, .1), ("gasPuff", 8, -25, .9, .08), ("gasPuffSmall", 24, -21, .85, .12),
        ("gasPuffBig", -22, -9, 1, 0), ("gasPuff", -4, -12, 1.05, .03), ("gasPuffBig", 16, -10, .95, .05), ("gasPuffSmall", 28, -6, 1, .08),
    ];

    // The spores: where each settles, how late, its size, its loop's phase, speeds and reach, how fast it breathes, its colour.
    private static readonly (double X, double Y, double Late, double Size, double Phase, double Wx, double Wy, double Ax, double Ay, double Breath, bool Violet)[] Spores = MakeSpores();

    private static (double, double, double, double, double, double, double, double, double, double, bool)[] MakeSpores()
    {
        var r = new Random(0x5e);
        var spores = new (double, double, double, double, double, double, double, double, double, double, bool)[22];
        for (int i = 0; i < spores.Length; i++)
            spores[i] = ((r.NextDouble() - .5) * 62, -4 - r.NextDouble() * 28, .04 + r.NextDouble() * .14, .7 + r.NextDouble() * .6, r.NextDouble() * Math.Tau,
                .35 + r.NextDouble() * .35, .3 + r.NextDouble() * .3, 2.5 + r.NextDouble() * 3.5, 1.5 + r.NextDouble() * 2.5, .9 + r.NextDouble() * 1.1, i % 4 == 3);
        return spores;
    }

    /// <summary>
    /// The gas <paramref name="t"/> after it was vented, <paramref name="going"/> gone: thrown from (<paramref name="vx"/>, <paramref name="vy"/>),
    /// the capback's caps, to settle round (<paramref name="ox"/>, <paramref name="oy"/>), its feet.
    /// </summary>
    public static void Gas(List<Sprite> sprites, List<LurkerArt.Light> lights, double t, double going, double ox, double oy, double vx, double vy)
    {
        for (int i = 0; i < Puffs.Length; i++)
        {
            var p = Puffs[i];
            if (t < p.Late) continue;
            double k = Out((t - p.Late) / .45);
            double x = Lerp(vx, ox + p.X, k) + Math.Sin(t * .7 + i * 1.7) * 2.2, y = Lerp(vy, oy + p.Y, k) - t * .5 + Math.Sin(t * .9 + i) * .8;
            double s = Lerp(.2, p.Size, k) * (1 + .06 * Math.Sin(t * 1.1 + i) + .3 * going);
            sprites.Add(new(p.Part, x, y, Math.Sin(t * .4 + i) * .08, s, s, .5 * Math.Min(1, k * 3) * (1 - going)));
        }
        foreach (var p in Spores)
        {
            if (t < p.Late) continue;
            double k = Out((t - p.Late) / .6);
            double x = Lerp(vx, ox + p.X, k) + Math.Sin(t * p.Wx + p.Phase) * p.Ax, y = Lerp(vy, oy + p.Y, k) - t * .6 + Math.Cos(t * p.Wy + p.Phase * 1.3) * p.Ay;
            double glow = .55 + .45 * Math.Sin(t * p.Breath + p.Phase);
            sprites.Add(new("spore", x, y, 0, p.Size, p.Size, glow * Math.Min(1, k * 2.5) * (1 - going), p.Violet ? LurkerArt.Violet : LurkerArt.Teal));
        }
        lights.Add(new(ox, oy - 17, 36, LurkerArt.Teal, .22 * Math.Min(1, t / .4) * (1 - going)));
    }

    /// <summary>The glob at (<paramref name="x"/>, <paramref name="y"/>) heading <paramref name="heading"/> (radians), <paramref name="t"/> into its flight: two drops behind it, spinning.</summary>
    public static void Glob(List<Sprite> sprites, List<LurkerArt.Light> lights, double x, double y, double heading, double t)
    {
        double ux = Math.Cos(heading), uy = Math.Sin(heading), spin = t * 7;
        sprites.Add(new("drop", x - ux * 6.4, y - uy * 6.4, 0, .5, .5, .9));
        sprites.Add(new("drop", x - ux * 3.6, y - uy * 3.6, 0, .75, .75, 1));
        sprites.Add(new("glob", x, y, spin, 1, 1, 1));
        sprites.Add(new("globLit", x, y, spin, 1, 1, .95, LurkerArt.Teal));
        lights.Add(new(x, y, 12, LurkerArt.Teal, .4));
    }

    /// <summary>
    /// The puddle <paramref name="t"/> after its glob landed, <paramref name="going"/> gone, its foot's middle at (<paramref name="x"/>,
    /// <paramref name="y"/>) on the floor: a splat, then a pool spreading, glowing and breathing, spores rising off it; nothing before it lands.
    /// </summary>
    public static void Puddle(List<Sprite> sprites, List<LurkerArt.Light> lights, double t, double going, double x, double y)
    {
        if (t < 0) return;
        double shown = 1 - going, g = Out(t / .2), wide = Lerp(.3, 1, g) * (1 - .1 * going);
        sprites.Add(new("pool", x, y, 0, wide, 1 - .3 * going, shown));
        if (t < .26) sprites.Add(new("splat", x, y, 0, Lerp(.6, 1.15, Out(t / .26)), Lerp(1.2, .7, t / .26), 1 - Smooth((t - .12) / .14)));
        sprites.Add(new("poolLit", x, y, 0, wide, 1, (.6 + .25 * Math.Sin(t * 2.4)) * shown, LurkerArt.Teal));
        for (int k = 0; k < 2; k++)
        {
            double u = (t * .33 + k * .5) % 1;
            sprites.Add(new("motes", x, y - u * 8, 0, k == 0 ? 1 : -1, 1, Math.Sin(u * Math.PI) * .85 * shown * Math.Min(1, t / .8), k == 0 ? LurkerArt.Teal : LurkerArt.Violet));
        }
        lights.Add(new(x, y - 4, 26, LurkerArt.Teal, .38 * shown));
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static double Out(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return 1 - (1 - t) * (1 - t) * (1 - t);
    }

    private static double Smooth(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }
}
