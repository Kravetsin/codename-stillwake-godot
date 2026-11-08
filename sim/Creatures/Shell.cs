using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// A shell (the geode snail, lurker-attacks): a reflex counting how long it stays open. An awake hero's reported cell within its
/// reach fills the count to its hold; else it counts down. Shut, at none, it turns every hit aside; open, every shot still: only
/// melee hurts it. It never takes charge.
/// </summary>
internal sealed class ShellDrive : DriveRule
{
    /// <summary>Kept in its <c>Mem</c>: the ticks it stays open; 0 shut.</summary>
    internal const string OpenKey = "open";

    internal override Type Serves => typeof(Spec.Shell);

    internal override void Reflex(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d)
    {
        var shell = (Spec.Shell)d;
        if (m.Nearest(c, awake: true) is ({ }, int far) && far <= shell.Reach) c.Keep(OpenKey, shell.Hold);
        else if (Open(c)) c.Keep(OpenKey, c.Recall(OpenKey) - 1);
    }

    /// <summary>Whether <paramref name="c"/>'s shell is open.</summary>
    internal static bool Open(Creature c) => c.Recall(OpenKey) > 0;

    internal override bool Shields(Creature c, Spec.Drive d, in Hit hit) => hit.Shot || !Open(c);

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => false;

    internal override Gait GaitOf(Creature c) => Gait.Still;
}
