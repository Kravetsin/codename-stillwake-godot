using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// What casts shadows: walls and closed doors, each merged into rectangles — each row's runs, joined
/// down with the same runs below — so a lamp walks a few hundred edges rather than one square a cell
/// (0025). A wall fills its cells; a door only as much as its picture, a panel in the middle of its
/// cells, so light reaches the back wall beside it. Each is one-sided: only its edges facing away from
/// a light cast, so a wall is lit on the faces light falls on and shadows what lies behind it. On a
/// change only the rectangles that differ are taken away or added.
/// </summary>
public partial class Occluders : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private readonly Dictionary<Rect2I, LightOccluder2D> _shown = new();

    /// <summary>The columns of its cell a closed door's picture fills, in pixels: where it casts a shadow.</summary>
    private static readonly (int From, int To) DoorPanel = PanelOf(PixelArt.Picture("door_closed_0").GetImage());

    public int Count => _shown.Count;

    /// <summary>Casts shadows from the walls and closed doors of <paramref name="structure"/>, a grid <paramref name="width"/> cells across.</summary>
    public void Show(ReadOnlySpan<int> structure, int width)
    {
        var wanted = new List<Rect2I>();
        foreach (var r in RectanglesOf(structure, width, Structure.Wall)) wanted.Add(new Rect2I(r.Position * Cell, r.Size * Cell));
        // Doors side by side are panels apart, one a column.
        foreach (var r in RectanglesOf(structure, width, Structure.DoorClosed))
            for (int x = r.Position.X; x < r.End.X; x++)
                wanted.Add(new Rect2I(x * Cell + DoorPanel.From, r.Position.Y * Cell, DoorPanel.To - DoorPanel.From, r.Size.Y * Cell));
        var keep = new HashSet<Rect2I>(wanted);
        foreach (var (rect, node) in new List<KeyValuePair<Rect2I, LightOccluder2D>>(_shown))
        {
            if (keep.Contains(rect)) continue;
            node.QueueFree();
            _shown.Remove(rect);
        }
        foreach (var rect in wanted)
        {
            if (_shown.ContainsKey(rect)) continue;
            var node = new LightOccluder2D
            {
                Position = rect.Position,
                Occluder = new OccluderPolygon2D
                {
                    Polygon = [Vector2.Zero, new Vector2(rect.Size.X, 0), rect.Size, new Vector2(0, rect.Size.Y)],
                    CullMode = OccluderPolygon2D.CullModeEnum.CounterClockwise,
                },
            };
            AddChild(node);
            _shown[rect] = node;
        }
    }

    /// <summary>The columns of a cell a picture's opaque pixels span, from the first to past the last.</summary>
    private static (int From, int To) PanelOf(Image picture)
    {
        if (picture.IsCompressed()) picture.Decompress();
        var used = picture.GetUsedRect();
        return (used.Position.X, used.End.X);
    }

    /// <summary>
    /// The cells of <paramref name="code"/> as rectangles, in cells: each row's runs, every run joined
    /// with the runs of the same columns in the rows below it.
    /// </summary>
    public static List<Rect2I> RectanglesOf(ReadOnlySpan<int> structure, int width, int code)
    {
        int height = width == 0 ? 0 : structure.Length / width;
        var rects = new List<Rect2I>();
        // The rectangle still growing down from the row above, by the column it starts at.
        var open = new Dictionary<int, int>();
        var next = new Dictionary<int, int>();
        for (int y = 0; y < height; y++)
        {
            next.Clear();
            for (int x = 0; x < width; x++)
            {
                int c = y * width + x;
                if (structure[c] != code || (x > 0 && structure[c - 1] == code)) continue;
                int run = 1;
                while (x + run < width && structure[c + run] == code) run++;
                if (open.TryGetValue(x, out int i) && rects[i].Size.X == run)
                {
                    rects[i] = rects[i].GrowSide(Side.Bottom, 1);
                    open.Remove(x);
                }
                else
                {
                    i = rects.Count;
                    rects.Add(new Rect2I(x, y, run, 1));
                }
                next[x] = i;
            }
            (open, next) = (next, open);
        }
        return rects;
    }
}
