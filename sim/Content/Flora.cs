namespace Stillwake.Content;

/// <summary>
/// What grows on a planet (0079, 0080): on its surface, the kinds of plant and the stones lying about that the ground's
/// generator (<c>Terrain</c>) sets on open soil under the sky; in its caves, the glowing things, crystals and stone it sets on
/// their floors, from their ceilings and in the rock round them, by depth band; and the pieces of the ruins' homes (0091). Each
/// stands in <c>Across</c> columns from its own cell, <c>Tall</c> cells of each, two by two at the least (0104); the client draws it as large as it is. A hit fells one, or breaking out the wall it holds on to, giving its <see cref="Growth.Yields"/>.
/// </summary>
public static class Flora
{
    /// <summary>What a growth holds on to: the wall under it, the wall over it, or the rock it is set in.</summary>
    public enum Anchor
    {
        /// <summary>
        /// Stands in an open cell on the wall under it, <c>Tall</c> cells up, or on a piece standing under it (0091: dishes on a
        /// table, a kettle on a stool).
        /// </summary>
        Floor,

        /// <summary>Hangs in an open cell from the wall over it, <c>Tall</c> cells down.</summary>
        Ceiling,

        /// <summary>Lies in a natural wall's own cell beside a cave: it goes when the wall is dug.</summary>
        Rock,

        /// <summary>Hangs on the back wall of its open cells, <c>Tall</c> cells down (0091): a painting, a shelf, a sconce.</summary>
        Back,
    }

    /// <summary>A kind of growth, on the surface or in the caves: what Building needs of any of them.</summary>
    /// <param name="Tall">The cells it stands in, its own and up (on a floor) or down (from a ceiling): a hit on any of them hits it.</param>
    public abstract record Growth(string Id, int Tall)
    {
        /// <summary>
        /// The columns it stands in (0091), its own and to the right, each holding on to the wall under it (over it from a
        /// ceiling, its own in the rock): 2, a 16 px column of 8 px cells (0104), for all but a ruin's wider pieces.
        /// </summary>
        public int Across { get; init; } = 2;

        /// <summary>
        /// Behind what stands in front (0091): a back wall's pieces, a rug. A cell holds one growth in front and one behind; a hit
        /// takes the front one first.
        /// </summary>
        public bool Behind { get; init; }

        /// <summary>Painted on or cut into the back wall (0091): no hit takes it.</summary>
        public bool Fixed { get; init; }

        /// <summary>
        /// What felling one gives the hero who did, by a hit or by breaking out what holds it: fixed counts, no draw.
        /// Nothing for what is only to look at: flowers, grass, stalactites, stalagmites, threads, roots, moss, fossils.
        /// </summary>
        public IReadOnlyList<(string Item, int Count)> Yields { get; init; } = [];

        /// <summary>
        /// Its hit points, as a natural wall's strength (0077): the damage of weapons' hits, cracking it by its own cell, that
        /// fells it. 0 for one that falls to any hit: flowers, grass, the caves' growth.
        /// </summary>
        public int Strength { get; init; }

        /// <summary>What it holds on to.</summary>
        public abstract Anchor On { get; }
    }

    /// <summary>
    /// A kind of plant or stone on the surface. In a column with room for it, the generator draws a number under 1000 and sets
    /// the kind whose <c>Chance</c>, in this list's order, it falls in, when it has room: the ground level <c>Level</c> columns
    /// either side, open sky <c>Clear</c> rows above its cell across <c>Room</c> columns either side, none of its kind nearer
    /// than <c>Apart</c> columns.
    /// </summary>
    public sealed record Kind(string Id, int Tall, int Level, int Room, int Clear, int Chance, int Apart) : Growth(Id, Tall)
    {
        public override Anchor On => Anchor.Floor;
    }

    /// <summary>
    /// The surface's growths, the catalog every planet type takes its plants from (0095), with the first planet's chances: an
    /// acacia with hanging fronds and glowing seed pods, a bush of glowing berries, a rock, bell flowers glowing in three
    /// colours, a tuft of grass. Strengths are seconds of the axe's 4 a second: a tree 3, a rock 2,
    /// a bush 1. A tree's 10 planks are more than any cargo crate's 3 to 6.
    /// </summary>
    public static readonly IReadOnlyList<Kind> All =
    [
        new("acacia", Tall: 18, Level: 2, Room: 6, Clear: 24, Chance: 260, Apart: 24) { Yields = [("planks", 10)], Strength = 12 },
        new("bush", Tall: 2, Level: 2, Room: 4, Clear: 4, Chance: 160, Apart: 10) { Yields = [("seed", 1)], Strength = 4 },
        new("rock", Tall: 2, Level: 2, Room: 2, Clear: 4, Chance: 65, Apart: 12) { Yields = [("rock", 2)], Strength = 8 },
        new("bellTeal", Tall: 2, Level: 0, Room: 0, Clear: 2, Chance: 100, Apart: 6),
        new("bellViolet", Tall: 2, Level: 0, Room: 0, Clear: 2, Chance: 80, Apart: 6),
        new("bellAmber", Tall: 2, Level: 0, Room: 0, Clear: 2, Chance: 75, Apart: 8),
        new("tuft", Tall: 2, Level: 0, Room: 0, Clear: 2, Chance: 150, Apart: 4),
    ];

    /// <summary>
    /// A kind of cave growth (0080). In each slot of its anchor — an open cave cell over a wall, one under a whole wall,
    /// or a natural wall beside a cave — the generator draws a number under 10000 and sets the kind whose chance there, in
    /// this list's order among those of that anchor, it falls in: <c>Chances</c> by its planet type's depth band
    /// (<see cref="PlanetType.Bands"/>), scaled by the patch noise there (<see cref="PlanetType.PatchMost"/>), so they gather,
    /// and in a great hall by <see cref="PlanetType.Hall"/>. It needs
    /// its <c>Tall</c> cells open and free, the cells <c>Wide</c> either side open over the same wall, walls
    /// all (Terraria's rule) — and none of its kind nearer than <c>Apart</c> cells along the row; one on
    /// a floor or a ceiling, nothing of its anchor in the cell before it.
    /// </summary>
    /// <param name="Chances">In ten-thousandths of a slot, in each band, the coldest first.</param>
    public sealed record CaveKind(string Id, Anchor Holds, int Tall, int Wide, int Apart, IReadOnlyList<int> Chances) : Growth(Id, Tall)
    {
        public override Anchor On => Holds;

        /// <summary>When set, only this many rows under its column's surface or fewer: the roots hanging near it.</summary>
        public int? Shallow { get; init; }

        /// <summary>In the rock, only in a floor: a wall with a cave cell over it. The heat's cracks run along floors, not up walls.</summary>
        public bool Floors { get; init; }
    }

    /// <summary>
    /// The caves' growths (0080), the catalog every planet type takes its caves' from (0095), with the first planet's chances
    /// in its four bands; by anchor, the rarest first within each, so a dense patch does not crowd them out but for mica. Teal and
    /// hanging caps give biomass, violet ones spores; ice crystals and veins ice, the others crystal, a geode six; copper veins
    /// ore; mica its flakes; the heat's cracks magnetite.
    /// </summary>
    public static readonly IReadOnlyList<CaveKind> Cave =
    [
        // On floors: glowing mushrooms in clusters, crystals by band, stalagmites.
        new("crystalRose", Anchor.Floor, Tall: 2, Wide: 0, Apart: 16, Chances: [0, 0, 160, 0]) { Yields = [("crystal", 1)] },
        new("crystalIce", Anchor.Floor, Tall: 2, Wide: 0, Apart: 12, Chances: [380, 40, 0, 0]) { Yields = [("ice", 1)] },
        new("crystalAmber", Anchor.Floor, Tall: 2, Wide: 0, Apart: 12, Chances: [0, 0, 380, 40]) { Yields = [("crystal", 1)] },
        new("crystalEmber", Anchor.Floor, Tall: 2, Wide: 0, Apart: 12, Chances: [0, 0, 30, 380]) { Yields = [("crystal", 1)] },
        new("glowcapViolet", Anchor.Floor, Tall: 2, Wide: 0, Apart: 8, Chances: [0, 220, 160, 0]) { Yields = [("spore", 1)] },
        new("glowcapTeal", Anchor.Floor, Tall: 2, Wide: 0, Apart: 6, Chances: [30, 420, 90, 15]) { Yields = [("biomass", 2)] },
        new("stalagmite", Anchor.Floor, Tall: 2, Wide: 0, Apart: 8, Chances: [400, 240, 320, 320]),
        // From ceilings: roots near the surface, crystals, the garden's threads and hanging caps, stalactites that drip.
        new("crystalIceHang", Anchor.Ceiling, Tall: 2, Wide: 0, Apart: 12, Chances: [320, 30, 0, 0]) { Yields = [("ice", 1)] },
        new("crystalAmberHang", Anchor.Ceiling, Tall: 2, Wide: 0, Apart: 12, Chances: [0, 0, 280, 60]) { Yields = [("crystal", 1)] },
        new("hangcap", Anchor.Ceiling, Tall: 2, Wide: 0, Apart: 14, Chances: [0, 200, 60, 0]) { Yields = [("biomass", 2)] },
        new("threadTeal", Anchor.Ceiling, Tall: 4, Wide: 0, Apart: 10, Chances: [30, 320, 0, 0]),
        new("threadViolet", Anchor.Ceiling, Tall: 4, Wide: 0, Apart: 10, Chances: [0, 500, 90, 0]),
        new("roots", Anchor.Ceiling, Tall: 4, Wide: 0, Apart: 4, Chances: [2000, 0, 0, 0]) { Shallow = 48 },
        new("stalactite", Anchor.Ceiling, Tall: 2, Wide: 0, Apart: 6, Chances: [700, 460, 620, 540]),
        // In the rock by a cave: a rare geode, fossils, veins, the heat's glowing cracks along the floors over the bedrock, glow
        // moss on its faces, and last mica that glints in a beam, which gives way where the moss fills a hall's walls.
        new("geode", Anchor.Rock, Tall: 2, Wide: 0, Apart: 80, Chances: [0, 4, 10, 4]) { Yields = [("crystal", 6)] },
        new("fossil", Anchor.Rock, Tall: 2, Wide: 0, Apart: 40, Chances: [0, 30, 50, 0]),
        new("veinIce", Anchor.Rock, Tall: 2, Wide: 0, Apart: 4, Chances: [360, 0, 0, 0]) { Yields = [("ice", 2)] },
        new("veinCopper", Anchor.Rock, Tall: 2, Wide: 0, Apart: 4, Chances: [0, 60, 360, 140]) { Yields = [("copperOre", 2)] },
        new("heatCrack", Anchor.Rock, Tall: 2, Wide: 0, Apart: 2, Chances: [0, 0, 0, 2400]) { Floors = true, Yields = [("magnetite", 2)] },
        new("moss", Anchor.Rock, Tall: 2, Wide: 0, Apart: 2, Chances: [360, 2000, 500, 0]),
        new("mica", Anchor.Rock, Tall: 2, Wide: 0, Apart: 4, Chances: [1600, 900, 1400, 800]) { Yields = [("mica", 1)] },
        // The ruins' (0085, Content.Ruins): never grown, no chance in any band, set where a spire is stamped. A cache on the
        // floor of its chamber, the lens in its tip's alloy; its relic's cradle is a piece (0096).
        new(Ruins.SpireCache, Anchor.Floor, Tall: 2, Wide: 0, Apart: 2, Chances: [0, 0, 0, 0]) { Yields = [("alloyScrap", 4), ("wire", 6), ("silicon", 2)] },
        new(Ruins.SpireLamp, Anchor.Rock, Tall: 2, Wide: 0, Apart: 2, Chances: [0, 0, 0, 0]),
    ];

    /// <summary>
    /// A piece of a ruin's furnishing (0091, <see cref="Ruins.Pieces"/>): never grown, set where a home is stamped, held by
    /// <paramref name="Holds"/>, <c>Across</c> columns and <c>Tall</c> rows.
    /// </summary>
    public sealed record Piece(string Id, Anchor Holds, int Tall) : Growth(Id, Tall)
    {
        public override Anchor On => Holds;
    }

    /// <summary>Every growth there is: the surface's, the caves', the ruins' pieces.</summary>
    public static IEnumerable<Growth> Every => All.Concat<Growth>(Cave).Concat(Ruins.Pieces);

    /// <summary>The kind <paramref name="id"/>, of the surface, the caves or a ruin's pieces; one not here is our bug.</summary>
    public static Growth Get(string id) =>
        (Growth?)All.FirstOrDefault(k => k.Id == id) ?? (Growth?)Cave.FirstOrDefault(k => k.Id == id) ?? Ruins.Pieces.FirstOrDefault(k => k.Id == id)
        ?? throw new KeyNotFoundException($"No plant '{id}'.");
}
