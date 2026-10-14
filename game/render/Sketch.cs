using System;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A node drawn by a callback (a Pixi <c>Graphics</c>'s place): lines, boxes and dots a view works out
/// itself, redrawn on <see cref="CanvasItem.QueueRedraw"/>. Unlit unless told, so what explains the
/// ship reads in the dark.
/// </summary>
public partial class Sketch : Node2D
{
    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    /// <summary>What it draws, in pixels of the world.</summary>
    public Action<Sketch>? Paint { get; set; }

    public Sketch(int z, bool lit = false)
    {
        ZIndex = z;
        if (!lit) Material = Unlit;
    }

    public override void _Draw() => Paint?.Invoke(this);

    /// <summary>A smooth line through <paramref name="points"/>, in cells: through the middle of each link, bending at the points.</summary>
    public void Rope(System.Collections.Generic.IReadOnlyList<(double X, double Y)> points, Color colour, float width)
    {
        if (points.Count < 2) return;
        var line = new System.Collections.Generic.List<Vector2>(points.Count * 4);
        const float cell = ShipCamera.Cell;
        Vector2 At(int i) => new Vector2((float)points[i].X, (float)points[i].Y) * cell;
        var from = At(0);
        line.Add(from);
        for (int i = 1; i < points.Count - 1; i++)
        {
            var p = At(i);
            var to = (p + At(i + 1)) / 2;
            for (int s = 1; s <= 4; s++)
            {
                float t = s / 4f, u = 1 - t;
                line.Add(u * u * from + 2 * u * t * p + t * t * to);
            }
            from = to;
        }
        line.Add(At(points.Count - 1));
        DrawPolyline(line.ToArray(), colour, width, true);
    }
}
