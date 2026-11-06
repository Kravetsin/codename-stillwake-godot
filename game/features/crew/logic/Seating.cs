using Stillwake.Sim;
using Stillwake.Sim.Landing;

namespace Stillwake.Game.Features.Crew;

/// <summary>Where a hero sitting in a drop pod or a treadmill wheel, or flying in a pod, is held (Landing's seats and flights): its feet on the seat's floor, in the middle of it.</summary>
public static class Seating
{
    /// <summary>
    /// The cell <see cref="Hero.At"/> puts hero <paramref name="peer"/> at while it sits in a seat, the middle of the seat's
    /// bottom, or while its pod flies, where the pod stood on this surface; null for a hero standing, or whose pod is gone.
    /// </summary>
    public static int? SeatOf(Registry m, int peer)
    {
        if (m.Landing.FlightOf(peer) is { } flight) return flight.Floor;
        if (m.Landing.SeatOf(peer) is not int pod || m.Grid.MachineById(pod) is not { } machine) return null;
        return LandingModule.FloorOf(machine.Kind, machine.Cell, m.Grid.Width);
    }
}
