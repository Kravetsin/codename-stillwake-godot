using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>
/// A blast spreading from a cell as a wave, cheapest way first (2026-10-29-meteor-wave): a meteor's and a blasting charge's
/// (blasting-charge). Reaching a cell costs <see cref="Meteors.Step"/> (<see cref="Meteors.Diagonal"/> aslant) and what its caller
/// says entering it costs more; past the charge, or past the box of <c>most</c> cells either way, it is not reached. Whole numbers
/// and buckets by cost, so every peer finds the same cells in the same order. One per module, its buffers kept between blasts.
/// </summary>
public sealed class Wave
{
    private int[] _spent = [];
    private List<int>[] _byCost = [];

    /// <summary>
    /// Spreads from <paramref name="start"/>, reached at <paramref name="first"/>, with <paramref name="charge"/> to spend, inside the
    /// box of <paramref name="most"/> cells either way of it, on a grid <paramref name="width"/> × <paramref name="height"/>.
    /// <paramref name="enter"/> says what entering a neighbour costs besides the step, or null where the wave stops;
    /// <paramref name="reached"/> hears each cell once, at the cheapest cost found, in rising cost (ties in the order found).
    /// </summary>
    public void Spread(int width, int height, int start, int first, int charge, int most, Func<int, int?> enter, Action<int, int> reached)
    {
        if (first > charge) return;
        int side = 2 * most + 1, cx = start % width, cy = start / width;
        if (_spent.Length < side * side) _spent = new int[side * side];
        Array.Fill(_spent, int.MaxValue, 0, side * side);
        if (_byCost.Length < charge + 1) _byCost = [.. Enumerable.Range(0, charge + 1).Select(_ => new List<int>())];
        foreach (var bucket in _byCost) bucket.Clear();
        int Box(int cell) => (cell / width - cy + most) * side + cell % width - cx + most;
        _spent[Box(start)] = first;
        _byCost[first].Add(start);
        for (int cost = first; cost <= charge; cost++)
            for (int i = 0; i < _byCost[cost].Count; i++)
            {
                int cell = _byCost[cost][i];
                if (_spent[Box(cell)] != cost) continue;
                reached(cell, cost);
                int x = cell % width, y = cell / width;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (dx == 0 && dy == 0 || nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                        if (Math.Abs(nx - cx) > most || Math.Abs(ny - cy) > most) continue;
                        int next = ny * width + nx;
                        if (enter(next) is not int more) continue;
                        int to = cost + (dx != 0 && dy != 0 ? Meteors.Diagonal : Meteors.Step) + more;
                        if (to > charge || to >= _spent[Box(next)]) continue;
                        _spent[Box(next)] = to;
                        _byCost[to].Add(next);
                    }
            }
    }
}
