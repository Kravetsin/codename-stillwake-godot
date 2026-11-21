using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// Where the look «Перо» is at one share of the opening (<see cref="HoloMotion.Progress"/>), each part 0 none to 1 whole: the
/// first beam's reach, the pens' run round the frame, the ground, the grid's wipe, the contents, and the pens' beams sliding
/// to the near corners.
/// </summary>
public readonly record struct PenFrame(double Reach, double Trace, double Ground, double Grid, double Contents, double Settle)
{
    /// <summary>How far each pen has run, eased: 1 where they meet across the window.</summary>
    public double Along => HoloPen.Smooth(Trace);

    /// <summary>The grid's wipe, eased.</summary>
    public double Wiped => HoloPen.Smooth(Grid);

    /// <summary>Whether the frame is whole: the pens have met, and it buzzes from now on.</summary>
    public bool Traced => Trace >= 1;

    /// <summary>Whether nothing of it is drawn.</summary>
    public bool Closed => Reach <= 0;
}

/// <summary>
/// What a hologram window shows of its controls this frame: their alpha, and how far the grid's wipe has come in from the
/// <paramref name="From"/> edge, in pixels; <see cref="float.PositiveInfinity"/> for the whole window.
/// </summary>
public readonly record struct HoloReveal(float Alpha, HoloEdge From, float Reach)
{
    /// <summary>Nothing shown.</summary>
    public static readonly HoloReveal None = new(0, HoloEdge.Right, 0);

    /// <summary>The whole window at <paramref name="alpha"/>.</summary>
    public static HoloReveal Whole(float alpha) => new(alpha, HoloEdge.Right, float.PositiveInfinity);
}

/// <summary>
/// The look «Перо» (the user's pick for every hologram window): a beam reaches the window's near edge, two laser pens run
/// from the touch point both ways round its outline and meet across it, the ground fades in under them, the slot grid wipes
/// in from the near edge, the contents last; then the pens' beams slide to the near corners and stay there, never across
/// the window's face. Rectangles are in screen pixels; a path runs clockwise (<c>+1</c>) or the other way (<c>-1</c>).
/// </summary>
public static class HoloPen
{
    /// <summary>Every part at <paramref name="progress"/> of the opening.</summary>
    public static PenFrame At(double progress)
    {
        double p = progress;
        return new PenFrame(
            Share(p, 0, HoloLook.ReachEnd),
            Share(p, HoloLook.TraceFrom, HoloLook.TraceFor),
            Share(p, HoloLook.GroundFrom, HoloLook.GroundFor),
            Share(p, HoloLook.GridFrom, HoloLook.GridFor),
            Share(p, HoloLook.ContentsFrom, 1 - HoloLook.ContentsFrom),
            Smooth(Share(p, HoloLook.SettleFrom, HoloLook.SettleFor)));
    }

    /// <summary>The controls' alpha at <paramref name="frame"/>: none before the grid's wipe, half behind it, whole as the contents come in.</summary>
    public static float ControlsAlpha(PenFrame frame) =>
        frame.Grid <= 0 ? 0 : (float)(HoloLook.GridAlpha + (1 - HoloLook.GridAlpha) * frame.Contents);

    /// <summary>Half the outline: how far each pen runs before they meet.</summary>
    public static float Half(HoloRect window) => window.W + window.H;

    /// <summary><paramref name="touch"/> moved along its edge to at least <see cref="HoloLook.CornerInset"/> from either corner, or to the edge's middle on a short edge.</summary>
    public static HoloTouch Inset(HoloRect window, HoloTouch touch)
    {
        if (touch.Edge is HoloEdge.Left or HoloEdge.Right)
        {
            float m = Math.Min(HoloLook.CornerInset, window.H / 2);
            return touch with { Y = Math.Min(Math.Max(touch.Y, window.Y + m), window.Bottom - m) };
        }
        float n = Math.Min(HoloLook.CornerInset, window.W / 2);
        return touch with { X = Math.Min(Math.Max(touch.X, window.X + n), window.Right - n) };
    }

    /// <summary>
    /// The pen's path going <paramref name="way"/> from <paramref name="touch"/> round <paramref name="window"/>'s outline for
    /// <paramref name="along"/> of half of it, into <paramref name="into"/>: the touch point, each corner passed, then the tip.
    /// </summary>
    public static void Path(HoloRect window, HoloTouch touch, double along, int way, List<(float X, float Y)> into)
    {
        into.Clear();
        float s = OnOutline(window, touch);
        into.Add(Point(window, s));
        if (window.W <= 0 || window.H <= 0) return;
        float left = (float)(Math.Clamp(along, 0, 1) * Half(window));
        // At most four corners before the tip.
        for (int i = 0; i < 5 && left > 0; i++)
        {
            float corner = ToCorner(window, s, way), step = Math.Min(corner, left);
            s = Wrap(window, s + way * step);
            into.Add(Point(window, s));
            left -= step;
        }
    }

    /// <summary>The corner of the touched edge a pen going <paramref name="way"/> reaches first, where its beam settles.</summary>
    public static (float X, float Y) Near(HoloRect window, HoloEdge edge, int way) => (edge, way > 0) switch
    {
        (HoloEdge.Top, true) or (HoloEdge.Right, false) => (window.Right, window.Y),
        (HoloEdge.Right, true) or (HoloEdge.Bottom, false) => (window.Right, window.Bottom),
        (HoloEdge.Bottom, true) or (HoloEdge.Left, false) => (window.X, window.Bottom),
        _ => (window.X, window.Y),
    };

    /// <summary>How far in from the near edge the grid's wipe has come at <paramref name="wiped"/>, running past the far edge to leave it whole.</summary>
    public static float WipeReach(HoloRect window, HoloEdge from, double wiped) =>
        (float)((Depth(window, from) + HoloLook.WipePast) * Math.Clamp(wiped, 0, 1));

    /// <summary>The window's depth away from <paramref name="from"/>: its width from a side, its height from the top or bottom.</summary>
    public static float Depth(HoloRect window, HoloEdge from) => from is HoloEdge.Left or HoloEdge.Right ? window.W : window.H;

    /// <summary>
    /// The band of <paramref name="window"/> from <paramref name="near"/> to <paramref name="far"/> pixels in from its
    /// <paramref name="from"/> edge, right across it and <paramref name="past"/> beyond either side.
    /// </summary>
    public static HoloRect Band(HoloRect window, HoloEdge from, float near, float far, float past = 0)
    {
        float depth = Math.Max(0, far - near);
        return from switch
        {
            HoloEdge.Right => new HoloRect(window.Right - far, window.Y - past, depth, window.H + 2 * past),
            HoloEdge.Left => new HoloRect(window.X + near, window.Y - past, depth, window.H + 2 * past),
            HoloEdge.Bottom => new HoloRect(window.X - past, window.Bottom - far, window.W + 2 * past, depth),
            _ => new HoloRect(window.X - past, window.Y + near, window.W + 2 * past, depth),
        };
    }

    /// <summary>The smoothstep of <paramref name="x"/> clamped to 0..1.</summary>
    public static double Smooth(double x)
    {
        x = Math.Clamp(x, 0, 1);
        return x * x * (3 - 2 * x);
    }

    // Whole from its end on exactly, rounding aside: the frame is whole the moment the pens' time is up.
    private static double Share(double p, double from, double length) =>
        p >= from + length ? 1 : p <= from ? 0 : Math.Min(1, (p - from) / length);

    // The outline as one line from the top-left corner, clockwise: top, right, bottom, left.
    private static float OnOutline(HoloRect w, HoloTouch t) => t.Edge switch
    {
        HoloEdge.Top => t.X - w.X,
        HoloEdge.Right => w.W + (t.Y - w.Y),
        HoloEdge.Bottom => w.W + w.H + (w.Right - t.X),
        _ => 2 * w.W + w.H + (w.Bottom - t.Y),
    };

    private static (float X, float Y) Point(HoloRect w, float s)
    {
        if (s <= w.W) return (w.X + s, w.Y);
        if (s <= w.W + w.H) return (w.Right, w.Y + s - w.W);
        if (s <= 2 * w.W + w.H) return (w.Right - (s - w.W - w.H), w.Bottom);
        return (w.X, w.Bottom - (s - 2 * w.W - w.H));
    }

    /// <summary>How far from <paramref name="s"/> going <paramref name="way"/> the next corner lies, more than nothing.</summary>
    private static float ToCorner(HoloRect w, float s, int way)
    {
        float all = 2 * (w.W + w.H), least = all;
        foreach (float corner in (ReadOnlySpan<float>)[0, w.W, w.W + w.H, 2 * w.W + w.H])
        {
            float d = Wrap(w, way > 0 ? corner - s : s - corner);
            if (d > 1e-4f && d < least) least = d;
        }
        return least;
    }

    private static float Wrap(HoloRect w, float s)
    {
        float all = 2 * (w.W + w.H);
        s %= all;
        return s < 0 ? s + all : s;
    }
}
