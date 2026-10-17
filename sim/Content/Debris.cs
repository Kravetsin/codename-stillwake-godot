namespace Stillwake.Content;

/// <summary>
/// Debris drifting by (ADRs 0006, 0009 and 0052), in <see cref="Zones"/>, each spawning on a beat of its
/// own by its own anchor. Times are in ticks, positions and speeds in thousandths of a cell, distances
/// in cells. The stream past the ship (<see cref="Ship"/>): a piece spawns <see cref="Every"/> ticks plus
/// up to <see cref="Jitter"/> more after the last, while fewer than <see cref="Max"/> of its own are out:
/// <see cref="Lead"/> cells right of the hull, past what the farthest zoom shows, in a row of the hull's
/// own and <see cref="Margin"/> more above and below; it drifts left at a speed of its own between
/// <see cref="DriftMin"/> and <see cref="DriftMax"/> a tick, and is gone <see cref="Lead"/> cells left of
/// the hull. Its kind is drawn by weight from <see cref="Kinds"/>. The wreck's world begins with
/// <see cref="AtStart"/> out, each already somewhere along its way, so a player waits for none. The far
/// field (<see cref="Far"/>) spawns round every hero out past the stream.
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

    /// <summary>The stream's kinds by weight: its one tier.</summary>
    public static readonly IReadOnlyList<(string Item, int Weight)> Kinds =
    [
        ("scrapChunk", 18),
        ("hullPlate", 9),
        ("cargoCrate", 6),
        ("supplyPod", 3),
        // Wreckage (Wreckage): 9 of 45, one spawn in five, a wreck about every 50 s.
        ("hullSection", 3),
        ("cargoContainer", 2),
        ("machineBlock", 1),
        ("cableRack", 1),
        ("tankCluster", 1),
        ("shieldSlab", 1),
    ];

    /// <summary>The stream past the ship, as it always was: by the hull, in one tier.</summary>
    public static readonly DebrisZone Ship = new("ship", new ByHull(Margin, Lead), Every, Jitter, Max, DriftMin, DriftMax, [new(0, Kinds)]);

    /// <summary>
    /// The far field round every hero out past the stream (0052), tuned by <c>DebrisDensityTests</c>: a hero
    /// sees some 4 pieces at the default zoom and 30 to 40 at the farthest, hovering or flying. The ring hugs
    /// the farthest view (60 × 34), so what spawns on its right drifts into sight soon; a beat every tick
    /// while a hero's keep box holds fewer than 110, so a hero flying by jetpack meets a piece a second;
    /// pieces left out of the small keep box go soon, at the hard box. 640 in all: four heroes' shares and
    /// what lingers. No jitter, so a world with no hero out takes no draw for it. Small salvage near the
    /// ship; the farther out, the more and the bigger the wrecks, and the circuits with them.
    /// </summary>
    public static readonly DebrisZone Far = new("far",
        new AroundHeroes(PerHero: 110, RingFrom: new(61, 36), RingTo: new(64, 42), View: new(60, 34), Keep: new(68, 48), Hard: new(100, 64), Linger: 600),
        Every: 1, Jitter: 0, Max: 640, DriftMin, DriftMax,
        [
            new(0,
            [
                ("scrapChunk", 12), ("hullPlate", 8), ("cargoCrate", 6), ("supplyPod", 4), ("hullSection", 4),
                ("cargoContainer", 3), ("machineBlock", 2), ("cableRack", 2), ("tankCluster", 2), ("shieldSlab", 1),
            ]),
            new(128,
            [
                ("scrapChunk", 6), ("hullPlate", 6), ("cargoCrate", 5), ("supplyPod", 5), ("hullSection", 4),
                ("cargoContainer", 4), ("machineBlock", 3), ("cableRack", 3), ("tankCluster", 3), ("shieldSlab", 2),
            ]),
            new(256,
            [
                ("scrapChunk", 2), ("hullPlate", 3), ("cargoCrate", 3), ("supplyPod", 5), ("hullSection", 3),
                ("cargoContainer", 4), ("machineBlock", 4), ("cableRack", 5), ("tankCluster", 4), ("shieldSlab", 3),
            ]),
        ]);

    /// <summary>Every zone, in the order they spawn on a tick: the stream first, so a world with no hero out draws as it always did.</summary>
    public static readonly IReadOnlyList<DebrisZone> Zones = [Ship, Far];
}

/// <summary>
/// A zone of debris: where its pieces spawn (<paramref name="Anchor"/>), on a beat of its own of
/// <paramref name="Every"/> ticks plus up to <paramref name="Jitter"/>, while fewer than <paramref name="Max"/>
/// of its own are out, drifting left by <paramref name="DriftMin"/> to <paramref name="DriftMax"/> a tick;
/// its kinds drawn from the <paramref name="Tiers"/>, in order of the distance from the hull each starts at.
/// </summary>
public sealed record DebrisZone(string Id, ZoneAnchor Anchor, int Every, int Jitter, int Max, int DriftMin, int DriftMax, IReadOnlyList<DebrisTier> Tiers);

/// <summary>
/// The kinds by weight a zone draws at <paramref name="From"/> cells or more from the hull's box, measured
/// as <c>max(dx, 2·dy)</c> (rows count double: the world is half as tall), up to where the next tier starts.
/// </summary>
public sealed record DebrisTier(int From, IReadOnlyList<(string Item, int Weight)> Kinds);

/// <summary>Where a zone's pieces spawn: a record for each way of anchoring them.</summary>
public abstract record ZoneAnchor;

/// <summary>
/// By the hull: in its rows and <paramref name="Margin"/> more above and below, from <paramref name="Lead"/>
/// cells right of it to as far left of it. That box, the hull grown so, is the stream's.
/// </summary>
public sealed record ByHull(int Margin, int Lead) : ZoneAnchor;

/// <summary>
/// Round every hero out past every stream's box, at the cell it last reported (0052): on a ring from
/// half-box <paramref name="RingFrom"/> out to <paramref name="RingTo"/> round it, never in a stream's box
/// nor in any hero's <paramref name="View"/> (the most the farthest zoom shows), while fewer than
/// <paramref name="PerHero"/> of the zone are in its <paramref name="Keep"/> box. A piece out of every
/// hero's keep box for <paramref name="Linger"/> ticks, or out of every <paramref name="Hard"/> box at
/// all, is gone.
/// </summary>
public sealed record AroundHeroes(int PerHero, HalfBox RingFrom, HalfBox RingTo, HalfBox View, HalfBox Keep, HalfBox Hard, int Linger) : ZoneAnchor;

/// <summary>A box round a cell: <paramref name="X"/> cells either way across, <paramref name="Y"/> up and down.</summary>
public sealed record HalfBox(int X, int Y);

/// <summary>One kind of thing a container holds: between <paramref name="Min"/> and <paramref name="Max"/> of <paramref name="Item"/>, in one of <paramref name="OneIn"/> of them.</summary>
public sealed record LootEntry(string Item, int Min, int Max, int? OneIn = null);

/// <summary>
/// What a container opens into when caught, or a wreck breaks into, drawn entry by entry in order from the world's Rng: the
/// count first, in [Min, Max], after the one-in roll when there is one.
/// </summary>
public static class Loot
{
    public static readonly IReadOnlyList<(string Container, IReadOnlyList<LootEntry> Entries)> All =
    [
        ("cargoCrate",
        [
            new("planks", 3, 6),
        ]),
        ("supplyPod",
        [
            new("canister", 1, 1),
            new("scrap", 6, 6),
        ]),
        // Wreckage breaks into what it was made of (Wreckage).
        ("hullSection",
        [
            new("hullPlate", 2, 3),
            new("scrapChunk", 2, 4),
        ]),
        ("cargoContainer",
        [
            new("planks", 8, 12),
            new("scrapChunk", 1, 2),
            new("gear", 1, 2, OneIn: 4),
            new("wire", 2, 4, OneIn: 4),
        ]),
        ("machineBlock",
        [
            new("gear", 2, 4),
            new("hullPlate", 1, 2),
            new("scrapChunk", 1, 2),
            new("rotor", 1, 1, OneIn: 4),
        ]),
        // The only source of circuits: one a rack, always, so three racks make a solar panel.
        ("cableRack",
        [
            new("wire", 6, 10),
            new("scrapChunk", 1, 2),
            new("circuit", 1, 1),
        ]),
        ("tankCluster",
        [
            new("canister", 2, 3),
            new("scrapChunk", 1, 1),
        ]),
        ("shieldSlab",
        [
            new("hullPlate", 5, 7),
            new("scrapChunk", 1, 2),
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
