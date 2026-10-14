namespace Stillwake.Content;

/// <summary>
/// A furnace's fire (ADR 0023). Its second slot, <see cref="FuelSlot"/>, holds what it burns. Lit, it
/// breathes <see cref="Breath"/> units of its room's air every <see cref="BreathEvery"/> ticks of
/// burning, as a hero does; given less, its fire goes out. What each fuel burns for is the item's
/// <see cref="ItemSpec.BurnTicks"/>.
/// </summary>
public static class Furnace
{
    public const int FuelSlot = 1;
    public const int BreathEvery = 20;
    public const int Breath = 1;
}
