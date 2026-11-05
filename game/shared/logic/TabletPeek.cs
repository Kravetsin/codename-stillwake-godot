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

    /// <summary>The keys' row (hologram-keys): a key's stride, how far its top floats above the bezel's, and the highest it goes, so the keys stay on screen over a tablet at rest.</summary>
    public const int KeyStride = 34, RowAbove = 33, RowLeast = 2;

    /// <summary>How much more of the art's width lies on the stylus's side than the other: the row's middle is that much left of the art's.</summary>
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

    /// <summary>Where the keys' row goes over a tablet's art at (<paramref name="artLeft"/>, <paramref name="artTop"/>), <paramref name="artWide"/> across: its middle over the bezel's plate, and the keys' top.</summary>
    public static (float Middle, float Top) RowAt(float artLeft, float artTop, float artWide) => (
        MathF.Round(artLeft + (artWide - StylusSide) / 2),
        MathF.Max(RowLeast, artTop + TabletLayout.BezelAbove - RowAbove));

    private static float Lerp(float from, float to, float t) => from + (to - from) * t;
}
