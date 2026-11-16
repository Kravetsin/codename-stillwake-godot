using Stillwake.Sim.Landing;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// The rider's own flight as its screen shows it: a drop pod's (<see cref="Flight"/>), or a shuttle's voyage (<see cref="Voyage"/>)
/// of <c>Shuttle.FlightTicks</c>, its screen cut to as the shuttle is gone under the belly (<see cref="ShuttleTimeline"/>).
/// </summary>
/// <param name="Up">Whether it climbs out of a planet's air: a pod's return only.</param>
/// <param name="TicksLeft">Ticks till it arrives.</param>
/// <param name="Total">Ticks the whole flight lasts.</param>
/// <param name="Shuttle">Whether the craft is the crew shuttle rather than a drop pod: its screen shows it, the star lines sideways.</param>
public sealed record Ride(bool Up, int TicksLeft, int Total, bool Shuttle = false)
{
    public static Ride Of(Flight f) => new(f.Up, f.TicksLeft, FlightTimeline.Total(f));

    public static Ride Of(Voyage v) => new(false, v.TicksLeft, Stillwake.Content.Shuttle.FlightTicks, true);

    /// <summary>Seconds in that the dip to black begins, cutting from the world to the flight screen.</summary>
    public double DipFrom => Shuttle ? ShuttleTimeline.DipFrom : FlightTimeline.DipFrom;

    /// <summary>Seconds in that the flight screen shows, at the dip's darkest.</summary>
    public double ScreenFrom => Shuttle ? ShuttleTimeline.DipPeak : FlightTimeline.DipPeak;

    /// <summary>How black the screen is at the cut, <paramref name="s"/> seconds in.</summary>
    public double Dip(double s) => Shuttle ? ShuttleTimeline.Dip(s) : FlightTimeline.Dip(s);

    /// <summary>The flight screen's fraction, 0 at the dip to 1 at the white, <paramref name="s"/> seconds in.</summary>
    public double Fraction(double s) => FlightTimeline.Fraction(s, DipFrom, FlightTimeline.Length(this));

    /// <summary>Seconds in that its engine is heard from inside: a lift-off at once, the pod's drop's kick, the shuttle's sinking flame.</summary>
    public double BurnsFrom => Up ? 0 : Shuttle ? ShuttleTimeline.SinkFrom : FlightTimeline.DropFrom;
}
