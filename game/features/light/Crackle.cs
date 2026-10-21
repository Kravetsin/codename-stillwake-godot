using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// The arc makers' arcs crackling (0091, the page's arc emitters, <see cref="Emits.Arc"/>): how often one strikes, and the arc it
/// strikes, cyan and branchy; for their homes' pieces and their lamp hung aboard alike.
/// </summary>
public static class Crackle
{
    /// <summary>The page's live layer steps at 24 a second: an emitter's rate is a chance a step.</summary>
    public const double StepsPerSecond = 24;

    /// <summary>The chance arc <paramref name="e"/> strikes in a step, its piece's <paramref name="ticks"/>th: its rate's, a beat's every sixth.</summary>
    public static double Chance(Emitter e, int ticks) => e.Beat ? (ticks % 6 == 0 ? 0.7 : 0) : (e.Rate == 0 ? 0.02 : e.Rate) * 1.8;

    /// <summary>An arc of <paramref name="e"/>'s from <paramref name="a"/> to <paramref name="b"/>, in pixels of the world, living its life.</summary>
    public static void Strike(Random random, Emitter e, Vector2 a, Vector2 b)
    {
        double Between(double from, double to) => from + (to - from) * random.NextDouble();
        int life = (int)Math.Round(e.Life * Between(1, 1.8) * 2.5);
        Bolts.Shared.Add(() => (a.X, a.Y, b.X, b.Y, Between(-0.15, 0.15)), life, 1, Math.Max(0.35, e.Width), branchy: true);
    }
}
