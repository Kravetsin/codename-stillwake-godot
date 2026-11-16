namespace Stillwake.Content;

/// <summary>The drop pod's flights (the descent design): what a descent burns, how long each part lasts in ticks (20 a second), where it lands.</summary>
public static class Landing
{
    /// <summary>The biofuel a descent burns from the pod's slot; a return burns none.</summary>
    public const int DescentFuel = 5;

    /// <summary>Ticks from a launch until the run it fell through shuts: the pod is through by then.</summary>
    public const int FallTicks = 20;

    /// <summary>Ticks from a launch aboard until the pod lands on the planet.</summary>
    public const int DescentTicks = 100;

    /// <summary>Ticks from a launch off the ship until the pod docks aboard.</summary>
    public const int AscentTicks = 100;

    /// <summary>The biofuel the engine burns from its slot on each flight between planets (solar-system).</summary>
    public const int JumpFuel = 25;

    /// <summary>Ticks the ship's flight to another planet of its system lasts: 30 s.</summary>
    public const int FlightTicks = 30 * 20;

    /// <summary>A planet of the solar system: its type (<see cref="PlanetTypes"/>) and the technology whose ruins prevail there.</summary>
    public sealed record SystemPlanet(string Type, string Technology);

    /// <summary>
    /// The solar system's planets, the ship at the first from the start (solar-system): every one the first type and Arc's until
    /// planets differ.
    /// </summary>
    public static readonly IReadOnlyList<SystemPlanet> Planets = [.. Enumerable.Repeat(new SystemPlanet(PlanetTypes.First.Id, "arc"), 6)];

    /// <summary>The kind of planet a descent opens when none is open.</summary>
    public static readonly string Planet = PlanetTypes.First.Id;
}
