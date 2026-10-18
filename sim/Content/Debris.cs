namespace Stillwake.Content;

/// <summary>
/// Debris drifting by (ADRs 0006, 0009 and 0052), in <see cref="Zones"/>, each spawning on a beat of its
/// own by its own anchor. Times are in ticks, positions and speeds in thousandths of a cell, distances
/// in cells. The stream past the ship (<see cref="Ship"/>): a piece spawns <see cref="Every"/> ticks plus
/// up to <see cref="Jitter"/> more after the last, while fewer than <see cref="Max"/> of its own are out:
/// <see cref="Lead"/> cells right of the hull, or farther right past every hero's <see cref="View"/> that
/// holds that point, in a row of the hull's own and <see cref="Margin"/> more above and below; it drifts
/// left at a speed of its own between <see cref="DriftMin"/> and <see cref="DriftMax"/> a tick, and is gone
/// <see cref="Lead"/> cells left of the hull once no hero sees it. Its kind is drawn by weight from
/// <see cref="Kinds"/>. The wreck's world begins with <see cref="AtStart"/> out, each already somewhere
/// along its way, so a player waits for none. The far field (<see cref="Far"/>) spawns round every hero
/// out past the stream.
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
    /// What the farthest zoom shows round a hero's reported cell, in cells either way: 1920×1080 at zoom 1,
    /// 16 pixels a cell. No zone spawns a piece in any hero's, nor takes one away in it but at the grid's edge.
    /// </summary>
    public static readonly HalfBox View = new(60, 34);

    /// <summary>
    /// How far from the pointer, in thousandths of a cell, a piece is still aimed at: a piece drifting by
    /// is small and moving, and a pointer exactly on it is hard to keep.
    /// </summary>
    public const int AimRadius = 1500;

    /// <summary>
    /// The stream's kinds by weight: its one tier. Three times 0041's, so the rare soil clod fits in whole numbers
    /// with the same odds for the rest (as 0056 scaled the far field's).
    /// </summary>
    public static readonly IReadOnlyList<(string Item, int Weight)> Kinds =
    [
        ("scrapChunk", 54),
        ("hullPlate", 27),
        ("cargoCrate", 18),
        ("supplyPod", 9),
        // Wreckage (Wreckage): 27 of 137, one spawn in five, a wreck about every 50 s.
        ("hullSection", 9),
        ("cargoContainer", 6),
        ("machineBlock", 3),
        ("cableRack", 3),
        ("tankCluster", 3),
        ("shieldSlab", 3),
        // Seeds' second source beside supply pods (0060): 2 of 137 of some 350 spawns an hour, one every 12 minutes or so.
        ("soilClod", 2),
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
    /// ship; the farther out, the more and the bigger the wrecks, and the circuits with them. Out past 128
    /// the derelicts (0056), one spawn in some 80, and past 256 one in 30, with big and rich rock: a sortie's
    /// prize, one or two a sortie (<c>DebrisDensityTests</c>). The weights are 0052's four times over, so the
    /// rare fit in whole.
    /// </summary>
    public static readonly DebrisZone Far = new("far",
        new AroundHeroes(PerHero: 110, RingFrom: new(61, 36), RingTo: new(64, 42), Keep: new(68, 48), Hard: new(100, 64), Linger: 600),
        Every: 1, Jitter: 0, Max: 640, DriftMin, DriftMax,
        [
            new(0,
            [
                ("scrapChunk", 48), ("hullPlate", 32), ("cargoCrate", 24), ("supplyPod", 16), ("hullSection", 16),
                ("cargoContainer", 12), ("machineBlock", 8), ("cableRack", 8), ("tankCluster", 8), ("shieldSlab", 4),
            ]),
            new(128,
            [
                ("scrapChunk", 24), ("hullPlate", 24), ("cargoCrate", 20), ("supplyPod", 20), ("hullSection", 16),
                ("cargoContainer", 16), ("machineBlock", 12), ("cableRack", 12), ("tankCluster", 12), ("shieldSlab", 8),
                ("engineBlock", 1), ("cargoHold", 1),
            ]),
            new(256,
            [
                ("scrapChunk", 8), ("hullPlate", 12), ("cargoCrate", 12), ("supplyPod", 20), ("hullSection", 12),
                ("cargoContainer", 16), ("machineBlock", 16), ("cableRack", 20), ("tankCluster", 16), ("shieldSlab", 12),
                ("engineBlock", 2), ("cargoHold", 1), ("podCluster", 2), ("asteroidLarge", 2), ("asteroidRich", 2),
            ]),
        ]);

    /// <summary>
    /// The asteroid band (0054): rock in two bands 22 to 44 rows above the hull's top and below its bottom,
    /// past the tether's 12 cells, so only a jetpack reaches it. Rare: a beat every 3 to 5 minutes, 4 on
    /// average, at most 2 out; drifting from the stream's start to its end as the stream does. Small and
    /// middling rock nearer; on its far edge, rows 36 to 44 out, more large and the rich (0056). Every piece
    /// of the band spawns <see cref="Lead"/> (64) columns right of the hull, so its distance is twice its rows
    /// past it once they pass 32: the far edge is the tier from 72.
    /// </summary>
    public static readonly DebrisZone Asteroids = new("asteroids", new HullBand(From: 22, To: 44, Lead), Every: 3600, Jitter: 2400, Max: 2,
        DriftMin, DriftMax,
        [
            new(0, [("asteroidSmall", 3), ("asteroid", 4), ("asteroidLarge", 1)]),
            new(2 * 36, [("asteroidSmall", 1), ("asteroid", 3), ("asteroidLarge", 3), ("asteroidRich", 2)]),
        ]);

    /// <summary>Every zone, in the order they spawn on a tick: the stream first, so a world with no hero out draws as it always did.</summary>
    public static readonly IReadOnlyList<DebrisZone> Zones = [Ship, Far, Asteroids];
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
/// cells right of it to as far left of it. That box, the hull grown so, is the stream's. A piece starts
/// farther right past every hero's <see cref="Debris.View"/> that holds its start, so none sees it appear.
/// </summary>
public sealed record ByHull(int Margin, int Lead) : ZoneAnchor;

/// <summary>
/// By the hull, in two bands with a gap: the rows <paramref name=From/> to <paramref name=To/> beyond
/// its top row and as far below its bottom row, from <paramref name=Lead/> cells right of it to as far left
/// of it (0054), starting farther right as the stream's pieces do. Not a stream's box: the far field
/// spawns in it as anywhere.
/// </summary>
public sealed record HullBand(int From, int To, int Lead) : ZoneAnchor;

/// <summary>
/// Round every hero out past every stream's box, at the cell it last reported (0052): on a ring from
/// half-box <paramref name="RingFrom"/> out to <paramref name="RingTo"/> round it, never in a stream's box
/// nor in any hero's <see cref="Debris.View"/>, while fewer than
/// <paramref name="PerHero"/> of the zone are in its <paramref name="Keep"/> box. A piece out of every
/// hero's keep box for <paramref name="Linger"/> ticks, or out of every <paramref name="Hard"/> box at
/// all, is gone.
/// </summary>
public sealed record AroundHeroes(int PerHero, HalfBox RingFrom, HalfBox RingTo, HalfBox Keep, HalfBox Hard, int Linger) : ZoneAnchor;

/// <summary>A box round a cell: <paramref name="X"/> cells either way across, <paramref name="Y"/> up and down.</summary>
public sealed record HalfBox(int X, int Y);

/// <summary>
/// How many looks each kind of debris has, its pictures under the client's <c>art/debris</c>, and the one a piece wears:
/// picked by its id, so every peer draws a piece alike and the Handbook records the look seen (0066). A kind not
/// listed has one look, its item's picture.
/// </summary>
public static class Looks
{
    public static readonly IReadOnlyList<(string Kind, int Count)> All =
    [
        ("scrapChunk", 12), ("hullPlate", 12), ("cargoCrate", 8), ("supplyPod", 4),
        ("hullSection", 1), ("cargoContainer", 1), ("machineBlock", 1), ("cableRack", 1), ("tankCluster", 1), ("shieldSlab", 1),
        ("soilClod", 3),
        ("asteroid", 6), ("asteroidSmall", 6), ("asteroidLarge", 6), ("asteroidRich", 6),
        ("engineBlock", 4), ("cargoHold", 4), ("podCluster", 4),
    ];

    /// <summary>How many looks <paramref name="kind"/> has: one for a kind with no pictures of its own.</summary>
    public static int CountOf(string kind)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Kind == kind) return All[i].Count;
        return 1;
    }

    /// <summary>The look piece <paramref name="piece"/> of <paramref name="kind"/> wears, from 0: its id round its kind's looks.</summary>
    public static int Of(string kind, int piece) => piece % CountOf(kind);
}

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
        // The farm's seeds come from supply pods (0060): one or two in every other pod; and from soil clods.
        ("supplyPod",
        [
            new("canister", 1, 1),
            new("scrap", 6, 6),
            new("seed", 1, 2, OneIn: 2),
        ]),
        // A clod of some ship's farm, its sprout still in it: seeds always, and a little soil.
        ("soilClod",
        [
            new("seed", 1, 3),
            new("soil", 1, 2),
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
        // Circuits' one sure source: one a rack, always, so three racks make a solar panel. Derelicts far out
        // may hold one or two besides (0056), far fewer for the hit points.
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
        ("asteroid",
        [
            new("rock", 3, 5),
        ]),
        // The asteroid's kinds (0056): a hit point buys a little more rock the bigger the rock, and twice it in the rich.
        ("asteroidSmall",
        [
            new("rock", 1, 2),
        ]),
        ("asteroidLarge",
        [
            new("rock", 6, 9),
        ]),
        ("asteroidRich",
        [
            new("rock", 8, 12),
        ]),
        // Derelicts (0056): the wrecks' own loot, some five wrecks' worth in one place, so a sortie's air goes on breaking, not searching.
        ("engineBlock",
        [
            new("gear", 10, 16),
            new("hullPlate", 5, 8),
            new("scrapChunk", 5, 8),
            new("wire", 4, 8),
            new("rotor", 1, 2),
            new("circuit", 1, 1, OneIn: 3),
        ]),
        ("cargoHold",
        [
            new("planks", 36, 54),
            new("hullPlate", 3, 5),
            new("scrapChunk", 4, 6),
            new("gear", 3, 5),
            new("wire", 6, 10, OneIn: 2),
            new("canister", 1, 2, OneIn: 3),
        ]),
        // The farthest prize: air for the way home, and the likeliest circuits after a rack.
        ("podCluster",
        [
            new("canister", 5, 8),
            new("scrapChunk", 5, 8),
            new("hullPlate", 3, 5),
            new("wire", 6, 10),
            new("circuit", 1, 2, OneIn: 2),
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
