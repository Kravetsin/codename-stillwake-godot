using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// The rugged tablet's measures (0073), in screen pixels: the strips its screens share, the build menu's and the
/// Handbook's (0081), and where its art lies round the screen. Plain numbers, so the layouts built on them are tested.
/// </summary>
public static class TabletLayout
{
    /// <summary>The screen's padding, and the space between its strips.</summary>
    public const int Pad = 8;

    /// <summary>The tab strip's height, and the search field's.</summary>
    public const int TabHeight = 24;

    /// <summary>The height of the line along the screen's foot.</summary>
    public const int FootHeight = 20;

    /// <summary>The scrollbar's width, and the shortest its thumb gets.</summary>
    public const int BarWidth = 6, ThumbLeast = 16;

    /// <summary>
    /// The art (tablet_frame.png, a 9-slice), a pixel of it two of the screen: the screen's top-left in it, and how much
    /// wider and taller it is than the screen, in its pixels.
    /// </summary>
    public const int ArtScale = 2, ScreenLeft = 9, ScreenTop = 16, ArtAround = 26;

    /// <summary>Above the screen, the bezel's top edge: what the prompt for E keeps above.</summary>
    public const int BezelAbove = 8 * ArtScale;

    /// <summary>How far past the bottom edge the tablet slides from its rest to be gone: the stylus's tip too, and a few pixels more.</summary>
    public const int Lift = ScreenTop * ArtScale + 4;

    /// <summary>
    /// A scrollbar's thumb beside a view <paramref name="view"/> tall over content <paramref name="content"/> tall,
    /// scrolled <paramref name="scroll"/> down: its top and length down the view; none while the content fits.
    /// </summary>
    public static (float Top, float Length)? Thumb(float view, float content, float scroll)
    {
        float last = content - view;
        if (last <= 0) return null;
        float length = Math.Max(ThumbLeast, view * view / content);
        return ((view - length) * Math.Clamp(scroll, 0, last) / last, length);
    }
}
