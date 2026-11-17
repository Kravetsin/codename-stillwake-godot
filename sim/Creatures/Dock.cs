using Spec = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Creatures;

/// <summary>
/// A vessel's guard asleep in its dock (derelict-enemies): still on its dock's floor, folded in its capsule or hung in its stand, always
/// wanted, never yielding. A blow hurts it as any other; nothing wakes it yet (the plan's G-2 and G-3).
/// </summary>
internal sealed class DockDrive : DriveRule
{
    internal override Type Serves => typeof(Spec.Dock);

    internal override bool Wants(Creature c, Spec.Drive d) => true;

    internal override bool Next(CreaturesModule m, Creature c, Spec.Species spec, Spec.Drive d) => true;

    internal override Gait GaitOf(Creature c) => Gait.Still;

    internal override Posture PostureOf(Creature c) => Posture.Docked;
}
