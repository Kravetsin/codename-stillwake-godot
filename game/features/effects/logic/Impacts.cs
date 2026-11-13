using System;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Effects;

/// <summary>A hit's point on what it struck, in cells, and the way out of the face it struck there, a unit vector.</summary>
public readonly record struct HitPoint(double X, double Y, double Nx, double Ny);

/// <summary>
/// Where a hit lands and what it strikes (0094), chosen from what the sim says is there, as <c>GroundSounds</c> chooses its
/// sound: the natural wall at the cell, a built one's metal, else the growth the hit takes; the point on the cell's face toward the hero, the share of
/// a whole hit a tool's hit throws. The looks are <see cref="HitLooks"/>'; this picks from them.
/// </summary>
public static class Impacts
{
    /// <summary>
    /// A hit at <paramref name="cell"/>, as what it strikes there (<see cref="BuildingModule.TargetAt"/>, hit-targets): a natural wall
    /// by its material; what is built, a wall, a piece or a machine, sparks as metal; a pile in the hold as a wreck
    /// (2026-11-07-pile-hits-felt); a plant as its growth; none for open air.
    /// </summary>
    public static Impact? At(IGridQueries grid, BuildingModule building, int cell) => building.TargetAt(cell) switch
    {
        { Kind: TargetKind.Wall } when grid.WallAt(cell) is { Natural: true } wall => HitLooks.GroundOf(wall),
        { Wreck: true } => Impact.Wreck,
        { Kind: TargetKind.Wall or TargetKind.Piece or TargetKind.Machine } => Impact.Alloy,
        { Kind: TargetKind.Plant } when building.PlantAt(cell) is { } plant && HitLooks.Growths.TryGetValue(plant.Kind, out var growth) => growth,
        _ => null,
    };

    /// <summary>
    /// The share of a whole hit, the axe's, that one of <paramref name="tool"/>'s throws: its pace over the axe's, so a drill's ten
    /// hits a second throw as much as the axe's two; 1 for a tool as slow as the axe or slower, or one that is no weapon.
    /// </summary>
    public static double ShareOf(string? tool) =>
        Spec.Weapons.Of(tool) is { } weapon ? Math.Min(1, (double)weapon.Pace / Spec.Weapons.Axe.Pace) : 1;

    /// <summary>
    /// The share of a whole hit <paramref name="tool"/>'s hit at <paramref name="cell"/> throws: a tool's own (<see cref="ShareOf"/>),
    /// but the whole for the hit that breaks what it strikes there, once an object, a crystal's only hit among them: one whose
    /// <paramref name="damage"/>, the sim's for its sender <paramref name="peer"/> (a module's boost in it), breaks it as the sim
    /// would (<see cref="BuildingModule.Breaks"/>), their own ship at a tenth.
    /// </summary>
    public static double ShareAt(BuildingModule building, int cell, string tool, int damage, int peer) =>
        building.Breaks(peer, cell, damage) ? 1 : ShareOf(tool);

    /// <summary>
    /// Where a hit on <paramref name="cell"/> from a hero at (<paramref name="fromX"/>, <paramref name="fromY"/>), in cells, lands:
    /// on a wall, the face of its four open to the air turned
    /// most toward the hero, else the closed one turned most toward it, at the point of it nearest the hero, kept off its
    /// corners; on a plant, its cell's middle drawn <see cref="HitLooks.PlantInset"/> of a cell toward the hero.
    /// </summary>
    public static HitPoint On(IGridQueries grid, int cell, double fromX, double fromY)
    {
        int w = grid.Width, cx = cell % w, cy = cell / w;
        double hx = fromX - cx, hy = fromY - cy;
        if (grid.WallAt(cell) == null)
        {
            var (ux, uy) = Unit(hx - 0.5, hy - 0.5, 0, -1);
            return new HitPoint(cx + 0.5 + ux * HitLooks.PlantInset, cy + 0.5 + uy * HitLooks.PlantInset, ux, uy);
        }
        var (dx, dy) = Unit(hx - 0.5, hy - 0.5, 0, -1);
        Face? best = null;
        double bestScore = double.NegativeInfinity;
        foreach (var face in Faces)
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
    /// The floor under <paramref name="point"/>, in cells: the top of the first solid cell down its column from the open cell
    /// just off its face, within <see cref="HitLooks.FloorRows"/>. None past them: chips fall on.
    /// </summary>
    public static double? FloorUnder(IGridQueries grid, HitPoint point)
    {
        double x = point.X + point.Nx * 0.05, y = point.Y + point.Ny * 0.05;
        int column = (int)Math.Floor(x), row = (int)Math.Floor(y);
        if (column < 0 || column >= grid.Width) return null;
        for (int r = Math.Max(0, row); r <= row + HitLooks.FloorRows && r < grid.Height; r++)
        {
            int cell = r * grid.Width + column;
            if (!grid.Solid(cell)) continue;
            if (r >= y - 1e-6) return r;
        }
        return null;
    }

    /// <summary>A face of a wall's solid part, from (Ax, Ay) to (Bx, By) within its cell, its way out (Nx, Ny), and the side it opens to, if any.</summary>
    private readonly record struct Face(double Ax, double Ay, double Bx, double By, double Nx, double Ny, int Dx, int Dy);

    private static readonly Face Top = new(0, 0, 1, 0, 0, -1, 0, -1), Bottom = new(0, 1, 1, 1, 0, 1, 0, 1);
    private static readonly Face Left = new(0, 0, 0, 1, -1, 0, -1, 0), Right = new(1, 0, 1, 1, 1, 0, 1, 0);

    /// <summary>A wall's four faces.</summary>
    private static readonly Face[] Faces = [Top, Right, Bottom, Left];

    /// <summary>Whether <paramref name="face"/> of the wall at (<paramref name="cx"/>, <paramref name="cy"/>) meets the air: where the next cell is no wall.</summary>
    private static bool Open(IGridQueries grid, int cx, int cy, Face face)
    {
        int x = cx + face.Dx, y = cy + face.Dy;
        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return false;
        return grid.WallAt(y * grid.Width + x) == null;
    }

    private static (double X, double Y) Unit(double x, double y, double fallbackX, double fallbackY)
    {
        double length = Math.Sqrt(x * x + y * y);
        return length < 1e-9 ? (fallbackX, fallbackY) : (x / length, y / length);
    }
}
