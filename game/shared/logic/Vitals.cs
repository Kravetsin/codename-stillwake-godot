using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// The vitals block at the screen's bottom-left (hud-vitals): the air row, the charge row under it, the thermometer under that
/// (the-thermometer) and the meals at the foot, each a sign and pieces of light or a number. Laid out in block units, a pixel
/// each at normal slots, scaled with the slots (<see cref="ScaleOf"/>); x from the screen's margin, y from the block's top to
/// its foot at <see cref="Foot"/>.
/// </summary>
public static class Vitals
{
    /// <summary>The block's foot above the screen's bottom, in screen pixels at every size: clear of the tablet's rim.</summary>
    public const int Below = 24;

    /// <summary>
    /// Every icon's side, in block units: the signs (oxygen, the bolt by its height, the thermometer's, hunger's), each meal's
    /// picture and the purge spot's ring, all one size.
    /// </summary>
    public const float Icon = 22;

    /// <summary>One row's centre line to the next's: the air, the charge, the thermometer and the meals, evenly spaced.</summary>
    public const float Pitch = 30;

    /// <summary>Where the signs centre across.</summary>
    public const float SignX = 20;

    /// <summary>The air row's, the charge row's, the thermometer's and the meals' centre lines, a <see cref="Pitch"/> apart.</summary>
    public const float AirY = 20, ChargeY = AirY + Pitch, ColdY = ChargeY + Pitch, MealY = ColdY + Pitch;

    /// <summary>The ring round the oxygen sign while a canister cools: just outside the icon.</summary>
    public const float RingRadius = Icon / 2 + 3;

    /// <summary>A sign's box: <see cref="Icon"/> square, centred in the signs' column on the row at <paramref name="y"/>.</summary>
    public static (float X, float Y, float Side) SignBox(float y) => (SignX - Icon / 2, y - Icon / 2, Icon);

    /// <summary>Where every row's pieces start; a piece's height; the gap between two pieces.</summary>
    public const float RowLeft = 44, PieceHeight = 10, Gap = 4;

    /// <summary>The longest a row grows before it shrinks to fit (<see cref="Fit"/>).</summary>
    public const float MostWidth = 300;

    /// <summary>A unit of air's length; a full battery's, a store's length following its capacity.</summary>
    public const float AirPerUnit = 2.3f, BatteryWidth = 40;

    /// <summary>
    /// The meals: the dish's picture's side and top, one slot to the next (the gap a <see cref="Gap"/> as between pieces), the
    /// time light's top and height.
    /// </summary>
    public const float MealSide = Icon, MealTop = MealY - Icon / 2, MealPitch = Icon + Gap, TimeTop = MealTop + Icon + 1.5f, TimeHeight = 3;

    /// <summary>The block's foot, in block units: under the meals' time lights.</summary>
    public const float Foot = TimeTop + TimeHeight + 0.5f;

    /// <summary>The highest anything is drawn, in block units: the canister's ring round the oxygen sign.</summary>
    public const float Top = AirY - RingRadius - 1;

    /// <summary>animate-pulse: down to half and back every two seconds, what a low row and a short sign breathe by.</summary>
    public static float Pulse(double clock) => 0.75f + 0.25f * (float)Math.Cos(clock * Math.PI);

    /// <summary>How large the block is drawn at a slot of <paramref name="slot"/> px: 1 at normal, 1.4 at large.</summary>
    public static float ScaleOf(int slot) => slot / (float)Slots.SizeOf(SlotScale.Normal);

    /// <summary>The block's origin on a screen <paramref name="screenHeight"/> tall: its unit (0, 0), in screen pixels.</summary>
    public static (float X, float Y) Origin(float screenHeight, int slot) => (Slots.Margin, screenHeight - Below - Foot * ScaleOf(slot));

    /// <summary>
    /// What the markers keep out of, in screen pixels: the block from its top to its foot, as wide as its longest row may grow.
    /// </summary>
    public static (float X, float Y, float W, float H) Covers(float screenHeight, int slot)
    {
        var (x, y) = Origin(screenHeight, slot);
        float s = ScaleOf(slot);
        return (x, y + Top * s, (RowLeft + MostWidth) * s, (Foot - Top) * s);
    }

    /// <summary>
    /// A row of pieces from <see cref="RowLeft"/>, each <paramref name="units"/> long at <paramref name="perUnit"/>, a
    /// <see cref="Gap"/> between: kept at that scale until the row would pass <see cref="MostWidth"/>, then all shrunk alike to
    /// fit it, the gaps kept. Their left edges and widths, in block units.
    /// </summary>
    public static IReadOnlyList<(float X, float Width)> Fit(IReadOnlyList<double> units, double perUnit)
    {
        double total = 0;
        foreach (double u in units) total += u;
        double gaps = Gap * Math.Max(0, units.Count - 1);
        if (total > 0 && total * perUnit + gaps > MostWidth) perUnit = (MostWidth - gaps) / total;
        var row = new List<(float, float)>(units.Count);
        double x = RowLeft;
        foreach (double u in units)
        {
            row.Add(((float)x, (float)(u * perUnit)));
            x += u * perUnit + Gap;
        }
        return row;
    }

    /// <summary>
    /// The order the belt's stores are drawn in, left to right, as indexes into slot order: reversed, so the store a tool
    /// drains first (the first in slot order) stands at the right end and the row empties from the right.
    /// </summary>
    public static int[] ChargeOrder(int stores)
    {
        var order = new int[stores];
        for (int i = 0; i < stores; i++) order[i] = stores - 1 - i;
        return order;
    }
}
