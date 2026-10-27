using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// The arc (the sentry's, 2026-10-27-sentry-arc): crouched its wind-up where it clings, its eye flickering; <c>Lock</c> ticks before
/// its end it takes its target's reported cell as it is then — its grudge wherever it is, else the nearest awake hero within its
/// reach — and holds it; none, and it strikes nothing. At the end its arc strikes that cell at once (<see
/// cref="CreaturesModule.StrikeArc"/>). Then it recovers, still clamped; never cut short by a blow. However it ends, the drive
/// that began it is told.
/// </summary>
internal sealed class ShootMove : MoveRule
{
    /// <summary>Kept in its <c>Mem</c>: the cell its arc will strike, plus one; 0 while it has none.</summary>
    internal const string Aim = "aim";

    internal override Type Serves => typeof(Spec.Shoot);

    internal override bool Interruptible => false;

    internal override void Begin(CreaturesModule m, Creature c, Spec.Species spec, int at)
    {
        c.Act = new Act { Move = at, Phase = ActPhase.WindUp, Left = ((Spec.Shoot)spec.Moves[at]).WindUp };
        c.Keep(Aim, 0);
        (c.VX, c.VY) = (0, 0);
    }

    internal override void Count(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv, bool grounded)
    {
        var act = c.Act!;
        var shoot = (Spec.Shoot)mv;
        if (act.Left > 0) act.Left -= 1;
        if (act.Phase == ActPhase.WindUp && act.Left == Math.Min(shoot.Lock, shoot.WindUp - 1))
        {
            if (AimOf(m, c, shoot.Reach) is not { } aim)
            {
                m.EndAct(c, spec, 0, hop: false);
                return;
            }
            c.Keep(Aim, m.CellOf(aim) + 1);
        }
        if (act.Left > 0) return;
        if (act.Phase == ActPhase.Active || !m.StrikeArc(c, spec, shoot, c.Recall(Aim) - 1))
        {
            c.Keep(Aim, 0);
            m.EndAct(c, spec, 0, hop: false);
            return;
        }
        c.Keep(Aim, 0);
        (act.Phase, act.Left) = (ActPhase.Active, shoot.Recover);
    }

    /// <summary>
    /// The cell <paramref name="c"/> takes aim at: its grudge's hero wherever it is, the arc falling short of one past its reach, so
    /// a blow from afar is answered at the striker and never at a friend nearer; else the nearest awake hero within
    /// <paramref name="reach"/>; null for none.
    /// </summary>
    private static (int X, int Y)? AimOf(CreaturesModule m, Creature c, int reach) =>
        c.Target is { Kind: RefKind.Hero } target && m.HeroAt(target.Id, awake: true) is { } grudge ? grudge
        : m.Nearest(c, awake: true) is ({ } hero, int far) && far <= reach ? hero : null;

    internal override void Steer(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv) { }

    internal override bool Hits(Act a) => false;

    internal override bool Walks(Act a) => false;

    /// <summary>Crouched for its arc while it winds up; after, it rests.</summary>
    internal override Posture PostureOf(Act a) => a.Phase == ActPhase.WindUp ? Posture.WindUp : Posture.Idle;
}
