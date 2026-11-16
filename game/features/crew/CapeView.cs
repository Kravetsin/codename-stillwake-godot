using System;
using System.Linq;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A side of a cape on a figure (a prototype, <see cref="Cape"/>): its sheet drawn as one mesh of triangles. The outside
/// shows its picture, the whole cape painted as it hangs, hood, gathered neck and clasp in it (<see cref="Cape.Painted"/>):
/// posed, it never folds over itself (2026-10-28-the-cape-flows). The inside, the same sheet wrapped round the far side of
/// the body, shows the lining all over. A child of the figure's frame, so its points are drawn where the cape gives them;
/// lit as the figure is.
/// </summary>
public partial class CapeView(Cape cape, bool inside) : Node2D
{
    /// <summary>The cape's pictures, by its picture id (skins): the item's own, `cape`, or a skin's.</summary>
    private Texture2D _outside = PixelArt.Picture("hero_cape"), _lining = PixelArt.Picture("hero_cape_lining");
    private string _picture = Stillwake.Content.Cape.Item;
    private static readonly Vector2[] Uvs = MakeUvs(Cape.Uv), PaintedUvs = MakeUvs(Cape.PaintedUv);
    private static readonly Color[] Colours = [.. Uvs.Select(_ => Colors.White)];
    private readonly Vector2[] _points = new Vector2[Cape.Cols * Cape.Rows];

    /// <summary>Draws the cape in <paramref name="picture"/>'s pictures: <c>hero_{picture}</c> and its lining.</summary>
    public void Wear(string picture)
    {
        if (picture == _picture) return;
        _picture = picture;
        (_outside, _lining) = (PixelArt.Picture($"hero_{picture}"), PixelArt.Picture($"hero_{picture}_lining"));
        QueueRedraw();
    }

    /// <summary>Takes the cape's points as it last moved, to draw them.</summary>
    public void Refresh()
    {
        var points = inside ? cape.Inside : cape.Points;
        for (int i = 0; i < points.Length; i++) _points[i] = new Vector2((float)points[i].X, (float)points[i].Y);
        QueueRedraw();
    }

    public override void _Draw() => RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), Cape.Triangles, _points, Colours,
        inside ? Uvs : PaintedUvs, texture: (inside ? _lining : _outside).GetRid());

    private static Vector2[] MakeUvs(Func<int, (double U, double V)> uv)
    {
        var uvs = new Vector2[Cape.Cols * Cape.Rows];
        for (int i = 0; i < uvs.Length; i++)
        {
            var (u, v) = uv(i);
            uvs[i] = new Vector2((float)u, (float)v);
        }
        return uvs;
    }
}
