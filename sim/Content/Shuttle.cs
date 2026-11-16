namespace Stillwake.Content;

/// <summary>
/// The crew shuttle's trip to a vessel the radar found and back (shuttle-flight): what its fuel would cost, how long a flight
/// lasts in ticks (20 a second), where it stops by the vessel.
/// </summary>
public static class Shuttle
{
    /// <summary>Kilometres a litre of fuel takes the shuttle: a leg of a course costs its distance over this, rounded up.</summary>
    public const int KmPerLitre = 10;

    /// <summary>Cells of open space between the shuttle's right edge and the vessel's frame, where it stops: the jetpack crosses it.</summary>
    public const int Gap = 30;

    /// <summary>Ticks a flight lasts, out or home, whatever the distance.</summary>
    public const int FlightTicks = 200;

    /// <summary>Ticks from setting off aboard until the run it fell through shuts: the shuttle is through by then.</summary>
    public const int FallTicks = 20;
}
