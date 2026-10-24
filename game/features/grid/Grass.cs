using Stillwake.Sim.Grid;
using Structure = Stillwake.Content.Structure;
using Structures = Stillwake.Content.Structures;
using Trait = Stillwake.Content.Trait;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// Where a planet's ground shows its living edge (0079, 0082), a rule of drawing, never the sim's: moss on a wall of soil with open
/// sky straight above it — an empty cell with no back wall; dug out, built over or roofed, it is bare soil.
/// Stone and bedrock show their chips under any empty cell, a cave's floor and a dug hole's too.
/// </summary>
public static class Grass
{
    /// <summary>
    /// Whether moss grows on a cell holding <paramref name="structure"/> of material <paramref name="material"/>, under a cell
    /// holding <paramref name="aboveStructure"/> and the back wall <paramref name="aboveBack"/>; <paramref name="soil"/> is soil's
    /// material code.
    /// </summary>
    public static bool Grows(int structure, int material, int aboveStructure, int aboveBack) =>
        Structures.Is(structure, Trait.Terrain) && Stillwake.Content.Materials.ByCode(material)?.Grows == true
        && aboveStructure == Structure.Empty && aboveBack == Back.None;

    /// <summary>
    /// Whether a cell shows its ground's living edge: moss where it <see cref="Grows"/> on soil, chips on any other
    /// <paramref name="natural"/> material under an empty cell.
    /// </summary>
    public static bool Edged(int structure, int material, bool natural, int aboveStructure, int aboveBack) =>
        Stillwake.Content.Materials.ByCode(material)?.Grows == true
            ? Grows(structure, material, aboveStructure, aboveBack)
            : natural && Structures.Is(structure, Trait.Terrain) && aboveStructure == Structure.Empty;

    /// <summary>Which of a cell's variants of moss it shows, of <paramref name="variants"/>: the same for a cell every time, neighbours mostly apart.</summary>
    public static int VariantOf(int cell, int variants)
    {
        uint h = unchecked((uint)cell * 0x9E3779B1u);
        return (int)((h ^ (h >> 15)) % (uint)variants);
    }
}
