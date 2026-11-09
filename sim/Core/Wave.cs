using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>
/// A blast spreading from a cell as a wave, cheapest way first (2026-10-29-meteor-wave, round-blasts): a meteor's and a blasting
/// charge's (blasting-charge). A cell costs <see cref="Damage.Step"/> and what its caller says entering it costs more, times the
/// length of the step to it: <see cref="Straight"/> to a side, up or down, <see cref="Aslant"/> aslant, so the wave keeps round in
/// even ground as in the open. Past the charge, or farther than <c>most</c> cells from where it starts, it is not reached. Whole
/// numbers and buckets by cost, so every peer finds the same cells in the same order. One per module, its buffers kept between blasts.
/// </summary>
public sealed class Wave
{
    /// <summary>A step's length, in fifths of a cell: 5 to a side, up or down, 7 aslant (7/5 ≈ √2), what a cell's cost is multiplied by.</summary>
    public const int Straight = 5, Aslant = 7;

    private int[] _spent = [], _left = [];
    private List<int>[] _byCost = [];
    private readonly List<int> _unpaid = [];

    /// <summary>
    /// Spreads from <paramref name="start"/>, reached at <paramref name="first"/>, with <paramref name="charge"/> to spend, within
    /// <paramref name="most"/> cells of it, on a grid <paramref name="width"/> × <paramref name="height"/>; costs in a straight step's
    /// units, as callers count them. <paramref name="enter"/> says what entering a neighbour costs besides the step, or null where the
    /// wave stops; <paramref name="reached"/> hears each cell once, in rising cost (ties in the order found), with what reaching it
    /// cost, rounded up: an aslant step costs 7/5 of a straight one. <paramref name="unpaid"/>, after, hears each cell the wave came
    /// to the edge of but could not pay to enter, ascending, once, with the most left for it on its cheapest way there once the step
    /// is paid, in the units of its entry cost, rounded down, when that is at least 1 (unified-damage): what a burst cracks it by. A
    /// start dearer than the charge hears the whole charge.
    /// </summary>
    public void Spread(int width, int height, int start, int first, int charge, int most, Func<int, int?> enter, Action<int, int> reached,
        Action<int, int>? unpaid = null)
    {
        if (first > charge)
        {
            if (charge >= 1) unpaid?.Invoke(start, charge);
            return;
        }
        int side = 2 * most + 1, cx = start % width, cy = start / width, budget = charge * Straight;
        if (_spent.Length < side * side) (_spent, _left) = (new int[side * side], new int[side * side]);
        Array.Fill(_spent, int.MaxValue, 0, side * side);
        Array.Fill(_left, 0, 0, side * side);
        _unpaid.Clear();
        if (_byCost.Length < budget + 1) _byCost = [.. Enumerable.Range(0, budget + 1).Select(_ => new List<int>())];
        foreach (var bucket in _byCost) bucket.Clear();
        int Box(int cell) => (cell / width - cy + most) * side + cell % width - cx + most;
        _spent[Box(start)] = first * Straight;
        _byCost[first * Straight].Add(start);
        for (int cost = first * Straight; cost <= budget; cost++)
            for (int i = 0; i < _byCost[cost].Count; i++)
            {
                int cell = _byCost[cost][i];
                if (_spent[Box(cell)] != cost) continue;
                reached(cell, (cost + Straight - 1) / Straight);
                int x = cell % width, y = cell / width;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (dx == 0 && dy == 0 || nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                        if ((nx - cx) * (nx - cx) + (ny - cy) * (ny - cy) > most * most) continue;
                        int next = ny * width + nx;
                        if (enter(next) is not int more) continue;
                        int length = dx != 0 && dy != 0 ? Aslant : Straight, to = cost + length * (Damage.Step + more);
                        if (to > budget)
                        {
                            // Clamped first, so a step dearer than what is left never divides a negative.
                            if (unpaid != null) Owe(Box(next), next, Math.Max(0, budget - cost - length * Damage.Step) / length);
                            continue;
                        }
                        if (to >= _spent[Box(next)]) continue;
                        _spent[Box(next)] = to;
                        _byCost[to].Add(next);
                    }
            }
        if (unpaid == null) return;
        _unpaid.Sort();
        foreach (int cell in _unpaid)
            if (_spent[Box(cell)] == int.MaxValue) unpaid(cell, _left[Box(cell)]);
    }

    /// <summary>Keeps <paramref name="left"/> for <paramref name="cell"/>, at <paramref name="box"/> in the buffers, if at least 1 and the most yet.</summary>
    private void Owe(int box, int cell, int left)
    {
        if (left < 1) return;
        if (_left[box] == 0) _unpaid.Add(cell);
        _left[box] = Math.Max(_left[box], left);
    }
}
