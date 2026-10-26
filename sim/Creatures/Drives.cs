using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>Grazing: idling and wandering by turns, each a drawn length; what a creature does when nothing else wants it.</summary>
internal sealed class GrazeDrive : DriveRule
{
    private const int Idling = 0, Wandering = 1;

    internal override Type Serves => typeof(Spec.Graze);

    internal override bool Wants(Creature c, Spec.Drive d) => true;

    // Only an idle or a wander begun draws: its length, and a wander its way first.
    internal override void Enter(CreaturesModule m, Creature c, Spec.Drive d) => Idle(m, c);

    internal override void Born(Creature c, Spec.Drive d) => (c.Step, c.DriveLeft) = (Idling, Spec.IdleLeast);

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        if (c.Step == Wandering)
        {
            Idle(m, c);
            return true;
        }
        int way = m.Rand.Below(2) == 0 ? 1 : -1;
        c.Step = Wandering;
        c.DriveLeft = Spec.WanderLeast + (int)m.Rand.Below(Spec.WanderMost - Spec.WanderLeast + 1);
        c.VX = way * ((Spec.Walker)spec.Body).Walk;
        return true;
    }

    private static void Idle(CreaturesModule m, Creature c)
    {
        c.Step = Idling;
        c.DriveLeft = Spec.IdleLeast + (int)m.Rand.Below(Spec.IdleMost - Spec.IdleLeast + 1);
    }

    internal override Gait GaitOf(Creature c) => c.Step == Wandering ? Gait.Walk : Gait.Still;

    internal override Posture PostureOf(Creature c) => c.Step == Wandering ? Posture.Wander : Posture.Idle;
}

/// <summary>
/// Fleeing what struck it: it claims every hit, bolting the knock's way at its run or the knock's speed if more; cornered with
/// a hero near on the side it would turn to, it may turn on it with its first move instead (0103).
/// </summary>
internal sealed class FleeDrive : DriveRule
{
    internal override Type Serves => typeof(Spec.Flee);

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => false;

    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit)
    {
        m.Claim(c, at, ((Spec.Flee)d).Ticks);
        m.Bolt(c, spec, hit.Away);
        return true;
    }

    internal override bool Blocked(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way)
    {
        var flee = (Spec.Flee)d;
        if (flee.CorneredChance == 0 || !m.Cornered(c, way, flee.CorneredReach) || m.Rand.Below(1000) >= (uint)flee.CorneredChance) return false;
        RuleBook.Move(spec.Moves[0]).Begin(m, c, spec, 0);
        return true;
    }

    internal override void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop) => m.Escape(c, spec, way, hop);

    internal override Gait GaitOf(Creature c) => Gait.Run;

    internal override Posture PostureOf(Creature c) => Posture.Flee;
}

/// <summary>
/// Retaliating (0103): struck by a hero near it, it mostly flees a moment, then turns back on it with its first move: at once
/// when it is cornered first.
/// </summary>
internal sealed class RetaliateDrive : DriveRule
{
    internal override Type Serves => typeof(Spec.Retaliate);

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        RuleBook.Move(spec.Moves[0]).Begin(m, c, spec, 0);
        return true;
    }

    // Two draws: whether it turns back, then how long it flees first.
    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit)
    {
        var r = (Spec.Retaliate)d;
        if (!m.Near(c, m.HeroCell(hit.Peer), r.Reach) || m.Rand.Below(1000) >= (uint)r.Chance) return false;
        m.Claim(c, at, r.FleeLeast + (int)m.Rand.Below((uint)(r.FleeMost - r.FleeLeast + 1)));
        m.Bolt(c, spec, hit.Away);
        return true;
    }

    internal override bool Blocked(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way)
    {
        RuleBook.Move(spec.Moves[0]).Begin(m, c, spec, 0);
        return true;
    }

    internal override void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop) => m.Escape(c, spec, way, hop);

    internal override Gait GaitOf(Creature c) => Gait.Run;

    internal override Posture PostureOf(Creature c) => Posture.Retreat;
}

/// <summary>Escaping after its move, hit or missed: a flight that never turns back, till it calms or is struck again (0103).</summary>
internal sealed class EscapeDrive : DriveRule
{
    internal override Type Serves => typeof(Spec.Escape);

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => false;

    internal override Gait GaitOf(Creature c) => Gait.Run;

    internal override Posture PostureOf(Creature c) => Posture.Escape;
}
