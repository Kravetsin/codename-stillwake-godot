using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Hunting the crew through its vessel (derelict-enemies), a spider's once awake: at its quarry (<see cref="CreaturesModule.Quarry"/>),
/// by the vessel's shape, no path searched. On its quarry's deck it runs at it, leaping what its jump clears and climbing a wall it does
/// not; on another it runs to the hatch well, opens the hatch and climbs up through it, or drops down through it. A shut doorway on its
/// way, a hatch too, it pries its <c>Pry</c> ticks and opens, the way in never; the doorway changed meanwhile, it starts over. With no
/// hero aboard it stands where it is, on a wall too. It fights as its guard does (<see cref="GuardDrive"/>), never from a wall.
/// </summary>
internal sealed class ProwlDrive : DriveRule
{
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
        if (c.Act == null) Steer(m, c, spec, prowl);
        return true;
    }

    /// <summary>Alerted long enough, it pounces or shoots as its guard does; holding a wall it waits till it is off.</summary>
    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        if (c.Recall(WalkerBody.Wall) == 0) return Guard.Next(m, c, spec, ((Spec.Prowl)d).Guard);
        if (c.Step == GuardDrive.Alerted) c.DriveLeft = 1;
        return true;
    }

    /// <summary>Struck, it answers as its guard does; an answer begun on a wall lets go of it.</summary>
    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit)
    {
        bool claimed = Guard.Hurt(m, c, spec, ((Spec.Prowl)d).Guard, at, hit);
        if (c.Act != null && c.Recall(WalkerBody.Wall) != 0) LetGo(c, 0);
        return claimed;
    }

    internal override void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop) =>
        Guard.Ended(m, c, spec, ((Spec.Prowl)d).Guard, way, hop);

    internal override Gait GaitOf(Creature c) => Gait.Steered;

    internal override Posture PostureOf(Creature c) =>
        c.Recall(WalkerBody.Wall) != 0 ? Posture.Climbing : c.VX != 0 ? Posture.Wander : Posture.Idle;

    /// <summary>Its speed and hold this tick, its body moving it after (<see cref="WalkerBody"/>).</summary>
    private static void Steer(CreaturesModule m, Creature c, Spec.Species spec, Spec.Prowl prowl)
    {
        var w = (Spec.Walker)spec.Body;
        int half = w.Width * Unit / 2;
        int left = Down(c.X - half, Unit), right = Down(c.X + half - 1, Unit), top = Down(c.Y - w.Height * Unit, Unit), feet = Down(c.Y - 1, Unit);
        // A doorway shut on it (0006): it pries it where it stands.
        if (m.Overlaps(w, c.X, c.Y))
        {
            if (m.ShutDoorway(left, top, right, feet) is int shut) Pry(m, c, prowl, shut);
            (c.VX, c.VY) = (0, 0);
            return;
        }
        var quarry = m.Quarry(c);
        int hold = c.Recall(WalkerBody.Wall);
        if (hold == WalkerBody.Well)
        {
            Well(m, c, w, prowl, quarry, left, right, top);
            return;
        }
        if (hold != 0)
        {
            OnWall(m, c, w, hold, quarry);
            return;
        }
        // In the air a leap or a fall flies on.
        if (!(c.VY >= 0 && m.Overlaps(w, c.X, c.Y + 1)))
        {
            Forget(c);
            return;
        }
        if (quarry is not { } q)
        {
            c.VX = 0;
            Forget(c);
            return;
        }
        int mine = m.Level(feet), its = m.Level(q.Y);
        int? floor = mine == its ? null : m.FloorBy(mine > its ? feet : feet + 1, up: mine > its);
        int? hatch = floor is int f ? m.HatchIn(f, c.X) : null;
        int goal = hatch ?? m.Middle(q.X), dx = goal - c.X;
        if (hatch == null && Math.Abs(dx) < Unit || hatch != null && dx == 0)
        {
            c.VX = 0;
            if (hatch == null) Forget(c);
            else if (mine > its) Climb(m, c, w, floor!.Value);
            // Over the hatch down: pried, then fallen through.
            else if (m.ShutDoorway(left, feet + 1, right, feet + 1) is int down) Pry(m, c, prowl, down);
            else Forget(c);
            return;
        }
        int way = Math.Sign(dx), speed = Math.Min(w.Run, Math.Abs(dx)), column = WalkerBody.AheadOf(c, w, way);
        if (m.ShutDoorway(column, top, column, feet) is int door)
        {
            c.VX = 0;
            Pry(m, c, prowl, door);
            return;
        }
        Forget(c);
        c.VX = way * speed;
        if (!WalkerBody.Blocked(m, c, w, way, speed) || !WalkerBody.Walled(m, c, w, column)) return;
        // A wall: leapt where a leap lands it, else climbed where it may be, else it stands (the way in, the hull's end).
        if (WalkerBody.JumpFor(m, c, w, way, speed, fleeing: true) is int jump) (c.VX, c.VY) = (way * Math.Min(speed, w.JumpAcross), -jump);
        else if (Climbable(m, c, w, way))
        {
            c.Keep(WalkerBody.Wall, way);
            (c.VX, c.VY) = (0, -w.Climb);
        }
        else c.VX = 0;
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

    /// <summary>
    /// A tick on the wall on side <paramref name="side"/>: over its top once past it; let go where it no longer holds one, or under what
    /// is over it; with no quarry, still.
    /// </summary>
    private static void OnWall(CreaturesModule m, Creature c, Spec.Walker w, int side, (int X, int Y)? quarry)
    {
        bool flush = (c.X + side * w.Width * Unit / 2) % Unit == 0;
        if (!flush || !m.Overlaps(w, c.X + side * Unit, c.Y))
        {
            c.Keep(WalkerBody.Wall, 0);
            // Past its top: over onto it.
            (c.VX, c.VY) = (flush ? side * w.Run : 0, 0);
            return;
        }
        if (m.Overlaps(w, c.X, c.Y - 1) || !m.InHull(w, c.X, c.Y - w.Climb))
        {
            c.Keep(WalkerBody.Wall, 0);
            (c.VX, c.VY) = (0, 0);
            return;
        }
        // Never past its top: it stops level with it, to go over next tick.
        int edge = Down(c.Y - 1, Unit) * Unit;
        c.VY = quarry == null ? 0 : c.Y - edge <= w.Climb && !m.Overlaps(w, c.X + side * Unit, edge) ? edge - c.Y : -w.Climb;
    }

    /// <summary>Takes hold of the well under the hatch in floor row <paramref name="floor"/> and climbs it.</summary>
    private static void Climb(CreaturesModule m, Creature c, Spec.Walker w, int floor)
    {
        Forget(c);
        c.Keep(WalkerBody.Wall, WalkerBody.Well);
        c.Keep(WellKey, floor + 1);
        (c.VX, c.VY) = (0, -w.Climb);
    }

    /// <summary>
    /// A tick in the hatch well: up through its hatch, on up the well while its quarry is higher still, else off to its side on the floor;
    /// its head under the hatch shut, prying it, as a shut doorway beside it at the top; let go out of line with it.
    /// </summary>
    private static void Well(CreaturesModule m, Creature c, Spec.Walker w, Spec.Prowl prowl, (int X, int Y)? quarry, int left, int right, int top)
    {
        int row = c.Recall(WellKey) - 1;
        if (row < 0 || !m.HatchAt(c.X, row))
        {
            LetGo(c, 0);
            return;
        }
        if (c.Y <= row * Unit)
        {
            if (quarry is { } q && m.Level(q.Y) < m.Level(Down(c.Y - 1, Unit)) && m.HatchOver(c.X, c.Y) is int next)
            {
                Climb(m, c, w, next);
                return;
            }
            // Off to its side: toward its quarry, a doorway there pried from the well first (stepped to, the hole is under it); a wall
            // there, the other side.
            int way = quarry is { } to && m.Middle(to.X) < c.X ? -1 : 1, column = WalkerBody.AheadOf(c, w, way);
            if (m.ShutDoorway(column, top, column, Down(c.Y - 1, Unit)) is int door)
            {
                c.VY = 0;
                Pry(m, c, prowl, door);
                return;
            }
            Forget(c);
            if (m.Overlaps(w, c.X + way * Unit, c.Y)) way = -way;
            LetGo(c, way * w.Run);
            return;
        }
        if (m.ShutDoorway(left, top - 1, right, top - 1) is int hatch)
        {
            c.VY = 0;
            Pry(m, c, prowl, hatch);
            return;
        }
        Forget(c);
        c.VY = quarry == null ? 0 : -Math.Min(w.Climb, c.Y - row * Unit);
    }

    private static void LetGo(Creature c, int vx)
    {
        c.Keep(WalkerBody.Wall, 0);
        c.Keep(WellKey, 0);
        (c.VX, c.VY) = (vx, 0);
    }

    /// <summary>A tick at shut doorway <paramref name="cell"/>: begun, or counted on, and opened at its <c>Pry</c>; another doorway begins anew.</summary>
    private static void Pry(CreaturesModule m, Creature c, Spec.Prowl prowl, int cell)
    {
        if (c.Recall(PryKey) != cell + 1)
        {
            c.Keep(PryKey, cell + 1);
            c.Keep(PrySince, m.Clock);
            return;
        }
        if (m.Clock - c.Recall(PrySince) < prowl.Pry) return;
        m.Open(cell);
        Forget(c);
    }

    /// <summary>No doorway pried: one met again begins anew.</summary>
    private static void Forget(Creature c)
    {
        if (c.Recall(PryKey) == 0) return;
        c.Keep(PryKey, 0);
        c.Keep(PrySince, 0);
    }
}
