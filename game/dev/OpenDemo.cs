using System;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Logistics;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --testbed --open=&lt;kind&gt;</c>: the hero starts just right of the testbed's machine of that kind
/// and opens it a second in, with the inventory, as E would: for recording a machine at work and its panel (the
/// Former, the circuit assembler, the lifepod's Fabricator, the furnace). With <c>--role=&lt;role&gt;</c> an opened crate
/// is made that as it opens, a requester asking for <see cref="Requested"/> (0062).
/// </summary>
public partial class OpenDemo(Ui ui, string kind, Func<Registry> m, Role? role = null, Action<Command>? send = null) : Node
{
    private const double OpenAt = 1.0;
    /// <summary>What a crate made a requester asks for: the full crate provides it, so drones bring it.</summary>
    public static readonly Request Requested = new("circuit", 5);
    private double _t;
    private bool _opened;

    /// <summary>
    /// Where the hero starts: on the bottom row of the machine of <paramref name="kind"/>, a cell right of it; <c>crate@1</c>
    /// names the second of a kind, in id order.
    /// </summary>
    public static int StartOf(Registry m, string kind)
    {
        var machine = Nth(m, kind);
        var spec = Content.Machines.Get(machine.Kind);
        return machine.Cell + (spec.Height - 1) * m.Grid.Width + spec.Width + 1;
    }

    public override void _Process(double delta)
    {
        _t += delta;
        if (_opened || _t < OpenAt) return;
        _opened = true;
        int id = Nth(m(), kind).Id;
        if (role is { } r && send != null)
        {
            send(new SetRole(id, r));
            if (r == Role.Requester) send(new SetRequests(id, [Requested]));
        }
        ui.OpenCrate.Set(id);
    }

    private static Sim.Grid.Machine Nth(Registry m, string kind)
    {
        var parts = kind.Split('@');
        return m.Grid.MachinesOf(parts[0])[parts.Length > 1 ? int.Parse(parts[1]) : 0];
    }
}
