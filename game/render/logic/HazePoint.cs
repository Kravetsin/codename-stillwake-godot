namespace Stillwake.Game.Render;

/// <summary>
/// A point of a tool's haze in the world's pixels and the unit way the haze moves there (spark-rule): where what a tool hits
/// throws its sparks and chips from, and the way they fly.
/// </summary>
public readonly record struct HazePoint(double X, double Y, double Dx, double Dy);
