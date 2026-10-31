using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// Keeping a post's arena (the warden): asleep, waking, waiting, its pounce, stuck, and waiting again. A guard waits while a sibling
/// crouches or flies; a stuck one does not hold it up, so one's window is the other's crouch. In id order, the first of two free goes.
/// </summary>
internal sealed class WardDrive : DriveRule
{
    /// <summary>Its steps.</summary>
    internal const int Asleep = 0, Waking = 1, Waiting = 2, Stuck = 3;

    /// <summary>Kept in its <c>Mem</c>: the creatures' clock it last knew of an awake hero in its arena, or took a blow.</summary>
    internal const string Heard = "heard";

    internal override Type Serves => typeof(Spec.Ward);

    internal override bool Wants(Creature c, Spec.Drive d) => true;

    internal override void Born(Creature c, Spec.Drive d) => (c.Step, c.DriveLeft) = (Asleep, 0);

    internal override bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        var ward = (Spec.Ward)d;
        if (c.Step == Asleep && m.HeroInArena(c)) Wake(m, c, ward);
        else if (c.Step == Asleep && AnySibling(m, c, s => s.Step != Asleep))
        {
            Wake(m, c, ward);
            // A tick behind the sibling that woke it, so the one struck goes first whatever their ids.
            c.DriveLeft += 1;
        }
        // Its clock starts on the floor, where a tool reaches it: it may have stopped at the roof.
        else if (c.Step == Stuck && !RuleBook.Body(spec.Body).Grounded(m, c, spec.Body)) c.DriveLeft = ward.Stuck;
        return true;
    }

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        if (c.Step is Waking or Stuck) c.Step = Waiting;
        else if (c.Step == Waiting && !AnySibling(m, c, s => s.Act != null))
        {
            var pounce = (Spec.Pounce)spec.Moves[0];
            if (PounceMove.Aim(m, c, pounce.Ticks * pounce.Speed / CreaturesModule.Unit) != null) RuleBook.Move(pounce).Begin(m, c, spec, 0);
        }
        return true;
    }

    /// <summary>A blow wakes it, and it means the striker, unless its aim is already taken.</summary>
    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit)
    {
        c.Keep(Heard, m.Clock);
        if (c.Step == Asleep) Wake(m, c, (Spec.Ward)d);
        if (c.Act == null) c.Target = Ref.Hero(hit.Peer);
        return true;
    }

    /// <summary>Its flight over, by a wall or a hero or its time: it falls and lies where it lands, its grudge spent.</summary>
    internal override void Ended(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int way, bool hop)
    {
        (c.VX, c.VY, c.Target) = (0, 0, Ref.None);
        (c.Step, c.DriveLeft) = (Stuck, ((Spec.Ward)d).Stuck);
    }

    internal override Gait GaitOf(Creature c) => Gait.Still;

    /// <summary>
    /// Whether <paramref name="c"/>, woken, has calmed and goes: no awake hero in its arena and no blow on it or a sibling for its
    /// <c>Calm</c>. Siblings go on one tick, so they come back asleep together and the one left never wakes the other anew.
    /// </summary>
    internal static bool Calmed(CreaturesModule m, Creature c, Spec.Ward ward)
    {
        if (m.HeroInArena(c)) c.Keep(Heard, m.Clock);
        if (c.Step == Asleep) return false;
        int heard = c.Recall(Heard);
        foreach (var s in m.Creatures)
            if (IsSibling(c, s)) heard = Math.Max(heard, s.Recall(Heard));
        return m.Clock - heard >= ward.Calm;
    }

    private static void Wake(CreaturesModule m, Creature c, Spec.Ward ward)
    {
        (c.Step, c.DriveLeft) = (Waking, ward.Wake);
        c.Keep(Heard, m.Clock);
    }

    private static bool AnySibling(CreaturesModule m, Creature c, Func<Creature, bool> which)
    {
        foreach (var s in m.Creatures)
            if (IsSibling(c, s) && which(s)) return true;
        return false;
    }

    /// <summary>Whether <paramref name="other"/> is another guard tied to <paramref name="c"/>'s relic.</summary>
    private static bool IsSibling(Creature c, Creature other) => other != c && !other.Dead && other.Recall(CreaturesModule.RelicKey) == c.Recall(CreaturesModule.RelicKey);
}
