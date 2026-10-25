using Stillwake.Sim;

namespace Stillwake.Game.Features.Crew;

/// <summary>Where a hero sitting in a drop pod is held (Landing's seats): its feet on the pod's floor, in the middle of it.</summary>
public static class Seating
{
    /// <summary>
    /// The cell <see cref="Hero.At"/> puts hero <paramref name="peer"/> at while it sits in a drop pod, the middle of the pod's
    /// bottom; null for a hero standing, or whose pod is gone.
    /// </summary>
    public static int? SeatOf(Registry m, int peer)
    {
        if (m.Landing.SeatOf(peer) is not int pod || m.Grid.MachineById(pod) is not { } machine) return null;
        var spec = Content.Machines.Get(machine.Kind);
        int width = m.Grid.Width, x = machine.Cell % width, y = machine.Cell / width;
        return (y + spec.Height - 1) * width + x + spec.Width / 2 - 1;
    }
}
