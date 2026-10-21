using System;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Effects;

/// <summary>A hit's point on what it struck, in cells, and the way out of the face it struck there, a unit vector.</summary>
public readonly record struct HitPoint(double X, double Y, double Nx, double Ny);

/// <summary>
/// Where a hit lands and what it strikes (0094), chosen from what the sim says is there, as <c>GroundSounds</c> chooses its
/// sound: the natural wall at the cell, else the growth the hit takes; the point on the cell's face toward the hero, the share of
/// a whole hit a tool's hit throws. The looks are <see cref="HitLooks"/>'; this picks from them.
/// </summary>
public static class Impacts
{
    /// <summary>A hit at <paramref name="cell"/>: the natural wall's material there decides, else the growth's kind; none for what is never struck.</summary>
    public static Impact? At(IGridQueries grid, BuildingModule building, int cell)
    {
        if (grid.WallAt(cell) is { Natural: true } wall) return HitLooks.Ground.TryGetValue(wall.Id, out var ground) ? ground : null;
        return building.PlantAt(cell) is { } plant && HitLooks.Growths.TryGetValue(plant.Kind, out var growth) ? growth : null;
    }

    /// <summary>
    /// The share of a whole hit, the axe's, that one of <paramref name="tool"/>'s throws: its pace over the axe's, so a drill's ten
    /// hits a second throw as much as the axe's two; 1 for a tool as slow as the axe or slower, or one that is no weapon.
    /// </summary>
    public static double ShareOf(string? tool) =>
        Spec.Weapons.Of(tool) is { } weapon ? Math.Min(1, (double)weapon.Pace / Spec.Weapons.Axe.Pace) : 1;

    /// <summary>
    /// The share of a whole hit <paramref name="tool"/>'s hit at <paramref name="cell"/> throws: a tool's own (<see cref="ShareOf"/>),
    /// but the whole for the hit that breaks the wall there or fells the growth, once an object, a crystal's only hit among them.
    /// </summary>
    public static double ShareAt(IGridQueries grid, BuildingModule building, int cell, string tool)
    {
        int damage = Spec.Weapons.Of(tool)?.Damage ?? 0;
        if (grid.WallAt(cell) is { Natural: true } wall) return CrackAt(building, cell) + damage >= wall.Strength ? 1 : ShareOf(tool);
        if (building.PlantAt(cell) is not { } plant) return ShareOf(tool);
        return CrackAt(building, plant.Cell) + damage >= Spec.Flora.Get(plant.Kind).Strength ? 1 : ShareOf(tool);
    }

    private static int CrackAt(BuildingModule building, int cell)
    {
        foreach (var crack in building.Cracks)
            if (crack.Cell == cell) return crack.Damage;
        return 0;
    }

    /// <summary>
    /// Where a hit on <paramref name="cell"/> from a hero at (<paramref name="fromX"/>, <paramref name="fromY"/>), in cells, lands:
    /// on a wall, the face of its solid part open to the air (a whole wall's four sides, a slope's two and its diagonal) turned
    /// most toward the hero, else the closed one turned most toward it, at the point of it nearest the hero, kept off its
    /// corners; on a plant, its cell's middle drawn <see cref="HitLooks.PlantInset"/> of a cell toward the hero.
    /// </summary>
    public static HitPoint On(IGridQueries grid, int cell, double fromX, double fromY)
    {
        int w = grid.Width, cx = cell % w, cy = cell / w;
        double hx = fromX - cx, hy = fromY - cy;
        if (grid.StructureAt(cell) != Structure.Wall)
        {
            var (ux, uy) = Unit(hx - 0.5, hy - 0.5, 0, -1);
            return new HitPoint(cx + 0.5 + ux * HitLooks.PlantInset, cy + 0.5 + uy * HitLooks.PlantInset, ux, uy);
        }
        int shape = grid.ShapeAt(cell);
        var (mx, my) = Middle(shape);
        var (dx, dy) = Unit(hx - mx, hy - my, 0, -1);
        Face? best = null;
        double bestScore = double.NegativeInfinity;
        foreach (var face in FacesOf(shape))
        {
            // Particles must come out into the air to be seen: any open face beats every closed one.
            double score = face.Nx * dx + face.Ny * dy + (Open(grid, cx, cy, face) ? 3 : 0);
            if (score > bestScore) (best, bestScore) = (face, score);
        }
        var f = best!.Value;
        double ex = f.Bx - f.Ax, ey = f.By - f.Ay;
        double along = ((hx - f.Ax) * ex + (hy - f.Ay) * ey) / (ex * ex + ey * ey);
        along = Math.Clamp(along, HitLooks.FaceMargin, 1 - HitLooks.FaceMargin);
        return new HitPoint(cx + f.Ax + ex * along, cy + f.Ay + ey * along, f.Nx, f.Ny);
    }

    /// <summary>
    /// The floor under <paramref name="point"/>, in cells: the top of the first wall or floor down its column from the open cell
    /// just off its face, within <see cref="HitLooks.FloorRows"/>; a slope's surface where it is. None past them: chips fall on.
    /// </summary>
    public static double? FloorUnder(IGridQueries grid, HitPoint point)
    {
        double x = point.X + point.Nx * 0.05, y = point.Y + point.Ny * 0.05;
        int column = (int)Math.Floor(x), row = (int)Math.Floor(y);
        if (column < 0 || column >= grid.Width) return null;
        for (int r = Math.Max(0, row); r <= row + HitLooks.FloorRows && r < grid.Height; r++)
        {
            int cell = r * grid.Width + column;
            int structure = grid.StructureAt(cell);
            if (structure is not (Structure.Wall or Structure.Floor)) continue;
            double top = r + (double)Shape.TopAt(grid.ShapeAt(cell), (int)((x - column) * Shape.Unit)) / Shape.Unit;
            // The cell off the face is open above its slope: chips land on the slope under the point, not over it.
            if (top >= y - 1e-6) return top;
        }
        return null;
    }

    /// <summary>A face of a wall's solid part, from (Ax, Ay) to (Bx, By) within its cell, its way out (Nx, Ny), and the side it opens to, if any.</summary>
    private readonly record struct Face(double Ax, double Ay, double Bx, double By, double Nx, double Ny, int Dx, int Dy);

    private static readonly double Diagonal = Math.Sqrt(0.5);
    private static readonly Face Top = new(0, 0, 1, 0, 0, -1, 0, -1), Bottom = new(0, 1, 1, 1, 0, 1, 0, 1);
    private static readonly Face Left = new(0, 0, 0, 1, -1, 0, -1, 0), Right = new(1, 0, 1, 1, 1, 0, 1, 0);

    /// <summary>The faces of a wall of <paramref name="shape"/>: a whole wall's four sides; a slope's two whole sides and its diagonal.</summary>
    private static Face[] FacesOf(int shape) => shape switch
    {
        Shape.BottomRight => [Right, Bottom, new(0, 1, 1, 0, -Diagonal, -Diagonal, 0, 0)],
        Shape.BottomLeft => [Left, Bottom, new(0, 0, 1, 1, Diagonal, -Diagonal, 0, 0)],
        Shape.TopLeft => [Top, Left, new(1, 0, 0, 1, Diagonal, Diagonal, 0, 0)],
        Shape.TopRight => [Top, Right, new(0, 0, 1, 1, -Diagonal, Diagonal, 0, 0)],
        _ => [Top, Right, Bottom, Left],
    };

    /// <summary>The middle of a wall's solid part, within its cell: a slope's triangle's.</summary>
    private static (double X, double Y) Middle(int shape) => shape switch
    {
        Shape.BottomRight => (2 / 3.0, 2 / 3.0),
        Shape.BottomLeft => (1 / 3.0, 2 / 3.0),
        Shape.TopLeft => (1 / 3.0, 1 / 3.0),
        Shape.TopRight => (2 / 3.0, 1 / 3.0),
        _ => (0.5, 0.5),
    };

    /// <summary>Whether <paramref name="face"/> of the wall at (<paramref name="cx"/>, <paramref name="cy"/>) meets the air: a diagonal always, a side where the next cell does not close it.</summary>
    private static bool Open(IGridQueries grid, int cx, int cy, Face face)
    {
        if (face.Dx == 0 && face.Dy == 0) return true;
        int x = cx + face.Dx, y = cy + face.Dy;
        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return false;
        int next = y * grid.Width + x;
        if (grid.StructureAt(next) != Structure.Wall) return true;
        // A slope next to it closes this side only if its own solid side faces it.
        int facing = face.Dx > 0 ? Doorways.Left : face.Dx < 0 ? Doorways.Right : face.Dy > 0 ? Doorways.Up : Doorways.Down;
        return (Shape.ClosedSides(grid.ShapeAt(next)) & facing) == 0;
    }

    private static (double X, double Y) Unit(double x, double y, double fallbackX, double fallbackY)
    {
        double length = Math.Sqrt(x * x + y * y);
        return length < 1e-9 ? (fallbackX, fallbackY) : (x / length, y / length);
    }
}
