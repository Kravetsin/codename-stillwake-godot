using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Where a running tool's sparks come out of what it cuts, along the haze it shows: the tool throws them itself, with no bead at
/// a touch. The grinder's working circle walked along its spin (grinder-contact): each stretch of its rim inside matter ends at
/// an exit, and sparks ride the rim to one. The drill's spiral along its axis (drill-contact): the bore is the axis's first
/// stretch inside, and sparks ride the spiral back from inside it to its mouth. Each flies on along the haze where it leaves.
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
    /// An exit: its point, the unit way the rim moves there, its angle, and how far back along the spin its stretch inside runs
    /// from its first point inside, radians, none for a stretch of one point.
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
    /// The drill's haze spiral in pixels of the world: the bit's point, the unit way to the reach and the unit way across it, how
    /// far back the tool reaches from its point to the shoulder and how far the reach goes past it; and its phase at the point as it is told.
    /// At <c>s</c> along the axis the spiral stands <see cref="WorkLooks.ScrewRadius"/> times the sine of the phase plus
    /// <see cref="ScrewK"/>·s across it, the phase turning <see cref="WorkLooks.ScrewTurns"/> turns a second, so its waves run back
    /// to the hand.
    /// </summary>
    public readonly record struct Screw((double X, double Y) Tip, (double X, double Y) Way, (double X, double Y) Across, double Back, double Length,
        double Phase)
    {
        /// <summary>The spiral's point <paramref name="s"/> pixels along the axis from the point, its phase at the point <paramref name="phase"/>.</summary>
        public (double X, double Y) At(double s, double phase)
        {
            double off = WorkLooks.ScrewRadius * Math.Sin(phase + ScrewK * s);
            return (Tip.X + Way.X * s + Across.X * off, Tip.Y + Way.Y * s + Across.Y * off);
        }

        /// <summary>The axis's point <paramref name="s"/> pixels along from the bit's point.</summary>
        public (double X, double Y) Axis(double s) => (Tip.X + Way.X * s, Tip.Y + Way.Y * s);

        /// <summary>
        /// The unit way a spark riding back to the hand at <paramref name="speed"/> pixels a second moves at <paramref name="s"/>, the
        /// phase at the point <paramref name="phase"/>: back along the axis, and across as the spiral turns under it.
        /// </summary>
        public (double X, double Y) Rides(double s, double phase, double speed)
        {
            double across = WorkLooks.ScrewRadius * Math.Cos(phase + ScrewK * s) * (ScrewSpin - ScrewK * speed);
            double x = -Way.X * speed + Across.X * across, y = -Way.Y * speed + Across.Y * across, l = Math.Sqrt(x * x + y * y);
            return l > 0 ? (x / l, y / l) : (-Way.X, -Way.Y);
        }
    }

    /// <summary>The drill's spiral's phase per pixel along its axis, and per second.</summary>
    public const double ScrewK = 2 * Math.PI / WorkLooks.ScrewPitch, ScrewSpin = 2 * Math.PI * WorkLooks.ScrewTurns;

    /// <summary>
    /// A bore: how far along the axis from the bit's point its mouth is, the first point inside, and its deepest, the last
    /// point inside before the axis comes out again; behind the point where the bit itself is in.
    /// </summary>
    public readonly record struct Bore(double Mouth, double Deepest);

    /// <summary>
    /// The bore of <paramref name="screw"/>'s axis into what <paramref name="inside"/> says is matter (world pixels), walked every
    /// <paramref name="step"/> pixels from the shoulder to the reach; its mouth at the shoulder while the shoulder is in. None while
    /// it misses.
    /// </summary>
    public static Bore? BoreOf(Screw screw, double step, Func<double, double, bool> inside)
    {
        if (step <= 0) return null;
        double? mouth = null;
        double deepest = 0;
        for (double s = -screw.Back; s <= screw.Length + 1e-9; s += step)
        {
            var (x, y) = screw.Axis(s);
            if (!inside(x, y))
            {
                if (mouth != null) break;
                continue;
            }
            mouth ??= s;
            deepest = s;
        }
        return mouth is { } m ? new Bore(m, deepest) : null;
    }

    /// <summary>
    /// A bore of no depth at the axis's point nearest a touch <paramref name="at"/>: where a wreck the axis misses throws, one an
    /// outer ray of the fan touched.
    /// </summary>
    public static Bore Under(Screw screw, (double X, double Y) at)
    {
        double s = Math.Clamp((at.X - screw.Tip.X) * screw.Way.X + (at.Y - screw.Tip.Y) * screw.Way.Y, -screw.Back, screw.Length);
        return new(s, s);
    }
}
