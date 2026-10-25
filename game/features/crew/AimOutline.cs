using System;
using System.Collections.Generic;
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
/// beside it as they are drawn. A mark outlines the machine it takes apart, else its cell. A tool's hit, every
/// cell and plant its fan would strike, each its own square (the fan of rays).
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
        if (aim.Fan is { } fan)
        {
            foreach (int cell in fan.Hewn.Cells) OneCell(outline, grid, cell, colour);
            foreach (var plant in fan.Hewn.Plants) OneCell(outline, grid, plant.Cell, colour);
            return;
        }
        if (grid.MachineAt(aim.Cell) is { } machine)
        {
            Machine(outline, machine.Kind, machine.Cell, width, colour);
            return;
        }
        if (Doorways.IsDoorway(shown.StructureAt(grid, aim.Cell)))
        {
            Doorway(outline, grid, shown, aim.Cell, colour);
            return;
        }
        if (shown.ValveOpenAt(m.Pipes, aim.Cell) is bool open)
        {
            var valve = ShipTiles.PictureOf(open ? "valve_open" : "valve_closed");
            outline.Add(valve, CornerOf(aim.Cell, width) + new Vector2(Cell, Cell) / 2, 0, 1, colour);
            return;
        }
        OneCell(outline, grid, aim.Cell, colour);
    }

    private static Vector2 CornerOf(int cell, int width) => new(cell % width * Cell, cell / width * Cell);

    /// <summary>The box round <paramref name="cells"/>, in pixels.</summary>
    private static Rect2 BoxOf(IReadOnlyList<int> cells, int width)
    {
        int left = int.MaxValue, top = int.MaxValue, right = 0, bottom = 0;
        foreach (int c in cells)
            (left, top, right, bottom) = (Math.Min(left, c % width), Math.Min(top, c / width), Math.Max(right, c % width), Math.Max(bottom, c / width));
        return new Rect2(left * Cell, top * Cell, (right - left + 1) * Cell, (bottom - top + 1) * Cell);
    }

    /// <summary>A cell's square.</summary>
    private static void OneCell(Outline outline, IGridQueries grid, int cell, Color colour) =>
        outline.AddBox(new Rect2(CornerOf(cell, grid.Width), Cell, Cell), colour);

    /// <summary>A machine's picture from its corner, and its socket where power plugs in, as the grid draws them.</summary>
    private static void Machine(Outline outline, string kind, int cell, int width, Color colour)
    {
        var corner = CornerOf(cell, width);
        // Its picture from the cells above, where it rises over its footprint (0099).
        outline.Add(PixelArt.Picture($"machine_{kind}"), corner - new Vector2(0, ShipTiles.RiseOf(kind) * Cell), colour);
        if (Machines.TryGet(kind, out var spec) && spec.Plug is { } plug)
            outline.Add(PixelArt.Picture("power-socket"), corner + new Vector2(plug.X, plug.Y), 0, 1, colour);
    }

    /// <summary>
    /// Every cell of the doorway <paramref name="cell"/> is in, its straight run as the sim toggles it
    /// (<see cref="Doorways"/>), each with its picture (<see cref="DoorShape"/>), as the grid draws it.
    /// </summary>
    private static void Doorway(Outline outline, IGridQueries grid, ShownGrid shown, int cell, Color colour)
    {
        int width = grid.Width, size = width * grid.Height;
        int StructureAt(int c) => shown.StructureAt(grid, c);
        int LinksOf(int c) => Doorways.LinksOf(c, width, size, StructureAt);
        var seen = new HashSet<int> { cell };
        var next = new Stack<int>([cell]);
        while (next.Count > 0)
        {
            int c = next.Pop(), linked = LinksOf(c);
            // Its quarter of its 16 px piece's picture (0104).
            var (picture, quarter) = DoorShape.TileOf(DoorShape.SpotOf(c, width, LinksOf, Doorways.Lies(StructureAt(c))), closed: Doorways.IsClosed(StructureAt(c)));
            outline.Add(ShipTiles.PictureOf(picture), CornerOf(c, width), colour, new Rect2(quarter.X * Cell, quarter.Y * Cell, Cell, Cell));
            foreach (var (side, n) in (ReadOnlySpan<(int, int)>)[(Doorways.Up, c - width), (Doorways.Right, c + 1), (Doorways.Down, c + width), (Doorways.Left, c - 1)])
                if ((linked & side) != 0 && seen.Add(n)) next.Push(n);
        }
    }

    /// <summary>A plan's own pictures, as the building draws them; a mark's machine, else its cell.</summary>
    private static void Plan(Outline outline, Registry m, Sim.Building.Plan plan, Color colour)
    {
        int width = m.Grid.Width;
        if (plan.Place is not { } place)
        {
            if (plan.Clear == "machine" && m.Grid.MachineAt(plan.Cell) is { } built) Machine(outline, built.Kind, built.Cell, width, colour);
            // A ladder's or a doorway's whole piece, which it takes apart (0104).
            else if (plan.Clear == "structure" && m.Grid.PieceOf(plan.Cell) is { Count: > 1 } piece) outline.AddBox(BoxOf(piece, width), colour);
            else OneCell(outline, m.Grid, plan.Cell, colour);
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
        foreach (var piece in BlueprintArt.PiecesOf(place, Blueprint.LinksOf(place, plan.Cell, width, width * m.Grid.Height, Joins)))
        {
            var picture = ShipTiles.PictureOf(piece.Picture);
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
