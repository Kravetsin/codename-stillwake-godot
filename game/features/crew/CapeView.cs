using System;
using System.Linq;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A side of a cape on a figure (a prototype, <see cref="Cape"/>), drawn as its rigid pieces (<see cref="Cape.Pieces"/>), so no
/// picture stretches. The outside shows its picture, the whole cape painted as it hangs, hood, gathered neck and clasp in it
/// (<see cref="Cape.Painted"/>). The inside, the same pieces wrapped round the far side of the body, shows the lining all over.
/// A child of the figure's frame; lit as the figure is.
/// </summary>
public partial class CapeView(Cape cape, bool inside) : Node2D
{
    /// <summary>The cape's pictures, by its picture id (skins): the item's own, `cape`, or a skin's.</summary>
    private Texture2D _outside = PixelArt.Picture("hero_cape"), _lining = PixelArt.Picture("hero_cape_lining");
    private string _picture = Stillwake.Content.Cape.Item;
    private static readonly Vector2[] Uvs = MakeUvs(Cape.Uv);

    /// <summary>Draws the cape in <paramref name="picture"/>'s pictures: <c>hero_{picture}</c> and its lining.</summary>
    public void Wear(string picture)
    {
        if (picture == _picture) return;
        _picture = picture;
        (_outside, _lining) = (PixelArt.Picture($"hero_{picture}"), PixelArt.Picture($"hero_{picture}_lining"));
        QueueRedraw();
    }

    /// <summary>Takes the cape's points as it last moved, to draw them.</summary>
    public void Refresh() => QueueRedraw();

    public override void _Draw()
    {
        // Piece by piece, the lowest first: each overlaps the one under it by a row, hidden under its foot. The inside lies on the
        // same pieces, its front edge carried forward as far as the sheet wraps round the body now.
        for (int k = Cape.Pieces.Length - 1; k >= 0; k--)
        {
            var piece = PieceOf[k];
            for (int i = 0; i < piece.Points.Length; i++)
            {
                int at = piece.First + i;
                var (x, y) = cape.OnPiece(k, Cape.Rest(at));
                float wrap = inside ? (float)(cape.Inside[at].X - cape.Points[at].X) : 0;
                piece.Points[i] = new Vector2((float)x + wrap, (float)y);
            }
            RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), piece.Triangles, piece.Points, piece.Colours,
                inside ? piece.LiningUvs : piece.Uvs, texture: (inside ? _lining : _outside).GetRid());
        }
    }

    /// <summary>A piece of the outside as drawn: the sheet's points from the row over its top (none over the first) to its bottom.</summary>
    private sealed record Piece(int First, Vector2[] Points, Vector2[] Uvs, Vector2[] LiningUvs, int[] Triangles, Color[] Colours);

    private static readonly Piece[] PieceOf = [.. Cape.Pieces.Select((p, k) =>
    {
        int top = k == 0 ? p.Top : p.Top - 1, rows = p.Bottom - top + 1, first = top * Cape.Cols;
        var uvs = Enumerable.Range(first, rows * Cape.Cols).Select(i => { var (u, v) = Cape.PaintedUv(i); return new Vector2((float)u, (float)v); }).ToArray();
        return new Piece(first, new Vector2[uvs.Length], uvs, [.. Uvs.Skip(first).Take(uvs.Length)], Cape.GridTriangles(rows), [.. uvs.Select(_ => Colors.White)]);
    })];

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
