using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Power;

namespace Stillwake.Game.Features.Computer;

/// <summary>Where a program's module stands for a computer: none on its network, there but short of power, or ready.</summary>
public enum ProgramState { Absent, Short, Ready }

/// <summary>A button of the computer's side panel: its program, and how its module stands.</summary>
public sealed record ProgramButton(ProgramSpec Program, ProgramState State);

/// <summary>
/// A computer as its screen reads it: whether it gets any power, and its buttons, one a program of
/// <see cref="Programs.All"/> in their order; none while no program's module is on its network.
/// </summary>
public sealed record Reading(bool Powered, IReadOnlyList<ProgramButton> Buttons);

/// <summary>What the screen shows: a line of text, an i18n key; or a program, by its id.</summary>
public sealed record Screen(string? Text, string? Program);

/// <summary>
/// The ship's computer's rules as its screen shows them. A program's module is brought when one stands on the computer's
/// power network, and ready when it gets all it draws; the computer lights its screen on any power, a network short of it
/// slowing all alike, so its modules then stand short. Read in the client: no command changes
/// what it shows, and nothing in the sim asks it yet.
/// </summary>
public static class Readout
{
    /// <summary>All of a machine's draw, in thousandths: less is short.</summary>
    public const int Full = 1000;

    /// <summary>Computer <paramref name="computer"/> as it stands now; unpowered and buttonless if it stands no more.</summary>
    public static Reading Of(IGridQueries grid, IPowerQueries power, int computer)
    {
        if (grid.MachineById(computer) is not { } machine) return new Reading(false, []);
        var network = NetworkOf(power, machine);
        var buttons = Programs.All.Select(p => new ProgramButton(p, StateOf(grid, power, network, p))).ToList();
        bool any = buttons.Any(b => b.State != ProgramState.Absent);
        return new Reading(power.Satisfaction(computer) > 0, any ? buttons : []);
    }

    /// <summary>
    /// What the screen shows with <paramref name="chosen"/> picked: no power, said, before all else; no modules, said; the
    /// program chosen while it is ready; else the first program short of power, said; else the call to pick one.
    /// </summary>
    public static Screen ScreenOf(Reading reading, string? chosen)
    {
        if (!reading.Powered) return new Screen("computer.noPower", null);
        if (reading.Buttons.Count == 0) return new Screen("computer.noModules", null);
        if (Ready(reading, chosen)) return new Screen(null, chosen);
        if (reading.Buttons.FirstOrDefault(b => b.State == ProgramState.Short) is { } shortOf) return new Screen($"computer.short.{shortOf.Program.Id}", null);
        return new Screen("computer.select", null);
    }

    /// <summary>Whether program <paramref name="id"/> may be shown: the computer powered and the program ready. A click on any other does nothing.</summary>
    public static bool Ready(Reading reading, string? id) =>
        reading.Powered && reading.Buttons.Any(b => b.Program.Id == id && b.State == ProgramState.Ready);

    private static ProgramState StateOf(IGridQueries grid, IPowerQueries power, PowerNetwork? network, ProgramSpec program)
    {
        if (network == null) return ProgramState.Absent;
        var modules = grid.MachinesOf(program.Module).Where(m => NetworkOf(power, m)?.Id == network.Id).ToList();
        if (modules.Count == 0) return ProgramState.Absent;
        return modules.Any(m => power.Satisfaction(m.Id) >= Full) ? ProgramState.Ready : ProgramState.Short;
    }

    private static PowerNetwork? NetworkOf(IPowerQueries power, Machine machine) =>
        power.NodeAt(machine.Cell) is int node ? power.NetworkOf(node) : null;
}
