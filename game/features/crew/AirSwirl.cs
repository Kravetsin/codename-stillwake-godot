using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Air coming back round a hero (0081), drawn only, each peer its own. A canister breathed raises a vortex of air round the
/// whole body (<see cref="Vortex"/>): a faint ring at the feet, then fine streaks and motes spiralling up round it from the
/// feet to above the head, fading out there, about a second in all, steered along their helix as the hero moves. Breathing
/// room air again puffs a few pale motes off the visor (<see cref="Puff"/>). All of it light, added over the world, so it
/// reads in the dark of space; a faint pale light round the body while it rises.
/// </summary>
public partial class AirSwirl : Node
{
    /// <summary>The pale cyan of the air: the ring and the light round the body.</summary>
    private static readonly (int, int, int) Air = (200, 240, 255);

    /// <summary>
    /// A vortex: the body it rises round, followed at <see cref="Fx.VortexFollow"/> a second so a turn or a step never
    /// jerks it; which way it turns; how long it has thrown, what it has thrown so far, and how many of its particles fly.
    /// </summary>
    private sealed class Throw(Func<(Vector2 Feet, Vector2 Up)> body, int way)
    {
        public readonly Func<(Vector2 Feet, Vector2 Up)> Body = body;
        public readonly int Way = way;
        public (Vector2 Feet, Vector2 Up) At = body();
        public double Ms;
        public int Streaks, Motes, Flying;
    }

    /// <summary>
    /// One particle on its helix: how high it starts, a share of the body; where round the body; its own clock, so one
    /// cleared with the rest on a change of world is let go all the same.
    /// </summary>
    private sealed class Spun(Glim glim, Throw thrown, double from, double angle)
    {
        public readonly Glim Glim = glim;
        public readonly Throw Throw = thrown;
        public readonly double From = from, Angle = angle;
        public double Ms;
    }

    private readonly List<Spun> _spun = [];
    private readonly List<Throw> _throws = [];
    /// <summary>Where its air spins: the hero's own layer, round its body (effects-on-their-layer).</summary>
    private static Glims Spinning => Stages.Of(Layers.Hero).Glims;

    /// <summary>A canister's air rising round the body that <paramref name="body"/> gives each frame: its feet and the way up.</summary>
    public void Vortex(Func<(Vector2 Feet, Vector2 Up)> body)
    {
        var t = new Throw(body, Spinning.Between(0, 1) < 0.5 ? -1 : 1);
        _throws.Add(t);
        var (feet, up) = t.At;
        var ring = feet + up * 1.5f;
        Stages.Of(Layers.Hero).Flares.Shock(ring.X, ring.Y, 3, Fx.VortexRing, 380, Air, 0.4 * Fx.Glow, 0.3, Fx.VortexRingSquash);
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
        for (int i = _throws.Count - 1; i >= 0; i--)
        {
            var t = _throws[i];
            t.Ms += ms;
            var (feet, up) = t.Body();
            float follow = (float)(1 - Math.Exp(-delta * Fx.VortexFollow));
            t.At = (t.At.Feet.Lerp(feet, follow), t.At.Up.Lerp(up, follow).Normalized());
            double due = Math.Min(1, t.Ms / Fx.VortexEmitMs);
            t.Streaks = Emit(t, Luminous.VortexStreak, t.Streaks, Fx.VortexStreaks, due);
            t.Motes = Emit(t, Luminous.VortexMote, t.Motes, Fx.VortexMotes, due);
            // A faint pale light round the body while the air rises, fading as it goes.
            var middle = t.At.Feet + t.At.Up * (float)(Rig.H / 2);
            double light = Fx.VortexLight * Math.Max(0, 1 - t.Ms / (Fx.VortexEmitMs + 500));
            if (light > 0) Stages.Of(Layers.Hero).Flares.Hold(t, middle.X, middle.Y, Air, light, 3, 0.5f, 0.7);
            if (due >= 1 && t.Flying == 0) _throws.RemoveAt(i);
        }
        // Steered before the effects layer moves them this frame: each heads for where its helix is when the frame ends.
        for (int i = _spun.Count - 1; i >= 0; i--)
        {
            var s = _spun[i];
            var g = s.Glim;
            s.Ms += ms;
            if (s.Ms >= g.LifeMs)
            {
                s.Throw.Flying--;
                _spun.RemoveAt(i);
                continue;
            }
            var to = On(s, s.Ms / g.LifeMs);
            (g.Vx, g.Vy) = ((to.X - g.X) / delta, (to.Y - g.Y) / delta);
        }
    }

    /// <summary>Throws the <paramref name="look"/>s due by <paramref name="due"/> of the throw, <paramref name="thrown"/> of <paramref name="all"/> so far.</summary>
    private int Emit(Throw t, Luminous look, int thrown, int all, double due)
    {
        int want = (int)Math.Round(all * Fx.Count * due);
        for (; thrown < want; thrown++)
        {
            // The first ones start at the feet, the later ones anywhere up to a third of the body: the column fills from below.
            double from = Spinning.Between(0, Fx.VortexStartShare * (0.3 + 0.7 * due)), angle = Spinning.Between(0, Math.Tau);
            var start = Place(t, from, angle, 0);
            if (Spinning.Add(look, start.X, start.Y, 0, 0) is not { } g) continue;
            _spun.Add(new Spun(g, t, from, angle));
            t.Flying++;
        }
        return thrown;
    }

    /// <summary>Where <paramref name="s"/> is <paramref name="q"/> of its life in: up its helix round the body as the vortex follows it.</summary>
    private static Vector2 On(Spun s, double q) => Place(s.Throw, s.From, s.Angle, Math.Clamp(q, 0, 1));

    /// <summary>
    /// A point of the helix round <paramref name="t"/>'s body: from <paramref name="from"/> of the body's height it rises,
    /// quickening, to above the head; it turns <see cref="Fx.VortexTurns"/> round it, narrowing a little; seen from the
    /// side, round is across and a little up and down.
    /// </summary>
    private static Vector2 Place(Throw t, double from, double angle, double q)
    {
        double height = from * Rig.H + (Rig.H * (1 - from) + Fx.VortexAbove) * q * (0.7 + 0.3 * q);
        double a = angle + t.Way * Fx.VortexTurns * Math.Tau * q, r = Fx.VortexRadius * (1 - 0.3 * q);
        var (feet, up) = t.At;
        var across = new Vector2(-up.Y, up.X);
        return feet + up * (float)(height + Math.Sin(a) * Fx.VortexTilt) + across * (float)(Math.Cos(a) * r);
    }
}
