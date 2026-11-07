namespace Stillwake.Content;

/// <summary>
/// A fire, a furnace's or a burner generator's (ADR 0023): it burns what is in the machine's
/// <see cref="MachineSpec.FuelSlot"/>, each fuel for its <see cref="ItemSpec.BurnTicks"/>. Lit, it
/// breathes <see cref="Breath"/> units of its room's air every <see cref="BreathEvery"/> ticks of
/// burning, three times a calm hero's (scarce-oxygen); given less, its fire goes out.
/// </summary>
public static class Furnace
{
    public const int BreathEvery = 20;
    public const int Breath = 3;

    /// <summary>
    /// What a lit fire reserves of its room's budget, units every <see cref="Air.LeakEvery"/> ticks: its breaths over that
    /// time, so its room's air pulls back while it burns (industrial-oxygen).
    /// </summary>
    public const int Reserve = Breath * Air.LeakEvery / BreathEvery;
}
