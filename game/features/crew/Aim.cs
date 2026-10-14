using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>Something E does: the command it sends, and why the sim would refuse it, if it would.</summary>
public sealed record Use(Command Command, string? Problem);

/// <summary>A plan the left button works on, why the sim would refuse it, and whether it still lacks something (delivers rather than builds).</summary>
public sealed record WorkAim(int Plan, string? Problem, bool Delivers = false);

/// <summary>
/// The cell under the pointer and what the hands do there: the left button held works a plan; E toggles
/// a door or valve, puts a canister into a tank, grabs a piece of debris or opens crate <see cref="Crate"/>.
/// <see cref="Piece"/> names the piece of debris a press grabs, which is outlined itself, not its cell.
/// </summary>
public sealed record Aim(int Cell, bool InReach, Use? Press = null, WorkAim? Work = null, int? Crate = null, int? Piece = null);

/// <summary>
/// What the hands send this frame, the crate they open if any, the blueprint they delivered to if any,
/// and the tick work was last sent on: -1 when none is under way.
/// </summary>
public sealed record Uses(IReadOnlyList<Command> Commands, int? Open, int? Served, int WorkedOnTick);

/// <summary>What the hands reach and work (apps/game/src/features/crew/aim.ts).</summary>
public static class Aiming
{
    /// <summary>How many ticks of work one frame sends at most, after a stall.</summary>
    private const int MaxWorkPerFrame = 5;

    /// <summary>Whether the middle of <paramref name="cell"/> is within the hero's reach of the middle of its body.</summary>
    public static bool Reaches(Hero hero, int cell, int width)
    {
        long dx = cell % width * Hero.Unit + Hero.Unit / 2 - hero.X;
        long dy = cell / width * Hero.Unit + Hero.Unit / 2 - hero.Middle.Y;
        return dx * dx + dy * dy <= (long)Spec.Hero.Reach * Spec.Hero.Reach;
    }

    /// <summary>Whether a machine of <paramref name="kind"/> holds items, opened with E: a crate, a furnace, an analyzer.</summary>
    private static bool HoldsItems(string kind) => Spec.Machines.TryGet(kind, out var spec) && spec.Slots != null;

    /// <summary>Whether crate, furnace or analyzer <paramref name="id"/> still stands with a cell of it within the hero's reach.</summary>
    public static bool CrateInReach(IGridQueries grid, Hero hero, int id)
    {
        var crate = grid.Machines.FirstOrDefault(m => m.Id == id && HoldsItems(m.Kind));
        var cells = crate == null ? [] : grid.Footprint(crate.Kind, crate.Cell) ?? [];
        return cells.Any(cell => Reaches(hero, cell, grid.Width));
    }

    /// <summary>
    /// What the hero of peer <paramref name="peer"/> aims at with the pointer at <paramref name="point"/>,
    /// in cells; nothing off the grid. A plan in the cell is worked: of several, the oldest that can be
    /// worked, so one the hero cannot pay for does not block the mark beside it; when none can, the
    /// oldest, to show why; none in <paramref name="served"/>, served in this hold already. E works what
    /// is under it all the same: a crate, a furnace or an analyzer, a tank, a door, a valve. With neither,
    /// the piece of debris nearest the pointer within <c>Debris.AimRadius</c>, only while the hero is out
    /// in space, where it can catch one, and none in <paramref name="passOver"/>: grabbed already, its
    /// grab still in flight.
    /// </summary>
    public static Aim? AimAt(
        Registry m, int peer, Hero hero, (double X, double Y) point,
        IReadOnlySet<int>? passOver = null, IReadOnlySet<int>? served = null)
    {
        var grid = m.Grid;
        int x = (int)Math.Floor(point.X), y = (int)Math.Floor(point.Y);
        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return null;
        int cell = y * grid.Width + x;
        bool inReach = Reaches(hero, cell, grid.Width);

        var plans = m.Building;
        bool Delivers(int id) => plans.Lacking(id).Count > 0;
        var here = plans.PlansAt(cell)
            // A sweep delivers to each blueprint once; one with all it needs is built while held on.
            .Where(p => !(served?.Contains(p.Id) == true && Delivers(p.Id)))
            .Select(p => new WorkAim(p.Id, plans.WorkProblem(p.Id, peer), Delivers(p.Id)))
            .ToList();
        var work = here.FirstOrDefault(w => w.Problem == null) ?? here.FirstOrDefault();
        var machine = grid.MachineAt(cell);
        int? crate = machine != null && HoldsItems(machine.Kind) ? machine.Id : null;
        var press = crate == null ? PressOf(m, peer, cell) : null;
        if (work != null || crate != null || press != null) return new Aim(cell, inReach, press, work, crate);
        if (PieceNear(m, hero, point, passOver) is not { } piece) return new Aim(cell, inReach);
        // Aimed at where the piece is, which may be a cell beside the pointer's.
        int at = DebrisModule.CellOf(piece.X, piece.Y, grid.Width);
        return new Aim(at, Reaches(hero, at, grid.Width), new Use(new Grab(piece.Id), m.Debris.GrabProblem(peer, piece.Id)), Piece: piece.Id);
    }

    /// <summary>The piece of debris nearest <paramref name="point"/> within <c>Debris.AimRadius</c>; none while the hero is aboard.</summary>
    private static Piece? PieceNear(Registry m, Hero hero, (double X, double Y) point, IReadOnlySet<int>? passOver)
    {
        if (m.Rooms.RoomAt(hero.BodyCell(m.Grid.Width)) != RoomsModule.Space) return null;
        Piece? nearest = null;
        double best = (double)Spec.Debris.AimRadius * Spec.Debris.AimRadius;
        foreach (var p in m.Debris.Pieces)
        {
            if (passOver?.Contains(p.Id) == true) continue;
            double dx = p.X - point.X * DebrisModule.Unit, dy = p.Y - point.Y * DebrisModule.Unit;
            if (dx * dx + dy * dy > best) continue;
            best = dx * dx + dy * dy;
            nearest = p;
        }
        return nearest;
    }

    private static Use? PressOf(Registry m, int peer, int cell)
    {
        var grid = m.Grid;
        if (grid.MachineAt(cell) is { Kind: "tank" } tank) return new Use(new LoadCanister(tank.Id), m.Atmosphere.CanisterProblem(tank.Id, peer));
        if (grid.StructureAt(cell) is Structure.DoorClosed or Structure.DoorOpen) return new Use(new ToggleDoor(cell), null);
        if (grid.ConduitAt(cell) is Conduit.ValveClosed or Conduit.ValveOpen) return new Use(new ToggleValve(cell), null);
        return null;
    }

    /// <summary>
    /// What the hands do this frame: <paramref name="pressed"/> is E, or the left button on a piece of
    /// debris; <paramref name="held"/> the left button. A press within reach sends what is pressed, unless
    /// the sim would refuse it. Held on a blueprint still lacking something, it delivers once: the caller
    /// keeps the blueprints served in this hold and passes them to <see cref="AimAt"/>, so sweeping the
    /// pointer delivers to each it passes. Held on a plan with all it needs, or a mark, it sends one
    /// <see cref="Work"/> for every tick released since the last it sent for, so a peer drawing fewer
    /// frames than the ticks it runs builds as fast as any other; a hold just begun sends one.
    /// <paramref name="tick"/> is the client's world tick.
    /// </summary>
    public static Uses UsesOf(Aim? aim, bool pressed, bool held, int tick, int workedOnTick)
    {
        if (aim is not { InReach: true }) return new([], null, null, -1);
        if (pressed && aim.Crate is int crate) return new([], crate, null, -1);
        var commands = new List<Command>();
        if (pressed && aim.Press is { Problem: null } press) commands.Add(press.Command);
        if (aim.Work is not { Problem: null } work || !held) return new(commands, null, null, -1);
        if (work.Delivers) return new([.. commands, new Work(work.Plan)], null, work.Plan, -1);
        // A hold just begun sends one; so does a load that took the world's tick back.
        bool fresh = workedOnTick < 0 || tick < workedOnTick;
        int due = fresh ? 1 : Math.Min(tick - workedOnTick, MaxWorkPerFrame);
        for (int i = 0; i < due; i++) commands.Add(new Work(work.Plan));
        return new(commands, null, null, due > 0 ? tick : workedOnTick);
    }
}
