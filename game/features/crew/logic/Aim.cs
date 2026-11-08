using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;
using Stillwake.Sim.Rooms;
using Spec = Stillwake.Content;
using Structures = Stillwake.Content.Structures;

namespace Stillwake.Game.Features.Crew;

/// <summary>Something E does: the command it sends, and why the sim would refuse it, if it would.</summary>
public sealed record Use(Command Command, string? Problem);

/// <summary>
/// A plan the left button works on, and why the sim would refuse it; or, with <c>Repair</c>, the broken machine it repairs
/// (magnetic-flare), <c>Plan</c> then unused.
/// </summary>
public sealed record WorkAim(int Plan, string? Problem, int? Repair = null);

/// <summary>
/// The cell under the pointer and what the hands do there: the left button held works a plan; E toggles
/// a door or valve, puts a canister into a tank, grabs a piece of debris or opens <see cref="Crate"/>, a machine holding items or a computer.
/// <see cref="Piece"/> names the piece of debris a press grabs, a wreck outlined itself, not its cell. What a tool or a weapon
/// hits is never aimed at: it hits what it is drawn touching (tool-edge, melee-edge).
/// </summary>
public sealed record Aim(int Cell, bool InReach, Use? Press = null, WorkAim? Work = null, int? Crate = null, int? Piece = null);

/// <summary>
/// What the hands send this frame, the crate they open if any, and the tick work was last sent on: -1 when
/// none is under way.
/// </summary>
public sealed record Uses(IReadOnlyList<Command> Commands, int? Open, int WorkedOnTick);

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

    private static (int HalfW, int HalfH)? HalfOf(string item) => WreckAim.HalfOf(item);

    private const int WreckMargin = WreckAim.Margin;

    /// <summary>Half a small piece's picture each way, in thousandths of a cell: they are a cell or a little more across.</summary>
    private const int SmallHalf = 1000;

    /// <summary>How far inside a picture's edge the harpoon's rod sticks, in thousandths of a cell: its point sinks in.</summary>
    private const int Bite = 300;

    /// <summary>
    /// The box a piece is struck in, half its size each way in thousandths of a cell, and half its picture:
    /// a wreck's as it is aimed at (<see cref="HalfOf"/>), a small piece's picture; both with the margin.
    /// </summary>
    private static (int HalfW, int HalfH, int PictureW, int PictureH) BoxOf(string item) => HalfOf(item) is var (w, h)
        ? (w, h, w - WreckMargin, h - WreckMargin)
        : (SmallHalf + WreckMargin, SmallHalf + WreckMargin, SmallHalf, SmallHalf);

    /// <summary>Half the picture of a piece of <paramref name="item"/> each way, in thousandths of a cell.</summary>
    public static (int HalfW, int HalfH) PictureOf(string item)
    {
        var (_, _, w, h) = BoxOf(item);
        return (w, h);
    }

    /// <summary>
    /// The piece the harpoon's rod meets first on its way from <paramref name="from"/> to <paramref name="to"/>,
    /// in thousandths of a cell, that peer <paramref name="peer"/> could reel (0044), each piece where it is drawn
    /// <paramref name="progress"/> of the way to its next tick; where the rod sticks, where it enters the
    /// piece's box kept <see cref="Bite"/> inside its picture; and how far along the way, from 0 to 1. None while
    /// it meets nothing; of two met, the nearer, the first in spawn order at a tie.
    /// </summary>
    public static (int Piece, (double X, double Y) At, double T)? Hooks(
        Registry m, int peer, (double X, double Y) from, (double X, double Y) to, double progress)
    {
        (int, (double, double), double)? first = null;
        foreach (var p in m.Debris.Pieces)
        {
            var (x, y) = Shared.PieceDrawn.At(p, progress);
            var (halfW, halfH, pictureW, pictureH) = BoxOf(p.Item);
            if (Entered((from.X, from.Y, to.X, to.Y), x - halfW, y - halfH, x + halfW, y + halfH) is not double t) continue;
            if (first is var (_, _, nearest) && nearest <= t) continue;
            if (m.Debris.ReelProblem(peer, p.Id) != null) continue;
            double ax = from.X + (to.X - from.X) * t, ay = from.Y + (to.Y - from.Y) * t;
            int inW = pictureW - Bite, inH = pictureH - Bite;
            first = (p.Id, (Math.Clamp(ax, x - inW, x + inW), Math.Clamp(ay, y - inH, y + inH)), t);
        }
        return first;
    }

    /// <summary>
    /// Whether <paramref name="cell"/> holds the harpoon's rod (the grapple, 0044): a wall, a floor, a closed
    /// door or a closed hatch, as <paramref name="ship"/> shows them. Not a back wall, behind the hero's plane; not an
    /// open door, an open hatch nor a ladder, which are passed through; nothing off the grid.
    /// </summary>
    public static bool Grips(Ship ship, int x, int y) =>
        x >= 0 && y >= 0 && x < ship.Width && y < ship.Height
        && Structures.Solid(ship.StructureAt(y * ship.Width + x));

    /// <summary>Whether the cell under <paramref name="at"/>, in thousandths of a cell, holds the rod (<see cref="Grips(Ship, int, int)"/>).</summary>
    public static bool Grips(Ship ship, (double X, double Y) at) =>
        Grips(ship, (int)Math.Floor(at.X / Hero.Unit), (int)Math.Floor(at.Y / Hero.Unit));

    /// <summary>
    /// The first cell that holds the rod (<see cref="Grips(Ship, int, int)"/>) on its way from
    /// <paramref name="from"/> to <paramref name="to"/>, in thousandths of a cell, walked cell by cell; where the
    /// rod sticks, <see cref="Bite"/> into it past where it enters, and how far along the way, from 0 to 1.
    /// </summary>
    public static ((double X, double Y) At, double T)? Grapples(Ship ship, (double X, double Y) from, (double X, double Y) to)
    {
        double dx = to.X - from.X, dy = to.Y - from.Y, length = Math.Sqrt(dx * dx + dy * dy);
        int x = (int)Math.Floor(from.X / Hero.Unit), y = (int)Math.Floor(from.Y / Hero.Unit);
        int endX = (int)Math.Floor(to.X / Hero.Unit), endY = (int)Math.Floor(to.Y / Hero.Unit);
        int stepX = Math.Sign(dx), stepY = Math.Sign(dy);
        // How far along the way the next column and row begin, and how far one cell across takes.
        double nextX = dx == 0 ? double.PositiveInfinity : ((stepX > 0 ? x + 1 : x) * (double)Hero.Unit - from.X) / dx;
        double nextY = dy == 0 ? double.PositiveInfinity : ((stepY > 0 ? y + 1 : y) * (double)Hero.Unit - from.Y) / dy;
        double acrossX = dx == 0 ? double.PositiveInfinity : Hero.Unit / Math.Abs(dx);
        double acrossY = dy == 0 ? double.PositiveInfinity : Hero.Unit / Math.Abs(dy);
        double t = 0;
        while (t <= 1)
        {
            if (Grips(ship, x, y))
            {
                double sink = length == 0 ? 0 : Bite / length;
                return ((from.X + dx * (t + sink), from.Y + dy * (t + sink)), t);
            }
            if (x == endX && y == endY) return null;
            if (nextX < nextY) (t, nextX, x) = (nextX, nextX + acrossX, x + stepX);
            else (t, nextY, y) = (nextY, nextY + acrossY, y + stepY);
        }
        return null;
    }

    /// <summary>
    /// What the harpoon's rod meets first on its way (0044): a piece peer <paramref name="peer"/> could reel
    /// (<see cref="Hooks"/>) or the ship's structure (<see cref="Grapples"/>), whichever is nearer along it.
    /// </summary>
    public static RodHit? RodMeets(Registry m, Ship ship, int peer, (double X, double Y) from, (double X, double Y) to, double progress)
    {
        var piece = Hooks(m, peer, from, to, progress);
        var wall = Grapples(ship, from, to);
        if (wall is var (at, t) && (piece is not var (_, _, near) || t < near)) return new RodHit(null, at);
        return piece is var (id, stuck, _) ? new RodHit(id, stuck) : null;
    }

    /// <summary>Whether a machine of <paramref name="kind"/> opens on E: one holding items (a crate, a furnace) or the ship's computer.</summary>
    public static bool Opens(string kind) =>
        Spec.Machines.TryGet(kind, out var spec) && (spec.Slots != null || spec.Is(Spec.MachineTrait.Computer) || spec.Is(Spec.MachineTrait.Switches));

    /// <summary>Whether a crate, a furnace or a computer <paramref name="id"/> still stands with a cell of it within the hero's reach.</summary>
    public static bool CrateInReach(IGridQueries grid, Hero hero, int id)
    {
        var crate = grid.MachineById(id) is { } m && Opens(m.Kind) ? m : null;
        var cells = crate == null ? [] : grid.Footprint(crate.Kind, crate.Cell) ?? [];
        return cells.Any(cell => Reaches(hero, cell, grid.Width));
    }

    /// <summary>
    /// What the hero of peer <paramref name="peer"/> aims at with the pointer at <paramref name="point"/>,
    /// in cells; nothing off the grid. A plan in the cell is worked: of several, the oldest that can be
    /// worked, so one the hero cannot pay for does not block the mark beside it; when none can, the
    /// oldest, to show why. E works what
    /// is under it all the same: a crate, a furnace or a tech station, a tank, a door, a valve. With neither,
    /// a wreck whose picture is under the pointer, else the piece of debris nearest it within
    /// <c>Debris.AimRadius</c>, only while the hero is out in space, where it can catch one, and none in
    /// <paramref name="passOver"/>: grabbed already, its grab still in flight. Bare-handed, a wreck is
    /// grabbed, which the sim refuses: too big. <paramref name="armed"/>, with a tool or a weapon in hand, a small piece is still
    /// grabbed and nothing else is aimed at: what it hits is what it is drawn touching (tool-edge, melee-edge). A door or valve is
    /// opened or shut as <paramref name="shown"/> shows it (the world's without it): with this peer's own toggles in
    /// flight, the next press undoes them, and a press says what its sender saw should become of it. Where a machine's
    /// picture, built or planned, rises over the cells above its footprint by <paramref name="riseOf"/> (<see cref="Risen"/>), and
    /// nothing there is worked, it is that machine or plan.
    /// </summary>
    public static Aim? AimAt(
        Registry m, int peer, Hero hero, (double X, double Y) point,
        IReadOnlySet<int>? passOver = null, bool armed = false, ShownGrid? shown = null, Func<string, int>? riseOf = null)
    {
        var grid = m.Grid;
        int x = (int)Math.Floor(point.X), y = (int)Math.Floor(point.Y);
        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return null;
        int cell = y * grid.Width + x;
        bool inReach = Reaches(hero, cell, grid.Width);

        var plans = m.Building;
        var here = BackFirst(plans.PlansAt(cell))
            .Select(p => new WorkAim(p.Id, plans.WorkProblem(p.Id, peer)))
            .ToList();
        var work = here.FirstOrDefault(w => w.Problem == null) ?? here.FirstOrDefault();
        var machine = grid.MachineAt(cell);
        // A broken machine with no plan on it is repaired; a mark on it wins, so it is taken apart (magnetic-flare).
        if (work == null && machine != null && m.Power.IsBroken(machine.Id))
            work = new WorkAim(0, m.Building.RepairProblem(machine.Id), machine.Id);
        int? crate = machine != null && Opens(machine.Kind) ? machine.Id : null;
        // A drop pod is boarded on E, and its slot opens as the hero sits (Landing): the press and the panel both.
        var press = machine != null && Spec.Machines.Is(machine.Kind, Spec.MachineTrait.Seats)
            ? new Use(new Board(machine.Id), m.Landing.BoardProblem(peer, machine.Id))
            : crate == null ? PressOf(m, peer, cell, shown) : null;
        if (work != null || crate != null || press != null) return new Aim(cell, inReach, press, work, crate);
        // Over a machine's or a plan's risen picture, as if at its footprint's top row under the pointer.
        if (riseOf != null && Risen.Under(grid, m.Building, cell, riseOf) is int top
            && AimAt(m, peer, hero, (point.X, top / grid.Width + 0.5), passOver, armed, shown) is ({ Work: not null } or { Crate: not null } or { Press: not null }) and var under)
            return under;
        var piece = PieceNear(m, hero, point, passOver);
        // Aimed at where the piece is, which may be a cell beside the pointer's.
        if (piece != null && HalfOf(piece.Item) == null)
        {
            int at = DebrisModule.CellOf(piece.X, piece.Y, grid.Width);
            return new Aim(at, Reaches(hero, at, grid.Width), new Use(new Grab(piece.Id), m.Debris.GrabProblem(peer, piece.Id)), Piece: piece.Id);
        }
        if (armed || piece is null || HalfOf(piece.Item) is not var (halfW, halfH)) return new Aim(cell, inReach);
        var grab = new Use(new Grab(piece.Id), m.Debris.GrabProblem(peer, piece.Id));
        return new Aim(DebrisModule.CellOf(piece.X, piece.Y, grid.Width), ReachesBox(hero, piece.X, piece.Y, halfW, halfH), grab, Piece: piece.Id);
    }

    /// <summary>
    /// The side of the square <paramref name="driver"/> works (power-driver): its <c>Area</c> while this player's mode is the
    /// <paramref name="square"/> and the belt can pay its draw (<paramref name="charged"/>), running or not, so the square shows
    /// before the press and holds while the run catches up; else 1, one cell.
    /// </summary>
    public static int SideOf(Spec.Screwdriver? driver, bool square, bool charged) =>
        driver is { Area: > 1 } d && square && charged ? d.Area : 1;

    /// <summary>
    /// How fast <paramref name="driver"/> works (power-driver): at most its <c>Work</c> ticks on one plan a tick, and its
    /// <c>Area</c> squared over all, while it runs or needs no charge; an electric one flat works as the basic screwdriver.
    /// </summary>
    public static (int Pace, int Budget) PaceOf(Spec.Screwdriver? driver, string? running)
    {
        var d = driver is { } b && (running == b.Item || Spec.Items.Get(b.Item).Draw == null) ? b : Spec.Screwdrivers.Basic;
        return (d.Work, d.Area * d.Area);
    }

    /// <summary>
    /// The ticks of work each of <paramref name="count"/> plans gets a tick, in their order: a tick each while
    /// <paramref name="budget"/> lasts, then round again, at most <paramref name="pace"/> on one (power-driver).
    /// </summary>
    public static int[] SharesOf(int count, int pace, int budget)
    {
        var shares = new int[count];
        for (int round = 0; round < pace; round++)
            for (int i = 0; i < count && budget > 0; i++, budget--) shares[i]++;
        return shares;
    }

    /// <summary>
    /// The square of side <paramref name="side"/> worked round <paramref name="point"/>, in cells: its top-left column and row
    /// as the placing stamp lies (<see cref="Brush"/>, 0104), kept on the grid, and its cells in the order they are worked,
    /// the bottom row first, left to right. None with the point off the grid.
    /// </summary>
    public static (int X, int Y, int[] Cells)? SquareAt(int width, int height, (double X, double Y) point, int side)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= width || point.Y >= height) return null;
        var (x, y) = Brush.Corner(width, height, point, side, side);
        var cells = new List<int>();
        for (int row = Math.Min(y + side, height) - 1; row >= y; row--)
            for (int col = x; col < Math.Min(x + side, width); col++) cells.Add(row * width + col);
        return (x, y, [.. cells]);
    }

    /// <summary>
    /// The square of side <paramref name="side"/> the hero of peer <paramref name="peer"/> works at <paramref name="point"/>
    /// (power-driver), with its <see cref="WorkTargets"/> but whole things (<see cref="IsWhole"/>, driver-order): only while it holds
    /// a plan or a mark of a cell; over nothing else, none, and the pointer aims at one cell as the screwdriver's does. None too
    /// while the pointer's own work, <paramref name="under"/>, is whole: a station is aimed at whole, as by the screwdriver.
    /// </summary>
    public static ((int X, int Y, int[] Cells) Square, List<WorkAim> Targets)? WorkedSquare(Registry m, int peer, Hero hero,
        (double X, double Y) point, int side, WorkAim? under = null)
    {
        if (side < 2 || under is { } own && IsWhole(m, own)) return null;
        if (SquareAt(m.Grid.Width, m.Grid.Height, point, side) is not { } square) return null;
        var targets = WorkTargets(m, peer, hero, square.Cells).Where(t => !IsWhole(m, t)).ToList();
        return targets.Count > 0 ? (square, targets) : null;
    }

    /// <summary>
    /// A cell's <paramref name="plans"/> in the order they are worked (driver-order): a back wall's blueprint first, the hull before
    /// what stands in front of it, so a station planned before a meteor holed the wall never stands in the way of patching it; the
    /// rest in the order made.
    /// </summary>
    public static IEnumerable<Plan> BackFirst(IEnumerable<Plan> plans) => plans.OrderBy(p => p.Place?.Layer != "back");

    /// <summary>
    /// Whether <paramref name="work"/> is on a thing larger than its cell, worked as one: a machine's blueprint, mark or repair,
    /// or a piece of several cells, a ladder or a doorway (0104).
    /// </summary>
    public static bool IsWhole(Registry m, WorkAim work)
    {
        if (work.Repair != null) return true;
        if (m.Building.Plans.FirstOrDefault(p => p.Id == work.Plan) is not { } plan) return false;
        if (plan.Place is { } place) return place.Layer == "machine" || m.Grid.Footprint(place.Kind, plan.Cell) is { Count: > 1 };
        return plan.Clear == "machine" || plan.Clear == "structure" && m.Grid.PieceOf(plan.Cell).Count > 1;
    }

    /// <summary>
    /// What a square's <paramref name="cells"/> give the left button to work, in their order: in each cell the hero of peer
    /// <paramref name="peer"/> reaches, each layer's oldest plan that can be worked (when none can, its oldest, to show why),
    /// in <see cref="BackFirst"/>'s order, else a broken machine's repair (magnetic-flare), as <see cref="AimAt"/> chooses for
    /// one cell. A plan or machine under several cells is worked once, at the first. A cell with nothing, a crate or out of
    /// reach gives nothing and stops none of the others.
    /// </summary>
    public static List<WorkAim> WorkTargets(Registry m, int peer, Hero hero, IReadOnlyList<int> cells)
    {
        var grid = m.Grid;
        var plans = m.Building;
        var targets = new List<WorkAim>();
        void Add(WorkAim target)
        {
            if (!targets.Any(t => t.Plan == target.Plan && t.Repair == target.Repair)) targets.Add(target);
        }
        foreach (int cell in cells)
        {
            if (!Reaches(hero, cell, grid.Width)) continue;
            var here = plans.PlansAt(cell);
            if (here.Count == 0)
            {
                if (grid.MachineAt(cell) is { } machine && m.Power.IsBroken(machine.Id))
                    Add(new WorkAim(0, plans.RepairProblem(machine.Id), machine.Id));
                continue;
            }
            var layers = BackFirst(here).Select(p => p.Place?.Layer ?? p.Clear).Distinct().ToList();
            foreach (var layer in layers)
            {
                var of = here.Where(p => (p.Place?.Layer ?? p.Clear) == layer).Select(p => new WorkAim(p.Id, plans.WorkProblem(p.Id, peer))).ToList();
                Add(of.FirstOrDefault(w => w.Problem == null) ?? of[0]);
            }
        }
        return targets;
    }

    /// <summary>
    /// <paramref name="aim"/> as the prompt says it while a square is worked: its work the first of <paramref name="targets"/>
    /// that can be worked, else the first, to show why, within reach; with no target, no work, so the pointer's press is said.
    /// </summary>
    public static Aim OverSquare(Aim aim, IReadOnlyList<WorkAim> targets) => targets.Count == 0
        ? aim with { Work = null }
        : aim with { InReach = true, Work = targets.FirstOrDefault(t => t.Problem == null) ?? targets[0] };

    /// <summary>A point in cells, in thousandths of a cell.</summary>
    public static Point PointOf((double X, double Y) cells) =>
        new((int)Math.Round(cells.X * Hero.Unit), (int)Math.Round(cells.Y * Hero.Unit));

    /// <summary>How far the shoulder is above the feet, in thousandths of a cell, the body upright.</summary>
    private const int ShoulderUp = (int)(Rig.Shoulder / Rig.Cell * Hero.Unit);

    /// <summary>How far past a creature's body its box is struck, in thousandths of a cell: its fur, and a tick's walk.</summary>
    private const int CreatureMargin = 400;

    /// <summary>The box creature <paramref name="c"/> is struck in, in thousandths of a cell: its body and a margin.</summary>
    internal static (int Left, int Top, int Right, int Bottom) BoxOf(Creature c)
    {
        var spec = Spec.Creatures.Get(c.Species);
        int half = spec.Body.Width * Hero.Unit / 2 + CreatureMargin;
        return (c.X - half, c.Y - spec.Body.Height * Hero.Unit - CreatureMargin, c.X + half, c.Y + CreatureMargin);
    }

    /// <summary>A strike with <paramref name="tool"/> on each of <paramref name="touched"/> the sim would let peer <paramref name="peer"/> make.</summary>
    public static List<Strike> StrikesOf(Registry m, int peer, IEnumerable<Creature> touched, string tool) =>
        touched.Where(c => m.Creatures.StrikeProblem(peer, c.Id, tool) == null).Select(c => new Strike(c.Id, tool)).ToList();

    /// <summary>How far along the segment <paramref name="e"/>, from 0 to 1, it enters the box, clipped to it; none when it misses.</summary>
    internal static double? Entered((double Ax, double Ay, double Bx, double By) e, double left, double top, double right, double bottom)
    {
        double enter = 0, leave = 1, dx = e.Bx - e.Ax, dy = e.By - e.Ay;
        bool Clip(double toward, double room)
        {
            if (toward == 0) return room >= 0;
            double t = room / toward;
            if (toward < 0) enter = Math.Max(enter, t);
            else leave = Math.Min(leave, t);
            return enter <= leave;
        }
        return Clip(-dx, e.Ax - left) && Clip(dx, right - e.Ax) && Clip(-dy, e.Ay - top) && Clip(dy, bottom - e.Ay) ? enter : null;
    }

    /// <summary>
    /// <paramref name="hero"/>'s shoulder, the body upright, in thousandths of a cell: where a stroke and a thrown tool's line begin.
    /// The sim takes what is sent from it on the client's word, among friends (0002).
    /// </summary>
    public static Point HandOf(Hero hero) => new(hero.X, hero.Y - ShoulderUp);

    /// <summary>The heading from <paramref name="from"/> to <paramref name="to"/>, in steps of the turn (<see cref="Spec.Headings"/>), to the nearest.</summary>
    public static int HeadingOf(Point from, Point to)
    {
        if (from == to) return 0;
        double turns = Math.Atan2(to.Y - from.Y, to.X - from.X) / (2 * Math.PI);
        return (int)Math.Round(turns * Spec.Headings.Turn) & (Spec.Headings.Turn - 1);
    }

    /// <summary>
    /// The first wreck, in spawn order, with <paramref name="point"/> on its picture, else the small piece
    /// of debris nearest it within <c>Debris.AimRadius</c>; none while the hero is aboard.
    /// </summary>
    private static Piece? PieceNear(Registry m, Hero hero, (double X, double Y) point, IReadOnlySet<int>? passOver)
    {
        if (m.Rooms.RoomAt(hero.BodyCell(m.Grid.Width)) != RoomsModule.Space) return null;
        if (WreckAim.Under(m.Debris.Pieces, point) is { } wreck) return wreck;
        double px = point.X * DebrisModule.Unit, py = point.Y * DebrisModule.Unit;
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

    private static Use? PressOf(Registry m, int peer, int cell, ShownGrid? shown)
    {
        var grid = m.Grid;
        // A debris net is emptied into the inventory, from outside or through the wall it hangs on (docs/decisions/2026-11-03-debris-net.md).
        if (grid.MachineAt(cell) is { } net && Stillwake.Content.Machines.Is(net.Kind, Stillwake.Content.MachineTrait.Catches))
            return new Use(new Collect(net.Id), m.Debris.CollectProblem(peer, net.Id));
        int structure = shown?.StructureAt(grid, cell) ?? grid.StructureAt(cell);
        bool? valve = shown?.ValveOpenAt(m.Pipes, cell) ?? m.Pipes.ValveAt(cell)?.Open;
        // A platform opens only with power (Landing): E on a dark one says why, as on an empty canister.
        if (Doorways.IsPlatform(structure)) return new Use(new SetPlatform(cell, Doorways.IsClosed(structure)), m.Landing.Problem(cell, Doorways.IsClosed(structure)));
        if (Doorways.IsDoorway(structure)) return new Use(new SetDoor(cell, Doorways.IsClosed(structure)), grid.DoorProblem(cell, Doorways.IsClosed(structure)));
        if (valve is bool open) return new Use(new Sim.Pipes.SetValve(cell, !open), null);
        return null;
    }

    /// <summary>
    /// What the hands do this frame: <paramref name="pressed"/> is E, or the left button on a piece of
    /// debris; <paramref name="held"/> the left button with a screwdriver in hand (0107). A press within reach sends what is
    /// pressed, unless the sim would refuse it. Held on a plan, it sends <paramref name="pace"/> <see cref="Work"/> (the
    /// screwdriver's) for every tick released since the last it sent for, so a peer drawing fewer frames than the ticks it
    /// runs builds as fast as any other; a hold just begun sends one tick's. A tick that begins a part of a blueprint pays
    /// its item (0063). <paramref name="tick"/> is the client's world tick. With <paramref name="targets"/> (a square's,
    /// <see cref="WorkTargets"/>), each that can be worked is worked so, in their order, in place of the aim's own work,
    /// <paramref name="budget"/> ticks a tick at most over all of them (<see cref="SharesOf"/>),
    /// the aim out of reach or not.
    /// </summary>
    public static Uses UsesOf(Aim? aim, bool pressed, bool held, int tick, int workedOnTick, int pace = 1, IReadOnlyList<WorkAim>? targets = null,
        int? budget = null)
    {
        var commands = new List<Command>();
        if (aim is { InReach: true })
        {
            // A drop pod boarded opens with it; one the sim would refuse neither seats nor opens.
            if (pressed && aim.Crate is int pod && aim.Press?.Command is Board) return aim.Press.Problem == null ? new([aim.Press.Command], pod, -1) : new([], null, -1);
            if (pressed && aim.Crate is int crate) return new([], crate, -1);
            if (pressed && aim.Press is { Problem: null } press) commands.Add(press.Command);
        }
        var work = (targets ?? (aim is { InReach: true, Work: { } own } ? [own] : [])).Where(t => t.Problem == null).ToList();
        if (work.Count == 0 || !held) return new(commands, null, -1);
        // A hold just begun sends one; so does a load that took the world's tick back.
        bool fresh = workedOnTick < 0 || tick < workedOnTick;
        int due = fresh ? 1 : Math.Min(tick - workedOnTick, MaxWorkPerFrame);
        var shares = SharesOf(work.Count, pace, budget ?? pace * work.Count);
        for (int t = 0; t < work.Count; t++)
            for (int i = 0; i < due * shares[t]; i++) commands.Add(work[t].Repair is int machine ? new Repair(machine) : new Work(work[t].Plan));
        return new(commands, null, due > 0 ? tick : workedOnTick);
    }
}
