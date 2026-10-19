namespace Stillwake.Content;

/// <summary>
/// SPIKE (planet): a site a hero beams down to on a sortie, dug and mined, never built on. Its ground is made
/// from a seed; what is kept of it is the cells dug out.
/// </summary>
public static class Site
{
    /// <summary>Sites a hero may beam to, numbered 1 to this; 0 is the ship.</summary>
    public const int Count = 4;

    public const int Width = 400;
    public const int Height = 200;

    /// <summary>The surface's middle row, and how far hills rise and fall either way of it.</summary>
    public const int Surface = 70;
    public const int Hills = 24;

    /// <summary>Soil under the surface, rows, before the rock: this plus up to <see cref="SoilVaries"/> more.</summary>
    public const int Soil = 6;
    public const int SoilVaries = 6;

    /// <summary>Caves as worms through the rock: how many, how long each, and the widest radius.</summary>
    public const int Caves = 26;
    public const int CaveSteps = 90;
    public const int CaveRadius = 3;

    /// <summary>Pockets of ore in the rock: how many, and the widest radius.</summary>
    public const int Ores = 40;
    public const int OreRadius = 3;

    /// <summary>What a cell of each ground takes to break, in a weapon's damage: the axe 4 a second, the drill 10.</summary>
    public static int HpOf(string material) => material switch
    {
        "soil" => 4,
        "rock" => 20,
        "iron" => 40,
        _ => 10,
    };

    /// <summary>What a cell of each ground gives, dug out: into the digger's inventory.</summary>
    public static (string Item, int Count) YieldOf(string material) => material switch
    {
        "soil" => ("soil", 1),
        "rock" => ("rock", 1),
        "iron" => ("hullPlate", 1),
        _ => ("scrap", 1),
    };

    /// <summary>
    /// Whether a site keeps its dug cells while no one is on it (the ship staying in orbit), or is forgotten when
    /// the last hero leaves, a fresh one made for the next visit.
    /// </summary>
    public static readonly bool Kept = true;
}
