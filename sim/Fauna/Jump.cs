using Spec = Stillwake.Content.Fauna;

namespace Stillwake.Sim.Fauna;

/// <summary>
/// Jumping (0102): a walker that must turn first tries a leap over the wall or across the gap, its landing found by flying the
/// leap through the grid as the move would, tick by tick; integers only, no draw.
/// </summary>
public sealed partial class FaunaModule
{
    /// <summary>How far over what it must clear a jump is sized to carry the body's feet, in thousandths: room to land on it.</summary>
    private const int JumpMargin = 200;

    /// <summary>The most ticks a leap is flown looking for its landing: past any jump's whole flight.</summary>
    private const int LeapTicks = 120;

    /// <summary>
    /// The speed up of a leap <paramref name="c"/>, standing and blocked going <paramref name="way"/> at <paramref name="speed"/>,
    /// makes over what blocks it, or null to turn: none for a species that never jumps; wandering, only up a wall at its feet as
    /// high as <c>Fauna.WanderLedge</c>, sized for it; fleeing, the lowest of a jump sized for that ledge, one a cell higher and its
    /// whole jump that lands it on the far side.
    /// </summary>
    private int? JumpFor(Spec.Species spec, Creature c, int way, int speed)
    {
        if (spec.Jump <= 0) return null;
        int g = Rules.Fall(Spec.Gravity), across = Math.Min(speed, spec.JumpAcross);
        int low = Leaps.SpeedFor(Spec.WanderLedge * Unit + JumpMargin, g, spec.Jump);
        if (!Fleeing(c.Mode))
        {
            int half = spec.Width * Unit / 2, column = Down(way > 0 ? c.X + half : c.X - half - 1, Unit);
            return Solid(column, c.Y / Unit - 1) && Lands(spec, c, way, low, across) ? low : null;
        }
        int higher = Leaps.SpeedFor((Spec.WanderLedge + 1) * Unit + JumpMargin, g, spec.Jump);
        foreach (int v in new[] { low, higher, spec.Jump })
            if (Lands(spec, c, way, v, across)) return v;
        return null;
    }

    /// <summary>
    /// Whether a leap of <paramref name="c"/> at <paramref name="vy"/> up and <paramref name="across"/> going <paramref name="way"/>,
    /// flown through the grid as <see cref="Move"/> moves it in the air — falling, pressing flush on what it meets — lands it at
    /// least a cell on and no deeper than <c>Fauna.Drop</c> rows down, a drop it turns from walking but leaps down fleeing (off a
    /// pillar it jumped onto): a reachable landing, not the void nor back where it stood.
    /// </summary>
    private bool Lands(Spec.Species spec, Creature c, int way, int vy, int across)
    {
        int g = Rules.Fall(Spec.Gravity), half = spec.Width * Unit / 2;
        int x = c.X, y = c.Y, vx = way * across, up = -vy;
        for (int t = 0; t < LeapTicks; t++)
        {
            up = Math.Min(up + g, Spec.MaxFall);
            int to = x + vx;
            x = Overlaps(spec, to, y) ? Flush(to, vx, half) : to;
            int down = y + up;
            if (!Overlaps(spec, x, down)) y = down;
            else if (up > 0) return (x - c.X) * way >= Unit && Down(down - 1, Unit) * Unit <= c.Y + Spec.Drop * Unit;
            else (y, up) = ((Down(down - spec.Height * Unit, Unit) + 1 + spec.Height) * Unit, 0);
            if (y > c.Y + Spec.Drop * Unit) return false;
        }
        return false;
    }
}

/// <summary>A jump's shape under a planet's gravity (0102): integer, as a creature moves, a tick at a time.</summary>
public static class Leaps
{
    /// <summary>How high a jump at <paramref name="vy"/> a tick up carries the feet under gravity <paramref name="g"/> a tick, in thousandths.</summary>
    public static int Height(int vy, int g)
    {
        int up = -vy, rise = 0;
        while ((up += g) < 0) rise -= up;
        return rise;
    }

    /// <summary>How far across, in thousandths, a jump at <paramref name="vy"/> up and <paramref name="across"/> a tick lands on ground level with where it left.</summary>
    public static int Length(int vy, int across, int g)
    {
        int up = -vy, y = 0, ticks = 0;
        do
        {
            up += g;
            y += up;
            ticks++;
        }
        while (y < 0);
        return across * ticks;
    }

    /// <summary>The least speed up, in tens, whose jump carries the feet <paramref name="height"/> up; at most <paramref name="most"/>.</summary>
    public static int SpeedFor(int height, int g, int most)
    {
        int v = 0;
        while (v < most && Height(v, g) < height) v += 10;
        return Math.Min(v, most);
    }
}
