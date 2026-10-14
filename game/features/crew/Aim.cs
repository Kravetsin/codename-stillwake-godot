using System;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Features.Crew;

/// <summary>Something E does: the command it sends, and why the sim would refuse it, if it would.</summary>
public sealed record Use(Command Command, string? Problem);

/// <summary>The cell under the pointer, whether the hero reaches it, and what E does there.</summary>
public sealed record Aim(int Cell, bool InReach, Use? Press = null);

/// <summary>
/// What the hands reach and work (apps/game/src/features/crew/aim.ts), as far as the ported modules go:
/// doors and valves. Tanks, crates, plans and debris come with their modules.
/// </summary>
public static class Aiming
{
    /// <summary>Whether the middle of <paramref name="cell"/> is within the hero's reach of the middle of its body.</summary>
    public static bool Reaches(Hero hero, int cell, int width)
    {
        long dx = cell % width * Hero.Unit + Hero.Unit / 2 - hero.X;
        long dy = cell / width * Hero.Unit + Hero.Unit / 2 - hero.Middle.Y;
        return dx * dx + dy * dy <= (long)Spec.Reach * Spec.Reach;
    }

    /// <summary>
    /// What the hero aims at with the pointer at <paramref name="point"/>, in cells; nothing off the grid.
    /// E toggles a door, or else a valve, under it.
    /// </summary>
    public static Aim? AimAt(IGridQueries grid, Hero hero, (double X, double Y) point)
    {
        int x = (int)Math.Floor(point.X), y = (int)Math.Floor(point.Y);
        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return null;
        int cell = y * grid.Width + x;
        return new Aim(cell, Reaches(hero, cell, grid.Width), PressOf(grid, cell));
    }

    private static Use? PressOf(IGridQueries grid, int cell)
    {
        if (grid.StructureAt(cell) is Structure.DoorClosed or Structure.DoorOpen) return new Use(new ToggleDoor(cell), null);
        if (grid.ConduitAt(cell) is Conduit.ValveClosed or Conduit.ValveOpen) return new Use(new ToggleValve(cell), null);
        return null;
    }

    /// <summary>What a press of E sends: the command under the pointer within reach, unless the sim would refuse it.</summary>
    public static Command? UseOf(Aim? aim, bool pressed) =>
        pressed && aim is { InReach: true, Press: { Problem: null } press } ? press.Command : null;
}
