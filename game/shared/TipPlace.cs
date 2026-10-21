using System;

namespace Stillwake.Game.Shared;

/// <summary>Where the one tooltip goes on the screen: an item's card by the pointer, beside the slot it tells of.</summary>
public static class TipPlace
{
    /// <summary>
    /// Where a card <paramref name="card"/> large goes for a pointer at <paramref name="pointer"/> on <paramref name="slot"/>,
    /// on a screen <paramref name="screen"/> large, its top-left corner. It moves with the pointer one to one: to its right
    /// by the slot's width and <paramref name="gap"/>, so it clears the slot from wherever in it the pointer is, and
    /// <paramref name="drop"/> below it. Past the screen's right edge it flips to the pointer's left by as much, past its
    /// foot above the pointer, and stays <paramref name="edge"/> inside the screen's top and bottom: beside the slot, never
    /// over it nor the pointer. A card too wide for either side, on a screen narrower than any the game opens at, is not
    /// handled.
    /// </summary>
    public static (float X, float Y) Near(
        (float X, float Y) pointer, (float X, float Y, float W, float H) slot, (float W, float H) card, (float W, float H) screen,
        float gap, float drop, float edge)
    {
        float reach = slot.W + gap;
        float x = pointer.X + reach;
        if (x + card.W > screen.W - edge) x = pointer.X - reach - card.W;
        float y = pointer.Y + drop;
        if (y + card.H > screen.H - edge) y = pointer.Y - drop - card.H;
        return (x, Math.Max(edge, Math.Min(y, screen.H - edge - card.H)));
    }
}
