using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// Where a planet's ground shows its living edge (0079, 0081), a rule of drawing, never the sim's: moss on a wall of soil, whole or a
/// floor slope, with open sky straight above it — an empty cell with no back wall; dug out, built over or roofed, it is bare soil.
/// Stone and bedrock show their chips under any empty cell, a cave's floor and a dug hole's too.
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

    /// <summary>
    /// Whether a cell shows its ground's living edge: moss where it <see cref="Grows"/> on soil, chips on any other
    /// <paramref name="natural"/> material whole or a floor slope under an empty cell.
    /// </summary>
    public static bool Edged(int structure, int material, bool natural, int shape, int aboveStructure, int aboveBack, int soil) =>
        material == soil
            ? Grows(structure, material, shape, aboveStructure, aboveBack, soil)
            : natural && structure == Structure.Wall && shape is Shape.None or Shape.BottomRight or Shape.BottomLeft && aboveStructure == Structure.Empty;

    /// <summary>Which of a cell's variants of moss it shows, of <paramref name="variants"/>: the same for a cell every time, neighbours mostly apart.</summary>
    public static int VariantOf(int cell, int variants)
    {
        uint h = unchecked((uint)cell * 0x9E3779B1u);
        return (int)((h ^ (h >> 15)) % (uint)variants);
    }
}
