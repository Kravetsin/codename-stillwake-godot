using L = Stillwake.Content.Liquids;

namespace Stillwake.Sim.Liquids;

/// <summary>
/// Puddles (flowing-liquids): a run, the cells of a kind short of full side by side in a row, each resting (on
/// what blocks, on a full cell of its kind, or on the bottom), levels along its row as pressure levels a body, so a shallow
/// puddle lies flat rather than as a ramp of a unit a cell. A run resting on the ground alone whose cells hold <c>Liquids.Dry</c>
/// or less and none of which can move dries up: thin water has nowhere to go, and a film is not left on the rock.
/// </summary>
public sealed partial class LiquidsModule
{
    /// <summary>Derived: the cells put in a run this tick; only added to and looked up, never walked.</summary>
    private readonly HashSet<int> _inRun = [];

    /// <summary>Derived: the runs found this tick, each its first (leftmost) cell and its length, and the runs planned to dry.</summary>
    private readonly List<(int First, int Count)> _runs = [];
    private readonly List<(int First, int Count)> _dries = [];

    /// <summary>For tests: the units dried since the world was made or loaded.</summary>
    internal int Dried { get; private set; }

    /// <summary>The runs with a cell in an awake chunk, or <paramref name="everywhere"/>, ordered by their first cells.</summary>
    private void FindRuns(byte[] layer, bool everywhere)
    {
        _inRun.Clear();
        _runs.Clear();
        const int c = L.Chunk;
        for (int chunk = 0; chunk < _awake.Length; chunk++)
        {
            if (!everywhere && !_awake[chunk]) continue;
            int cx = chunk % _chunksAcross, cy = chunk / _chunksAcross;
            for (int y = cy * c; y < Math.Min(_height, (cy + 1) * c); y++)
                for (int x = cx * c; x < Math.Min(_width, (cx + 1) * c); x++)
                {
                    int cell = y * _width + x;
                    if (!Resting(layer, cell) || _inRun.Contains(cell)) continue;
                    byte kind = L.KindOf(layer[cell]);
                    int left = x, right = x;
                    while (left > 0 && Resting(layer, cell - (x - left) - 1) && L.KindOf(layer[cell - (x - left) - 1]) == kind) left--;
                    while (right + 1 < _width && Resting(layer, cell + (right - x) + 1) && L.KindOf(layer[cell + (right - x) + 1]) == kind) right++;
                    for (int i = left; i <= right; i++) _inRun.Add(y * _width + i);
                    _runs.Add((y * _width + left, right - left + 1));
                }
        }
        _runs.Sort((a, b) => a.First.CompareTo(b.First));
    }

    /// <summary>
    /// The moves of the run from <paramref name="first"/>, up to <paramref name="press"/> units from its fullest cell to its
    /// emptiest while 2 or more apart, ties to the left, added to <see cref="_moves"/>; with none, the run added to
    /// <see cref="_dries"/> if it dries. Whether it does either.
    /// </summary>
    private bool PlanRun(byte[] layer, int first, int count, int press)
    {
        _change.Clear();
        bool any = false;
        for (int moved = 0; moved < press; moved++)
        {
            int from = -1, to = -1, high = int.MinValue, low = int.MaxValue;
            for (int cell = first; cell < first + count; cell++)
            {
                int a = Now(layer, cell);
                if (a > high) (from, high) = (cell, a);
                if (a < low) (to, low) = (cell, a);
            }
            if (high - low < 2) break;
            _moves.Add((from, to));
            _change.Add((from, -1));
            _change.Add((to, 1));
            any = true;
        }
        if (any) return true;
        for (int cell = first; cell < first + count; cell++)
        {
            int y = cell / _width;
            if (L.AmountOf(layer[cell]) > L.Dry || y + 1 < _height && !_grid.Blocks(cell + _width) || CanMove(layer, cell, cell % _width, y))
                return false;
        }
        _dries.Add((first, count));
        return true;
    }

    /// <summary>The planned runs dried, each cell still holding <c>Liquids.Dry</c> or less after the moves made before.</summary>
    private void Dry(byte[] layer)
    {
        foreach (var (first, count) in _dries)
            for (int cell = first; cell < first + count; cell++)
                if (L.AmountOf(layer[cell]) is var a && a > 0 && a <= L.Dry)
                {
                    Dried += a;
                    Set(layer, cell, L.None);
                }
    }

    /// <summary>Whether <paramref name="cell"/> is in a run: holds liquid short of full, and rests where it lies.</summary>
    private bool Resting(byte[] layer, int cell)
    {
        int a = L.AmountOf(layer[cell]);
        if (a == 0 || a == L.Full) return false;
        int under = cell + _width;
        return under >= layer.Length || _grid.Blocks(under) || L.KindOf(layer[under]) == L.KindOf(layer[cell]) && Full(layer, under);
    }
}
