namespace Stillwake.Game.Menu;

/// <summary>
/// The title's plate (2026-10-25-title-scene): a chunk of hull torn off a ship, bent down and ragged at its left, its right
/// end whole, level and square, where the hero sits. Painted by <c>tools/anime-art</c> (<c>title.js</c>) on this box and
/// these points, in the world's pixels from its top left.
/// </summary>
public static class PlateArt
{
    /// <summary>Its width and height in pixels.</summary>
    public const int Width = 140, Height = 63;

    /// <summary>The top of its right end, where the hero's hips rest, the edge just to its right.</summary>
    public static readonly (int X, int Y) Seat = (136, 26);

    /// <summary>Where the emergency lamp sits on its top.</summary>
    public static readonly (int X, int Y) Lamp = (86, 26);

    /// <summary>Where the hero's tether is made fast, on its top.</summary>
    public static readonly (int X, int Y) Anchor = (110, 26);
}
