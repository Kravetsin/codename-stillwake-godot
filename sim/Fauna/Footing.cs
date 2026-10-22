using Stillwake.Sim.Grid;
using Spec = Stillwake.Content.Fauna;

namespace Stillwake.Sim.Fauna;

public sealed partial class FaunaModule
{
    /// <summary>
    /// Where the ground lies under <paramref name="c"/>'s back and front feet (<c>Fauna.FootSpan</c> of its width either side of
    /// its middle, the back on the left), in thousandths of a cell down the grid: what the client draws it standing on (0102). The
    /// body stands on the top of a slope's cell, whole; its feet on the ground's own surface, a slope's diagonal, read in its feet's
    /// row and the two under: walking up a run of slopes a body two cells wide stands on the next as its front enters it while its
    /// back foot is still on the ground two rows down, and walking down its front foot is as far ahead. So a whole cell's step of
    /// its body moves neither foot: up and down a slope they go no faster than it walks across. A foot with no ground there hangs
    /// <c>Fauna.Teeter</c> under its body.
    /// </summary>
    public (int Back, int Front) FeetOf(Creature c)
    {
        int span = Spec.Get(c.Species).Width * Unit * Spec.FootSpan / 1000;
        return (GroundAt(c.X - span, c.Y), GroundAt(c.X + span, c.Y));
    }

    /// <summary>The ground's surface under (<paramref name="x"/>, <paramref name="y"/>), a body's feet there, as <see cref="FeetOf"/> reads it.</summary>
    private int GroundAt(int x, int y)
    {
        int column = Down(x, Unit), row = Down(y, Unit);
        if (column < 0 || column >= _grid.Width) return y;
        for (int r = Math.Max(0, row); r <= row + 2 && r < _grid.Height; r++)
        {
            if (!Solid(column, r)) continue;
            int shape = _grid.ShapeAt(r * _grid.Width + column);
            return r * Unit + (Shape.IsFloor(shape) ? Shape.TopAt(shape, x - column * Unit) : 0);
        }
        return y + Spec.Teeter;
    }
}
