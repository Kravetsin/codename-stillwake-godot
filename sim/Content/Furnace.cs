namespace Stillwake.Content;

/// <summary>
/// A fire, a furnace's or a burner generator's (ADR 0023): it burns what is in the machine's
/// <see cref="MachineSpec.FuelSlot"/>, each fuel for its <see cref="ItemSpec.BurnTicks"/>. Lit, it
/// breathes <see cref="Breath"/> units of its room's air every <see cref="BreathEvery"/> ticks of
/// burning, as a hero does; given less, its fire goes out.
/// </summary>
public static class Furnace
{
    public const int BreathEvery = 20;
    public const int Breath = 1;
}
