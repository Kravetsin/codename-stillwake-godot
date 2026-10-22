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

    /// <summary>What lives on it (0102): each species where it spawns, drawn by weight; none for a lifeless type.</summary>
    public IReadOnlyList<Content.Fauna.Habitat> Fauna { get; init; } = [];
}

/// <summary>The planet types there are (0095): each a surface kind a world may open (<see cref="Surfaces.All"/>).</summary>
public static class PlanetTypes
{
    /// <summary>
    /// The first planet (0077, 0079, 0080, 0085, 0087, 0091). Gravity two thirds of the ship's, so a hero jumps higher and
    /// farther; thin air, the suit paying one breath in five outside; soft, warm daylight, the "Stillwake" style's; no debris;
    /// ladders, floors, walls, back walls and doors built, no machines. Its ground 2048 × 768 cells, some minutes' walk across, its
    /// surface a fifth of the way down under its sky, some six hundred rows of ground under that: hills of soil over stone, caves
    /// denser deeper, lava over bedrock. Its caves in four bands: the cold, ice-blue (above the surface's middle too); the garden,
    /// teal and violet, the busiest; the amber deep; and the ember band over the bedrock, its rock cracked with heat. The halls
    /// are gardens, the tunnels sparse. Opened by <c>--planet</c> and the dev panel till the drop pod.
    /// </summary>
    public static readonly PlanetType First = new("planet", new Ground(2048, 768)
    {
        Surface = 160,
        RangeWave = 384,
        RangeRise = 44,
        CliffStep = 5,
        HillWave = 64,
        HillRise = 12,
        BumpWave = 12,
        BumpRise = 2,
        SoilLeast = 3,
        SoilMost = 8,
        SoilWave = 24,
        CaveFrom = 12,
        MouthWave = 40,
        MouthOver = 44000,
        MouthFadeLeast = 3,
        MouthFadeMost = 6,
        TunnelWave = 30,
        TunnelTall = 18,
        TunnelTop = 2200,
        TunnelBottom = 4200,
        CavernWave = 14,
        CavernTop = 49000,
        CavernBottom = 41500,
        Chasms = 4,
        ChasmWide = 12,
        ChasmDeep = 260,
        Halls = 6,
        HallWide = 72,
        HallTall = 28,
        HallFrom = 80,
        Clear = 120,
        Spires = 4,
        RuinClear = 384,
        SpireRise = 10,
        Homes = 3,
        HomeClear = 384,
        HomeDeepLeast = 8,
        HomeDeepMost = 20,
        LavaRows = 2,
        BedrockRows = 1,
        BedrockSide = 1,
    })
    {
        Gravity = 700,
        ThinAir = 5,
        Sky = new Surfaces.Light(1000, 970, 900),
        Debris = false,
        Builds = ["wall", "backWall", "floor", "ladder", "door"],
        Soil = "soil",
        Stone = "stone",
        Bedrock = "bedrock",
        Core = Lava.Material,
        Plants = Flora.All,
        Caves = Flora.Cave,
        Bands = [250, 600, 850],
        PatchWave = 12,
        PatchMost = 220,
        Hall = 700,
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
    /// The first planet grown small, 64 × 48, for tests and the fuzz: the same generator, a fraction of the cells. Low hills,
    /// caves a little nearer the surface, a chasm and a hall of its size, one spire (0085), so the fuzz digs alloy, and one home
    /// (0091), its scheme by the seed, anywhere it fits beside the spire, for a home is wider than the columns left beside a
    /// clear middle there.
    /// </summary>
    public static readonly PlanetType Small = First with
    {
        Id = "smallPlanet",
        Ground = First.Ground with
        {
            Width = 64,
            Height = 48,
            Surface = 14,
            RangeWave = 48,
            RangeRise = 4,
            CliffStep = 2,
            HillWave = 24,
            HillRise = 3,
            CaveFrom = 8,
            Chasms = 1,
            ChasmWide = 3,
            ChasmDeep = 16,
            Halls = 1,
            HallWide = 6,
            HallTall = 4,
            HallFrom = 14,
            Clear = 8,
            Spires = 1,
            RuinClear = 12,
            Homes = 1,
            HomeClear = 0,
            HomeDeepLeast = 4,
            HomeDeepMost = 10,
        },
    };

    /// <summary>Every type, in the order <see cref="Surfaces.All"/> lists them; ids never reused, for saves hold them.</summary>
    public static readonly IReadOnlyList<PlanetType> All = [First, Small];
}
