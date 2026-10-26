using Stillwake.Content;

namespace Stillwake.Sim;

/// <summary>A shot's flight as it stands: its point and speed in thousandths of a cell, and the ticks of flight it has left.</summary>
public readonly record struct ShotMotion(int X, int Y, int VX, int VY, int Left);

/// <summary>
/// How a shot flies (shots-are-the-sims), in whole numbers: the sim's step, and the client's for the shooter's own shot, so the
/// two agree on one world.
/// </summary>
public static class ShotFlight
{
    private const int Unit = 1000;

    /// <summary>
    /// A shot of <paramref name="p"/> fired from (<paramref name="x"/>, <paramref name="y"/>) along <paramref name="heading"/>
    /// (<see cref="Headings"/>): its speed along the heading, rounded toward zero, and its reach's ticks, rounded down.
    /// </summary>
    public static ShotMotion Launch(Projectile p, int x, int y, int heading)
    {
        var (dx, dy) = Rays.Direction(heading);
        return new(x, y, dx * p.Speed / Unit, dy * p.Speed / Unit, p.Reach * Unit / p.Speed);
    }

    /// <summary>
    /// What <paramref name="p"/> gains falling a tick on a surface of <paramref name="kind"/>: <see cref="Creatures.Gravity"/> times
    /// its weight under the kind's gravity; none where it floats (the ship's, open space to a shot) or for no weight.
    /// </summary>
    public static int Gravity(Surfaces.Kind kind, Projectile p) => p.Weight == 0 || kind.Floats ? 0 : kind.Fall(Creatures.Gravity * p.Weight);

    /// <summary>
    /// A tick of <paramref name="f"/>: a tick less left, <paramref name="gravity"/> added to its fall, then the walk from its point to
    /// the new one (<see cref="Rays.Walk"/>), its own cell first: <c>Met</c> is the first cell <paramref name="stops"/> says true of,
    /// the x one first at an exact corner, -1 for none. Cells are asked only, never changed, so the caller applies a hit after.
    /// </summary>
    public static (ShotMotion Next, int Met) Step(ShotMotion f, int gravity, int width, int height, Func<int, bool> stops)
    {
        int vy = f.VY + gravity, met = -1;
        Rays.Walk(width, height, f.X, f.Y, f.VX, vy, cell =>
        {
            if (met < 0 && stops(cell)) met = cell;
            return met >= 0;
        });
        return (new ShotMotion(f.X + f.VX, f.Y + vy, f.VX, vy, f.Left - 1), met);
    }

    /// <summary>Whether <paramref name="f"/>'s point is on a grid <paramref name="width"/> by <paramref name="height"/> cells.</summary>
    public static bool OnGrid(ShotMotion f, int width, int height) => f.X >= 0 && f.Y >= 0 && f.X < width * Unit && f.Y < height * Unit;
}
