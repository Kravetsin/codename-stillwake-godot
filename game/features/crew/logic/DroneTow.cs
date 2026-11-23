using System;
using Stillwake.Game.Shared;
using F = Stillwake.Game.Features.Crew.DroneFlight;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The light drone as a tug (drone-tow.js, candidate tug): a hero flown one way for a while, it runs ahead, throws a thread back
/// to the shoulder and heaves; a turn, a brake, a wall or anything that matters more and it lets go, flying on alone and back.
/// Only looks: the hero flies as the jetpack says. Pixels of the world about the body's middle, seconds of its own time.
/// </summary>
public sealed class DroneTow
{
    /// <summary>The pick's numbers, the client's alone: they never enter the sim.</summary>
    public static class Tune
    {
        /// <summary>How long a hero flies one way before it runs ahead, ms, as picked.</summary>
        public const double WaitMs = 2000;

        /// <summary>The share of the jetpack's top that is a flight, how far one way may wander, and how far it wanders before it lets go, radians.</summary>
        public const double Fast = 0.8, OneWay = Math.PI / 15, LetGo = Math.PI / 12.8;

        /// <summary>A way steeper than this pulls by the rear shoulder, up or down along the back.</summary>
        public const double Steep = 0.85;

        /// <summary>It is ahead by Arrive, throws from then until Caught, rides from then by Out, and heaves from Heaves, s.</summary>
        public const double Arrive = 0.85, Caught = 1.3, Out = 1.75, Heaves = 1.7;

        /// <summary>A heave's and a glance back's period, s.</summary>
        public const double HeaveEvery = 1.3, GlanceEvery = 3.9;

        /// <summary>The spring it rides by, per ms, how damped, and how long its grip takes, s.</summary>
        public const double Rate = 0.014, Damped = 0.8, GripIn = 0.3;

        /// <summary>It flies on alone and back this long, s, so far further on, px; the thread reels in, s.</summary>
        public const double Leave = 1.05, On = 12, Reel = 0.3;

        /// <summary>How far apart a wall is looked for along the thread's way, px.</summary>
        public const double Probe = 4;

        /// <summary>How long its looks ease, s.</summary>
        public const double MoodEase = 0.11;
    }

    /// <summary>How many points a thread is drawn by.</summary>
    public const int Points = 17;

    /// <summary>From the anchor down to the body's middle afloat, px.</summary>
    internal const double MiddleDown = F.Tune.Above + Rig.Neck - Rig.H / 2;

    private static readonly double Flying = Tune.Fast * F.Length(F.VelocityOf(Content.Jetpack.Top, 0));

    private Vec _heading, _way, _from, _goal, _leftAt, _middle;
    private double _ranMs, _q, _left = double.PositiveInfinity, _slack, _heaved;
    private bool _steep, _detours, _thrown, _seen;
    private int _side;

    /// <summary>Whether it has hold of its hero's flight: it flies to <see cref="Ahead"/> and not by its will.</summary>
    public bool Riding { get; private set; }

    /// <summary>Whether the thread has caught the shoulder and is held.</summary>
    public bool Caught => Riding && _q >= Tune.Caught;

    /// <summary>Where it rides, from its hero's anchor, px.</summary>
    public Vec Ahead => new(_goal.X, _goal.Y + MiddleDown);

    /// <summary>How firmly it rides, 0..1, eased in as it sets off.</summary>
    public double Grip => Span(_q, 0, Tune.GripIn);

    /// <summary>How hard it heaves now, 0..1: what puffs, squints, hurries and makes the thread buzz.</summary>
    public double Heave => Riding ? HeaveAt(_q) : 0;

    /// <summary>The way it pulls, a unit vector.</summary>
    public Vec Way => _way;

    /// <summary>Whether a thread is out, thrown, held or reeling in.</summary>
    public bool Threaded => Riding ? _q >= Tune.Arrive : _thrown && _left < Tune.Reel;

    /// <summary>Eased: how much flatter its orbits are (1 as drawn at rest), how tilted, and how far its eye is shut.</summary>
    public double Flat { get; private set; } = 1;

    /// <inheritdoc cref="Flat"/>
    public double Tilt { get; private set; }

    /// <inheritdoc cref="Flat"/>
    public double Squint { get; private set; }

    /// <summary>Eased: the body drawn tighter and the orbits hurried, by how hard it heaves, 0..1.</summary>
    public double Effort { get; private set; }

    /// <summary>Forgets the flight: hidden, or its hero jumped.</summary>
    public void Lose()
    {
        (Riding, _ranMs, _left, _thrown, _heading, _seen) = (false, 0, double.PositiveInfinity, false, default, false);
        (Flat, Tilt, Squint, Effort) = (1, 0, 0, 0);
    }

    /// <summary>
    /// A frame of it: the hero's <paramref name="anchor"/> afloat moving at <paramref name="hero"/> px/ms, faced <paramref name="facing"/>, the drone
    /// at <paramref name="at"/>, whether it <paramref name="may"/> (nothing matters more) and what stops light on the surface.
    /// </summary>
    public void Step(Vec anchor, Vec hero, int facing, Vec at, bool may, Func<double, double, bool>? stops, double ms)
    {
        double speed = F.Length(hero), s = ms / 1000;
        var now = speed >= Flying ? F.Scaled(hero, 1 / speed) : default;
        bool fast = speed >= Flying;
        var middle = new Vec(anchor.X, anchor.Y + MiddleDown);
        // The drone is where the last frame left it, so it is measured from the last frame's middle.
        var beside = F.Minus(at, _seen ? _middle : middle);
        (_middle, _seen) = (middle, true);
        _left += s;
        if (Riding)
        {
            _q += s;
            if (!may || !fast || F.Dot(now, _way) < Math.Cos(Tune.LetGo) || Walled(middle, stops))
            {
                (Riding, _ranMs, _left, _heading) = (false, 0, 0, default);
                (_leftAt, _detours, _thrown, _heaved) = (beside, may, _q >= Tune.Arrive, HeaveAt(_q));
            }
            else _goal = GoalAt(_q);
        }
        else if (!may || !fast)
        {
            (_ranMs, _heading) = (0, default);
            if (!may) _detours = false;
        }
        else
        {
            if (F.Dot(now, _heading) < Math.Cos(Tune.OneWay)) (_heading, _ranMs) = (now, 0);
            else _ranMs += ms;
            if (_ranMs >= Tune.WaitMs) Hook(now, facing, middle, beside, stops);
        }
        Looks(s);
    }

    /// <summary>Where it flies by its will's <paramref name="free"/> goal, let go of its hero: on alone, then round the head and back.</summary>
    public Vec Back(Vec anchor, Vec free)
    {
        if (Riding || !_detours || _left >= Tune.Leave) return free;
        var middle = new Vec(anchor.X, anchor.Y + MiddleDown);
        double k = _left / Tune.Leave;
        var way = Round(_leftAt, F.Minus(free, middle), Smooth(k));
        return F.Plus(middle, F.Plus(way, F.Scaled(_way, Tune.On * Bump(k * 1.5))));
    }

    /// <summary>
    /// Where the thread holds on the body upright, from its middle, px: the front shoulder's edge flying level, the rear's up or down;
    /// the same side of the world however the hero turns.
    /// </summary>
    public Vec Hold => _steep ? new Vec(_side * 4.1, Rig.H / 2 - 27.3) : new Vec(_side * 4.2, Rig.H / 2 - 24.6);

    /// <summary>
    /// The thread as drawn, <see cref="Points"/> of it from the drone at <paramref name="drone"/> toward the shoulder drawn at
    /// <paramref name="hold"/>, at <paramref name="timeS"/>: sagging between heaves, taut and buzzing in one. Its length.
    /// </summary>
    public double Thread(Vec drone, Vec hold, double timeS, Span<Vec> into)
    {
        var bow = _steep ? new Vec(_side, 0) : new Vec(0, 1);
        Vec tip;
        double slack, buzz = 0;
        if (Riding)
        {
            double k = Span(_q, Tune.Arrive, Tune.Caught);
            tip = F.Plus(Mixed(drone, hold, k), F.Scaled(bow, 7 * Bump(k)));
            double e = HeaveAt(_q);
            slack = Lerp(0.09 * F.Length(F.Minus(tip, drone)), 4.5 * (1 - e) * (1 - e), Span(_q, Tune.Caught, Tune.Caught + 0.45));
            (_slack, buzz) = (slack, e);
        }
        else
        {
            double k = Span(_left, 0, Tune.Reel);
            tip = F.Plus(Mixed(hold, drone, k), F.Scaled(bow, (_steep ? 4 : 5) * Bump(k)));
            slack = _slack * (1 - k);
        }
        var along = F.Minus(tip, drone);
        double far = F.Length(along), length = far + slack;
        var across = far > 1e-6 ? new Vec(-along.Y / far, along.X / far) : new Vec(0, 0);
        double sag = Math.Sqrt(0.375 * length) * slack / Math.Sqrt(slack + 1.5) * Math.Clamp(2 * F.Dot(bow, across), -1, 1);
        double wave = 0.3 * Math.Min(2.4, slack * 0.5);
        for (int j = 0; j < into.Length; j++)
        {
            double u = j / (double)(into.Length - 1), e = Math.Sin(Math.PI * u);
            double off = sag * 4 * u * (1 - u) + wave * e * Math.Sin(u * 8 - timeS * 11) + buzz * 0.55 * e * Math.Sin(timeS * 47);
            into[j] = F.Plus(drone, F.Plus(F.Scaled(along, u), F.Scaled(across, off)));
        }
        return length;
    }

    /// <summary>
    /// A puff of sparks <paramref name="ageS"/> old: how bright, 0..1, and how far it was left along the way by the drone heaving on since.
    /// </summary>
    public (double Heat, double Along) Puff(double ageS)
    {
        if (Riding) return (HeaveAt(_q - ageS), 9 * (HeaveAt(_q - ageS) - HeaveAt(_q)));
        return (_thrown ? _heaved * (1 - Span(_left, 0, Tune.Reel)) : 0, 0);
    }

    /// <summary>Where its eye turns from its will's or its mode's look: ahead, back at the throw, glancing back each few heaves; none otherwise.</summary>
    public Vec? Eye(Vec at, Vec shoulder)
    {
        if (!Riding) return null;
        var back = F.Minus(shoulder, at);
        double far = F.Length(back);
        if (far < 1) return _way;
        back = F.Scaled(back, 1 / far);
        if (_q < 1.4) return _q < 0.6 ? _way : back;
        double p = Frac(Math.Max(0, _q - Tune.Heaves) / Tune.GlanceEvery), k = Span(p, 0.7, 0.76) * (1 - Span(p, 0.9, 0.97));
        return k > 0.5 ? back : _way;
    }

    private void Hook(Vec now, int facing, Vec middle, Vec beside, Func<double, double, bool>? stops)
    {
        _steep = Math.Abs(now.Y) > Tune.Steep;
        _side = _steep ? (facing < 0 ? 1 : -1) : Math.Sign(now.X);
        _way = now;
        if (Walled(middle, stops)) return;
        (Riding, _q, _slack) = (true, 0, 0);
        _from = _goal = beside;
    }

    private Vec GoalAt(double q)
    {
        var near = Near;
        return q < Tune.Arrive ? Round(_from, near, Span(q, 0, Tune.Arrive)) : Mixed(near, Life(HeaveAt(q), Math.Sin(q * 41) * 0.5), Span(q, Tune.Caught, Tune.Out));
    }

    /// <summary>Where it throws from.</summary>
    private Vec Near => _steep ? F.Plus(Hold, new Vec(_side * 20, Down(31))) : F.Plus(Hold, new Vec(_side * 29, Rise + 1));

    private Vec Life(double heave, double shake)
    {
        shake *= heave;
        return _steep ? F.Plus(Hold, new Vec(_side * (19 + shake), Down(31 + 7 * heave))) : F.Plus(Hold, new Vec(_side * (30 + 9 * heave), Rise + shake));
    }

    /// <summary>Level it pulls a little from above; up slantwise higher, down slantwise lower, the thread never over the glass.</summary>
    private double Rise => -15 + (_way.Y > 0 ? 25 : 5) * _way.Y;

    /// <summary>So far along a steep way; a dive reaches past the boots.</summary>
    private double Down(double far) => _way.Y < 0 ? -far : far + 5;

    /// <summary>Whether a wall stands in the thread's way from the middle to the far end of a heave.</summary>
    private bool Walled(Vec middle, Func<double, double, bool>? stops)
    {
        if (stops == null) return false;
        var to = Life(1, 0);
        int steps = (int)Math.Ceiling(F.Length(to) / Tune.Probe);
        for (int i = 1; i <= steps; i++)
        {
            var p = F.Plus(middle, F.Scaled(to, i / (double)steps));
            if (stops(p.X, p.Y)) return true;
        }
        return false;
    }

    /// <summary>Round the body, never through it: over the head flying level, behind the back flying steep.</summary>
    private Vec Round(Vec a, Vec b, double k)
    {
        if (k <= 0) return a;
        if (k >= 1) return b;
        const double Wide = 14, High = 28;
        double avoid = !_steep ? Math.PI / 2 : _side > 0 ? Math.PI : 0;
        double ax = a.X / Wide, ay = a.Y / High, bx = b.X / Wide, by = b.Y / High;
        double th = Lerp(Turn(Math.Atan2(ay, ax) - avoid), Turn(Math.Atan2(by, bx) - avoid), k) + avoid;
        double r = Lerp(Lerp(Math.Sqrt(ax * ax + ay * ay), Math.Sqrt(bx * bx + by * by), k), 1.45, Bump(k) * 0.8);
        return new Vec(Math.Cos(th) * r * Wide, Math.Sin(th) * r * High);
    }

    private void Looks(double s)
    {
        double e = Heave, ease = 1 - Math.Exp(-s / Tune.MoodEase);
        bool working = Riding && _q >= 1.4;
        Effort = Lerp(Effort, working ? e : 0, ease);
        Squint = Lerp(Squint, working ? 0.4 * e : 0, ease);
        Flat = Lerp(Flat, working ? Lerp(1.2, 0.5, e) : !Riding && _left < Tune.Leave ? 0.7 : 1, ease);
        Tilt = Lerp(Tilt, working ? (_steep ? 1.5 : 0.2) : 0, ease);
    }

    private static double HeaveAt(double q)
    {
        double p = Frac(Math.Max(0, q - Tune.Heaves) / Tune.HeaveEvery);
        return p < 0.5 ? Span(p, 0, 0.5) : 1 - Span(p, 0.62, 1);
    }

    private static double Turn(double a) => (a % Math.Tau + Math.Tau) % Math.Tau;

    private static double Frac(double v) => v - Math.Floor(v);

    private static double Bump(double k) => Math.Sin(Math.PI * Math.Clamp(k, 0, 1));

    private static double Lerp(double a, double b, double k) => a + (b - a) * k;

    private static Vec Mixed(Vec a, Vec b, double k) => new(Lerp(a.X, b.X, k), Lerp(a.Y, b.Y, k));

    private static double Smooth(double k)
    {
        k = Math.Clamp(k, 0, 1);
        return k * k * (3 - 2 * k);
    }

    private static double Span(double v, double from, double to) => Smooth((v - from) / (to - from));
}
