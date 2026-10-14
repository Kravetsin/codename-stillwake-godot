using Godot;
using Stillwake.Content;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using SimLayout = Stillwake.Sim.Layout;

namespace Stillwake.Game;

/// <summary>
/// The wreck from the C# sim, drawn cell by cell: rooms in colour, space dark. A placeholder until the
/// client's features are ported (docs/plans/port.md); the sim steps at the physics rate, 20 a second.
/// </summary>
public partial class Main : Node2D
{
    private const int CellPx = 20;
    private static readonly Rect2I View = new(28, 15, 32, 10);
    private World _world = null!;
    private GridModule _grid = null!;
    private RoomsModule _rooms = null!;

    public override void _Ready()
    {
        _grid = new GridModule();
        _rooms = new RoomsModule(_grid);
        _world = World.Create(1, SimLayout.Parse(Wreck.Rows, Wreck.Under), [_grid, _rooms]);
    }

    public override void _PhysicsProcess(double delta)
    {
        _world.Step([]);
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (int y = View.Position.Y; y < View.End.Y; y++)
            for (int x = View.Position.X; x < View.End.X; x++)
            {
                int cell = y * _grid.Width + x;
                var room = _rooms.RoomAt(cell);
                Color c = _grid.StructureAt(cell) switch
                {
                    Structure.Wall => new Color(0.45f, 0.47f, 0.52f),
                    Structure.DoorClosed => new Color(0.8f, 0.48f, 0.23f),
                    _ when room == RoomsModule.Space => new Color(0.04f, 0.05f, 0.08f),
                    _ when room is int id => Color.FromHsv(id * 0.137f % 1f, 0.45f, 0.75f),
                    _ => Colors.Magenta,
                };
                DrawRect(new Rect2((x - View.Position.X) * CellPx, (y - View.Position.Y) * CellPx, CellPx - 1, CellPx - 1), c);
            }
    }
}
