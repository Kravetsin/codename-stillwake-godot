using System.Collections.Generic;
using Stillwake.Sim.Building;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// Which plants were felled since a view last looked, from the cells it kept of every plant then and the plants standing now:
/// both ascend by cell and felling only takes from the plants (<see cref="BuildingModule.Plants"/>), so a run of cells kept whose
/// ends stand with as many plants between them as it holds lost none. Halving the runs that lost some finds the few felled
/// among a planet's 57,000 by a few dozen binary searches: a set of them all made for each felling took 3 to 8 ms.
/// </summary>
public static class Felling
{
    /// <summary>
    /// Takes from <paramref name="kept"/>, the cells of every plant once standing, ascending, each cell no longer in
    /// <paramref name="standing"/>, adding it to <paramref name="felled"/>, ascending.
    /// </summary>
    public static void Take(List<int> kept, IReadOnlyList<Plant> standing, List<int> felled)
    {
        int from = felled.Count;
        if (kept.Count != standing.Count) Find(kept, standing, 0, kept.Count, felled);
        for (int i = from; i < felled.Count; i++) kept.RemoveAt(kept.BinarySearch(felled[i]));
    }

    /// <summary>The cells of <paramref name="kept"/> from <paramref name="lo"/> to <paramref name="hi"/>, exclusive, no longer standing.</summary>
    private static void Find(List<int> kept, IReadOnlyList<Plant> standing, int lo, int hi, List<int> felled)
    {
        if (lo >= hi) return;
        int first = From(standing, kept[lo]), last = From(standing, kept[hi - 1]);
        if (last < standing.Count && standing[last].Cell == kept[hi - 1]) last++;
        if (last - first == hi - lo) return;
        if (hi - lo == 1)
        {
            felled.Add(kept[lo]);
            return;
        }
        int mid = (lo + hi) / 2;
        Find(kept, standing, lo, mid, felled);
        Find(kept, standing, mid, hi, felled);
    }

    /// <summary>The index of the first plant standing at <paramref name="cell"/> or past it.</summary>
    private static int From(IReadOnlyList<Plant> standing, int cell)
    {
        int lo = 0, hi = standing.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (standing[mid].Cell < cell) lo = mid + 1;
            else hi = mid;
        }
        return lo;
    }
}
