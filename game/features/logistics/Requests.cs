using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;
using Stillwake.Sim.Logistics;

namespace Stillwake.Game.Features.Logistics;

/// <summary>How a request slot looks (0062): nothing set, a faint "+"; met, dim; short, edged amber.</summary>
public enum RequestLook { Empty, Met, Unmet }

/// <summary>
/// What the request slots and the role toggle do, without Godot (0062): the requests a click, the wheel or a right-click
/// leaves; a slot's look; what this peer's own commands in flight show at once (0014); where a flying drone is drawn
/// between ticks.
/// </summary>
public static class Requests
{
    /// <summary>
    /// <paramref name="now"/> with slot <paramref name="slot"/> set to <paramref name="count"/> of <paramref name="item"/>: the
    /// stack on the cursor. A slot past the last request adds one at the end: requests are a list, not places.
    /// </summary>
    public static IReadOnlyList<Request> Set(IReadOnlyList<Request> now, int slot, string item, int count)
    {
        var next = now.ToList();
        if (slot < next.Count) next[slot] = new Request(item, count);
        else next.Add(new Request(item, count));
        return next;
    }

    /// <summary><paramref name="now"/> with slot <paramref name="slot"/>'s count changed by <paramref name="by"/>, never under one; a slot with nothing set stays so.</summary>
    public static IReadOnlyList<Request> Nudge(IReadOnlyList<Request> now, int slot, int by)
    {
        if (slot >= now.Count) return now;
        var next = now.ToList();
        next[slot] = next[slot] with { Count = Math.Max(1, next[slot].Count + by) };
        return next;
    }

    /// <summary><paramref name="now"/> without slot <paramref name="slot"/>'s request.</summary>
    public static IReadOnlyList<Request> Clear(IReadOnlyList<Request> now, int slot)
    {
        if (slot >= now.Count) return now;
        var next = now.ToList();
        next.RemoveAt(slot);
        return next;
    }

    /// <summary>How much a wheel step changes a request: one, ten with Shift.</summary>
    public static int StepOf(bool shift) => shift ? 10 : 1;

    /// <summary>How a slot looks with <paramref name="request"/> in it, or none, the machine holding <paramref name="held"/> of its item.</summary>
    public static RequestLook LookOf(Request? request, int held) =>
        request == null ? RequestLook.Empty : held >= request.Count ? RequestLook.Met : RequestLook.Unmet;

    /// <summary>What <paramref name="machine"/> requests as this peer shows it: its last <see cref="SetRequests"/> in flight, else the sim's.</summary>
    public static IReadOnlyList<Request> Shown(IReadOnlyList<Request> sim, int machine, IEnumerable<Command> inFlight) =>
        inFlight.OfType<SetRequests>().LastOrDefault(c => c.Machine == machine)?.Requests ?? sim;

    /// <summary>What crate <paramref name="crate"/> is as this peer shows it: its last <see cref="SetRole"/> in flight, else the sim's.</summary>
    public static Role RoleShown(Role sim, int crate, IEnumerable<Command> inFlight) =>
        inFlight.OfType<SetRole>().LastOrDefault(c => c.Crate == crate)?.Role ?? sim;

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
