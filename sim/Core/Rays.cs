using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>
/// A tool's fan of rays walked over the grid in whole numbers (the fan of rays): from a point in thousandths of a cell, each ray
/// visits the cells it crosses in order until the visitor says it stops there, the grid's edge stops it, or its range runs
/// out. A ray through a cell's exact corner visits both cells beside it, the x one first, and stops if either stops it, so two
/// cells touching at a corner close the gap between them. The same walk for the sim and for the client's preview.
/// </summary>
public static class Rays
{
    private const int Cell = 1000;

    /// <summary>The direction of <paramref name="heading"/> (<see cref="Headings"/>) in thousandths: 0 along +x, 64 along +y (down).</summary>
    public static (int X, int Y) Direction(int heading) => (Sine(heading + Headings.Turn / 4), Sine(heading));

    /// <summary>The sine of <paramref name="heading"/> in thousandths, the quarter table mirrored to the whole turn.</summary>
    private static int Sine(int heading)
    {
        const int quarter = Headings.Turn / 4;
        int h = heading & (Headings.Turn - 1), step = h % quarter;
        return (h / quarter) switch
        {
            0 => Headings.Sines[step],
            1 => Headings.Sines[quarter - step],
            2 => -Headings.Sines[step],
            _ => -Headings.Sines[quarter - step],
        };
    }

    /// <summary>
    /// The heading of ray <paramref name="index"/> of <paramref name="weapon"/>'s fan aimed at <paramref name="heading"/>: its rays
    /// spread evenly over its arc, the first on the anticlockwise edge, each offset truncated toward zero; one ray goes straight.
    /// </summary>
    public static int HeadingOf(Weapon weapon, int heading, int index)
    {
        int n = weapon.Rays;
        int offset = n == 1 ? 0 : weapon.Arc * (2 * index - (n - 1)) / (2 * (n - 1));
        return (heading + offset) & (Headings.Turn - 1);
    }

    /// <summary>
    /// Walks every ray of <paramref name="weapon"/>'s fan from (<paramref name="x"/>, <paramref name="y"/>), in thousandths of a
    /// cell, aimed at <paramref name="heading"/>, in ray order (<see cref="Cast"/>); a cell met by several rays is visited by each.
    /// </summary>
    public static void Fan(int width, int height, int x, int y, int heading, Weapon weapon, Func<int, bool> stops)
    {
        for (int i = 0; i < weapon.Rays; i++) Cast(width, height, x, y, HeadingOf(weapon, heading, i), weapon.Range, stops);
    }

    /// <summary>
    /// Walks one ray from (<paramref name="x"/>, <paramref name="y"/>), in thousandths of a cell, along <paramref name="heading"/>
    /// for <paramref name="range"/> thousandths, its end truncated toward its start: <paramref name="stops"/> is asked of each cell
    /// it crosses, its start's first, but one it only touches at its end, and the ray ends at the first that answers true. Off the grid it ends unasked.
    /// </summary>
    public static void Cast(int width, int height, int x, int y, int heading, int range, Func<int, bool> stops)
    {
        var (dx, dy) = Direction(heading);
        long run = (long)dx * range / Cell, rise = (long)dy * range / Cell;
        int sx = Math.Sign(run), sy = Math.Sign(rise);
        long ax = Math.Abs(run), ay = Math.Abs(rise);
        int cx = FloorCell(x), cy = FloorCell(y);
        // How far along each axis the ray goes to its next column and row line: compared across, as fractions of the ray.
        long nx = sx > 0 ? (cx + 1L) * Cell - x : (long)x - (long)cx * Cell;
        long ny = sy > 0 ? (cy + 1L) * Cell - y : (long)y - (long)cy * Cell;
        bool Stops(int cellX, int cellY) =>
            cellX < 0 || cellY < 0 || cellX >= width || cellY >= height || stops(cellY * width + cellX);

        if (Stops(cx, cy)) return;
        while (true)
        {
            bool alongX = sx != 0 && nx < ax, alongY = sy != 0 && ny < ay;
            if (!alongX && !alongY) return;
            int order = !alongX ? 1 : !alongY ? -1 : (nx * ay).CompareTo(ny * ax);
            if (order < 0)
            {
                cx += sx;
                nx += Cell;
            }
            else if (order > 0)
            {
                cy += sy;
                ny += Cell;
            }
            else
            {
                bool side = Stops(cx + sx, cy), below = Stops(cx, cy + sy);
                if (side || below) return;
                cx += sx;
                cy += sy;
                nx += Cell;
                ny += Cell;
            }
            if (Stops(cx, cy)) return;
        }
    }

    /// <summary>The cell column or row of <paramref name="at"/> thousandths, rounded down past zero too.</summary>
    private static int FloorCell(int at) => at >= 0 ? at / Cell : -((Cell - 1 - at) / Cell);
}
