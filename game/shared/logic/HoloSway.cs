using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// The motion «Отставание» (the user's pick for every hologram window): the windows trail the hero by a few pixels. A
/// high-pass of the hero's velocity sets where the window would be, against the change, a damped spring carries it there:
/// a start, a stop or a jump swings it, a steady run does not. While the pointer is in a hologram window or a stack is
/// carried it calms to <see cref="HoloLook.SwayCalm"/> of itself. Screen pixels and seconds.
/// </summary>
public sealed class HoloSway
{
    /// <summary>Its longest step: a long frame is taken in steps of this, so the spring stays stable.</summary>
    private const double Longest = 1 / 60.0;

    private double _smoothX, _smoothY, _x, _y, _speedX, _speedY;

    /// <summary>The share of the sway shown: 1 free, <see cref="HoloLook.SwayCalm"/> calmed.</summary>
    public double Calm { get; private set; } = 1;

    /// <summary>Where the windows are carried off their places now, never past <see cref="HoloLook.SwayReachX"/> and <see cref="HoloLook.SwayReachY"/>.</summary>
    public (float X, float Y) Offset =>
        ((float)Math.Clamp(_x * Calm, -HoloLook.SwayReachX, HoloLook.SwayReachX), (float)Math.Clamp(_y * Calm, -HoloLook.SwayReachY, HoloLook.SwayReachY));

    /// <summary>One frame of <paramref name="seconds"/>, the hero moving at (<paramref name="vx"/>, <paramref name="vy"/>) screen pixels a second; <paramref name="calm"/> while pointed at or carrying.</summary>
    public void Step(double seconds, double vx, double vy, bool calm)
    {
        if (!(seconds > 0)) return;
        double want = calm ? HoloLook.SwayCalm : 1;
        Calm += (want - Calm) * Math.Min(1, seconds * (want < Calm ? HoloLook.SwayCalmIn : HoloLook.SwayCalmOut));
        // A hitch of a second or more is taken as one: the spring settles within it anyway.
        int steps = (int)Math.Ceiling(Math.Min(seconds, 1) / Longest);
        double dt = Math.Min(seconds, 1) / steps;
        double k = HoloLook.SwayStiffness, damping = 2 * Math.Sqrt(k) * HoloLook.SwayDamping;
        for (int i = 0; i < steps; i++)
        {
            _smoothX += (vx - _smoothX) * Math.Min(1, dt * HoloLook.SwayFollow);
            _smoothY += (vy - _smoothY) * Math.Min(1, dt * HoloLook.SwayFollow);
            double toX = Math.Clamp(-(vx - _smoothX) * HoloLook.SwayGainX, -HoloLook.SwayReachX, HoloLook.SwayReachX);
            double toY = Math.Clamp(-(vy - _smoothY) * HoloLook.SwayGainY, -HoloLook.SwayReachY, HoloLook.SwayReachY);
            _speedX += ((toX - _x) * k - _speedX * damping) * dt;
            _speedY += ((toY - _y) * k - _speedY * damping) * dt;
            _x += _speedX * dt;
            _y += _speedY * dt;
        }
    }
}
