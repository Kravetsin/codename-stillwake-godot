namespace Stillwake.Content;

/// <summary>The drop pod's flights (the descent design): what a descent burns, how long each part lasts in ticks (20 a second), where it lands.</summary>
public static class Landing
{
    /// <summary>The biofuel a descent burns from the pod's slot; a return burns none.</summary>
    public const int DescentFuel = 5;

    /// <summary>Ticks from a launch until the run it fell through shuts: the pod is through by then.</summary>
    public const int FallTicks = 20;

    /// <summary>Ticks from a launch aboard until the pod lands on the planet.</summary>
    public const int DescentTicks = 160;

    /// <summary>Ticks from a launch off the ship until the pod docks aboard.</summary>
    public const int AscentTicks = 100;

    /// <summary>The kind of planet a descent opens when none is open.</summary>
    public static readonly string Planet = PlanetTypes.First.Id;
}
