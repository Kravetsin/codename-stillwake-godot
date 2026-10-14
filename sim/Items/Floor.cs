using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Tune = Stillwake.Content.Floor;

namespace Stillwake.Sim.Items;

/// <summary>Stacks on the floor (packages/sim/src/modules/items/floor.ts): laid down, moved a tick, merged.</summary>
internal static class FloorRules
{
    /// <summary>Thousandths of a cell: a stack's position and speed, as the hero's and debris's are.</summary>
    public const int Unit = 1000;

    /// <summary>Division rounding down, as Math.floor does, for positions left of or above the grid.</summary>
    private static int Down(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

    /// <summary>Built up: a wall, a floor, a closed door or a machine. A stack rests on one and rises out of one.</summary>
    private static bool Built(IGridQueries grid, int cell) =>
        grid.StructureAt(cell) is Structure.Wall or Structure.Floor or Structure.DoorClosed || grid.MachineAt(cell) != null;

    /// <summary>Built, or past the grid's edge: what stops a stack inside the ship.</summary>
    private static bool Blocks(IGridQueries grid, int x, int y) =>
        x < 0 || y < 0 || x >= grid.Width || y >= grid.Height || Built(grid, y * grid.Width + x);

    /// <summary>The cell a stack is in: the one its bottom middle point lies just above.</summary>
    public static int CellOf(int x, int y, int width) => Down(y - 1, Unit) * width + Down(x, Unit);

    /// <summary>
    /// Lays <paramref name="count"/> of <paramref name="item"/> on cell <paramref name="cell"/>, dropped by
    /// peer <paramref name="by"/> and thrown at <paramref name="vx"/>: as new stacks of at most the stack
    /// limit each, from the cell's bottom middle, rising a little as they go.
    /// </summary>
    public static void LayDown(ItemsState state, string item, int count, int cell, int vx, int width, int by)
    {
        int x = cell % width * Unit + Unit / 2;
        int y = (cell / width + 1) * Unit;
        int limit = Content.Items.Get(item).Stack;
        for (int left = count; left > 0;)
        {
            int n = Math.Min(left, limit);
            state.Floor.Add(new FloorStack
            {
                Id = state.NextFloorId++,
                Item = item,
                Count = n,
                Cell = cell,
                X = x,
                Y = y,
                Vx = vx,
                Vy = vx == 0 ? 0 : -Tune.ThrowLift,
                By = by,
                At = state.Ticks,
            });
            left -= n;
        }
    }

    /// <summary>
    /// A tick of one stack. Inside something built it rises a cell. In space it flies on as it was
    /// thrown, and with everything outside it drifts back past the moving ship. Inside the ship it
    /// falls, stops against what is built, and slides to a stop on what it lands on. Whether it is
    /// still on the grid: past the edge it is gone.
    /// </summary>
    private static bool Move(FloorStack s, IGridQueries grid, IRoomsQueries rooms)
    {
        int width = grid.Width, height = grid.Height;
        int row = Down(s.Y - 1, Unit);
        if (Built(grid, s.Cell))
        {
            if (row > 0) s.Y -= Unit;
            s.Vx = 0;
            s.Vy = 0;
        }
        else if (rooms.RoomAt(s.Cell) == RoomsModule.Space)
        {
            s.X += s.Vx - Tune.SpaceDrift;
            s.Y += s.Vy;
        }
        else
        {
            s.Vy = Math.Min(s.Vy + Tune.Gravity, Tune.MaxFall);
            int nx = s.X + s.Vx;
            if (Blocks(grid, Down(nx, Unit), row)) s.Vx = 0;
            else s.X = nx;
            int x = Down(s.X, Unit);
            if (s.Vy > 0)
            {
                int to = s.Y + s.Vy;
                bool landed = false;
                for (int r = row + 1; r <= Down(to - 1, Unit); r++)
                {
                    if (!Blocks(grid, x, r)) continue;
                    s.Y = r * Unit;
                    s.Vy = 0;
                    landed = true;
                    break;
                }
                if (!landed) s.Y = to;
            }
            else if (s.Vy < 0)
            {
                int to = s.Y + s.Vy;
                if (Blocks(grid, x, Down(to - 1, Unit))) s.Vy = 0;
                else s.Y = to;
            }
            // Resting on something: it slides to a stop.
            if (s.Vy == 0 && Blocks(grid, x, Down(s.Y - 1, Unit) + 1))
                s.Vx = Math.Sign(s.Vx) * Math.Max(0, Math.Abs(s.Vx) - Tune.Friction);
        }
        if (s.X < 0 || s.Y <= 0 || s.X >= width * Unit || s.Y > height * Unit) return false;
        s.Cell = CellOf(s.X, s.Y, width);
        return true;
    }

    /// <summary>
    /// A tick of the floor: each stack moves, in id order, and one that left the grid is gone for good:
    /// thrown out into space. Then stacks of one item at rest in a cell merge into the oldest, up to its limit.
    /// </summary>
    public static void Settle(ItemsState state, IGridQueries grid, IRoomsQueries rooms)
    {
        var kept = new List<FloorStack>();
        foreach (var s in state.Floor)
        {
            if (!Move(s, grid, rooms)) continue;
            bool resting = s.Vx == 0 && s.Vy == 0;
            foreach (var into in kept)
            {
                if (!resting || into.Cell != s.Cell || into.Item != s.Item) continue;
                if (into.Vx != 0 || into.Vy != 0) continue;
                int n = Math.Min(s.Count, Content.Items.Get(s.Item).Stack - into.Count);
                into.Count += n;
                s.Count -= n;
            }
            if (s.Count > 0) kept.Add(s);
        }
        state.Floor = kept;
    }
}
