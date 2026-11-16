using System.Linq;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A side of a cape on a figure (a prototype, <see cref="Cape"/>): its sheet drawn as one mesh of triangles. The outside
/// shows its picture all over: posed, it never folds over itself (2026-10-28-the-cape-flows). The inside, the same sheet
/// wrapped round the far side of the body, shows the lining all over. A child of the figure's frame, so its points are drawn where the cape
/// gives them; lit as the figure is. The outside wears its hood and its fur collar.
/// </summary>
public partial class CapeView(Cape cape, bool inside) : Node2D
{
    /// <summary>The cape's pictures, by its picture id (skins): the item's own, `cape`, or a skin's.</summary>
    private Texture2D _outside = PixelArt.Picture("hero_cape"), _lining = PixelArt.Picture("hero_cape_lining"),
        _hoodPicture = PixelArt.Picture("hero_cape_hood"), _collar = PixelArt.Picture("hero_cape_collar");
    private string _picture = Stillwake.Content.Cape.Item;
    /// <summary>Where the collar lies, its top left, from the feet: round the neck over the cape's top, from the back to the throat.</summary>
    private static readonly Vector2 CollarAt = new(-10.5f, (float)-(Rig.Neck + 1.5));
    private static readonly Vector2[] Uvs = MakeUvs();
    private static readonly Vector2[] HoodUvs = MakeHoodUvs();
    private static readonly Color[] Colours = [.. Uvs.Select(_ => Colors.White)], HoodColours = [.. HoodUvs.Select(_ => Colors.White)];
    private readonly Vector2[] _points = new Vector2[Cape.Cols * Cape.Rows], _hood = new Vector2[Cape.HoodCols * Cape.HoodRows];

    /// <summary>Draws the cape in <paramref name="picture"/>'s pictures: <c>hero_{picture}</c>, its lining, hood and collar.</summary>
    public void Wear(string picture)
    {
        if (picture == _picture) return;
        _picture = picture;
        (_outside, _lining) = (PixelArt.Picture($"hero_{picture}"), PixelArt.Picture($"hero_{picture}_lining"));
        (_hoodPicture, _collar) = (PixelArt.Picture($"hero_{picture}_hood"), PixelArt.Picture($"hero_{picture}_collar"));
        QueueRedraw();
    }

    /// <summary>Takes the cape's points as it last moved, to draw them.</summary>
    public void Refresh()
    {
        var points = inside ? cape.Inside : cape.Points;
        for (int i = 0; i < points.Length; i++) _points[i] = new Vector2((float)points[i].X, (float)points[i].Y);
        for (int i = 0; i < _hood.Length; i++) _hood[i] = new Vector2((float)cape.Hood[i].X, (float)cape.Hood[i].Y);
        QueueRedraw();
    }

    public override void _Draw()
    {
        RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), Cape.Triangles, _points, Colours, Uvs, texture: (inside ? _lining : _outside).GetRid());
        if (inside) return;
        // The hood on the back; the collar over its opening and the cape's top.
        RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), Cape.HoodTriangles, _hood, HoodColours, HoodUvs, texture: _hoodPicture.GetRid());
        DrawTexture(_collar, CollarAt);
    }

    private static Vector2[] MakeHoodUvs()
    {
        var uvs = new Vector2[Cape.HoodCols * Cape.HoodRows];
        for (int i = 0; i < uvs.Length; i++)
        {
            var (u, v) = Cape.HoodUv(i);
            uvs[i] = new Vector2((float)u, (float)v);
        }
        return uvs;
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
