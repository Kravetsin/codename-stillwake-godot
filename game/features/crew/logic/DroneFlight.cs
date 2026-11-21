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

        /// <summary>How fast it drifts to a goal near, px/ms: to a perch <see cref="Drift"/>, to a window's place <see cref="Cruise"/>.</summary>
        public const double Drift = 0.025, Cruise = 0.12;

        /// <summary>Near its goal it slows to the way left over this, ms: to a perch <see cref="ArriveMs"/>, to a window's place <see cref="WindowArriveMs"/>.</summary>
        public const double ArriveMs = 400, WindowArriveMs = 150;

        /// <summary>
        /// Left behind further than <see cref="Slack"/>, px, it catches up at ((way - Slack) / <see cref="Snap"/>)² px/ms more:
        /// about 24 px behind a running hero, 40 behind the fastest fall.
        /// </summary>
        public const double Slack = 12, Snap = 28;

        /// <summary>The fastest it flies, px/ms: past the hero's fastest fall; darting, <see cref="DartSpeed"/>.</summary>
        public const double Chase = 1.3, DartSpeed = 0.6;

        /// <summary>How fast its velocity turns toward the wanted one, px/ms².</summary>
        public const double Accel = 0.002;

        /// <summary>The furthest it falls behind its anchor outside a window, px; an anchor jumping further than <see cref="Lost"/> in a frame puts it at its goal.</summary>
        public const double Reach = 48, Lost = 64;

        /// <summary>The longest frame it flies by, ms: a stall never flings it.</summary>
        public const double MaxFrameMs = 33;

        /// <summary>The box round its anchor it perches in, half-sizes, px.</summary>
        public const double HomeX = 10, HomeY = 5;

        /// <summary>How long it sits on a perch before it is bored of it, ms, drawn between these.</summary>
        public const int BoredMinMs = 8000, BoredMaxMs = 20000;

        /// <summary>How near a meteor's cell must be to its anchor for it to dart, px; a bolt's zone is its spread and <see cref="BoltMargin"/> columns.</summary>
        public const double Notice = 160;

        /// <summary>The columns past a bolt's spread it still darts at.</summary>
        public const int BoltMargin = 2;

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
        // Its own pace, not the hero's: left behind, it catches up the faster the further it is.
        var way = Minus(goal, At);
        double gap = Length(way);
        var wanted = gap < 1e-9 ? default : Scaled(way, Speed(gap, window, darting) / gap);
        var velocity = Plus(Velocity, Capped(Minus(wanted, Velocity), Tune.Accel * dt));
        var at = Plus(At, Scaled(velocity, dt));
        var out_ = Minus(at, anchor);
        double far = Length(out_);
        if (!window && far > Tune.Reach)
        {
            var outWay = Scaled(out_, 1 / far);
            at = Plus(anchor, Scaled(outWay, Tune.Reach));
            // Held on its circle it keeps the hero's pace outward, no more.
            double outward = Dot(Minus(velocity, hero), outWay);
            if (outward > 0) velocity = Minus(velocity, Scaled(outWay, outward));
        }
        (At, Velocity, Off) = (at, velocity, Length(Minus(goal, at)));
        return at;
    }

    /// <summary>How fast it wants to fly at <paramref name="gap"/> px from its goal, px/ms: a drift easing in near it, a catch-up past <see cref="Tune.Slack"/>.</summary>
    private static double Speed(double gap, bool window, bool darting)
    {
        double drift = window ? Math.Min(gap / Tune.WindowArriveMs, Tune.Cruise) : Math.Min(gap / Tune.ArriveMs, Tune.Drift);
        double behind = Math.Max(0, gap - Tune.Slack) / Tune.Snap;
        return Math.Min(drift + behind * behind, darting ? Tune.DartSpeed : Tune.Chase);
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
