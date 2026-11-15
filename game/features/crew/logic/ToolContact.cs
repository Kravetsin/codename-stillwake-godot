using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Where a running tool's sparks come out of what it cuts, along the haze it shows, a closed loop: the tool throws them itself,
/// with no bead at a touch. The grinder's working circle walked along its spin (grinder-contact), the drill's double helix out
/// along one strand and back along the other (drill-contact): each stretch of the loop inside matter ends at an exit, sparks
/// ride the loop to one and fly on along it there. A loop with no exit throws from its point nearest the touch.
/// </summary>
public static class ToolContact
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
    /// An exit: its point, the unit way the loop runs there, its place on the loop (the circle's angle, the helix's share), and how
    /// far back along the loop its stretch inside runs from its first point inside, in the same measure, none for a stretch of one.
    /// </summary>
    public readonly record struct Exit((double X, double Y) At, (double X, double Y) Way, double Angle, double Run);

    /// <summary>
    /// The exits of <paramref name="wheel"/>'s rim from what <paramref name="inside"/> says is matter (world pixels), the rim walked
    /// in <paramref name="steps"/> along its spin, at most <paramref name="most"/>, the longest stretches first; none while the rim is
    /// all in or all out, so an exit is good to a step.
    /// </summary>
    public static List<Exit> Exits(Circle wheel, int steps, Func<double, double, bool> inside, int most)
    {
        var exits = new List<Exit>();
        if (wheel.Radius <= 0 || steps < 2 || most <= 0) return exits;
        double step = 2 * Math.PI / steps;
        var hit = new bool[steps];
        for (int k = 0; k < steps; k++)
        {
            var (x, y) = wheel.At(wheel.Spin * k * step);
            hit[k] = inside(x, y);
        }
        foreach (var (last, run) in Stretches(hit))
        {
            double angle = wheel.Spin * last * step;
            exits.Add(new(wheel.At(angle), wheel.Along(angle), angle, (run - 1) * step));
        }
        return Longest(exits, most);
    }

    /// <summary>
    /// The point of <paramref name="wheel"/>'s rim nearest a touch <paramref name="at"/>, as an exit of no stretch: where a wreck the
    /// rim has no exit from throws, the wheel wholly in it or it wholly inside the wheel; toward the tool, <paramref name="toward"/>,
    /// for a touch at the hub.
    /// </summary>
    public static Exit Nearest(Circle wheel, (double X, double Y) at, (double X, double Y) toward)
    {
        double dx = at.X - wheel.Centre.X, dy = at.Y - wheel.Centre.Y;
        double angle = dx * dx + dy * dy > 1e-12 ? Math.Atan2(dy, dx) : Math.Atan2(toward.Y, toward.X);
        return new(wheel.At(angle), wheel.Along(angle), angle, 0);
    }

    /// <summary>
    /// The drill's haze in pixels of the world, as the user picked it («Двойная спираль»): two strands round its axis from the chuck
    /// to its reach, met at both ends into one loop. <see cref="From"/> is the chuck, <see cref="Way"/> the unit way along the axis
    /// and <see cref="Across"/> across it, <see cref="Length"/> the axis's length, <see cref="Phase"/> its turn as it is told. A place
    /// on the loop is a share u: out along the first strand to one half, back along the other to one. Each strand stands
    /// <see cref="RadiusAt"/> times the sine of the phase plus <see cref="HelixK"/>·a across the axis, a along it, so the strands
    /// cross and turn <see cref="WorkLooks.HelixTurns"/> times a second.
    /// </summary>
    public readonly record struct Helix((double X, double Y) From, (double X, double Y) Way, (double X, double Y) Across, double Length, double Phase)
    {
        /// <summary>The loop's point at share <paramref name="u"/>, the helix turned to <paramref name="phase"/>.</summary>
        public (double X, double Y) At(double u, double phase)
        {
            u -= Math.Floor(u);
            double v = u < 0.5 ? 2 * u : 2 - 2 * u, a = v * Length, off = (u < 0.5 ? 1 : -1) * RadiusAt(v) * Math.Sin(phase + HelixK * a);
            return (From.X + Way.X * a + Across.X * off, From.Y + Way.Y * a + Across.Y * off);
        }

        /// <summary>The unit way the loop runs at share <paramref name="u"/>, the helix turned to <paramref name="phase"/>.</summary>
        public (double X, double Y) Along(double u, double phase)
        {
            var (ax, ay) = At(u - 1e-3, phase);
            var (bx, by) = At(u + 1e-3, phase);
            double x = bx - ax, y = by - ay, l = Math.Sqrt(x * x + y * y);
            return l > 0 ? (x / l, y / l) : Way;
        }

        /// <summary>How much of the loop a spark rides a second: <see cref="WorkLooks.HelixRide"/> pixels along the axis.</summary>
        public double Rate => Length > 0 ? WorkLooks.HelixRide / (2 * Length) : 0;

        /// <summary>
        /// A strand's distance from the axis at share <paramref name="v"/> of its length: none at the chuck and the reach, where the
        /// strands meet, grown over the first <see cref="WorkLooks.HelixGrow"/> and narrowing toward the reach as a bit does.
        /// </summary>
        public static double RadiusAt(double v) =>
            WorkLooks.HelixRadius * Math.Sin(Math.PI / 2 * Math.Min(1, v / WorkLooks.HelixGrow)) * Math.Pow(Math.Max(0, 1 - v), 0.75);
    }

    /// <summary>The helix's phase per pixel along its axis, and per second.</summary>
    public const double HelixK = 2 * Math.PI / WorkLooks.HelixPitch, HelixSpin = 2 * Math.PI * WorkLooks.HelixTurns;

    /// <summary>
    /// The exits of <paramref name="helix"/>'s loop, turned as it is told, from what <paramref name="inside"/> says is matter (world
    /// pixels), walked in <paramref name="steps"/> along it, at most <paramref name="most"/>, the longest stretches first; none while
    /// it is all in or all out.
    /// </summary>
    public static List<Exit> Exits(Helix helix, int steps, Func<double, double, bool> inside, int most)
    {
        var exits = new List<Exit>();
        if (helix.Length <= 0 || steps < 2 || most <= 0) return exits;
        var hit = new bool[steps];
        for (int k = 0; k < steps; k++)
        {
            var (x, y) = helix.At((double)k / steps, helix.Phase);
            hit[k] = inside(x, y);
        }
        foreach (var (last, run) in Stretches(hit))
        {
            double u = (double)last / steps;
            exits.Add(new(helix.At(u, helix.Phase), helix.Along(u, helix.Phase), u, (double)(run - 1) / steps));
        }
        return Longest(exits, most);
    }

    /// <summary>
    /// The point of <paramref name="helix"/>'s loop, walked in <paramref name="steps"/>, nearest a touch <paramref name="at"/>, as an
    /// exit of no stretch: where a wreck the loop has no exit from throws, the loop wholly in it or missing it.
    /// </summary>
    public static Exit Nearest(Helix helix, (double X, double Y) at, int steps)
    {
        double best = double.MaxValue, u = 0;
        for (int k = 0; k < steps; k++)
        {
            var (x, y) = helix.At((double)k / steps, helix.Phase);
            double d = (x - at.X) * (x - at.X) + (y - at.Y) * (y - at.Y);
            if (d < best) (best, u) = (d, (double)k / steps);
        }
        return new(helix.At(u, helix.Phase), helix.Along(u, helix.Phase), u, 0);
    }

    // The stretches of a closed walk inside: each one's last point inside and how many points it runs; none while all in or all out.
    // Walked from a point outside, so a stretch across the walk's start is one stretch, not two.
    private static List<(int Last, int Run)> Stretches(bool[] hit)
    {
        var stretches = new List<(int, int)>();
        int steps = hit.Length, outside = Array.IndexOf(hit, false);
        if (outside < 0) return stretches;
        int run = 0;
        for (int j = 1; j <= steps; j++)
        {
            int k = (outside + j) % steps;
            if (!hit[k]) continue;
            run++;
            if (hit[(k + 1) % steps]) continue;
            stretches.Add((k, run));
            run = 0;
        }
        return stretches;
    }

    private static List<Exit> Longest(List<Exit> exits, int most)
    {
        exits.Sort((a, b) => b.Run.CompareTo(a.Run));
        if (exits.Count > most) exits.RemoveRange(most, exits.Count - most);
        return exits;
    }
}
