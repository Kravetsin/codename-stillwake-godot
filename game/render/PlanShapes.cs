using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>
/// The silhouettes of plans and marks, for their dashes (<see cref="PlanOutline"/>): the rim of the opaque pixels of
/// what a placement is drawn with, read from each picture on its own, never from the ship's atlas, which packs its
/// pictures with no room round them. Worked out once a picture and once a way of laying them, then kept.
/// </summary>
public static class PlanShapes
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>A pixel at least this opaque is the picture's, as the aim outline reads a mask.</summary>
    private const float Opaque = 0.5f;

    private static readonly Dictionary<string, (bool[] Mask, int Width, int Height)> Masks = new();
    /// <summary>Each placement's rim by how it joins and which cell of a picture laid over the world it shows.</summary>
    private static readonly Dictionary<(Placement, int Linked, int CutX, int CutY), IReadOnlyList<Rim>> Rims = new();

    /// <summary>
    /// The rim of <paramref name="placement"/> at cell (x, y), joined on the sides in <paramref name="linked"/> as it is
    /// drawn (<see cref="BlueprintArt.PiecesOf"/>), from the cell's top-left: every piece's opaque pixels together.
    /// </summary>
    public static IReadOnlyList<Rim> RimOf(Placement placement, int x, int y, int linked)
    {
        var pieces = BlueprintArt.PiecesOf(placement, linked);
        // A picture laid over the world (a wall's) shows the piece ShipTiles.CutAt picks for the cell.
        var cut = Vector2I.Zero;
        foreach (var piece in pieces)
            if (piece.Cut) cut = new Vector2I(x, y) % ShipTiles.Shared.SizeOf(piece.Picture);
        var key = (placement, linked, cut.X, cut.Y);
        if (Rims.TryGetValue(key, out var kept)) return kept;
        var laid = new List<(bool[] Mask, int Stride, int Sx, int Sy, int W, int H, int Dx, int Dy)>();
        int width = 0, height = 0;
        foreach (var piece in pieces)
        {
            var (mask, w, h) = MaskOf(piece.Picture);
            // A cut picture shorter than its cells (a floor's 4 px slab) is read only as far as it goes.
            var at = piece.Cut ? (cut.X * Cell, cut.Y * Cell, Mathf.Clamp(w - cut.X * Cell, 0, Cell), Mathf.Clamp(h - cut.Y * Cell, 0, Cell), 0, 0)
                // A valve's wheel sits in the middle of its cell.
                : piece.Layer == TileLayer.Valves ? (0, 0, w, h, (Cell - w) / 2, (Cell - h) / 2)
                : (0, 0, w, h, 0, 0);
            laid.Add((mask, w, at.Item1, at.Item2, at.Item3, at.Item4, at.Item5, at.Item6));
            width = Mathf.Max(width, at.Item5 + at.Item3);
            height = Mathf.Max(height, at.Item6 + at.Item4);
        }
        var union = new bool[width * height];
        foreach (var (mask, stride, sx, sy, w, h, dx, dy) in laid)
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    if (mask[(sy + j) * stride + sx + i]) union[(dy + j) * width + dx + i] = true;
        var rims = PlanOutline.RimOf((i, j) => union[j * width + i], width, height);
        // A machine's picture rising over its footprint (0099) is laid from the cells above.
        int rise = pieces.Count > 0 ? pieces.Max(p => p.Rise) * Cell : 0;
        return Rims[key] = rise == 0 ? rims : rims.Select(r => r with { Y = r.Y - rise }).ToList();
    }

    private static (bool[] Mask, int Width, int Height) MaskOf(string picture)
    {
        if (Masks.TryGetValue(picture, out var kept)) return kept;
        var image = ShipTiles.ImageOf(picture);
        int w = image.GetWidth(), h = image.GetHeight();
        var mask = new bool[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                mask[y * w + x] = image.GetPixel(x, y).A >= Opaque;
        return Masks[picture] = (mask, w, h);
    }
}
