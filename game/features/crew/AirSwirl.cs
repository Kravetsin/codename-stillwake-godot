using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Air coming back round a hero (0081), drawn only, each peer its own. A canister breathed flings its air out of the chest
/// (<see cref="Fling"/>, air-feedback as picked): a flash, then motes flying out to the sides and up, slowing and curling,
/// each leaving a trail of finer motes that hangs in the dark and fades, about a second in all. Breathing room air again
/// puffs a few pale motes off the visor (<see cref="Puff"/>). All of it light, added over the world, so it reads in space.
/// </summary>
public partial class AirSwirl : Node
{
    /// <summary>The pale cyan of the air: the ring and the light round the body.</summary>
    private static readonly (int, int, int) Air = (200, 240, 255);

    /// <summary>A mote flung out: how it curls, a turn a second; when it last left a trail; its own clock, so one cleared with the rest on a change of world is let go all the same.</summary>
    private sealed class Flung(Glim glim, double curl)
    {
        public readonly Glim Glim = glim;
        public readonly double Curl = curl;
        public double Ms, SinceTrail;
    }

    private readonly List<Flung> _flung = [];
    private readonly List<(double X, double Y, double Ms)> _flashes = [];
    /// <summary>Where its air flies: the hero's own layer, round its body (effects-on-their-layer).</summary>
    private static Stage Spun => Stages.Of(Layers.Hero);
    private static Glims Spinning => Spun.Glims;

    /// <summary>A canister's air flung out of the chest of the body <paramref name="body"/> gives: its feet and the way up.</summary>
    public void Fling(Func<(Vector2 Feet, Vector2 Up)> body)
    {
        var (feet, up) = body();
        var right = new Vector2(-up.Y, up.X);
        var chest = feet + up * (float)(Rig.H * Fx.FlingChest);
        _flashes.Add((chest.X, chest.Y, 0));
        Spun.Flares.Flash(chest.X, chest.Y, Air, 0.5 * Fx.Glow, 3, 0.5f, 0.18);
        int count = (int)Math.Round(Fx.FlingMotes * Fx.Count);
        for (int k = 0; k < count; k++)
        {
            // A few straight up, the rest to either side in turn, from a little below level to well up.
            double a;
            if (k < count / 6) a = -Math.PI / 2 + Spinning.Between(-0.45, 0.45);
            else
            {
                double rise = Spinning.Between(-0.35, 1.15);
                a = k % 2 == 1 ? -rise : Math.PI + rise;
            }
            double s = Spinning.Between(Fx.FlingSpeed.From, Fx.FlingSpeed.To);
            var v = right * (float)(Math.Cos(a) * s) - up * (float)(Math.Sin(a) * s);
            if (Spinning.Add(Luminous.FlingMote, chest.X + Spinning.Between(-1.5, 1.5), chest.Y + Spinning.Between(-2, 2), v.X, v.Y) is { } g)
                _flung.Add(new Flung(g, Spinning.Between(-Fx.FlingCurl, Fx.FlingCurl)));
        }
    }

    /// <summary>A breath of room air: a few pale motes drifting off the visor at <paramref name="visor"/>, mostly up.</summary>
    public void Puff(Vector2 visor)
    {
        for (int i = 0; i < Fx.VisorMotes; i++)
        {
            double a = -Math.PI / 2 + Spinning.Between(-1.1, 1.1), s = Spinning.Between(0.4, 1) * Fx.VisorDrift;
            Spinning.Add(Luminous.VisorMote, visor.X + Spinning.Between(-1.5, 1.5), visor.Y + Spinning.Between(-1, 1), Math.Cos(a) * s, Math.Sin(a) * s);
        }
    }

    /// <summary>
    /// Rammed (0103): the suit's air knocked out of it at <paramref name="chest"/>, jets and motes bursting toward the blow, the side
    /// <paramref name="from"/> says (+1 right), and up, round a faint ring.
    /// </summary>
    public void Leak(Vector2 chest, int from)
    {
        // A blow knocked it out: over the hero, as every blow on one (effects-on-their-layer).
        var glims = Stages.Of(Layers.Effects).Glims;
        for (int i = 0; i < Fx.LeakStreaks * Fx.Count; i++)
        {
            double a = (from > 0 ? 0 : Math.PI) - from * glims.Between(0.1, 1.2), s = glims.Between(60, 150);
            glims.Add(Luminous.SuitLeak, chest.X + from * 2, chest.Y + glims.Between(-2, 2), Math.Cos(a) * s, Math.Sin(a) * s);
        }
        for (int i = 0; i < Fx.LeakMotes * Fx.Count; i++)
        {
            double a = -Math.PI / 2 + from * glims.Between(0.2, 1.4), s = glims.Between(0.5, 1) * Fx.VisorDrift * 2;
            glims.Add(Luminous.VisorMote, chest.X + glims.Between(-2, 2), chest.Y + glims.Between(-2, 2), Math.Cos(a) * s, Math.Sin(a) * s);
        }
        Stages.Of(Layers.Effects).Flares.Shock(chest.X + from * 2, chest.Y, 2, Fx.LeakRing, 260, Air, 0.5 * Fx.Glow, 0.3);
    }

    public override void _Process(double delta)
    {
        double ms = delta * 1000;
        if (ms <= 0) return;
        for (int i = _flashes.Count - 1; i >= 0; i--)
        {
            var (x, y, age) = _flashes[i];
            double q = age / Fx.FlingFlashMs;
            if (q >= 1)
            {
                _flashes.RemoveAt(i);
                continue;
            }
            Spun.Flares.Bloom(x, y, 14, Air, 0.9 * (1 - q) * (1 - q) * Fx.Glow);
            Spun.Flares.Bloom(x, y, 2, (255, 255, 255), 1 - q);
            _flashes[i] = (x, y, age + ms);
        }
        // Turned before the effects layer moves them this frame: each curls as it slows, its look's drag slowing it.
        for (int i = _flung.Count - 1; i >= 0; i--)
        {
            var f = _flung[i];
            var g = f.Glim;
            f.Ms += ms;
            if (f.Ms >= g.LifeMs)
            {
                _flung.RemoveAt(i);
                continue;
            }
            double c = f.Curl * delta, cs = Math.Cos(c), sn = Math.Sin(c);
            (g.Vx, g.Vy) = (g.Vx * cs - g.Vy * sn, g.Vx * sn + g.Vy * cs);
            double life = 1 - f.Ms / g.LifeMs;
            Spun.Flares.Bloom(g.X, g.Y, 3.4, Air, 0.45 * life * Fx.Glow);
            for (f.SinceTrail += ms; f.SinceTrail >= Fx.FlingTrailMs; f.SinceTrail -= Fx.FlingTrailMs)
                Spinning.Add(Luminous.FlingTrail, g.X + Spinning.Between(-0.4, 0.4), g.Y + Spinning.Between(-0.4, 0.4),
                    g.Vx * 0.06 + Spinning.Between(-4, 4), g.Vy * 0.06 + Spinning.Between(-4, 4), bright: 0.5 + 0.5 * life);
        }
    }
}
