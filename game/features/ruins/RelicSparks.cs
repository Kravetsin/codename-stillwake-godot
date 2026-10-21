using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Ruins;

/// <summary>
/// A relic sparking (0095), the relic page's live emitter a step at a time: small arcs on twos from its cracks to its own metal or
/// into the air, now and then a reach to the room's metal, sparks dripping off it, and every few seconds a big discharge from its
/// heart into the room's metal that re-lights it (<see cref="Flash"/>), sparks raining. <see cref="Fade"/> thins it out once it is
/// taken. Drawn only, each peer its own: a spire's cradle and a home's niche alike.
/// </summary>
public sealed class RelicSparks
{
    private readonly Random _random;
    private readonly RelicArt.Sparking _art;
    private readonly bool _small;
    private readonly Vector2 _corner;
    private readonly IReadOnlyList<Vector2> _far;
    private readonly float _floor;
    private double _next;

    /// <param name="corner">Its piece's box corner, in pixels of the world.</param>
    /// <param name="far">The room's metal round it, in pixels of the world: its walls, a ladder, a cache, a cable.</param>
    /// <param name="floor">Where its sparks land, a row of pixels.</param>
    public RelicSparks(Random random, RelicArt.Sparking art, Vector2 corner, IReadOnlyList<Vector2> far, float floor, bool small)
    {
        (_random, _art, _corner, _far, _floor, _small) = (random, art, corner, far, floor, small);
        _next = Between(0.6, (small ? RelicLooks.SmallBig : RelicLooks.Big).From);
    }

    /// <summary>How much of its sparking is left: 1 alive, down to 0 as a taken one dies.</summary>
    public double Fade { get; set; } = 1;

    /// <summary>A big discharge's light, 1 as it strikes, easing off a step at a time.</summary>
    public double Flash { get; private set; }

    /// <summary>Its heart, in pixels of the world.</summary>
    public Vector2 Core => At(_art.Core);

    private Vector2 At((float X, float Y) p) => _corner + new Vector2(p.X, p.Y);
    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();
    private T Pick<T>(IReadOnlyList<T> of) => of[_random.Next(of.Count)];
    private Vector2 Jit(Vector2 p, double by) => p + new Vector2((float)Between(-by, by), (float)Between(-by, by));
    private static int Frames(double steps) => (int)Math.Round(steps * 2.5);

    private static readonly Luminous Spark = Luminous.HitSpark with
    {
        Ramp = [RelicLooks.Core, RelicLooks.Hot, RelicLooks.Mid, (117, 82, 224)],
        LifeMs = (500, 1300),
        Gravity = 240,
        Bounce = 0.3,
        Halo = 2,
    };

    private void Sparks(Vector2 at, int count, double up)
    {
        for (int i = 0; i < count; i++)
        {
            double a = Between(0, Math.Tau), s = Between(25, 80);
            Glims.Shared.Add(Spark, at.X, at.Y, Math.Cos(a) * s, Math.Sin(a) * s - up, _floor);
        }
    }

    /// <summary>One step of the page's 24 a second at <paramref name="seconds"/>: true when it discharged.</summary>
    public bool Step(double seconds)
    {
        Flash *= 0.74;
        if (Flash < 0.01) Flash = 0;
        if (Fade <= 0) return false;
        var src = _art.Sources;
        double rate = (_small ? RelicLooks.SmallRate : RelicLooks.Rate) * Fade;
        int arcs = _random.NextDouble() < rate ? (_random.NextDouble() < 0.35 ? 2 : 1) : 0;
        for (int a = 0; a < arcs; a++)
        {
            var from = At(Pick(src));
            if (_random.NextDouble() < 0.75)
            {
                var to = At(Pick(_art.Metal));
                var (f, t) = (Jit(from, 0.6), Jit(to, 0.7));
                Bolts.Shared.Add(() => (f.X, f.Y, t.X, t.Y, Between(-0.18, 0.18)), Frames(Between(2, 5)), 0.8, _small ? 0.45 : 0.6, tint: BoltTint.Violet);
            }
            else
            {
                double angle = Between(0, Math.Tau), reach = Between(4, 9) * (_small ? 0.7 : 1.5);
                var f = Jit(from, 0.4);
                var t = from + new Vector2((float)(Math.Cos(angle) * reach), (float)(Math.Sin(angle) * reach));
                Bolts.Shared.Add(() => (f.X, f.Y, t.X, t.Y, 0), Frames(Between(2, 3)), 0.6, 0.4, tint: BoltTint.Violet);
            }
            if (_random.NextDouble() < 0.4) Glims.Shared.Add(Spark, from.X, from.Y, Between(-25, 25), Between(-40, 0), _floor);
        }
        if (_far.Count > 0 && _random.NextDouble() < rate * RelicLooks.FarShare)
        {
            var (f, t) = (Jit(At(Pick(src)), 0.5), Jit(Pick(_far), 1));
            Bolts.Shared.Add(() => (f.X, f.Y, t.X, t.Y, Between(-0.15, 0.15)), Frames(Between(3, 5)), 1, 0.7, branchy: true, tint: BoltTint.Violet);
            Sparks(t, 3, 25);
        }
        if (_random.NextDouble() < (_small ? RelicLooks.SmallDrip : RelicLooks.Drip) * Fade)
        {
            var d = At(Pick(src));
            Glims.Shared.Add(Spark, d.X + Between(-1, 1), d.Y, Between(-8, 8), Between(-6, 10), _floor);
        }
        if (Fade < 1 || seconds < _next) return false;
        var big = _small ? RelicLooks.SmallBig : RelicLooks.Big;
        _next = seconds + Between(big.From, big.To);
        Discharge(_far.Count > 0 ? Pick(_far) : At(Pick(_art.Metal)));
        return true;
    }

    /// <summary>A big discharge from its heart into <paramref name="target"/>, three more to its metal round it, sparks raining.</summary>
    public void Discharge(Vector2 target)
    {
        var core = Core;
        var (c, t) = (Jit(core, 1), Jit(target, 1.2));
        Bolts.Shared.Add(() => (c.X, c.Y, t.X, t.Y, Between(-0.2, 0.2)), Frames(8), 1.4, _small ? 0.9 : 1.35, branchy: true, tint: BoltTint.Violet);
        for (int i = 0; i < 3; i++)
        {
            var n = _random.Next(_art.Metal.Length + _far.Count) is var k && k < _art.Metal.Length ? At(_art.Metal[k]) : _far[k - _art.Metal.Length];
            var (a, b) = (Jit(core, 1), Jit(n, 1));
            Bolts.Shared.Add(() => (a.X, a.Y, b.X, b.Y, Between(-0.15, 0.15)), Frames(Between(3, 6)), 1, 0.8, branchy: true, tint: BoltTint.Violet);
        }
        Flash = 1;
        Sparks(t, _small ? 8 : 16, 35);
        Sparks(core, 8, 10);
        Flares.Shared.Glint(t.X, t.Y, _small ? 4 : 6, RelicLooks.Hot);
    }

    /// <summary>An arc from its heart to a hero that struck it (the sim's shock), crackling.</summary>
    public void Zap(Vector2 hero)
    {
        var core = Core;
        Bolts.Shared.Add(() => (core.X, core.Y, hero.X, hero.Y, Between(-0.25, 0.25)), RelicLooks.ZapLife, 1.3, 1.1, branchy: true, tint: BoltTint.Violet);
        Sparks(hero, 10, 20);
        Flares.Shared.Glint(hero.X, hero.Y, 5, RelicLooks.Core);
        Flash = Math.Max(Flash, 0.6);
    }
}
