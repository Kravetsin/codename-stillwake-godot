using System;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --testbed --open=&lt;kind&gt;</c>: the hero starts just right of the testbed's machine of that kind
/// and opens it a second in, with the inventory, as E would: for recording a machine at work and its panel (the
/// Former, the circuit assembler, the lifepod's Fabricator, the furnace).
/// </summary>
public partial class OpenDemo(Ui ui, string kind, Func<Registry> m) : Node
{
    private const double OpenAt = 1.0;
    private double _t;
    private bool _opened;

    /// <summary>Where the hero starts: on the bottom row of the machine of <paramref name="kind"/>, a cell right of it.</summary>
    public static int StartOf(Registry m, string kind)
    {
        var machine = m.Grid.MachinesOf(kind)[0];
        var spec = Content.Machines.Get(kind);
        return machine.Cell + (spec.Height - 1) * m.Grid.Width + spec.Width + 1;
    }

    public override void _Process(double delta)
    {
        _t += delta;
        if (_opened || _t < OpenAt) return;
        _opened = true;
        ui.OpenCrate.Set(m().Grid.MachinesOf(kind)[0].Id);
    }
}
