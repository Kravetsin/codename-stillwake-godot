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

    /// <summary>Built up: a wall, a floor, a closed door or a machine. A stack rests on one and is pushed out of one.</summary>
    private static bool Built(IGridQueries grid, int cell) =>
        grid.StructureAt(cell) is Structure.Wall or Structure.Floor or Structure.DoorClosed || grid.MachineAt(cell) != null;

    /// <summary>Built, or past the grid's edge: what stops a stack inside the ship.</summary>
    private static bool Blocks(IGridQueries grid, int x, int y) =>
        x < 0 || y < 0 || x >= grid.Width || y >= grid.Height || Built(grid, y * grid.Width + x);

    /// <summary>The floor slope (◢ ◣) cell (x, y) is cut to, or <see cref="Shape.None"/>: a stack rests on its diagonal, not its cell (0069).</summary>
    private static int SlopeAt(IGridQueries grid, int x, int y)
    {
        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return Shape.None;
        int shape = grid.ShapeAt(y * grid.Width + x);
        return Shape.IsFloor(shape) ? shape : Shape.None;
    }

    /// <summary>
    /// Whether the point (<paramref name="x"/>, <paramref name="y"/>), in thousandths, is in what stops a stack: past the
    /// grid's edge, in a built cell, or in a floor slope's solid half; its open half is open. A ceiling slope is whole.
    /// </summary>
    private static bool InSolid(IGridQueries grid, int x, int y)
    {
        int cx = Down(x, Unit), cy = Down(y, Unit);
        if (!Blocks(grid, cx, cy)) return false;
        int slope = SlopeAt(grid, cx, cy);
        return slope == Shape.None || Shape.Solid(slope, x - cx * Unit, y - cy * Unit);
    }

    /// <summary>Where a stack falling at column <paramref name="x"/> lands in row <paramref name="r"/>: its top, or a slope's diagonal there.</summary>
    private static int TopOf(IGridQueries grid, int x, int r)
    {
        int cx = Down(x, Unit), slope = SlopeAt(grid, cx, r);
        return r * Unit + (slope == Shape.None ? 0 : Shape.TopAt(slope, x - cx * Unit));
    }

    /// <summary>The cell a stack is in: the one its bottom middle point lies just above.</summary>
    public static int CellOf(int x, int y, int width) => Down(y - 1, Unit) * width + Down(x, Unit);

    /// <summary>The bottom middle point of <paramref name="cell"/>: where a stack laid on it lies.</summary>
    private static (int X, int Y) BottomMiddle(int cell, int width) => (cell % width * Unit + Unit / 2, (cell / width + 1) * Unit);

    /// <summary>
    /// Lays <paramref name="stack"/> on cell <paramref name="cell"/>, dropped by peer <paramref name="by"/>
    /// and thrown at <paramref name="vx"/>: as new stacks of at most the stack limit each, every one holding
    /// what its item holds of its own, from the cell's bottom middle, rising a little as they go; each part of
    /// <paramref name="body"/> when a hero left it where it died.
    /// </summary>
    public static void LayDown(ItemsState state, Stack stack, int cell, int vx, int width, int by, Body? body = null)
    {
        var (x, y) = BottomMiddle(cell, width);
        int limit = Content.Items.Get(stack.Item).Stack;
        for (int left = stack.Count; left > 0;)
        {
            int n = Math.Min(left, limit);
            state.Floor.Add(new FloorStack
            {
                Id = state.NextFloorId++,
                Item = stack.Item,
                Count = n,
                Charge = stack.Charge,
                Cell = cell,
                X = x,
                Y = y,
                Vx = vx,
                Vy = vx == 0 ? 0 : -Tune.ThrowLift,
                By = by,
                At = state.Ticks,
                Body = body,
            });
            left -= n;
        }
    }

    /// <summary>
    /// Where a stack inside something built at <paramref name="from"/> is pushed out to: breadth-first
    /// through built cells, up to <see cref="Tune.PushReach"/> steps, the nearest open cell in a room;
    /// with none, the nearest open cell (space); ties by cell index. Null when nothing open is in reach.
    /// </summary>
    private static int? PushedTo(int from, IGridQueries grid, IRoomsQueries rooms)
    {
        int width = grid.Width, size = width * grid.Height;
        var seen = new List<int> { from };
        var layer = new List<int> { from };
        int? open = null;
        for (int step = 1; step <= Tune.PushReach && layer.Count > 0; step++)
        {
            var next = new List<int>();
            int? room = null, space = null;
            foreach (int c in layer)
            {
                int x = c % width;
                foreach (int n in new[] { c - width, x > 0 ? c - 1 : -1, x < width - 1 ? c + 1 : -1, c + width })
                {
                    if (n < 0 || n >= size || seen.Contains(n)) continue;
                    seen.Add(n);
                    if (Built(grid, n)) next.Add(n);
                    else if (rooms.RoomAt(n) >= 0) room = Math.Min(room ?? n, n);
                    else space = Math.Min(space ?? n, n);
                }
            }
            if (room != null) return room;
            open ??= space;
            layer = next;
        }
        return open;
    }

    /// <summary>
    /// A tick of one stack. Inside something built it is pushed out, still, into the cell
    /// <see cref="PushedTo"/> finds, or rises a cell with nothing open in reach. In space it flies on as
    /// it was thrown, and with everything outside it drifts back past the moving ship. Inside the ship it
    /// falls, stops against what is built, and slides to a stop on what it lands on. Whether it is
    /// still on the grid: past the edge it is gone.
    /// </summary>
    private static bool Move(FloorStack s, IGridQueries grid, IRoomsQueries rooms)
    {
        int width = grid.Width, height = grid.Height;
        int row = Down(s.Y - 1, Unit);
        if (InSolid(grid, s.X, s.Y - 1))
        {
            if (PushedTo(s.Cell, grid, rooms) is int to) (s.X, s.Y) = BottomMiddle(to, width);
            else if (row > 0) s.Y -= Unit;
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
            if (!InSolid(grid, nx, s.Y - 1)) s.X = nx;
            // Into a slope's diagonal no steeper than it moves, it skids up onto it; else it stops.
            else if (SlopeAt(grid, Down(nx, Unit), row) != Shape.None && s.Y - TopOf(grid, nx, row) <= Math.Abs(s.Vx)
                     && !InSolid(grid, nx, TopOf(grid, nx, row) - 1))
                (s.X, s.Y) = (nx, TopOf(grid, nx, row));
            else s.Vx = 0;
            row = Down(s.Y - 1, Unit);
            int x = Down(s.X, Unit);
            if (s.Vy > 0)
            {
                int to = s.Y + s.Vy;
                bool landed = false;
                // From its own row, whose slope it may be above; a whole cell there it is not in.
                for (int r = row; r <= Down(to - 1, Unit); r++)
                {
                    int top = TopOf(grid, s.X, r);
                    if (!Blocks(grid, x, r) || top < s.Y || top > to) continue;
                    s.Y = top;
                    s.Vy = 0;
                    landed = true;
                    break;
                }
                if (!landed) s.Y = to;
            }
            else if (s.Vy < 0)
            {
                int to = s.Y + s.Vy;
                if (InSolid(grid, s.X, to - 1)) s.Vy = 0;
                else s.Y = to;
            }
            // Resting on something, a slope's diagonal too: it slides to a stop.
            bool resting = SlopeAt(grid, x, Down(s.Y - 1, Unit)) != Shape.None ? InSolid(grid, s.X, s.Y) : Blocks(grid, x, Down(s.Y - 1, Unit) + 1);
            if (s.Vy == 0 && resting)
                s.Vx = Math.Sign(s.Vx) * Math.Max(0, Math.Abs(s.Vx) - Tune.Friction);
        }
        if (s.X < 0 || s.Y <= 0 || s.X >= width * Unit || s.Y > height * Unit) return false;
        s.Cell = CellOf(s.X, s.Y, width);
        return true;
    }

    /// <summary>
    /// A tick of the floor: each stack moves, in id order, and one that left the grid is gone for good:
    /// thrown out into space; so is a body's stack <see cref="Tune.BodyLasts"/> ticks after its death, wherever it
    /// is. Then stacks of one item at rest in a cell merge into the oldest, up to its limit, only within one body
    /// or outside any.
    /// </summary>
    public static void Settle(ItemsState state, IGridQueries grid, IRoomsQueries rooms)
    {
        var kept = new List<FloorStack>();
        foreach (var s in state.Floor)
        {
            if (s.Body is { } body && state.Ticks - body.Died >= Tune.BodyLasts) continue;
            if (!Move(s, grid, rooms)) continue;
            bool resting = s.Vx == 0 && s.Vy == 0;
            foreach (var into in kept)
            {
                if (!resting || into.Cell != s.Cell || into.Item != s.Item || into.Body != s.Body) continue;
                if (into.Vx != 0 || into.Vy != 0 || !Stacks.Alike(into.AsStack(), s.AsStack())) continue;
                int n = Math.Min(s.Count, Content.Items.Get(s.Item).Stack - into.Count);
                into.Count += n;
                s.Count -= n;
            }
            if (s.Count > 0) kept.Add(s);
        }
        state.Floor = kept;
    }
}
