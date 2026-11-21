using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// The row of holograms over the tablet's edge (hologram-keys): only the keys that can be used now, in their fixed order, centred, a stride
/// apart, so the row closes up with no empty slot; as large as the interface's size setting has them; and when they are bright, the pointer near the
/// screen's foot.
/// </summary>
public static class EdgeRow
{
    /// <summary>The keys on the edge, in their order: the air, the build, the research board, the market, the journal (thread-icons).</summary>
    public static readonly string[] Keys = ["air", "build", "research", "market", "journal"];

    /// <summary>Whether the row shows: in a world, once the drone is paired, its keys being the drone's (drone-windows).</summary>
    public static bool Shown(bool inWorld, bool paired) => inWorld && paired;

    /// <summary>How far above the row's top the pointer brightens the keys.</summary>
    public const float NearBand = 64;

    /// <summary>
    /// Where each key's left lies for a row whose middle is at <paramref name="middle"/>, keys <paramref name="wide"/> across, <paramref name="stride"/>
    /// apart: null for a key not shown.
    /// </summary>
    public static float?[] Lefts(IReadOnlyList<bool> shown, float middle, float wide, float stride)
    {
        int count = 0;
        foreach (bool s in shown) count += s ? 1 : 0;
        float left = FirstLeft(count, middle, wide, stride);
        var lefts = new float?[shown.Count];
        for (int i = 0, at = 0; i < shown.Count; i++)
            if (shown[i]) lefts[i] = left + at++ * stride;
        return lefts;
    }

    /// <summary>Where the first of <paramref name="count"/> keys shown lies, the row centred on <paramref name="middle"/>.</summary>
    public static float FirstLeft(int count, float middle, float wide, float stride) => middle - ((count - 1) * stride + wide) / 2;

    /// <summary>
    /// Where the full row of <paramref name="keys"/> starts over the rim in a window <paramref name="windowWide"/> wide, the keys grown
    /// <paramref name="grown"/> times: what the vitals' air stops short of (<see cref="Vitals.AirEnd"/>), the same whichever keys
    /// show now and whichever tablet is up, so the bar never moves as they come and go.
    /// </summary>
    public static float RimLeft(float windowWide, int keys, float grown)
    {
        var (left, wide) = TabletPeek.RimArt(windowWide);
        return FirstLeft(keys, TabletPeek.RowAt(left, 0, wide).Middle, TabletPeek.KeySide * grown, TabletPeek.KeyStride * grown);
    }

    /// <summary>How many times their own size the holograms are drawn: as the slots are, against the normal slot (<see cref="Ui.Slot"/>).</summary>
    public static float Grown(int slot) => slot / (float)Slots.Base;

    /// <summary>
    /// Where the row's top lies for keys <paramref name="tall"/> high at their own size grown <paramref name="grown"/> times, their feet where
    /// <paramref name="top"/> has them at their own size: a larger key stands taller, never off the screen's top.
    /// </summary>
    public static float Top(float top, float tall, float grown) => MathF.Max(TabletPeek.RowLeast, top + tall - tall * grown);

    /// <summary>
    /// Whether the keys are bright: the pointer at <paramref name="pointerY"/> no further above the row's top than the band, or over a tablet that
    /// is up; never with the pointer off the window (<paramref name="inWindow"/>), where it would keep them flickering, drawn each frame.
    /// </summary>
    public static bool Bright(float pointerY, float rowTop, bool overTablet, bool inWindow) =>
        inWindow && (overTablet || pointerY >= rowTop - NearBand);
}
