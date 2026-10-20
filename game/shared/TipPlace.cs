using System;

namespace Stillwake.Game.Shared;

/// <summary>Where the one tooltip goes on the screen: an item's card beside the slot it tells of (the item card mockup).</summary>
public static class TipPlace
{
    /// <summary>
    /// Where a card <paramref name="card"/> large goes beside <paramref name="slot"/> on a screen <paramref name="screen"/>
    /// large, its top-left corner: right of the slot, <paramref name="gap"/> off it, its top level with the slot's; past the
    /// screen's right edge flipped to the slot's left, so it never covers its own slot; kept <paramref name="edge"/> inside
    /// the screen's top and bottom. A card too wide for either side, on a screen narrower than any the game opens at, is
    /// not handled.
    /// </summary>
    public static (float X, float Y) Beside(
        (float X, float Y, float W, float H) slot, (float W, float H) card, (float W, float H) screen, float gap, float edge)
    {
        float x = slot.X + slot.W + gap;
        if (x + card.W > screen.W - edge) x = slot.X - gap - card.W;
        float y = Math.Max(edge, Math.Min(slot.Y, screen.H - edge - card.H));
        return (x, y);
    }
}
