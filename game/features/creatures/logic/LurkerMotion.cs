using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// How one cave lurker moves (the pick page's cycle), from what the sim decided. The capback lurks hunkered, its moss breathing; it
/// notices dimming, crouches flaring violet, leaps, lands squashed and comes to; struck it shudders and flinches; dying it sags. The
/// geode snail lurks open; it notices laying its eyes back, shuts and rocks, rolls, peeks out; struck it shuts; dying its shell cracks
/// with light. Its moments are the view's to see and hear. Times in seconds. Plain C#, tested.
/// </summary>
public sealed class LurkerMotion(bool snail, double radius)
{
    /// <summary>
    /// What the sim says of it this frame: whether it has noticed a hero, is winding up (<paramref name="WindUpS"/> seconds into it), is
    /// acting; how far it was drawn across since the last frame, in pixels; its speed across in pixels a second; its hurt, 0..1; the way
    /// to the hero nearest it across (none for no hero).
    /// </summary>
    public readonly record struct Seen(bool Alerted, bool WindingUp, double WindUpS, bool Acting, double Dx, double Vx, double Hurt, double? Hero);

    /// <summary>The page's: how long it comes to after acting, how long a blow shows, how long it dies; its blow's white.</summary>
    public const double ComeToS = 1.1, StruckS = 0.5, DieS = 0.9, FlashTau = 0.04;

    /// <summary>How fast a capback's legs stride, strides a pixel.</summary>
    private const double StridePerPx = 1 / 14.0;

    private Seen _was;
    private bool _seen;
    private double _clock, _sinceAct = 99, _sinceHit = 99, _spinTo;
    private LurkerArt.Pose _p = new(Hunk: 1, Glow: 0.6);

    /// <summary>The way it faces: 1 right, −1 left.</summary>
    public int Face { get; set; } = -1;

    /// <summary>A blow's white flash over it, 0..1.</summary>
    public double Flash { get; private set; }

    /// <summary>Seconds since it began to die; none alive.</summary>
    public double? Dying { get; private set; }

    /// <summary>Its pose for <see cref="LurkerRig"/>.</summary>
    public LurkerArt.Pose Pose => _p;

    /// <summary>Struck, as this player's own blow goes or a friend's shows.</summary>
    public void Struck()
    {
        _sinceHit = 0;
        Flash = 1;
        _p = snail ? _p with { Rock = -0.12 } : _p with { Shudder = 1, Flinch = 1 };
    }

    /// <summary>From now it dies where it is.</summary>
    public void Die() => Dying ??= 0;

    /// <summary>A frame of <paramref name="dt"/> seconds of it as <paramref name="s"/>; answers whether its act ended this frame: a capback landing.</summary>
    public bool Step(Seen s, double dt)
    {
        _clock += dt;
        _sinceAct += dt;
        _sinceHit += dt;
        Flash *= Math.Exp(-dt / FlashTau);
        if (Flash < 0.01) Flash = 0;
        if (Dying is double dying)
        {
            Dying = dying + dt;
            Dies(dying + dt, dt);
            return false;
        }
        bool ended = _seen && _was.Acting && !s.Acting;
        if (ended)
        {
            _sinceAct = 0;
            if (!snail) _p = _p with { Squash = 1, Fly = 0 };
        }
        (_was, _seen) = (s, true);
        if (s.Acting && Math.Abs(s.Vx) > 1) Face = Math.Sign(s.Vx);
        else if (!s.Acting && s.Hero is double hero && Math.Abs(hero) > 2 && (s.Alerted || s.WindingUp)) Face = Math.Sign(hero);
        if (snail) Snail(s, dt);
        else Capback(s, dt);
        return ended;
    }

    /// <summary>The capback's page states: lurk, notice, wind, leap, recover, struck.</summary>
    private void Capback(Seen s, double dt)
    {
        double tau = 0.2, walk = 0;
        LurkerArt.Pose to;
        if (s.Acting) (to, tau) = (_p with { Hunk = 0, Crouch = 0, Fly = 1, Flare = 0.8, Glow = 1.1, Shudder = 0, EyeLit = 0.6 }, 0.025);
        else if (s.WindingUp) (to, tau) = (_p with { Hunk = 0, Crouch = 1, Alert = 1, Glow = 1.25, Flare = 1, Shudder = 0.35, EyeLit = 1 }, 0.045);
        else if (_sinceHit < StruckS) (to, tau) = (_p with { Shudder = 0, Flinch = 0, Hunk = 0.2, Alert = 1, Glow = 0.55, Flare = 0, Crouch = 0, EyeLit = 0.9 }, 0.11);
        else if (_sinceAct < ComeToS) (to, tau) = (_p with { Fly = 0, Squash = 0, Hunk = 0.25, Alert = 0.6, Glow = 0.45, Flare = 0, Crouch = 0, EyeLit = 0.8 }, 0.22);
        else if (s.Alerted) (to, tau) = (_p with { Hunk = 0.88, Alert = 1, Glow = 0.2, Flare = 0, Crouch = 0, Fly = 0, Squash = 0, EyeLit = 1 }, 0.09);
        else
        {
            to = _p with { Hunk = 1, Alert = 0, Glow = 0.55 + 0.2 * Math.Sin(_clock * 1000 / 650), Flare = 0, Crouch = 0, Fly = 0, Squash = 0, EyeLit = 0.85 };
            walk = Math.Abs(s.Vx) > 1 ? 1 : 0;
        }
        double lean = s.WindingUp ? Math.Sin(_clock * 1000 / 24) * 0.015 * _p.Crouch : 0;
        _p = Follow(_p, to, dt, tau) with
        {
            Lean = lean,
            Walk = _p.Walk + (walk - _p.Walk) * Toward(dt, 0.1),
            Gait = _p.Gait + Math.Abs(s.Dx) * StridePerPx,
            Clock = _p.Shudder > 0.01 ? _clock * 1000 : 0,
        };
    }

    /// <summary>The geode snail's page states: lurk, notice, wind, roll, peek, struck; its cracks as it is hurt.</summary>
    private void Snail(Seen s, double dt)
    {
        double tau = 0.2, rock = 0, crack = Math.Clamp(s.Hurt, 0, 1) * 0.6;
        LurkerArt.Pose to;
        if (s.Acting)
        {
            (to, tau) = (_p with { Stalk = 1, Retract = 1, Lid = 1, Glow = 0.9, EyeLit = 0 }, 0.018);
            // Rolled forward in its own frame, the way it faces: a turn a radius rolled.
            _spinTo = _p.Spin + s.Dx * Face / radius;
        }
        else if (s.WindingUp)
        {
            (to, tau) = (_p with { Stalk = 1, Retract = 1, Lid = 1, Glow = 0.9, EyeLit = 0 }, 0.018);
            rock = Math.Sin(s.WindUpS * Math.Tau * 3.4) * 0.14 * Math.Min(1, s.WindUpS / 0.25);
        }
        else if (_sinceHit < StruckS) (to, tau) = (_p with { Lid = 1, Retract = 1, Stalk = 1, Glow = 0.9, EyeLit = 0 }, _sinceHit < 0.2 ? 0.03 : 0.09);
        else if (_sinceAct < ComeToS) (to, tau) = (_p with { Lid = 0.15, Retract = 0.3, Stalk = 0.25, Glow = 1, EyeLit = 0.8 }, 0.16);
        else if (s.Alerted) (to, tau) = (_p with { Stalk = 1, Retract = 0.35, Lid = 0, Glow = 0.9, EyeLit = 0 }, 0.08);
        else (to, tau) = (_p with { Retract = 0, Lid = 0, Stalk = 0, Glow = 1, EyeLit = 1 }, 0.2);
        // Its roll over, it rolls on to rest mouth forward: on the next whole turn.
        if (!s.Acting) _spinTo = Math.Ceiling(_spinTo / Math.Tau - 1e-9) * Math.Tau;
        _p = Follow(_p, to, dt, tau) with
        {
            Spin = s.Acting ? _spinTo : _p.Spin + (_spinTo - _p.Spin) * Toward(dt, 0.15),
            Rock = s.WindingUp ? rock : _p.Rock * (1 - Toward(dt, _sinceHit < StruckS ? 0.03 : 0.06)),
            Crack = _p.Crack + (crack - _p.Crack) * Toward(dt, 0.12),
            Clock = _clock * 1000,
        };
    }

    /// <summary>The page's death: the capback sags and goes dark, the snail shuts and its cracks fill with light, rocking.</summary>
    private void Dies(double dying, double dt)
    {
        double q = Math.Clamp(dying / DieS, 0, 1);
        if (snail)
            _p = Follow(_p, _p with { Lid = 1, Retract = 1, Stalk = 1, Glow = 0.7, EyeLit = 0 }, dt, 0.12) with
            {
                Crack = Math.Min(1, dying / 0.6),
                Rock = Math.Sin(_clock * 1000 / 30) * 0.03 * q,
                Clock = _clock * 1000,
            };
        else
            _p = Follow(_p, _p with { Shudder = 0, Sag = 1, Hunk = 0, Alert = 0, Glow = dying < 0.6 ? 0.8 : 0, Crouch = 0, Flare = 0, Fly = 0, EyeLit = 0.5 * (1 - q) }, dt, 0.26)
                with
            { Clock = _clock * 1000 };
    }

    /// <summary>Every number of <paramref name="at"/> eased toward <paramref name="to"/>'s with time constant <paramref name="tau"/>, the same at any frame rate.</summary>
    private static LurkerArt.Pose Follow(LurkerArt.Pose at, LurkerArt.Pose to, double dt, double tau)
    {
        double k = Toward(dt, tau);
        static double M(double a, double b, double k) => Math.Abs(b - a) < 0.001 ? b : a + (b - a) * k;
        return new LurkerArt.Pose(M(at.Hunk, to.Hunk, k), M(at.Alert, to.Alert, k), M(at.Crouch, to.Crouch, k), M(at.Fly, to.Fly, k), M(at.Sag, to.Sag, k),
            M(at.Squash, to.Squash, k), M(at.Flinch, to.Flinch, k), to.Lean, M(at.Shudder, to.Shudder, k), at.Walk, at.Gait, M(at.Retract, to.Retract, k),
            M(at.Stalk, to.Stalk, k), M(at.Lid, to.Lid, k), at.Spin, at.Rock, M(at.Glow, to.Glow, k), M(at.Flare, to.Flare, k), M(at.EyeLit, to.EyeLit, Toward(dt, 0.08)),
            at.Crack, at.Clock);
    }

    /// <summary>The share of the way an eased number goes in <paramref name="dt"/> with time constant <paramref name="tau"/>.</summary>
    private static double Toward(double dt, double tau) => 1 - Math.Exp(-dt / tau);
}
