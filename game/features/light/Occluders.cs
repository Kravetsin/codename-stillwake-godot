using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// What casts shadows: walls and closed doors, merged into rectangles — each row's runs, joined down
/// with the same runs below — so a lamp walks a few hundred edges rather than
/// one square a cell (0025). Each is one-sided: only its edges facing away from a light cast, so a
/// wall is lit on the faces light falls on and shadows what lies behind it. On a change only the
/// rectangles that differ are taken away or added.
/// </summary>
public partial class Occluders : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private readonly Dictionary<Rect2I, LightOccluder2D> _shown = new();

    public int Count => _shown.Count;

    /// <summary>Casts shadows from the solid cells of <paramref name="structure"/>, a grid <paramref name="width"/> cells across.</summary>
    public void Show(ReadOnlySpan<int> structure, int width)
    {
        var wanted = RectanglesOf(structure, width);
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
                Position = rect.Position * Cell,
                Occluder = new OccluderPolygon2D
                {
                    Polygon = [Vector2.Zero, new Vector2(rect.Size.X * Cell, 0), rect.Size * Cell, new Vector2(0, rect.Size.Y * Cell)],
                    CullMode = OccluderPolygon2D.CullModeEnum.CounterClockwise,
                },
            };
            AddChild(node);
            _shown[rect] = node;
        }
    }

    private static bool Solid(int code) => code is Structure.Wall or Structure.DoorClosed;

    /// <summary>
    /// The solid cells as rectangles, in cells: each row's runs, every run joined with the runs of the
    /// same columns in the rows below it.
    /// </summary>
    public static List<Rect2I> RectanglesOf(ReadOnlySpan<int> structure, int width)
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
                if (!Solid(structure[c]) || (x > 0 && Solid(structure[c - 1]))) continue;
                int run = 1;
                while (x + run < width && Solid(structure[c + run])) run++;
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
