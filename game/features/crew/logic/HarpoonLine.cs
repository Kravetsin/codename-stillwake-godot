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
    /// How much the drum winds in a step at most, in thousandths of a cell: 48 cells a second, so slack goes in
    /// a moment and a piece reeled at 16 a second never leaves the line loose behind it.
    /// </summary>
    private const double WindIn = 800;
    /// <summary>Wound in to its span, the rope is held this short of it, so it pulls straight at once, as a line under load.</summary>
    private const double Stretch = 0.7;

    private readonly Rope _rope;
    private double _length;

    public HarpoonLine((double X, double Y) gun, (double X, double Y) eye, int seed = 0)
    {
        _rope = new Rope(Tether.Space, gun, eye, seed);
        _length = Span(gun, eye);
    }

    /// <summary>
    /// Where the line ends for a rod with its point at <paramref name="tip"/>, pointing <paramref name="pointing"/>, its eye
    /// <paramref name="rodLength"/> behind it: at its eye, or at <paramref name="gun"/> while that eye is still in the gun (the gun
    /// on the rod, between its eye and its point), as a line tied behind the muzzle would run back through the gun. A hero
    /// anywhere else, behind a stuck rod's point too, keeps the line to the eye.
    /// </summary>
    public static (double X, double Y) EndOf((double X, double Y) gun, (double X, double Y) tip, (double X, double Y) pointing, double rodLength)
    {
        var eye = (X: tip.X - pointing.X * rodLength, Y: tip.Y - pointing.Y * rodLength);
        double along = (gun.X - eye.X) * pointing.X + (gun.Y - eye.Y) * pointing.Y;
        double aside = Math.Abs((gun.X - eye.X) * pointing.Y - (gun.Y - eye.Y) * pointing.X);
        return along >= 0 && along <= rodLength && aside <= rodLength / 2 ? gun : eye;
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
