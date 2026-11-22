using System;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The light drone's body (drone-spring ADR): where it flies by its hero, in pixels of the world, after the goal its will
/// gives (<see cref="DroneWill"/>). A spring that never swings: it learns the hero's pace a little late, so it falls behind a
/// start or a turn, speeds up past the hero until it is by it again, and keeps by it. Left further it hurries more;
/// nothing holds it on a circle. It flies through walls.
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

        /// <summary>How long it takes to reach a thing it fetches, ms, and how much quicker than its hero it may still be as it is back, px/ms.</summary>
        public const double FetchMs = 45, Fetched = 0.12;

        /// <summary>The longest frame it flies by and the steps it flies one in, ms: a stall never flings it.</summary>
        public const double MaxFrameMs = 50, StepMs = 4;

        /// <summary>It perches aside, px from its anchor: between <see cref="HomeNear"/> and <see cref="HomeX"/> across either way, up to <see cref="HomeY"/> up or down.</summary>
        public const double HomeNear = 12, HomeX = 18, HomeY = 3;

        /// <summary>Under a ceiling or by a wall it slides into the open, no nearer the helmet's middle than this, px.</summary>
        public const double Clear = 13;

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
    /// <summary>Riding a tow: where it is from the anchor, how fast by it, and the goal it was last given there.</summary>
    private Vec _beside, _besideSpeed, _led;
    private bool _riding;

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
    public void Lose() => (_placed, _riding) = (false, false);

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
        _riding = false;
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

    /// <summary>
    /// Flies it <paramref name="elapsedMs"/> after what it fetches (companion-fights), at <paramref name="goal"/>, however far from
    /// the anchor that is; <see cref="Step"/> goes on from there, slowed to its hero's pace.
    /// </summary>
    public Vec Fetch(Vec anchor, Vec hero, Vec goal, double elapsedMs)
    {
        if (!_placed) (_placed, At) = (true, anchor);
        At = Plus(At, Scaled(Minus(goal, At), 1 - Math.Exp(-Math.Min(elapsedMs, Tune.MaxFrameMs) / Tune.FetchMs)));
        (_anchor, _pace, _rate, _riding) = (anchor, hero, 1 / Tune.DartMs, false);
        Velocity = Plus(hero, Capped(Minus(Scaled(Minus(goal, At), 1 / Tune.FetchMs), hero), Tune.Fetched));
        Off = Length(Minus(goal, At));
        return At;
    }

    /// <summary>
    /// Flies it <paramref name="elapsedMs"/> held by a tow (<see cref="DroneTow"/>): to <paramref name="goal"/> from the anchor and carried with it,
    /// as firmly as <paramref name="grip"/>, 0..1. Let go, <see cref="Step"/> goes on with the pace it was carried at.
    /// </summary>
    public Vec Ride(Vec anchor, Vec hero, Vec goal, double grip, double elapsedMs)
    {
        if (!_placed || Length(Minus(anchor, _anchor)) > Tune.Lost)
            (_placed, _riding, _beside, _besideSpeed, _led) = (true, true, goal, default, goal);
        else if (!_riding)
            (_riding, _beside, _besideSpeed, _led) = (true, Minus(At, _anchor), Minus(Velocity, hero), goal);
        double frame = Math.Clamp(elapsedMs, 0, Tune.MaxFrameMs), left = frame, rate = DroneTow.Tune.Rate * grip;
        var moving = frame > 0 ? Scaled(Minus(goal, _led), 1 / frame) : default;
        while (left > 0)
        {
            double dt = Math.Min(left, Tune.StepMs);
            left -= dt;
            var to = Minus(goal, Scaled(moving, left));
            var pull = Plus(Scaled(Minus(to, _beside), rate * rate), Scaled(Minus(moving, _besideSpeed), 2 * DroneTow.Tune.Damped * rate));
            _besideSpeed = Plus(_besideSpeed, Scaled(pull, dt));
            _beside = Plus(_beside, Scaled(_besideSpeed, dt));
        }
        (_anchor, _led, _pace, _rate) = (anchor, goal, hero, 1 / Tune.FollowMs);
        (At, Velocity, Off) = (Plus(anchor, _beside), Plus(hero, _besideSpeed), Length(Minus(goal, _beside)));
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
