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
    /// What <paramref name="act"/> lays at each point of a drag, columns by rows: a ladder and a door segment their piece (0104);
    /// everything else one cell, a click one cell, as building has no brush (the dig's stays, <see cref="Brush"/>).
    /// </summary>
    public static (int W, int H) StampOf(Act act) =>
        act is PlaceTool p && Spec.Builds.PieceOf(p.Placement.Kind) is { } piece ? piece : (1, 1);

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
        if (act is PlaceTool { Placement.Layer: "machine" }) return [from.Y * width + from.X];
        // Ladder pieces, door segments or hatches laid end to end along the drag, a piece's length a step (0104, 0111):
        // a piece runs along its own longer axis, a square one along the drag's longer axis. Their top-left cells only.
        if (act is PlaceTool { Placement.Kind: "ladder" or "door" or Spec.Builds.Hatch })
        {
            bool along = w > h || (w == h && Math.Abs(to.X - from.X) >= Math.Abs(to.Y - from.Y));
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
    /// The cells a drag covers as <see cref="LineOf"/> lays them, but for a removal the rectangle between its corners, as
    /// planning walls fills one: every cell in it marked, or its plans cancelled (<see cref="Judge"/>), a piece once.
    /// </summary>
    public static List<int> CellsOf(int width, Act act, Spot from, Spot to, (int W, int H)? stamp = null) =>
        act is Remove ? RectangleOf(width, from, to) : LineOf(width, act, from, to, stamp);

    /// <summary>
    /// The cells a stroke of <paramref name="act"/> from world point <paramref name="from"/> to <paramref name="to"/>, in cells,
    /// covers: its stamp (<see cref="StampOf"/>) round each end, laid as <see cref="CellsOf"/> lays a drag.
    /// </summary>
    public static List<int> StrokeOf(int width, int height, Act act, (double X, double Y) from, (double X, double Y) to)
    {
        var stamp = StampOf(act);
        return CellsOf(width, act, StampAt(width, height, from, stamp), StampAt(width, height, to, stamp), stamp);
    }

    /// <summary>Whether a thing of <paramref name="layer"/> is <paramref name="remove"/>'s to take (0110).</summary>
    private static bool Takes(Remove remove, string layer) => remove.Layers.Contains(layer);

    /// <summary>The plans at <paramref name="cell"/> in <paramref name="remove"/>'s layer, a blueprint by what it places, a mark by what it takes (0107).</summary>
    private static List<Plan> PlansIn(BuildingModule building, int cell, Remove remove) =>
        building.PlansAt(cell).Where(p => p.Place is { } place
            ? Takes(remove, place.Layer)
            : Takes(remove, p.Clear!)).ToList();

    /// <summary>What at <paramref name="cell"/> a mark of <paramref name="remove"/> would take, as <see cref="BuildingModule.ClearAt"/> finds it: its layer's things only, a layer at a time.</summary>
    public static List<(string Layer, int Cell)> ThingsIn(Remove remove, int cell, BuildingModule building)
    {
        var things = new List<(string, int)>();
        foreach (string layer in remove.Layers)
            if (building.ClearAt(cell, [layer]) is { } next && Takes(remove, layer)) things.Add(next);
        return things;
    }

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
    /// Removing cancels plans in its layer if the drag begins on one, a mark included, and marks what is built in its
    /// layer otherwise (0110): the focused layer only, whatever else is under where it began.
    /// </summary>
    public static List<Verdict> Judge(Act act, IReadOnlyList<int> cells, BuildingModule building)
    {
        // A machine spans several cells of a line, yet takes one mark and is cancelled once.
        var cancelled = new HashSet<int>();
        var marked = new HashSet<(string, int)>();
        bool cancels = act is Remove of && cells.Count > 0 && PlansIn(building, cells[0], of).Count > 0;
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
                        var here = PlansIn(building, cell, remove);
                        var ids = here.Select(p => p.Id).Where(id => !cancelled.Contains(id)).ToList();
                        foreach (int id in ids) cancelled.Add(id);
                        verdicts.Add(new Verdict(cell, here.Count > 0 ? null : "building.noPlan",
                            ids.Select(id => (Command)new CancelPlan(id)).ToList()));
                        break;
                    }
                case Remove remove:
                    {
                        // Everything of the focused layer in the cell, each piece once over the whole drag.
                        var things = ThingsIn(remove, cell, building);
                        if (things.Count == 0)
                        {
                            verdicts.Add(new Verdict(cell, "building.nothingThere", []));
                            break;
                        }
                        string? problem = null;
                        var marks = new List<Command>();
                        foreach (var (layer, at) in things)
                        {
                            if (building.Problem(cell, layer) is { } why) problem ??= why;
                            else if (marked.Add((layer, at))) marks.Add(new Deconstruct(cell, layer));
                        }
                        verdicts.Add(new Verdict(cell, marks.Count > 0 ? null : problem, marks));
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
