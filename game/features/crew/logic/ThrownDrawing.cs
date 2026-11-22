using System;
using Stillwake.Game.Shared;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>An item as drawn out of a swing: its grip, in pixels of the world, its turn and the height of its picture, 1 or -1 mirrored.</summary>
public readonly record struct HeldPlace(Vec Grip, double Angle, double Side);

/// <summary>One frame of a throw as the figure hears it: where it flies, in pixels of the world, its number, whether the thread is cast.</summary>
public readonly record struct Flight(Vec At, int Count, bool Hauled);

/// <summary>
/// A thrown weapon as drawn (companion-fights), in pixels and ms: let go as the hand last held it, it spins by its speed or flies
/// point first, and hauled home it turns into the fist held out for it. What it hits is <see cref="WeaponThrow"/>'s alone.
/// </summary>
public sealed class ThrownDrawing
{
    /// <summary>The share of its spin it keeps standing, at what speed by its hero it has it whole, px/ms, and how long it takes to change.</summary>
    private const double SpinKept = 0.16, SpinsWholeAt = 0.5, SpinEaseMs = 70;

    /// <summary>One flying point first follows its way over this fast, px/ms, turning in this long; hauled, it trails its point till this near the hand.</summary>
    private const double SteersOver = 0.1, SteerMs = 50, TrailsTill = 16;

    /// <summary>How long it takes from where the hand let it go onto the throw's line; how far up from the grip to the edge its weight is.</summary>
    private const double OffHandMs = 50, WeightUp = 0.7;

    /// <summary>Over how much of its way home at least it turns into the fist; twice from its weight to the fist where that is more, so its grip never passes it.</summary>
    private const double LandsOver = 96;

    /// <summary>A flight ended this near the hand is a catch, and how long what is left of it takes to close into the fist.</summary>
    private const double CaughtWithin = 64, SettleMs = 70;

    /// <summary>The least height of its picture as it turns over: none has no inverse.</summary>
    private const double LeastSide = 0.05;

    private HeldPlace? _last;
    private HeldLook? _look;
    private Vec _from, _offHand;
    private int? _count;
    private double _angle, _turns, _side = 1;
    private double _farMost, _landed, _turnLeft, _sideFrom;
    private bool _near;
    private HeldPlace _left;
    private double _settleMs = SettleMs;

    /// <summary>How long the thread has been cast to it.</summary>
    public double CastMs { get; private set; }

    /// <summary>How long it has been out of the hand.</summary>
    public double OutMs { get; private set; }

    /// <summary>Whether it is out of the hand.</summary>
    public bool Flying => _count != null;

    /// <summary>Whether it was caught and still closes into the fist.</summary>
    public bool Landing => _settleMs < SettleMs;

    /// <summary>Drops it, drawn no more: the hand holds another thing or none.</summary>
    public void Reset() => (_count, _last, _look, _settleMs) = (null, null, null, SettleMs);

    /// <summary>
    /// A frame of it: <paramref name="inFist"/> is where the hand would hold it now, <paramref name="shoulder"/> the point it is
    /// hauled to, <paramref name="own"/> its hero's speed; where to draw it, or none while it is simply held.
    /// </summary>
    public HeldPlace? Step(double ms, HeldLook? look, Spec.Abilities.Throw? spec, Flight? flight, HeldPlace inFist, Vec shoulder, Vec own)
    {
        if (look == null || spec == null)
        {
            Reset();
            return null;
        }
        if (look != _look)
        {
            Reset();
            _look = look;
        }
        if (flight is not { } now)
        {
            // Kept from the fist, not in the world: caught, it moves with the hand.
            if (_count != null && _near) _settleMs = 0;
            _count = null;
            _settleMs = Math.Min(SettleMs, _settleMs + ms);
            _last = inFist;
            if (!Landing) return null;
            double left = 1 - _settleMs / SettleMs;
            var settling = new HeldPlace(DroneFlight.Plus(inFist.Grip, DroneFlight.Scaled(_left.Grip, left)),
                inFist.Angle + _left.Angle * left, Seen(inFist.Side + _left.Side * left));
            _last = settling;
            return settling;
        }
        bool pointFirst = spec.Spin == 0;
        double spins = spec.Spin / 10.0;
        var (ex, ey) = pointFirst ? look.EdgeTo : look.EdgePoint(0.5);
        double up = pointFirst ? 1 : WeightUp;
        var weight = new Vec((ex - look.Grip.X) * up * look.Size, (ey - look.Grip.Y) * up * look.Size);
        var away = DroneFlight.Minus(now.At, shoulder);
        double far = DroneFlight.Length(away);
        if (_count != now.Count)
        {
            var held = _last ?? inFist;
            (_angle, _side, _count, _turns) = (held.Angle, held.Side < 0 ? -1 : 1, now.Count, spins);
            (CastMs, OutMs, _farMost, _landed, _near, _settleMs) = (0, 0, 0, 0, false, SettleMs);
            (_from, _offHand) = (now.At, DroneFlight.Minus(DroneFlight.Plus(held.Grip, Turned(weight, _angle, _side)), now.At));
        }
        else if (ms > 0)
        {
            OutMs += ms;
            var by = DroneFlight.Minus(DroneFlight.Scaled(DroneFlight.Minus(now.At, _from), 1 / ms), own);
            // Once it lands, the fist alone turns it.
            if (_landed == 0 && pointFirst)
            {
                var along = now.Hauled ? away : by;
                if (DroneFlight.Length(along) > (now.Hauled ? TrailsTill : SteersOver))
                {
                    double wanted = Math.Atan2(along.Y, along.X) - _side * Math.Atan2(weight.Y, weight.X);
                    _angle += Poses.Apart(_angle, wanted) * (1 - Math.Exp(-ms / SteerMs));
                }
            }
            else if (_landed == 0)
            {
                double wanted = spins * (SpinKept + (1 - SpinKept) * Math.Min(1, DroneFlight.Length(by) / SpinsWholeAt));
                _turns += (wanted - _turns) * (1 - Math.Exp(-ms / SpinEaseMs));
                _angle = (_angle + _side * _turns * Math.Tau * ms / 1000) % Math.Tau;
            }
            _from = now.At;
            _offHand = DroneFlight.Scaled(_offHand, Math.Exp(-ms / OffHandMs));
        }
        var fist = DroneFlight.Minus(inFist.Grip, shoulder);
        if (now.Hauled)
        {
            CastMs += ms;
            _farMost = Math.Max(_farMost, far);
            double over = Math.Min(_farMost, Math.Max(LandsOver, 2 * (DroneFlight.Length(weight) + DroneFlight.Length(fist))));
            double landed = over > 0 ? Math.Max(_landed, 1 - far / over) : 1;
            if (_landed == 0 && landed > 0) (_turnLeft, _sideFrom) = (Poses.Apart(inFist.Angle, _angle), _side);
            _landed = landed;
        }
        double angle = _angle, side = _side;
        if (_landed > 0) (angle, side) = (inFist.Angle + _turnLeft * (1 - _landed), Seen(_sideFrom + (inFist.Side - _sideFrom) * _landed));
        var free = DroneFlight.Minus(_offHand, Turned(weight, angle, side));
        var offset = DroneFlight.Plus(free, DroneFlight.Scaled(DroneFlight.Minus(fist, free), _landed));
        var drawn = new HeldPlace(DroneFlight.Plus(now.At, offset), angle, side);
        _near = _landed > 0 && far <= CaughtWithin;
        _left = new HeldPlace(DroneFlight.Minus(drawn.Grip, inFist.Grip), Poses.Apart(inFist.Angle, angle), side - inFist.Side);
        _last = drawn;
        return drawn;
    }

    /// <summary>The arm held out to a thing <paramref name="dx"/>, <paramref name="dy"/> from the shoulder, its elbow a little soft: its angle from straight ahead.</summary>
    public static double ArmToward(double dx, double dy) => Math.Atan2(dy, Math.Abs(dx)) + OutElbow / 2;

    /// <summary>How soft the elbow of the arm held out is.</summary>
    public const double OutElbow = 0.25;

    private static double Seen(double side) => Math.Abs(side) >= LeastSide ? side : side < 0 ? -LeastSide : LeastSide;

    private static Vec Turned(Vec p, double angle, double side)
    {
        double cos = Math.Cos(angle), sin = Math.Sin(angle), y = p.Y * side;
        return new Vec(p.X * cos - y * sin, p.X * sin + y * cos);
    }
}
