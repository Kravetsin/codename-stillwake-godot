using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Sim;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Shared;

/// <summary>
/// Shots in flight drawn (shots-are-the-sims): each a streak behind its point along its way (<see cref="ShotLooks"/>), handed in
/// every frame; the sim's by <c>ShotsView</c>, this player's own by its hero's hands, which fly them ahead of the sim's.
/// </summary>
public partial class ShotStreaks : Node2D
{
    private readonly List<(Vector2 At, Vector2 Way, ShotLook Look)> _streaks = [];

    /// <summary>The streaks this frame: each shot's point and way, in pixels of the world, and its look.</summary>
    public void Show(IEnumerable<(Vector2 At, Vector2 Way, ShotLook Look)> streaks)
    {
        _streaks.Clear();
        _streaks.AddRange(streaks);
        QueueRedraw();
    }

    /// <summary>The steps a streak fades over, tail to head.</summary>
    private const int Fade = 6;

    private readonly Vector2[] _points = new Vector2[Fade + 1];
    private readonly Color[] _colours = new Color[Fade + 1], _rim = new Color[Fade + 1];
    private static readonly Color Rim = new(0.07f, 0.08f, 0.11f, 0.7f);

    public ShotStreaks() => Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    public override void _Draw()
    {
        foreach (var (at, way, look) in _streaks)
        {
            var tail = at - way.Normalized() * (float)look.Length;
            var head = new Color(look.Head);
            for (int i = 0; i <= Fade; i++)
            {
                float k = (float)i / Fade;
                _points[i] = tail.Lerp(at, k);
                _colours[i] = head with { A = k * k };
            }
            float w = (float)look.Width;
            // A dark rim under it, so a bright streak reads on a pale sky as on a cave's dark.
            for (int i = 0; i <= Fade; i++) _rim[i] = Rim with { A = Rim.A * _colours[i].A };
            DrawPolylineColors(_points, _rim, w + 2);
            DrawRect(new Rect2(at - new Vector2(w + 2, w + 2) / 2, new Vector2(w + 2, w + 2)), Rim);
            DrawPolylineColors(_points, _colours, w);
            DrawRect(new Rect2(at - new Vector2(w, w) / 2, new Vector2(w, w)), head);
        }
    }

    /// <summary>A shot's point <paramref name="progress"/> of the way from <paramref name="was"/> to <paramref name="now"/>, and its way, in pixels of the world.</summary>
    public static (Vector2 At, Vector2 Way) Between(ShotMotion was, ShotMotion now, double progress)
    {
        float k = (float)(Camera.Cell / 1000.0);
        var from = new Vector2(was.X, was.Y) * k;
        var to = new Vector2(now.X, now.Y) * k;
        var way = to - from;
        return (from.Lerp(to, (float)progress), way == Vector2.Zero ? new Vector2(now.VX, now.VY) : way);
    }
}

