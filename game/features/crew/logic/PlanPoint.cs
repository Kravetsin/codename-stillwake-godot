using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The near hand showing the drone where a plan goes (drone-holo): placing one, the arm comes up straight at it and jabs once, the
/// point gesture's second jab (<see cref="GestureMotions"/>), and stays pointing while plans keep coming, then goes down. Client only.
/// </summary>
public sealed class PlanPoint
{
    /// <summary>How long the arm takes to come up and to go down, and how long it stays up after the last plan, ms.</summary>
    public const double InMs = 150, OutMs = 250, HoldMs = 700;

    /// <summary>The jab played: from <see cref="JabFrom"/> to <see cref="JabTo"/> of the point gesture, the arm straight at either end.</summary>
    public const double JabFrom = 0.24, JabTo = 0.5;

    private static readonly GestureMotion Pointing = GestureMotions.Of(Gesture.Point);
    private double _sincePlan = double.PositiveInfinity, _jabMs = double.PositiveInfinity;

    /// <summary>How far up the arm is, 0 hanging to 1 pointing.</summary>
    public double Weight { get; private set; }

    /// <summary>Where it points, in the world's pixels: the latest plan.</summary>
    public (double X, double Y) At { get; private set; }

    /// <summary>The motion its arm plays, and how far in, for <see cref="Gestures.ArmAt"/>.</summary>
    public (GestureMotion Motion, double Ms) Arm =>
        (Pointing, Pointing.Ms * (_jabMs < (JabTo - JabFrom) * Pointing.Ms ? JabFrom + _jabMs / Pointing.Ms : JabTo));

    /// <summary>A plan placed at <paramref name="at"/>: it points there, jabbing again once the last jab is done.</summary>
    public void Placed((double X, double Y) at)
    {
        At = at;
        _sincePlan = 0;
        if (_jabMs >= (JabTo - JabFrom) * Pointing.Ms) _jabMs = 0;
    }

    /// <summary>Plays on by <paramref name="elapsedMs"/>; while it <paramref name="may"/> not (the hands busy, the plan away), it goes down at once.</summary>
    public void Step(double elapsedMs, bool may)
    {
        _sincePlan += elapsedMs;
        _jabMs += elapsedMs;
        if (!may)
        {
            (Weight, _sincePlan, _jabMs) = (0, double.PositiveInfinity, double.PositiveInfinity);
            return;
        }
        Weight = _sincePlan < HoldMs ? Math.Min(1, Weight + elapsedMs / InMs) : Math.Max(0, Weight - elapsedMs / OutMs);
    }
}
