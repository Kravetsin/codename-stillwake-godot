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
/// peer's own switch in flight shown at once (0014), bay slots filled or emptied, a clamp's grip.
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
    /// A bay's slots that came to hold a drone since the last read (<paramref name="filled"/>, one docked) and those
    /// that stopped (<paramref name="emptied"/>, one launched), from which slots held one then and now.
    /// </summary>
    public static void Changed(IReadOnlyList<bool> was, IReadOnlyList<bool> now, List<int> filled, List<int> emptied)
    {
        for (int i = 0; i < Math.Min(was.Count, now.Count); i++)
        {
            if (now[i] && !was[i]) filled.Add(i);
            else if (was[i] && !now[i]) emptied.Add(i);
        }
    }

    /// <summary>Whether a drone's clamp worked between two reads of its cargo: it took a stack, or put some or all of it in.</summary>
    public static bool Gripped((string Item, int Count)? was, (string Item, int Count)? now) =>
        was is not { } w ? now != null : now is not { } n || n.Item != w.Item || n.Count < w.Count;
}
