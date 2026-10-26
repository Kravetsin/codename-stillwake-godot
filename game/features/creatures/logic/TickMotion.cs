using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// How one tick moves (the sentry page's candidate 3), from what the sim decided: clamped on its wreck it watches, glancing
/// about, its legs stepping as it crawls along; alerted its eye goes yellow with a start; winding up it crouches, trembling, its
/// eye red and stuttering; launched it folds its legs and noses along its flight under its burner; clamped again it grips with a
/// bounce; struck it flashes white and shakes; killed it crouches while its cracks spread. Its moments (<see cref="Moment"/>)
/// are the view's to be seen and heard. Times in seconds, speeds in pixels a second, the page's numbers. Plain C#, tested.
/// </summary>
public sealed class TickMotion(Func<double> random)
{
    /// <summary>
    /// What the sim says of it this frame: clamped on, alerted, winding up (<paramref name="WindUpS"/> seconds into it), flying a
    /// pounce (<paramref name="FlightLeft"/> of its ticks still to go, none at 0), its speed, how fast it crawls along its wreck,
    /// and the way to the hero nearest it from its middle (none for no hero).
    /// </summary>
    public readonly record struct Seen(
        bool Clamped, bool Alerted, bool WindingUp, double WindUpS, bool Pouncing, int FlightLeft,
        double Vx, double Vy, double Crawl, (double X, double Y)? Hero);

    /// <summary>Its moments, seen and heard once each as it comes.</summary>
    [Flags]
    public enum Moment
    {
        None = 0,
        Notice = 1,
        WindUp = 2,
        Unclamp = 4,
        Clamp = 8,
        Brake = 16,
        /// <summary>Its pounce stopped short: it struck a hero.</summary>
        HeroHit = 32,
    }

    public static readonly TickArt.Rgb Watching = new(64, 242, 217), Alerted = new(255, 209, 51), Attacking = new(255, 56, 46);

    /// <summary>The page's: its crouch, its unfolding at launch, its eye's red after a blow, its flash and shake.</summary>
    public const double CrouchS = 0.3, LaunchS = 0.06, EyeHitS = 0.15, FlashS = 0.09, ShakeS = 0.12;

    /// <summary>How long its death's cracks take to spread before it bursts (the page's <c>crack</c>).</summary>
    public const double CracksS = 0.32;

    private Seen _was;
    private bool _seen;
    private double _clock, _sinceAlert = 99, _sinceClamp = 99, _sinceLaunch = 99, _sinceHit = 99, _sinceRed = 99, _gait;
    private int _flightLeft;
    private TickArt.Rgb _eye = Watching;
    private double _crouch, _fly, _walk, _thrust, _angle;

    /// <summary>The way it faces: 1 right, −1 left.</summary>
    public int Face { get; private set; } = 1;

    /// <summary>Its turn about its middle, radians: along its flight, none clamped.</summary>
    public double Angle => _angle;

    /// <summary>How hard its burner burns, 0..1, and the way its plume goes (a unit vector).</summary>
    public double Thrust => _thrust;
    public (double X, double Y) Plume { get; private set; } = (-1, 0);

    /// <summary>Its speed as last seen, pixels a second: what its plume's sparks are thrown with.</summary>
    public (double X, double Y) Velocity { get; private set; }

    /// <summary>Its tremble or shake across, in pixels.</summary>
    public double Shake { get; private set; }

    /// <summary>A blow's white flash over it, 0..0.9.</summary>
    public double Flash => Math.Max(0, 1 - _sinceHit / FlashS) * 0.9;

    /// <summary>Its feet's magnets' glow, 0..1: on while it grips.</summary>
    public double Mags { get; private set; } = 1;

    /// <summary>Whether it acts: crouched or flying at a hero, its lamp the brighter.</summary>
    public bool Hot { get; private set; }

    /// <summary>How far through its wind-up it is, 0..1: its arcs come the thicker.</summary>
    public double Charge { get; private set; }

    /// <summary>Seconds since it was killed; none alive.</summary>
    public double? Dying { get; private set; }

    /// <summary>Its pose for <see cref="TickArt"/>.</summary>
    public TickArt.Pose Pose { get; private set; }

    /// <summary>Struck, as this player's own blow goes or a friend's shows: a white flash, a shake, its eye red a moment.</summary>
    public void Struck() => (_sinceHit, _sinceRed) = (0, 0);

    /// <summary>It cut a line on its wreck: its eye red a moment, no flash.</summary>
    public void Snipped() => _sinceRed = 0;

    /// <summary>Killed where it is: from now it crouches, its eye stuttering red, its cracks spreading.</summary>
    public void Die() => Dying ??= 0;

    /// <summary>A frame of <paramref name="dt"/> seconds of it as <paramref name="s"/>; answers the moments that came.</summary>
    public Moment Step(Seen s, double dt)
    {
        _clock += dt;
        _sinceAlert += dt;
        _sinceClamp += dt;
        _sinceLaunch += dt;
        _sinceHit += dt;
        _sinceRed += dt;
        if (Dying is double dying) return Dies(dying + dt, dt);
        var moments = _seen ? MomentsOf(s) : Moment.None;
        (_was, _seen) = (s, true);
        if (s.Pouncing) _flightLeft = s.FlightLeft;
        Velocity = (s.Vx, s.Vy);
        bool flying = !s.Clamped;
        double speed = Math.Sqrt(s.Vx * s.Vx + s.Vy * s.Vy);

        if (flying && Math.Abs(s.Vx) > 1) Face = Math.Sign(s.Vx);
        else if (!flying && (s.WindingUp || s.Alerted || _sinceHit < 1) && s.Hero is { } toHero && Math.Abs(toHero.X) > 2) Face = Math.Sign(toHero.X);
        else if (!flying && Math.Abs(s.Crawl) > 1) Face = Math.Sign(s.Crawl);

        double crouch = 0;
        if (s.WindingUp) crouch = Ease(s.WindUpS / CrouchS);
        else if (s.Pouncing && _sinceLaunch < LaunchS) crouch = 1 - _sinceLaunch / LaunchS;
        else if (s.Clamped && _sinceClamp < 1) crouch = 0.7 * Math.Exp(-_sinceClamp * 7) * Math.Cos(_sinceClamp * 16);
        else if (s.Alerted && _sinceAlert > 0.22 && _sinceAlert < 0.5) crouch = -0.5 * Math.Sin((_sinceAlert - 0.22) / 0.28 * Math.PI);
        _crouch += (crouch - _crouch) * Follow(dt, 0.02);

        double fly = flying ? (s.Pouncing && _sinceLaunch < LaunchS ? _sinceLaunch / LaunchS : 1) : 0;
        _fly += (fly - _fly) * Follow(dt, flying ? 0.02 : 0.04);
        _walk += ((flying ? 0 : Math.Clamp(Math.Abs(s.Crawl) / 12, 0, 1)) - _walk) * Follow(dt, 0.1);
        if (!flying) _gait += Math.Abs(s.Crawl) * dt * 0.8;
        Mags = flying ? 0 : Math.Clamp(_sinceClamp / 0.05, 0, 1);

        double angle = 0;
        if (flying && speed > 1) angle = Math.Atan2(Face * s.Vy, Face * s.Vx);
        else if (flying) angle = _angle;
        _angle += (angle - _angle) * Follow(dt, 0.04);

        double thrust = 0;
        if (s.Pouncing) thrust = _sinceLaunch < LaunchS ? _sinceLaunch / LaunchS * 0.6 : 1;
        else if (flying && speed > 1) thrust = 0.75;
        _thrust += (thrust - _thrust) * Follow(dt, 0.03);
        Plume = speed > 1 ? (-s.Vx / speed, -s.Vy / speed) : (-Face * Math.Cos(_angle), -Face * Math.Sin(_angle));

        Hot = s.WindingUp || s.Pouncing;
        Charge = s.WindingUp ? Math.Clamp(s.WindUpS / 0.8, 0, 1) : 0;
        var eye = _sinceRed < EyeHitS || Hot ? Attacking
            : flying || s.Alerted || _sinceClamp < 0.3 ? Alerted
            : Watching;
        _eye = TickArt.Rgb.Mix(_eye, eye, Follow(dt, s.Alerted && _sinceAlert < 0.1 ? 0.03 : 0.04));
        double amp = s.WindingUp ? (s.WindUpS < 0.45 ? (random() < 0.35 ? 0.3 : 1.4) : 1.6)
            : s.Pouncing ? 1.5
            : _sinceHit < 1 ? 1.2
            : flying ? 1.1
            : s.Alerted ? 1.15 + 0.25 * Math.Exp(-_sinceAlert * 6)
            : 0.9 + 0.1 * Math.Sin(_clock * 3);
        double look = flying ? 1
            : (s.Alerted || s.WindingUp || _sinceHit < 1) && s.Hero is { } h ? Math.Sign(h.X) * Face
            : _walk > 0.2 ? 0.8
            : Math.Sin(_clock * 1.7) * 1.2;
        Shake = s.WindingUp ? (random() - 0.5) * 0.5 * _crouch : _sinceHit < ShakeS ? (random() - 0.5) * 0.8 : 0;
        Pose = new TickArt.Pose(_crouch, _walk, _gait, _fly, _eye, amp, look);
        return moments;
    }

    private Moment MomentsOf(Seen s)
    {
        var m = Moment.None;
        if (s.Alerted && !_was.Alerted && !s.WindingUp)
        {
            m |= Moment.Notice;
            _sinceAlert = 0;
        }
        if (s.WindingUp && !_was.WindingUp) m |= Moment.WindUp;
        if (!s.Clamped && _was.Clamped)
        {
            m |= Moment.Unclamp;
            _sinceLaunch = 0;
        }
        if (s.Clamped && !_was.Clamped)
        {
            m |= Moment.Clamp;
            _sinceClamp = 0;
        }
        if (_was.Pouncing && !s.Pouncing)
        {
            m |= Moment.Brake;
            // A flight ends on its last tick; one that stopped with ticks to go struck a hero (HitBy ends it).
            if (_flightLeft > 1) m |= Moment.HeroHit;
        }
        return m;
    }

    /// <summary>Its death: crouching, its eye red and stuttering, its cracks spreading over <see cref="CracksS"/>.</summary>
    private Moment Dies(double dying, double dt)
    {
        Dying = dying;
        _crouch += (1 - _crouch) * Follow(dt, 0.08);
        _eye = Attacking;
        _thrust += (0 - _thrust) * Follow(dt, 0.03);
        Hot = true;
        Charge = 0;
        Shake = (random() - 0.5) * 0.5;
        Pose = new TickArt.Pose(_crouch, 0, _gait, _fly, _eye, random() < 0.3 ? 0.5 : 1.5, Pose.Look, Math.Clamp(dying / CracksS, 0, 1));
        return Moment.None;
    }

    /// <summary>The page's <c>ease</c>: smoothstep, clamped.</summary>
    public static double Ease(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>The share of the way to a target closed in <paramref name="dt"/> with time constant <paramref name="tau"/>: the same at any frame rate.</summary>
    private static double Follow(double dt, double tau) => 1 - Math.Exp(-dt / tau);
}
