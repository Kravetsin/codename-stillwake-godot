using System;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The light drone's body (docs/plans/drone-motion.md): where it flies by its hero, in pixels of the world, after the goal its will
/// gives (<see cref="DroneWill"/>). It keeps the hero's velocity plus a pull to its goal, its own velocity turning toward that at a
/// bounded acceleration, so a turn carries it on past the head and round, never a jump; outside a window it stays within
/// <see cref="Tune.Reach"/> of its anchor. It ignores walls (light-drone).
/// </summary>
public sealed class DroneFlight
{
    /// <summary>Its tunables, in px and ms: the client's alone, they never enter the sim.</summary>
    public static class Tune
    {
        /// <summary>How far its anchor is over the neck, px.</summary>
        public const double Above = 16;

        /// <summary>The fastest its pull to its goal flies it, past the hero's own speed, px/ms; darting, <see cref="DartSpeed"/>.</summary>
        public const double Cruise = 0.12, DartSpeed = 0.6;

        /// <summary>How fast its velocity turns toward the wanted one, px/ms².</summary>
        public const double Accel = 0.002;

        /// <summary>The pull to its goal: the way left over this, px/ms.</summary>
        public const double ArriveMs = 250;

        /// <summary>The furthest it flies from its anchor outside a window, px; an anchor jumping further than <see cref="Lost"/> in a frame puts it at its goal.</summary>
        public const double Reach = 40, Lost = 64;

        /// <summary>The longest frame it flies by, ms: a stall never flings it.</summary>
        public const double MaxFrameMs = 33;

        /// <summary>The box round its anchor it perches in, half-sizes, px.</summary>
        public const double HomeX = 10, HomeY = 5;

        /// <summary>How long it sits on a perch before it is bored of it, ms, drawn between these.</summary>
        public const int BoredMinMs = 3000, BoredMaxMs = 8000;

        /// <summary>How near a meteor's cell must be to its anchor for it to dart, px; a bolt's zone is its spread and <see cref="BoltMargin"/> columns.</summary>
        public const double Notice = 160;

        /// <summary>The columns past a bolt's spread it still darts at.</summary>
        public const int BoltMargin = 2;

        /// <summary>How long it watches its hero's work, how long it rests before watching again, and the gap in the work that keeps it watching, ms.</summary>
        public const double WatchMs = 2500, WatchRestMs = 6000, WatchGraceMs = 500;

        /// <summary>How far short of the work point it hovers to watch, px: never on the point itself.</summary>
        public const double WatchApart = 12;

        /// <summary>How far it leans toward a hunter, px.</summary>
        public const double Lean = 6;

        /// <summary>A friend's low air: one red pulse each this long, ms; a dart's danger flashes it red each <see cref="FlashMs"/>.</summary>
        public const double PulseMs = 1200, FlashMs = 300;
    }

    private Vec _anchor;
    private bool _placed;

    /// <summary>Where it is, pixels of the world.</summary>
    public Vec At { get; private set; }

    /// <summary>How fast it flies, px/ms.</summary>
    public Vec Velocity { get; private set; }

    /// <summary>How far it is from its goal, px, as last flown.</summary>
    public double Off { get; private set; }

    /// <summary>
    /// Its anchor: <see cref="Tune.Above"/> over the neck at <paramref name="neck"/>, in the column of the feet at
    /// <paramref name="feetX"/> so a turn moves nothing; lying, over the body's middle at <paramref name="lying"/>.
    /// </summary>
    public static Vec Anchor(Vec neck, double feetX, Vec? lying = null) =>
        lying is { } body ? new Vec(body.X, body.Y - Tune.Above) : new Vec(feetX, neck.Y - Tune.Above);

    /// <summary>A hero's velocity in px/ms from its own, thousandths of a cell a step.</summary>
    public static Vec VelocityOf(int vx, int vy) => new(vx * Rig.Cell / Hero.Unit / Hero.StepMs, vy * Rig.Cell / Hero.Unit / Hero.StepMs);

    /// <summary>Forgets where it was: the next <see cref="Step"/> puts it at its goal at rest (hidden, then shown).</summary>
    public void Lose() => _placed = false;

    /// <summary>
    /// Flies it <paramref name="elapsedMs"/> toward <paramref name="goal"/>, its hero's anchor at <paramref name="anchor"/> moving at
    /// <paramref name="hero"/> px/ms: <paramref name="darting"/> faster, in a <paramref name="window"/> with no reach. Where it is now.
    /// </summary>
    public Vec Step(Vec anchor, Vec hero, Vec goal, bool window, bool darting, double elapsedMs)
    {
        double dt = Math.Clamp(elapsedMs, 0, Tune.MaxFrameMs);
        if (!_placed || Length(Minus(anchor, _anchor)) > Tune.Lost)
        {
            var placed = window ? goal : Plus(anchor, Capped(Minus(goal, anchor), Tune.Reach));
            (_placed, _anchor, At, Velocity, Off) = (true, anchor, placed, default, Length(Minus(goal, placed)));
            return At;
        }
        _anchor = anchor;
        var pull = Capped(Scaled(Minus(goal, At), 1 / Tune.ArriveMs), darting ? Tune.DartSpeed : Tune.Cruise);
        var wanted = Plus(hero, pull);
        var velocity = Plus(Velocity, Capped(Minus(wanted, Velocity), Tune.Accel * dt));
        var at = Plus(At, Scaled(velocity, dt));
        var out_ = Minus(at, anchor);
        double far = Length(out_);
        if (!window && far > Tune.Reach)
        {
            var way = Scaled(out_, 1 / far);
            at = Plus(anchor, Scaled(way, Tune.Reach));
            // Held on its circle it keeps the hero's pace outward, no more.
            double outward = Dot(Minus(velocity, hero), way);
            if (outward > 0) velocity = Minus(velocity, Scaled(way, outward));
        }
        (At, Velocity, Off) = (at, velocity, Length(Minus(goal, at)));
        return at;
    }

    internal static Vec Plus(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y);

    internal static Vec Minus(Vec a, Vec b) => new(a.X - b.X, a.Y - b.Y);

    internal static Vec Scaled(Vec a, double k) => new(a.X * k, a.Y * k);

    internal static double Dot(Vec a, Vec b) => a.X * b.X + a.Y * b.Y;

    internal static double Length(Vec a) => Math.Sqrt(a.X * a.X + a.Y * a.Y);

    /// <summary><paramref name="a"/>, no longer than <paramref name="most"/>.</summary>
    internal static Vec Capped(Vec a, double most)
    {
        double length = Length(a);
        return length > most ? Scaled(a, most / length) : a;
    }
}
