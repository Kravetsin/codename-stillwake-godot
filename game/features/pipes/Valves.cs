using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Pipes;

/// <summary>
/// The cells holding a valve, open or closed, ascending, kept up with the grid's log (0051): looked for
/// within what is built only for a grid not seen before, or when the log let go of what changed. Plain C#,
/// so it is tested.
/// </summary>
public sealed class Valves
{
    private readonly List<int> _cells = [];
    private IGridQueries? _grid;
    private int _version;

    /// <summary>The valves of <paramref name="grid"/> as it stands, ascending.</summary>
    public IReadOnlyList<int> Of(IGridQueries grid)
    {
        var log = grid == _grid ? grid.ChangedSince(_version) : null;
        (_grid, _version) = (grid, grid.Version);
        var conduit = grid.ConduitLayer;
        if (log == null)
        {
            _cells.Clear();
            var built = grid.Built;
            for (int y = built.Top; y <= built.Bottom; y++)
                for (int cell = y * grid.Width + built.Left, end = cell + built.Width; cell < end; cell++)
                    if (conduit[cell] is Conduit.ValveOpen or Conduit.ValveClosed) _cells.Add(cell);
            return _cells;
        }
        foreach (var change in log)
        {
            if ((change.Layers & GridLayers.Conduit) == 0) continue;
            int at = _cells.BinarySearch(change.Cell);
            bool valve = conduit[change.Cell] is Conduit.ValveOpen or Conduit.ValveClosed;
            if (valve && at < 0) _cells.Insert(~at, change.Cell);
            else if (!valve && at >= 0) _cells.RemoveAt(at);
        }
        return _cells;
    }
}
