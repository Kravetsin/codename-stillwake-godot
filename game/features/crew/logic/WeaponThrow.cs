using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What a throw needs of the hands each frame: the item wielded, the hands <paramref name="Free"/> of the building plan, the hero
/// <paramref name="Held"/> (passed out, in a scene, seated or flying), and the surface it is on.
/// </summary>
public readonly record struct ThrowHands(string? Wielded, bool Free, bool Held, int Surface);

/// <summary>Where a throw is: raised by the arm, out along its aim or on its way home; none between throws.</summary>
public enum ThrowLeg { None, Raised, Out, Home }

/// <summary>
/// An item thrown and hauled back on the drone's thread (companion-fights), the thrower's client's word as a swing is. It has weight:
/// it leaves with its hero's own speed, slows to a stop at its range or is knocked back by the first cell that stops a ray, and the
/// thread, caught, has to turn it and bring it up to speed. Cells are struck out only, and nothing past a solid cell home; each
/// target once a throw, or once each way for one that strikes twice. In thousandths of a cell, seconds and frames of milliseconds.
/// </summary>
public sealed class WeaponThrow
{
    /// <summary>How long the thread takes from its cast to holding the item, ms: cast so it holds as the item stops.</summary>
    public const double CastMs = 120;

    /// <summary>How long the thread takes to turn what speed the item has across it, s: how wide its arc home is.</summary>
    private const double TurnS = 0.12;

    /// <summary>The share of its speed a cell that stops it knocks it back with.</summary>
    private const double Bounce = 0.15;

    /// <summary>How near the hand it is caught, and how far short of the cell that stopped it it turns, so it never starts inside that cell.</summary>
    private const double CatchWithin = 800, ShortOfWall = 100;

    /// <summary>How far the thread yanks it from where it turned before it strikes again: what it stopped in takes two hits, not a double one.</summary>
    private const double StrikesAgainAfter = 500;

    private readonly HashSet<(Sim.Building.TargetKind, int)> _targets = [];
    private readonly HashSet<int> _pieces = [], _creatures = [];
    private Spec.Abilities.Throw? _spec;
    private (double X, double Y) _way, _carried, _velocity;
    private double _speed, _castMs, _paceMs, _clock;
    private readonly Dictionary<string, double> _readyAt = [];
    private (double X, double Y) _turnedAt;
    private bool _walled, _strikesAgain;
    private int _surface;

    public ThrowLeg Leg { get; private set; }

    /// <summary>Whether the item is out of the hand.</summary>
    public bool Flying => Leg is ThrowLeg.Out or ThrowLeg.Home;

    /// <summary>Whether the thread is cast to it: reaching for it, then hauling.</summary>
    public bool Hauled { get; private set; }

    /// <summary>Where the item is, while it flies.</summary>
    public (double X, double Y) At { get; private set; }

    /// <summary>How fast it flies, a second.</summary>
    public (double X, double Y) Velocity => _velocity;

    /// <summary>How many have left the hand: a new one is news to the others.</summary>
    public int Count { get; private set; }

    /// <summary>A frame of <paramref name="elapsedMs"/> toward the next throw's pace, whether one is out or none.</summary>
    public void Wait(double elapsedMs) => _clock += elapsedMs;

    /// <summary>Whether <paramref name="item"/> still rests from its throw: each weapon its own, and no swing of it meanwhile.</summary>
    public bool Rests(string? item) => item != null && _readyAt.TryGetValue(item, out double at) && _clock < at;

    /// <summary>The raised one's stroke begun: its rest counts from it, as a swing's pace does.</summary>
    public void Swung()
    {
        if (Leg == ThrowLeg.Raised) _readyAt[_spec!.Item] = _clock + _paceMs;
    }

    /// <summary>
    /// The arm raised for a throw of what <paramref name="hands"/> wield, no sooner than <paramref name="paceMs"/> after the last;
    /// false, silently, for an item never thrown, hands that cannot, or one already raised or out.
    /// </summary>
    public bool Begin(ThrowHands hands, double paceMs)
    {
        if (Leg != ThrowLeg.None || hands.Held || !hands.Free || Spec.Abilities.ThrowOf(hands.Wielded) is not { } spec || Rests(spec.Item)) return false;
        (_spec, _surface, _paceMs, Leg) = (spec, hands.Surface, paceMs, ThrowLeg.Raised);
        _readyAt[spec.Item] = _clock + paceMs;
        return true;
    }

    /// <summary>Ends it at once, hits and drawing, unless <paramref name="hands"/> still wield it, free, unheld, on the surface it began on.</summary>
    public void Keep(ThrowHands hands)
    {
        if (Leg != ThrowLeg.None && (hands.Held || !hands.Free || hands.Surface != _surface || hands.Wielded != _spec!.Item)) Cancel();
    }

    /// <summary>The raised item leaves <paramref name="hero"/>'s hand toward <paramref name="toward"/>, the hero's own speed in it; none raised, nothing leaves.</summary>
    public bool Leave(Hero hero, Point toward)
    {
        if (Leg != ThrowLeg.Raised) return false;
        var hand = Aiming.HandOf(hero);
        double dx = toward.X - hand.X, dy = toward.Y - hand.Y, length = Math.Sqrt(dx * dx + dy * dy);
        _way = length > 0 ? (dx / length, dy / length) : (hero.Facing, 0);
        (_carried, _speed) = (SpeedOf(hero), (double)_spec!.Out * World.TicksPerSecond);
        _velocity = (_carried.X + _way.X * _speed, _carried.Y + _way.Y * _speed);
        (At, _castMs, _walled, _strikesAgain, Hauled, Leg) = ((hand.X, hand.Y), 0, false, false, false, ThrowLeg.Out);
        _targets.Clear();
        _pieces.Clear();
        _creatures.Clear();
        Count++;
        return true;
    }

    public void Cancel() => (Leg, Hauled) = (ThrowLeg.None, false);

    /// <summary>
    /// A frame of <paramref name="elapsedMs"/> of its flight by <paramref name="hero"/>: what it struck on it for the first time
    /// this throw (<see cref="ToolRays.Segment"/>); none while none flies.
    /// </summary>
    public EdgeHit? Step(double elapsedMs, Registry m, Hero hero, bool outside, double progress, Func<Piece, double, double, bool> solid)
    {
        if (!Flying) return null;
        double ms = Math.Min(elapsedMs, AxeSwing.MostPerFrameMs);
        if (Hauled) _castMs += ms;
        return Leg == ThrowLeg.Out ? Outward(ms / 1000, m, outside, progress, solid) : Homeward(ms / 1000, m, hero, outside, progress, solid);
    }

    private EdgeHit Outward(double s, Registry m, bool outside, double progress, Func<Piece, double, double, bool> solid)
    {
        var spec = _spec!;
        double launch = (double)spec.Out * World.TicksPerSecond, slows = launch * launch / (2.0 * spec.Range);
        double going = Math.Min(s, _speed / slows), step = _speed * going - slows * going * going / 2;
        var from = At;
        var to = (X: from.X + _carried.X * s + _way.X * step, Y: from.Y + _carried.Y * s + _way.Y * step);
        var (hit, stopped) = ToolRays.Segment(m, from, to, outside, progress, solid);
        if (stopped)
        {
            double length = Math.Max(Distance(from, to), 1e-6), ux = (to.X - from.X) / length, uy = (to.Y - from.Y) / length;
            double back = Math.Min(ShortOfWall, Distance(from, hit.End)), knocked = Bounce * Distance(_velocity, (0, 0));
            At = (hit.End.X - ux * back, hit.End.Y - uy * back);
            _velocity = (-ux * knocked, -uy * knocked);
            (Hauled, Leg) = (true, ThrowLeg.Home);
        }
        else
        {
            bool stops = going < s || _speed - slows * going <= 0;
            _speed = stops ? 0 : _speed - slows * going;
            (At, _velocity) = (to, (_carried.X + _way.X * _speed, _carried.Y + _way.Y * _speed));
            Hauled |= _speed / slows <= CastMs / 1000;
            if (stops)
            {
                Leg = ThrowLeg.Home;
                if (spec.Twice)
                {
                    (_strikesAgain, _turnedAt) = (true, to);
                    _readyAt[spec.Item] += _paceMs;
                }
            }
        }
        return Fresh(m, hit, cuts: true);
    }

    private EdgeHit? Homeward(double s, Registry m, Hero hero, bool outside, double progress, Func<Piece, double, double, bool> solid)
    {
        var spec = _spec!;
        var hand = Aiming.HandOf(hero);
        var from = At;
        (double X, double Y) to;
        bool caught = false;
        if (_castMs < CastMs) to = (from.X + _velocity.X * s, from.Y + _velocity.Y * s);
        else
        {
            // Hauled as seen from the hero: what speed it has along the thread grows, what it has across dies away.
            var own = SpeedOf(hero);
            double dx = hand.X - from.X, dy = hand.Y - from.Y, far = Math.Sqrt(dx * dx + dy * dy);
            var (ux, uy) = far > 0 ? (dx / far, dy / far) : (0.0, 0.0);
            double rx = _velocity.X - own.X, ry = _velocity.Y - own.Y, along = rx * ux + ry * uy;
            double across = Math.Exp(-s / TurnS), sideX = (rx - ux * along) * across, sideY = (ry - uy * along) * across;
            along = Math.Min((double)spec.Home * World.TicksPerSecond, along + (double)spec.Pull * World.TicksPerSecond * World.TicksPerSecond * s);
            _velocity = (own.X + ux * along + sideX, own.Y + uy * along + sideY);
            caught = far <= CatchWithin || along * s >= far;
            to = caught ? (hand.X, hand.Y) : (from.X + _velocity.X * s, from.Y + _velocity.Y * s);
        }
        At = to;
        if (_strikesAgain && _castMs >= CastMs && Distance(to, _turnedAt) >= StrikesAgainAfter)
        {
            _strikesAgain = false;
            _pieces.Clear();
            _creatures.Clear();
        }
        if (caught) Cancel();
        if (_walled) return null;
        var (hit, stopped) = ToolRays.Segment(m, from, to, outside, progress, solid);
        _walled = stopped;
        return Fresh(m, hit, cuts: false);
    }

    /// <summary>What of <paramref name="hit"/> this throw had not struck yet; its cells only while it <paramref name="cuts"/>.</summary>
    private EdgeHit Fresh(Registry m, EdgeHit hit, bool cuts)
    {
        var cells = new List<int>();
        if (cuts)
            foreach (int cell in hit.Cells)
                if (m.Building.TargetAt(cell) is { } target && _targets.Add((target.Kind, target.Key))) cells.Add(cell);
        var wrecks = hit.Wrecks.Where(p => _pieces.Add(p.Id)).ToList();
        var creatures = hit.Creatures.Where(c => _creatures.Add(c.Id)).ToList();
        var touches = hit.Touches.Where(t => t.Cell is int cell ? cells.Contains(cell) : t.Wreck is { } wreck && wrecks.Contains(wreck)).ToList();
        return new EdgeHit(cells, wrecks, creatures, touches, hit.End);
    }

    /// <summary>A hero's own speed, a second.</summary>
    private static (double X, double Y) SpeedOf(Hero hero) => (hero.Vx * 1000 / Hero.StepMs, hero.Vy * 1000 / Hero.StepMs);

    private static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
