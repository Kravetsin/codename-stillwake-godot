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

    /// <summary>The kind of seat hero <paramref name="peer"/> sits in; null for a hero not seated, or whose seat is gone.</summary>
    public static string? KindOf(Registry m, int peer) => m.Landing.SeatOf(peer) is int pod ? m.Grid.MachineById(pod)?.Kind : null;

    /// <summary>Whether hero <paramref name="peer"/> is in a drop pod, seated or flying: the pod holds its own air, so its sound.</summary>
    public static bool InPod(Registry m, int peer) =>
        m.Landing.FlightOf(peer) != null || KindOf(m, peer) is { } kind && Content.Machines.Is(kind, Content.MachineTrait.Lands);

    /// <summary>Whether hero <paramref name="peer"/> stands in a seat that never lands (a treadmill wheel), where a held move key runs it.</summary>
    public static bool InWheel(Registry m, int peer) => KindOf(m, peer) is { } kind && !Content.Machines.Is(kind, Content.MachineTrait.Lands);

    /// <summary>
    /// Whether hero <paramref name="peer"/> sits in a seat or flies in a pod, its hands tied: whatever it holds swings, runs or
    /// shoots at nothing, and E is its only key there.
    /// </summary>
    public static bool Held(Registry m, int peer) => m.Landing.SeatOf(peer) != null || m.Landing.FlightOf(peer) != null;
}
