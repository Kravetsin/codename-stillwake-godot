using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The lunge, a ram along the ground (0103): crouched its wind-up, counted once it stands, then charging the hero nearest as
/// the wind-up ends, never leaping, till it is past it, at a wall or a drop, or out of time. A hero farther than its run, or
/// none, is let be. However it ends, the drive that began it is told which way the creature goes on.
/// </summary>
internal sealed class LungeMove : MoveRule
{
    internal override Type Serves => typeof(Spec.Lunge);

    internal override void Begin(CreaturesModule m, Creature c, Spec.Species spec, int at)
    {
        c.Act = new Act { Move = at, Phase = ActPhase.WindUp, Left = ((Spec.Lunge)spec.Moves[at]).WindUp };
        c.VX = 0;
    }

    internal override void Count(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv, bool grounded)
    {
        var act = c.Act!;
        // A wind-up begun in the air, a retreat's knock still flying, counts from the ground: its crouch is seen whole.
        if (act.Left > 0 && !(act.Phase == ActPhase.WindUp && !grounded)) act.Left -= 1;
        if (act.Left > 0) return;
        var lunge = (Spec.Lunge)mv;
        if (act.Phase == ActPhase.Active)
        {
            m.EndAct(c, spec, m.FleeWay(c), hop: false);
            return;
        }
        // At the hero nearest now, by its reported cell: none, level with it or past a charge's run, nothing to charge at.
        var (target, far) = m.Nearest(c);
        int at = target is { } hero && far <= lunge.Ticks * lunge.Speed / CreaturesModule.Unit ? Math.Sign(m.Middle(hero.X) - c.X) : 0;
        if (at == 0) m.EndAct(c, spec, m.FleeWay(c), hop: false);
        else (act.Phase, act.Left, c.VX) = (ActPhase.Active, lunge.Ticks, at * lunge.Speed);
    }

    internal override void Steer(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv)
    {
        if (c.Act!.Phase == ActPhase.WindUp)
        {
            c.VX = 0;
            return;
        }
        // A wall it cannot step up or a drop ends the charge, never leaped: it turns from it, as a walker turns. Past the hero
        // it charged by its run past it runs on.
        var lunge = (Spec.Lunge)mv;
        int way = c.VX >= 0 ? 1 : -1;
        if (WalkerBody.Blocked(m, c, (Spec.Walker)spec.Body, way, lunge.Speed)) m.EndAct(c, spec, -way, hop: false);
        else if (m.Nearest(c).Hero is { } hero && (c.X - m.Middle(hero.X)) * way >= lunge.Past * CreaturesModule.Unit) m.EndAct(c, spec, way, hop: false);
        else c.VX = way * lunge.Speed;
    }

    internal override bool Hits(Act a) => a.Phase == ActPhase.Active;

    internal override bool Walks(Act a) => a.Phase == ActPhase.Active;
}
