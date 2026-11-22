using System;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The light drone's body (drone-motion ADR): where it flies by its hero, in pixels of the world, after the goal its will
/// gives (<see cref="DroneWill"/>). A spring that never swings: it learns the hero's pace a little late, so it falls behind a
/// start or a turn, speeds up past the hero until it is by it again, and keeps by it. Left further it hurries more;
/// nothing holds it on a circle. It ignores walls (light-drone).
/// </summary>
public sealed class DroneFlight
{
    /// <summary>Its tunables, in px and ms: the client's alone, they never enter the sim.</summary>
    public static class Tune
    {
        /// <summary>How far its anchor is over the neck standing upright, px: clear of the crown on its lowest perch.</summary>
        public const double Above = 24;

        /// <summary>How long it takes to answer, ms: following, going to a window's place, darting.</summary>
        public const double FollowMs = 260, WindowMs = 110, DartMs = 120;

        /// <summary>How long it takes to learn its hero's new pace, ms, while it follows unhurried.</summary>
        public const double LearnMs = 180;

        /// <summary>Further than its perches it answers quicker: once more each <see cref="Far"/> px, up to <see cref="Hurry"/> times more.</summary>
        public const double Far = 60, Hurry = 2;

        /// <summary>How long a change of how quick it answers eases over, ms: it speeds up, it never jerks.</summary>
        public const double EaseMs = 80;

        /// <summary>An anchor jumping further than this in a frame puts it at its goal, px.</summary>
        public const double Lost = 64;

        /// <summary>The longest frame it flies by and the steps it flies one in, ms: a stall never flings it.</summary>
        public const double MaxFrameMs = 50, StepMs = 4;

        /// <summary>It perches aside, px from its anchor: between <see cref="HomeNear"/> and <see cref="HomeX"/> across either way, up to <see cref="HomeY"/> up or down.</summary>
        public const double HomeNear = 12, HomeX = 18, HomeY = 3;

        /// <summary>Shown at its anchor, it leaves for a perch aside this soon after, ms.</summary>
        public const double AsideMs = 400;

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

    private Vec _anchor, _pace;
    private double _rate;
    private bool _placed;

    /// <summary>Where it is, pixels of the world.</summary>
    public Vec At { get; private set; }

    /// <summary>How fast it flies, px/ms.</summary>
    public Vec Velocity { get; private set; }

    /// <summary>How far it is from its goal, px, as last flown.</summary>
    public double Off { get; private set; }

    /// <summary>
    /// Its anchor: <see cref="Tune.Above"/> over the neck at <paramref name="neck"/>, in the hero's column at
    /// <paramref name="feetX"/> so a turn moves nothing; lying, over the body's middle at <paramref name="lying"/>.
    /// </summary>
    public static Vec Anchor(Vec neck, double feetX, Vec? lying = null) =>
        lying is { } body ? new Vec(body.X, body.Y - Tune.Above) : new Vec(feetX, neck.Y - Tune.Above);

    /// <summary>
    /// Its anchor over a body afloat, its middle at <paramref name="middle"/>: where it is over one upright, so the body leaning or
    /// twisting in flight moves nothing.
    /// </summary>
    public static Vec AnchorAfloat(Vec middle) => Anchor(new Vec(middle.X, middle.Y - (Rig.Neck - Rig.H / 2)), middle.X);

    /// <summary>A hero's velocity in px/ms from its own, thousandths of a cell a step.</summary>
    public static Vec VelocityOf(int vx, int vy) => new(vx * Rig.Cell / Hero.Unit / Hero.StepMs, vy * Rig.Cell / Hero.Unit / Hero.StepMs);

    /// <summary>Forgets where it was: the next <see cref="Step"/> puts it at its goal at rest (hidden, then shown).</summary>
    public void Lose() => _placed = false;

    /// <summary>
    /// Flies it <paramref name="elapsedMs"/> toward <paramref name="goal"/>, its hero's anchor at <paramref name="anchor"/> moving at
    /// <paramref name="hero"/> px/ms: quicker <paramref name="darting"/> and to a <paramref name="window"/>'s place. Where it is now.
    /// </summary>
    public Vec Step(Vec anchor, Vec hero, Vec goal, bool window, bool darting, double elapsedMs)
    {
        if (!_placed || Length(Minus(anchor, _anchor)) > Tune.Lost)
        {
            (_placed, _anchor, At, Velocity, Off) = (true, anchor, goal, default, 0);
            (_pace, _rate) = (default, 1 / Tune.FollowMs);
            return At;
        }
        var moved = Minus(anchor, _anchor);
        _anchor = anchor;
        double frame = Math.Clamp(elapsedMs, 0, Tune.MaxFrameMs), left = frame;
        while (left > 0)
        {
            double dt = Math.Min(left, Tune.StepMs);
            left -= dt;
            // The anchor went along through the frame: chased at the frame's end alone it led by half a frame's way.
            var still = Scaled(moved, left / frame);
            _rate += (Rate(Minus(anchor, still), window, darting) - _rate) * (dt / Tune.EaseMs);
            // Critically damped about the hero's pace as learnt, not as it is: so it lags a turn and then catches up.
            _pace = Plus(_pace, Scaled(Minus(hero, _pace), dt * _rate * Tune.FollowMs / Tune.LearnMs));
            var pull = Plus(Scaled(Minus(Minus(goal, still), At), _rate * _rate), Scaled(Minus(_pace, Velocity), 2 * _rate));
            Velocity = Plus(Velocity, Scaled(pull, dt));
            At = Plus(At, Scaled(Velocity, dt));
        }
        Off = Length(Minus(goal, At));
        return At;
    }

    /// <summary>How quick it answers now, per ms: to a window's place or darting its own, else the quicker the further it is left.</summary>
    private double Rate(Vec anchor, bool window, bool darting)
    {
        if (window) return 1 / Tune.WindowMs;
        if (darting) return 1 / Tune.DartMs;
        double behind = Math.Max(0, Length(Minus(At, anchor)) - Tune.HomeX);
        return (1 + Math.Min(behind / Tune.Far, Tune.Hurry)) / Tune.FollowMs;
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
