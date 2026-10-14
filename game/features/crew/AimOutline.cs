using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Machines = Stillwake.Content.Machines;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What E would work, outlined along the pictures it is drawn with: a machine's whole picture with its
/// socket, a whole doorway (it opens as one), a valve's wheel, a plan's own pictures joined to the plans
/// beside it as they are drawn. A mark outlines the machine it takes apart, else its cell.
/// </summary>
public static class AimOutline
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>Lays what <paramref name="aim"/> works into <paramref name="outline"/> in <paramref name="colour"/>, doors and valves as <paramref name="shown"/>.</summary>
    public static void Trace(Outline outline, Registry m, Aim aim, ShownGrid shown, Color colour)
    {
        var grid = m.Grid;
        int width = grid.Width;
        if (aim.Work is { } work)
        {
            foreach (var plan in m.Building.Plans)
                if (plan.Id == work.Plan) Plan(outline, m, plan, colour);
            return;
        }
        if (grid.MachineAt(aim.Cell) is { } machine)
        {
            Machine(outline, machine.Kind, machine.Cell, width, colour);
            return;
        }
        if (shown.StructureAt(grid, aim.Cell) is Structure.DoorClosed or Structure.DoorOpen)
        {
            Doorway(outline, grid, shown, aim.Cell, colour);
            return;
        }
        int conduit = shown.ConduitAt(grid, aim.Cell);
        if (conduit is Conduit.ValveOpen or Conduit.ValveClosed)
        {
            var valve = PixelArt.Picture(conduit == Conduit.ValveOpen ? "valve_open" : "valve_closed");
            outline.Add(valve, CornerOf(aim.Cell, width) + new Vector2(Cell, Cell) / 2, 0, 1, colour);
            return;
        }
        outline.AddBox(new Rect2(CornerOf(aim.Cell, width), Cell, Cell), colour);
    }

    private static Vector2 CornerOf(int cell, int width) => new(cell % width * Cell, cell / width * Cell);

    /// <summary>A machine's picture from its corner, and its socket where power plugs in, as the grid draws them.</summary>
    private static void Machine(Outline outline, string kind, int cell, int width, Color colour)
    {
        var corner = CornerOf(cell, width);
        outline.Add(PixelArt.Picture($"machine_{kind}"), corner, colour);
        if (Machines.TryGet(kind, out var spec) && spec.Plug is { } plug)
            outline.Add(PixelArt.Picture("power-socket"), corner + new Vector2(plug.X, plug.Y), 0, 1, colour);
    }

    /// <summary>Every cell of the doorway <paramref name="cell"/> is in, each its door's picture joined above and below, as the grid draws it.</summary>
    private static void Doorway(Outline outline, IGridQueries grid, ShownGrid shown, int cell, Color colour)
    {
        int width = grid.Width, size = width * grid.Height;
        bool IsDoor(int c) => c >= 0 && c < size && shown.StructureAt(grid, c) is Structure.DoorClosed or Structure.DoorOpen;
        int top = cell;
        while (IsDoor(top - width)) top -= width;
        for (int c = top; IsDoor(c); c += width)
        {
            int linked = (IsDoor(c - width) ? Blueprint.Up : 0) | (IsDoor(c + width) ? Blueprint.Down : 0);
            string look = shown.StructureAt(grid, c) == Structure.DoorClosed ? "closed" : "open";
            outline.Add(PixelArt.Picture($"door_{look}_{linked}"), CornerOf(c, width), colour);
        }
    }

    /// <summary>A plan's own pictures, as the building draws them; a mark's machine, else its cell.</summary>
    private static void Plan(Outline outline, Registry m, Sim.Building.Plan plan, Color colour)
    {
        int width = m.Grid.Width;
        if (plan.Place is not { } place)
        {
            if (plan.Clear == "machine" && m.Grid.MachineAt(plan.Cell) is { } built) Machine(outline, built.Kind, built.Cell, width, colour);
            else outline.AddBox(new Rect2(CornerOf(plan.Cell, width), Cell, Cell), colour);
            return;
        }
        if (place.Layer == "machine")
        {
            Machine(outline, place.Kind, plan.Cell, width, colour);
            return;
        }
        var plans = m.Building.Plans;
        bool Joins(int c)
        {
            foreach (var other in plans)
                if (other.Cell == c && other.Place is { } o && Blueprint.Joins(place, o)) return true;
            return false;
        }
        var corner = CornerOf(plan.Cell, width);
        foreach (var piece in BlueprintArt.PiecesOf(place, Blueprint.LinksOf(plan.Cell, width, Joins)))
        {
            var picture = PixelArt.Picture(piece.Picture);
            if (piece.Cut)
            {
                var size = picture.GetSize();
                var from = new Vector2(corner.X % size.X, corner.Y % size.Y);
                outline.Add(picture, corner, colour, new Rect2(from, Cell, Cell));
            }
            // A valve's wheel sits in the middle of its cell, as the grid lays it.
            else if (piece.Layer == TileLayer.Valves) outline.Add(picture, corner + new Vector2(Cell, Cell) / 2, 0, 1, colour);
            else outline.Add(picture, corner, colour);
        }
    }
}
