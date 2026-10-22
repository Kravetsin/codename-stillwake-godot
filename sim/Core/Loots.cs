using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>What a list of loot entries gives, drawn from the world's Rng: a container's or a wreck's (0034), a creature's yields (0102).</summary>
public static class Loots
{
    /// <summary>
    /// <paramref name="entries"/> drawn by <paramref name="rng"/> entry by entry: the one-in roll, then the count. Merged by item,
    /// in the order the entries first give it.
    /// </summary>
    public static IReadOnlyList<(string Item, int Count)> Roll(IReadOnlyList<LootEntry> entries, Rng rng)
    {
        var got = new List<(string Item, int Count)>();
        foreach (var entry in entries)
        {
            if (entry.OneIn is int oneIn && rng.Below((uint)oneIn) != 0) continue;
            int count = entry.Min + (int)rng.Below((uint)(entry.Max - entry.Min + 1));
            if (count <= 0) continue;
            int at = got.FindIndex(g => g.Item == entry.Item);
            if (at < 0) got.Add((entry.Item, count));
            else got[at] = (entry.Item, got[at].Count + count);
        }
        return got;
    }
}
