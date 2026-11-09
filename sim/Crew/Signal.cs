namespace Stillwake.Sim.Crew;

/// <summary>What a hero's tracker bracelet shows of its hidden wanted meter (tracker-catchers): never a number.</summary>
public enum Signal
{
    /// <summary>Quiet: the meter low, or not drawn yet.</summary>
    Calm,
    /// <summary>Blinking now and then: two thirds full or more.</summary>
    Blinking,
    /// <summary>Full: a catcher hunts the hero.</summary>
    Hunted,
}
