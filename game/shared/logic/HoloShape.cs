using System;
using System.Collections.Generic;
using Stillwake.Content;

namespace Stillwake.Game.Shared;

/// <summary>A rectangle in screen or world pixels: a machine window, a machine's box.</summary>
public readonly record struct HoloRect(float X, float Y, float W, float H)
{
    public float Right => X + W;
    public float Bottom => Y + H;
    public (float X, float Y) Middle => (X + W / 2, Y + H / 2);
}

/// <summary>The edge of a window a beam touches.</summary>
public enum HoloEdge { Left, Top, Right, Bottom }

/// <summary>Where the beam touches a window: the point, and the edge it lies on.</summary>
public readonly record struct HoloTouch(float X, float Y, HoloEdge Edge);

/// <summary>
/// The geometry of a machine window's hologram (machine-holograms): where the beam touches the window, where it leaves
/// the machine, and the tone of a machine's station; how the window is traced is <see cref="HoloPen"/>'s.
/// </summary>
public static class HoloShape
{
    /// <summary>
    /// The point of <paramref name="window"/>'s edge nearest the middle of <paramref name="box"/>, and its edge: the side the
    /// middle lies beyond, the farther one at a corner; from inside, the nearest side.
    /// </summary>
    public static HoloTouch Touch(HoloRect window, HoloRect box)
    {
        var (x, y) = box.Middle;
        float left = window.X - x, right = x - window.Right, above = window.Y - y, below = y - window.Bottom;
        float outX = Math.Max(Math.Max(left, right), 0), outY = Math.Max(Math.Max(above, below), 0);
        if (outX > 0 || outY > 0)
        {
            float cx = Math.Clamp(x, window.X, window.Right), cy = Math.Clamp(y, window.Y, window.Bottom);
            var edge = outX >= outY ? (left > 0 ? HoloEdge.Left : HoloEdge.Right) : (above > 0 ? HoloEdge.Top : HoloEdge.Bottom);
            return new HoloTouch(cx, cy, edge);
        }
        // Inside: the nearest side, the right first on a tie, as the reference draws its stations there.
        float toLeft = -left, toRight = -right, toTop = -above, toBottom = -below;
        float least = Math.Min(Math.Min(toLeft, toRight), Math.Min(toTop, toBottom));
        if (toRight == least) return new HoloTouch(window.Right, y, HoloEdge.Right);
        if (toLeft == least) return new HoloTouch(window.X, y, HoloEdge.Left);
        if (toBottom == least) return new HoloTouch(x, window.Bottom, HoloEdge.Bottom);
        return new HoloTouch(x, window.Y, HoloEdge.Top);
    }

    /// <summary>
    /// Where the beam leaves the machine: the point of <paramref name="box"/> nearest <paramref name="touch"/>, inset by one
    /// <paramref name="cell"/>, or by half the box along a side under two cells.
    /// </summary>
    public static (float X, float Y) Emitter(HoloRect box, (float X, float Y) touch, float cell)
    {
        float insetX = box.W < 2 * cell ? box.W / 2 : cell, insetY = box.H < 2 * cell ? box.H / 2 : cell;
        return (Within(touch.X, box.X + insetX, box.Right - insetX), Within(touch.Y, box.Y + insetY, box.Bottom - insetY));
    }

    // Math.Clamp throws where rounding leaves the two ends of a half-box inset a hair apart the wrong way.
    private static float Within(float x, float low, float high) => Math.Min(Math.Max(x, low), Math.Max(low, high));

    /// <summary>The tone of <paramref name="spec"/>'s window, by its static traits, never by whether it has power now.</summary>
    public static HoloTone ToneOf(MachineSpec spec) =>
        spec.Is(MachineTrait.Fire) ? HoloTone.Warm : spec.Powered ? HoloTone.Cold : HoloTone.Neutral;

    /// <summary>The box round <paramref name="cells"/> on a grid <paramref name="width"/> wide, in cells: its corner and size.</summary>
    public static (int X, int Y, int W, int H) BoxOf(IReadOnlyList<int> cells, int width)
    {
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        foreach (int c in cells)
            (left, top, right, bottom) = (Math.Min(left, c % width), Math.Min(top, c / width), Math.Max(right, c % width), Math.Max(bottom, c / width));
        return (left, top, right - left + 1, bottom - top + 1);
    }
}
