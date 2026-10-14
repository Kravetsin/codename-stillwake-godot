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
        // A rope of so many points is always traced into as many: one array for each count, filled anew each frame.
        int count = 2 + (points.Count - 2) * 4;
        if (!Traces.TryGetValue(count, out var line)) Traces[count] = line = new Vector2[count];
        const float cell = ShipCamera.Cell;
        Vector2 At(int i) => new Vector2((float)points[i].X, (float)points[i].Y) * cell;
        var from = At(0);
        int n = 0;
        line[n++] = from;
        for (int i = 1; i < points.Count - 1; i++)
        {
            var p = At(i);
            var to = (p + At(i + 1)) / 2;
            for (int s = 1; s <= 4; s++)
            {
                float t = s / 4f, u = 1 - t;
                line[n++] = u * u * from + 2 * u * t * p + t * t * to;
            }
            from = to;
        }
        line[n] = At(points.Count - 1);
        DrawPolyline(line, colour, width, true);
    }

    private static readonly System.Collections.Generic.Dictionary<int, Vector2[]> Traces = new();
}
