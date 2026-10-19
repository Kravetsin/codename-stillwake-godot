using System;
using System.Collections.Generic;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// The grid's six layers as <see cref="GridView"/> last drew them, this peer's toggles in flight made
/// (0014), and the cells that changed since. The grid is looked at again only when its version moved, and
/// then only at the cells its log names (0051); the first time, or when the log let go of them, within what
/// is built now and was at the last look (<see cref="BuiltRows"/>). The cells a toggle in flight shows
/// apart, now or last time, are looked at every time. Plain C#, so it is tested.
/// </summary>
public sealed class DrawnLayers
{
    /// <summary>A cell whose drawing changed, and its structure and conduit as drawn before: a door or valve heard moving.</summary>
    public readonly record struct Change(int Cell, int Structure, int Conduit);

    public int Width { get; }
    public int Height { get; }
    /// <summary>The layers as drawn, a code a cell: empty past what was ever built, as the grid's are.</summary>
    public int[] Structure { get; }
    public int[] Material { get; }
    public int[] Conduit { get; }
    public int[] Back { get; }
    public int[] BackMaterial { get; }
    /// <summary>Each wall's shape as drawn (0069).</summary>
    public int[] Shape { get; }

    private Box _looked = Box.None;
    private int _version = -1;
    /// <summary>The cells drawn as a toggle in flight showed them, apart from the grid.</summary>
    private readonly List<int> _apart = [];
    /// <summary>Scratch for <see cref="Stale"/>, a flag a cell, all false between calls.</summary>
    private readonly bool[] _marked;

    public DrawnLayers(int width, int height)
    {
        (Width, Height) = (width, height);
        int size = width * height;
        (Structure, Material, Conduit, Back, BackMaterial, Shape) = (new int[size], new int[size], new int[size], new int[size], new int[size], new int[size]);
        _marked = new bool[size];
    }

    /// <summary>
    /// Brings the layers up to <paramref name="grid"/> as <paramref name="shown"/> shows it, and answers the
    /// cells that changed, ascending, each with what it was.
    /// </summary>
    public List<Change> Follow(IGridQueries grid, ShownGrid shown)
    {
        var changes = new List<Change>();
        if (grid.Version != _version)
        {
            ReadOnlySpan<int> s = grid.StructureLayer, m = grid.MaterialLayer, cd = grid.ConduitLayer, b = grid.BackLayer, bm = grid.BackMaterialLayer, sh = grid.ShapeLayer;
            int[] ds = Structure, dm = Material, dcd = Conduit, db = Back, dbm = BackMaterial, dsh = Shape;
            var apart = (shown.Structure, shown.Conduit);
            // The world's codes at a cell; one a toggle in flight shows apart is looked at below.
            void Follow(int c, ReadOnlySpan<int> s, ReadOnlySpan<int> m, ReadOnlySpan<int> cd, ReadOnlySpan<int> b, ReadOnlySpan<int> bm, ReadOnlySpan<int> sh)
            {
                if ((s[c] != ds[c] || cd[c] != dcd[c] || m[c] != dm[c] || b[c] != db[c] || bm[c] != dbm[c] || sh[c] != dsh[c])
                    && apart.Structure?.ContainsKey(c) != true && apart.Conduit?.ContainsKey(c) != true)
                    Take(c, s[c], m[c], cd[c], b[c], bm[c], sh[c], changes);
            }
            if (grid.ChangedSince(_version) is { } log)
                foreach (var change in log) Follow(change.Cell, s, m, cd, b, bm, sh);
            else
            {
                // The first drawing, or the grid's log let go of what changed: every row of what is built, now and last time.
                var box = _looked.Union(grid.Built);
                int w = Width;
                for (int y = box.Top; y <= box.Bottom; y++)
                {
                    if (BuiltRows.Same(s, ds, w, box, y) && BuiltRows.Same(cd, dcd, w, box, y) && BuiltRows.Same(m, dm, w, box, y)
                        && BuiltRows.Same(b, db, w, box, y) && BuiltRows.Same(bm, dbm, w, box, y) && BuiltRows.Same(sh, dsh, w, box, y))
                        continue;
                    for (int c = y * w + box.Left, end = c + box.Width; c < end; c++) Follow(c, s, m, cd, b, bm, sh);
                }
            }
            (_looked, _version) = (grid.Built, grid.Version);
        }
        // A row the same as the grid's may still hold a cell shown apart, now or the last time.
        int before = changes.Count;
        foreach (int c in _apart) Look(grid, shown, c, changes);
        _apart.Clear();
        if (shown.Structure != null) _apart.AddRange(shown.Structure.Keys);
        if (shown.Conduit != null) _apart.AddRange(shown.Conduit.Keys);
        foreach (int c in _apart) Look(grid, shown, c, changes);
        if (changes.Count > before) changes.Sort((a, b) => a.Cell.CompareTo(b.Cell));
        return changes;
    }

    private void Look(IGridQueries grid, ShownGrid shown, int c, List<Change> changes)
    {
        int s = shown.StructureAt(grid, c), cd = shown.ConduitAt(grid, c);
        int m = grid.MaterialLayer[c], b = grid.BackLayer[c], bm = grid.BackMaterialLayer[c], sh = grid.ShapeLayer[c];
        if (s == Structure[c] && cd == Conduit[c] && m == Material[c] && b == Back[c] && bm == BackMaterial[c] && sh == Shape[c]) return;
        Take(c, s, m, cd, b, bm, sh, changes);
    }

    private void Take(int c, int s, int m, int cd, int b, int bm, int sh, List<Change> changes)
    {
        changes.Add(new Change(c, Structure[c], Conduit[c]));
        (Structure[c], Material[c], Conduit[c], Back[c], BackMaterial[c], Shape[c]) = (s, m, cd, b, bm, sh);
    }

    /// <summary>
    /// The cells whose drawing <paramref name="changes"/> made stale: each changed cell and its four neighbours; a door
    /// cell come or gone, its four corners too, as a hatch beside a cell it made a door's ends there (0067).
    /// </summary>
    public List<int> Stale(List<Change> changes)
    {
        int w = Width, size = Width * Height;
        var stale = new List<int>(changes.Count * 3);
        void Mark(int c)
        {
            if (c < 0 || c >= size || _marked[c]) return;
            _marked[c] = true;
            stale.Add(c);
        }
        foreach (var change in changes)
        {
            int c = change.Cell, x = c % w;
            Mark(c);
            Mark(c - w);
            Mark(c + w);
            if (x > 0) Mark(c - 1);
            if (x < w - 1) Mark(c + 1);
            if (Doorways.IsDoor(change.Structure) == Doorways.IsDoor(Structure[c])) continue;
            foreach (int row in (ReadOnlySpan<int>)[c - w, c + w])
            {
                if (x > 0) Mark(row - 1);
                if (x < w - 1) Mark(row + 1);
            }
        }
        foreach (int c in stale) _marked[c] = false;
        return stale;
    }
}
