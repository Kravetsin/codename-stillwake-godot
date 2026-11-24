using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Hunting the crew through its vessel (derelict-guards), a spider's once awake: at its quarry (<see cref="CreaturesModule.Quarry"/>),
/// by the vessel's shape, no path searched. On its quarry's deck it runs at it, leaping what its jump clears and climbing a wall it does
/// not; on another it runs to the hatch well, opens the hatch and climbs up through it, or drops down through it. A shut doorway on its
/// way, a hatch too, it pries its <c>Pry</c> ticks and opens, the way in never; the doorway changed meanwhile, it starts over. With no
/// awake hero aboard within its <c>Lose</c> it patrols: the same way to a cell of a room drawn at random (<see cref="CreaturesModule.RoamTo"/>),
/// on its deck or another, or up a room's side wall to cling there a while (<see cref="Cling"/>); another drawn once there, at a
/// brink, or <c>Roam</c> ticks on; a hero come within its <c>Chase</c>, it hunts again. It patrols at its <c>Walk</c>, hunts at its <c>Run</c> (<see cref="Pace"/>). Its body never leaves the hull box: no step past
/// its side, out the open way in, nor over a hole down into space, no leap flown out of it. It fights as its guard does
/// (<see cref="GuardDrive"/>), never from a wall.
/// </summary>
internal sealed class ProwlDrive : DriveRule
{
    /// <summary>Kept in its <c>Mem</c>: 1 while it hunts its quarry, 0 while it patrols.</summary>
    internal const string HuntKey = "hunt";

    /// <summary>Kept in its <c>Mem</c>: the column of the cell it patrols to, plus one; 0 for none drawn.</summary>
    internal const string RoamX = "roamX";

    /// <summary>Kept in its <c>Mem</c>: the row of the cell it patrols to.</summary>
    internal const string RoamY = "roamY";

    /// <summary>Kept in its <c>Mem</c>: the creatures' clock it draws another cell to patrol to at.</summary>
    internal const string RoamTill = "roamTill";

    /// <summary>Kept in its <c>Mem</c>: for a patrol up a wall, the wall's side of the cell it patrols to, -1 left, 1 right; 0 for a room's cell.</summary>
    internal const string RoamWall = "roamWall";

    /// <summary>Kept in its <c>Mem</c>: for a patrol up a wall, the row its feet cling at.</summary>
    internal const string RoamSpot = "roamSpot";

    /// <summary>Kept in its <c>Mem</c>: the creatures' clock it comes down the wall it patrols up at; -1 climbing to its spot; 0 holding none for it.</summary>
    internal const string ClingTill = "clingTill";

    /// <summary>Kept in its <c>Mem</c>: the way it patrols by decks to its cell, 1 down, -1 up; 0 for none yet.</summary>
    internal const string RoamDeck = "roamDeck";

    /// <summary>Kept in its <c>Mem</c>: the shut doorway cell it pries, plus one; 0 for none.</summary>
    internal const string PryKey = "pry";

    /// <summary>Kept in its <c>Mem</c>: the creatures' clock its pry began.</summary>
    internal const string PrySince = "prySince";

    /// <summary>Kept in its <c>Mem</c>: the floor row of the hatch it climbs the well to, plus one.</summary>
    internal const string WellKey = "well";

    /// <summary>Its fight: the guard's rule, run on its <c>Guard</c>. Rules hold no state, so one more is as good as the book's.</summary>
    private static readonly GuardDrive Guard = new();

    internal override Type Serves => typeof(Spec.Prowl);

    internal override bool Wants(Creature c, Spec.Drive d) => true;

    internal override void Born(Creature c, Spec.Drive d) => Guard.Born(c, ((Spec.Prowl)d).Guard);

    internal override void Enter(CreaturesModule m, Creature c, Spec.Drive d) => Guard.Enter(m, c, ((Spec.Prowl)d).Guard);

    internal override bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        var prowl = (Spec.Prowl)d;
        Guard.Run(m, c, spec, prowl.Guard);
        if (c.Act == null)
        {
            Choose(m, c, (Spec.Walker)spec.Body, prowl);
            Steer(m, c, spec, prowl);
        }
        else Forget(c);
        return true;
    }

    /// <summary>
    /// Whether it hunts or patrols this tick: its quarry lost past its <c>Lose</c>, or none aboard, it patrols; one within its
    /// <c>Chase</c>, it hunts. Patrolling with no cell to go to, there, or <c>Roam</c> ticks on, it draws another
    /// (<see cref="CreaturesModule.RoamTo"/>); never while it clings to a wall it patrolled up.
    /// </summary>
    private static void Choose(CreaturesModule m, Creature c, Spec.Walker w, Spec.Prowl prowl)
    {
        long? far = m.Quarry(c) is { } q ? Far(c, q) : null;
        if (c.Recall(HuntKey) == 1)
        {
            if (far <= (long)prowl.Lose * prowl.Lose) return;
            c.Keep(HuntKey, 0);
            Roam(c, null, 0);
        }
        else if (far <= (long)prowl.Chase * prowl.Chase)
        {
            c.Keep(HuntKey, 1);
            Roam(c, null, 0);
            return;
        }
        if (Roaming(c) is { } to && (Clinging(c) || m.Clock < c.Recall(RoamTill) && !There(m, c, to))) return;
        Roam(c, m.RoamTo(c, w, prowl), m.Clock + prowl.Roam);
    }

    /// <summary>
    /// The cell <paramref name="c"/> patrols to, kept till the creatures' clock <paramref name="till"/>; none forgets it. Clinging to a wall
    /// it patrolled up, it lets go.
    /// </summary>
    private static void Roam(Creature c, CreaturesModule.Roamed? to, int till)
    {
        if (Clinging(c)) LetGo(c, 0);
        c.Keep(RoamX, to is { } t ? t.X + 1 : 0);
        c.Keep(RoamY, to?.Y ?? 0);
        c.Keep(RoamTill, to == null ? 0 : till);
        c.Keep(RoamWall, to?.Wall ?? 0);
        c.Keep(RoamSpot, to?.Spot ?? 0);
        c.Keep(ClingTill, 0);
        c.Keep(RoamDeck, 0);
    }

    /// <summary>Whether <paramref name="c"/> holds the wall it patrols up.</summary>
    private static bool Clinging(Creature c) =>
        c.Recall(ClingTill) != 0 && c.Recall(RoamWall) is int side and not 0 && c.Recall(WalkerBody.Wall) == side;

    /// <summary>The cell <paramref name="c"/> patrols to; null for none.</summary>
    private static (int X, int Y)? Roaming(Creature c) => c.Recall(RoamX) is int x and > 0 ? (x - 1, c.Recall(RoamY)) : null;

    /// <summary>Whether <paramref name="c"/> stands where it patrols to: on its deck, within a cell of its column. Up a wall it is there once down again.</summary>
    private static bool There(CreaturesModule m, Creature c, (int X, int Y) to) =>
        c.Recall(RoamWall) == 0 && m.Level(Down(c.Y - 1, Unit)) == m.Level(to.Y) && Math.Abs(m.Middle(to.X) - c.X) < Unit;

    /// <summary>Where <paramref name="c"/> goes: its quarry while it hunts, else the cell it patrols to; null for neither.</summary>
    private static (int X, int Y)? Goal(CreaturesModule m, Creature c) => c.Recall(HuntKey) == 1 ? m.Quarry(c) : Roaming(c);

    /// <summary>
    /// Whether a body of <paramref name="w"/> as <paramref name="c"/> stands, moved <paramref name="dx"/> across, would leave the hull box:
    /// past its side, or over no floor down to its bottom, a hole into space.
    /// </summary>
    private static bool Leaves(CreaturesModule m, Creature c, Spec.Walker w, int dx) =>
        !m.InHull(w, c.X + dx, c.Y) || !m.Floored(w, c.X + dx, c.Y);

    /// <summary>A patrol stopped at a brink or a wall it cannot pass: it draws another cell next tick. A hunt waits there.</summary>
    private static void Balked(Creature c)
    {
        c.VX = 0;
        if (c.Recall(HuntKey) != 1) Roam(c, null, 0);
    }

    /// <summary>Alerted long enough, it pounces or shoots as its guard does; holding a wall it waits till it is off.</summary>
    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        if (c.Recall(WalkerBody.Wall) != 0)
        {
            if (c.Step == GuardDrive.Alerted) c.DriveLeft = 1;
            return true;
        }
        bool next = Guard.Next(m, c, spec, ((Spec.Prowl)d).Guard);
        if (c.Act != null) Forget(c);
        return next;
    }

    /// <summary>Struck, it answers as its guard does; an answer begun on a wall lets go of it. A pry an act breaks off starts over after.</summary>
    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit)
    {
        bool claimed = Guard.Hurt(m, c, spec, ((Spec.Prowl)d).Guard, at, hit);
        if (c.Act == null) return claimed;
        Forget(c);
        if (c.Recall(WalkerBody.Wall) != 0) LetGo(c, 0);
        return claimed;
    }

    internal override void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop) =>
        Guard.Ended(m, c, spec, ((Spec.Prowl)d).Guard, way, hop);

    internal override Gait GaitOf(Creature c) => Gait.Steered;

    /// <summary>Off the walls, an open hatch is floor to it unless its quarry is on a deck under its own: then down through it it goes.</summary>
    internal override bool Spans(CreaturesModule m, Creature c) => Bridges(m, c);

    private static bool Bridges(CreaturesModule m, Creature c) =>
        c.Recall(WalkerBody.Wall) == 0 && (Goal(m, c) is not { } q || m.Level(q.Y) <= m.Level(Down(c.Y - 1, Unit)));

    internal override Posture PostureOf(Creature c) =>
        c.Recall(WalkerBody.Wall) != 0 ? Posture.Climbing : c.VX != 0 ? Posture.Wander : Posture.Idle;

    /// <summary>
    /// Its pace a tick, across and up a wall: its <c>Run</c> and <c>Climb</c> while it hunts; patrolling, its <c>Walk</c>, and its
    /// <c>Climb</c> cut to the same share of its run, rounded down.
    /// </summary>
    private static (int Across, int Up) Pace(Creature c, Spec.Walker w) =>
        c.Recall(HuntKey) == 1 || w.Run <= 0 ? (w.Run, w.Climb) : (w.Walk, w.Climb * w.Walk / w.Run);

    /// <summary>Its speed and hold this tick, its body moving it after (<see cref="WalkerBody"/>).</summary>
    private static void Steer(CreaturesModule m, Creature c, Spec.Species spec, Spec.Prowl prowl)
    {
        var w = (Spec.Walker)spec.Body;
        var pace = Pace(c, w);
        int half = w.Width * Unit / 2;
        int left = Down(c.X - half, Unit), right = Down(c.X + half - 1, Unit), top = Down(c.Y - w.Height * Unit, Unit), feet = Down(c.Y - 1, Unit);
        // A doorway shut on it (0006): it pries it where it stands.
        if (m.Overlaps(w, c.X, c.Y))
        {
            if (m.ShutDoorway(left, top, right, feet) is int shut) Pry(m, c, prowl.Pry, shut);
            (c.VX, c.VY) = (0, 0);
            return;
        }
        var quarry = Goal(m, c);
        int hold = c.Recall(WalkerBody.Wall);
        // Off every wall, it holds none for a cling: a climb over in its way is no cling, its wall taken hold of again first.
        if (hold == 0) c.Keep(ClingTill, 0);
        if (hold == WalkerBody.Well)
        {
            Well(m, c, w, pace, prowl, quarry, left, right, top);
            return;
        }
        if (hold != 0)
        {
            OnWall(m, c, w, pace, prowl, hold, quarry);
            return;
        }
        // In the air a leap or a fall flies on, but never across out of the hull box; on an open hatch it spans, it stands.
        if (!(c.VY >= 0 && (m.Overlaps(w, c.X, c.Y + 1) || m.OnOpenHatch(w, c.X, c.Y) && Bridges(m, c))))
        {
            Forget(c);
            if (Leaves(m, c, w, c.VX)) c.VX = 0;
            return;
        }
        if (quarry is not { } q)
        {
            c.VX = 0;
            Forget(c);
            return;
        }
        int mine = m.Level(feet), its = m.Level(q.Y);
        // A patrol past its cell's deck, up then down or down then up: sections laying their decks at other rows, the way by decks
        // never gets there. It draws another.
        if (mine != its && c.Recall(HuntKey) != 1)
        {
            int need = Math.Sign(its - mine), was = c.Recall(RoamDeck);
            if (was == -need)
            {
                Balked(c);
                return;
            }
            c.Keep(RoamDeck, need);
        }
        // Up by a well rising from its floor, else by the hatch in the floor next up; down by a hatch in its floor.
        int? floor = mine == its ? null : m.FloorBy(mine > its ? feet : feet + 1, up: mine > its), hatch = floor is int f ? m.HatchIn(f, c.X) : null;
        if (mine > its && m.WayUp(feet + 1, c.X) is { } rise) (hatch, floor) = (rise.X, rise.Row);
        // A patrol up a wall on its deck: to stand flush against it.
        int wall = mine == its && c.Recall(HuntKey) != 1 ? c.Recall(RoamWall) : 0;
        int goal = hatch ?? (wall < 0 ? q.X * Unit + half : wall > 0 ? (q.X + 1) * Unit - half : m.Middle(q.X)), dx = goal - c.X;
        if (hatch == null && (wall != 0 ? dx == 0 : Math.Abs(dx) < Unit) || hatch != null && dx == 0)
        {
            c.VX = 0;
            if (hatch == null)
            {
                Forget(c);
                if (wall != 0) Hold(m, c, w, pace.Up, wall);
            }
            else if (mine > its) Climb(c, pace.Up, floor!.Value);
            // Over the hatch down: pried, then fallen through.
            else if (m.ShutDoorway(left, feet + 1, right, feet + 1) is int down) Pry(m, c, prowl.Pry, down);
            else Forget(c);
            return;
        }
        int way = Math.Sign(dx), speed = Math.Min(pace.Across, Math.Abs(dx)), column = WalkerBody.AheadOf(c, w, way);
        if (m.ShutDoorway(column, top, column, feet) is int door)
        {
            c.VX = 0;
            Pry(m, c, prowl.Pry, door);
            return;
        }
        Forget(c);
        c.VX = way * speed;
        if (!WalkerBody.Blocked(m, c, w, way, speed) || !WalkerBody.Walled(m, c, w, column))
        {
            // Never a step out of the hull box: past its side (the open way in, a hole in the armour) or over a hole down into space.
            if (Leaves(m, c, w, c.VX)) Balked(c);
            return;
        }
        // A wall: leapt where a leap lands it, else climbed where it may be, else it stands (the way in, the hull's end).
        if (WalkerBody.JumpFor(m, c, w, way, speed, fleeing: true) is int jump) (c.VX, c.VY) = (way * Math.Min(speed, w.JumpAcross), -jump);
        else if (Climbable(m, c, w, way))
        {
            c.Keep(WalkerBody.Wall, way);
            (c.VX, c.VY) = (0, -pace.Up);
        }
        else Balked(c);
    }

    /// <summary>
    /// Whether <paramref name="c"/>, flush against a wall <paramref name="way"/>, may climb it: risen, still in the hull box and its head
    /// clear, it is past the wall's top before anything over it: never up to a ceiling.
    /// </summary>
    private static bool Climbable(CreaturesModule m, Creature c, Spec.Walker w, int way)
    {
        for (int y = c.Y - Unit; y >= w.Height * Unit; y -= Unit)
        {
            if (!m.InHull(w, c.X, y) || m.Overlaps(w, c.X, y)) return false;
            if (!m.Overlaps(w, c.X + way * Unit, y)) return m.InHull(w, c.X + way * Unit, y);
        }
        return false;
    }

    /// <summary>Flush against the wall it patrols up, on side <paramref name="side"/>: it takes hold and climbs; the wall gone, it draws another goal.</summary>
    private static void Hold(CreaturesModule m, Creature c, Spec.Walker w, int up, int side)
    {
        if (!m.Overlaps(w, c.X + side * Unit, c.Y))
        {
            Roam(c, null, 0);
            return;
        }
        c.Keep(WalkerBody.Wall, side);
        c.Keep(ClingTill, -1);
        c.VY = -up;
    }

    /// <summary>
    /// A tick on the wall on side <paramref name="side"/>: over its top once past it; let go where it no longer holds one, or under what
    /// is over it; with no quarry, still. A wall it patrols up it clings to (<see cref="Cling"/>).
    /// </summary>
    private static void OnWall(CreaturesModule m, Creature c, Spec.Walker w, (int Across, int Up) pace, Spec.Prowl prowl, int side, (int X, int Y)? quarry)
    {
        bool flush = (c.X + side * w.Width * Unit / 2) % Unit == 0;
        if (!flush || !m.Overlaps(w, c.X + side * Unit, c.Y))
        {
            c.Keep(WalkerBody.Wall, 0);
            // Past its top: over onto it.
            (c.VX, c.VY) = (flush ? side * pace.Across : 0, 0);
            return;
        }
        if (Clinging(c))
        {
            Cling(m, c, w, pace.Up, prowl);
            return;
        }
        if (m.Overlaps(w, c.X, c.Y - 1) || !m.InHull(w, c.X, c.Y - pace.Up))
        {
            c.Keep(WalkerBody.Wall, 0);
            (c.VX, c.VY) = (0, 0);
            return;
        }
        // Never past its top: it stops level with it, to go over next tick.
        int edge = Down(c.Y - 1, Unit) * Unit;
        c.VY = quarry == null ? 0 : c.Y - edge <= pace.Up && !m.Overlaps(w, c.X + side * Unit, edge) ? edge - c.Y : -pace.Up;
    }

    /// <summary>
    /// A tick up the wall it patrols up, <paramref name="up"/> a tick: up to its spot, or as high as its head clears; there it clings a
    /// number of ticks drawn from its <c>Cling</c>, then comes down to the floor, lets go and draws another goal.
    /// </summary>
    private static void Cling(CreaturesModule m, Creature c, Spec.Walker w, int up, Spec.Prowl prowl)
    {
        int till = c.Recall(ClingTill), spot = (c.Recall(RoamSpot) + 1) * Unit;
        if (till < 0)
        {
            if (c.Y > spot && !m.Overlaps(w, c.X, c.Y - 1))
            {
                c.VY = -Math.Min(up, c.Y - spot);
                return;
            }
            c.VY = 0;
            c.Keep(ClingTill, m.Clock + prowl.Cling.Min + (int)m.Rand.Below((uint)(prowl.Cling.Max - prowl.Cling.Min + 1)));
            return;
        }
        if (m.Clock < till)
        {
            c.VY = 0;
            return;
        }
        // Down to its room's floor, never on down an open hatch under it.
        int floor = (c.Recall(RoamY) + 1) * Unit;
        if (c.Y < floor && !m.Overlaps(w, c.X, c.Y + 1))
        {
            c.VY = Math.Min(up, floor - c.Y);
            return;
        }
        Roam(c, null, 0);
    }

    /// <summary>Takes hold of the well under the hatch in floor row <paramref name="floor"/> and climbs it, <paramref name="up"/> a tick.</summary>
    private static void Climb(Creature c, int up, int floor)
    {
        Forget(c);
        c.Keep(WalkerBody.Wall, WalkerBody.Well);
        c.Keep(WellKey, floor + 1);
        (c.VX, c.VY) = (0, -up);
    }

    /// <summary>
    /// A tick in the hatch well: up through its hatch, on up the well while its quarry is higher still, else off to its side on the floor;
    /// its head under the hatch shut, prying it, as a shut doorway beside it at the top; let go out of line with it.
    /// </summary>
    private static void Well(CreaturesModule m, Creature c, Spec.Walker w, (int Across, int Up) pace, Spec.Prowl prowl, (int X, int Y)? quarry, int left, int right, int top)
    {
        int row = c.Recall(WellKey) - 1;
        if (row < 0 || !m.HatchAt(c.X, row))
        {
            LetGo(c, 0);
            return;
        }
        if (c.Y <= row * Unit)
        {
            if (quarry is { } q && m.Level(q.Y) < m.Level(Down(c.Y - 1, Unit)) && m.WellOver(c.X, row) is int next)
            {
                Climb(c, pace.Up, next);
                return;
            }
            // Off to its side: toward its quarry, a doorway there pried from the well first (stepped to, the hole is under it); a wall
            // there, the other side.
            int way = quarry is { } to && m.Middle(to.X) < c.X ? -1 : 1, column = WalkerBody.AheadOf(c, w, way);
            if (m.ShutDoorway(column, top, column, Down(c.Y - 1, Unit)) is int door)
            {
                c.VY = 0;
                Pry(m, c, prowl.Pry, door);
                return;
            }
            Forget(c);
            if (m.Overlaps(w, c.X + way * Unit, c.Y)) way = -way;
            LetGo(c, way * pace.Across);
            return;
        }
        if (m.ShutDoorway(left, top - 1, right, top - 1) is int hatch)
        {
            c.VY = 0;
            Pry(m, c, prowl.Pry, hatch);
            return;
        }
        Forget(c);
        // Under something solid that is no hatch to pry (a wreck's sunk floor across the well): it lets go.
        if (m.Overlaps(w, c.X, c.Y - 1))
        {
            LetGo(c, 0);
            return;
        }
        c.VY = quarry == null ? 0 : -Math.Min(pace.Up, c.Y - row * Unit);
    }

    private static void LetGo(Creature c, int vx)
    {
        c.Keep(WalkerBody.Wall, 0);
        c.Keep(WellKey, 0);
        (c.VX, c.VY) = (vx, 0);
    }

    /// <summary>
    /// A tick at shut doorway <paramref name="cell"/>: begun, or counted on, and opened <paramref name="pry"/> ticks on; another doorway begins
    /// anew. A guard's pry, the escort's too (<see cref="PatrolDrive"/>).
    /// </summary>
    internal static void Pry(CreaturesModule m, Creature c, int pry, int cell)
    {
        if (c.Recall(PryKey) != cell + 1)
        {
            c.Keep(PryKey, cell + 1);
            c.Keep(PrySince, m.Clock);
            return;
        }
        if (m.Clock - c.Recall(PrySince) < pry) return;
        m.Open(cell);
        Forget(c);
    }

    /// <summary>No doorway pried: one met again begins anew.</summary>
    internal static void Forget(Creature c)
    {
        if (c.Recall(PryKey) == 0) return;
        c.Keep(PryKey, 0);
        c.Keep(PrySince, 0);
    }
}
