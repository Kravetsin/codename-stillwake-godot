using System;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim;
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
    /// What E does for a hero sitting in a seat of <paramref name="kind"/>, wherever the pointer is: it leaves, in the
    /// seat's own words where it has them (use.alight.treadWheel: run, then get out).
    /// </summary>
    public static Prompt SeatedIn(string? kind) => new(Own("use.alight", kind), null);

    /// <summary><paramref name="action"/> in <paramref name="kind"/>'s own words (<c>action.kind</c>) where it has them.</summary>
    private static string Own(string action, string? kind) =>
        kind != null && I18n.En.Table.ContainsKey($"{action}.{kind}") ? $"{action}.{kind}" : action;

    /// <summary>
    /// What E would do at <paramref name="aim"/> within reach, in words; nothing out of reach or with
    /// nothing to do. A door or valve says what its press would make of it (<see cref="Aiming.AimAt"/>). A builder's work
    /// names the builder <paramref name="held"/> when it has its own words (use.build.powerDriver), else the screwdriver.
    /// </summary>
    public static Prompt? Of(Registry m, Aim? aim, string? held = null)
    {
        if (aim is not { InReach: true }) return null;
        if (aim.Work is { Repair: not null } repair) return new Prompt(Own("use.repair", held), repair.Problem);
        if (aim.Work is { } work)
        {
            var plan = m.Building.Plans.FirstOrDefault(p => p.Id == work.Plan);
            string action = plan?.Clear != null ? "use.takeApart" : "use.build";
            return new Prompt(Own(action, held), work.Problem);
        }
        // A seat is boarded, a drop pod's slot opening as the hero sits: said as a seat of its kind, with why not.
        if (aim.Press is { Command: Sim.Landing.Board b } board) return new Prompt(Own("use.board", m.Grid.MachineById(b.Pod)?.Kind), board.Problem);
        // Named for what it opens, by its kind's own words (use.open.<kind>, every kind that opens has them): a furnace, the lifepod's Fabricator.
        if (aim.Crate is int opens)
            return new Prompt($"use.open.{m.Grid.MachineById(opens)?.Kind ?? "crate"}", null);
        if (aim.Press is not { } press) return null;
        return press.Command switch
        {
            Sim.Water.PourWater => new Prompt("use.pourWater", press.Problem),
            Grab => new Prompt("use.catch", press.Problem),
            Collect => new Prompt("use.collect", press.Problem),
            HewWrecks or HewPieces => new Prompt("use.chop", press.Problem),
            Hew or HewCells => new Prompt("use.dig", press.Problem),
            SetDoor d => new Prompt(d.Open ? "use.openDoor" : "use.closeDoor", press.Problem),
            Sim.Landing.SetPlatform p => new Prompt(p.Open ? "use.openPlatform" : "use.closePlatform", press.Problem),
            Sim.Pipes.SetValve v => new Prompt(v.Open ? "use.openValve" : "use.closeValve", press.Problem),
            _ => throw new InvalidOperationException($"No prompt for {press.Command.GetType().Name}"),
        };
    }
}
