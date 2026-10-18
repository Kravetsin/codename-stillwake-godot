using System;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --testbed --open=&lt;kind&gt;</c>: the hero starts just right of the testbed's machine of that kind
/// and opens it a second in (or <c>openAt</c> seconds), with the inventory, as E would: for recording a machine at work and its panel (the
/// Former, the circuit assembler, the lifepod's Fabricator, the furnace, a crate and its drone badge, the drone bay).
/// </summary>
public partial class OpenDemo(Ui ui, string kind, Func<Registry> m, double openAt = 1.0) : Node
{
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
        if (_opened || _t < openAt) return;
        _opened = true;
        ui.OpenCrate.Set(Nth(m(), kind).Id);
    }

    private static Sim.Grid.Machine Nth(Registry m, string kind)
    {
        var parts = kind.Split('@');
        return m.Grid.MachinesOf(parts[0])[parts.Length > 1 ? int.Parse(parts[1]) : 0];
    }
}
