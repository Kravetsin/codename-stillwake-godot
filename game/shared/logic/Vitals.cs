using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// The vitals block at the screen's bottom-left (hud-vitals, the pick «dials» A1 at 40): a row of six equal round dials (charge,
/// the body's temperature, three meals, the purge spot), a number under each that has one, and under the first the oxygen sign in
/// the same circle with the air's long bar running right from it toward the tablet edge's keys. Laid out in block units, a pixel
/// each at the base slot, scaled with the slots (<see cref="ScaleOf"/>); x from the screen's margin, y from the block's top to its
/// foot at <see cref="Foot"/>.
/// </summary>
public static class Vitals
{
    /// <summary>The block's foot above the screen's bottom, in screen pixels at every size: clear of the tablet's rim.</summary>
    public const int Below = 24;

    /// <summary>Every icon's side, in block units: the signs (oxygen, the bolt by its height, the thermometer's, hunger's) and each dish.</summary>
    public const float Icon = 26;

    /// <summary>
    /// A dial's circle across; the radius its ring of light runs at (the canister's ring round the oxygen sign and the purge
    /// spot's too); how thick that light is; the gap between two circles, across and down alike.
    /// </summary>
    public const float Dial = 40, RingRadius = 17.5f, RingLight = 4, CircleGap = 16;

    /// <summary>One dial's centre to the next's.</summary>
    public const float DialPitch = Dial + CircleGap;

    /// <summary>
    /// The most dials the row shows: charge, temperature, a dial for each meal eaten (none for an empty slot), then the purge spot or
    /// hunger (<see cref="LastDial"/>), all a <see cref="DialPitch"/> apart; three meals make six.
    /// </summary>
    public const int Dials = 6;

    /// <summary>The fixed dials' places in the row, and the first meal's.</summary>
    public const int ChargeDial = 0, TemperatureDial = 1, FirstMealDial = 2;

    /// <summary>Where the purge spot or hunger stands with <paramref name="meals"/> meals shown before it: right after them.</summary>
    public static int LastDial(int meals) => FirstMealDial + Math.Clamp(meals, 0, Dials - FirstMealDial - 1);

    /// <summary>How many dials the row shows with <paramref name="meals"/> meals: 3 with none, 6 with three.</summary>
    public static int DialsShown(int meals) => LastDial(meals) + 1;

    /// <summary>The block's top, where the dials' circles start a little below; the highest anything is drawn.</summary>
    public const float Top = 0;

    /// <summary>The dials' centre line; the air's, a gap under their circles.</summary>
    public const float RowY = Top + 4 + Dial / 2, AirY = RowY + Dial + CircleGap;

    /// <summary>The block's foot, in block units: the oxygen circle's bottom.</summary>
    public const float Foot = AirY + Dial / 2;

    /// <summary>The numbers under the dials: their middle, in the gap between the rows, and their size.</summary>
    public const float NumberY = RowY + Dial / 2 + CircleGap / 2, NumberSize = 10;

    /// <summary>Where the signs' column centres across: the first dial's, and the oxygen sign's under it.</summary>
    public const float SignX = Dial / 2;

    /// <summary>The right edge of the row of dials at its widest, three meals eaten: what <see cref="Covers"/> keeps.</summary>
    public const float RowRight = SignX + (Dials - 1) * DialPitch + Dial / 2;

    /// <summary>Where the air's pieces start; a piece's height; the gap between two pieces.</summary>
    public const float RowLeft = 44, PieceHeight = 10, Gap = 4;

    /// <summary>
    /// Where the pick page's air of 60 units in three parts ended (9 px short of the keys at the slot of 48 it was drawn for): what
    /// sets a unit of air's length, and where the bar may end until the keys are first laid.
    /// </summary>
    public const float AirReach = 410;

    /// <summary>A unit of air's length, until the bar would reach the keys (<see cref="AirLength"/>).</summary>
    public const float AirPerUnit = (AirReach - RowLeft - 2 * Gap) / 60;

    /// <summary>How far short of the edge's keys the air's bar stops, in screen pixels at every size.</summary>
    public const float KeysGap = 9;

    /// <summary>animate-pulse: down to half and back every two seconds, what a low row and a short sign breathe by.</summary>
    public static float Pulse(double clock) => 0.75f + 0.25f * (float)Math.Cos(clock * Math.PI);

    /// <summary>How large the block is drawn at a slot of <paramref name="slot"/> px: 1.1 at normal, 1.5 at large.</summary>
    public static float ScaleOf(int slot) => slot / (float)Slots.Base;

    /// <summary>The block's origin on a screen <paramref name="screenHeight"/> tall: its unit (0, 0), in screen pixels.</summary>
    public static (float X, float Y) Origin(float screenHeight, int slot) => (Slots.Margin, screenHeight - Below - Foot * ScaleOf(slot));

    /// <summary>The centre across of dial <paramref name="dial"/>, 0 the charge's.</summary>
    public static float Column(int dial) => SignX + dial * DialPitch;

    /// <summary>
    /// Where the air's bar may end, in block units, at a slot of <paramref name="slot"/> px with the edge's full row of keys starting
    /// at <paramref name="keysLeft"/> on the screen (<see cref="EdgeRow.RimLeft"/>): <see cref="KeysGap"/> short of them.
    /// </summary>
    public static float AirEnd(float keysLeft, int slot) => (keysLeft - KeysGap - Slots.Margin) / ScaleOf(slot);

    /// <summary>
    /// The air's piece, gaps not counted, for <paramref name="units"/> of air in <paramref name="parts"/> parts with a
    /// <see cref="Gap"/> between: <see cref="AirPerUnit"/> a unit until the bar, gaps and all, would pass <paramref name="end"/>,
    /// then shrunk alike to end there.
    /// </summary>
    public static float AirLength(int units, int parts, float end)
    {
        float room = Math.Max(0, end - RowLeft - Gap * Math.Max(0, parts - 1));
        return Math.Min(Math.Max(0, units) * AirPerUnit, room);
    }

    /// <summary>
    /// What the markers keep out of, in screen pixels: the block from its top to its foot, as wide as the row of dials or the air's
    /// bar may reach, whichever is further.
    /// </summary>
    public static (float X, float Y, float W, float H) Covers(float screenHeight, int slot, float keysLeft)
    {
        var (x, y) = Origin(screenHeight, slot);
        float s = ScaleOf(slot);
        return (x, y + Top * s, Math.Max(RowRight, AirEnd(keysLeft, slot)) * s, (Foot - Top) * s);
    }

    /// <summary>
    /// The order the belt's stores stand round the charge dial, clockwise from its top, as indexes into slot order: reversed, so the
    /// store a tool drains first (the first in slot order) is the last arc and the ring empties back toward its start.
    /// </summary>
    public static int[] ChargeOrder(int stores)
    {
        var order = new int[stores];
        for (int i = 0; i < stores; i++) order[i] = stores - 1 - i;
        return order;
    }

    /// <summary>The gap between two of the charge dial's arcs, along its ring, in block units; one stays at the top too.</summary>
    public const float ArcGap = 3;

    /// <summary>
    /// The charge dial's arcs for <paramref name="count"/> stores, clockwise from its top in screen radians (y down, the top at
    /// −π/2): the ring shared alike, an <see cref="ArcGap"/> between each two and at the top.
    /// </summary>
    public static IReadOnlyList<(float From, float To)> ChargeArcs(int count)
    {
        var arcs = new List<(float, float)>(count);
        float gap = ArcGap / RingRadius, each = 2 * MathF.PI / Math.Max(1, count), top = -MathF.PI / 2;
        for (int i = 0; i < count; i++) arcs.Add((top + i * each + gap / 2, top + (i + 1) * each - gap / 2));
        return arcs;
    }
}
