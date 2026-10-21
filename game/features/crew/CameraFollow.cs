using System;
using Look = Stillwake.Game.Camera;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The hero as the camera follows it, one frame: its body's middle as drawn, in cells; how far it moved in its last
/// step, in thousandths of a cell, what the lead reads; and whether it floats, in space. Moved by the controller's
/// steps, not by where it faces, so turning to aim never swings the view.
/// </summary>
public readonly record struct Followed(double X, double Y, int Dx, int Dy, bool Afloat);

/// <summary>
/// Where the camera looks, in cells (<see cref="Look"/>): the hero followed exactly across, and in space up and down,
/// with an eased lead ahead the way it moves; under gravity the height eased after the body's middle, never far
/// behind. The same at any frame rate. Plain C#: stepped by <c>HeroView</c> every frame.
/// </summary>
public sealed class CameraFollow
{
    private bool _placed, _afloat;
    private double _leadX, _leadY, _aheadX, _aheadY;
    /// <summary>How far from the screen's middle the body's middle may get now: <see cref="Look.Catch"/>, or more just after floating.</summary>
    private double _catch = double.MaxValue;

    /// <summary>The point at the screen's middle, in cells.</summary>
    public double X { get; private set; }

    public double Y { get; private set; }

    /// <summary>The lead as it has turned so far, each way a share of the full lead from -1 to 1.</summary>
    public (double X, double Y) Lead => (_leadX, _leadY);

    /// <summary>Looks straight at <paramref name="hero"/>, nothing led: the first frame, and wherever the hero is put anew.</summary>
    public void Snap(Followed hero)
    {
        (X, Y, _leadX, _leadY, _aheadX, _aheadY, _catch, _afloat) = (hero.X, hero.Y, 0, 0, 0, 0, double.MaxValue, hero.Afloat);
        _placed = true;
    }

    /// <summary>
    /// Follows <paramref name="hero"/> for <paramref name="elapsedMs"/>, with a screen <paramref name="viewWidth"/>
    /// cells wide, so the lead is the same share of it at every zoom.
    /// </summary>
    public void Step(Followed hero, double viewWidth, double elapsedMs)
    {
        if (!_placed)
        {
            Snap(hero);
            return;
        }
        double turn = Ease(elapsedMs, Look.LeadTurnMs), k = Ease(elapsedMs, Look.SmoothMs);
        var (wantX, wantY) = LeadOf(hero);
        _leadX += (wantX - _leadX) * turn;
        _leadY += (wantY - _leadY) * turn;
        double reach = viewWidth * Look.LeadShare;
        _aheadX += (_leadX * reach - _aheadX) * k;
        X = hero.X + _aheadX;
        if (hero.Afloat)
        {
            // Floating off, the view goes on from where it was, then leads.
            if (!_afloat) _aheadY = Y - hero.Y;
            _aheadY += (_leadY * reach - _aheadY) * k;
            Y = hero.Y + _aheadY;
            (_catch, _afloat) = (double.MaxValue, true);
            return;
        }
        _afloat = false;
        Y += (hero.Y - Y) * k;
        // A jump's push or a fall outruns any easing: held within the catch, which closes to Catch as the view does.
        Y = Math.Clamp(Y, hero.Y - _catch, hero.Y + _catch);
        _catch = Math.Max(Look.Catch, Math.Min(_catch, Math.Abs(hero.Y - Y)));
    }

    /// <summary>
    /// The lead <paramref name="hero"/>'s last step asks for, each way a share from -1 to 1, full at
    /// <see cref="Look.LeadSpeed"/>: across only under gravity, both ways in space, no longer than full.
    /// </summary>
    public static (double X, double Y) LeadOf(Followed hero)
    {
        double x = (double)hero.Dx / Look.LeadSpeed, y = hero.Afloat ? (double)hero.Dy / Look.LeadSpeed : 0;
        double length = Math.Sqrt(x * x + y * y);
        return length > 1 ? (x / length, y / length) : (x, y);
    }

    /// <summary>How much of the way to its aim an ease with time constant <paramref name="ms"/> goes in <paramref name="elapsedMs"/>.</summary>
    public static double Ease(double elapsedMs, double ms) => ms > 0 ? 1 - Math.Exp(-elapsedMs / ms) : 1;
}
