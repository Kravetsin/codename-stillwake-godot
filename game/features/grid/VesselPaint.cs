using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// An abandoned vessel's name and code (derelict-looks), painted once as its surface loads: its labels laid in one picture from the
/// painted letters (<c>vessel_letters</c>), each a row of cells, then cut into a tile a cell, which <see cref="GridView"/> lays on each
/// cell of the label while it is still a hull wall. A letter half on a cell gone is cut at that cell's edge.
/// </summary>
public static class VesselPaint
{
    private const int Cell = ShipCamera.Cell, S = HiRes.Scale;

    /// <summary>The source its tiles are in.</summary>
    public const int Source = 0;

    /// <summary>The tiles of <paramref name="dress"/>'s labels, a cell each; none for a vessel with no label.</summary>
    public static TileSet? Of(VesselDress dress)
    {
        var (across, down) = dress.PaintCells;
        if (across == 0 || down == 0) return null;
        var picture = Image.CreateEmpty(across * Cell * S, down * Cell * S, false, Image.Format.Rgba8);
        Image? large = Letters("vessel_letters"), small = Letters("vessel_letters_small");
        foreach (var label in dress.Labels)
        {
            var (sheet, (w, h)) = label.Large ? (large, VesselLooks.LargeGlyph) : (small, VesselLooks.SmallGlyph);
            if (sheet == null) continue;
            for (int i = 0; i < label.Text.Length; i++)
            {
                int glyph = VesselLooks.Glyphs.IndexOf(label.Text[i]);
                if (glyph >= 0)
                    picture.BlitRect(sheet, new Rect2I(glyph * w * S, 0, w * S, h * S), new Vector2I(i * w * S, (label.Row * Cell + label.TextY) * S));
            }
        }
        var source = new TileSetAtlasSource { Texture = HiRes.Texture(picture), TextureRegionSize = new Vector2I(Cell, Cell), UseTexturePadding = false };
        var set = new TileSet { TileSize = new Vector2I(Cell, Cell) };
        set.AddSource(source, Source);
        for (int y = 0; y < down; y++)
            for (int x = 0; x < across; x++) source.CreateTile(new Vector2I(x, y));
        return set;
    }

    private static Image? Letters(string name)
    {
        if (HiRes.Load(name) is not { } loaded) return null;
        var image = (Image)loaded.Duplicate();
        image.Convert(Image.Format.Rgba8);
        return image;
    }
}
