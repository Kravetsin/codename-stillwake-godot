namespace Stillwake.Content;

/// <summary>
/// How a planet's ground grows from its seed (0077), read by the core's <c>Terrain</c>: plains and highlands, ranges and hills of soil
/// over stone, caves denser deeper, a few landmarks at places the seed chooses — chasms and great halls — and lava over
/// bedrock under it all, bedrock at its sides. Noise values run from 0 to 65535; a wave is how many cells one swell of noise spans.
/// Rows count down from the top of the grid, depths down from a column's surface. Each planet type's numbers are in its
/// entry (<see cref="PlanetTypes"/>, 0095): none here.
/// </summary>
public sealed record Ground(int Width, int Height)
{
    /// <summary>The surface's middle row: ranges and hills rise and sink round it.</summary>
    public required int Surface { get; init; }

    /// <summary>
    /// Plains and highlands along the planet: one swell every <see cref="RegionWave"/> columns, from plains, where the ranges,
    /// hills and bumps rise <see cref="RegionLeast"/> percent of their most, to highlands, where they rise all of it and the
    /// land stands <see cref="RegionLift"/> rows higher.
    /// </summary>
    public required int RegionWave { get; init; }
    public required int RegionLeast { get; init; }
    public required int RegionLift { get; init; }

    /// <summary>
    /// The ranges and valleys: one swell every <see cref="RangeWave"/> columns, lifting or sinking the surface up to
    /// <see cref="RangeRise"/> rows, in shelves <see cref="TerraceStep"/> rows apart; the whole surface is then made a stair of
    /// one-row steps (0106), so a shelf's riser is a run of steps, never a cliff.
    /// </summary>
    public required int RangeWave { get; init; }
    public required int RangeRise { get; init; }
    public required int TerraceStep { get; init; }

    /// <summary>The hills on them: one swell every <see cref="HillWave"/> columns, up to <see cref="HillRise"/> rows either way.</summary>
    public required int HillWave { get; init; }
    public required int HillRise { get; init; }

    /// <summary>The bumps on the hills: an octave finer and lower.</summary>
    public required int BumpWave { get; init; }
    public required int BumpRise { get; init; }

    /// <summary>Soil over the stone: <see cref="SoilLeast"/> to <see cref="SoilMost"/> rows deep, swelling every <see cref="SoilWave"/> columns.</summary>
    public required int SoilLeast { get; init; }
    public required int SoilMost { get; init; }
    public required int SoilWave { get; init; }

    /// <summary>
    /// How deep under its surface a column's caves begin: under the soil and a few rows of stone, so a first shaft
    /// finds one soon. Where the cave mouths' noise (every <see cref="MouthWave"/> columns) passes <see cref="MouthOver"/>,
    /// caves come right up and a few open to the sky.
    /// </summary>
    public required int CaveFrom { get; init; }
    public required int MouthWave { get; init; }
    public required int MouthOver { get; init; }

    /// <summary>
    /// How far under a column's surface line open ground straight under the sky still shows the sky, with no back wall: from
    /// <see cref="MouthFadeLeast"/> to <see cref="MouthFadeMost"/> rows, varying smoothly along the surface (a swell every 12 columns), so a cave's mouth meets rock in a gentle line.
    /// Deeper, a shaft or a chasm keeps its back wall.
    /// </summary>
    public required int MouthFadeLeast { get; init; }
    public required int MouthFadeMost { get; init; }

    /// <summary>
    /// Tunnels: winding where the tunnels' noise is near its middle, within <see cref="TunnelTop"/> of it where caves
    /// begin, widening to <see cref="TunnelBottom"/> at the bedrock. Its swells span <see cref="TunnelWave"/> columns
    /// and <see cref="TunnelTall"/> rows, so tunnels run more along than down and a hero walks them.
    /// </summary>
    public required int TunnelWave { get; init; }
    public required int TunnelTall { get; init; }
    public required int TunnelTop { get; init; }
    public required int TunnelBottom { get; init; }

    /// <summary>
    /// Caverns: open where the caverns' noise, swelling every <see cref="CavernWave"/> cells, passes <see cref="CavernTop"/>
    /// where caves begin, falling to <see cref="CavernBottom"/> at the bedrock: more of them, and bigger, deeper down.
    /// </summary>
    public required int CavernWave { get; init; }
    public required int CavernTop { get; init; }
    public required int CavernBottom { get; init; }

    /// <summary>
    /// Chasms, landmarks at columns the seed chooses: <see cref="Chasms"/> clefts up to <see cref="ChasmWide"/> cells
    /// across, wandering as they go, open from the surface down <see cref="ChasmDeep"/> rows at most, half that at least.
    /// </summary>
    public required int Chasms { get; init; }
    public required int ChasmWide { get; init; }
    public required int ChasmDeep { get; init; }

    /// <summary>
    /// Great halls, landmarks at places the seed chooses: <see cref="Halls"/> caverns up to <see cref="HallWide"/> cells
    /// either side of their middle and <see cref="HallTall"/> above and below it, half that at least, their walls
    /// ragged by the caverns' noise, from <see cref="HallFrom"/> rows under the surface down to the bedrock.
    /// </summary>
    public required int Halls { get; init; }
    public required int HallWide { get; init; }
    public required int HallTall { get; init; }
    public required int HallFrom { get; init; }

    /// <summary>The columns either side of the middle no landmark nor cave mouth comes into: where heroes beam down.</summary>
    public required int Clear { get; init; }

    /// <summary>
    /// Ruins (0085): <see cref="Spires"/> spires (<see cref="Ruins.Spire"/>) at columns the seed chooses, each in a band of its
    /// own, none within <see cref="RuinClear"/> columns of the middle, so finding one takes a walk; each anchored at the highest
    /// ground across it, <see cref="SpireRise"/> of its rows standing above that, so its tip shows over the hills (0 buries it).
    /// </summary>
    public required int Spires { get; init; }
    public required int RuinClear { get; init; }
    public required int SpireRise { get; init; }

    /// <summary>
    /// The arc makers' homes (0091): <see cref="Homes"/> of them (<see cref="Ruins.Homes"/>, each scheme in turn), each in a band of
    /// its own along the columns at least <see cref="HomeClear"/> from the middle, clear of the spires; its ceiling
    /// <see cref="HomeDeepLeast"/> to <see cref="HomeDeepMost"/> rows (by the seed) under the lowest surface across it, so it is
    /// found by digging near the surface.
    /// </summary>
    public required int Homes { get; init; }
    public required int HomeClear { get; init; }
    public required int HomeDeepLeast { get; init; }
    public required int HomeDeepMost { get; init; }

    /// <summary>
    /// The planet's core under the caves, <see cref="CoreRows"/> rows: <see cref="LavaRows"/> of lava (<see cref="Lava"/>) over
    /// <see cref="BedrockRows"/> of bedrock, the lava between the bedrock columns at either side, <see cref="BedrockSide"/>
    /// wide the whole height of the grid.
    /// </summary>
    public required int LavaRows { get; init; }
    public required int BedrockRows { get; init; }
    public required int BedrockSide { get; init; }

    /// <summary>
    /// The columns at each side hidden in fog (fog-edges): bedrock from the ground's top down, sky over it, no landmark,
    /// ruin or growth, and outside the build zone, so nothing is dug or built there and no creature goes in.
    /// </summary>
    public required int Fog { get; init; }

    /// <summary>
    /// Rows over a band's ground line beside the zone that a hero moved across the fog arrives at, its feet: it glides out
    /// of the band and falls to the ground (fog-edges). At least the hero's height.
    /// </summary>
    public required int FogDrop { get; init; }

    /// <summary>The rows under the caves: the lava and the bedrock under it.</summary>
    public int CoreRows => LavaRows + BedrockRows;
}
