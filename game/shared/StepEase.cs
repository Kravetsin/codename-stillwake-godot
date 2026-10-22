using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// How a body walking a stair is drawn (0106): the sim lifts or lowers it a whole step at once, and its drawing follows, closing
/// a step of <c>step</c> pixels in <c>ms</c>, or as fast as the body moves across where that is faster, so a stair is drawn
/// walked as a ramp. Read from where the body is, frame by frame, so every peer draws a body the same. Plain C#, so it is tested.
/// </summary>
public sealed class StepEase(double step, double ms)
{
    private double _x = double.NaN, _y, _offset;

    /// <summary>
    /// How far below where a body's feet are, at (<paramref name="x"/>, <paramref name="y"/>) in pixels of the world, to draw them
    /// <paramref name="elapsedMs"/> after the last frame (above, while negative). <paramref name="grounded"/>, on the ground now and the
    /// frame before: a rise or fall of it then of up to two steps is a step, the drawing held where it was; off the ground nothing is
    /// added, and what is left closes at the same pace, so a jump begun mid-step neither waits nor jumps.
    /// </summary>
    public double Offset(double x, double y, bool grounded, double elapsedMs)
    {
        if (double.IsNaN(_x)) (_x, _y) = (x, y);
        double dy = y - _y, across = Math.Abs(x - _x);
        (_x, _y) = (x, y);
        if (grounded && Math.Abs(dy) <= 2 * step) _offset = Math.Clamp(_offset - dy, -2 * step, 2 * step);
        double close = Math.Max(across, step * elapsedMs / ms);
        _offset = _offset > 0 ? Math.Max(0, _offset - close) : Math.Min(0, _offset + close);
        return _offset;
    }
}
