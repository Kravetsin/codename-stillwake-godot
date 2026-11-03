using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// The tablet's top edge showing along the window's foot ([tablet-edge](../../../docs/decisions/2026-11-02-tablet-edge.md)), in screen pixels: where the rim lies, how a
/// tablet goes from it to where it rests, and where the keys sit on whichever is up.
/// </summary>
public static class TabletPeek
{
    /// <summary>The gap kept between the tablet's art and the window's sides, the widest tablet's as well as the rim's.</summary>
    public const int Margin = 12;

    /// <summary>How much of the dark screen shows above the window's foot, and how tall the rim's screen is laid, past the foot whatever it shows.</summary>
    public const int ScreenShows = 6, RimTall = 64;

    /// <summary>The metal the five keys sit on: its size, how far its top stands above the bezel's, and how far the keys lie in from its left and top, a key's stride apart.</summary>
    public const int SaddleWide = 212, SaddleTall = 36, SaddleAbove = 12, KeyIn = 6, KeyDown = 2, KeyStride = 34;

    /// <summary>How much more of the art's width lies on the stylus's side than the other: the metal's middle is that much left of the art's.</summary>
    private const int StylusSide = 16;

    /// <summary>The widest a tablet's screen can be in a window <paramref name="windowWide"/> across: the rim's.</summary>
    public static float ScreenWidth(float windowWide) =>
        MathF.Floor(windowWide - TabletLayout.ArtAround * TabletLayout.ArtScale - 2 * Margin);

    /// <summary>Where that screen's left lies, the art between the window's sides.</summary>
    public static float ScreenLeft(float windowWide) =>
        MathF.Round((windowWide - ScreenWidth(windowWide) - TabletLayout.ArtAround * TabletLayout.ArtScale) / 2 + TabletLayout.ScreenLeft * TabletLayout.ArtScale);

    /// <summary>Where the rim's screen starts in a window <paramref name="windowTall"/> high.</summary>
    public static float ScreenTop(float windowTall) => windowTall - ScreenShows;

    /// <summary>
    /// Where a tablet's screen is now, <paramref name="up"/> of the way (<see cref="TabletMotion.Up"/>) from the rim to where it rests: the rim's at 0, so it rises out of
    /// the rim whatever its own size.
    /// </summary>
    public static (float Left, float Top, float Width) Pose(double up, (float Left, float Top, float Width) rest, float windowWide, float windowTall)
    {
        float t = (float)up;
        return (
            MathF.Round(Lerp(ScreenLeft(windowWide), rest.Left, t)),
            MathF.Round(Lerp(ScreenTop(windowTall), rest.Top, t)),
            MathF.Round(Lerp(ScreenWidth(windowWide), rest.Width, t)));
    }

    /// <summary>Where the metal the keys sit on goes on a tablet's art at (<paramref name="artLeft"/>, <paramref name="artTop"/>), <paramref name="artWide"/> across: on the bezel's top, over its plate.</summary>
    public static (float X, float Y) SaddleAt(float artLeft, float artTop, float artWide) => (
        MathF.Round(artLeft + (artWide - StylusSide) / 2 - SaddleWide / 2f),
        artTop + TabletLayout.BezelAbove - SaddleAbove);

    /// <summary>The credits' display over the art's charge (market-key): its size, and how far its left edge lies in from the art's right and its top below the art's.</summary>
    public const int DisplayWide = 60, DisplayTall = 14, DisplayIn = 153, DisplayDown = 18;

    /// <summary>Where the credits' display lies on a tablet's art whose right edge is at <paramref name="artRight"/> and top at <paramref name="artTop"/>.</summary>
    public static (float X, float Y) DisplayAt(float artRight, float artTop) => (MathF.Round(artRight - DisplayIn), MathF.Round(artTop + DisplayDown));

    private static float Lerp(float from, float to, float t) => from + (to - from) * t;
}
