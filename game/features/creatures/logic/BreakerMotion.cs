using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// How one breaker flies and fights (breakers-look), from what the sim decided, as its pick page showed it: it turns whole to its
/// hero, never mirrored (the user's: a mirror reads as paper turning), lifts its bands winding up, its slits red, reaches as it darts, reels from a blow and, killed, drifts off
/// as it comes apart. Times in seconds, speeds in pixels a second. Plain C#, tested.
/// </summary>
public sealed class BreakerMotion(double cruise, double phase)
{
    /// <summary>What the sim says of it this frame; <paramref name="Hero"/> the way from its middle to the nearest, <paramref name="Blown"/> a blow landed since the last.</summary>
    public readonly record struct Seen(bool WindingUp, bool Pouncing, int FlightLeft, double Vx, double Vy, (double X, double Y)? Hero, bool Blown = false);

    [Flags]
    public enum Moment
    {
        None = 0,
        WindUp = 1,
        /// <summary>Its dart stopped short of its last tick: it struck a hero.</summary>
        HeroHit = 2,
    }

    /// <summary>How far it comes apart and how long that takes it.</summary>
    public const double Apart = 1.6, ApartS = 1.7;

    /// <summary>How long it holds on to a hero it struck, drawn only.</summary>
    public const double HoldS = 0.45;

    public static readonly TickArt.Rgb Calm = new(255, 225, 150), Acting = TickMotion.Attacking;

    /// <summary>How fast it turns round, in radians a second: never mirrored, a ball turns whole.</summary>
    public const double TurnRate = 9;

    private const double FlashS = 0.09, ShakeS = 0.12, FollowS = 0.1, DriftS = 0.17, DriftPixels = 15;

    private bool _seen, _wasWinding, _wasPouncing;
    private int _flightLeft;
    private double _sinceHit = 99, _sinceHeld = 99, _fly, _tell, _grip, _red, _dying;
    private (double X, double Y) _away;

    public double Clock { get; set; } = phase;

    /// <summary>The way its arms point, radians from the right, down positive.</summary>
    public double Angle { get; private set; }

    /// <summary>A blow's shake across, in pixels.</summary>
    public double Shake { get; private set; }

    public double Flash => Math.Max(0, 1 - _sinceHit / FlashS) * 0.9;

    /// <summary>Killed and coming apart.</summary>
    public bool Dead { get; private set; }

    /// <summary>Come apart to nothing.</summary>
    public bool Gone => _dying >= ApartS;

    /// <summary>How far it has drifted since it was killed, in pixels.</summary>
    public (double X, double Y) Drift { get; private set; }

    public BreakerPose Pose { get; private set; }

    /// <summary>Turned to <paramref name="angle"/> at once: as it was at work.</summary>
    public void Turn(double angle) => Angle = angle;

    public void Struck() => _sinceHit = 0;

    /// <summary>Killed by a blow along <paramref name="away"/>.</summary>
    public void Die((double X, double Y) away)
    {
        double far = Math.Sqrt(away.X * away.X + away.Y * away.Y);
        (Dead, _away) = (true, far > 0.01 ? (away.X / far, away.Y / far) : (0, 0));
    }

    /// <summary>A frame of <paramref name="dt"/> seconds of it as <paramref name="s"/>; answers the moments that came.</summary>
    public Moment Step(Seen s, double dt)
    {
        Clock += dt;
        _sinceHit += dt;
        _sinceHeld += dt;
        if (Dead) return Dies(dt);
        var moments = Moment.None;
        if (_seen)
        {
            if (s.WindingUp && !_wasWinding) moments |= Moment.WindUp;
            // Stopped with ticks to go it struck a hero, unless a blow cut it.
            if (_wasPouncing && !s.Pouncing && _flightLeft > 1 && !s.Blown)
            {
                moments |= Moment.HeroHit;
                _sinceHeld = 0;
            }
        }
        (_seen, _wasWinding, _wasPouncing) = (true, s.WindingUp, s.Pouncing);
        if (s.Pouncing) _flightLeft = s.FlightLeft;

        double speed = Math.Sqrt(s.Vx * s.Vx + s.Vy * s.Vy);
        bool acting = s.WindingUp || s.Pouncing, reeling = _sinceHit < 0.8;
        // Arms at its hero; with none, along its flight.
        double? toward = reeling ? null
            : s.Hero is { } hero && hero.X * hero.X + hero.Y * hero.Y > 4 ? Math.Atan2(hero.Y, hero.X)
            : speed > 25 ? Math.Atan2(s.Vy, s.Vx) : null;
        if (toward is { } aim)
        {
            double turn = Math.IEEERemainder(aim - Angle, Math.Tau) * Follow(dt, FollowS);
            Angle = Math.IEEERemainder(Angle + Math.Clamp(turn, -dt * TurnRate, dt * TurnRate), Math.Tau);
        }
        _fly += (Math.Min(1, speed / cruise) - _fly) * Follow(dt, FollowS);
        _tell += ((s.WindingUp ? 1 : 0) - _tell) * Follow(dt, s.WindingUp ? 0.12 : 0.05);
        _grip += ((s.Pouncing && s.FlightLeft <= 3 || _sinceHeld < HoldS ? 1 : 0) - _grip) * Follow(dt, 0.05);
        _red += ((acting ? 1 : 0) - _red) * Follow(dt, 0.04);
        double hurt = Hurt();
        Shake = _sinceHit < ShakeS ? Math.Sin(Clock * 90) * 0.5 : 0;
        Pose = new BreakerPose(Clock, 0, _fly, _tell, _grip, hurt, Lamp: 0.85 + 0.15 * _red, Tint: TickArt.Rgb.Mix(Calm, Acting, _red));
        return moments;
    }

    private double Hurt() => TickMotion.Ease(_sinceHit / 0.07) * Math.Exp(-3.5 * Math.Max(0, _sinceHit - 0.07));

    private Moment Dies(double dt)
    {
        _dying += dt;
        double apart = Apart * (1 - Math.Pow(1 - Math.Clamp(_dying / ApartS, 0, 1), 1.4)), off = DriftPixels * (1 - Math.Exp(-_dying / DriftS));
        Drift = (_away.X * off, _away.Y * off);
        Shake = 0;
        Pose = Pose with { T = Clock, Work = 0, Hurt = Hurt(), Dead = Math.Max(apart, 1e-6), Lamp = 0 };
        return Moment.None;
    }

    private static double Follow(double dt, double tau) => 1 - Math.Exp(-dt / tau);
}
