namespace Stillwake.Content;

/// <summary>
/// How a planet's ground grows from its seed (0077), read by the core's <c>Terrain</c>: ranges, hills and cliffs of soil
/// over stone, caves denser deeper, a few landmarks at places the seed chooses — chasms and great halls — and lava over
/// bedrock under it all, bedrock at its sides. Noise values run from 0 to 65535; a wave is how many cells one swell of noise spans.
/// Rows count down from the top of the grid, depths down from a column's surface.
/// </summary>
public sealed record Ground(int Width, int Height)
{
    /// <summary>The surface's middle row: ranges and hills rise and sink round it.</summary>
    public int Surface { get; init; }

    /// <summary>
    /// The ranges and valleys: one swell every <see cref="RangeWave"/> columns, lifting or sinking the surface up to
    /// <see cref="RangeRise"/> rows, in steps of <see cref="CliffStep"/> rows, so their sides are terraces of cliffs.
    /// </summary>
    public int RangeWave { get; init; } = 384;
    public int RangeRise { get; init; } = 44;
    public int CliffStep { get; init; } = 5;

    /// <summary>The hills on them: one swell every <see cref="HillWave"/> columns, up to <see cref="HillRise"/> rows either way.</summary>
    public int HillWave { get; init; } = 64;
    public int HillRise { get; init; } = 12;

    /// <summary>The bumps on the hills: an octave finer and lower.</summary>
    public int BumpWave { get; init; } = 12;
    public int BumpRise { get; init; } = 2;

    /// <summary>Soil over the stone: <see cref="SoilLeast"/> to <see cref="SoilMost"/> rows deep, swelling every <see cref="SoilWave"/> columns.</summary>
    public int SoilLeast { get; init; } = 3;
    public int SoilMost { get; init; } = 8;
    public int SoilWave { get; init; } = 24;

    /// <summary>
    /// How deep under its surface a column's caves begin: under the soil and a few rows of stone, so a first shaft
    /// finds one soon. Where the cave mouths' noise (every <see cref="MouthWave"/> columns) passes <see cref="MouthOver"/>,
    /// caves come right up and a few open to the sky.
    /// </summary>
    public int CaveFrom { get; init; } = 12;
    public int MouthWave { get; init; } = 40;
    public int MouthOver { get; init; } = 44000;

    /// <summary>
    /// How far under a column's surface line open ground straight under the sky still shows the sky, with no back wall: from
    /// <see cref="MouthFadeLeast"/> to <see cref="MouthFadeMost"/> rows, varying smoothly along the surface (a swell every 6 columns), so a cave's mouth meets rock in a gentle line.
    /// Deeper, a shaft or a chasm keeps its back wall.
    /// </summary>
    public int MouthFadeLeast { get; init; } = 3;
    public int MouthFadeMost { get; init; } = 6;

    /// <summary>
    /// Tunnels: winding where the tunnels' noise is near its middle, within <see cref="TunnelTop"/> of it where caves
    /// begin, widening to <see cref="TunnelBottom"/> at the bedrock. Its swells span <see cref="TunnelWave"/> columns
    /// and <see cref="TunnelTall"/> rows, so tunnels run more along than down and a hero walks them.
    /// </summary>
    public int TunnelWave { get; init; } = 30;
    public int TunnelTall { get; init; } = 18;
    public int TunnelTop { get; init; } = 2200;
    public int TunnelBottom { get; init; } = 4200;

    /// <summary>
    /// Caverns: open where the caverns' noise, swelling every <see cref="CavernWave"/> cells, passes <see cref="CavernTop"/>
    /// where caves begin, falling to <see cref="CavernBottom"/> at the bedrock: more of them, and bigger, deeper down.
    /// </summary>
    public int CavernWave { get; init; } = 14;
    public int CavernTop { get; init; } = 49000;
    public int CavernBottom { get; init; } = 41500;

    /// <summary>
    /// Chasms, landmarks at columns the seed chooses: <see cref="Chasms"/> clefts up to <see cref="ChasmWide"/> cells
    /// across, wandering as they go, open from the surface down <see cref="ChasmDeep"/> rows at most, half that at least.
    /// </summary>
    public int Chasms { get; init; } = 4;
    public int ChasmWide { get; init; } = 12;
    public int ChasmDeep { get; init; } = 260;

    /// <summary>
    /// Great halls, landmarks at places the seed chooses: <see cref="Halls"/> caverns up to <see cref="HallWide"/> cells
    /// either side of their middle and <see cref="HallTall"/> above and below it, half that at least, their walls
    /// ragged by the caverns' noise, from <see cref="HallFrom"/> rows under the surface down to the bedrock.
    /// </summary>
    public int Halls { get; init; } = 6;
    public int HallWide { get; init; } = 72;
    public int HallTall { get; init; } = 28;
    public int HallFrom { get; init; } = 80;

    /// <summary>The columns either side of the middle no landmark comes into: where heroes beam down.</summary>
    public int Clear { get; init; } = 120;

    /// <summary>
    /// Ruins (0085): <see cref="Spires"/> spires (<see cref="Ruins.Spire"/>) at columns the seed chooses, each in a band of its
    /// own, none within <see cref="RuinClear"/> columns of the middle, so finding one takes a walk; each anchored at the highest
    /// ground across it, <see cref="SpireRise"/> of its rows standing above that, so its tip shows over the hills (0 buries it).
    /// </summary>
    public int Spires { get; init; } = 4;
    public int RuinClear { get; init; } = 384;
    public int SpireRise { get; init; } = 10;

    /// <summary>
    /// The arc makers' homes (0090): <see cref="Homes"/> of them (<see cref="Ruins.Homes"/>, each scheme in turn), each in a band of
    /// its own along the columns at least <see cref="HomeClear"/> from the middle, clear of the spires; its ceiling
    /// <see cref="HomeDeepLeast"/> to <see cref="HomeDeepMost"/> rows (by the seed) under the lowest surface across it, so it is
    /// found by digging near the surface.
    /// </summary>
    public int Homes { get; init; } = 3;
    public int HomeClear { get; init; } = 384;
    public int HomeDeepLeast { get; init; } = 8;
    public int HomeDeepMost { get; init; } = 20;

    /// <summary>
    /// The planet's core under the caves, <see cref="CoreRows"/> rows: <see cref="LavaRows"/> of lava (<see cref="Lava"/>) over
    /// <see cref="BedrockRows"/> of bedrock, the lava between the bedrock columns at either side, <see cref="BedrockSide"/>
    /// wide the whole height of the grid.
    /// </summary>
    public int LavaRows { get; init; } = 2;
    public int BedrockRows { get; init; } = 1;
    public int BedrockSide { get; init; } = 1;

    /// <summary>The rows under the caves: the lava and the bedrock under it.</summary>
    public int CoreRows => LavaRows + BedrockRows;

    /// <summary>
    /// The first planet's ground, 2048 × 768 cells: some minutes' walk across, its surface a fifth of the way down under
    /// its sky, some six hundred rows of ground under that.
    /// </summary>
    public static readonly Ground Planet = new(2048, 768) { Surface = 160 };

    /// <summary>
    /// The same ground small, 64 × 48, for tests and the fuzz: low hills, caves a little nearer the surface, a chasm and a
    /// hall of its size, one spire (0085), so the fuzz digs alloy, and one home (0090), its scheme by the seed, anywhere it
    /// fits beside the spire, for a home is wider than the columns left beside a clear middle there.
    /// </summary>
    public static readonly Ground Small = new(64, 48)
    {
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
    };
}
