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
/// An item thrown and brought back by the drone (companion-fights), the thrower's client's word as a swing is: out along the aim
/// to its range or the first cell that stops a ray, then home to the hero as it is. Cells are struck out only, and nothing past a
/// solid cell home; each target once a throw. In thousandths of a cell and milliseconds.
/// </summary>
public sealed class AxeThrow
{
    /// <summary>How far short of the cell that stopped it the item turns home, so it never starts inside that cell.</summary>
    private const double ShortOfWall = 100;

    private readonly HashSet<(Sim.Building.TargetKind, int)> _targets = [];
    private readonly HashSet<int> _pieces = [], _creatures = [];
    private Spec.Abilities.Throw? _spec;
    private (double X, double Y) _way;
    private double _gone, _waitMs;
    private bool _walled;
    private int _surface;

    public ThrowLeg Leg { get; private set; }

    /// <summary>Whether the item is out of the hand.</summary>
    public bool Flying => Leg is ThrowLeg.Out or ThrowLeg.Home;

    /// <summary>Where the item is, while it flies.</summary>
    public (double X, double Y) At { get; private set; }

    /// <summary>How many have left the hand: a new one is news to the others.</summary>
    public int Count { get; private set; }

    /// <summary>The way it was thrown across: 1 right, -1 left.</summary>
    public int Side => _way.X < 0 ? -1 : 1;

    /// <summary>A frame of <paramref name="elapsedMs"/> toward the next throw's pace, whether one is out or none.</summary>
    public void Wait(double elapsedMs) => _waitMs = Math.Max(0, _waitMs - elapsedMs);

    /// <summary>
    /// The arm raised for a throw of what <paramref name="hands"/> wield, no sooner than <paramref name="paceMs"/> after the last;
    /// false, silently, for an item never thrown, hands that cannot, or one already raised or out.
    /// </summary>
    public bool Begin(ThrowHands hands, double paceMs)
    {
        if (Leg != ThrowLeg.None || _waitMs > 0 || hands.Held || !hands.Free || Spec.Abilities.ThrowOf(hands.Wielded) is not { } spec) return false;
        (_spec, _surface, _waitMs, Leg) = (spec, hands.Surface, paceMs, ThrowLeg.Raised);
        return true;
    }

    /// <summary>Ends it at once, hits and drawing, unless <paramref name="hands"/> still wield it, free, unheld, on the surface it began on.</summary>
    public void Keep(ThrowHands hands)
    {
        if (Leg != ThrowLeg.None && (hands.Held || !hands.Free || hands.Surface != _surface || hands.Wielded != _spec!.Item)) Cancel();
    }

    /// <summary>The raised item leaves <paramref name="hand"/> toward <paramref name="toward"/>; none raised, nothing leaves.</summary>
    public bool Leave(Point hand, Point toward)
    {
        if (Leg != ThrowLeg.Raised) return false;
        double dx = toward.X - hand.X, dy = toward.Y - hand.Y, length = Math.Sqrt(dx * dx + dy * dy);
        _way = length > 0 ? (dx / length, dy / length) : (1, 0);
        (At, _gone, _walled, Leg) = ((hand.X, hand.Y), 0, false, ThrowLeg.Out);
        _targets.Clear();
        _pieces.Clear();
        _creatures.Clear();
        Count++;
        return true;
    }

    public void Cancel() => Leg = ThrowLeg.None;

    /// <summary>
    /// A frame of <paramref name="elapsedMs"/> of its flight by <paramref name="hero"/>: what it struck on it for the first time
    /// this throw (<see cref="ToolRays.Segment"/>); none while none flies.
    /// </summary>
    public EdgeHit? Step(double elapsedMs, Registry m, Hero hero, bool outside, double progress, Func<Piece, double, double, bool> solid)
    {
        if (!Flying) return null;
        var spec = _spec!;
        double ms = Math.Min(elapsedMs, AxeSwing.MostPerFrameMs);
        return Leg == ThrowLeg.Out ? Outward(spec, ms, m, outside, progress, solid) : Homeward(spec, ms, m, hero, outside, progress, solid);
    }

    private EdgeHit Outward(Spec.Abilities.Throw spec, double ms, Registry m, bool outside, double progress, Func<Piece, double, double, bool> solid)
    {
        double step = Math.Min(spec.Range - _gone, spec.Out * ms * World.TicksPerSecond / 1000);
        var from = At;
        var to = (X: from.X + _way.X * step, Y: from.Y + _way.Y * step);
        var (hit, stopped) = ToolRays.Segment(m, from, to, outside, progress, solid);
        _gone += step;
        double back = stopped ? Math.Min(ShortOfWall, Distance(from, hit.End)) : 0;
        At = stopped ? (hit.End.X - _way.X * back, hit.End.Y - _way.Y * back) : to;
        if (stopped || _gone >= spec.Range) Leg = ThrowLeg.Home;
        return Fresh(m, hit, cuts: true);
    }

    private EdgeHit? Homeward(Spec.Abilities.Throw spec, double ms, Registry m, Hero hero, bool outside, double progress, Func<Piece, double, double, bool> solid)
    {
        var hand = Aiming.HandOf(hero);
        var from = At;
        double far = Distance(from, (hand.X, hand.Y)), step = Math.Min(far, spec.Home * ms * World.TicksPerSecond / 1000);
        var to = far > 0 ? (X: from.X + (hand.X - from.X) / far * step, Y: from.Y + (hand.Y - from.Y) / far * step) : from;
        At = to;
        if (step >= far) Leg = ThrowLeg.None;
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

    private static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
