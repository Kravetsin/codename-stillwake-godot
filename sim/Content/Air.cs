namespace Stillwake.Content;

/// <summary>
/// Oxygen, in whole units, and how it moves; times in ticks, 20 to a second. A cell of air is one
/// unit. A room is aired cell by cell, <see cref="CellsPerFill"/> cells every <see cref="TicksPerCell"/> ticks (0105). Every
/// <see cref="LeakEvery"/> ticks, ten minutes so that a rate of a tenth of a unit a minute is whole, a
/// room holding air pays its tanks a leak through what its air touches (0020): the back wall behind each
/// of its cells and each outer hull cell by its material's leak, a closed door's cell by <see cref="DoorLeak"/>.
/// A room open to space loses <see cref="VentLoss"/> cells a tick, and a vent in it, or in space, drains
/// its network by <see cref="VentDrain"/> a tick: what a vent can pass, so a breach empties a tank over
/// minutes, with time to reach a valve. A generator makes <see cref="GeneratorRate"/> units every
/// <see cref="LeakEvery"/> ticks into a store of its own holding <see cref="GeneratorStore"/>; what does
/// not fit is lost. Sized so the starting room holds with two heroes breathing. A cell is 8 px (0104): what is
/// counted by area is four times what it was at 16 px, so a room holds, fills and empties as it did.
/// </summary>
public static class Air
{
    public const int TankCapacity = 36000;
    public const int Canister = 9000;
    public const int TicksPerCell = 1;

    /// <summary>The cells a filling room airs on each fill beat, four of the 8 px cells to the 16 px cell it aired before (0105).</summary>
    public const int CellsPerFill = 4;

    public const int VentLoss = 24;
    public const int LeakEvery = 12000;
    public const int DoorLeak = 20;
    public const int VentDrain = 8;
    public const int GeneratorRate = 12000;
    public const int GeneratorStore = 160;
}
