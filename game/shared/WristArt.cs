using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// How the wrist computer is drawn (0074): its pieces of metal (<see cref="WristPieces"/>) as 9-slices round their
/// windows, and its screens: a flat colour with the recess's shadow along the top and left edges and a faint glare,
/// all under what the screen shows, so no slot, digit or mark reads any differently.
/// </summary>
public static class WristArt
{
    /// <summary>The hero's own screens: the hotbar's, the gauges', the bag's and the equipment's.</summary>
    public static readonly Color Screen = new("0f1729");

    /// <summary>An open machine's screen, greyer and greener than the hero's own: its slots never read as the bag's.</summary>
    public static readonly Color MachineScreen = new("152124");

    private static readonly Color Glare = new(200 / 255f, 225 / 255f, 1);

    /// <summary>A piece's art, its window left undrawn; it takes the clicks on its bezel, or lets them through.</summary>
    public static NinePatchRect Frame(WristPiece piece, Control.MouseFilterEnum mouse = Control.MouseFilterEnum.Stop)
    {
        var art = new NinePatchRect
        {
            AxisStretchHorizontal = NinePatchRect.AxisStretchMode.Tile,
            AxisStretchVertical = NinePatchRect.AxisStretchMode.Tile,
            DrawCenter = false,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = mouse,
        };
        Use(art, piece);
        return art;
    }

    /// <summary>Gives <paramref name="art"/> another piece's texture and margins.</summary>
    public static void Use(NinePatchRect art, WristPiece piece)
    {
        art.Texture = GD.Load<Texture2D>(piece.Texture);
        (art.PatchMarginLeft, art.PatchMarginTop, art.PatchMarginRight, art.PatchMarginBottom) =
            (piece.Patch.Left, piece.Patch.Top, piece.Patch.Right, piece.Patch.Bottom);
    }

    /// <summary>Lays <paramref name="art"/> round a window of <paramref name="window"/> at its parent's origin.</summary>
    public static void Fit(NinePatchRect art, WristPiece piece, Vector2 window)
    {
        var (x, y, w, h) = piece.Around((int)Mathf.Ceil(window.X), (int)Mathf.Ceil(window.Y));
        art.Scale = new Vector2(WristPiece.Scale, WristPiece.Scale);
        art.Position = new Vector2(x, y);
        art.Size = new Vector2(w, h);
    }

    /// <summary>
    /// A screen of <paramref name="size"/> at <paramref name="on"/>'s origin: <paramref name="colour"/>, the recess's
    /// shadow on its top and left edges and, with <paramref name="glare"/>, the glass's two faint streaks.
    /// </summary>
    public static void DrawScreen(CanvasItem on, Vector2 size, Color colour, bool glare = true)
    {
        float w = size.X, h = size.Y;
        on.DrawRect(new Rect2(Vector2.Zero, size), colour);
        for (int y = 0; y < 6; y++) on.DrawRect(new Rect2(0, y, w, 1), Colors.Black with { A = 0.5f * (1 - (y + 0.5f) / 6) });
        for (int x = 0; x < 4; x++) on.DrawRect(new Rect2(x, 6, 1, h - 6), Colors.Black with { A = 0.35f * (1 - (x + 0.5f) / 4) });
        if (!glare) return;
        Vector2[] bounds = [Vector2.Zero, new(w, 0), size, new(0, h)];
        foreach (var (streak, alpha) in ((Vector2[], float)[])[
            ([new(70, 0), new(190, 0), new(190 - 0.9f * h, h), new(70 - 0.9f * h, h)], 0.022f),
            ([new(206, 0), new(222, 0), new(222 - 0.9f * h, h), new(206 - 0.9f * h, h)], 0.018f)])
            foreach (var part in Geometry2D.IntersectPolygons(streak, bounds))
                on.DrawColoredPolygon(part, Glare with { A = alpha });
    }
}
