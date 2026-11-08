namespace Stillwake.Content;

/// <summary>
/// Oxygen, in whole units, and how it moves; times in ticks, 20 to a second. A cell of air is one
/// unit. A room is aired cell by cell, <see cref="CellsPerFill"/> cells every <see cref="TicksPerCell"/> ticks (0105). Every
/// <see cref="LeakEvery"/> ticks, ten minutes so that a rate of a tenth of a unit a minute is whole, a
/// room holds as many cells round its vents as its sources pay the leak of (0020, 2026-11-05-air-bubbles): the back
/// wall behind each and the hull cells beside it by their material's leak, a closed door in its hull by <see cref="DoorLeak"/>;
/// the pod's <see cref="PodRate"/> holds the starting room alone, a vaporizer's <see cref="GeneratorRate"/> as much, 133 cells of scrap
/// (scarce-oxygen: the lock, the hold and every room built after need a vaporizer and canisters).
/// A room open to space loses <see cref="VentLoss"/> cells a tick, and a vent in it, or in space, drains
/// its network by <see cref="VentDrain"/> a tick: what a vent can pass. A tank holds liquid oxygen, a canister
/// <see cref="Canister"/> of it; a vaporizer (the generator) turns it into air at <see cref="GeneratorRate"/> units every
/// <see cref="LeakEvery"/> ticks at full power, one unit of liquid a unit of air, into a store of its own holding
/// <see cref="GeneratorStore"/> that its vents draw from. Liquid leaves at most at that rate, so a breach empties a
/// canister over minutes, with time to reach a valve. Sized so the starting room holds with two heroes breathing. A cell is 8 px (0104): what is
/// counted by area is four times what it was at 16 px, so a room holds, fills and empties as it did.
/// </summary>
public static class Air
{
    /// <summary>What a tank holds, fifty canisters: liquids are kept in tanks, not crates (industrial-oxygen).</summary>
    public const int TankCapacity = 50 * Canister;
    public const int Canister = 3000;
    public const int TicksPerCell = 1;

    /// <summary>The cells a filling room airs on each fill beat, four of the 8 px cells to the 16 px cell it aired before (0105).</summary>
    public const int CellsPerFill = 4;

    public const int VentLoss = 24;
    public const int LeakEvery = 12000;
    public const int DoorLeak = 60;
    public const int VentDrain = 8;
    public const int GeneratorRate = 12000;

    /// <summary>What the pod makes from nothing every <see cref="LeakEvery"/> ticks: room A of the starting ship, whose leak is 11520, and no more (scarce-oxygen).</summary>
    public const int PodRate = 12000;

    public const int GeneratorStore = 160;

    /// <summary>
    /// What a tank passes a tick to the tanks piped from its output to their inputs (tanks-in-a-line): a canister moves on
    /// in some 8 seconds, so a line fills its last tank while the player watches.
    /// </summary>
    public const int TankFlow = 20;
}
