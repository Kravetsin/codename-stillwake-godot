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
/// <see cref="Piece"/> names the piece of debris a press grabs, or the wreck a swing of the axe would hit
/// (0034, 0039), which is outlined itself, not its cell.
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

    /// <summary>
    /// Whether any point of a box <paramref name="halfW"/> by <paramref name="halfH"/> thousandths of a
    /// cell either side of (<paramref name="x"/>, <paramref name="y"/>) is within the hero's reach: a wreck
    /// is reached at its nearest edge, not its middle.
    /// </summary>
    public static bool ReachesBox(Hero hero, int x, int y, int halfW, int halfH)
    {
        long dx = Math.Max(0, Math.Abs((long)x - hero.X) - halfW);
        long dy = Math.Max(0, Math.Abs((long)y - hero.Middle.Y) - halfH);
        return dx * dx + dy * dy <= (long)Spec.Hero.Reach * Spec.Hero.Reach;
    }

    /// <summary>
    /// Half a wreck's picture each way, in thousandths of a cell, and a little more, as it rocks; none for
    /// a piece caught by hand.
    /// </summary>
    private static (int HalfW, int HalfH)? HalfOf(string item) =>
        DebrisLooks.Wrecks.TryGetValue(item, out var size)
            ? (size.W * Hero.Unit / (2 * PixelsPerCell) + WreckMargin, size.H * Hero.Unit / (2 * PixelsPerCell) + WreckMargin)
            : null;

    private const int PixelsPerCell = 16, WreckMargin = 250;

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
    /// a wreck whose picture is under the pointer, else the piece of debris nearest it within
    /// <c>Debris.AimRadius</c>, only while the hero is out in space, where it can catch one, and none in
    /// <paramref name="passOver"/>: grabbed already, its grab still in flight. Bare-handed, a wreck is
    /// grabbed, which the sim refuses: too big. With the <paramref name="axe"/>, a small piece is still
    /// grabbed, but no wreck by the pointer is aimed at: the first a swing toward the pointer begun now
    /// would touch is (<see cref="Struck"/>), wherever the pointer is.
    /// </summary>
    public static Aim? AimAt(
        Registry m, int peer, Hero hero, (double X, double Y) point,
        IReadOnlySet<int>? passOver = null, IReadOnlySet<int>? served = null, bool axe = false)
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
        var piece = PieceNear(m, hero, point, passOver);
        // Aimed at where the piece is, which may be a cell beside the pointer's.
        if (piece != null && HalfOf(piece.Item) == null)
        {
            int at = DebrisModule.CellOf(piece.X, piece.Y, grid.Width);
            return new Aim(at, Reaches(hero, at, grid.Width), new Use(new Grab(piece.Id), m.Debris.GrabProblem(peer, piece.Id)), Piece: piece.Id);
        }
        if (axe)
        {
            // A swing begun now faces the pointer (Poses.FaceOf), and so does the stroke it is judged by.
            var toward = hero with { Facing = Poses.FaceOf(point, hero) ?? hero.Facing };
            if (Struck(m, toward, PointOf(point)).FirstOrDefault() is not { } wreck) return new Aim(cell, inReach);
            return new Aim(DebrisModule.CellOf(wreck.X, wreck.Y, grid.Width), true, new Use(new Hit(wreck.Id), m.Debris.HitProblem(peer, wreck.Id)), Piece: wreck.Id);
        }
        if (piece is null || HalfOf(piece.Item) is not var (halfW, halfH)) return new Aim(cell, inReach);
        var grab = new Use(new Grab(piece.Id), m.Debris.GrabProblem(peer, piece.Id));
        return new Aim(DebrisModule.CellOf(piece.X, piece.Y, grid.Width), ReachesBox(hero, piece.X, piece.Y, halfW, halfH), grab, Piece: piece.Id);
    }

    /// <summary>A point in cells, in thousandths of a cell.</summary>
    public static Point PointOf((double X, double Y) cells) =>
        new((int)Math.Round(cells.X * Hero.Unit), (int)Math.Round(cells.Y * Hero.Unit));

    /// <summary>How far the shoulder is above the feet, in thousandths of a cell, the body upright.</summary>
    private const int ShoulderUp = (int)(Rig.Shoulder / Rig.Cell * Hero.Unit);

    /// <summary>The downstroke sampled every this many radians (5°), at three distances from the shoulder.</summary>
    private const double SampleStep = Math.PI / 36;

    /// <summary>
    /// The wrecks a swing of the axe by <paramref name="hero"/> toward <paramref name="at"/>, in thousandths
    /// of a cell, touches as it strikes (0039), in spawn order; none while the hero is aboard. The blade
    /// sweeps the downstroke's last stretch, from the line to the point to <see cref="AxeSwing.Through"/>
    /// past it, <see cref="AxeSwing.Edge"/> wider either side, out to <c>Axe.Reach</c> from the shoulder of
    /// the body upright, turned the way the hero faces; never the raised stretch over the head. A wreck is
    /// touched where it is aimed at: its picture and a margin, square to the world.
    /// </summary>
    public static List<Piece> Struck(Registry m, Hero hero, Point at)
    {
        var struck = new List<Piece>();
        if (m.Rooms.RoomAt(hero.BodyCell(m.Grid.Width)) != RoomsModule.Space) return struck;
        double sx = hero.X, sy = hero.Y - ShoulderUp;
        double line = Math.Atan2(at.Y - sy, (at.X - sx) * hero.Facing);
        double from = line - AxeSwing.Edge, span = AxeSwing.Through + 2 * AxeSwing.Edge;
        int steps = (int)Math.Ceiling(span / SampleStep);
        foreach (var p in m.Debris.Pieces)
        {
            if (HalfOf(p.Item) is not var (halfW, halfH)) continue;
            bool touched = false;
            for (int i = 0; i <= steps && !touched; i++)
            {
                double angle = from + span * i / steps, cos = Math.Cos(angle), sin = Math.Sin(angle);
                for (int r = 1; r <= 3 && !touched; r++)
                {
                    double far = Spec.Axe.Reach * r / 3.0;
                    touched = Math.Abs(sx + hero.Facing * cos * far - p.X) <= halfW && Math.Abs(sy + sin * far - p.Y) <= halfH;
                }
            }
            if (touched) struck.Add(p);
        }
        return struck;
    }

    /// <summary>
    /// The hits peer <paramref name="peer"/>'s swing toward <paramref name="at"/> sends as it strikes:
    /// one for each wreck it touches (<see cref="Struck"/>) the sim would let it hit.
    /// </summary>
    public static List<Hit> HitsOf(Registry m, int peer, Hero hero, Point at) =>
        Struck(m, hero, at).Where(p => m.Debris.HitProblem(peer, p.Id) == null).Select(p => new Hit(p.Id)).ToList();

    /// <summary>
    /// The first wreck, in spawn order, with <paramref name="point"/> on its picture, else the small piece
    /// of debris nearest it within <c>Debris.AimRadius</c>; none while the hero is aboard.
    /// </summary>
    private static Piece? PieceNear(Registry m, Hero hero, (double X, double Y) point, IReadOnlySet<int>? passOver)
    {
        if (m.Rooms.RoomAt(hero.BodyCell(m.Grid.Width)) != RoomsModule.Space) return null;
        double px = point.X * DebrisModule.Unit, py = point.Y * DebrisModule.Unit;
        foreach (var p in m.Debris.Pieces)
            if (HalfOf(p.Item) is var (halfW, halfH) && Math.Abs(p.X - px) <= halfW && Math.Abs(p.Y - py) <= halfH) return p;
        Piece? nearest = null;
        double best = (double)Spec.Debris.AimRadius * Spec.Debris.AimRadius;
        foreach (var p in m.Debris.Pieces)
        {
            if (passOver?.Contains(p.Id) == true || HalfOf(p.Item) != null) continue;
            double dx = p.X - px, dy = p.Y - py;
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
        // A wreck is chopped by a swing of the axe (AxeSwing), never by a press.
        if (pressed && aim.Press is { Problem: null } press && press.Command is not Hit) commands.Add(press.Command);
        if (aim.Work is not { Problem: null } work || !held) return new(commands, null, null, -1);
        if (work.Delivers) return new([.. commands, new Work(work.Plan)], null, work.Plan, -1);
        // A hold just begun sends one; so does a load that took the world's tick back.
        bool fresh = workedOnTick < 0 || tick < workedOnTick;
        int due = fresh ? 1 : Math.Min(tick - workedOnTick, MaxWorkPerFrame);
        for (int i = 0; i < due; i++) commands.Add(new Work(work.Plan));
        return new(commands, null, null, due > 0 ? tick : workedOnTick);
    }
}
