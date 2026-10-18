using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;
using Stillwake.Sim.Logistics;

namespace Stillwake.Game.Features.Logistics;

/// <summary>How a machine's drone badge looks (0062): lit while drones serve it, crossed while switched off, grey where no bay reaches.</summary>
public enum BadgeLook { Lit, Off, Unreached }

/// <summary>
/// What the drone badge and the drones in the world show, without Godot (0062): the badge's look and tooltip keys, this
/// peer's own switch in flight shown at once (0014), and where a flying drone is drawn between ticks.
/// </summary>
public static class Served
{
    /// <summary>The badge of a machine drones serve or not, reached by a bay or not: switched off shows first.</summary>
    public static BadgeLook LookOf(bool served, bool reached) => !served ? BadgeLook.Off : reached ? BadgeLook.Lit : BadgeLook.Unreached;

    /// <summary>What the badge's tooltip says for <paramref name="look"/>, its name and what a click does, as i18n keys.</summary>
    public static (string Name, string Click) TipOf(BadgeLook look) => look switch
    {
        BadgeLook.Lit => ("logistics.served", "logistics.switchOff"),
        BadgeLook.Off => ("logistics.off", "logistics.switchOn"),
        _ => ("logistics.unreached", "logistics.switchOff"),
    };

    /// <summary>Whether drones serve <paramref name="machine"/> as this peer shows it: its last <see cref="SetServed"/> in flight, else the sim's.</summary>
    public static bool Shown(bool sim, int machine, IEnumerable<Command> inFlight) =>
        inFlight.OfType<SetServed>().LastOrDefault(c => c.Machine == machine)?.On ?? sim;

    /// <summary>
    /// Where a drone at (<paramref name="x"/>, <paramref name="y"/>) flying to (<paramref name="toX"/>, <paramref name="toY"/>),
    /// in thousandths of a cell, is drawn <paramref name="progress"/> of a tick on: carried on toward its next centre at
    /// <paramref name="speed"/> a tick, never past it. The sim moves it a whole tick at a time.
    /// </summary>
    public static (double X, double Y) Smoothed(int x, int y, int toX, int toY, int speed, double progress)
    {
        double left = Math.Abs(toX - x) + Math.Abs(toY - y);
        double go = Math.Min(left, speed * Math.Clamp(progress, 0, 1));
        return (x + Math.Sign(toX - x) * go, y + Math.Sign(toY - y) * go);
    }
}
