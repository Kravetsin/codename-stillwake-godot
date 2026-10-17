using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// How one kind of particle looks and moves, in pixels of the world and milliseconds (render/particles.ts):
/// a square of <see cref="Colour"/> that fades out over its life, with a soft glow around it if it has
/// one, which also lights what is around (0019). <see cref="Spread"/> is the angle around its heading it
/// may leave at; <see cref="Gravity"/> pulls it down (up when negative) aboard and never in space;
/// <see cref="Drag"/> is the share of its speed it loses a second. Colours are 0xRRGGBB.
/// </summary>
public sealed record Effect(
    int Colour, double Alpha, (double From, double To) Size, (double From, double To) LifeMs,
    (double From, double To) Speed, double Spread, double Gravity, double Drag,
    (int Colour, double Size)? Glow = null, bool Flicker = false)
{
    // Every effect, by what makes it (EFFECTS). Headings are radians, screen-wise: 0 right, -π/2 up.

    /// <summary>Kicked up by a boot on the floor, settling again.</summary>
    public static readonly Effect Dust = new(0xd4dae2, 0.8, (1.5, 3), (450, 900), (14, 36), 1.2, 18, 2.5);
    /// <summary>Flying off work on a plan, falling in an arc aboard.</summary>
    public static readonly Effect Spark = new(0xfff3c4, 1, (1, 1.4), (250, 550), (40, 110), 2.2, 260, 1, Glow: (0xffa040, 6));
    /// <summary>Rising from a furnace at work, drifting and flickering out.</summary>
    public static readonly Effect Ember = new(0xffe0a0, 1, (1, 1.6), (1200, 2200), (6, 18), 0.9, -8, 0.4, Glow: (0xff7a20, 7), Flicker: true);
    /// <summary>Air streaming out of a room through its openings into space.</summary>
    public static readonly Effect Air = new(0xcfeaff, 0.7, (1.5, 2.5), (500, 1000), (40, 90), 0.8, 0, 0.5);
    /// <summary>The harpoon's puff of air from its muzzle as it fires (0044), aboard: braked by the air, gone in a third of a second.</summary>
    public static readonly Effect Puff = new(0xc8ced6, 0.85, (1, 2), (220, 360), (30, 75), 0.7, 0, 6);
    /// <summary>The same puff out in space: nothing brakes it, so it spreads wider and drifts on a little longer.</summary>
    public static readonly Effect PuffInSpace = Puff with { LifeMs = (380, 620), Spread = 1.1, Drag = 2.2 };
}

/// <summary>One particle in flight.</summary>
public sealed class Particle
{
    public double X, Y, Vx, Vy, AgeMs;
    public required double LifeMs { get; init; }
    public required double Size { get; init; }
    public required double Gravity { get; init; }
    public required Effect Effect { get; init; }

    /// <summary>How opaque it is now: fading out over its life, an ember flickering as it goes (alphaOf).</summary>
    public double Alpha
    {
        get
        {
            double left = 1 - AgeMs / LifeMs;
            double flicker = Effect.Flicker ? 0.65 + 0.35 * Math.Sin(AgeMs / 37 + LifeMs) : 1;
            return Effect.Alpha * left * flicker;
        }
    }
}

/// <summary>
/// Particles: emitted in bursts, moved and aged each frame, gone at the end of their life
/// (createParticles of render/particles.ts). Purely drawn, never part of the world: each peer emits its
/// own, from what it sees. <paramref name="random"/> draws from 0 to 1; tests pass their own.
/// </summary>
public sealed class Particles(int limit, Func<double>? random = null)
{
    /// <summary>At most this many particles fly at once: a big breach thins out rather than costing frames.</summary>
    public const int Limit = 2000;

    /// <summary>The game's particles, one set as TypeScript's one per page: features emit into it, the effects layer moves and draws it.</summary>
    public static Particles Shared { get; } = new(Limit);

    private readonly Func<double> _random = random ?? Random.Shared.NextDouble;
    private readonly List<Particle> _live = [];

    public IReadOnlyList<Particle> Live => _live;

    private double Draw((double From, double To) range) => range.From + (range.To - range.From) * _random();

    /// <summary>
    /// <paramref name="count"/> particles of <paramref name="effect"/> at (<paramref name="x"/>, <paramref name="y"/>),
    /// heading around <paramref name="heading"/>; past the limit, no more. In space nothing pulls them.
    /// </summary>
    public void Emit(Effect effect, double x, double y, int count, double heading = -Math.PI / 2, bool inSpace = false)
    {
        for (int i = 0; i < count && _live.Count < limit; i++)
        {
            double angle = heading + (_random() - 0.5) * effect.Spread;
            double speed = Draw(effect.Speed);
            _live.Add(new Particle
            {
                X = x,
                Y = y,
                Vx = Math.Cos(angle) * speed,
                Vy = Math.Sin(angle) * speed,
                LifeMs = Draw(effect.LifeMs),
                Size = Draw(effect.Size),
                Gravity = inSpace ? 0 : effect.Gravity,
                Effect = effect,
            });
        }
    }

    /// <summary>Moves every particle on by <paramref name="elapsedMs"/> and lets go of those past their life.</summary>
    public void Step(double elapsedMs)
    {
        double dt = elapsedMs / 1000;
        for (int i = _live.Count - 1; i >= 0; i--)
        {
            var p = _live[i];
            p.AgeMs += elapsedMs;
            if (p.AgeMs >= p.LifeMs)
            {
                // Order does not matter: the last takes the place of the one gone.
                _live[i] = _live[^1];
                _live.RemoveAt(_live.Count - 1);
                continue;
            }
            double keep = Math.Max(0, 1 - p.Effect.Drag * dt);
            p.Vx *= keep;
            p.Vy = p.Vy * keep + p.Gravity * dt;
            p.X += p.Vx * dt;
            p.Y += p.Vy * dt;
        }
    }

    public void Clear() => _live.Clear();
}
