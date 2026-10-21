using System;
using Look = Stillwake.Game.Camera;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The hero as the camera follows it, one frame: its body's middle as drawn, in cells; how far it moved in its last
/// step, in thousandths of a cell, what the lead reads; whether it stands, on the ground or a ladder, and whether it
/// floats, in space. Moved by the controller's steps, not by where it faces, so turning to aim never swings the view.
/// </summary>
public readonly record struct Followed(double X, double Y, int Dx, int Dy, bool Settled, bool Afloat);

/// <summary>
/// Where the camera looks, in cells (<see cref="Look"/>): ahead of the hero the way it moves, eased there whatever
/// the frame rate, and under gravity at the height the hero last stood at through a jump, following a landing on
/// another height or a long fall. Plain C#: stepped by <c>HeroView</c> every frame.
/// </summary>
public sealed class CameraFollow
{
    private bool _placed;
    private double _holdY, _leadX, _leadY;
    /// <summary>How far below the screen's middle a fall may get now: <see cref="Look.CatchDown"/>, or more just after floating.</summary>
    private double _catch = double.MaxValue;

    /// <summary>The point at the screen's middle, in cells.</summary>
    public double X { get; private set; }

    public double Y { get; private set; }

    /// <summary>The lead as it has turned so far, each way a share of the full lead from -1 to 1.</summary>
    public (double X, double Y) Lead => (_leadX, _leadY);

    /// <summary>Looks straight at <paramref name="hero"/>, nothing led or held: the first frame, and wherever the hero is put anew.</summary>
    public void Snap(Followed hero)
    {
        (X, Y, _holdY, _leadX, _leadY, _catch) = (hero.X, hero.Y, hero.Y, 0, 0, double.MaxValue);
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
        double turn = Ease(elapsedMs, Look.LeadTurnMs);
        var (wantX, wantY) = LeadOf(hero);
        _leadX += (wantX - _leadX) * turn;
        _leadY += (wantY - _leadY) * turn;
        double reach = viewWidth * Look.LeadShare;
        double aimX = hero.X + _leadX * reach, aimY;
        if (hero.Afloat)
        {
            _holdY = hero.Y;
            aimY = hero.Y + _leadY * reach;
        }
        else
        {
            if (hero.Settled) _holdY = hero.Y;
            aimY = Math.Clamp(_holdY, hero.Y - Look.HoldDown, hero.Y + Look.HoldUp);
        }
        double k = Ease(elapsedMs, Look.SmoothMs);
        X += (aimX - X) * k;
        Y += (aimY - Y) * k;
        if (hero.Afloat)
        {
            _catch = double.MaxValue;
            return;
        }
        // A fall outruns any easing: held within the catch, which closes to CatchDown as the view does.
        Y = Math.Max(Y, hero.Y - _catch);
        _catch = Math.Max(Look.CatchDown, Math.Min(_catch, hero.Y - Y));
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
