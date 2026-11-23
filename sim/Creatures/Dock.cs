using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// A vessel's guard in its dock (derelict-enemies): still on its dock's floor, folded in its capsule or hung in its stand, never yielding
/// while asleep. An awake hero's reported cell in its dock's room, or a blow from anywhere, wakes one that wakes (<c>Wake</c> over 0): its
/// <c>Wake</c> ticks waking, then awake for good (<see cref="Woke"/>), the next drive of its brain in charge. A blow hurts it as any other.
/// </summary>
internal sealed class DockDrive : DriveRule
{
    /// <summary>Its steps.</summary>
    internal const int Asleep = 0, Waking = 1;

    /// <summary>Kept in its <c>Mem</c>: 1 once it has woken; it never sleeps again.</summary>
    internal const string Woke = "woke";

    internal override Type Serves => typeof(Spec.Dock);

    internal override bool Wants(Creature c, Spec.Drive d) => c.Recall(Woke) == 0;

    internal override void Born(Creature c, Spec.Drive d) => (c.Step, c.DriveLeft) = (Asleep, 0);

    internal override bool Run(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        // No dock of this vessel to sleep in (a save of another build): awake at once.
        if (!m.Docked(c))
        {
            c.Keep(Woke, 1);
            return false;
        }
        if (c.Step == Asleep && m.HeroInCabin(c)) Wake(c, (Spec.Dock)d);
        return true;
    }

    /// <summary>Its waking done: awake for good, it yields to the drive under it.</summary>
    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        if (c.Step != Waking) return true;
        c.Keep(Woke, 1);
        return false;
    }

    /// <summary>A blow wakes it, from anywhere, and is claimed: no answer while it wakes. Awake, the drive under it is told.</summary>
    internal override bool Hurt(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d, int at, in Hit hit)
    {
        if (c.Recall(Woke) != 0) return false;
        if (c.Step == Asleep) Wake(c, (Spec.Dock)d);
        return true;
    }

    internal override Gait GaitOf(Creature c) => Gait.Still;

    internal override Posture PostureOf(Creature c) => c.Step == Waking ? Posture.Waking : Posture.Docked;

    private static void Wake(Creature c, Spec.Dock dock)
    {
        if (dock.Wake > 0) (c.Step, c.DriveLeft) = (Waking, dock.Wake);
    }
}
