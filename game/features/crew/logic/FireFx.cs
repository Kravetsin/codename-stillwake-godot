using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using Pens = Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Crew;

/// <summary>«Пламя» (<see cref="FireLook"/>): tongues and embers on the blade at rest; a torn ribbon of fire and its cinders on a swing.</summary>
public sealed class FireFx(FireLook look) : BladeFx(look)
{
    private const int Ember = 0, Cinder = 1;
    private readonly List<(double X, double Y)> _was = [], _now = [], _shape = [];

    protected override void Throw(double elapsedMs)
    {
        if (Now is { Lip.Count: > 0 } held)
            for (int n = HowMany(look.RestEmbersPerS * elapsedMs / 1000); n > 0; n--)
            {
                var (at, o) = Outward(held, held.Lip[(int)(Random.NextDouble() * held.Lip.Count)]);
                Motes.Add(new Mote
                {
                    Kind = Ember,
                    X = at.X + o.X * 0.6,
                    Y = at.Y + o.Y * 0.6,
                    Vx = (Random.NextDouble() - 0.5) * 3,
                    Vy = -Between(look.EmberRise),
                    Ay = -2,
                    Drag = 0.99,
                    Life = Between(look.EmberMs),
                    Size = Between((0.3, 0.5)),
                    Phase = Random.NextDouble() * Math.Tau,
                });
            }
        if (!Swept(out var was, out var now, out double shown)) return;
        for (int n = HowMany(look.CindersPerPx * SweptPx(was, now) * shown); n > 0; n--)
        {
            var (at, v) = InBand(was, now, elapsedMs);
            Motes.Add(new Mote
            {
                Kind = Cinder,
                X = at.X,
                Y = at.Y,
                Vx = v.X * look.CinderCarry + (Random.NextDouble() - 0.5) * 10,
                Vy = v.Y * look.CinderCarry + (Random.NextDouble() - 0.5) * 10 - 3,
                Ay = look.CinderFall,
                Drag = look.CinderDrag,
                Life = Between(look.CinderMs),
                Size = Between((0.3, 0.55)),
                Phase = Random.NextDouble() * Math.Tau,
            });
        }
    }

    public override void Draw(Ink under, Ink dark, Ink over, Func<(double X, double Y), (double X, double Y)> local)
    {
        Ribbon(over, local);
        if (Now is { Lip.Count: > 0 } held) Tongues(over, local, held);
        foreach (var m in Motes)
        {
            double a = m.Age / m.Life;
            var at = local((m.X + Math.Sin(m.Age * 0.008 + m.Phase) * 0.35, m.Y));
            Disc(over, at, Math.Max(0.12, m.Size * (1 - 0.45 * a)), Tone(Mix(look.Ember, look.Deep, a), Math.Pow(1 - a, 0.8) * (m.Kind == Cinder ? 0.95 : 0.8)));
        }
    }

    // The ribbon: each stretch lifted as it ages, ragged point by point, torn away in pieces, its youngest part a hotter core.
    private void Ribbon(Ink ink, Func<(double X, double Y), (double X, double Y)> local)
    {
        var samples = Trail.Samples;
        for (int i = 1; i < samples.Count; i++)
        {
            var (was, now) = (samples[i - 1], samples[i]);
            if (now.Shown <= 0) continue;
            double left = Trail.Left(now), age = 1 - left;
            if (Pens.Hash(now.Id, 99) < look.Tear * age) continue;
            double a = look.RibbonAlpha * now.Shown * left;
            Lifted(was, local, _was);
            Lifted(now, local, _now);
            ink.Band(_was, _now, Tone(Mix(look.Flame, look.Deep, Math.Clamp(age * 1.4, 0, 1)), a));
            if (left > 0.45) ink.Band(_was.GetRange(1, 5), _now.GetRange(1, 5), Tone(look.Heart, a * 0.7 * (left - 0.45) / 0.55));
        }
    }

    private void Lifted(BladeTrail.Sample sample, Func<(double X, double Y), (double X, double Y)> local, List<(double X, double Y)> into)
    {
        double age = 1 - Trail.Left(sample);
        into.Clear();
        for (int j = 0; j < sample.Edge.Length; j++)
            into.Add(local((sample.Edge[j].X, sample.Edge[j].Y - look.RibbonRisePx * age * (0.4 + 1.2 * Pens.Hash(sample.Id, j)))));
    }

    // The tongues at rest, always: a few fixed places along the lip, each licking up at its own beat.
    private void Tongues(Ink ink, Func<(double X, double Y), (double X, double Y)> local, Held held)
    {
        double t = ClockMs / 1000;
        for (int q = 0; q < look.Tongues; q++)
        {
            var (root, o) = Outward(held, held.Lip[Math.Min(held.Lip.Count - 1, (int)((q + 0.5) / look.Tongues * held.Lip.Count))]);
            double hz = look.TongueHz.From + (look.TongueHz.To - look.TongueHz.From) * Pens.Hash(q, 3), ph = Pens.Hash(q, 4) * Math.Tau;
            double flick = 0.5 + 0.3 * Math.Sin(t * hz * Math.Tau + ph) + 0.2 * Math.Sin(t * hz * 2.7 * Math.Tau + ph * 1.7);
            double h = look.TonguePx.From + (look.TonguePx.To - look.TonguePx.From) * flick;
            var up = Unit(o.X * 0.4, o.Y * 0.4 - 1);
            var side = (X: -up.Y, Y: up.X);
            double sway = Math.Sin(t * hz * 1.3 * Math.Tau + ph) * 0.45, half = look.TongueWidePx / 2;
            Tongue(ink, local, root, up, side, sway, half, h, Tone(look.Flame, look.TongueAlpha));
            Tongue(ink, local, root, up, side, sway * 0.5, half * 0.5, h * 0.5, Tone(look.Heart, look.TongueAlpha * 0.9));
            Soft(ink, local(root), 1.6, Tone(look.Flame, 0.1));
        }
    }

    // A tongue: from either side of its root, curving in to its swaying tip.
    private void Tongue(Ink ink, Func<(double X, double Y), (double X, double Y)> local, (double X, double Y) root, (double X, double Y) up,
        (double X, double Y) side, double sway, double half, double h, Ink.Tone tone)
    {
        (double X, double Y) tip = (root.X + up.X * h + side.X * sway, root.Y + up.Y * h + side.Y * sway);
        (double X, double Y) a = (root.X - side.X * half, root.Y - side.Y * half), b = (root.X + side.X * half, root.Y + side.Y * half);
        (double X, double Y) ca = (root.X + up.X * h * 0.45 - side.X * half * 0.9, root.Y + up.Y * h * 0.45 - side.Y * half * 0.9);
        (double X, double Y) cb = (root.X + up.X * h * 0.45 + side.X * half * 0.9, root.Y + up.Y * h * 0.45 + side.Y * half * 0.9);
        _shape.Clear();
        for (int k = 0; k <= 5; k++) _shape.Add(local(Curve(a, ca, tip, k / 5.0)));
        for (int k = 1; k <= 5; k++) _shape.Add(local(Curve(tip, cb, b, k / 5.0)));
        ink.Fill(_shape, local((root.X + up.X * h * 0.3, root.Y + up.Y * h * 0.3)), tone);
    }

    private static (double X, double Y) Curve((double X, double Y) a, (double X, double Y) c, (double X, double Y) b, double t) =>
        ((1 - t) * (1 - t) * a.X + 2 * (1 - t) * t * c.X + t * t * b.X, (1 - t) * (1 - t) * a.Y + 2 * (1 - t) * t * c.Y + t * t * b.Y);
}
