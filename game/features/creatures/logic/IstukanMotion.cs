using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// How one Istukan moves (the guard page's cycle), from what the sim decided: asleep a column, its lamp dark; woken it unfolds, its lamp
/// teal, then yellow while it waits; winding up it crouches trembling, red; launched it flies fists first along its flight; stuck it
/// lodges, its arms shivering, then rises; struck it flashes white and shakes. Falling (<see cref="Fall"/>, the client's) its lamp
/// stutters out and it lies dim as blocks. Its moments are the view's to see and hear. Times in seconds. Plain C#, tested.
/// </summary>
public sealed class IstukanMotion(Func<double> random, double wakeS)
{
    /// <summary>
    /// What the sim says of it this frame: asleep, winding up (<paramref name="WindUpS"/> seconds into it), flying a pounce, stuck, its speed in pixels a second, whether its last tick moved it not at all, the
    /// way to the hero nearest it from its middle (none for no hero).
    /// </summary>
    public readonly record struct Seen(bool Asleep, bool WindingUp, double WindUpS, bool Pouncing, bool Stuck, double Vx, double Vy, bool Still,
        (double X, double Y)? Hero);

    /// <summary>Its moments, seen and heard once each as it comes.</summary>
    [Flags]
    public enum Moment
    {
        None = 0,
        Wake = 1,
        Alert = 2,
        WindUp = 4,
        Pounce = 8,
        /// <summary>It came to rest after a flight.</summary>
        Land = 16,
    }

    public static readonly TickArt.Rgb Teal = new(79, 230, 200), Yellow = new(255, 207, 70), Red = new(255, 59, 46);

    /// <summary>The page's: its crouch's ease, its unfolding at launch, its lodging, its flash and shake; the fall, and how long its blocks lie.</summary>
    public const double CrouchS = 0.36, LaunchS = 0.04, LodgeS = 0.05, FlashS = 0.09, ShakeS = 0.12, FallS = 0.9, LieS = 3;

    private Seen _was;
    private bool _seen, _landing, _waking;
    private double _clock, _sinceWake = 99, _sinceAlert = 99, _sinceLaunch = 99, _sinceHit = 99, _crouch, _fly, _stuck, _angle, _fold = 1;
    private TickArt.Rgb? _lamp;

    /// <summary>The way it faces: 1 right, −1 left.</summary>
    public int Face { get; set; } = 1;

    /// <summary>Its turn about its middle, radians: along its flight, none on its feet.</summary>
    public double Angle => _angle;

    /// <summary>Its tremble or shake across, in pixels.</summary>
    public double Shake { get; private set; }

    /// <summary>A blow's white flash over it, 0..0.9.</summary>
    public double Flash => Math.Max(0, 1 - _sinceHit / FlashS) * 0.9;

    /// <summary>Whether it acts: crouched or flying at a hero, its lamp the brighter.</summary>
    public bool Hot { get; private set; }

    /// <summary>Seconds since it began to fall; none standing.</summary>
    public double? Falling { get; private set; }

    /// <summary>Its pose for <see cref="IstukanArt"/>.</summary>
    public IstukanArt.Pose Pose { get; private set; }

    /// <summary>Struck, as this player's own blow goes or a friend's shows: a white flash, a shake.</summary>
    public void Struck() => _sinceHit = 0;

    /// <summary>From now it falls where it is: its lamp stutters out and it comes apart.</summary>
    public void Fall() => Falling ??= 0;

    /// <summary>A frame of <paramref name="dt"/> seconds of it as <paramref name="s"/>; answers the moments that came.</summary>
    public Moment Step(Seen s, double dt)
    {
        _clock += dt;
        _sinceWake += dt;
        _sinceAlert += dt;
        _sinceLaunch += dt;
        _sinceHit += dt;
        if (Falling is double falling) return Falls(falling + dt, dt, s.Still);
        if (!_seen) _fold = s.Asleep ? 1 : 0;
        var moments = _seen ? MomentsOf(s) : Moment.None;
        (_was, _seen) = (s, true);

        if (s.Pouncing && Math.Abs(s.Vx) > 1) Face = Math.Sign(s.Vx);
        else if (!s.Asleep && !s.Pouncing && !s.Stuck && s.Hero is { } hero && Math.Abs(hero.X) > 2) Face = Math.Sign(hero.X);

        // The page's unfolding, over nine tenths of its wake; it folds up only by being brought back whole, asleep.
        _fold = s.Asleep ? 1 : Math.Min(_fold, 1 - Ease((_sinceWake / wakeS - 0.1) / 0.8));
        double seam = s.Asleep ? 0.08 : 0.08 + 0.92 * Math.Clamp(_sinceWake / (wakeS * 0.7), 0, 1);
        if (s.Stuck)
        {
            _stuck = Math.Min(1, _stuck + dt / LodgeS);
            // Lodged in full its other poses are hidden: dropped, so it rises from here straight to its feet.
            if (_stuck >= 1) (_crouch, _fly) = (0, 0);
            else _crouch = 1;
        }
        else if (s.Pouncing)
        {
            _crouch = 1;
            _fly = Math.Clamp(_sinceLaunch / LaunchS, 0, 1);
            _stuck = 0;
        }
        else
        {
            double to = s.WindingUp ? Ease(s.WindUpS / CrouchS) : 0;
            _crouch = Toward(_crouch, to, dt, s.WindingUp ? 0.02 : 0.08);
            _stuck = Toward(_stuck, 0, dt, 0.08);
            _fly = Toward(_fly, 0, dt, 0.04);
        }
        double speed = Math.Sqrt(s.Vx * s.Vx + s.Vy * s.Vy);
        // The flown pose is already nosed along a level flight; a sloping one is turned from it.
        double angle = s.Pouncing && speed > 1 ? Math.Atan2(Face * s.Vy, Face * s.Vx) : s.Pouncing ? _angle : 0;
        _angle = Toward(_angle, angle, dt, 0.03);

        Hot = s.WindingUp || s.Pouncing;
        bool waking = _sinceWake < wakeS;
        _lamp = s.Asleep ? null
            : waking ? Teal
            : Hot || s.Stuck ? Red
            // Teal stuttering into yellow a moment as it takes the hero in, the page's.
            : _sinceAlert < 0.12 && Math.Floor(_sinceAlert * 50) % 2 == 1 ? Teal
            : Yellow;
        double amp = s.Asleep ? 0
            : waking ? Math.Clamp(_sinceWake / (wakeS * 0.25), 0, 1)
            : s.WindingUp ? (s.WindUpS < 0.45 ? (random() < 0.35 ? 0.4 : 1.2) : 1.4)
            : s.Pouncing ? 1.4
            : 1;
        Shake = _sinceHit < ShakeS ? (random() - 0.5) * 0.8
            : s.WindingUp && s.WindUpS > 0.44 ? Math.Floor(s.WindUpS * 50) % 2
            : 0;
        // Its clock only while it shivers, so a pose held is drawn once.
        Pose = new IstukanArt.Pose(_fold, _crouch, _fly, _stuck, 0, _lamp, amp, seam, Clock: s.Stuck ? _clock : 0, OnFloor: !s.Pouncing && s.Still);
        return moments;
    }

    private Moment MomentsOf(Seen s)
    {
        var m = Moment.None;
        if (_was.Asleep && !s.Asleep)
        {
            m |= Moment.Wake;
            (_sinceWake, _waking) = (0, true);
        }
        // It takes the hero in as its wake ends.
        if (_waking && _sinceWake >= wakeS)
        {
            (_waking, _sinceAlert) = (false, 0);
            m |= Moment.Alert;
        }
        if (s.WindingUp && !_was.WindingUp) m |= Moment.WindUp;
        if (s.Pouncing && !_was.Pouncing)
        {
            m |= Moment.Pounce;
            _sinceLaunch = 0;
        }
        if (_was.Pouncing && !s.Pouncing) _landing = true;
        if (_landing && s.Still && !s.Pouncing)
        {
            _landing = false;
            m |= Moment.Land;
        }
        return m;
    }

    /// <summary>The page's fall: seams and lamp stuttering a moment, then dark, its keys going to the heap, dimming.</summary>
    private Moment Falls(double falling, double dt, bool onFloor)
    {
        Falling = falling;
        double u = Math.Clamp(falling / FallS, 0, 1);
        bool flick = u < 0.4 && random() > 0.45;
        double dead = Ease((u - 0.15) / 0.6);
        _angle = Toward(_angle, 0, dt, 0.06);
        Shake = 0;
        Hot = false;
        Pose = new IstukanArt.Pose(_fold, _crouch, _fly, _stuck, dead, flick ? Red : null, 0.8, flick ? 0.7 : 0, dead * 0.22, flick ? _clock : 0, onFloor);
        return Moment.None;
    }

    /// <summary>The page's <c>smooth</c>: smoothstep, clamped.</summary>
    public static double Ease(double t) => TickMotion.Ease(t);

    /// <summary>
    /// <paramref name="at"/> eased toward <paramref name="to"/> over <paramref name="dt"/> with time constant <paramref name="tau"/>, the same at any frame rate;
    /// there once no pixel can tell, so a pose held is drawn once.
    /// </summary>
    private static double Toward(double at, double to, double dt, double tau)
    {
        double next = at + (to - at) * (1 - Math.Exp(-dt / tau));
        return Math.Abs(to - next) < 0.001 ? to : next;
    }
}
