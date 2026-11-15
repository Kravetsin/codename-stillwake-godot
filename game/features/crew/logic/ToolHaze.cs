using System;
using System.Collections.Generic;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A tool's haze as points with the way it moves at each (spark-rule), the one thing each tool gives: a struck blade's edge
/// swept from one frame to the next (the spear's thrust among them), the grinder's working circle along its spin, the drill's
/// double helix along its loop, the cutter's jet along itself. What it hits throws its own look, by its material, from where
/// the haze lies in it, along the haze; one rule for every tool and every target.
/// </summary>
public static class ToolHaze
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
    /// A struck blade's haze: its edge's points as they are (<paramref name="now"/>) and the middle of each one's sweep from where it
    /// was (<paramref name="was"/>, one to one), each moving the way it went; a point that did not move is left out.
    /// </summary>
    public static void OfEdge(IReadOnlyList<(double X, double Y)> was, IReadOnlyList<(double X, double Y)> now, List<HazePoint> into)
    {
        if (was.Count != now.Count) return;
        for (int i = 0; i < now.Count; i++)
        {
            var (a, b) = (was[i], now[i]);
            double dx = b.X - a.X, dy = b.Y - a.Y, l = Math.Sqrt(dx * dx + dy * dy);
            if (l <= 0) continue;
            into.Add(new((a.X + b.X) / 2, (a.Y + b.Y) / 2, dx / l, dy / l));
            into.Add(new(b.X, b.Y, dx / l, dy / l));
        }
    }

    /// <summary>The grinder's haze: <paramref name="wheel"/>'s rim in <paramref name="steps"/> points, each moving along the spin.</summary>
    public static void OfCircle(Circle wheel, int steps, List<HazePoint> into)
    {
        for (int k = 0; k < steps; k++)
        {
            double angle = 2 * Math.PI * k / steps;
            var (x, y) = wheel.At(angle);
            var (dx, dy) = wheel.Along(angle);
            into.Add(new(x, y, dx, dy));
        }
    }

    /// <summary>
    /// The drill's haze: <paramref name="helix"/>'s loop as it is told, in <paramref name="steps"/> points, each moving back along the
    /// axis to the chuck, as its waves run and as a bit carries out what it cuts.
    /// </summary>
    public static void OfHelix(Helix helix, int steps, List<HazePoint> into)
    {
        for (int k = 0; k < steps; k++)
        {
            double u = (double)k / steps;
            var (x, y) = helix.At(u, helix.Phase);
            into.Add(new(x, y, -helix.Way.X, -helix.Way.Y));
        }
    }

    /// <summary>The cutter's haze: its jet from <paramref name="from"/> to <paramref name="to"/>, a point every <paramref name="step"/> px, each moving along it.</summary>
    public static void OfLine((double X, double Y) from, (double X, double Y) to, double step, List<HazePoint> into)
    {
        double dx = to.X - from.X, dy = to.Y - from.Y, l = Math.Sqrt(dx * dx + dy * dy);
        if (l <= 0) return;
        for (double s = 0; s <= l; s += step) into.Add(new(from.X + dx * s / l, from.Y + dy * s / l, dx / l, dy / l));
        into.Add(new(to.X, to.Y, dx / l, dy / l));
    }

    /// <summary>
    /// Where <paramref name="haze"/> lies in what was hit at <paramref name="at"/>: its points <paramref name="inside"/> it; else its
    /// point nearest <paramref name="at"/>, so what is hit always throws off the haze; none without a haze. All move one way, the
    /// haze's flow through it, glanced off <paramref name="matter"/> (what <paramref name="inside"/> tests, if none is given) (<see cref="Flow"/>).
    /// </summary>
    public static List<HazePoint> Contacts(IReadOnlyList<HazePoint> haze, Func<double, double, bool> inside, (double X, double Y) at,
        Func<double, double, bool>? matter = null)
    {
        matter ??= inside;
        var contacts = new List<HazePoint>();
        foreach (var p in haze)
            if (inside(p.X, p.Y)) contacts.Add(p);
        if (contacts.Count > 0 || haze.Count == 0) return Flow(contacts, matter);
        var nearest = haze[0];
        double best = double.MaxValue;
        foreach (var p in haze)
        {
            double d = (p.X - at.X) * (p.X - at.X) + (p.Y - at.Y) * (p.Y - at.Y);
            if (d < best) (best, nearest) = (d, p);
        }
        contacts.Add(nearest);
        return Flow(contacts, matter);
    }

    /// <summary>
    /// <paramref name="contacts"/> all moving one way, the haze's net flow through what was hit, so what it throws flies as one stream
    /// whatever the haze's shape there (a wheel's arc, a helix's turns); each kept its own where the flow cancels out, under
    /// <see cref="WorkLooks.FlowLeast"/> of a whole; then each glanced off <paramref name="matter"/> (<see cref="Glance"/>).
    /// </summary>
    public static List<HazePoint> Flow(List<HazePoint> contacts, Func<double, double, bool> matter)
    {
        double dx = 0, dy = 0;
        foreach (var p in contacts) (dx, dy) = (dx + p.Dx, dy + p.Dy);
        double l = Math.Sqrt(dx * dx + dy * dy);
        bool one = l > WorkLooks.FlowLeast * contacts.Count;
        for (int i = 0; i < contacts.Count; i++)
            contacts[i] = Glance(one ? contacts[i] with { Dx = dx / l, Dy = dy / l } : contacts[i], matter);
        return contacts;
    }

    /// <summary>
    /// <paramref name="p"/> moving its way unless that leads into <paramref name="matter"/> (<see cref="WorkLooks.GlanceProbe"/> px on):
    /// then glanced off its surface, the way mirrored by the surface's outward normal, the mean of the ways round the point that
    /// lead out of it at the nearest of <see cref="WorkLooks.GlanceRadii"/>; straight back where all round is matter.
    /// </summary>
    public static HazePoint Glance(HazePoint p, Func<double, double, bool> matter)
    {
        if (!matter(p.X + p.Dx * WorkLooks.GlanceProbe, p.Y + p.Dy * WorkLooks.GlanceProbe)) return p;
        foreach (double r in WorkLooks.GlanceRadii)
        {
            double nx = 0, ny = 0;
            for (int k = 0; k < WorkLooks.GlanceRays; k++)
            {
                double a = 2 * Math.PI * k / WorkLooks.GlanceRays, ox = Math.Cos(a), oy = Math.Sin(a);
                if (!matter(p.X + ox * r, p.Y + oy * r)) (nx, ny) = (nx + ox, ny + oy);
            }
            double l = Math.Sqrt(nx * nx + ny * ny);
            if (l < 1e-9) continue;
            (nx, ny) = (nx / l, ny / l);
            double into = p.Dx * nx + p.Dy * ny;
            if (into >= 0) return p;
            double dx = p.Dx - 2 * into * nx, dy = p.Dy - 2 * into * ny, m = Math.Sqrt(dx * dx + dy * dy);
            return p with { Dx = dx / m, Dy = dy / m };
        }
        return p with { Dx = -p.Dx, Dy = -p.Dy };
    }
}
