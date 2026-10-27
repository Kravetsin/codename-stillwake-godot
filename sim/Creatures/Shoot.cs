using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The shot (the sentry's bolt, shots-are-the-sims): crouched its wind-up where it clings, its eye flickering; then its
/// projectile leaves its middle at its target's reported cell as it is then — its grudge if within reach, else the nearest awake
/// hero within it — at its speed on the longer axis, the other its share, rounded toward none; none within reach, no shot. Then
/// it recovers, still clamped; never cut short by a blow. However it ends, the drive that began it is told.
/// </summary>
internal sealed class ShootMove : MoveRule
{
    internal override Type Serves => typeof(Spec.Shoot);

    internal override bool Interruptible => false;

    internal override void Begin(CreaturesModule m, Creature c, Spec.Species spec, int at)
    {
        c.Act = new Act { Move = at, Phase = ActPhase.WindUp, Left = ((Spec.Shoot)spec.Moves[at]).WindUp };
        (c.VX, c.VY) = (0, 0);
    }

    internal override void Count(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv, bool grounded)
    {
        var act = c.Act!;
        if (act.Left > 0) act.Left -= 1;
        if (act.Left > 0) return;
        if (act.Phase == ActPhase.Active)
        {
            m.EndAct(c, spec, 0, hop: false);
            return;
        }
        var shoot = (Spec.Shoot)mv;
        if (PounceMove.Aim(m, c, shoot.Reach) is not var (hx, hy) || !m.Launch(c, spec, shoot, hx, hy))
        {
            m.EndAct(c, spec, 0, hop: false);
            return;
        }
        (act.Phase, act.Left) = (ActPhase.Active, shoot.Recover);
    }

    internal override void Steer(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv) { }

    internal override bool Hits(Act a) => false;

    internal override bool Walks(Act a) => false;

    /// <summary>Crouched for its shot while it winds up; after, it rests.</summary>
    internal override Posture PostureOf(Act a) => a.Phase == ActPhase.WindUp ? Posture.WindUp : Posture.Idle;
}
