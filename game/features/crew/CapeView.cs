using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A cape on a figure (a prototype, <see cref="Cape"/>): its sheet drawn as one mesh of triangles, the picture of its
/// outside laid over the grid and, where a triangle has turned over, its lining. A child of the figure's frame, so its
/// points are drawn where the cape gives them; lit as the figure is.
/// </summary>
public partial class CapeView : Node2D
{
    private static readonly Texture2D Outside = GD.Load<Texture2D>("res://art/hero_cape.png"), Lining = GD.Load<Texture2D>("res://art/hero_cape_lining.png");
    private readonly Cape _cape = new();
    private readonly Vector2[] _points = new Vector2[Cape.Cols * Cape.Rows], _uvs = new Vector2[Cape.Cols * Cape.Rows];
    private readonly Color[] _colours = new Color[Cape.Cols * Cape.Rows];
    private readonly List<int> _front = [], _back = [];

    public CapeView()
    {
        for (int i = 0; i < _uvs.Length; i++)
        {
            var (u, v) = Cape.Uv(i);
            _uvs[i] = new Vector2((float)u, (float)v);
            _colours[i] = Colors.White;
        }
    }

    /// <summary>Moves the cape on by <paramref name="elapsedMs"/> with <paramref name="frame"/>, the figure's frame, as it now stands.</summary>
    public void Step(double elapsedMs, Node2D frame)
    {
        _cape.Step(
            elapsedMs,
            p => { var g = frame.ToGlobal(new Vector2((float)p.X, (float)p.Y)); return (g.X, g.Y); },
            p => { var l = frame.ToLocal(new Vector2((float)p.X, (float)p.Y)); return (l.X, l.Y); });
        var points = _cape.Points;
        for (int i = 0; i < points.Length; i++) _points[i] = new Vector2((float)points[i].X, (float)points[i].Y);
        QueueRedraw();
    }

    /// <summary>Lets it fall from the shoulders afresh.</summary>
    public void Reset() => _cape.Reset();

    public override void _Draw()
    {
        _front.Clear();
        _back.Clear();
        for (int t = 0; t < Cape.Triangles.Length / 3; t++)
        {
            var into = _cape.Flipped(t) ? _back : _front;
            for (int k = 0; k < 3; k++) into.Add(Cape.Triangles[t * 3 + k]);
        }
        // The lining first: where the sheet folds over itself its outside lies over it.
        if (_back.Count > 0) RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), [.. _back], _points, _colours, _uvs, texture: Lining.GetRid());
        if (_front.Count > 0) RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), [.. _front], _points, _colours, _uvs, texture: Outside.GetRid());
    }
}
