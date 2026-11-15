using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using static Stillwake.Game.Features.Crew.Retching;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What a bare hero throws up (food-tuning, bile-slide): drops of bile out of the mouth, in world pixels, stretched along their
/// flight and drawn as one liquid with a dark tide line. On a deck they fall and lie in puddles that spread, some splashing,
/// and dry after <see cref="MarkMs"/>; afloat they drift, wobbling, braked by the air, and fade after <see cref="DriftMs"/>.
/// A drop lands where the feet stood as it left the mouth: a floor that falls away under it is not looked for.
/// </summary>
public sealed class RetchSpray
{
    private sealed class Drop
    {
        public double X, Y, Vx, Vy, R, Age, Seed, Floor;
        public bool Splash;
    }

    private readonly record struct Mark(double X, double Y, double R, double Seed, double Age);

    /// <summary>How long a puddle lies, its last second drying away; how long a drop drifts afloat, its last 0.6 s fading.</summary>
    public const double MarkMs = 9000, DriftMs = 4000;

    private const double Gravity = 220, DeckDrag = 0.9, FloatDrag = 1.6;

    private readonly List<Drop> _drops = [];
    private readonly List<Mark> _marks = [];
    private readonly Ink.Draw.Rng _rng = new(1234);
    private double _due;

    /// <summary>Whether nothing is left to draw.</summary>
    public bool Empty => _drops.Count == 0 && _marks.Count == 0;

    /// <summary>
    /// <paramref name="dt"/> seconds on: drops thrown at <paramref name="gush"/> (<see cref="Retching.Gush"/>) from
    /// <paramref name="mouth"/> along <paramref name="toward"/>, a unit vector, onto a deck at <paramref name="floor"/> or
    /// <paramref name="afloat"/>; those in flight moved, those landing turned to puddles.
    /// </summary>
    public void Step(double dt, double gush, (double X, double Y) mouth, (double X, double Y) toward, bool afloat, double floor)
    {
        _due += gush * dt * (afloat ? 70 : 150);
        while (_due >= 1)
        {
            _due--;
            double a = (_rng.Next() - 0.5) * 2 * (afloat ? 0.22 : 0.13), speed = afloat ? 26 + 24 * _rng.Next() : 38 + 22 * _rng.Next();
            double dx = toward.X * Math.Cos(a) - toward.Y * Math.Sin(a), dy = toward.X * Math.Sin(a) + toward.Y * Math.Cos(a);
            _drops.Add(new Drop
            {
                X = mouth.X + (_rng.Next() - 0.5) * 0.6,
                Y = mouth.Y + (_rng.Next() - 0.5) * 0.6,
                Vx = dx * speed,
                Vy = dy * speed,
                R = afloat ? 0.55 + _rng.Next() * 0.9 : 0.32 + _rng.Next() * 0.42,
                Seed = _rng.Next() * 1000,
                Floor = afloat ? double.NaN : floor,
            });
        }
        double brake = Math.Exp(-(afloat ? FloatDrag : DeckDrag) * dt);
        for (int i = _drops.Count - 1; i >= 0; i--)
        {
            var p = _drops[i];
            if (!double.IsNaN(p.Floor)) p.Vy += Gravity * dt;
            p.Vx *= brake;
            p.Vy *= brake;
            p.X += p.Vx * dt;
            p.Y += p.Vy * dt;
            p.Age += dt;
            bool landed = !double.IsNaN(p.Floor) && p.Y >= p.Floor - 0.1 && p.Vy > 0;
            if (landed) Land(p);
            if (landed || p.Age * 1000 >= DriftMs) _drops.RemoveAt(i);
        }
        for (int i = _marks.Count - 1; i >= 0; i--)
        {
            var m = _marks[i] with { Age = _marks[i].Age + dt };
            if (m.Age * 1000 >= MarkMs) _marks.RemoveAt(i);
            else _marks[i] = m;
        }
    }

    private void Land(Drop p)
    {
        if (p.Splash) return;
        _marks.Add(new Mark(p.X, p.Floor, 0.7 + p.R * 1.6, p.Seed, 0));
        if (_rng.Next() >= 0.35) return;
        for (int i = 0; i < 2; i++)
            _drops.Add(new Drop
            {
                X = p.X,
                Y = p.Floor - 0.2,
                Vx = (_rng.Next() - 0.3) * 26,
                Vy = -18 - _rng.Next() * 22,
                R = 0.22 + _rng.Next() * 0.14,
                Seed = _rng.Next() * 1000,
                Floor = p.Floor,
                Splash = true,
            });
    }

    /// <summary>The puddles, behind the figure.</summary>
    public void DrawFloor(Ink ink)
    {
        foreach (var (tone, g) in new[] { (0.95, 0.0), (0.82, -0.32) })
            foreach (var m in _marks)
            {
                double dry = 1 - Smooth(MarkMs / 1000 - 1, MarkMs / 1000, m.Age);
                var colour = g == 0 ? Of(Retching.Rim, tone * dry) : Of(Retching.Fill, tone * dry);
                Blob(ink, m.X, m.Y + 0.3, m.R * (1 + 1.6 * Smooth(0, 0.5, m.Age)) + g, 0.62 + g * 0.5, 0, m.Seed, 0.15, 7, colour);
            }
    }

    /// <summary>The drops in the air, before it.</summary>
    public void DrawAir(Ink ink)
    {
        foreach (var (tone, g) in new[] { (0.95, 0.0), (0.85, -0.24) })
            foreach (var p in _drops)
            {
                double fade = 1 - Smooth(DriftMs / 1000 - 0.6, DriftMs / 1000, p.Age);
                var colour = g == 0 ? Of(Retching.Rim, tone * fade) : Of(Retching.Fill, tone * fade);
                double turn = Math.Atan2(p.Vy, p.Vx);
                if (double.IsNaN(p.Floor))
                {
                    double w = 0.2 * Math.Sin(p.Age * 9 + p.Seed) * Math.Exp(-p.Age * 0.6);
                    Blob(ink, p.X, p.Y, p.R * (1 + w) + g, p.R * (1 - w) + g, turn, p.Seed, 0.12, 7, colour);
                }
                else Blob(ink, p.X, p.Y, p.R + Math.Sqrt(p.Vx * p.Vx + p.Vy * p.Vy) * 0.022 + g, p.R + g, turn, p.Seed, 0.12, 6, colour);
            }
        foreach (var p in _drops)
            if (p.R > 0.5)
            {
                double fade = 1 - Smooth(DriftMs / 1000 - 0.6, DriftMs / 1000, p.Age);
                Blob(ink, p.X - p.R * 0.3, p.Y - p.R * 0.35, p.R * 0.22, p.R * 0.22, 0, p.Seed, 0, 8, Of(Retching.Shine, 0.7 * fade));
            }
    }
}
