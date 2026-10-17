using System;
using System.Collections.Generic;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The harpoon's line as drawn (0044): a rope in the tether's space style (<see cref="Tether.Space"/>) from
/// the gun to the rod's eye, in thousandths of a cell. Paid out a little longer than the way it spans, so
/// it hangs slack in flight and hooked; reeling or winding back, wound in to the span, taut.
/// </summary>
public sealed class HarpoonLine
{
    /// <summary>How much longer than the way it spans the slack line is let out.</summary>
    private const double Slack = 1.15;
    /// <summary>The share of the difference the line pays out in each step, so it slackens smoothly.</summary>
    private const double PayOut = 0.15;
    /// <summary>
    /// How much the drum winds in a step at most, in thousandths of a cell: 24 cells a second, so slack goes in
    /// a moment and a piece reeled at 5 a second never leaves the line loose behind it.
    /// </summary>
    private const double WindIn = 400;
    /// <summary>Wound in to its span, the rope is held this short of it, so it pulls straight at once, as a line under load.</summary>
    private const double Stretch = 0.7;

    private readonly Rope _rope;
    private double _length;

    public HarpoonLine((double X, double Y) gun, (double X, double Y) eye, int seed = 0)
    {
        _rope = new Rope(Tether.Space, gun, eye, seed);
        _length = Span(gun, eye);
    }

    /// <summary>How long the line is let out now.</summary>
    public double Length => _length;

    /// <summary>Adds the line's points as drawn this frame, in thousandths of a cell, to <paramref name="into"/>.</summary>
    public void DrawnInto(List<(double X, double Y)> into) => _rope.DrawnInto(into);

    /// <summary>Moves the line on by <paramref name="elapsedMs"/>, from <paramref name="gun"/> to <paramref name="eye"/>, slack or <paramref name="taut"/>.</summary>
    public void Update((double X, double Y) gun, (double X, double Y) eye, bool taut, double elapsedMs) =>
        _rope.Update(gun, eye, elapsedMs, span =>
        {
            _length = taut ? Math.Max(span, _length - WindIn) : Math.Max(span, _length + (span * Slack - _length) * PayOut);
            return taut && _length <= span ? span * Stretch : _length;
        });

    private static double Span((double X, double Y) a, (double X, double Y) b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
