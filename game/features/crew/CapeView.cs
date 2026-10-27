using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A side of a cape on a figure (a prototype, <see cref="Cape"/>): its sheet drawn as one mesh of triangles. The outside
/// shows its picture, and its lining where a triangle has turned over; the inside, the same sheet wrapped round the far
/// side of the body, shows the lining all over. A child of the figure's frame, so its points are drawn where the cape
/// gives them; lit as the figure is. The outside wears its hood and its clasp.
/// </summary>
public partial class CapeView(Cape cape, bool inside) : Node2D
{
    private static readonly Texture2D Outside = GD.Load<Texture2D>("res://art/hero_cape.png"), Lining = GD.Load<Texture2D>("res://art/hero_cape_lining.png"),
        Hood = GD.Load<Texture2D>("res://art/hero_cape_hood.png");
    /// <summary>Where the hood lies, its top left, from the feet: on the back behind the neck, over the cape's top.</summary>
    private static readonly Vector2 HoodAt = new(-12.5f, (float)-(Rig.Neck + 2));
    /// <summary>The clasp at the throat, a pixel of brass on the outside.</summary>
    private static readonly Color Brass = new("c9a35a");
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
        if (inside) return;
        // The outside's hood on the back and its clasp at the throat, where it is pinned.
        DrawTexture(Hood, HoodAt);
        var (cx, cy) = Cape.Clasp;
        DrawRect(new Rect2((float)Math.Floor(cx) - 1, (float)Math.Floor(cy), 2, 2), Brass);
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
