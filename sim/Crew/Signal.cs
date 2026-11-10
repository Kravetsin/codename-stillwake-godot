namespace Stillwake.Sim.Crew;

/// <summary>What a hero's tracker bracelet shows of its hidden wanted meter (tracker-catchers): never a number, nothing before it fills.</summary>
public enum Signal
{
    /// <summary>Quiet: the meter not full, or not drawn yet.</summary>
    Calm,
    /// <summary>Full: a catcher is sent and comes once the alarm runs out.</summary>
    Coming,
    /// <summary>A catcher hunts the hero.</summary>
    Hunted,
}
