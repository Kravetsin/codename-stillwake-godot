using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// What the tick («Клещ», candidate 3 of the sentry page) is drawn from: its pose and where that puts its parts, for its rig
/// (<see cref="TickRig"/>), and the page's leg sums. Facing right, y down, its origin the middle of its feet. Plain C#, tested.
/// </summary>
public static class TickArt
{
    /// <summary>A colour, 0..255 a channel.</summary>
    public readonly record struct Rgb(double R, double G, double B)
    {
        /// <summary>As a flare takes a colour.</summary>
        public (int, int, int) Bytes => ((int)R, (int)G, (int)B);

        public static Rgb Mix(Rgb a, Rgb b, double t) => new(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
    }

    public static readonly Rgb Black = new(0, 0, 0), White = new(255, 255, 255);

    /// <summary>
    /// What it is doing, the page's <c>P</c>: crouched (1, −0.5 a hop up), walking (0..1) and how far its legs have gone, its legs
    /// folded for flight (0..1), its eye's colour and brightness (lit at 1 and over), where it looks across (−1..1, its own way),
    /// how far its death's cracks have spread (0..1, none at 0), its front claw raised to work at a line (0..1), its mandible open (0..1) and how alert it stands (0..1).
    /// </summary>
    public readonly record struct Pose(double Crouch, double Walk, double Gait, double Fly, Rgb Eye, double EyeAmp, double Look, double Cracks = 0,
        double Saw = 0, double Jaw = 0, double Alert = 0);

    /// <summary>
    /// Where its parts are this frame, from the middle of its feet: the eye, its nozzle, its near feet, the
    /// claw that cuts, the points its wind-up's arcs jump between (knees, then near feet), the middle of its body.
    /// </summary>
    public sealed record Anchors((double X, double Y) Eye, (double X, double Y) Nozzle, IReadOnlyList<(double X, double Y)> Feet,
        (double X, double Y) Cut, IReadOnlyList<(double X, double Y)> Arcs, (double X, double Y) Centre);

    /// <summary>The page's <c>ik</c>, its knee bent upward.</summary>
    public static (double X, double Y) Knee((double X, double Y) hip, (double X, double Y) foot, double upper, double lower)
    {
        double dx = foot.X - hip.X, dy = foot.Y - hip.Y, dd = Math.Sqrt(dx * dx + dy * dy);
        if (dd == 0) dd = 1e-3;
        double d = Math.Min(dd, upper + lower - 0.01);
        double a = (upper * upper - lower * lower + d * d) / (2 * d), h = Math.Sqrt(Math.Max(0, upper * upper - a * a));
        double ux = dx / dd, uy = dy / dd, px = -uy, py = ux;
        if (py > 0) (px, py) = (-px, -py);
        return (hip.X + ux * a + px * h, hip.Y + uy * a + py * h);
    }

    /// <summary>The page's <c>stepFoot</c>: a foot <paramref name="phase"/> through its step, lifted only on its way forward.</summary>
    public static (double X, double Y) StepFoot(double rest, double phase, double walk, double stride, double lift)
    {
        double s = Math.Sin(phase), c = Math.Cos(phase);
        return (rest - c * stride * walk, s > 0 ? -Math.Floor(s * lift * walk + 0.5) : 0);
    }
}
