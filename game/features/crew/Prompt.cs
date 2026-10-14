using System;
using System.Linq;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Crew;

/// <summary>What E does at the aim, as an i18n key, and why the sim would refuse it, if it would. Two that say the same are equal.</summary>
public sealed record Prompt(string Action, string? Problem);

/// <summary>The prompt for E (apps/game/src/features/crew/prompt.ts).</summary>
public static class Prompts
{
    /// <summary>What E would do at <paramref name="aim"/> within reach, in words; nothing out of reach or with nothing to do.</summary>
    public static Prompt? Of(Registry m, Aim? aim)
    {
        if (aim is not { InReach: true }) return null;
        if (aim.Work is { } work)
        {
            var plan = m.Building.Plans.FirstOrDefault(p => p.Id == work.Plan);
            string action = plan?.Clear != null ? "use.takeApart" : work.Delivers ? "use.deliver" : "use.build";
            return new Prompt(action, work.Problem);
        }
        if (aim.Crate != null) return new Prompt("use.openCrate", null);
        if (aim.Press is not { } press) return null;
        return press.Command switch
        {
            LoadCanister => new Prompt("use.loadCanister", press.Problem),
            Grab => new Prompt("use.catch", press.Problem),
            ToggleDoor => new Prompt(m.Grid.StructureAt(aim.Cell) == Structure.DoorOpen ? "use.closeDoor" : "use.openDoor", press.Problem),
            ToggleValve => new Prompt(m.Grid.ConduitAt(aim.Cell) == Conduit.ValveOpen ? "use.closeValve" : "use.openValve", press.Problem),
            _ => throw new InvalidOperationException($"No prompt for {press.Command.GetType().Name}"),
        };
    }
}
