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
/// the machine, how much of the window has grown, and the tone of a machine's station.
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

    /// <summary>
    /// How much of <paramref name="window"/> shows at <paramref name="progress"/> of its growth: first a line along the
    /// touched edge, out from the touch point (<see cref="HoloLook.EdgeFirst"/> of it), then spreading across, each easing
    /// out; null at none. <c>Line</c> while it is still the edge alone.
    /// </summary>
    public static (HoloRect Rect, bool Line)? Grown(HoloRect window, HoloTouch touch, double progress)
    {
        if (progress <= 0) return null;
        if (progress >= 1) return (window, false);
        const float Thin = 2;
        bool upright = touch.Edge is HoloEdge.Left or HoloEdge.Right;
        if (progress < HoloLook.EdgeFirst)
        {
            float v = EaseOut(progress / HoloLook.EdgeFirst);
            if (upright)
            {
                float top = touch.Y - (touch.Y - window.Y) * v, bottom = touch.Y + (window.Bottom - touch.Y) * v;
                float x = touch.Edge == HoloEdge.Right ? window.Right - Thin : window.X;
                return (new HoloRect(x, top, Thin, Math.Max(1, bottom - top)), true);
            }
            float from = touch.X - (touch.X - window.X) * v, to = touch.X + (window.Right - touch.X) * v;
            float y = touch.Edge == HoloEdge.Bottom ? window.Bottom - Thin : window.Y;
            return (new HoloRect(from, y, Math.Max(1, to - from), Thin), true);
        }
        float across = EaseOut((progress - HoloLook.EdgeFirst) / (1 - HoloLook.EdgeFirst));
        return touch.Edge switch
        {
            HoloEdge.Right => (window with { X = window.Right - Math.Max(Thin, window.W * across), W = Math.Max(Thin, window.W * across) }, false),
            HoloEdge.Left => (window with { W = Math.Max(Thin, window.W * across) }, false),
            HoloEdge.Bottom => (window with { Y = window.Bottom - Math.Max(Thin, window.H * across), H = Math.Max(Thin, window.H * across) }, false),
            _ => (window with { H = Math.Max(Thin, window.H * across) }, false),
        };
    }

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

    private static float EaseOut(double x) => (float)(1 - Math.Pow(1 - x, 3));
}
