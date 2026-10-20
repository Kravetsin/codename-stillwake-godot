using System;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The build menu's frame (0073), in screen pixels for a slot of <paramref name="Slot"/>: a fixed panel docked at the
/// bottom middle, its size set by the slot size alone, never by the tab or how many cards it holds. The tab strip on
/// top, then <see cref="Columns"/> by <see cref="Rows"/> cards, the inventory's slots, scrolled a row at a time, a
/// scrollbar beside them, and the line of the card in hand and the hint under them.
/// </summary>
public readonly record struct Catalogue(int Slot)
{
    /// <summary>Cards in a row and rows shown at once: 42 before the grid scrolls.</summary>
    public const int Columns = 14, Rows = 3;

    /// <summary>The tablet's strips (<see cref="TabletLayout"/>): its padding, the tabs, the line under the cards, the scrollbar.</summary>
    public const int Pad = TabletLayout.Pad, TabHeight = TabletLayout.TabHeight, FootHeight = TabletLayout.FootHeight,
        BarWidth = TabletLayout.BarWidth, ThumbLeast = TabletLayout.ThumbLeast;

    /// <summary>From one card's left edge to the next's.</summary>
    public int Step => Slot + Slots.Gap;

    public int GridWidth => Columns * Step - Slots.Gap;
    public int GridHeight => Rows * Step - Slots.Gap;

    /// <summary>The panel's size: the grid, the scrollbar beside it, the strips above and below, padded.</summary>
    public int Width => Pad + GridWidth + Slots.Gap + BarWidth + Pad;
    public int Height => GridTop + GridHeight + Pad + FootHeight + Pad;

    /// <summary>Where the grid's top edge lies in the panel; its left edge is <see cref="Pad"/>.</summary>
    public int GridTop => Pad + TabHeight + Pad;

    /// <summary>Where the scrollbar's left edge lies in the panel.</summary>
    public int BarLeft => Pad + GridWidth + Slots.Gap;

    /// <summary>Where the line under the cards begins.</summary>
    public int FootTop => GridTop + GridHeight + Pad;

    /// <summary>How many rows <paramref name="count"/> cards fill.</summary>
    public static int RowsOf(int count) => (count + Columns - 1) / Columns;

    /// <summary>The farthest the grid scrolls: the first row shown when the last row is at the bottom; 0 while all fit.</summary>
    public static int LastTop(int count) => Math.Max(0, RowsOf(count) - Rows);

    /// <summary>The first row shown after scrolling <paramref name="by"/> rows from <paramref name="top"/>, kept within the cards.</summary>
    public static int Scrolled(int top, int by, int count) => Math.Clamp(top + by, 0, LastTop(count));

    /// <summary>The first row shown so that the card at <paramref name="index"/> is on screen, moving as little as it can.</summary>
    public static int Revealing(int top, int index)
    {
        int row = index / Columns;
        return row < top ? row : row >= top + Rows ? row - Rows + 1 : top;
    }

    /// <summary>Where the card at <paramref name="index"/> lies in the grid with row <paramref name="top"/> first; off the grid when not shown.</summary>
    public (int X, int Y) SpotOf(int top, int index) => (index % Columns * Step, (index / Columns - top) * Step);

    /// <summary>
    /// The card at (<paramref name="x"/>, <paramref name="y"/>) from the grid's top-left with row <paramref name="top"/>
    /// first, as an index into the <paramref name="count"/> cards; none off the grid, in a gap or past the last card.
    /// </summary>
    public int? CardAt(float x, float y, int top, int count)
    {
        if (x < 0 || y < 0 || x >= GridWidth || y >= GridHeight) return null;
        int column = (int)x / Step, row = (int)y / Step;
        if ((int)x - column * Step >= Slot || (int)y - row * Step >= Slot) return null;
        int index = (top + row) * Columns + column;
        return index < count ? index : null;
    }

    /// <summary>The scrollbar's thumb, its top and length down the grid's height, with row <paramref name="top"/> first; none while every card fits.</summary>
    public (float Top, float Length)? Thumb(int top, int count)
    {
        int last = LastTop(count);
        if (last == 0) return null;
        float length = Math.Max(ThumbLeast, (float)GridHeight * Rows / RowsOf(count));
        return ((GridHeight - length) * Math.Clamp(top, 0, last) / last, length);
    }
}
