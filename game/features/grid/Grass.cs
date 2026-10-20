using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// Where moss grows on a planet (0078), a rule of drawing, never the sim's: on a wall of soil, whole or a floor slope, with
/// open sky straight above it — an empty cell with no back wall. Dug out, built over or roofed, it is bare soil.
/// </summary>
public static class Grass
{
    /// <summary>
    /// Whether moss grows on a cell holding <paramref name="structure"/> of material <paramref name="material"/> cut to
    /// <paramref name="shape"/>, under a cell holding <paramref name="aboveStructure"/> and the back wall <paramref name="aboveBack"/>;
    /// <paramref name="soil"/> is soil's material code.
    /// </summary>
    public static bool Grows(int structure, int material, int shape, int aboveStructure, int aboveBack, int soil) =>
        structure == Structure.Wall && material == soil && shape is Shape.None or Shape.BottomRight or Shape.BottomLeft
        && aboveStructure == Structure.Empty && aboveBack == Back.None;

    /// <summary>Which of a cell's variants of moss it shows, of <paramref name="variants"/>: the same for a cell every time, neighbours mostly apart.</summary>
    public static int VariantOf(int cell, int variants)
    {
        uint h = unchecked((uint)cell * 0x9E3779B1u);
        return (int)((h ^ (h >> 15)) % (uint)variants);
    }
}
