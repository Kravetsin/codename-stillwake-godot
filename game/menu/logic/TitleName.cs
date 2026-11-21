using System;
using System.Runtime.InteropServices;
using Ink = Stillwake.Game.Menu.TitleNameInk;

namespace Stillwake.Game.Menu;

/// <summary>One grain of the name's dust, in the page's pixels: where it rests in its letter, and its drift once it leaves.</summary>
public readonly record struct Grain(double X, double Y, double Leaves, double Alpha, double Half, bool Glint, double Vx, double Vy, double Sway, double Period);

/// <summary>
/// The game's name on the title as picked (title-logo, «Пепел», «Отражение»; <see cref="TitleNameInk"/>): the word whole for
/// <see cref="Ink.D0"/> s, then it turns to dust from its right end, the wind carrying the dust toward the sun; the dust slows,
/// stops and comes back the same way, and the word is whole again at <see cref="Ink.Ge"/> s, to the loop's end. In the page's
/// pixels, the loop's seconds. Plain arithmetic.
/// </summary>
public static class TitleName
{
    private const double Tau = Math.PI * 2, Wind = 0.8, Appear = 0.45, Thinning = 6;

    /// <summary>Every grain of the dust, as exported.</summary>
    public static readonly Grain[] Grains = Read();

    private static Grain[] Read()
    {
        var f = MemoryMarshal.Cast<byte, float>(Convert.FromBase64String(Ink.Grains));
        var grains = new Grain[f.Length / 10];
        for (int i = 0; i < grains.Length; i++)
        {
            var g = f.Slice(i * 10, 10);
            grains[i] = new Grain(g[0], g[1], g[2], g[3], g[4], g[5] > 0.5f, g[6], g[7], g[8], g[9]);
        }
        return grains;
    }

    /// <summary>The loop's own clock at <paramref name="t"/> s into it: forward to the middle of the time apart, a soft stop, then back.</summary>
    public static double Clock(double t)
    {
        double d = Math.Abs(t - Ink.Mid);
        return Ink.Mid - (d >= Ink.Eps ? d : (d * d + Ink.Eps * Ink.Eps) / (2 * Ink.Eps));
    }

    /// <summary>Whether the word is whole at <paramref name="t"/>, every grain home.</summary>
    public static bool Whole(double t) => Clock(t) <= Ink.D0;

    /// <summary>Where <paramref name="g"/> is at <paramref name="t"/> and how much of it shows, or none while it is home in its letter.</summary>
    public static (double X, double Y, double Alpha)? At(in Grain g, double t)
    {
        double s = Clock(t) - g.Leaves;
        if (s <= 0) return null;
        double r = s - Wind * (1 - Math.Exp(-s / Wind)), k = Smooth(s / 2);
        double x = g.X + g.Vx * r + g.Sway * Math.Sin(Tau * s / g.Period) * k;
        double y = g.Y + g.Vy * r + g.Sway * 0.6 * (1 - Math.Cos(Tau * s / (g.Period * 1.3))) * k;
        return (x, y, g.Alpha * Smooth(s / Appear) * (0.35 + 0.65 * Math.Exp(-s / Thinning)));
    }

    /// <summary>How warm a grain at (<paramref name="x"/>, <paramref name="y"/>) is, 0 cold to 1 the sun's colour: the sun's reach, a glint more.</summary>
    public static double Warmth(double x, double y, bool glint)
    {
        double near = Reach(x, y);
        return Math.Min(7, Math.Floor(near * 7) + (glint ? Math.Round(near * 9) : 0)) / 7;
    }

    /// <summary>How much of the sun's warm light reaches a point, by its soft falloff.</summary>
    public static double Reach(double x, double y)
    {
        double u = Math.Min(1, Math.Sqrt((x - Ink.SunX) * (x - Ink.SunX) + (y - Ink.SunY) * (y - Ink.SunY)) / Ink.ReachR);
        var p = Ink.Reach;
        for (int i = 1; i < p.Length; i++)
            if (u <= p[i].U) return p[i - 1].Light + (p[i].Light - p[i - 1].Light) * (u - p[i - 1].U) / (p[i].U - p[i - 1].U);
        return p[^1].Light;
    }

    private static double Smooth(double u)
    {
        u = Math.Clamp(u, 0, 1);
        return u * u * (3 - 2 * u);
    }
}
