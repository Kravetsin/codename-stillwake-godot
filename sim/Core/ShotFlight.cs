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
    private const int Unit = Rays.Cell;

    /// <summary>
    /// A shot fired from <paramref name="gun"/> at (<paramref name="x"/>, <paramref name="y"/>) along <paramref name="heading"/>
    /// (<see cref="Headings"/>): the gun's speed along the heading, rounded toward zero, and its reach's ticks, rounded down.
    /// </summary>
    public static ShotMotion Launch(Gun gun, int x, int y, int heading)
    {
        var (dx, dy) = Rays.Direction(heading);
        return new(x, y, dx * gun.Speed / Unit, dy * gun.Speed / Unit, gun.Reach * Unit / gun.Speed);
    }

    /// <summary>
    /// What <paramref name="p"/> fired from <paramref name="gun"/> gains falling a tick on a surface of <paramref name="kind"/>:
    /// <see cref="Creatures.Gravity"/> times its weight under the kind's gravity, times the gun's <c>Fall</c> thousandths, rounded down
    /// (2026-10-27-the-railgun), a unit at least as the kind's fall is; no gun, the whole; none <paramref name="afloat"/>, where a hero
    /// would float (open space, <c>Rooms.Afloat</c>), or for no weight. Aboard the ship it falls by the ship's own gravity, the
    /// whole, harder than on a planet (2026-10-27-shots-fall-aboard).
    /// </summary>
    public static int Gravity(Surfaces.Kind kind, Projectile p, Gun? gun, bool afloat) =>
        p.Weight == 0 || afloat ? 0 : Math.Max(1, kind.Fall(Creatures.Gravity * p.Weight) * (gun?.Fall ?? 1000) / 1000);

    /// <summary>
    /// A tick of <paramref name="f"/>: a tick less left, then the walk from its point to the new one (<see cref="Rays.Walk"/>), its
    /// own cell first, falling by its fall and half of <paramref name="gravity"/> (rounded down), its fall then gaining the whole: the
    /// tick's mean, so its arc is the parabola's at every tick. <c>Met</c> is the first cell <paramref name="stops"/> says true of, the
    /// x one first at an exact corner, -1 for none; <c>Last</c> the last cell the walk asked of, on the grid. Each cell is asked
    /// once, none after <c>Met</c>: the sim's <paramref name="stops"/> hits what it meets there as it is asked, the walk going on
    /// over the world so changed (2026-10-27-shots-pierce); a client's copy only asks.
    /// </summary>
    public static (ShotMotion Next, int Met, int Last) Step(ShotMotion f, int gravity, int width, int height, Func<int, bool> stops)
    {
        int fall = f.VY + gravity / 2, met = -1, last = -1;
        Rays.Walk(width, height, f.X, f.Y, f.VX, fall, cell =>
        {
            last = cell;
            if (met < 0 && stops(cell)) met = cell;
            return met >= 0;
        });
        return (new ShotMotion(f.X + f.VX, f.Y + fall, f.VX, f.VY + gravity, f.Left - 1), met, last);
    }

    /// <summary>Whether (<paramref name="x"/>, <paramref name="y"/>), in thousandths of a cell, is on a grid <paramref name="width"/> by <paramref name="height"/> cells.</summary>
    public static bool OnGrid(int x, int y, int width, int height) => x >= 0 && y >= 0 && x < width * Unit && y < height * Unit;
}
