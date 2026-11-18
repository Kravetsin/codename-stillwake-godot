using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Hunting the crew on its deck (derelict-enemies), the escort's once awake: at its quarry (<see cref="CreaturesModule.Quarry"/>), bound to
/// its dock's deck, never jumping. Its quarry on its deck within its strike, it lunges (its first move); within its notice and in line with
/// its middle, on any deck, it stands and shoots (its second). Else it walks: toward its quarry on its deck; off it, to the nearest door of
/// its deck (the lower column at a tie) and on that way, turning back at a wall. A shut door on its way it pries as a spider does
/// (<see cref="ProwlDrive.Pry"/>), never a hatch, which lies under its feet or over its head, nor the way in. With no hero aboard it stands.
/// </summary>
internal sealed class PatrolDrive : DriveRule
{
    /// <summary>Kept in its <c>Mem</c>: the way it walks its deck with its quarry off it, -1 or 1; 0 for none yet.</summary>
    internal const string WayKey = "way";

    internal override Type Serves => typeof(Spec.Patrol);

    internal override bool Wants(Creature c, Spec.Drive d) => true;

    internal override void Born(Creature c, Spec.Drive d) => (c.Step, c.DriveLeft) = (0, 0);

    internal override void Enter(CreaturesModule m, Creature c, Spec.Drive d) => (c.Step, c.DriveLeft) = (0, 0);

    internal override bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        var patrol = (Spec.Patrol)d;
        var w = (Spec.Walker)spec.Body;
        int half = w.Width * Unit / 2;
        int left = Down(c.X - half, Unit), right = Down(c.X + half - 1, Unit), top = Down(c.Y - w.Height * Unit, Unit), feet = Down(c.Y - 1, Unit);
        // A door shut on it (0006): it pries it where it stands.
        if (m.Overlaps(w, c.X, c.Y))
        {
            if (m.ShutDoorway(left, top, right, feet) is int shut) ProwlDrive.Pry(m, c, patrol.Pry, shut);
            c.VX = 0;
            return true;
        }
        if (m.Quarry(c) is not { } q)
        {
            c.VX = 0;
            ProwlDrive.Forget(c);
            return true;
        }
        int floor = m.DeckFloor(c);
        bool level = m.Level(q.Y) == m.Level(floor - 1);
        if (level && Math.Abs(q.X - Down(c.X, Unit)) <= patrol.Strike && Begin(m, c, spec, 0)) return true;
        int far = Math.Max(Math.Abs(q.X - Down(c.X, Unit)), Math.Abs(q.Y - feet));
        if (far <= patrol.Notice && Sees(m, c, spec, q) && Begin(m, c, spec, 1)) return true;
        Walk(m, c, w, patrol, q, level, floor, top, feet);
        return true;
    }

    /// <summary>Its move <paramref name="at"/> begun, if it has one there and it is ready: whether it did.</summary>
    private static bool Begin(CreaturesModule m, Creature c, Spec.Species spec, int at)
    {
        if (at >= spec.Moves.Count || !RuleBook.Move(spec.Moves[at]).Ready(m, c, spec.Moves[at])) return false;
        ProwlDrive.Forget(c);
        c.VX = 0;
        RuleBook.Move(spec.Moves[at]).Begin(m, c, spec, at);
        return true;
    }

    /// <summary>Whether nothing solid lies between <paramref name="c"/>'s middle and the middle of the hero standing in cell <paramref name="at"/>: where its gauss flies.</summary>
    internal static bool Sees(CreaturesModule m, Creature c, Spec.Species spec, (int X, int Y) at) =>
        m.FirstSolid(c.X, c.Y - spec.Body.Height * Unit / 2, m.Middle(at.X), (at.Y + 1) * Unit - Content.Hero.Height / 2) == null;

    /// <summary>A tick of its walk: toward its quarry on its deck, else its way along it; a shut door ahead pried; a wall, or the hull's end, turning it.</summary>
    private static void Walk(CreaturesModule m, Creature c, Spec.Walker w, Spec.Patrol patrol, (int X, int Y) q, bool level, int floor, int top, int feet)
    {
        int way;
        if (level)
        {
            c.Keep(WayKey, 0);
            int dx = m.Middle(q.X) - c.X;
            if (Math.Abs(dx) < Unit)
            {
                c.VX = 0;
                ProwlDrive.Forget(c);
                return;
            }
            way = Math.Sign(dx);
        }
        else
        {
            way = c.Recall(WayKey);
            if (way == 0) way = m.DoorOn(floor, c.X) is int door && door > c.X ? 1 : -1;
        }
        int column = WalkerBody.AheadOf(c, w, way);
        if (m.ShutDoorway(column, top, column, feet) is int shut)
        {
            c.VX = 0;
            ProwlDrive.Pry(m, c, patrol.Pry, shut);
            return;
        }
        ProwlDrive.Forget(c);
        if (WalkerBody.Blocked(m, c, w, way, w.Run) || !m.InHull(w, c.X + way * w.Run, c.Y))
        {
            c.VX = 0;
            if (!level) c.Keep(WayKey, -way);
            return;
        }
        if (!level) c.Keep(WayKey, way);
        c.VX = way * w.Run;
    }

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => true;

    /// <summary>Struck it hunts on: a blow is no answer's business, its quarry the nearest hero still.</summary>
    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit) => true;

    /// <summary>A move over, it stands till its next tick's steering.</summary>
    internal override void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop) => (c.VX, c.VY) = (0, 0);

    internal override Gait GaitOf(Creature c) => Gait.Steered;

    internal override Posture PostureOf(Creature c) => c.VX != 0 ? Posture.Wander : Posture.Idle;
}
