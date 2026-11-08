using Stillwake.Sim.Grid;

namespace Stillwake.Game.Shared;

/// <summary>
/// The hold's piles as the client sees them (hold-piles, 2026-11-07-pile-hit-points): a machine with hit points, broken by hits
/// as a wreck is. A tool's rays pass through one, touching it on the way (2026-11-07-pile-hits-felt).
/// </summary>
public static class Piles
{
    /// <summary>The pile at <paramref name="cell"/>, any cell of its footprint, and its hit points; none for anything else.</summary>
    public static (Machine Pile, int Hp)? At(IGridQueries grid, int cell) =>
        grid.MachineAt(cell) is { } machine && Content.Machines.Get(machine.Kind).Hp is int hp ? (machine, hp) : null;
}
