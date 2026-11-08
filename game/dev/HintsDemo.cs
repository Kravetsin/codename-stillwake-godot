using System;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --testbed --hints</c>: the plan's hints in turn (build-hints), a step every <see cref="Every"/> seconds from
/// <see cref="From"/>: nothing in hand, wiring, piping, the valve's card, a wall planned with the pointer on it, the pointer
/// on the generator. A recording's every 90th frame at 60 fps falls inside each step.
/// </summary>
public partial class HintsDemo(Hands hands, Func<Registry> world, Action<Command> send) : Node
{
    private const double From = 1.0, Every = 1.5;
    private double _t;
    private int _step = -1;
    private (double X, double Y)? _pointer;

    public override void _Process(double delta)
    {
        _t += delta;
        int step = _t < From ? -1 : (int)((_t - From) / Every);
        if (step == _step || step > 5) return;
        _step = step;
        var m = world();
        var grid = m.Grid;
        int w = grid.Width;
        if (grid.MachinesOf("generator").FirstOrDefault() is not { } generator) return;
        var spec = Machines.Get("generator");
        (double, double) on = (generator.Cell % w + spec.Width / 2.0, generator.Cell / w + spec.Height / 2.0);
        switch (step)
        {
            case 0:
                hands.Plan.Toggle();
                hands.Mouse.Script = () => _pointer;
                _pointer = (on.Item1, on.Item2 - 6);
                break;
            case 1:
                hands.Plan.SetMode(PlanMode.Wiring);
                break;
            case 2:
                hands.Plan.SetMode(PlanMode.Piping);
                break;
            case 3:
                hands.Plan.PickToolCard(Tools.Valve);
                break;
            case 4:
                hands.Plan.SetMode(PlanMode.Build);
                int cell = Enumerable.Range(0, w * grid.Height).First(c =>
                    m.Rooms.RoomAt(c) != null && grid.StructureAt(c) == Structure.Empty && grid.MachineAt(c) == null && grid.StructureAt(c + w) != Structure.Empty);
                send(new PlacePlan(cell, Placement.Wall()));
                _pointer = (cell % w + 0.5, cell / w + 0.5);
                break;
            case 5:
                _pointer = on;
                break;
        }
    }
}
