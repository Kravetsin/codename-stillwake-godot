using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Fauna;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>Something E does: the command it sends, and why the sim would refuse it, if it would.</summary>
public sealed record Use(Command Command, string? Problem);

/// <summary>A plan the left button works on, and why the sim would refuse it.</summary>
public sealed record WorkAim(int Plan, string? Problem);

/// <summary>
/// The cell under the pointer and what the hands do there: the left button held works a plan; E toggles
/// a door or valve, puts a canister into a tank, grabs a piece of debris or opens crate <see cref="Crate"/>.
/// <see cref="Piece"/> names the piece of debris a press grabs, or the wreck a swing of the axe would hit
/// (0034, 0039), which is outlined itself, not its cell. <see cref="Square"/> is the brush a dig covers, its top-left column
/// and row and its side, when it is wider than a cell (<see cref="Brush"/>): outlined whole.
/// </summary>
public sealed record Aim(int Cell, bool InReach, Use? Press = null, WorkAim? Work = null, int? Crate = null, int? Piece = null,
    (int X, int Y, int Side)? Square = null);

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

    /// <summary>
    /// Half a wreck's picture each way, in thousandths of a cell, and a little more, as it rocks; none for
    /// a piece caught by hand.
    /// </summary>
    private static (int HalfW, int HalfH)? HalfOf(string item) =>
        DebrisLooks.Wrecks.TryGetValue(item, out var size)
            ? (size.W * Hero.Unit / (2 * PixelsPerCell) + WreckMargin, size.H * Hero.Unit / (2 * PixelsPerCell) + WreckMargin)
            : null;

    private const int PixelsPerCell = Camera.Cell, WreckMargin = 500;

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
    /// Whether <paramref name="cell"/> holds the harpoon's rod (the grapple, 0044): a wall, a floor or a closed
    /// door, as <paramref name="ship"/> shows them. Not a back wall, behind the hero's plane; not an open door
    /// nor a ladder, which are passed through; nothing off the grid.
    /// </summary>
    public static bool Grips(Ship ship, int x, int y) =>
        x >= 0 && y >= 0 && x < ship.Width && y < ship.Height
        && ship.StructureAt(y * ship.Width + x) is Structure.Wall or Structure.Floor or Structure.DoorClosed;

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

    /// <summary>Whether a machine of <paramref name="kind"/> holds items, opened with E: a crate, a furnace, an analyzer.</summary>
    private static bool HoldsItems(string kind) => Spec.Machines.TryGet(kind, out var spec) && spec.Slots != null;

    /// <summary>Whether crate, furnace or analyzer <paramref name="id"/> still stands with a cell of it within the hero's reach.</summary>
    public static bool CrateInReach(IGridQueries grid, Hero hero, int id)
    {
        var crate = grid.MachineById(id) is { } m && HoldsItems(m.Kind) ? m : null;
        var cells = crate == null ? [] : grid.Footprint(crate.Kind, crate.Cell) ?? [];
        return cells.Any(cell => Reaches(hero, cell, grid.Width));
    }

    /// <summary>
    /// What the hero of peer <paramref name="peer"/> aims at with the pointer at <paramref name="point"/>,
    /// in cells; nothing off the grid. A plan in the cell is worked: of several, the oldest that can be
    /// worked, so one the hero cannot pay for does not block the mark beside it; when none can, the
    /// oldest, to show why. E works what
    /// is under it all the same: a crate, a furnace or an analyzer, a tank, a door, a valve. With neither,
    /// a wreck whose picture is under the pointer, else the piece of debris nearest it within
    /// <c>Debris.AimRadius</c>, only while the hero is out in space, where it can catch one, and none in
    /// <paramref name="passOver"/>: grabbed already, its grab still in flight. Bare-handed, a wreck is
    /// grabbed, which the sim refuses: too big. With the <paramref name="axe"/>, a small piece is still
    /// grabbed, but no wreck by the pointer is aimed at: the first a swing toward the pointer begun now
    /// would touch is (<see cref="Struck"/>), wherever the pointer is. With a <paramref name="drill"/>, the plain or the arc
    /// drill, by its item, the same, the first wreck its bit aimed at the pointer touches (<see cref="Touched"/>); ground or a plant in
    /// the <paramref name="brush"/>'s square round the pointer is dug first (<see cref="DugAim"/>). With a <paramref name="gun"/>, the
    /// harpoon, a small piece is still grabbed and no wreck is aimed at: it is shot, not worked. A door or valve is
    /// opened or shut as <paramref name="shown"/> shows it (the world's without it): with this peer's own toggles in
    /// flight, the next press undoes them, and a press says what its sender saw should become of it.
    /// </summary>
    public static Aim? AimAt(
        Registry m, int peer, Hero hero, (double X, double Y) point,
        IReadOnlySet<int>? passOver = null, bool axe = false, bool gun = false, string? drill = null, ShownGrid? shown = null, int brush = 1,
        bool back = false)
    {
        var grid = m.Grid;
        int x = (int)Math.Floor(point.X), y = (int)Math.Floor(point.Y);
        if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height) return null;
        int cell = y * grid.Width + x;
        bool inReach = Reaches(hero, cell, grid.Width);

        var plans = m.Building;
        var here = plans.PlansAt(cell)
            .Select(p => new WorkAim(p.Id, plans.WorkProblem(p.Id, peer)))
            .ToList();
        var work = here.FirstOrDefault(w => w.Problem == null) ?? here.FirstOrDefault();
        var machine = grid.MachineAt(cell);
        int? crate = machine != null && HoldsItems(machine.Kind) ? machine.Id : null;
        var press = crate == null ? PressOf(m, peer, cell, shown) : null;
        if (work != null || crate != null || press != null) return new Aim(cell, inReach, press, work, crate);
        var piece = PieceNear(m, hero, point, passOver);
        // Aimed at where the piece is, which may be a cell beside the pointer's.
        if (piece != null && HalfOf(piece.Item) == null)
        {
            int at = DebrisModule.CellOf(piece.X, piece.Y, grid.Width);
            return new Aim(at, Reaches(hero, at, grid.Width), new Use(new Grab(piece.Id), m.Debris.GrabProblem(peer, piece.Id)), Piece: piece.Id);
        }
        if (axe || drill != null)
        {
            // A swing begun now faces the pointer (Poses.FaceOf), and so does the stroke it is judged by; the drill always does.
            string tool = axe ? Spec.Weapons.Axe.Item : drill!;
            // Natural ground under the brush is dug where it is (0077), in a shaft or a cave as under the open sky; a plant felled (0079).
            // The right button's: the back walls under it, whatever they are of (0107), and nothing else.
            if (DugAim(m, peer, hero, point, cell, tool, brush, back) is { } dug) return dug;
            if (back) return new Aim(cell, inReach);
            var toward = hero with { Facing = Poses.FaceOf(point, hero) ?? hero.Facing };
            var touched = axe ? Struck(m, toward, PointOf(point)) : Touched(m, toward, PointOf(point));
            if (touched.FirstOrDefault() is not { } wreck) return new Aim(cell, inReach);
            return new Aim(DebrisModule.CellOf(wreck.X, wreck.Y, grid.Width), true, new Use(new Hit(wreck.Id, tool), m.Debris.HitProblem(peer, wreck.Id, tool)), Piece: wreck.Id);
        }
        if (gun || piece is null || HalfOf(piece.Item) is not var (halfW, halfH)) return new Aim(cell, inReach);
        var grab = new Use(new Grab(piece.Id), m.Debris.GrabProblem(peer, piece.Id));
        return new Aim(DebrisModule.CellOf(piece.X, piece.Y, grid.Width), ReachesBox(hero, piece.X, piece.Y, halfW, halfH), grab, Piece: piece.Id);
    }

    /// <summary>A point in cells, in thousandths of a cell.</summary>
    public static Point PointOf((double X, double Y) cells) =>
        new((int)Math.Round(cells.X * Hero.Unit), (int)Math.Round(cells.Y * Hero.Unit));

    /// <summary>How far the shoulder is above the feet, in thousandths of a cell, the body upright.</summary>
    private const int ShoulderUp = (int)(Rig.Shoulder / Rig.Cell * Hero.Unit);

    /// <summary>The downstroke sampled every this many radians (2°): the edge moves under a tenth of a cell between two.</summary>
    private const double SampleStep = Math.PI / 90;

    /// <summary>
    /// The wrecks a swing of <paramref name="look"/>, the fire axe unless a test says, by
    /// <paramref name="hero"/> toward <paramref name="at"/>, in thousandths of a cell, touches as it
    /// strikes (0039, 0040), in spawn order; none while the hero is aboard. Its edge, held as the figure
    /// draws it (<see cref="AxeSwing.EdgeAt"/>), sweeps the downstroke's last stretch, from crossing the
    /// line to the point to <see cref="AxeSwing.Through"/> past it, from the shoulder of the body upright,
    /// turned the way the hero faces; never the raised stretch over the head. A wreck is touched where it
    /// is aimed at: its picture and a margin, square to the world.
    /// </summary>
    public static List<Piece> Struck(Registry m, Hero hero, Point at, HeldLook? look = null)
    {
        var struck = new List<Piece>();
        if (m.Rooms.RoomAt(hero.BodyCell(m.Grid.Width)) != RoomsModule.Space) return struck;
        var edges = SwingEdges(hero, at, look ?? HeldLook.FireAxe);
        foreach (var p in m.Debris.Pieces)
        {
            if (HalfOf(p.Item) is not var (halfW, halfH)) continue;
            if (edges.Any(e => Crosses(e, p.X - halfW, p.Y - halfH, p.X + halfW, p.Y + halfH))) struck.Add(p);
        }
        return struck;
    }

    /// <summary>
    /// The edge of <paramref name="look"/>, held as the figure draws it, swung by <paramref name="hero"/> toward <paramref name="at"/>,
    /// at every sample of the downstroke's last stretch, from the shoulder of the body upright, turned the way the hero faces.
    /// </summary>
    private static (double Ax, double Ay, double Bx, double By)[] SwingEdges(Hero hero, Point at, HeldLook look)
    {
        double sx = hero.X, sy = hero.Y - ShoulderUp, perPixel = Hero.Unit / Rig.Cell;
        double line = Math.Atan2(at.Y - sy, (at.X - sx) * hero.Facing), from = line + AxeSwing.Lead(look);
        int steps = (int)Math.Ceiling(AxeSwing.Through / SampleStep);
        var edges = new (double Ax, double Ay, double Bx, double By)[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            var (a, b) = AxeSwing.EdgeAt(look, from + AxeSwing.Through * i / steps);
            edges[i] = (sx + hero.Facing * a.X * perPixel, sy + a.Y * perPixel, sx + hero.Facing * b.X * perPixel, sy + b.Y * perPixel);
        }
        return edges;
    }

    /// <summary>How far past a creature's body its box is struck, in thousandths of a cell: its fur, and a tick's walk.</summary>
    private const int CreatureMargin = 400;

    /// <summary>The box creature <paramref name="c"/> is struck in, in thousandths of a cell: its body and a margin.</summary>
    private static (int Left, int Top, int Right, int Bottom) BoxOf(Creature c)
    {
        var spec = Spec.Fauna.Get(c.Species);
        int half = spec.Width * Hero.Unit / 2 + CreatureMargin;
        return (c.X - half, c.Y - spec.Height * Hero.Unit - CreatureMargin, c.X + half, c.Y + CreatureMargin);
    }

    /// <summary>
    /// The creatures a swing of <paramref name="look"/>, the fire axe unless a test says, by <paramref name="hero"/> toward
    /// <paramref name="at"/> touches as it strikes, in id order (0102): its edge as for a wreck (<see cref="Struck"/>), against each
    /// creature's body and a margin, wherever the hero stands.
    /// </summary>
    public static List<Creature> StruckCreatures(Registry m, Hero hero, Point at, HeldLook? look = null)
    {
        var struck = new List<Creature>();
        var all = m.Fauna.Creatures;
        if (all.Count == 0) return struck;
        var edges = SwingEdges(hero, at, look ?? HeldLook.FireAxe);
        foreach (var c in all)
        {
            var (left, top, right, bottom) = BoxOf(c);
            if (edges.Any(e => Crosses(e, left, top, right, bottom))) struck.Add(c);
        }
        return struck;
    }

    /// <summary>
    /// The creatures the bit of <paramref name="look"/>, the drill unless a test says, touches held by <paramref name="hero"/>
    /// aimed at <paramref name="at"/> (0102), in id order: its bit as for a wreck (<see cref="Touched"/>), wherever the hero stands.
    /// </summary>
    public static List<Creature> TouchedCreatures(Registry m, Hero hero, Point at, HeldLook? look = null)
    {
        var touched = new List<Creature>();
        var all = m.Fauna.Creatures;
        if (all.Count == 0) return touched;
        var bit = BitOf(hero, at, look ?? HeldLook.Drill);
        foreach (var c in all)
        {
            var (left, top, right, bottom) = BoxOf(c);
            if (Crosses(bit, left, top, right, bottom)) touched.Add(c);
        }
        return touched;
    }

    /// <summary>A strike with <paramref name="tool"/> on each of <paramref name="touched"/> the sim would let peer <paramref name="peer"/> make.</summary>
    public static List<Strike> StrikesOf(Registry m, int peer, IEnumerable<Creature> touched, string tool) =>
        touched.Where(c => m.Fauna.StrikeProblem(peer, c.Id, tool) == null).Select(c => new Strike(c.Id, tool)).ToList();

    /// <summary>Whether the segment <paramref name="e"/> meets the box from (<paramref name="left"/>, <paramref name="top"/>) to (<paramref name="right"/>, <paramref name="bottom"/>): clipped to it, something is left.</summary>
    private static bool Crosses((double Ax, double Ay, double Bx, double By) e, double left, double top, double right, double bottom) =>
        Entered(e, left, top, right, bottom) != null;

    /// <summary>How far along the segment <paramref name="e"/>, from 0 to 1, it enters the box, as <see cref="Crosses"/> clips it; none when it misses.</summary>
    private static double? Entered((double Ax, double Ay, double Bx, double By) e, double left, double top, double right, double bottom)
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
    /// The hits peer <paramref name="peer"/>'s swing of the axe toward <paramref name="at"/> sends as it strikes:
    /// one for each wreck it touches (<see cref="Struck"/>) the sim would let it hit.
    /// </summary>
    public static List<Hit> HitsOf(Registry m, int peer, Hero hero, Point at) => HitsOf(m, peer, Struck(m, hero, at), Spec.Weapons.Axe.Item);

    /// <summary>A hit with <paramref name="tool"/> on each of <paramref name="touched"/> the sim would let peer <paramref name="peer"/> hit.</summary>
    public static List<Hit> HitsOf(Registry m, int peer, IEnumerable<Piece> touched, string tool) =>
        touched.Where(p => m.Debris.HitProblem(peer, p.Id, tool) == null).Select(p => new Hit(p.Id, tool)).ToList();

    /// <summary>
    /// The wrecks the bit of <paramref name="look"/>, the drill unless a test says, touches, held by
    /// <paramref name="hero"/> aimed at <paramref name="at"/>, in thousandths of a cell (0054), in spawn order;
    /// none while the hero is aboard. Held in both hands, the drill turns at the chest so the bit's line runs
    /// through the point, as far as it turns (<see cref="TwoHand.AngleFor"/>), from the shoulder of the body upright, turned the way
    /// the hero faces, as the figure draws it; the bit, its edge, is tested against each wreck's aim box as the axe's blade is.
    /// </summary>
    public static List<Piece> Touched(Registry m, Hero hero, Point at, HeldLook? look = null)
    {
        var touched = new List<Piece>();
        if (m.Rooms.RoomAt(hero.BodyCell(m.Grid.Width)) != RoomsModule.Space) return touched;
        var bit = BitOf(hero, at, look ?? HeldLook.Drill);
        foreach (var p in m.Debris.Pieces)
            if (HalfOf(p.Item) is var (halfW, halfH) && Crosses(bit, p.X - halfW, p.Y - halfH, p.X + halfW, p.Y + halfH)) touched.Add(p);
        return touched;
    }

    /// <summary>The bit of <paramref name="look"/> held by <paramref name="hero"/> aimed at <paramref name="at"/>, turned at the chest as far as it turns, as the figure draws it.</summary>
    private static (double Ax, double Ay, double Bx, double By) BitOf(Hero hero, Point at, HeldLook look)
    {
        double sx = hero.X, sy = hero.Y - ShoulderUp, perPixel = Hero.Unit / Rig.Cell;
        double angle = TwoHand.AngleFor(look, (at.X - sx) * hero.Facing / perPixel, (at.Y - sy) / perPixel);
        var (a, b) = (TwoHand.At(look, angle, look.EdgeFrom), TwoHand.At(look, angle, look.EdgeTo));
        return (sx + hero.Facing * a.X * perPixel, sy + a.Y * perPixel, sx + hero.Facing * b.X * perPixel, sy + b.Y * perPixel);
    }

    /// <summary>
    /// The digs a strike of <paramref name="tool"/> at <paramref name="at"/>, in thousandths of a cell, sends (0077) with a brush
    /// <paramref name="brush"/> cells across round it (<see cref="Brush"/>), ascending: one at each natural wall or plant of its
    /// square within the hero's reach, as plans are reached, but one a plant, at its first cell, so a tree falls as fast as
    /// it did to one cell; none where the sim would refuse it, nor off the grid. Each takes its full damage. Wherever the hero
    /// stands, a shaft or a cave too: no swing nor bit is traced, the cells are the pointer's.
    /// </summary>
    public static List<Dig> DigsAt(Registry m, int peer, Hero hero, Point at, string tool, int brush, bool back = false)
    {
        var digs = new List<Dig>();
        var grid = m.Grid;
        if (CellAt(grid, at) == null) return digs;
        var felled = new List<Plant>();
        foreach (int cell in Brush.Cells(grid.Width, grid.Height, (at.X / (double)Hero.Unit, at.Y / (double)Hero.Unit), brush))
        {
            if (!Digs(m, peer, hero, cell, tool, back)) continue;
            if (back)
            {
                digs.Add(new Dig(cell, tool, Back: true));
                continue;
            }
            if (grid.WallAt(cell) is not { Natural: true } && m.Building.PlantAt(cell) is { } plant)
            {
                if (felled.Contains(plant)) continue;
                felled.Add(plant);
            }
            digs.Add(new Dig(cell, tool));
        }
        return digs;
    }

    /// <summary>The dig of <paramref name="digs"/> heard and seen as a strike's own at <paramref name="at"/>: the one in its cell, else the first.</summary>
    public static Dig? LeadOf(IReadOnlyList<Dig> digs, Point at, int width)
    {
        int x = (int)Math.Floor(at.X / (double)Hero.Unit), y = (int)Math.Floor(at.Y / (double)Hero.Unit);
        foreach (var dig in digs)
            if (dig.Cell == y * width + x) return dig;
        return digs.Count > 0 ? digs[0] : null;
    }

    /// <summary>
    /// Whether a strike of <paramref name="tool"/> digs <paramref name="cell"/>: natural ground or a plant there, or with
    /// <paramref name="back"/> its back wall, within reach, as the sim would take it.
    /// </summary>
    private static bool Digs(Registry m, int peer, Hero hero, int cell, string tool, bool back = false) =>
        IsGround(m, cell, back) && Reaches(hero, cell, m.Grid.Width) && m.Building.DigProblem(peer, cell, tool, back) == null;

    /// <summary>Whether <paramref name="cell"/> holds what is dug: a natural wall, or a plant (0079); a back wall of any material for the right button (0107).</summary>
    private static bool IsGround(Registry m, int cell, bool back = false) =>
        back ? m.Grid.BackAt(cell) != null : m.Grid.WallAt(cell) is { Natural: true } || m.Building.PlantAt(cell) != null;

    /// <summary>
    /// The aim of a strike of <paramref name="tool"/> with the brush <paramref name="brush"/> cells across round
    /// <paramref name="point"/>, in cells, when ground or a plant is in its square: at the pointer's <paramref name="cell"/> if it
    /// could be dug, else the first cell that could, else the pointer's own if it is ground, else the first that is, with why
    /// the sim would refuse it; none with no ground in the square.
    /// </summary>
    private static Aim? DugAim(Registry m, int peer, Hero hero, (double X, double Y) point, int cell, string tool, int brush, bool back = false)
    {
        var grid = m.Grid;
        var ground = Brush.Cells(grid.Width, grid.Height, point, brush).Where(c => IsGround(m, c, back)).OrderBy(c => c == cell ? 0 : 1).ToList();
        if (ground.Count == 0) return null;
        int at = ground.FirstOrDefault(c => Digs(m, peer, hero, c, tool, back), ground[0]);
        (int, int, int)? square = brush > 1 && Brush.Corner(grid.Width, grid.Height, point, brush, brush) is var (x, y) ? (x, y, brush) : null;
        return new Aim(at, Reaches(hero, at, grid.Width), new Use(new Dig(at, tool, back), m.Building.DigProblem(peer, at, tool, back)), Square: square);
    }

    private static int? CellAt(IGridQueries grid, Point at)
    {
        int x = (int)Math.Floor(at.X / (double)Hero.Unit), y = (int)Math.Floor(at.Y / (double)Hero.Unit);
        return x < 0 || y < 0 || x >= grid.Width || y >= grid.Height ? null : y * grid.Width + x;
    }

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

    private static Use? PressOf(Registry m, int peer, int cell, ShownGrid? shown)
    {
        var grid = m.Grid;
        if (grid.MachineAt(cell) is { Kind: "tank" } tank) return new Use(new LoadCanister(tank.Id), m.Atmosphere.CanisterProblem(tank.Id, peer));
        int structure = shown?.StructureAt(grid, cell) ?? grid.StructureAt(cell);
        int conduit = shown?.ConduitAt(grid, cell) ?? grid.ConduitAt(cell);
        if (structure is Structure.DoorClosed or Structure.DoorOpen) return new Use(new SetDoor(cell, structure == Structure.DoorClosed), null);
        if (conduit is Conduit.ValveClosed or Conduit.ValveOpen) return new Use(new SetValve(cell, conduit == Conduit.ValveClosed), null);
        return null;
    }

    /// <summary>
    /// What the hands do this frame: <paramref name="pressed"/> is E, or the left button on a piece of
    /// debris; <paramref name="held"/> the left button with a screwdriver in hand (0107). A press within reach sends what is
    /// pressed, unless the sim would refuse it. Held on a plan, it sends <paramref name="pace"/> <see cref="Work"/> (the
    /// screwdriver's) for every tick released since the last it sent for, so a peer drawing fewer frames than the ticks it
    /// runs builds as fast as any other; a hold just begun sends one tick's. A tick that begins a part of a blueprint pays
    /// its item (0063). <paramref name="tick"/> is the client's world tick.
    /// </summary>
    public static Uses UsesOf(Aim? aim, bool pressed, bool held, int tick, int workedOnTick, int pace = 1)
    {
        if (aim is not { InReach: true }) return new([], null, -1);
        if (pressed && aim.Crate is int crate) return new([], crate, -1);
        var commands = new List<Command>();
        // A wreck is chopped by a swing of the axe (AxeSwing), never by a press; the ground dug so too (0077).
        if (pressed && aim.Press is { Problem: null } press && press.Command is not (Hit or Dig)) commands.Add(press.Command);
        if (aim.Work is not { Problem: null } work || !held) return new(commands, null, -1);
        // A hold just begun sends one; so does a load that took the world's tick back.
        bool fresh = workedOnTick < 0 || tick < workedOnTick;
        int due = fresh ? 1 : Math.Min(tick - workedOnTick, MaxWorkPerFrame);
        for (int i = 0; i < due * pace; i++) commands.Add(new Work(work.Plan));
        return new(commands, null, due > 0 ? tick : workedOnTick);
    }
}
