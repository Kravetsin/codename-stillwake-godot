namespace Stillwake.Sim.Items;

/// <summary>How loot sprayed out of a creature is dealt into pieces (2026-10-29-kill-feel).</summary>
internal static class SprayRules
{
    /// <summary>
    /// <paramref name="loot"/> as pieces: every item one piece holding all of it, then, the items taken in turn, a unit peeled off
    /// into a piece of its own while there are fewer than <paramref name="most"/> pieces and an item has more than one left. In
    /// the loot's order, an item's pieces together; nothing is lost.
    /// </summary>
    public static IReadOnlyList<(string Item, int Count)> Pieces(IReadOnlyList<(string Item, int Count)> loot, int most)
    {
        var items = loot.Where(e => e.Count > 0).ToList();
        var peeled = new int[items.Count];
        int pieces = items.Count;
        for (bool more = true; more && pieces < most;)
        {
            more = false;
            for (int i = 0; i < items.Count && pieces < most; i++)
            {
                if (items[i].Count - peeled[i] <= 1) continue;
                peeled[i]++;
                pieces++;
                more = true;
            }
        }
        var dealt = new List<(string Item, int Count)>();
        for (int i = 0; i < items.Count; i++)
        {
            for (int k = 0; k < peeled[i]; k++) dealt.Add((items[i].Item, 1));
            dealt.Add((items[i].Item, items[i].Count - peeled[i]));
        }
        return dealt;
    }
}
