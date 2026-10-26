using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Guarding a wreck (the sentry): watching; a hero within its notice alerts it, and alerted its notice through it pounces;
/// a hero gone from its notice calms it. Struck, or its wreck struck since it last looked, it turns on the striker and crouches
/// at once. Its pounce over, <see cref="ReturnDrive"/>, above it, wants the creature till it is clamped on again.
/// </summary>
internal sealed class GuardDrive : DriveRule
{
    private const int Watching = 0, Alerted = 1;

    /// <summary>Kept in its <c>Mem</c>: the debris clock of the last blow on its wreck it answered, plus one.</summary>
    internal const string Answered = "answered";

    internal override Type Serves => typeof(Spec.Guard);

    internal override bool Wants(Creature c, Spec.Drive d) => true;

    internal override bool ClaimsHits => true;

    internal override void Born(Creature c, Spec.Drive d) => (c.Step, c.DriveLeft) = (Watching, 0);

    internal override void Enter(CreaturesModule m, Creature c, Spec.Drive d) => (c.Step, c.DriveLeft) = (Watching, 0);

    internal override bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        var guard = (Spec.Guard)d;
        if (m.Debris.PieceById(c.Recall(ClingerBody.Piece))?.Struck is { } blow && blow.At + 1 > c.Recall(Answered))
        {
            c.Keep(Answered, blow.At + 1);
            Pounce(m, c, spec, Ref.Hero(blow.By));
            return true;
        }
        bool near = m.Nearest(c) is ({ }, int far) && far <= guard.Notice;
        if (c.Step == Watching && near) (c.Step, c.DriveLeft) = (Alerted, guard.AlertTicks);
        else if (c.Step == Alerted && !near) (c.Step, c.DriveLeft) = (Watching, 0);
        return true;
    }

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        if (c.Step == Alerted) Pounce(m, c, spec, Ref.None);
        return true;
    }

    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit)
    {
        // Mid-pounce it flies on (a pounce is never cut short), its aim already taken.
        if (c.Act != null) return true;
        m.Claim(c, at, 0);
        Pounce(m, c, spec, Ref.Hero(hit.Peer));
        return true;
    }

    internal override Gait GaitOf(Creature c) => Gait.Still;

    private static void Pounce(CreaturesModule m, Creature c, Spec.Species spec, Ref target)
    {
        (c.Step, c.DriveLeft, c.Target) = (Watching, 0, target);
        RuleBook.Move(spec.Moves[0]).Begin(m, c, spec, 0);
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
        (c.VX, c.VY) = (speed * dx / far, speed * dy / far);
        return true;
    }

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => true;

    internal override Gait GaitOf(Creature c) => Gait.Run;
}

/// <summary>A reflex: a harpoon's line on its wreck is let go the tick it is seen, whatever it is doing (0044).</summary>
internal sealed class CutTetherDrive : DriveRule
{
    internal override Type Serves => typeof(Spec.CutTether);

    internal override void Reflex(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        if (m.Debris.PieceById(c.Recall(ClingerBody.Piece)) is { Reel: not null } piece) m.DebrisOps.Unreel(piece.Id);
    }

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => false;

    internal override Gait GaitOf(Creature c) => Gait.Still;
}
