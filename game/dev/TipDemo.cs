using System.Linq;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --testbed --tip=&lt;what&gt;</c>: two seconds in, the pointer held where a tip speaks (tip-signs), for a frame of it:
/// <c>air</c>, the air overlay on and the pointer on an empty cell of an aired room, near its middle; any machine kind, the
/// pointer on the first such machine's middle, out of every overlay.
/// </summary>
public partial class TipDemo(Hands hands, System.Func<Registry> world, string what) : Node
{
    private const double At = 2.0;
    private double _t;
    private bool _held;

    public override void _Process(double delta)
    {
        _t += delta;
        if (_held || _t < At) return;
        _held = true;
        var m = world();
        int width = m.Grid.Width;
        (double X, double Y)? cell = null;
        if (what == "air")
        {
            hands.Ui.AirOn.Set(true);
            var room = m.Atmosphere.RoomsAir().Where(r => r.Budget > 0 && r.Aired > 0).OrderByDescending(r => r.Aired).FirstOrDefault();
            if (room != null && m.Rooms.RoomById(room.Room) is { } r)
            {
                var cells = Enumerable.Range(0, m.Grid.Width * m.Grid.Height)
                    .Where(c => m.Rooms.RoomAt(c) == r.Id && m.Grid.StructureAt(c) == Content.Structure.Empty && m.Grid.MachineAt(c) == null)
                    .ToList();
                double mx = cells.Average(c => c % width), my = cells.Average(c => c / width);
                int at = cells.OrderBy(c => (c % width - mx) * (c % width - mx) + (c / width - my) * (c / width - my)).First();
                cell = (at % width + 0.5, at / width + 0.5);
            }
        }
        else if (m.Grid.MachinesOf(what).FirstOrDefault() is { } machine)
        {
            var spec = Content.Machines.Get(what);
            cell = (machine.Cell % width + spec.Width / 2.0, machine.Cell / width + spec.Height / 2.0);
        }
        if (cell is { } p) hands.Mouse.Script = () => p;
        else GD.PushWarning($"--tip={what}: nothing to point at");
    }
}
