using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Building;

/// <summary>A cell by its column and row.</summary>
public readonly record struct Spot(int X, int Y);

/// <summary>One cell of a drag: why the tool cannot work there, if it cannot, and what it sends if it can.</summary>
public sealed record Verdict(int Cell, string? Problem, IReadOnlyList<Command> Commands);

/// <summary>
/// A drag of the left or right button over the grid (features/building/drag.ts): the line of cells it
/// covers and what the card, or removal, would do in each, as the sim judges it now.
/// </summary>
public static class Drag
{
    /// <summary>The cell under a world point, kept on the grid: a drag that leaves it runs along the edge.</summary>
    public static Spot SpotAt(int width, int height, (double X, double Y) p) =>
        new(Math.Clamp((int)Math.Floor(p.X), 0, width - 1), Math.Clamp((int)Math.Floor(p.Y), 0, height - 1));

    public static bool OnGrid(int width, int height, (double X, double Y) p) =>
        p.X >= 0 && p.Y >= 0 && p.X < width && p.Y < height;

    /// <summary>
    /// One left-button event in a drag: a press on the grid starts one where it is, a release ends the
    /// one under way and gives its two ends. A press off the grid starts nothing.
    /// </summary>
    public static (Spot? Pressed, (Spot From, Spot To)? Line) OnButton(Spot? pressed, ButtonEvent button, int width, int height)
    {
        if (button.Down) return (OnGrid(width, height, button.At) ? SpotAt(width, height, button.At) : null, null);
        if (pressed is not Spot from) return (null, null);
        return (null, (from, SpotAt(width, height, button.At)));
    }

    /// <summary>
    /// The brush <paramref name="act"/> lays at each point of a drag, columns by rows, with a brush <paramref name="side"/> cells
    /// across (<see cref="Brush"/>): walls and back walls the square; a ladder, a door and a hatch their piece (0104); a floor its
    /// top row, one cell thick as a doubled map draws it; a pipe, a valve, a machine and a removal one cell, as a pipe run is one
    /// cell thick and a footprint placed once.
    /// </summary>
    public static (int W, int H) StampOf(Act act, int side) => act switch
    {
        // A piece is its own size, a ladder's two by two, a door's two by six, however fine the brush (0104).
        PlaceTool p when Spec.Builds.PieceOf(p.Placement.Kind) is { } piece => piece,
        PlaceTool { Placement.Kind: "wall" or "backWall" } => (side, side),
        PlaceTool { Placement.Kind: "floor" } => (side, 1),
        _ => (1, 1),
    };

    /// <summary>
    /// <paramref name="act"/> as a stroke from world point <paramref name="from"/> to <paramref name="to"/> places it: the door's
    /// card a hatch when the stroke runs further across than down (0104), as a doorway drawn across was one; every other as it is.
    /// </summary>
    public static Act Oriented(Act act, (double X, double Y) from, (double X, double Y) to) =>
        act is PlaceTool { Placement.Kind: "door" } door && Math.Abs(to.X - from.X) > Math.Abs(to.Y - from.Y)
            ? door with { Placement = door.Placement with { Kind = Spec.Builds.Hatch } }
            : act;

    /// <summary>The top-left cell of <paramref name="stamp"/> round a world point, kept on the grid; a one-cell stamp is <see cref="SpotAt"/>.</summary>
    public static Spot StampAt(int width, int height, (double X, double Y) p, (int W, int H) stamp)
    {
        var (x, y) = Brush.Corner(width, height, p, stamp.W, stamp.H);
        return new(x, y);
    }

    /// <summary>
    /// The cells a drag from <paramref name="from"/> to <paramref name="to"/>, the top-left cells of its <paramref name="stamp"/>
    /// (one cell unless said), covers: the stamp laid at each cell of a straight line along the longer axis, horizontal on
    /// a tie, its cells ascending, none twice. Walls and back walls fill the rectangle round both ends' stamps instead, row
    /// by row from where it began, so a room's worth is one drag. A machine goes only where the drag began.
    /// </summary>
    public static List<int> LineOf(int width, Act act, Spot from, Spot to, (int W, int H)? stamp = null)
    {
        var (w, h) = stamp ?? (1, 1);
        if (act is PlaceTool { Placement.Layer: "machine" } or PlaceTool { Placement.Kind: "door" or Spec.Builds.Hatch }) return [from.Y * width + from.X];
        // A ladder's pieces laid end to end along the drag, a piece's length a step (0104): their top-left cells only.
        if (act is PlaceTool { Placement.Kind: "ladder" })
        {
            bool along = Math.Abs(to.X - from.X) > Math.Abs(to.Y - from.Y);
            int far = along ? to.X - from.X : to.Y - from.Y, size = along ? w : h;
            var pieces = new List<int>();
            for (int i = 0; i <= Math.Abs(far) / size; i++)
            {
                int x = along ? from.X + Math.Sign(far) * i * size : from.X, y = along ? from.Y : from.Y + Math.Sign(far) * i * size;
                pieces.Add(y * width + x);
            }
            return pieces;
        }
        if (act is PlaceTool { Placement.Kind: "wall" or "backWall" })
        {
            // From the begun stamp's side away from the end to the end stamp's far side, so both stamps are inside.
            bool right = to.X >= from.X, down = to.Y >= from.Y;
            return RectangleOf(width, new(right ? from.X : from.X + w - 1, down ? from.Y : from.Y + h - 1),
                new(right ? to.X + w - 1 : to.X, down ? to.Y + h - 1 : to.Y));
        }
        bool across = Math.Abs(to.X - from.X) >= Math.Abs(to.Y - from.Y);
        int length = across ? Math.Abs(to.X - from.X) : Math.Abs(to.Y - from.Y);
        int step = Math.Sign(across ? to.X - from.X : to.Y - from.Y);
        var cells = new List<int>();
        var seen = new HashSet<int>();
        for (int i = 0; i <= length; i++)
        {
            int x = across ? from.X + i * step : from.X;
            int y = across ? from.Y : from.Y + i * step;
            for (int dy = 0; dy < h; dy++)
                for (int dx = 0; dx < w; dx++)
                    if (seen.Add((y + dy) * width + x + dx)) cells.Add((y + dy) * width + x + dx);
        }
        return cells;
    }

    /// <summary>
    /// The cells a drag covers as <see cref="LineOf"/> lays them, but for a removal begun on a wall's or back wall's
    /// blueprint, with nothing else planned there: that fills the rectangle between its corners as planning them does,
    /// and takes off only those
    /// (<see cref="Judge"/>).
    /// </summary>
    public static List<int> CellsOf(int width, Act act, Spot from, Spot to, BuildingModule building, (int W, int H)? stamp = null) =>
        act is Remove && CancelsWalls(building, from.Y * width + from.X) ? RectangleOf(width, from, to) : LineOf(width, act, from, to, stamp);

    /// <summary>
    /// The cells a stroke of <paramref name="act"/> from world point <paramref name="from"/> to <paramref name="to"/>, in cells,
    /// covers with a brush <paramref name="side"/> cells across: its stamp (<see cref="StampOf"/>) round each end, laid as
    /// <see cref="CellsOf"/> lays a drag.
    /// </summary>
    public static List<int> StrokeOf(int width, int height, Act act, (double X, double Y) from, (double X, double Y) to, BuildingModule building, int side)
    {
        var stamp = StampOf(act, side);
        return CellsOf(width, act, StampAt(width, height, from, stamp), StampAt(width, height, to, stamp), building, stamp);
    }

    /// <summary>Whether a removal begun at <paramref name="cell"/> cancels only walls' and back walls' blueprints: begun where nothing else is planned.</summary>
    private static bool CancelsWalls(BuildingModule building, int cell) =>
        building.PlansAt(cell) is { Count: > 0 } here && here.All(IsWallBlueprint);

    private static bool IsWallBlueprint(Plan plan) => plan.Place is { Kind: "wall" or "backWall" };

    /// <summary>
    /// The most cells a wall rectangle spans each way: judged and drawn every frame of the drag, a whole
    /// 256×128 grid of them took 54 ms, a 32×32 a few.
    /// </summary>
    public const int MostAcross = 32;

    /// <summary>
    /// Every cell from <paramref name="from"/> to <paramref name="to"/>, both corners in, a row at a time from
    /// <paramref name="from"/>'s; the far corner kept within <see cref="MostAcross"/> of it each way.
    /// </summary>
    private static List<int> RectangleOf(int width, Spot from, Spot to)
    {
        to = new(Math.Clamp(to.X, from.X - MostAcross + 1, from.X + MostAcross - 1), Math.Clamp(to.Y, from.Y - MostAcross + 1, from.Y + MostAcross - 1));
        int dx = Math.Sign(to.X - from.X), dy = Math.Sign(to.Y - from.Y);
        var cells = new List<int>((Math.Abs(to.X - from.X) + 1) * (Math.Abs(to.Y - from.Y) + 1));
        for (int y = from.Y; ; y += dy)
        {
            for (int x = from.X; ; x += dx)
            {
                cells.Add(y * width + x);
                if (x == to.X) break;
            }
            if (y == to.Y) break;
        }
        return cells;
    }

    /// <summary>
    /// What <paramref name="act"/> would do in each of <paramref name="cells"/>, as the sim judges it now.
    /// Removing cancels plans if the drag begins on one, a mark included, and marks what is built in its
    /// layers otherwise; begun where only walls and back walls are planned, it cancels only their blueprints.
    /// </summary>
    public static List<Verdict> Judge(Act act, IReadOnlyList<int> cells, BuildingModule building)
    {
        // A machine spans several cells of a line, yet takes one mark and is cancelled once.
        var cancelled = new HashSet<int>();
        var marked = new HashSet<(string, int)>();
        bool cancels = cells.Count > 0 && building.PlansAt(cells[0]).Count > 0;
        bool walls = cancels && CancelsWalls(building, cells[0]);
        var verdicts = new List<Verdict>();
        foreach (int cell in cells)
        {
            switch (act)
            {
                case PlaceTool place:
                    {
                        string? problem = building.Problem(cell, place.Placement);
                        verdicts.Add(new Verdict(cell, problem, problem != null ? [] : [new PlacePlan(cell, place.Placement)]));
                        break;
                    }
                case Remove remove when cancels:
                    {
                        // Begun on a wall's blueprint, a pipe or machine planned in the rectangle is let be.
                        IReadOnlyList<Plan> here = walls ? building.PlansAt(cell).Where(IsWallBlueprint).ToList() : building.PlansAt(cell);
                        var ids = here.Select(p => p.Id).Where(id => !cancelled.Contains(id)).ToList();
                        foreach (int id in ids) cancelled.Add(id);
                        verdicts.Add(new Verdict(cell, here.Count > 0 ? null : "building.noPlan",
                            ids.Select(id => (Command)new CancelPlan(id)).ToList()));
                        break;
                    }
                case Remove remove:
                    {
                        if (building.ClearAt(cell, remove.Layers) is not { } next)
                        {
                            verdicts.Add(new Verdict(cell, "building.nothingThere", []));
                            break;
                        }
                        string layer = next.Layer;
                        string? problem = building.Problem(cell, layer);
                        if (problem != null || !marked.Add((layer, next.Cell)))
                        {
                            verdicts.Add(new Verdict(cell, problem, []));
                            break;
                        }
                        verdicts.Add(new Verdict(cell, null, [new Deconstruct(cell, layer)]));
                        break;
                    }
                default:
                    // A cable is laid by the power feature, from press to release, not along a line of cells.
                    verdicts.Add(new Verdict(cell, null, []));
                    break;
            }
        }
        return verdicts;
    }
}
