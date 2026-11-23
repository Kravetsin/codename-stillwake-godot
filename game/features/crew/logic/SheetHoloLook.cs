namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Where the drone floats while a drone window is open or the plan is out (drone-holo): before the hero's waist, its shapes let go.
/// Client only. Lengths in world px.
/// </summary>
public static class SheetHoloLook
{
    /// <summary>The drone before the hero, <see cref="OverAhead"/> past its middle the way it faces and <see cref="OverUp"/> over its feet.</summary>
    public const double OverAhead = 16, OverUp = 12;

    /// <summary>How near that place the drone counts as there, pixels.</summary>
    public const double OverArrived = 1.5;
}
