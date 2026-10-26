using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The pounce (the sentry): crouched its wind-up where it clings, then a flight straight at its target — its grudge if within
/// the flight's run, else the hero nearest within it — at its speed on the longer axis, the other its share, rounded toward
/// none; never cut short by a blow. No hero within the run, it lets be. However it ends, the drive that began it is told.
/// </summary>
internal sealed class PounceMove : MoveRule
{
    internal override Type Serves => typeof(Spec.Pounce);

    internal override bool Interruptible => false;

    internal override void Begin(CreaturesModule m, Creature c, Spec.Species spec, int at)
    {
        c.Act = new Act { Move = at, Phase = ActPhase.WindUp, Left = ((Spec.Pounce)spec.Moves[at]).WindUp };
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
        var pounce = (Spec.Pounce)mv;
        int run = pounce.Ticks * pounce.Speed / CreaturesModule.Unit;
        if (Aim(m, c, run) is not var (hx, hy))
        {
            m.EndAct(c, spec, 0, hop: false);
            return;
        }
        // From its middle to the middle of the hero's reported cell.
        int dx = m.Middle(hx) - c.X, dy = m.Middle(hy) - (c.Y - spec.Body.Height * CreaturesModule.Unit / 2);
        int far = Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (far == 0)
        {
            m.EndAct(c, spec, 0, hop: false);
            return;
        }
        c.Keep(ClingerBody.Mounted, 0);
        (act.Phase, act.Left, c.VX, c.VY) = (ActPhase.Active, pounce.Ticks, pounce.Speed * dx / far, pounce.Speed * dy / far);
    }

    /// <summary>The cell <paramref name="c"/> pounces at: its grudge\x27s hero within <paramref name="run"/> cells, else the nearest within it; null for none.</summary>
    private static (int X, int Y)? Aim(CreaturesModule m, Creature c, int run)
    {
        int x = CreaturesModule.Down(c.X, CreaturesModule.Unit), y = CreaturesModule.Down(c.Y - 1, CreaturesModule.Unit);
        if (c.Target is { Kind: RefKind.Hero } target && m.HeroAt(target.Id) is { } grudge && Math.Max(Math.Abs(grudge.X - x), Math.Abs(grudge.Y - y)) <= run)
            return grudge;
        return m.Nearest(c) is ({ } hero, int far) && far <= run ? hero : null;
    }

    internal override void Steer(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv) { }

    internal override bool Hits(Act a) => a.Phase == ActPhase.Active;

    internal override bool Walks(Act a) => false;
}
