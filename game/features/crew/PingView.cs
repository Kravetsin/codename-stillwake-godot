using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The pings in the world (<see cref="Ping"/>): a soft dot of light in its player's colour, a bright core in a breathing halo, added
/// over what lies there, so it reads on dark hull and bright ground alike. No ring and no words.
/// </summary>
public partial class PingView : Node2D
{
    /// <summary>The halo's and the core's radius in the world's pixels (a cell 8), and how fast the halo breathes, in breaths a second.</summary>
    private const float Halo = 22, Core = 4.5f, Breaths = 1.1f;

    /// <summary>One ping drawn: where, in the world's pixels; its colour, 0xRRGGBB; how much of it shows, 0 to 1; how long it has shown.</summary>
    public readonly record struct Dot(Vector2 At, int Colour, float Shown, float Seconds);

    private readonly List<Dot> _dots = [];

    public PingView()
    {
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        TextureFilter = TextureFilterEnum.Linear;
    }

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
            float breath = 0.5f - 0.5f * Mathf.Cos(Mathf.Tau * Breaths * dot.Seconds);
            // Come in with a little swell, then breathe: the halo grows and dims as one.
            float swell = 1 + 0.6f * Mathf.Max(0, 1 - dot.Seconds / 0.4f);
            float halo = Halo * swell * (0.85f + 0.3f * breath);
            DrawTextureRect(SoftDots.Puff, new Rect2(dot.At - Vector2.One * halo, Vector2.One * 2 * halo), false, colour with { A = dot.Shown * (0.55f - 0.2f * breath) });
            DrawTextureRect(SoftDots.Dot, new Rect2(dot.At - Vector2.One * 2.5f * Core, Vector2.One * 5 * Core), false, colour.Lightened(0.35f) with { A = dot.Shown });
            DrawCircle(dot.At, Core * 0.45f, Colors.White with { A = dot.Shown * 0.9f });
        }
    }
}
