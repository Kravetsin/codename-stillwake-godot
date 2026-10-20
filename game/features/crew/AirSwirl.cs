using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Air coming back round a hero (0081), drawn only, each peer its own: a canister breathed swirls fine streaks, motes and
/// translucent wisps in to its visor (<see cref="Swirl"/>), steered along a spiral that closes on the visor as each one's
/// life ends, following the head as the hero moves; breathing room air again puffs a few pale motes off it (<see cref="Puff"/>).
/// The particles are the shared luminous ones (0075), so the effects layer draws them.
/// </summary>
public partial class AirSwirl : Node
{
    /// <summary>
    /// One particle on its way in: where on the ring it started, how far out, which way it turns; its own clock, so one
    /// cleared with the rest on a change of world is let go all the same.
    /// </summary>
    private sealed class Spun(Glim glim, Throw thrown, double angle, double radius)
    {
        public readonly Glim Glim = glim;
        public readonly Throw Throw = thrown;
        public readonly double Angle = angle, Radius = radius;
        public double Ms;
    }

    /// <summary>
    /// A swirl: the visor it swirls to, followed at <see cref="Fx.SwirlFollow"/> a second so a head turned does not jerk
    /// its air into long lines; how long it has thrown, what it has thrown so far, and how many of its particles fly.
    /// </summary>
    private sealed class Throw(Func<Vector2> visor, int way)
    {
        public readonly Func<Vector2> Visor = visor;
        public readonly int Way = way;
        public Vector2 At = visor();
        public double Ms;
        public int Streaks, Motes, Wisps, Flying;
    }

    private readonly List<Spun> _spun = [];
    private readonly List<Throw> _throws = [];
    private readonly Glims _glims = Glims.Shared;

    /// <summary>A canister's air swirling in to the visor that <paramref name="visor"/> gives each frame.</summary>
    public void Swirl(Func<Vector2> visor) => _throws.Add(new Throw(visor, _glims.Between(0, 1) < 0.5 ? -1 : 1));

    /// <summary>A breath of room air: a few pale motes drifting off the visor at <paramref name="visor"/>, mostly up.</summary>
    public void Puff(Vector2 visor)
    {
        for (int i = 0; i < Fx.VisorMotes; i++)
        {
            double a = -Math.PI / 2 + _glims.Between(-1.1, 1.1), s = _glims.Between(0.4, 1) * Fx.VisorDrift;
            _glims.Add(Luminous.VisorMote, visor.X + _glims.Between(-1.5, 1.5), visor.Y + _glims.Between(-1, 1), Math.Cos(a) * s, Math.Sin(a) * s);
        }
    }

    public override void _Process(double delta)
    {
        double ms = delta * 1000;
        if (ms <= 0) return;
        for (int i = _throws.Count - 1; i >= 0; i--)
        {
            var t = _throws[i];
            t.Ms += ms;
            t.At = t.At.Lerp(t.Visor(), (float)(1 - Math.Exp(-delta * Fx.SwirlFollow)));
            double due = Math.Min(1, t.Ms / Fx.SwirlEmitMs);
            t.Streaks = Emit(t, Luminous.AirStreak, t.Streaks, Fx.SwirlStreaks, due);
            t.Motes = Emit(t, Luminous.AirMote, t.Motes, Fx.SwirlMotes, due);
            t.Wisps = Emit(t, Luminous.AirWisp, t.Wisps, Fx.SwirlWisps, due);
            if (due >= 1 && t.Flying == 0) _throws.RemoveAt(i);
        }
        // Steered before the effects layer moves them this frame: each heads for where its spiral is when the frame ends.
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
            double angle = _glims.Between(0, Math.Tau), radius = _glims.Between(Fx.SwirlFrom, Fx.SwirlTo);
            double x = t.At.X + Math.Cos(angle) * radius, y = t.At.Y + Math.Sin(angle) * radius * Fx.SwirlSquash;
            if (_glims.Add(look, x, y, 0, 0) is not { } g) continue;
            _spun.Add(new Spun(g, t, angle, radius));
            t.Flying++;
        }
        return thrown;
    }

    /// <summary>
    /// Where <paramref name="s"/> is <paramref name="q"/> of its life in: on a spiral round its swirl's middle, its
    /// radius closing to nothing, turning faster as it closes.
    /// </summary>
    private static Vector2 On(Spun s, double q)
    {
        q = Math.Clamp(q, 0, 1);
        double r = s.Radius * Math.Pow(1 - q, 1.25), a = s.Angle + s.Throw.Way * Fx.SwirlTurns * Math.Tau * q * q * (3 - 2 * q);
        var visor = s.Throw.At;
        return new Vector2((float)(visor.X + Math.Cos(a) * r), (float)(visor.Y + Math.Sin(a) * r * Fx.SwirlSquash));
    }
}
