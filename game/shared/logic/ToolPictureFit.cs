using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// A tool's picture on its card (2026-11-14-tool-card-pictures): a dark window across the card's top, the picture as large as
/// fits inside its margin, its shape kept, but never more than <see cref="Most"/> times its size in the world, so a screwdriver
/// does not swell into a blur.
/// </summary>
public static class ToolPictureFit
{
    /// <summary>The window, in screen pixels: as wide as a tool's card inside its padding, and its margin round the picture.</summary>
    public const int Width = 214, Height = 76, Margin = 6;
    /// <summary>The most a picture is enlarged, screen pixels a world pixel: the art is drawn at four, so a little past that.</summary>
    public const double Most = 5;

    /// <summary>The size, in screen pixels, a picture <paramref name="width"/> × <paramref name="height"/> world pixels is drawn at.</summary>
    public static (double Width, double Height) Of(double width, double height)
    {
        double scale = Math.Min(Most, Math.Min((Width - 2 * Margin) / width, (Height - 2 * Margin) / height));
        return (width * scale, height * scale);
    }
}
