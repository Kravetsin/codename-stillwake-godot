using System;
using System.Collections.Generic;
using Look = Stillwake.Game.SalvageLook;

namespace Stillwake.Game.Features.Logistics;

/// <summary>A puff of air drawn: its middle, its size a share of its picture, how opaque, which picture, its turn in radians.</summary>
public readonly record struct Puff(double X, double Y, double Size, double Alpha, int Picture, double Spin);

/// <summary>
/// A salvage drone's nozzles firing (salvage-drones), apart from Godot: the pick page's <c>firing</c> and <c>puffsAt</c>, stepped
/// frame by frame instead of read off a path. A group of nozzles (<see cref="SalvageInk.Nozzles"/>) fires while the drone
/// accelerates its way past <see cref="SalvageLook.Fire"/>; in the ship's air the downward ones puff weaker, every other
/// time, holding it up. A firing nozzle puffs every <see cref="SalvageLook.PuffEveryS"/>; a puff blows out of it, keeping some of
/// the drone's speed, grows and thins, living <see cref="SalvageLook.LifeInS"/> in air and <see cref="SalvageLook.LifeOutS"/> in vacuum.
/// </summary>
public sealed class Puffs
{
    /// <summary>How hard a group fires: 0 not, 1 holding the drone up in air, 2 a burn.</summary>
    public readonly record struct Groups(int L, int R, int U, int D)
    {
        public int Of(char group) => group switch { 'L' => L, 'R' => R, 'U' => U, _ => D };
    }

    private readonly record struct Born(double At, double X, double Y, double Dx, double Dy, double Vx, double Vy, double S, int On, bool Inside, bool Low, int Picture, double Spin);

    private readonly List<Born> _born = [];
    private readonly List<Puff> _alive = [];
    private double _t, _vx, _vy;
    private long _next;
    private bool _seen;

    /// <summary>The puffs alive after the last step.</summary>
    public IReadOnlyList<Puff> Alive => _alive;

    /// <summary>
    /// Which groups fire at acceleration (<paramref name="ax"/>, <paramref name="ay"/>), pixels a second squared, y down: those
    /// blowing against it burn; <paramref name="inside"/> the ship's air, the downward ones hold the drone up unless they burn.
    /// </summary>
    public static Groups Firing(double ax, double ay, bool inside)
    {
        int l = ax > Look.Fire ? 2 : 0, r = ax < -Look.Fire ? 2 : 0;
        int d = ay < -Look.Fire ? 2 : 0, u = ay > Look.Fire ? 2 : 0;
        if (inside && d == 0) d = 1;
        return new Groups(l, r, u, d);
    }

    /// <summary>
    /// <paramref name="dt"/> seconds on: the drone's middle at (<paramref name="x"/>, <paramref name="y"/>) moving at
    /// (<paramref name="vx"/>, <paramref name="vy"/>), pixels a second, leaning <paramref name="tilt"/> radians and facing
    /// <paramref name="face"/>; puffing while <paramref name="flying"/>, in the ship's air while <paramref name="inside"/>, smaller
    /// while <paramref name="low"/>. Its acceleration is its speed's change over the step.
    /// </summary>
    public void Step(double dt, double x, double y, double vx, double vy, double tilt, int face, bool inside, bool low, bool flying)
    {
        double ax = _seen && dt > 0 ? (vx - _vx) / dt : 0, ay = _seen && dt > 0 ? (vy - _vy) / dt : 0;
        (_vx, _vy, _seen) = (vx, vy, true);
        _t += dt;
        // After a long frame, only the puffs that would still be alive are born.
        _next = Math.Max(_next, (long)Math.Ceiling((_t - Look.LifeInS) / Look.PuffEveryS));
        for (; _next * Look.PuffEveryS <= _t; _next++)
            if (flying) Bear(_next, x, y, vx, vy, tilt, face, Firing(ax, ay, inside), inside, low);
        _born.RemoveAll(b => _t - b.At >= (b.Inside ? Look.LifeInS : Look.LifeOutS));
        _alive.Clear();
        foreach (var b in _born)
        {
            double age = _t - b.At, u = age / (b.Inside ? Look.LifeInS : Look.LifeOutS);
            double dist = (b.Inside ? Look.ReachIn : Look.ReachOut) * b.S * (1 - (1 - u) * (1 - u)) + Look.Mouth;
            double size = (Look.PuffBorn + u * (b.Inside ? Look.GrowIn : Look.GrowOut)) * b.S * (b.Low ? Look.LowShrink : 1) * (b.On == 2 ? 1 : Look.HoldShrink);
            _alive.Add(new Puff(b.X + b.Dx * dist + b.Vx * age * Look.Carry, b.Y + b.Dy * dist + b.Vy * age * Look.Carry,
                size, Look.PuffAlpha * Math.Pow(1 - u, 1.4), b.Picture, b.Spin));
        }
    }

    /// <summary>Puff <paramref name="k"/>: one at each nozzle of a group that fires, a holding one only every other time.</summary>
    private void Bear(long k, double x, double y, double vx, double vy, double tilt, int face, Groups groups, bool inside, bool low)
    {
        double c = Math.Cos(tilt), s = Math.Sin(tilt);
        for (int i = 0; i < SalvageInk.Nozzles.Length; i++)
        {
            var n = SalvageInk.Nozzles[i];
            int dx = n.Dx * face;
            char group = dx != 0 ? (dx < 0 ? 'L' : 'R') : n.Group;
            int on = groups.Of(group);
            if (on == 0 || (on == 1 && k % 2 != 0)) continue;
            double ox = (n.X - SalvageInk.AnchorX) * face, oy = n.Y - SalvageInk.AnchorY;
            _born.Add(new Born(k * Look.PuffEveryS, x + ox * c - oy * s, y + ox * s + oy * c, dx * c - n.Dy * s, dx * s + n.Dy * c, vx, vy, n.S, on, inside, low,
                (int)((k + i) % SalvageInk.Puffs), k * 1.3 + i));
        }
    }
}
