namespace Stillwake.Content;

/// <summary>
/// A type of planet (0095): a kind of surface whose ground grows from each planet's seed. Its rules are the kind's — gravity,
/// air, sky, what may be built — and with them what its ground grows from: the terrain's numbers, the materials of its strata,
/// what grows on its surface and in its caves by depth band. A planet is a type and a seed; its looks are the client's, keyed
/// by the type's id.
/// </summary>
/// <param name="Ground">Its terrain (0077): size, hills, caves, landmarks, ruins' counts and places, its core's rows.</param>
public sealed record PlanetType(string Id, Ground Ground) : Surfaces.Kind(Id, [])
{
    /// <summary>The material of its top layer, over <see cref="Stone"/>: what plants grow on (<see cref="Materials"/>).</summary>
    public required string Soil { get; init; }

    /// <summary>The material of the rock under its soil, where the caves' rock growths lie.</summary>
    public required string Stone { get; init; }

    /// <summary>The material of its floor under the core and its sides, the grid's whole height: nothing breaks it.</summary>
    public required string Bedrock { get; init; }

    /// <summary>The material of its core, <see cref="Ground.LavaRows"/> over the bedrock (0087).</summary>
    public required string Core { get; init; }

    /// <summary>What grows on its surface (0079): kinds of <see cref="Flora.All"/>, in the order they are drawn, with their chances.</summary>
    public required IReadOnlyList<Flora.Kind> Plants { get; init; }

    /// <summary>What grows in its caves (0080), veins among them: kinds of <see cref="Flora.Cave"/>, a chance for each of its bands.</summary>
    public required IReadOnlyList<Flora.CaveKind> Caves { get; init; }

    /// <summary>
    /// Its depth bands, strata of the ground (0080): the bottom of each but the last in thousandths of the way from the
    /// surface's middle row (<c>Ground.Surface</c>) down to the bedrock; one band more than these.
    /// </summary>
    public required IReadOnlyList<int> Bands { get; init; }

    /// <summary>
    /// How its caves' growth gathers: chances scaled by a smooth noise, a swell every <see cref="PatchWave"/> cells, from nothing
    /// to <see cref="PatchMost"/> percent, so it comes in patches and clusters and much of the rock stays bare.
    /// </summary>
    public required int PatchWave { get; init; }

    public required int PatchMost { get; init; }

    /// <summary>In a great hall chances are this many percent of a tunnel's.</summary>
    public required int Hall { get; init; }

    /// <summary>The veins of ore in its rock, painted in this order: kinds of <see cref="Vein"/>.</summary>
    public IReadOnlyList<Vein> Veins { get; init; } = [];

    /// <summary>
    /// Its lodes, the stretches of rock rich in veins and crystals: where a smooth noise, a swell every <see cref="LodeWave"/>
    /// cells, passes <see cref="LodeOver"/>. Its veins run there, thickest at a lode's heart, and its crystals and geodes
    /// (<see cref="Flora.CaveKind.Lode"/>) grow only there, their chances scaled from nothing at its edge to <see cref="LodeMost"/>
    /// percent at its heart, so they gather in rich caves with bare ones between.
    /// </summary>
    public required int LodeWave { get; init; }

    public required int LodeOver { get; init; }

    public required int LodeMost { get; init; }

    /// <summary>What lives on it (0102): each species where it spawns, drawn by weight; none for a lifeless type.</summary>
    public IReadOnlyList<Content.Fauna.Habitat> Fauna { get; init; } = [];
}

/// <summary>The planet types there are (0095): each a surface kind a world may open (<see cref="Surfaces.All"/>).</summary>
public static class PlanetTypes
{
    /// <summary>
    /// The first planet (0077, 0079, 0080, 0085, 0087, 0091). Gravity two thirds of the ship's, so a hero jumps higher and
    /// farther; thin air, the suit paying one breath in five outside; soft, warm daylight, the "Stillwake" style's; no debris;
    /// ladders, floors, walls, back walls and doors built, no machines. Its ground 4096 × 1536 cells (8 px, 0104), some minutes' walk
    /// across, its surface a fifth of the way down under its sky, some twelve hundred rows of ground under that: hills of soil over stone, caves
    /// denser deeper, lava over bedrock. Its caves in four bands: the cold, ice-blue (above the surface's middle too); the garden,
    /// teal and violet, the busiest; the amber deep; and the ember band over the bedrock, its rock cracked with heat. The halls
    /// are gardens, the tunnels sparse. Opened by <c>--planet</c> and the dev panel till the drop pod.
    /// </summary>
    public static readonly PlanetType First = new("planet", new Ground(4096, 1536)
    {
        Surface = 320,
        RegionWave = 1024,
        RegionLeast = 10,
        RegionLift = 64,
        RangeWave = 768,
        RangeRise = 88,
        TerraceStep = 10,
        HillWave = 128,
        HillRise = 24,
        BumpWave = 24,
        BumpRise = 4,
        SoilLeast = 6,
        SoilMost = 16,
        SoilWave = 48,
        CaveFrom = 24,
        MouthWave = 80,
        MouthOver = 44000,
        MouthFadeLeast = 6,
        MouthFadeMost = 12,
        TunnelWave = 60,
        TunnelTall = 36,
        TunnelTop = 2200,
        TunnelBottom = 4200,
        CavernWave = 28,
        CavernTop = 49000,
        CavernBottom = 41500,
        Chasms = 4,
        ChasmWide = 24,
        ChasmDeep = 520,
        Halls = 6,
        HallWide = 144,
        HallTall = 56,
        HallFrom = 160,
        Clear = 240,
        Spires = 4,
        RuinClear = 768,
        SpireRise = 20,
        Homes = 3,
        HomeClear = 768,
        HomeDeepLeast = 16,
        HomeDeepMost = 40,
        LavaRows = 4,
        BedrockRows = 2,
        BedrockSide = 2,
    })
    {
        Gravity = 700,
        ThinAir = 5,
        Sky = new Surfaces.Light(1000, 970, 900),
        Debris = false,
        Builds = ["wall", "backWall", "floor", "ladder", "door", "hatch"],
        Soil = "soil",
        Stone = "stone",
        Bedrock = "bedrock",
        Core = Lava.Material,
        Plants = Flora.All,
        Caves = Flora.Cave,
        Bands = [250, 600, 850],
        PatchWave = 24,
        PatchMost = 220,
        Hall = 700,
        // Ice veins in the cold band, copper below it, richest in the amber deep: as their growths were (0080).
        Veins =
        [
            new("iceVein", Wave: 40, Tall: 24, Widths: [1800, 0, 0, 0]),
            new("copperVein", Wave: 40, Tall: 24, Widths: [0, 1400, 2400, 1800]),
        ],
        LodeWave = 96,
        LodeOver = 42000,
        LodeMost = 1000,
        // Neutral creatures only on the first planet (the user's word), spawned by habitat (the user's rule, 2026-10-22): the
        // loper, the tapir and the sixleg on the open ground; the fluff on cave floors of the cold band and, more, the garden's.
        // Nothing yet in the amber and ember bands: their kinds come later.
        Fauna =
        [
            new(Content.Fauna.Loper.Id, Content.Fauna.Place.Surface, 3),
            new(Content.Fauna.Tapir.Id, Content.Fauna.Place.Surface, 2),
            new(Content.Fauna.Sixleg.Id, Content.Fauna.Place.Surface, 2),
            new(Content.Fauna.Fluff.Id, Content.Fauna.Place.Band, 2, Band: 0),
            new(Content.Fauna.Fluff.Id, Content.Fauna.Place.Band, 3, Band: 1),
        ],
    };

    /// <summary>
    /// The first planet grown small, 128 × 96, for tests and the fuzz: the same generator, a fraction of the cells. Low hills,
    /// caves a little nearer the surface, a chasm and a hall of its size, one spire (0085), so the fuzz digs alloy, and one home
    /// (0091), its scheme by the seed, anywhere it fits beside the spire, for a home is wider than the columns left beside a
    /// clear middle there.
    /// </summary>
    public static readonly PlanetType Small = First with
    {
        Id = "smallPlanet",
        LodeWave = 24,
        Ground = First.Ground with
        {
            Width = 128,
            Height = 96,
            Surface = 28,
            RangeWave = 96,
            RangeRise = 8,
            TerraceStep = 4,
            RegionWave = 64,
            RegionLift = 6,
            HillWave = 48,
            HillRise = 6,
            CaveFrom = 16,
            Chasms = 1,
            ChasmWide = 6,
            ChasmDeep = 32,
            Halls = 1,
            HallWide = 12,
            HallTall = 8,
            HallFrom = 28,
            Clear = 16,
            Spires = 1,
            RuinClear = 24,
            Homes = 1,
            HomeClear = 0,
            HomeDeepLeast = 8,
            HomeDeepMost = 20,
        },
    };

    /// <summary>Every type, in the order <see cref="Surfaces.All"/> lists them; ids never reused, for saves hold them.</summary>
    public static readonly IReadOnlyList<PlanetType> All = [First, Small];
}
