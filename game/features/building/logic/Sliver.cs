namespace Stillwake.Game.Features.Building;

/// <summary>
/// A quad of the build glow too thin to draw: a band clipped to a hair of a plan far from the origin keeps a width the
/// float sum swallows, its corners meet, and Godot cannot triangulate it. Under a hundredth of a pixel nothing shows.
/// </summary>
public static class Sliver
{
    /// <summary>The least width and height on screen, in pixels, a quad is drawn at.</summary>
    public const float Least = 0.01f;

    /// <summary>Whether the screen quad from (<paramref name="x0"/>, <paramref name="y0"/>) to (<paramref name="x1"/>, <paramref name="y1"/>) is too thin to draw.</summary>
    public static bool Is(float x0, float y0, float x1, float y1) => !(x1 - x0 >= Least && y1 - y0 >= Least);
}
