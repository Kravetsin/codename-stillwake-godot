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
    /// <summary>What E does for a hero sitting in a drop pod, wherever the pointer is: it leaves.</summary>
    public static readonly Prompt Seated = new("use.alight", null);

    /// <summary>
    /// What E would do at <paramref name="aim"/> within reach, in words; nothing out of reach or with
    /// nothing to do. A door or valve says what its press would make of it (<see cref="Aiming.AimAt"/>). A builder's work
    /// names the builder <paramref name="held"/> when it has its own words (use.build.powerDriver), else the screwdriver.
    /// </summary>
    public static Prompt? Of(Registry m, Aim? aim, string? held = null)
    {
        if (aim is not { InReach: true }) return null;
        string By(string action) => held != null && I18n.En.Table.ContainsKey($"{action}.{held}") ? $"{action}.{held}" : action;
        if (aim.Work is { Repair: not null } repair) return new Prompt(By("use.repair"), repair.Problem);
        if (aim.Work is { } work)
        {
            var plan = m.Building.Plans.FirstOrDefault(p => p.Id == work.Plan);
            string action = plan?.Clear != null ? "use.takeApart" : "use.build";
            return new Prompt(By(action), work.Problem);
        }
        // A drop pod is boarded, its slot opening as the hero sits: said as a seat, with why not.
        if (aim.Press is { Command: Sim.Landing.Board } board) return new Prompt("use.board", board.Problem);
        // Named for what it opens, by its kind's own words (use.open.<kind>, every kind that opens has them): a furnace, the lifepod's Fabricator.
        if (aim.Crate is int opens)
            return new Prompt($"use.open.{m.Grid.MachineById(opens)?.Kind ?? "crate"}", null);
        if (aim.Press is not { } press) return null;
        return press.Command switch
        {
            LoadCanister => new Prompt("use.loadCanister", press.Problem),
            Sim.Water.PourWater => new Prompt("use.pourWater", press.Problem),
            Grab => new Prompt("use.catch", press.Problem),
            Collect => new Prompt("use.collect", press.Problem),
            HewWrecks => new Prompt("use.chop", press.Problem),
            Hew => new Prompt("use.dig", press.Problem),
            SetDoor d => new Prompt(d.Open ? "use.openDoor" : "use.closeDoor", press.Problem),
            Sim.Landing.SetPlatform p => new Prompt(p.Open ? "use.openPlatform" : "use.closePlatform", press.Problem),
            Sim.Pipes.SetValve v => new Prompt(v.Open ? "use.openValve" : "use.closeValve", press.Problem),
            _ => throw new InvalidOperationException($"No prompt for {press.Command.GetType().Name}"),
        };
    }
}
