namespace Stillwake.Content;

/// <summary>
/// Debris drifting past the ship (ADRs 0006 and 0009). Times are in ticks, positions and speeds in
/// thousandths of a cell, distances in cells. A piece spawns <see cref="Every"/> ticks plus up to
/// <see cref="Jitter"/> more after the last, while fewer than <see cref="Max"/> are out:
/// <see cref="Lead"/> cells right of the hull, past what the farthest zoom shows, in a row of the hull's
/// own and <see cref="Margin"/> more above and below; it drifts left at a speed of its own between
/// <see cref="DriftMin"/> and <see cref="DriftMax"/> a tick, and is gone <see cref="Lead"/> cells left of
/// the hull. Its kind is drawn by weight from <see cref="Kinds"/>. The wreck's world begins with
/// <see cref="AtStart"/> out, each already somewhere along its way, so a player waits for none.
/// </summary>
public static class Debris
{
    public const int Every = 160;
    public const int Jitter = 80;
    public const int Max = 20;
    public const int AtStart = 8;
    public const int DriftMin = 30;
    public const int DriftMax = 55;
    public const int Margin = 12;
    public const int Lead = 64;

    /// <summary>
    /// How far from the pointer, in thousandths of a cell, a piece is still aimed at: a piece drifting by
    /// is small and moving, and a pointer exactly on it is hard to keep.
    /// </summary>
    public const int AimRadius = 1500;

    public static readonly IReadOnlyList<(string Item, int Weight)> Kinds =
    [
        ("scrapChunk", 6),
        ("hullPlate", 3),
        ("cargoCrate", 2),
        ("supplyPod", 1),
    ];
}

/// <summary>One kind of thing a container holds: between <paramref name="Min"/> and <paramref name="Max"/> of <paramref name="Item"/>, in one of <paramref name="OneIn"/> of them.</summary>
public sealed record LootEntry(string Item, int Min, int Max, int? OneIn = null);

/// <summary>
/// What a container opens into when caught, drawn entry by entry in order from the world's Rng: the
/// count first, in [Min, Max], after the one-in roll when there is one.
/// </summary>
public static class Loot
{
    public static readonly IReadOnlyList<(string Container, IReadOnlyList<LootEntry> Entries)> All =
    [
        ("cargoCrate",
        [
            new("planks", 3, 6),
            new("schematic", 1, 1, OneIn: 4),
        ]),
        ("supplyPod",
        [
            new("canister", 1, 1),
            new("scrap", 6, 6),
            new("schematic", 1, 1, OneIn: 2),
        ]),
    ];

    /// <summary>What <paramref name="item"/> opens into, or null when it is no container.</summary>
    public static IReadOnlyList<LootEntry>? Of(string item)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Container == item) return All[i].Entries;
        return null;
    }
}
