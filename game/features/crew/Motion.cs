using System;
using Look = Stillwake.Game.HeroMotion;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What the figure reads from how its hero's motion changes, frame by frame (0090): the lean of its acceleration, the
/// skid of a hard stop, a landing's depth and length by how fast it fell, the time since a jump and the time falling.
/// <see cref="Lean"/> radians forward; <see cref="Skid"/> from 0 to 1, <see cref="Along"/> 1 while it slides the way it
/// faces, braking, and -1 sliding backwards, turned; <see cref="Depth"/> the landing's sink as a share of the old one.
/// </summary>
public readonly record struct Momentum(
    double Lean = 0, double Skid = 0, int Along = 1, double Depth = 1, double SinceJumpMs = double.PositiveInfinity, double FallingMs = 0)
{
    /// <summary>A body that has done nothing yet: no lean, no skid, no jump, landing as deep as ever. Not <c>default</c>, all zeros.</summary>
    public static readonly Momentum AtRest = new(Depth: 1);
}

/// <summary>
/// The figure's memory of its hero's motion (0090), from the movement alone: <see cref="Hero.Vx"/>, <see cref="Hero.Vy"/>,
/// <see cref="Hero.Facing"/> and the pose, all a friend's hero sends, so others' heroes lean, skid and land as their own peer
/// draws them. A friend's speed comes a send at a time; the lean's smoothing goes by time, so a change of three steps at
/// once leans the body about as far as three steps one by one.
/// </summary>
public sealed class Motion
{
    /// <summary>
    /// What a frame set off: a jump, a landing and how fast it fell, how many streaks of dust the soles throw, and how many
    /// sparks the gloves throw sliding down a ladder (0092).
    /// </summary>
    public readonly record struct Cues(bool Jumped, int? Landed, int SkidDust, int Sparks = 0);

    private int? _vx;
    private Pose? _was;
    /// <summary>The acceleration across, smoothed, in thousandths of a cell a step gained a step.</summary>
    private double _acceleration;
    private double _skidMs, _skid, _dustOwed, _sparksOwed;
    private int _along = 1, _sliding;
    private double _sinceJumpMs = double.PositiveInfinity, _fallingMs;
    private int _fall;
    private double _landMs, _landTotalMs = Look.Landing.Ms, _depth = 1;

    /// <summary>The motion as the pose reads it now.</summary>
    public Momentum Now { get; private set; } = Momentum.AtRest;

    /// <summary>How far through its landing the figure is, from 1 at touching down to 0; none once done.</summary>
    public double Landing => _landMs / _landTotalMs;

    /// <summary>How fast the hero last fell, in thousandths of a cell a step: how hard it lands.</summary>
    public int Fall => _fall;

    /// <summary>The way the skid slides, -1 or 1 across the world: where its soles' dust flies.</summary>
    public int Sliding => _sliding;

    /// <summary>Whether a landing from a fall of <paramref name="fall"/> is heavy: a wider ring and streaks along the floor.</summary>
    public static bool Heavy(int fall) => fall >= Look.Landing.Heavy;

    /// <summary>A landing's sink as a share of the old one, from a fall of <paramref name="fall"/> at <paramref name="vx"/> across.</summary>
    public static double DepthOf(int fall, int vx) =>
        Math.Clamp((double)fall / Look.Landing.PerDepth, Look.Landing.Least, Look.Landing.Most) * (Math.Abs(vx) > Look.Landing.RunningSpeed ? Look.Landing.Running : 1);

    /// <summary>How long a landing from a fall of <paramref name="fall"/> takes to spring back, in milliseconds.</summary>
    public static double LandingMs(int fall) => Look.Landing.Ms + Look.Landing.LongerMs * Math.Clamp((double)fall / Look.Landing.LongFall, 0, 1);

    /// <summary>The radius, in pixels, a landing's ring starts from.</summary>
    public const double RingFrom = 3;

    /// <summary>How far out a landing's ring goes from a fall of <paramref name="fall"/>, in pixels, how long it lasts and how bright it is.</summary>
    public static (double To, double Ms, double Alpha) RingOf(int fall) => Heavy(fall)
        ? (Look.Landing.HeavyRing, Look.Landing.HeavyRingMs, Look.Landing.HeavyRingAlpha)
        : (RingFrom + Look.Landing.Ring * Math.Clamp(fall / Look.Landing.RingFall, Look.Landing.RingLeast, 1), Look.Landing.RingMs, Look.Landing.RingAlpha);

    /// <summary>The lean of an acceleration across of <paramref name="acceleration"/>, for a hero facing <paramref name="facing"/>.</summary>
    public static double LeanOf(double acceleration, int facing) =>
        Math.Clamp(acceleration * facing * Look.Lean.Gain, -Look.Lean.Back, Look.Lean.Forward);

    /// <summary>
    /// Takes <paramref name="hero"/> in <paramref name="pose"/>, <paramref name="elapsedMs"/> after the last frame: what it
    /// set off, and <see cref="Now"/> for the pose.
    /// </summary>
    public Cues Step(Hero hero, Pose pose, double elapsedMs)
    {
        double steps = elapsedMs / Hero.StepMs;
        bool ground = pose is Pose.Stand or Pose.Walk or Pose.Run;
        int before = _vx ?? hero.Vx, change = hero.Vx - before;
        _vx = hero.Vx;
        _acceleration = _acceleration * Math.Pow(1 - Look.Lean.Smooth, steps) + Look.Lean.Smooth * (ground ? change : 0);

        bool braking = ground && Math.Abs(hero.Vx) > Look.Skid.Speed && Math.Sign(change) == -Math.Sign(hero.Vx) && Math.Abs(change) >= Look.Skid.Brake;
        if (braking) (_skidMs, _along, _sliding) = (Look.Skid.Ms, Math.Sign(hero.Vx) * hero.Facing, Math.Sign(hero.Vx));
        // Stopped dead from over the crawl, a wall took the speed, not the soles.
        else if (!ground || hero.Vx == 0 && Math.Abs(before) > Look.Skid.Speed) _skidMs = 0;
        else _skidMs = Math.Max(0, _skidMs - elapsedMs);
        _skid += ((_skidMs > 0 ? 1 : 0) - _skid) * Math.Min(1, elapsedMs / Look.Skid.BlendMs);
        int dust = 0;
        if (_skidMs > 0 && Math.Sign(hero.Vx) == _sliding && Math.Abs(hero.Vx) > Look.Skid.Speed)
        {
            _dustOwed += Look.Skid.StreaksPerStep * steps;
            dust = (int)_dustOwed;
            _dustOwed -= dust;
        }
        else _dustOwed = 0;

        // Sliding down a ladder, seen from the fall alone: the gloves throw sparks off its rails as fast as it slides.
        int sparks = 0;
        if (pose == Pose.Climb && Poses.SlideOf(hero.Vy) is var slide and > 0)
        {
            _sparksOwed += Look.Ladder.SparksPerStep * slide * steps;
            sparks = (int)_sparksOwed;
            _sparksOwed -= sparks;
        }
        else _sparksOwed = 0;

        bool jumped = Poses.Jumped(_was, pose, hero);
        if (jumped) _sinceJumpMs = 0;
        else _sinceJumpMs += elapsedMs;
        if (pose == Pose.Air) _fall = Math.Max(0, hero.Vy);
        _fallingMs = pose == Pose.Air && hero.Vy > 0 ? _fallingMs + elapsedMs : 0;
        int? landed = null;
        if (Poses.Landed(_was, pose, _fall))
        {
            landed = _fall;
            (_depth, _landTotalMs) = (DepthOf(_fall, hero.Vx), LandingMs(_fall));
            _landMs = _landTotalMs;
        }
        _landMs = Math.Max(0, _landMs - elapsedMs);
        _was = pose;
        Now = new Momentum(ground ? LeanOf(_acceleration, hero.Facing) : 0, _skid, _along, _depth, _sinceJumpMs, _fallingMs);
        return new Cues(jumped, landed, dust, sparks);
    }
}
