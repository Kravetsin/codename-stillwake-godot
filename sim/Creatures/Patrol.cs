using static Stillwake.Sim.Creatures.CreaturesModule;
using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Hunting the crew on its deck (derelict-enemies), the escort's once awake: at its quarry (<see cref="CreaturesModule.Quarry"/>), bound to
/// its dock's deck, never jumping. Its quarry on its deck within its strike, it lunges (its first move), dropping a shot it was charging.
/// Else it walks: toward its quarry on its deck; off it, to the nearest door of its deck (the lower column at a tie) and on that way,
/// turning back at a wall; and on its way, its quarry within its notice and in line with its gun, on any deck, it shoots (its second, made
/// on the move: <see cref="Afoot"/>) whenever that is ready. A shut door on its way it pries as a spider does
/// (<see cref="ProwlDrive.Pry"/>), never a hatch, which lies under its feet (an open one it walks over) or over its head, nor the way in.
/// With no hero aboard it stands.
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
        // No hero aboard, or no dock of this vessel (a save of another build) to keep its deck by: it stands.
        if (m.QuarryOf(c) is not var (peer, q) || m.DeckFloor(c) is not int floor)
        {
            c.VX = 0;
            ProwlDrive.Forget(c);
            return true;
        }
        bool level = m.Level(q.Y) == m.Level(floor - 1);
        // Its claw first: a lunge begun takes the place of a shot it was charging, so it never fires while it claws.
        if (level && Math.Abs(q.X - Down(c.X, Unit)) <= patrol.Strike && Lunge(m, c, spec)) return true;
        int far = Math.Max(Math.Abs(q.X - Down(c.X, Unit)), Math.Abs(q.Y - feet));
        if (c.Act == null && spec.Moves.Count > 1 && RuleBook.Move(spec.Moves[1]).Ready(m, c, spec.Moves[1]) && far <= patrol.Notice && Sees(m, c, spec, q))
        {
            // Locked on the hero it saw: the shot's aim is taken at him (ShootMove's grudge), never at another nearer behind a floor.
            c.Target = Ref.Hero(peer);
            RuleBook.Move(spec.Moves[1]).Begin(m, c, spec, 1);
        }
        Walk(m, c, w, patrol, q, level, floor, top, feet);
        return true;
    }

    /// <summary>Its lunge, its first move, begun where it stands, if it is ready: whether it was.</summary>
    private static bool Lunge(CreaturesModule m, Creature c, Spec.Species spec)
    {
        if (!RuleBook.Move(spec.Moves[0]).Ready(m, c, spec.Moves[0])) return false;
        ProwlDrive.Forget(c);
        c.VX = 0;
        RuleBook.Move(spec.Moves[0]).Begin(m, c, spec, 0);
        return true;
    }

    /// <summary>Its shot is made on the move: it walks on, steering, while it charges and fires.</summary>
    internal override bool Afoot(Spec.Move mv) => mv is Spec.Shoot;

    /// <summary>
    /// Whether nothing solid lies between <paramref name="c"/>'s muzzle (<see cref="CreaturesModule.MuzzleY"/>, its middle with no shot) and
    /// the middle of cell <paramref name="at"/>, a hero's reported cell, its body's middle's: where its gauss flies.
    /// </summary>
    internal static bool Sees(CreaturesModule m, Creature c, Spec.Species spec, (int X, int Y) at)
    {
        int y = c.Y - spec.Body.Height * Unit / 2;
        foreach (var mv in spec.Moves)
            if (mv is Spec.Shoot shoot) y = MuzzleY(c, spec, shoot);
        return m.Sees(c.X, y, at);
    }

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
        if (WalkerBody.Blocked(m, c, w, way, w.Run, spans: true) || !m.InHull(w, c.X + way * w.Run, c.Y))
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
    internal override void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop) =>
        (c.VX, c.VY, c.Target) = (0, 0, Ref.None);

    /// <summary>An open hatch in its deck's floor is floor to it: its feet span the hatch's two cells, and it never goes down one.</summary>
    internal override bool Spans(CreaturesModule m, Creature c) => true;

    internal override Gait GaitOf(Creature c) => Gait.Steered;

    internal override Posture PostureOf(Creature c) => c.VX != 0 ? Posture.Wander : Posture.Idle;
}
