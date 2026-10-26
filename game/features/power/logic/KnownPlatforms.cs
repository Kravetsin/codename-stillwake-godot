using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Power;

/// <summary>
/// The launch platform runs (<see cref="PlatformRuns.Of"/>) kept up with a grid through its log (0051): a run lies along one row, so
/// a change finds again only the rows it touched. A planet is millions of cells, and finding every run again on each cell dug or
/// built froze a frame for a tenth of a second.
/// </summary>
public sealed class KnownPlatforms
{
    private readonly List<(int First, int Length)> _runs = [];
    private readonly HashSet<int> _rows = [];
    private IGridQueries? _grid;
    private int _version = -1;

    /// <summary>The runs as the grid stood when last followed, in reading order.</summary>
    public IReadOnlyList<(int First, int Length)> Runs => _runs;

    /// <summary>Brings <see cref="Runs"/> up to <paramref name="grid"/>: answers whether they changed.</summary>
    public bool Follow(IGridQueries grid)
    {
        if (grid == _grid && grid.Version == _version) return false;
        var log = grid == _grid ? grid.ChangedSince(_version) : null;
        (_grid, _version) = (grid, grid.Version);
        if (log == null)
        {
            _runs.Clear();
            _runs.AddRange(PlatformRuns.Of(grid));
            return true;
        }
        _rows.Clear();
        foreach (var change in log)
            if ((change.Layers & GridLayers.Structure) != 0) _rows.Add(change.Cell / grid.Width);
        if (_rows.Count == 0) return false;
        var before = _runs.ToArray();
        _runs.RemoveAll(r => _rows.Contains(r.First / grid.Width));
        foreach (int y in _rows) PlatformRuns.AddRow(grid, y, 0, grid.Width - 1, _runs);
        _runs.Sort();
        return !before.SequenceEqual(_runs);
    }
}
