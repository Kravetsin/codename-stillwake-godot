using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Guarding a wreck (the sentry): watching, crawling along its perch; a hero within its notice alerts it, and alerted its
/// notice through it pounces at one within its strike, or shoots at one farther (<see cref="ShootMove"/>); a hero gone from its
/// notice calms it. Struck, or its wreck struck since it last looked, it answers the striker at once the same way, by how far
/// the striker is. Its pounce over, <see cref="ReturnDrive"/>, above it, wants the creature till it is clamped on again.
/// A walker on guard (a cave lurker, cave-hostiles) stands still instead of crawling and notices only a hero in line with it.
/// </summary>
internal sealed class GuardDrive : DriveRule
{
    private const int Watching = 0;

    /// <summary>Its step while alerted.</summary>
    internal const int Alerted = 1;

    /// <summary>Kept in its <c>Mem</c>: the debris clock of the last blow on its wreck it answered, plus one.</summary>
    internal const string Answered = "answered";

    internal override Type Serves => typeof(Spec.Guard);

    internal override bool Wants(Creature c, Spec.Drive d) => true;

    internal override void Born(Creature c, Spec.Drive d) => (c.Step, c.DriveLeft) = (Watching, 0);

    internal override void Enter(CreaturesModule m, Creature c, Spec.Drive d) => (c.Step, c.DriveLeft) = (Watching, 0);

    internal override bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        var guard = (Spec.Guard)d;
        if (m.Debris.PieceById(c.Recall(ClingerBody.Piece))?.Struck is { } blow && blow.At + 1 > c.Recall(Answered))
        {
            Answer(m, c, spec, guard, Ref.Hero(blow.By), m.Debris.Clock);
            return true;
        }
        bool near = Within(m, c, spec, guard.Notice);
        if (c.Step == Watching && near) (c.Step, c.DriveLeft) = (Alerted, guard.AlertTicks);
        else if (c.Step == Alerted && !near) (c.Step, c.DriveLeft) = (Watching, 0);
        // A walker on guard (a cave lurker) stands still; a clinger crawls its perch.
        if (c.Step == Watching && spec.Body is Spec.Clinger && !CutTetherDrive.Cutting(c)) ClingerBody.Crawl(m, c, spec);
        return true;
    }

    /// <summary>Alerted long enough: it pounces at an awake hero within its strike, else shoots at one within its notice, and waits for one till then.</summary>
    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        if (c.Step != Alerted) return true;
        var guard = (Spec.Guard)d;
        bool strike = Within(m, c, spec, guard.Strike), sight = Within(m, c, spec, guard.Notice) && ShotOf(spec) >= 0;
        if ((strike || sight) && MoveFor(m, c, spec, guard, Ref.None) is int move) Answer(m, c, spec, guard, Ref.None, m.Debris.Clock, move);
        else c.DriveLeft = 1;
        return true;
    }

    /// <summary>
    /// Whether the nearest awake hero is within <paramref name="cells"/>; for a walker, in line too (<see cref="CreaturesModule.InLine"/>):
    /// its move stops at rock, so it waits for one it can reach. A clinger's flight passes walls, so it senses through them.
    /// </summary>
    private static bool Within(CreaturesModule m, Creature c, Spec.Species spec, int cells) =>
        m.Nearest(c, awake: true) is ({ } hero, int far) && far <= cells && (spec.Body is not Spec.Walker || m.InLine(c, spec, hero));

    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit)
    {
        // Mid-move it goes on (neither a pounce nor a shot is cut short), its aim already taken.
        if (c.Act != null) return true;
        m.Claim(c, at, 0);
        Answer(m, c, spec, (Spec.Guard)d, Ref.Hero(hit.Peer), hit.Answered);
        return true;
    }

    /// <summary>Its move over: it hangs where it stopped; one that never left, or a walker, lets its grudge go; one away flies home first.</summary>
    internal override void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop)
    {
        (c.VX, c.VY) = (0, 0);
        if (c.Recall(ClingerBody.Mounted) == 1 || spec.Body is Spec.Walker) c.Target = Ref.None;
    }

    internal override Gait GaitOf(Creature c) => Gait.Still;

    /// <summary>
    /// A move begun at <paramref name="target"/> (none: the nearest awake hero): a pounce at one within its strike, or with no shot
    /// of its own, or off its perch (flying home, a shot would hang it in the air); else its shot. Every blow on its wreck up to
    /// this tick answered with it, so one swing that hits the sentry and its wreck answers once; a blow while it is away is
    /// answered once it is home. A blow is stamped with the debris clock of the commands it came with, and the debris clock moves
    /// on as the simulation runs: from a hit (commands) <paramref name="answered"/> is the clock and one, from the system the clock.
    /// </summary>
    private static void Answer(CreaturesModule m, Creature c, Spec.Species spec, Spec.Guard guard, Ref target, int answered) =>
        Answer(m, c, spec, guard, target, answered, MoveFor(m, c, spec, guard, target));

    /// <summary>
    /// The answer with its move chosen, or none: the blows up to now answered all the same, and one that answers provoked
    /// (<see cref="Spec.Guard.Provokes"/>).
    /// </summary>
    private static void Answer(CreaturesModule m, Creature c, Spec.Species spec, Spec.Guard guard, Ref target, int answered, int? move)
    {
        c.Keep(Answered, answered);
        (c.Step, c.DriveLeft, c.Target) = (Watching, 0, move == null ? Ref.None : target);
        if (guard.Provokes > 0) c.Keep(ProvokedKey, m.Clock + guard.Provokes);
        if (move is int at) RuleBook.Move(spec.Moves[at]).Begin(m, c, spec, at);
    }

    /// <summary>
    /// The move <paramref name="c"/> makes at <paramref name="target"/> (none: the nearest awake hero), by how far: its first within its
    /// strike, once ready; farther its shot, from its perch, not nearer than the shot's least, and while provoked if it must be; else a
    /// clinger pounces as before (off its perch, flying home, a shot would hang it in the air) and a walker makes none. None with no hero.
    /// </summary>
    private static int? MoveFor(CreaturesModule m, Creature c, Spec.Species spec, Spec.Guard guard, Ref target)
    {
        if (DistanceTo(m, c, target) is not int far) return null;
        if (far <= guard.Strike) return RuleBook.Move(spec.Moves[0]).Ready(m, c, spec.Moves[0]) ? 0 : null;
        int shot = ShotOf(spec);
        bool clinger = spec.Body is Spec.Clinger, perched = !clinger || c.Recall(ClingerBody.Mounted) == 1;
        if (shot >= 0 && perched && (guard.Provokes == 0 || Provoked(m, c)) && far >= ((Spec.Shoot)spec.Moves[shot]).From) return shot;
        return clinger ? 0 : null;
    }

    /// <summary>Kept in its <c>Mem</c>: the creatures' clock its provocation lasts till.</summary>
    internal const string ProvokedKey = "provoked";

    /// <summary>Whether <paramref name="c"/> is provoked: it answered a hero lately (<see cref="Spec.Guard.Provokes"/>).</summary>
    internal static bool Provoked(CreaturesModule m, Creature c) => c.Recall(ProvokedKey) > m.Clock;

    /// <summary>How far <paramref name="target"/>'s hero is from <paramref name="c"/> by box distance in cells, or the nearest awake one with none; null with none on the grid.</summary>
    private static int? DistanceTo(CreaturesModule m, Creature c, Ref target)
    {
        if (target.Kind == RefKind.Hero && m.HeroAt(target.Id) is { } at)
            return Math.Max(Math.Abs(at.X - CreaturesModule.Down(c.X, CreaturesModule.Unit)), Math.Abs(at.Y - CreaturesModule.Down(c.Y - 1, CreaturesModule.Unit)));
        return m.Nearest(c, awake: true) is ({ }, int far) ? far : null;
    }

    /// <summary>The place among <paramref name="spec"/>'s moves of its shot; -1 for none.</summary>
    private static int ShotOf(Spec.Species spec)
    {
        for (int i = 0; i < spec.Moves.Count; i++)
            if (spec.Moves[i] is Spec.Shoot) return i;
        return -1;
    }
}

/// <summary>Flying back to where it clamps on, straight, at its speed; clamped on, its grudge is spent and it yields.</summary>
internal sealed class ReturnDrive : DriveRule
{
    internal override Type Serves => typeof(Spec.Return);

    internal override bool Wants(Creature c, Spec.Drive d) => c.Recall(ClingerBody.Mounted) != 1;

    internal override void Enter(CreaturesModule m, Creature c, Spec.Drive d) => (c.Step, c.DriveLeft) = (0, 0);

    internal override bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        if (c.Recall(ClingerBody.Mounted) == 1)
        {
            c.Target = Ref.None;
            return false;
        }
        int speed = ((Spec.Return)d).Speed;
        var (x, y) = ClingerBody.Hold(m, c);
        int dx = x - c.X, dy = y - c.Y, far = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (far <= speed)
        {
            (c.X, c.Y, c.VX, c.VY) = (x, y, 0, 0);
            c.Keep(ClingerBody.Mounted, 1);
            return true;
        }
        // Its speed on the longer axis, the other its share, rounded toward none: a diagonal goes up to 41% faster.
        (c.VX, c.VY) = ((int)((long)speed * dx / far), (int)((long)speed * dy / far));
        return true;
    }

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => true;

    internal override Gait GaitOf(Creature c) => Gait.Run;
}

/// <summary>
/// A reflex: a harpoon's line on its wreck is let go <c>Ticks</c> after it is seen, whatever it is doing (0044), if it is on still:
/// long enough for its claws to be seen at the line.
/// </summary>
internal sealed class CutTetherDrive : DriveRule
{
    /// <summary>Kept in its <c>Mem</c>: the creatures' clock of its last cut, plus one.</summary>
    internal const string Cut = "cut";

    /// <summary>Kept in its <c>Mem</c>: the creatures' clock it cuts the line on its wreck at; 0 while there is none.</summary>
    internal const string CutsAt = "cutsAt";

    internal override Type Serves => typeof(Spec.CutTether);

    internal override void Reflex(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        if (m.Debris.PieceById(c.Recall(ClingerBody.Piece)) is not { Reel: not null } piece)
        {
            c.Keep(CutsAt, 0);
            return;
        }
        if (c.Recall(CutsAt) == 0) c.Keep(CutsAt, m.Clock + ((Spec.CutTether)d).Ticks);
        if (m.Clock < c.Recall(CutsAt)) return;
        m.DebrisOps.Unreel(piece.Id);
        c.Keep(CutsAt, 0);
        // When it cut: what the client shows a snap by, as the line is gone within the tick.
        c.Keep(Cut, m.Clock + 1);
    }

    /// <summary>Whether <paramref name="c"/> is at a line on its wreck, cutting it.</summary>
    internal static bool Cutting(Creature c) => c.Recall(CutsAt) != 0;

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => false;

    internal override Gait GaitOf(Creature c) => Gait.Still;
}
