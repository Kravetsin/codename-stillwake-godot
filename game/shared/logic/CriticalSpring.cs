using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// A value following its aim on a critically damped spring, stepped by its exact solution with the real time between frames: it
/// glides in without overshoot, the same at any frame rate, never rounded (the air bar's motion, air-motion pick E).
/// </summary>
public struct CriticalSpring(double at)
{
    /// <summary>Where it is, and how fast it moves a second.</summary>
    public double X = at, V;

    /// <summary>
    /// Moves it <paramref name="seconds"/> on toward <paramref name="aim"/> at <paramref name="rate"/> a second: the higher, the
    /// quicker it settles, most of the way in about 3 / rate seconds.
    /// </summary>
    public void Step(double aim, double rate, double seconds)
    {
        double d = X - aim, c = V + rate * d, e = Math.Exp(-rate * seconds);
        X = aim + (d + c * seconds) * e;
        V = (V - rate * c * seconds) * e;
    }
}
