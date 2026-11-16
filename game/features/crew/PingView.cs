using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The pings in the world (<see cref="Ping"/>): a small dot in its player's colour, rimmed dark so it reads on hull and ground alike,
/// blinking gently. Unlit: the scene's light and shadow never dim it. No ring and no words.
/// </summary>
public partial class PingView : Node2D
{
    /// <summary>The dot's radius and its dark rim's width in the world's pixels (a cell 8), and its blinks a second.</summary>
    private const float Radius = 3.5f, Rim = 1, Blinks = 1.5f;

    /// <summary>How faint the dot gets at the bottom of a blink, 0 to 1.</summary>
    private const float Faintest = 0.45f;

    private static readonly Color RimColour = new(0.02f, 0.03f, 0.06f, 0.85f);

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
            float blink = 1 - (1 - Faintest) * (0.5f - 0.5f * Mathf.Cos(Mathf.Tau * Blinks * dot.Seconds));
            float a = dot.Shown * blink;
            DrawCircle(dot.At, Radius + Rim, RimColour with { A = RimColour.A * a }, antialiased: true);
            DrawCircle(dot.At, Radius, new Color((uint)dot.Colour << 8 | 0xff) with { A = a }, antialiased: true);
        }
    }
}
