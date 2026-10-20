using System;
using System.Linq;

namespace Stillwake.Game.Shared;

/// <summary>
/// The Handbook's tablet screen (0081), in screen pixels for a slot of <paramref name="Slot"/>: fixed, its size set by
/// the slot size alone, as the build menu's (0073). The tab strip on top, then the body, the debris rows beside a kind's
/// entry, scrolled by the wheel with a scrollbar beside it, and the line along its foot. Read by the Handbook's shell
/// and by its debris page, so the page fills the screen it is shown on.
/// </summary>
public readonly record struct HandbookScreen(int Slot)
{
    /// <summary>A kind's slot, and the room its picture fits in.</summary>
    public int KindSide => Slot + Handbook.KindOver;
    public int KindRoom => KindSide - 2 * Slots.Inset;

    /// <summary>The most kinds in a row, and how many rows.</summary>
    public static int Columns => Handbook.DebrisRows.Max(r => r.Kinds.Count);
    public static int RowCount => Handbook.DebrisRows.Count;

    public int KindsWidth => Columns * KindSide + (Columns - 1) * Handbook.KindGap;
    public int KindsHeight => RowCount * KindSide + (RowCount - 1) * Handbook.KindGap;

    /// <summary>The body's view: the rows and the entry beside them, and room under the rows for a long entry before it scrolls.</summary>
    public int BodyWidth => KindsWidth + Handbook.EntryGap + Handbook.EntryWidth;
    public int BodyHeight => KindsHeight + Handbook.BodyOver;

    /// <summary>Where the body's top edge lies in the screen; its left edge is <see cref="TabletLayout.Pad"/>.</summary>
    public static int BodyTop => TabletLayout.Pad + TabletLayout.TabHeight + TabletLayout.Pad;

    /// <summary>Where the scrollbar's left edge lies in the screen.</summary>
    public int BarLeft => TabletLayout.Pad + BodyWidth + Slots.Gap;

    /// <summary>Where the line along the foot begins.</summary>
    public int FootTop => BodyTop + BodyHeight + TabletLayout.Pad;

    /// <summary>The screen's size: the body, the scrollbar beside it, the strips above and below, padded.</summary>
    public int Width => BarLeft + TabletLayout.BarWidth + TabletLayout.Pad;
    public int Height => FootTop + TabletLayout.FootHeight + TabletLayout.Pad;

    /// <summary>How far one notch of the wheel scrolls the body: a row of kinds.</summary>
    public int Step => KindSide + Handbook.KindGap;

    /// <summary>How far down the body is scrolled after <paramref name="by"/> notches from <paramref name="scroll"/>, over content <paramref name="content"/> tall.</summary>
    public int Scrolled(int scroll, int by, int content) => Math.Clamp(scroll + by * Step, 0, Math.Max(0, content - BodyHeight));
}
