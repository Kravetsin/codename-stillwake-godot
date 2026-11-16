using Stillwake.Sim.Landing;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// The rider's own flight as its screen shows it: a drop pod's (<see cref="Flight"/>), or a shuttle's voyage (<see cref="Voyage"/>)
/// shown as a descent of <c>Shuttle.FlightTicks</c>, star lines whichever way it flies (shuttle-flight, plain pictures).
/// </summary>
/// <param name="Up">Whether it climbs out of a planet's air: a pod's return only.</param>
/// <param name="TicksLeft">Ticks till it arrives.</param>
/// <param name="Total">Ticks the whole flight lasts.</param>
public sealed record Ride(bool Up, int TicksLeft, int Total)
{
    public static Ride Of(Flight f) => new(f.Up, f.TicksLeft, FlightTimeline.Total(f));

    public static Ride Of(Voyage v) => new(false, v.TicksLeft, Stillwake.Content.Shuttle.FlightTicks);
}
