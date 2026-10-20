namespace Stillwake.Content;

/// <summary>
/// What grows on a planet (0079, 0080): on its surface, the kinds of plant and the stones lying about that the ground's
/// generator (<c>Terrain</c>) sets on open soil under the sky; in its caves, the glowing things, crystals and stone it sets on
/// their floors, from their ceilings and in the rock round them, by depth band. Each stands in one cell, its own; the client
/// draws it as large as it is. Cosmetic for now: a hit fells one, giving its <see cref="Growth.Yields"/>.
/// </summary>
public static class Flora
{
    /// <summary>What a growth holds on to: the wall under it, the wall over it, or the rock it is set in.</summary>
    public enum Anchor
    {
        /// <summary>Stands in an open cell on the wall under it, <c>Tall</c> cells up.</summary>
        Floor,

        /// <summary>Hangs in an open cell from the wall over it, <c>Tall</c> cells down.</summary>
        Ceiling,

        /// <summary>Lies in a natural wall's own cell beside a cave: it goes when the wall is dug.</summary>
        Rock,
    }

    /// <summary>A kind of growth, on the surface or in the caves: what Building needs of any of them.</summary>
    /// <param name="Tall">The cells it stands in, its own and up (on a floor) or down (from a ceiling): a hit on any of them hits it.</param>
    public abstract record Growth(string Id, int Tall)
    {
        /// <summary>
        /// What felling one gives the hero who did: nothing yet. The hook where trees give wood and seeds, glowcaps biomass
        /// and spores, crystals their shards.
        /// </summary>
        public IReadOnlyList<(string Item, int Count)> Yields { get; init; } = [];

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
    /// The first planet's: an acacia with hanging fronds and glowing seed pods, a bush of glowing berries, a rock, bell
    /// flowers glowing in three colours, a tuft of grass; most columns of open soil bare.
    /// </summary>
    public static readonly IReadOnlyList<Kind> All =
    [
        new("acacia", Tall: 9, Level: 1, Room: 3, Clear: 12, Chance: 80, Apart: 30),
        new("bush", Tall: 1, Level: 1, Room: 2, Clear: 2, Chance: 55, Apart: 10),
        new("rock", Tall: 1, Level: 1, Room: 1, Clear: 2, Chance: 22, Apart: 16),
        new("bellTeal", Tall: 1, Level: 0, Room: 0, Clear: 1, Chance: 40, Apart: 4),
        new("bellViolet", Tall: 1, Level: 0, Room: 0, Clear: 1, Chance: 30, Apart: 5),
        new("bellAmber", Tall: 1, Level: 0, Room: 0, Clear: 1, Chance: 25, Apart: 6),
        new("tuft", Tall: 1, Level: 0, Room: 0, Clear: 1, Chance: 150, Apart: 2),
    ];

    /// <summary>
    /// A kind of cave growth (0080). In each slot of its anchor — an open cave cell over a wall, one under a whole wall,
    /// or a natural wall beside a cave — the generator draws a number under 10000 and sets the kind whose chance there, in
    /// this list's order among those of that anchor, it falls in: <c>Chances</c> by depth band (<see cref="Bands"/>), scaled
    /// by the patch noise there (<see cref="PatchMost"/>), so they gather, and in a great hall by <see cref="Hall"/>. It needs
    /// its <c>Tall</c> cells open and free, the cells <c>Wide</c> either side open over the same wall — a wide one only on whole
    /// walls, a narrow one on a floor's slope too — and none of its kind nearer than <c>Apart</c> cells along the row; one on
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
    /// The depth bands, strata of the ground (0080): the bottom of each but the last in thousandths of the way from the
    /// surface's middle row (<c>Ground.Surface</c>) down to the bedrock. The cold band, ice-blue (above the surface's middle
    /// too); the garden, teal and violet, the busiest; the amber deep; and the ember band over the bedrock, its rock cracked
    /// with heat.
    /// </summary>
    public static readonly IReadOnlyList<int> Bands = [250, 600, 850];

    /// <summary>
    /// How growth gathers: chances scaled by a smooth noise, a swell every <see cref="PatchWave"/> cells, from nothing to
    /// <see cref="PatchMost"/> percent, so it comes in patches and clusters and much of the rock stays bare.
    /// </summary>
    public const int PatchWave = 12, PatchMost = 220;

    /// <summary>In a great hall chances are this many percent of a tunnel's: the halls are gardens, tunnels sparse.</summary>
    public const int Hall = 700;

    /// <summary>
    /// The caves' (0080), by anchor, the rarest first within each, so a dense patch does not crowd them out but for mica. The hooks for
    /// what comes, through <see cref="Growth.Yields"/>, empty till then: glowcaps give biomass and spores, crystals shards, a
    /// geode its crystals, the veins copper and ice once those are materials.
    /// </summary>
    public static readonly IReadOnlyList<CaveKind> Cave =
    [
        // On floors: glowing mushrooms in clusters, crystals by band, stalagmites.
        new("crystalRose", Anchor.Floor, Tall: 1, Wide: 0, Apart: 8, Chances: [0, 0, 160, 0]),
        new("crystalIce", Anchor.Floor, Tall: 1, Wide: 0, Apart: 6, Chances: [380, 40, 0, 0]),
        new("crystalAmber", Anchor.Floor, Tall: 1, Wide: 0, Apart: 6, Chances: [0, 0, 380, 40]),
        new("crystalEmber", Anchor.Floor, Tall: 1, Wide: 0, Apart: 6, Chances: [0, 0, 30, 380]),
        new("glowcapViolet", Anchor.Floor, Tall: 1, Wide: 0, Apart: 4, Chances: [0, 220, 160, 0]),
        new("glowcapTeal", Anchor.Floor, Tall: 1, Wide: 0, Apart: 3, Chances: [30, 420, 90, 15]),
        new("stalagmite", Anchor.Floor, Tall: 1, Wide: 0, Apart: 4, Chances: [400, 240, 320, 320]),
        // From ceilings: roots near the surface, crystals, the garden's threads and hanging caps, stalactites that drip.
        new("crystalIceHang", Anchor.Ceiling, Tall: 1, Wide: 0, Apart: 6, Chances: [320, 30, 0, 0]),
        new("crystalAmberHang", Anchor.Ceiling, Tall: 1, Wide: 0, Apart: 6, Chances: [0, 0, 280, 60]),
        new("hangcap", Anchor.Ceiling, Tall: 1, Wide: 0, Apart: 7, Chances: [0, 200, 60, 0]),
        new("threadTeal", Anchor.Ceiling, Tall: 2, Wide: 0, Apart: 5, Chances: [30, 320, 0, 0]),
        new("threadViolet", Anchor.Ceiling, Tall: 2, Wide: 0, Apart: 5, Chances: [0, 500, 90, 0]),
        new("roots", Anchor.Ceiling, Tall: 2, Wide: 0, Apart: 2, Chances: [2000, 0, 0, 0]) { Shallow = 24 },
        new("stalactite", Anchor.Ceiling, Tall: 1, Wide: 0, Apart: 3, Chances: [700, 460, 620, 540]),
        // In the rock by a cave: a rare geode, fossils, veins, the heat's glowing cracks along the floors over the bedrock, glow
        // moss on its faces, and last mica that glints in a beam, which gives way where the moss fills a hall's walls.
        new("geode", Anchor.Rock, Tall: 1, Wide: 0, Apart: 40, Chances: [0, 4, 10, 4]),
        new("fossil", Anchor.Rock, Tall: 1, Wide: 0, Apart: 20, Chances: [0, 30, 50, 0]),
        new("veinIce", Anchor.Rock, Tall: 1, Wide: 0, Apart: 2, Chances: [360, 0, 0, 0]),
        new("veinCopper", Anchor.Rock, Tall: 1, Wide: 0, Apart: 2, Chances: [0, 60, 360, 140]),
        new("heatCrack", Anchor.Rock, Tall: 1, Wide: 0, Apart: 1, Chances: [0, 0, 0, 2400]) { Floors = true },
        new("moss", Anchor.Rock, Tall: 1, Wide: 0, Apart: 1, Chances: [360, 2000, 500, 0]),
        new("mica", Anchor.Rock, Tall: 1, Wide: 0, Apart: 2, Chances: [1600, 900, 1400, 800]),
    ];

    /// <summary>The kind <paramref name="id"/>, of the surface or the caves; one not here is our bug.</summary>
    public static Growth Get(string id) =>
        (Growth?)All.FirstOrDefault(k => k.Id == id) ?? Cave.FirstOrDefault(k => k.Id == id) ?? throw new KeyNotFoundException($"No plant '{id}'.");
}
