using System;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
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
    /// nothing to do. A door or valve says what its press would make of it (<see cref="Aiming.AimAt"/>).
    /// </summary>
    public static Prompt? Of(Registry m, Aim? aim)
    {
        if (aim is not { InReach: true }) return null;
        if (aim.Work is { } work)
        {
            var plan = m.Building.Plans.FirstOrDefault(p => p.Id == work.Plan);
            string action = plan?.Clear != null ? "use.takeApart" : "use.build";
            return new Prompt(action, work.Problem);
        }
        // Named for what it opens, by its kind's own words (use.open.<kind>): a furnace, the lifepod's Fabricator; else a crate.
        if (aim.Crate is int opens)
            return new Prompt(m.Grid.MachineById(opens)?.Kind is { } kind && I18n.En.Table.ContainsKey($"use.open.{kind}") ? $"use.open.{kind}" : "use.openCrate", null);
        if (aim.Press is not { } press) return null;
        return press.Command switch
        {
            LoadCanister => new Prompt("use.loadCanister", press.Problem),
            Grab => new Prompt("use.catch", press.Problem),
            HewWrecks => new Prompt("use.chop", press.Problem),
            Hew => new Prompt("use.dig", press.Problem),
            SetDoor d => new Prompt(d.Open ? "use.openDoor" : "use.closeDoor", press.Problem),
            Sim.Pipes.SetValve v => new Prompt(v.Open ? "use.openValve" : "use.closeValve", press.Problem),
            _ => throw new InvalidOperationException($"No prompt for {press.Command.GetType().Name}"),
        };
    }
}
