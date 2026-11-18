using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>A place by the doll on the equipment's screen (gear-tabs): a slot there, and what of the body its leader line reaches.</summary>
public enum DollSpot
{
    Head,
    Mount,
    Cape,
    Suit,
    Back,
    Belt1,
    Belt2,
    Belt3,
    /// <summary>By the hand, where the first belt slot is: the weapon effect's, on the Appearance tab only.</summary>
    Hand,
}

/// <summary>
/// The equipment's screen as a doll (gear-tabs, pick B): the hero standing in the middle, facing right, and the slots round it, the
/// left column the head, the cape and the back, the right the helmet's mount, the suit and the belt's row; each slot's leader
/// line reaching the part it dresses. One table for the items feature's gear and crew's Appearance tab, at any slot size: drawn
/// for the 44 px slot and scaled with it. The screen ends a margin past its last open slot (<see cref="WidthFor"/>): a belt slot
/// opened widens it, as the clothes under the suit will (body-temperature, phase 3).
/// </summary>
public sealed class DollLayout
{
    /// <summary>The slot side the table is drawn for.</summary>
    public const int BaseSlot = 44;

    /// <summary>The spots of the gear tab: every equipment slot's, once.</summary>
    public static readonly IReadOnlyList<DollSpot> Gear =
        [DollSpot.Head, DollSpot.Mount, DollSpot.Cape, DollSpot.Suit, DollSpot.Back, DollSpot.Belt1, DollSpot.Belt2, DollSpot.Belt3];

    /// <summary>The spots of the Appearance tab: the head's, the suit's and the cape's as the gear's, the weapon effect's by the hand.</summary>
    public static readonly IReadOnlyList<DollSpot> Looks = [DollSpot.Head, DollSpot.Suit, DollSpot.Cape, DollSpot.Hand];

    // At the base slot: each spot's slot's top-left in the screen, and the body point its line reaches as thousandths of the hero's
    // height ahead of its feet and up from them; none for a belt slot after the first, whose row the first's line stands for.
    private static readonly (DollSpot Spot, int X, int Y, (int Ahead, int Up)? To)[] Table =
    [
        (DollSpot.Head, 12, 24, (20, 820)),
        (DollSpot.Cape, 12, 96, (-130, 640)),
        (DollSpot.Back, 12, 168, (-120, 510)),
        (DollSpot.Mount, 230, 24, (160, 980)),
        (DollSpot.Suit, 230, 96, (70, 560)),
        (DollSpot.Belt1, 230, 168, (40, 410)),
        (DollSpot.Belt2, 280, 168, null),
        (DollSpot.Belt3, 330, 168, null),
        (DollSpot.Hand, 230, 168, (90, 300)),
    ];

    private const int BaseMargin = 12, BaseHeight = 260, BaseFeetX = 150, BaseFeetY = 252, BaseTall = 196;

    private readonly int[] _x = new int[Table.Length], _y = new int[Table.Length];
    private readonly (int X, int Y)?[] _to = new (int X, int Y)?[Table.Length];

    private DollLayout(int slot)
    {
        Slot = slot;
        Height = Scaled(BaseHeight, slot);
        Feet = (Scaled(BaseFeetX, slot), Scaled(BaseFeetY, slot));
        Tall = Scaled(BaseTall, slot);
        foreach (var (spot, x, y, to) in Table)
        {
            int i = (int)spot;
            (_x[i], _y[i]) = (Scaled(x, slot), Scaled(y, slot));
            _to[i] = to is var (ahead, up) ? (Feet.X + (int)Math.Round(ahead * Tall / 1000.0), Feet.Y - (int)Math.Round(up * Tall / 1000.0)) : null;
        }
        Width = WidthFor(1);
    }

    /// <summary>The doll laid out for slots <paramref name="slot"/> px wide.</summary>
    public static DollLayout Of(int slot) => new(slot);

    /// <summary>The slot side it is laid out for.</summary>
    public int Slot { get; }

    /// <summary>The screen's size it fills with the first belt slot only open, px: the Appearance tab's always.</summary>
    public int Width { get; }

    /// <summary>The screen's width with <paramref name="belts"/> belt slots open, ending a margin past the last.</summary>
    public int WidthFor(int belts)
    {
        var last = belts >= 3 ? DollSpot.Belt3 : belts == 2 ? DollSpot.Belt2 : DollSpot.Belt1;
        return _x[(int)last] + Slot + Scaled(BaseMargin, Slot);
    }

    public int Height { get; }

    /// <summary>Where the hero's feet stand, px from the top-left, and how tall it is drawn.</summary>
    public (int X, int Y) Feet { get; }

    public int Tall { get; }

    /// <summary>The top-left of <paramref name="spot"/>'s slot.</summary>
    public (int X, int Y) SlotAt(DollSpot spot) => (_x[(int)spot], _y[(int)spot]);

    /// <summary>The body point <paramref name="spot"/>'s leader line reaches; none for a belt slot after the first.</summary>
    public (int X, int Y)? LeadTo(DollSpot spot) => _to[(int)spot];

    private static int Scaled(int v, int slot) => (int)Math.Round(v * slot / (double)BaseSlot);
}
