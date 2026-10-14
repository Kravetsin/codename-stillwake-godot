using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Furnace;

/// <summary>
/// Embers rising from a fire (EFFECTS.ember of render/particles.ts): specks that drift up, slow, flicker
/// as they fade and glow, a soft halo added under each. Drawn only, never part of the world; in pixels
/// of the world and milliseconds, as TypeScript's particles.
/// </summary>
public partial class Embers : Node2D
{
    /// <summary>At most this many fly at once (LIMIT of particles.ts).</summary>
    private const int Limit = 2000;
    private static readonly Color Colour = new(0xffe0a0ff);
    private static readonly Color GlowColour = new(0xff7a20ff);
    private const float GlowSize = 7;
    private const float MinSize = 1, MaxSize = 1.6f;
    private const float MinLifeMs = 1200, MaxLifeMs = 2200;
    private const float MinSpeed = 6, MaxSpeed = 18;
    /// <summary>The angle around straight up an ember may leave at, in radians.</summary>
    private const float Spread = 0.9f;
    /// <summary>Pulls an ember down, up when negative, in pixels a second a second.</summary>
    private const float Gravity = -8;
    /// <summary>The share of its speed an ember loses a second.</summary>
    private const float Drag = 0.4f;

    private struct Ember
    {
        public Vector2 At, Speed;
        public float AgeMs, LifeMs, Size;
    }

    private readonly List<Ember> _live = new();
    private readonly Random _random = new();

    public Embers()
    {
        Material = new CanvasItemMaterial
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
        };
    }

    public int Count => _live.Count;

    private float Between(float from, float to) => from + (to - from) * _random.NextSingle();

    /// <summary>One ember at <paramref name="at"/>, heading up; past the limit, none.</summary>
    public void Emit(Vector2 at)
    {
        if (_live.Count >= Limit) return;
        float angle = -Mathf.Pi / 2 + (_random.NextSingle() - 0.5f) * Spread;
        float speed = Between(MinSpeed, MaxSpeed);
        _live.Add(new Ember
        {
            At = at,
            Speed = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
            LifeMs = Between(MinLifeMs, MaxLifeMs),
            Size = Between(MinSize, MaxSize),
        });
    }

    public override void _Process(double delta)
    {
        float elapsedMs = (float)delta * 1000, dt = (float)delta;
        for (int i = _live.Count - 1; i >= 0; i--)
        {
            var e = _live[i];
            e.AgeMs += elapsedMs;
            if (e.AgeMs >= e.LifeMs)
            {
                // Order does not matter: the last takes the place of the one gone.
                _live[i] = _live[^1];
                _live.RemoveAt(_live.Count - 1);
                continue;
            }
            float keep = Math.Max(0, 1 - Drag * dt);
            e.Speed = new Vector2(e.Speed.X * keep, e.Speed.Y * keep + Gravity * dt);
            e.At += e.Speed * dt;
            _live[i] = e;
        }
        QueueRedraw();
    }

    /// <summary>How opaque an ember is now: fading out over its life, flickering as it goes (alphaOf).</summary>
    private static float AlphaOf(Ember e) =>
        (1 - e.AgeMs / e.LifeMs) * (0.65f + 0.35f * Mathf.Sin(e.AgeMs / 37 + e.LifeMs));

    public override void _Draw()
    {
        // Halos under the specks, as the effects layer added them.
        foreach (var e in _live)
        {
            var halo = new Rect2(e.At - Vector2.One * GlowSize / 2, Vector2.One * GlowSize);
            DrawTextureRect(PixelArt.Glow, halo, false, GlowColour with { A = AlphaOf(e) * 0.8f });
        }
        foreach (var e in _live)
            DrawRect(new Rect2(e.At - Vector2.One * e.Size / 2, Vector2.One * e.Size), Colour with { A = AlphaOf(e) });
    }
}
