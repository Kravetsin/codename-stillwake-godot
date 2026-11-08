namespace Stillwake.Sim.Crew;

/// <summary>What hurt a hero last (2026-11-08-hurts-are-shown): the client shows each hurt in its kind's colour.</summary>
public enum Harm
{
    /// <summary>Nothing yet.</summary>
    None,
    /// <summary>A creature's ram, pounce or punch.</summary>
    Blow,
    /// <summary>An arc or a zap: a sentry's bolt, a resonator, a cradle dug.</summary>
    Shock,
    /// <summary>A capback's gas or slime.</summary>
    Poison,
    /// <summary>Lava under the hero.</summary>
    Burn,
}
