using System;
using System.Linq;
using Stillwake.Game.Shared;
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
    /// <summary>
    /// What E would do at <paramref name="aim"/> within reach, in words; nothing out of reach or with
    /// nothing to do. Doors and valves are as <paramref name="shown"/> (the world's without it): with
    /// this peer's own toggles in flight, the next press undoes them.
    /// </summary>
    public static Prompt? Of(Registry m, Aim? aim, ShownGrid? shown = null)
    {
        if (aim is not { InReach: true }) return null;
        if (aim.Work is { } work)
        {
            var plan = m.Building.Plans.FirstOrDefault(p => p.Id == work.Plan);
            string action = plan?.Clear != null ? "use.takeApart" : work.Delivers ? "use.deliver" : "use.build";
            return new Prompt(action, work.Problem);
        }
        // Named for what it opens: a furnace, an analyzer, the lifepod's Fabricator, else a crate.
        if (aim.Crate is int opens)
            return new Prompt(m.Grid.Machines.FirstOrDefault(x => x.Id == opens)?.Kind switch
            {
                "furnace" => "use.openFurnace",
                "analyzer" => "use.openAnalyzer",
                "lifepod" => "use.openFabricator",
                _ => "use.openCrate",
            }, null);
        if (aim.Press is not { } press) return null;
        var grid = m.Grid;
        int structure = shown?.StructureAt(grid, aim.Cell) ?? grid.StructureAt(aim.Cell);
        int conduit = shown?.ConduitAt(grid, aim.Cell) ?? grid.ConduitAt(aim.Cell);
        return press.Command switch
        {
            LoadCanister => new Prompt("use.loadCanister", press.Problem),
            Grab => new Prompt("use.catch", press.Problem),
            Hit => new Prompt("use.chop", press.Problem),
            ToggleDoor => new Prompt(structure == Structure.DoorOpen ? "use.closeDoor" : "use.openDoor", press.Problem),
            ToggleValve => new Prompt(conduit == Conduit.ValveOpen ? "use.closeValve" : "use.openValve", press.Problem),
            _ => throw new InvalidOperationException($"No prompt for {press.Command.GetType().Name}"),
        };
    }
}
