using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// A point of a tool's haze in the world's pixels and the unit way the haze moves there (spark-rule), with the index of the haze's
/// next point along its path, -1 where the path ends: where what a tool hits throws its sparks and chips from, and the way they fly.
/// </summary>
public readonly record struct HazePoint(double X, double Y, double Dx, double Dy, int Next = -1)
{
    /// <summary>A contact's ride: the haze's points it runs through before it flies off its way (spark-rule); none to fly at once.</summary>
    public IReadOnlyList<(double X, double Y)>? Ride { get; init; }
}
