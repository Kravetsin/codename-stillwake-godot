using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The pings in the world (<see cref="Ping"/>), picked by the user 2026-11-16 ("ripple"): a small dot in its player's colour, rimmed
/// dark so it reads on hull and ground alike, and once a second a thin ring of its colour spreading from it and fading. Unlit: the
/// scene's light and shadow never dim it. No words.
/// </summary>
public partial class PingView : Node2D
{
    /// <summary>The dot's radius, its rim's radius and width, in the world's pixels (a cell 8).</summary>
    private const float Radius = 2, RimAt = 2.35f, RimWidth = 0.7f;

    /// <summary>The ring: from this radius to that one over <see cref="RingMs"/>, once every <see cref="EveryMs"/>; its line widths.</summary>
    private const float RingFrom = 2.6f, RingTo = 8, RingMs = 700, EveryMs = 1000, RingLine = 0.8f, RingBacking = 1.6f;

    private static readonly Color Dark = new(5 / 255f, 8 / 255f, 15 / 255f);

    /// <summary>One ping drawn: where, in the world's pixels; its colour, 0xRRGGBB; how much of it shows, 0 to 1; how long it has shown.</summary>
    public readonly record struct Dot(Vector2 At, int Colour, float Shown, float Seconds);

    private readonly List<Dot> _dots = [];

    public PingView() => Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    /// <summary>Draws <paramref name="dots"/> this frame in place of the last.</summary>
    public void Show(IReadOnlyList<Dot> dots)
    {
        if (dots.Count == 0 && _dots.Count == 0) return;
        _dots.Clear();
        _dots.AddRange(dots);
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var dot in _dots)
        {
            var colour = new Color((uint)dot.Colour << 8 | 0xff);
            float a = dot.Shown, ms = dot.Seconds * 1000 % EveryMs;
            if (ms < RingMs)
            {
                float u = ms / RingMs, eased = 1 - (1 - u) * (1 - u) * (1 - u), r = RingFrom + (RingTo - RingFrom) * eased, fade = a * (1 - u);
                DrawArc(dot.At, r, 0, Mathf.Tau, 40, Dark with { A = 0.55f * fade }, RingBacking, antialiased: true);
                DrawArc(dot.At, r, 0, Mathf.Tau, 40, colour with { A = fade }, RingLine, antialiased: true);
            }
            DrawCircle(dot.At, Radius, colour with { A = a }, antialiased: true);
            DrawArc(dot.At, RimAt, 0, Mathf.Tau, 24, Dark with { A = 0.85f * a }, RimWidth, antialiased: true);
        }
    }
}
