using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where the keys' row lies along the window's foot ([tablet-edge](../../../docs/decisions/2026-11-02-tablet-edge.md)), in screen pixels: over where the tablet's rim
/// lay, fixed whatever window is open (holo windows), and the lowest a window may reach so it never covers them.
/// </summary>
public static class TabletPeek
{
    /// <summary>The gap kept between the tablet's art and the window's sides, the widest tablet's as well as the rim's.</summary>
    public const int Margin = 12;

    /// <summary>How much of the old rim's screen showed above the window's foot: where the keys' row is still measured from.</summary>
    public const int ScreenShows = 6;

    /// <summary>The keys' row (hologram-keys): a key's stride, how far its top floats above the bezel's, and the highest it goes, so the keys stay on screen over a tablet at rest.</summary>
    public const int KeyStride = 34, RowAbove = 33, RowLeast = 2;

    /// <summary>A key's side at its own size (<c>EdgeKey</c>), before the interface's size grows it.</summary>
    public const int KeySide = 28;

    /// <summary>How much more of the art's width lies on the stylus's side than the other: the row's middle is that much left of the art's.</summary>
    private const int StylusSide = 16;

    /// <summary>The widest a tablet's screen can be in a window <paramref name="windowWide"/> across: the rim's.</summary>
    public static float ScreenWidth(float windowWide) =>
        MathF.Floor(windowWide - TabletLayout.ArtAround * TabletLayout.ArtScale - 2 * Margin);

    /// <summary>Where that screen's left lies, the art between the window's sides.</summary>
    public static float ScreenLeft(float windowWide) =>
        MathF.Round((windowWide - ScreenWidth(windowWide) - TabletLayout.ArtAround * TabletLayout.ArtScale) / 2 + TabletLayout.ScreenLeft * TabletLayout.ArtScale);

    /// <summary>
    /// Where the rim's art lies across a window <paramref name="windowWide"/> wide: its left and width, the frame round the rim's
    /// screen as the tablet laid it.
    /// </summary>
    public static (float Left, float Wide) RimArt(float windowWide) => (
        ScreenLeft(windowWide) - TabletLayout.ScreenLeft * TabletLayout.ArtScale,
        ScreenWidth(windowWide) + TabletLayout.ArtAround * TabletLayout.ArtScale);

    /// <summary>Where the rim's screen starts in a window <paramref name="windowTall"/> high.</summary>
    public static float ScreenTop(float windowTall) => windowTall - ScreenShows;

    /// <summary>Where the rim's art started in a window <paramref name="windowTall"/> high: what the keys' row stands on.</summary>
    public static float RimArtTop(float windowTall) => ScreenTop(windowTall) - TabletLayout.ScreenTop * TabletLayout.ArtScale;

    /// <summary>The keys' top in a window <paramref name="windowTall"/> high, the keys grown <paramref name="grown"/> times: fixed, whatever window is open.</summary>
    public static float KeysTop(float windowTall, float grown) => EdgeRow.Top(RowAt(0, RimArtTop(windowTall), 0).Top, KeySide, grown);

    /// <summary>The lowest a window may reach in a window <paramref name="windowTall"/> high, a margin over the keys grown <paramref name="grown"/> times.</summary>
    public static float Floor(float windowTall, float grown) => KeysTop(windowTall, grown) - Margin;

    /// <summary>Where the keys' row goes over a tablet's art at (<paramref name="artLeft"/>, <paramref name="artTop"/>), <paramref name="artWide"/> across: its middle over the bezel's plate, and the keys' top.</summary>
    public static (float Middle, float Top) RowAt(float artLeft, float artTop, float artWide) => (
        MathF.Round(artLeft + (artWide - StylusSide) / 2),
        MathF.Max(RowLeast, artTop + TabletLayout.BezelAbove - RowAbove));

}
