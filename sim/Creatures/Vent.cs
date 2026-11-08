using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// A vent (the capback's gas, lurker-attacks): no wind-up, its patch laid round its middle the tick it begins, then its ticks spent
/// venting, still; never cut short by a blow. Not begun again till its cooldown has passed since the last began.
/// </summary>
internal sealed class VentMove : MoveRule
{
    /// <summary>Kept in its <c>Mem</c>: the creatures' clock it last vented on, plus one; 0 for never.</summary>
    internal const string Vented = "vented";

    internal override Type Serves => typeof(Spec.Vent);

    internal override bool Interruptible(Spec.Move mv) => false;

    internal override bool Ready(CreaturesModule m, Creature c, Spec.Move mv) =>
        c.Recall(Vented) == 0 || m.Clock - (c.Recall(Vented) - 1) >= ((Spec.Vent)mv).Cooldown;

    internal override void Begin(CreaturesModule m, Creature c, Spec.Species spec, int at)
    {
        var vent = (Spec.Vent)spec.Moves[at];
        c.Act = new Act { Move = at, Phase = ActPhase.Active, Left = vent.Ticks };
        c.Keep(Vented, m.Clock + 1);
        (c.VX, c.VY) = (0, 0);
        m.LayPatch(vent.Patch, CreaturesModule.Down(c.X, CreaturesModule.Unit), CreaturesModule.Down(c.Y - spec.Body.Height * CreaturesModule.Unit / 2, CreaturesModule.Unit), floor: false);
    }

    internal override void Count(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv, bool grounded)
    {
        var act = c.Act!;
        if (act.Left > 0) act.Left -= 1;
        if (act.Left == 0) m.EndAct(c, spec, 0, hop: false);
    }

    internal override void Steer(CreaturesModule m, Creature c, Spec.Species spec, Spec.Move mv) => c.VX = 0;

    internal override bool Hits(Act a) => false;

    internal override bool Walks(Act a) => false;
}
