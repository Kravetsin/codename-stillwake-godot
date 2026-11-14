using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Where a running grinder's wheel leaves what it cuts (grinder-contact): its working circle, the haze round it, walked along its
/// spin; each stretch of the rim inside matter ends at an exit, the last point inside before the rim comes out. Sparks ride the
/// rim to an exit and fly off along it there, as a real wheel throws them.
/// </summary>
public static class WheelContact
{
    /// <summary>
    /// The wheel's working circle in pixels of the world: its hub, its radius, and its spin, +1 turning clockwise on screen (the angle
    /// growing, +y down), -1 the other way.
    /// </summary>
    public readonly record struct Circle((double X, double Y) Centre, double Radius, int Spin)
    {
        /// <summary>The rim's point at <paramref name="angle"/> radians.</summary>
        public (double X, double Y) At(double angle) => (Centre.X + Math.Cos(angle) * Radius, Centre.Y + Math.Sin(angle) * Radius);

        /// <summary>The unit way the rim moves at <paramref name="angle"/>, along the spin.</summary>
        public (double X, double Y) Along(double angle) => (-Math.Sin(angle) * Spin, Math.Cos(angle) * Spin);
    }

    /// <summary>
    /// An exit: its point, the unit way the rim moves there, its angle, and how far back along the spin its stretch inside runs
    /// from its first point inside, radians, none for a stretch of one point.
    /// </summary>
    public readonly record struct Exit((double X, double Y) At, (double X, double Y) Way, double Angle, double Run);

    /// <summary>
    /// The exits of <paramref name="wheel"/>'s rim from what <paramref name="inside"/> says is matter (world pixels), the rim walked
    /// in <paramref name="steps"/> along its spin, at most <paramref name="most"/>, the longest stretches first; none while the rim is
    /// all in or all out, so an exit is good to a step.
    /// </summary>
    public static List<Exit> Of(Circle wheel, int steps, Func<double, double, bool> inside, int most)
    {
        var exits = new List<Exit>();
        if (wheel.Radius <= 0 || steps < 2 || most <= 0) return exits;
        double step = 2 * Math.PI / steps;
        var hit = new bool[steps];
        int outside = -1;
        for (int k = 0; k < steps; k++)
        {
            var (x, y) = wheel.At(wheel.Spin * k * step);
            hit[k] = inside(x, y);
            if (!hit[k] && outside < 0) outside = k;
        }
        if (outside < 0) return exits;
        // From a point outside, so a stretch across the walk's start is one stretch, not two.
        int run = 0;
        for (int j = 1; j <= steps; j++)
        {
            int k = (outside + j) % steps, next = (k + 1) % steps;
            if (!hit[k]) continue;
            run++;
            if (hit[next]) continue;
            double angle = wheel.Spin * k * step;
            exits.Add(new(wheel.At(angle), wheel.Along(angle), angle, (run - 1) * step));
            run = 0;
        }
        exits.Sort((a, b) => b.Run.CompareTo(a.Run));
        if (exits.Count > most) exits.RemoveRange(most, exits.Count - most);
        return exits;
    }
}
