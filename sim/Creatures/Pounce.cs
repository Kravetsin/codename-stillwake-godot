using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The pounce (the sentry): crouched its wind-up where it clings, then a flight straight at its target — its grudge if within
/// the flight's run, else the hero nearest within it — at its speed on the longer axis, the other its share, rounded toward
/// none; never cut short by a blow unless cuttable. No hero within the run, it lets be. However it ends but cut, the drive that
/// began it is told.
/// </summary>
internal sealed class PounceMove : MoveRule
{
    internal override Type Serves => typeof(Spec.Pounce);

    internal override bool Interruptible(Spec.Move mv) => ((Spec.Pounce)mv).Cuttable;

    internal override void Begin(CreaturesModule m, Creature c, Spec.Species spec, int at)
    {
        c.Act = new Act { Move = at, Phase = ActPhase.WindUp, Left = ((Spec.Pounce)spec.Moves[at]).WindUp };
        m.Slow(c, spec);
    }

    internal override void Count(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv, bool grounded)
    {
        var act = c.Act!;
        if (act.Phase == ActPhase.WindUp && m.Drifts(spec)) m.Slow(c, spec);
        if (act.Left > 0) act.Left -= 1;
        if (act.Left > 0) return;
        if (act.Phase == ActPhase.Active)
        {
            m.EndAct(c, spec, 0, hop: false);
            return;
        }
        var pounce = (Spec.Pounce)mv;
        int run = pounce.Ticks * pounce.Speed / CreaturesModule.Unit;
        if ((pounce.AtTarget ? AimAtTarget(m, c, run) : Aim(m, c, run)) is not var (hx, hy))
        {
            m.EndAct(c, spec, 0, hop: false);
            return;
        }
        // From its middle to the middle of the hero's reported cell, the one its body's middle is in (Hero.BodyCell).
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

    /// <summary>The cell <paramref name="c"/> pounces at: its grudge's hero within <paramref name="run"/> cells, else the nearest within it; null for none.</summary>
    internal static (int X, int Y)? Aim(CreaturesModule m, Creature c, int run)
    {
        int x = CreaturesModule.Down(c.X, CreaturesModule.Unit), y = CreaturesModule.Down(c.Y - 1, CreaturesModule.Unit);
        if (c.Target is { Kind: RefKind.Hero } target && m.HeroAt(target.Id, awake: true) is { } grudge && Math.Max(Math.Abs(grudge.X - x), Math.Abs(grudge.Y - y)) <= run)
            return grudge;
        return m.Nearest(c, awake: true) is ({ } hero, int far) && far <= run ? hero : null;
    }

    /// <summary>The cell of <paramref name="c"/>'s target alone, out in space within <paramref name="run"/> cells; null otherwise.</summary>
    private static (int X, int Y)? AimAtTarget(CreaturesModule m, Creature c, int run) =>
        c.Target is { Kind: RefKind.Hero } target && m.OutAt(target.Id) is { } at
        && Math.Max(Math.Abs(at.X - CreaturesModule.Down(c.X, CreaturesModule.Unit)), Math.Abs(at.Y - CreaturesModule.Down(c.Y - 1, CreaturesModule.Unit))) <= run
            ? at : null;

    internal override void Steer(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv) { }

    internal override bool Hits(Act a) => a.Phase == ActPhase.Active;

    internal override bool Walks(Act a) => false;

    internal override bool Flies(Spec.Species spec, Act a) => spec.Body is Spec.Walker && a.Phase == ActPhase.Active;
}
