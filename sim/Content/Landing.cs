namespace Stillwake.Content;

/// <summary>The drop pod's flights (the descent design): how long each part lasts in ticks (20 a second), where it lands.</summary>
public static class Landing
{
    /// <summary>Ticks from a launch until the run it fell through shuts: the pod is through by then.</summary>
    public const int FallTicks = 20;

    /// <summary>Ticks from a launch aboard until the pod lands on the planet.</summary>
    public const int DescentTicks = 100;

    /// <summary>Ticks from a launch off the ship until the pod docks aboard.</summary>
    public const int AscentTicks = 100;

    /// <summary>The millilitres the engine burns from its store on each flight between planets (solar-system): 8 l of its 20.</summary>
    public const int JumpFuel = 8 * Fuel.Litre;

    /// <summary>Ticks the ship's flight to another planet of its system lasts: 30 s.</summary>
    public const int FlightTicks = 30 * 20;

    /// <summary>A planet of the solar system: its type (<see cref="PlanetTypes"/>) and the technology whose ruins prevail there.</summary>
    public sealed record SystemPlanet(string Type, string Technology);

    /// <summary>
    /// The solar system's planets, the ship at the first from the start (solar-system), each of a fixed type (planet-variety): the
    /// first planet's, two scorched near the sun, the first's again, two icy far out. Arc's ruins on every one.
    /// </summary>
    public static readonly IReadOnlyList<SystemPlanet> Planets =
    [
        new(PlanetTypes.First.Id, "arc"),
        new(PlanetTypes.Scorched.Id, "arc"),
        new(PlanetTypes.Scorched.Id, "arc"),
        new(PlanetTypes.First.Id, "arc"),
        new(PlanetTypes.Icy.Id, "arc"),
        new(PlanetTypes.Icy.Id, "arc"),
    ];

    /// <summary>The kind of planet a descent opens when no system planet is named: the first's.</summary>
    public static readonly string Planet = Planets[0].Type;

    /// <summary>
    /// Every ore a planet's ground may grow or lack (planet-variety), item ids in the order a set lists them: the starting ores,
    /// then the varying ones, mica last. An item not here grows wherever its type grows it.
    /// </summary>
    public static readonly IReadOnlyList<string> Ores = ["copperOre", "ironOre", "ice", "sulfur", "magnetite", "crystal", "mica"];

    /// <summary>The ores the first planet always has, so the way off it needs no other: mica besides, which every planet has.</summary>
    public static readonly IReadOnlyList<string> Starting = ["copperOre", "ironOre", "ice"];

    /// <summary>The ores dealt among planets 2 to 6, each to one at least: what the flights are for.</summary>
    public static readonly IReadOnlyList<string> Varying = ["sulfur", "magnetite", "crystal"];

    /// <summary>The ore on every planet.</summary>
    public const string Everywhere = "mica";

    /// <summary>The fewest and most ores, mica aside, dealt to each of planets 2 to 6.</summary>
    public const int OresLeast = 2, OresMost = 4;
}
