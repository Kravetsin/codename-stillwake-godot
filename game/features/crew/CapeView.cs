using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A side of a cape on a figure (a prototype, <see cref="Cape"/>): its sheet drawn as one mesh of triangles. The outside
/// shows its picture, and its lining where a triangle has turned over; the inside, the same sheet wrapped round the far
/// side of the body, shows the lining all over. A child of the figure's frame, so its points are drawn where the cape
/// gives them; lit as the figure is.
/// </summary>
public partial class CapeView(Cape cape, bool inside) : Node2D
{
    private static readonly Texture2D Outside = GD.Load<Texture2D>("res://art/hero_cape.png"), Lining = GD.Load<Texture2D>("res://art/hero_cape_lining.png");
    private static readonly Vector2[] Uvs = MakeUvs();
    private static readonly Color[] Colours = [.. Uvs.Select(_ => Colors.White)];
    private readonly Vector2[] _points = new Vector2[Cape.Cols * Cape.Rows];
    private readonly List<int> _front = [], _back = [];

    /// <summary>Takes the cape's points as it last moved, to draw them.</summary>
    public void Refresh()
    {
        var points = inside ? cape.Inside : cape.Points;
        for (int i = 0; i < points.Length; i++) _points[i] = new Vector2((float)points[i].X, (float)points[i].Y);
        QueueRedraw();
    }

    public override void _Draw()
    {
        _front.Clear();
        _back.Clear();
        for (int t = 0; t < Cape.Triangles.Length / 3; t++)
        {
            var into = !inside && cape.Flipped(t) ? _back : _front;
            for (int k = 0; k < 3; k++) into.Add(Cape.Triangles[t * 3 + k]);
        }
        // The lining first: where the sheet folds over itself its outside lies over it.
        if (_back.Count > 0) RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), [.. _back], _points, Colours, Uvs, texture: Lining.GetRid());
        if (_front.Count > 0) RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), [.. _front], _points, Colours, Uvs, texture: (inside ? Lining : Outside).GetRid());
    }

    private static Vector2[] MakeUvs()
    {
        var uvs = new Vector2[Cape.Cols * Cape.Rows];
        for (int i = 0; i < uvs.Length; i++)
        {
            var (u, v) = Cape.Uv(i);
            uvs[i] = new Vector2((float)u, (float)v);
        }
        return uvs;
    }
}
