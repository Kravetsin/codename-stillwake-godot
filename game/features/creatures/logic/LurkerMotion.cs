using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// How one cave lurker moves (lurker-attacks), from what the sim decided. The capback lurks hunkered, its moss breathing; it notices
/// dimming; venting it squeezes and puffs up, caps flared and throbbing; it crouches flaring violet to spit and recoils; struck it
/// shudders and flinches; dying it sags. The geode snail sits shut, and open while a hero is close; it bites leaning back, then
/// lurching forward; struck it jolts; dying its shell cracks with light. Its moments are the view's to see and hear. Times in seconds.
/// Plain C#, tested.
/// </summary>
public sealed class LurkerMotion(bool snail)
{
    /// <summary>
    /// What the sim says of it this frame: whether it has noticed a hero, is winding up (<paramref name="WindUpS"/> seconds into it), is
    /// acting; how far it was drawn across since the last frame, in pixels; its speed across in pixels a second; its hurt, 0..1; the way
    /// to the hero nearest it across (none for no hero); whether its act is a vent; whether its shell is shut.
    /// </summary>
    public readonly record struct Seen(bool Alerted, bool WindingUp, double WindUpS, bool Acting, double Dx, double Vx, double Hurt, double? Hero,
        bool Venting = false, bool Shut = false);

    /// <summary>How long it comes to after acting, how long a blow shows, how long it dies; its blow's white.</summary>
    public const double ComeToS = 1.1, StruckS = 0.5, DieS = 0.9, FlashTau = 0.04;

    /// <summary>How fast a capback's legs stride, strides a pixel.</summary>
    private const double StridePerPx = 1 / 14.0;

    private Seen _was;
    private bool _seen;
    private double _clock, _sinceAct = 99, _sinceHit = 99;
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

    /// <summary>A frame of <paramref name="dt"/> seconds of it as <paramref name="s"/>; answers whether an act began this frame: a vent, a spit's recoil, a bite.</summary>
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
        bool began = s.Acting && !(_seen && _was.Acting);
        if (_seen && _was.Acting && !s.Acting) _sinceAct = 0;
        // Venting it empties at once: squeezed, then easing back as the gas goes.
        if (began && s.Venting) _p = _p with { Squash = 0.8 };
        (_was, _seen) = (s, true);
        if (s.Acting && Math.Abs(s.Vx) > 1) Face = Math.Sign(s.Vx);
        else if (!s.Acting && s.Hero is double hero && Math.Abs(hero) > 2 && (s.Alerted || s.WindingUp)) Face = Math.Sign(hero);
        if (snail) Snail(s, dt);
        else Capback(s, dt);
        return began;
    }

    /// <summary>The capback's states: lurk, notice, vent, wind up to spit, recoil, come to, struck.</summary>
    private void Capback(Seen s, double dt)
    {
        double tau = 0.2, walk = 0;
        LurkerArt.Pose to;
        if (s.Acting && s.Venting)
            (to, tau) = (_p with { Hunk = 0.3, Alert = 1, Crouch = 0, Fly = 0, Squash = 0, Flare = 0.6 + 0.4 * Math.Sin(_clock * Math.Tau * 6), Glow = 1.25, EyeLit = 1 }, 0.04);
        else if (s.Acting) (to, tau) = (_p with { Hunk = 0.15, Alert = 1, Crouch = 0, Fly = 0, Squash = 0, Flare = 0.5, Glow = 0.85, EyeLit = 1 }, 0.06);
        else if (s.WindingUp) (to, tau) = (_p with { Hunk = 0, Crouch = 1, Alert = 1, Glow = 1.25, Flare = 1, Shudder = 0.35, EyeLit = 1 }, 0.045);
        else if (_sinceHit < StruckS) (to, tau) = (_p with { Hunk = 0.2, Alert = 1, Glow = 0.55, Flare = 0, Crouch = 0, EyeLit = 0.9 }, 0.11);
        else if (_sinceAct < ComeToS) (to, tau) = (_p with { Fly = 0, Squash = 0, Hunk = 0.25, Alert = 0.6, Glow = 0.45, Flare = 0, Crouch = 0, EyeLit = 0.8 }, 0.22);
        else if (s.Alerted) (to, tau) = (_p with { Hunk = 0.88, Alert = 1, Glow = 0.2, Flare = 0, Crouch = 0, Fly = 0, Squash = 0, EyeLit = 1 }, 0.09);
        else
        {
            to = _p with { Hunk = 1, Alert = 0, Glow = 0.55 + 0.2 * Math.Sin(_clock * 1000 / 650), Flare = 0, Crouch = 0, Fly = 0, Squash = 0, EyeLit = 0.85 };
            walk = Math.Abs(s.Vx) > 1 ? 1 : 0;
        }
        // Only a wind-up shakes it: every other pose eases a blow's or a cut wind-up's shudder back to rest.
        if (!s.WindingUp) to = to with { Shudder = 0, Flinch = 0 };
        double lean = s.WindingUp ? Math.Sin(_clock * 1000 / 24) * 0.015 * _p.Crouch : 0;
        _p = Follow(_p, to, dt, tau) with
        {
            Lean = lean,
            Walk = _p.Walk + (walk - _p.Walk) * Toward(dt, 0.1),
            Gait = _p.Gait + Math.Abs(s.Dx) * StridePerPx,
            Clock = _p.Shudder > 0.01 ? _clock * 1000 : 0,
        };
    }

    /// <summary>The geode snail's states: shut, open, bite (lean back, lurch), struck; its cracks as it is hurt.</summary>
    private void Snail(Seen s, double dt)
    {
        double tau, rock = 0, crack = Math.Clamp(s.Hurt, 0, 1) * 0.6;
        var shut = _p with { Lid = 1, Retract = 1, Stalk = 1, Glow = 0.9, EyeLit = 0 };
        var open = _p with { Lid = 0, Retract = 0, Stalk = s.Alerted ? 0.4 : 0, Glow = 1, EyeLit = 1 };
        LurkerArt.Pose to;
        if (s.Acting) (to, tau, rock) = (open with { Stalk = 1, Glow = 1.15 }, 0.03, 0.16);
        else if (s.WindingUp)
            (to, tau, rock) = (open with { Retract = 0.45, Stalk = 0.7, Glow = 1.1 }, 0.06,
                -0.12 * Math.Min(1, s.WindUpS / 0.5) + Math.Sin(s.WindUpS * Math.Tau * 7) * 0.015);
        else if (s.Shut) (to, tau) = (shut, _sinceHit < StruckS ? 0.03 : 0.09);
        else (to, tau) = (open, 0.16);
        bool biting = s.Acting || s.WindingUp;
        _p = Follow(_p, to, dt, tau) with
        {
            Spin = 0,
            Rock = biting ? _p.Rock + (rock - _p.Rock) * Toward(dt, 0.04) : _p.Rock * (1 - Toward(dt, _sinceHit < StruckS ? 0.03 : 0.06)),
            Crack = _p.Crack + (crack - _p.Crack) * Toward(dt, 0.12),
            Clock = _clock * 1000,
        };
    }

    /// <summary>Its death: the capback sags and goes dark, the snail shuts and its cracks fill with light, rocking.</summary>
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
