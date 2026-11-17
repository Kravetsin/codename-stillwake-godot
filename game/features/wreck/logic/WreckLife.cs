using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Wrecked;

/// <summary>The kinds of mote a wreck's air holds (the mockup's particles), each drawn alike: a colour, an alpha, how it shows.</summary>
public enum Mote { Dust, DustLit, RockDust, Paint, Bit, Rivet, Ice, IceDull, Fuel, Ash, AshLight, Ember }

/// <summary>A mote: where it hangs in cells, its size, how far and fast it sways, its phase.</summary>
public readonly record struct Speck(float X, float Y, float Size, float Ax, float Ay, float Rate, float Phase);

/// <summary>What a drifter is: a hull shard, a bit, a stone, or a thrown machine's picture.</summary>
public enum DriftKind { Shard, Bit, Rock, Machine }

/// <summary>
/// A thing drifting outside a wreck (the mockup's drifters): decoration only, never grabbable, never the sim's. It sways along its
/// scatter line round (<c>X</c>, <c>Y</c>) and turns.
/// </summary>
public sealed record Drifter(DriftKind Kind, double X, double Y, double Vx, double Vy, double Amp, double Rate, double Phase, double Turn, double Spin,
    double Size, IReadOnlyList<(double X, double Y)>? Shape = null, string? Machine = null, uint Colour = 0, bool Bent = false)
{
    /// <summary>Where it is and how turned <paramref name="t"/> seconds in.</summary>
    public (double X, double Y, double Turn) At(double t)
    {
        double len = Math.Sqrt(Vx * Vx + Vy * Vy) is var l && l > 0 ? l : 1, ux = Vx / len, uy = Vy / len, b = Amp * Math.Sin(t * Rate + Phase);
        return (X + ux * b - uy * b * 0.25, Y + uy * b + ux * b * 0.25, Turn + Spin * t);
    }
}

/// <summary>A torn edge, a cable's end or a smashed console sparking (the mockup's sparks): eight bursts a period, each a spray falling.</summary>
public sealed record Sparker(double X, double Y, double Dx, double Dy, uint Id)
{
    private double[]? _times;
    private double _period;

    /// <summary>The streaks <paramref name="t"/> seconds in, each from (X0, Y0) to (X1, Y1) with its colour; and the flash's alpha.</summary>
    public (List<(double X0, double Y0, double X1, double Y1, Rgba C)> Streaks, double Flash) At(double t)
    {
        if (_times == null)
        {
            _times = new double[8];
            double acc = Hash.Of(Id, 99, 0) * 2;
            for (int k = 0; k < 8; k++)
            {
                _times[k] = acc;
                acc += 0.7 + 2.5 * Hash.Of(Id, k, 5);
            }
            _period = acc;
        }
        var streaks = new List<(double, double, double, double, Rgba)>();
        double flash = 0, tt = t % _period;
        for (int k = 0; k < 8; k++)
        {
            double tau = tt - _times[k];
            if (tau < 0 || tau > 0.9) continue;
            int n = 10 + (int)(Hash.Of(Id, k, 1) * 9);
            if (tau < 0.16) flash = Math.Max(flash, 0.3 * (1 - tau / 0.16));
            for (int j = 0; j < n; j++)
            {
                double life = 0.35 + 0.5 * Hash.Of(Id, k * 31 + j, 2);
                if (tau > life) continue;
                double vx = (Hash.Of(Id, k * 31 + j, 3) * 2 - 1) * 7 + Dx * 3, vy = -Hash.Of(Id, k * 31 + j, 4) * 6 + 2 + Dy * 3;
                double x = X + vx * tau, y = Y + vy * tau + 13 * tau * tau;
                streaks.Add((x, y, x - vx * 0.035, y - (vy + 26 * tau) * 0.035, Rgba.Of(255, 200 + j % 3 * 20, 120 + j % 2 * 60, 1 - tau / life)));
            }
        }
        return (streaks, flash);
    }
}

/// <summary>Vapour out of a burst pipe or tank, or fuel out of a broken bay, freezing as it leaves: crystals running out and fading.</summary>
public sealed record Vent(double X, double Y, double Ux, double Uy, double Len, Mote Kind)
{
    public const int Crystals = 46;

    /// <summary>Each crystal <paramref name="t"/> seconds in: its top-left, size and alpha, in cells.</summary>
    public IEnumerable<(double X, double Y, double Size, double Alpha)> At(double t)
    {
        for (int j = 0; j < Crystals; j++)
        {
            double h = Hash.Of((uint)j, (int)Math.Round(X * 10), 3), u = (t * 0.45 + (double)j / Crystals + h) % 1;
            double d = u * Len, sp = (Hash.Of((uint)j, 7, 4) - 0.5) * u * Len * 0.7;
            yield return (X + Ux * d - Uy * sp, Y + Uy * d + Ux * sp, 0.06 + u * 0.12, (1 - u) * 0.85);
        }
    }
}

/// <summary>A glow: embers in a fresh burnt room (a light too), or hot metal at a torn edge, breathing.</summary>
public sealed record Glow(double X, double Y, double Reach, double Phase, bool Light)
{
    /// <summary>How bright, of its whole, <paramref name="t"/> seconds in (the mockup's ember level, a hot edge's breath).</summary>
    public double Level(double t) => Light ? (0.55 + 0.18 * Math.Sin(t * 1.7 + Phase) + 0.1 * Math.Sin(t * 5.3 + Phase * 2)) * 0.8 : 0.45 + 0.3 * Math.Sin(t * 1.3 + Phase);
}

/// <summary>Numbers in [0, 1) from three whole numbers: the same each frame, a draw of their own.</summary>
public static class Hash
{
    public static double Of(uint a, int b, int c) => WreckLooks.Hash01(a, b, c);
}

/// <summary>
/// What still moves on a wreck (vessel-wrecks W-3, the mockup's fates and render): its motes, drifters, sparkers, vents and glows, each
/// placed from its record as the mockup's events place them, seeded so every run shows the same. An old group keeps a sixth of its motes
/// (rivets two in five), spread twice as far, its ice dull and no embers; a third of its drifters, farther out; no sparks, vents or glows.
/// </summary>
public sealed class WreckLife
{
    /// <summary>The share of an old group's motes kept, rivets', and drifters'.</summary>
    public const double OldMotes = 0.16, OldRivets = 0.4, OldDrifters = 0.3;

    public Dictionary<Mote, List<Speck>> Motes { get; } = [];
    public List<Drifter> Drifters { get; } = [];
    public List<Sparker> Sparkers { get; } = [];
    public List<Vent> Vents { get; } = [];
    public List<Glow> Glows { get; } = [];

    private readonly WreckScene _s;
    private readonly bool _big;
    private Mulberry _r = new(1);
    private bool _old;

    private WreckLife(WreckScene scene)
    {
        _s = scene;
        _big = scene.Built.Vessel.Class is "liner" or "tanker";
        foreach (var k in Enum.GetValues<Mote>()) Motes[k] = [];
    }

    /// <summary>
    /// What moves on <paramref name="scene"/>'s wreck: <paramref name="intact"/> the same vessel built whole, what the events threw out
    /// read against it; <paramref name="ends"/> the cables' and pipes' ends its looks hang (<see cref="WreckLooks.Of"/>); dust within
    /// <paramref name="lights"/>' reach shows warm.
    /// </summary>
    public static WreckLife Of(WreckScene scene, Built intact, IReadOnlyList<WreckLooks.End> ends, IReadOnlyList<(double X, double Y, double Reach)> lights)
    {
        var life = new WreckLife(scene);
        var w = scene.Wreck;
        var lost = Lost(scene, intact);
        for (int k = 0; k < w.Events.Count; k++)
        {
            var e = w.Events[k];
            life._r = new Mulberry(scene.Built.Vessel.Seed * 2246822519u + (uint)k * 3266489917u + 3u);
            life._old = e.Old;
            var thrown = lost.Where(m => Nearest(w, m) == k).ToList();
            var spots = new List<(double X, double Y)>();
            switch (e.Kind)
            {
                case WreckKind.Explosion: life.Blast(e, thrown, spots); break;
                case WreckKind.Fire: life.Fire(e, spots); break;
                case WreckKind.Icing: life.Icing(e, k == 0); break;
                case WreckKind.Asteroid: life.Rock(e, thrown, spots); break;
                default: life.Break(e, thrown, spots, ends.Where(x => x.Event == k).ToList()); break;
            }
            if (e.Old) continue;
            // Fresh: a few of the event's torn edges spark, and most of its cables' cut ends.
            for (int n = 0; n < 3 && spots.Count > 0; n++)
            {
                int at = life._r.Int(spots.Count);
                life.Sparkers.Add(new(spots[at].X, spots[at].Y, 0, 0, (uint)life._r.Int(1_000_000_000)));
                spots.RemoveAt(at);
            }
            foreach (var end in ends.Where(x => x.Event == k && !x.Pipe))
                if (life._r.Chance(0.6)) life.Sparkers.Add(new(end.X, end.Y, 0, 0, (uint)life._r.Int(1_000_000_000)));
        }
        // Dust a lamp's or an ember's light falls on shows warm (the mockup's dustLit).
        var all = lights.Concat(life.Glows.Where(g => g.Light).Select(g => (g.X, g.Y, g.Reach))).ToList();
        var dust = life.Motes[Mote.Dust];
        for (int i = dust.Count - 1; i >= 0; i--)
        {
            var p = dust[i];
            if (!all.Any(l => Math.Sqrt((l.X - p.X) * (l.X - p.X) + (l.Y - p.Y) * (l.Y - p.Y)) < l.Reach * 0.9)) continue;
            life.Motes[Mote.DustLit].Add(p);
            dust.RemoveAt(i);
        }
        return life;
    }

    /// <summary>The machines the intact vessel stood with that the wreck has nowhere near (fourteen cells, a kind matched once): thrown out.</summary>
    private static List<WreckScene.Thing> Lost(WreckScene scene, Built intact)
    {
        var left = scene.Machines.Select(m => (m.Kind, m.X, m.Y)).ToList();
        var lost = new List<WreckScene.Thing>();
        foreach (var p in intact.Parts.Where(p => p.Layer == "machine" && p.Kind != Vessels.Lamp))
        {
            int best = -1, bd = 14 * 14 + 1;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i].Kind != p.Kind) continue;
                int d = (left[i].X - p.X) * (left[i].X - p.X) + (left[i].Y - p.Y) * (left[i].Y - p.Y);
                if (d < bd) (bd, best) = (d, i);
            }
            if (best >= 0)
            {
                left.RemoveAt(best);
                continue;
            }
            var spec = Machines.Get(p.Kind);
            lost.Add(new WreckScene.Thing { Kind = p.Kind, X = p.X, Y = p.Y, W = spec.Width, H = spec.Height });
        }
        return lost;
    }

    /// <summary>The event that threw <paramref name="m"/> out: the nearest that carves, by its centre.</summary>
    private static int Nearest(VesselWreck w, WreckScene.Thing m)
    {
        int best = -1;
        double bd = double.MaxValue;
        for (int k = 0; k < w.Events.Count; k++)
        {
            var e = w.Events[k];
            if (e.Kind is WreckKind.Fire or WreckKind.Icing) continue;
            double d = (e.X - m.X) * (e.X - m.X) + (e.Y - m.Y) * (e.Y - m.Y);
            if (d < bd) (bd, best) = (d, k);
        }
        return best;
    }

    // ---------- the mockup's makers ----------

    private void Dot(Mote kind, double x, double y, double s, double drift = 1)
    {
        if (_old)
        {
            if (kind == Mote.Ember || !_r.Chance(kind == Mote.Rivet ? OldRivets : OldMotes)) return;
            if (kind is Mote.Ice or Mote.Fuel) kind = Mote.IceDull;
        }
        if (x < 0 || y < 0 || x >= _s.Width || y >= _s.Height) return;
        double a = _r.Next() * Math.PI * 2, amp = (0.2 + _r.Next() * 1.1) * drift;
        Motes[kind].Add(new((float)x, (float)y, (float)s, (float)(Math.Cos(a) * amp), (float)(Math.Sin(a) * amp), (float)(0.06 + _r.Next() * 0.22), (float)(_r.Next() * 6.28)));
    }

    private double Gauss() => (_r.Next() + _r.Next() + _r.Next() - 1.5) / 1.5;

    private void Cloud(Mote kind, int n, double cx, double cy, double sx, double sy, (double Lo, double Hi) size, double drift = 1)
    {
        double k = _old ? 2 : 1;
        for (int j = 0; j < n; j++)
        {
            double x = cx + Gauss() * sx * k, y = cy + Gauss() * sy * k;
            Dot(kind, x, y, size.Lo + _r.Next() * (size.Hi - size.Lo), drift);
        }
    }

    private void Fan(Mote kind, int n, double x, double y, double ux, double uy, double spread, double d0, double d1, (double Lo, double Hi) size, double drift = 1)
    {
        double b = Math.Atan2(uy, ux), k = _old ? 1.8 : 1;
        for (int j = 0; j < n; j++)
        {
            double a = b + (_r.Next() - 0.5) * spread, d = (d0 + (d1 - d0) * Math.Pow(_r.Next(), 0.75)) * k;
            Dot(kind, x + Math.Cos(a) * d, y + Math.Sin(a) * d, size.Lo + _r.Next() * (size.Hi - size.Lo), drift);
        }
    }

    private void RoomDots(Mote kind, Vessel.Compartment rm, int n, (double Lo, double Hi) size, bool low = false)
    {
        for (int j = 0; j < n; j++)
        {
            double x = rm.Left + _r.Next() * rm.Width, y = low ? rm.Floor - _r.Next() * _r.Next() * rm.Height : rm.Top + _r.Next() * rm.Height;
            if (_s.Open((int)Math.Floor(x), (int)Math.Floor(y))) Dot(kind, x, y, size.Lo + _r.Next() * (size.Hi - size.Lo), 0.5);
        }
    }

    private void Drift(Drifter d, double fx, double fy)
    {
        if (_old)
        {
            if (!_r.Chance(OldDrifters)) return;
            d = d with { X = fx + (d.X - fx) * 1.8, Y = fy + (d.Y - fy) * 1.8 };
        }
        Drifters.Add(d);
    }

    /// <summary>A drifter's sway and turn drawn as the mockup's: the rest given.</summary>
    private Drifter Make(DriftKind kind, double x, double y, double vx, double vy, double size, IReadOnlyList<(double, double)>? shape = null, string? machine = null,
        uint colour = 0, bool bent = false, double? spin = null)
    {
        double amp = 0.3 + _r.Next() * 0.9, rate = 0.12 + _r.Next() * 0.22, ph = _r.Next() * 6.28, turn = _r.Next() * 6.28, vr = (_r.Next() - 0.5) * 0.35;
        if (kind == DriftKind.Machine) (vr, turn) = (vr * 0.35, (_r.Next() - 0.5) * 1.2);
        return new(kind, x, y, vx, vy, amp, rate, ph, turn, spin ?? vr, size, shape, machine, colour, bent);
    }

    private List<(double, double)> ShardShape()
    {
        int n = 4 + _r.Int(3);
        var pts = new List<(double, double)>();
        for (int j = 0; j < n; j++)
        {
            double a = (double)j / n * Math.PI * 2 + _r.Next() * 0.7, rr = 0.35 + _r.Next() * 0.7;
            pts.Add((Math.Cos(a) * rr, Math.Sin(a) * rr * (0.4 + _r.Next() * 0.4)));
        }
        return pts;
    }

    /// <summary>A stone's outline (the mockup's rockPoly), a unit across.</summary>
    public static List<(double X, double Y)> RockShape(Mulberry r)
    {
        int n = 7 + r.Int(4);
        var pts = new List<(double, double)>();
        for (int j = 0; j < n; j++)
        {
            double a = (double)j / n * Math.PI * 2 + r.Next() * 0.4, rr = 0.6 + r.Next() * 0.45;
            pts.Add((Math.Cos(a) * rr, Math.Sin(a) * rr * (0.65 + r.Next() * 0.3)));
        }
        return pts;
    }

    private static readonly uint[] Bits = [0x8f9599, 0x6e6a63, 0xa7a294, 0x4f5256];

    /// <summary>Debris trailing out of a breach along (<paramref name="ux"/>, <paramref name="uy"/>): shards, bits, stones, a few thrown machines.</summary>
    private void Trail(double x0, double y0, double ux, double uy, int n, double d0 = 2, double d1 = 30, double spread = 1.4, bool rocks = false,
        IReadOnlyList<string>? machines = null, double machineShare = 0.12)
    {
        for (int j = 0; j < n; j++)
        {
            double u = (j + _r.Next()) / n, dist = d0 + (d1 - d0) * Math.Pow(u, 0.85), perp = (_r.Next() - 0.5) * spread * (0.4 + dist / d1 * 2);
            double x = x0 + ux * dist - uy * perp, y = y0 + uy * dist + ux * perp, roll = _r.Next();
            if (machines != null && roll < machineShare)
            {
                string k = machines[_r.Int(machines.Count)];
                Drift(Make(DriftKind.Machine, x, y, ux, uy, 1, machine: k), x0, y0);
            }
            else if (rocks && roll < 0.24) Drift(Make(DriftKind.Rock, x, y, ux, uy, 0.35 + _r.Next() * 0.9, RockShape(_r)), x0, y0);
            else if (roll < 0.72)
            {
                var shape = ShardShape();
                Drift(Make(DriftKind.Shard, x, y, ux, uy, 0.45 + _r.Next() * 1.2, shape, colour: _r.Next() < 0.22 ? 1u : 0u, bent: _r.Next() < 0.4), x0, y0);
            }
            else Drift(Make(DriftKind.Bit, x, y, ux, uy, 0.22 + _r.Next() * 0.3, colour: Bits[_r.Int(Bits.Length)]), x0, y0);
        }
    }

    /// <summary>A machine thrown out of the hull from (<paramref name="hx"/>, <paramref name="hy"/>) along its way, <paramref name="dist"/> cells.</summary>
    private void Eject(WreckScene.Thing m, double hx, double hy, double ux, double uy, double dist)
    {
        double x = hx + ux * dist + (_r.Next() - 0.5) * 3, y = hy + uy * dist + (_r.Next() - 0.5) * 3;
        Drift(Make(DriftKind.Machine, x, y, ux, uy, 1, machine: m.Kind), m.X + m.W / 2.0, m.Y + m.H / 2.0);
    }

    private (int Top, int Bottom) Body()
    {
        var bodies = _s.Built.Vessel.Sections.Where(k => k.Kind == Vessel.SectionKind.Body).ToList();
        return (bodies.Min(k => k.Top), bodies.Max(k => k.Bottom));
    }

    /// <summary>The torn edges of an event's carving, a hull cell beside each: where it may spark.</summary>
    private List<(double X, double Y)> Edges(WreckEvent e)
    {
        var spots = new List<(double, double)>();
        foreach (int i in e.Carved)
        {
            int x = i % _s.Width, y = i / _s.Width;
            if (_s.At(x, y) != WreckScene.Space) continue;
            if (_s.Skin(x, y - 1) || _s.Skin(x, y + 1) || _s.Skin(x - 1, y) || _s.Skin(x + 1, y) || _s.Open(x, y + 1)) spots.Add((x + 0.5, y + 0.5));
        }
        return spots;
    }

    // ---------- the events ----------

    private void Blast(WreckEvent e, List<WreckScene.Thing> thrown, List<(double X, double Y)> spots)
    {
        double cx = e.X + 0.5, cy = e.Y + 0.5, R = e.Radius;
        var (top, bottom) = Body();
        bool toTop = cy - top < bottom - cy;
        foreach (var m in thrown)
        {
            double ax = m.X + m.W / 2.0 - cx, ay = m.Y + m.H / 2.0 - cy, l = Math.Sqrt(ax * ax + ay * ay) is var h && h > 0 ? h : 1;
            if (_r.Chance(0.4)) continue;
            Eject(m, cx + ax / l * R, cy + ay / l * R, ax / l, ay / l, 4 + _r.Next() * 18);
        }
        spots.AddRange(Edges(e));
        if (!_old)
            foreach (var (x, y) in spots)
                if (Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) < R * 1.6 && _r.Chance(0.3)) Glows.Add(new(x, y, 0, _r.Next() * 6, false));
        Trail(cx, toTop ? top - 1 : bottom + 1, 0.15, toTop ? -1 : 1, _big ? 30 : 20, 2, 30, 10);
        Cloud(Mote.Ash, _big ? 700 : 420, cx, toTop ? top - 3 : bottom + 3, R * 1.6, R * 0.9, (0.06, 0.22));
        Cloud(Mote.AshLight, 200, cx, cy, R * 1.2, R * 0.9, (0.05, 0.15));
        Cloud(Mote.Ash, _big ? 500 : 300, cx, cy, R * 1.8, R * 0.9, (0.05, 0.18), 0.6);
        if (!_old) Cloud(Mote.Ember, 60, cx, cy, R * 0.9, R * 0.6, (0.08, 0.16), 0.4);
    }

    private void Fire(WreckEvent e, List<(double X, double Y)> spots)
    {
        for (int k = 0; k < e.Compartments.Count; k++)
        {
            double I = (k < e.Heats.Count ? e.Heats[k] : 100) / 100.0;
            foreach (var rm in _s.Rooms[e.Compartments[k]])
            {
                if (!_old)
                {
                    RoomDots(Mote.Ash, rm, (int)Math.Round(rm.Width * rm.Height * 0.35 * I), (0.06, 0.2));
                    RoomDots(Mote.AshLight, rm, (int)Math.Round(rm.Width * rm.Height * 0.12 * I), (0.05, 0.14));
                    if (I > 0.55) RoomDots(Mote.Ember, rm, (int)Math.Round(rm.Width * I * 1.2), (0.08, 0.16), low: true);
                    if (I > 0.4)
                    {
                        double x = rm.Holds(e.X, e.Y) ? e.X + 0.5 : rm.Left + rm.Width / 2.0;
                        Glows.Add(new(x, rm.Floor - 0.6, Math.Clamp(rm.Width * 0.5, 5, 12), k * 1.7, true));
                    }
                }
                else RoomDots(Mote.Ash, rm, (int)Math.Round(rm.Width * 0.8 * I), (0.08, 0.2), low: true);
                if (I > 0.3) spots.Add((rm.Left + 2 + _r.Next() * Math.Max(0, rm.Width - 4), rm.Top + 0.6));
            }
        }
    }

    private static readonly System.Text.RegularExpressions.Regex Wet = new("LifeTank|salvageTanks|Pump|Valves|Manifold|Riser|vesselTank|Stove|Bar");

    private void Icing(WreckEvent e, bool first)
    {
        if (first && e.Radius == 0)
        {
            var wet = _s.Machines.Where(m => Wet.IsMatch(m.Kind)).ToList();
            for (int k = wet.Count - 1; k > 0; k--)
            {
                int j = _r.Int(k + 1);
                (wet[k], wet[j]) = (wet[j], wet[k]);
            }
            Burst(wet.Take(_big ? 7 : 3));
            foreach (var rooms in _s.Rooms)
                foreach (var rm in rooms) RoomDots(Mote.Ice, rm, (int)Math.Round(rm.Width * rm.Height * 0.05), (0.04, 0.09));
            return;
        }
        Freeze(e.X + 0.5, e.Y + 0.5, e.DirX, e.DirY, e.Compartments);
    }

    /// <summary>A breach's cold (the mockup's freeze): a fan of crystals out of it, burst pipes in its frozen rooms venting, rime in the air there.</summary>
    private void Freeze(double bx, double by, int dirX, int dirY, IReadOnlyList<int> frozen)
    {
        double ux = dirX * 0.55, uy = dirY == 0 ? -1 : dirY, l = Math.Sqrt(ux * ux + uy * uy);
        (ux, uy) = (ux / l, uy / l);
        double len = _big ? 24 : 16;
        Fan(Mote.Ice, _big ? 520 : 360, bx, by, ux, uy, 1.15, 0.5, len, (0.05, 0.15));
        var set = frozen.ToHashSet();
        Burst(_s.Machines.Where(m => Wet.IsMatch(m.Kind) && set.Contains(_s.RoomAt(m.X + m.W / 2, m.Y + m.H - 1))).Take(4));
        foreach (int c in frozen)
            foreach (var rm in _s.Rooms[c]) RoomDots(Mote.Ice, rm, (int)Math.Round(rm.Width * rm.Height * 0.12), (0.04, 0.1));
    }

    private void Burst(IEnumerable<WreckScene.Thing> machines)
    {
        foreach (var m in machines)
        {
            double x = m.X + m.W * (0.3 + _r.Next() * 0.4);
            Fan(Mote.Ice, 70, x, m.Y, 0, -1, 2.2, 0.3, 4, (0.05, 0.12), 0.4);
            if (!_old) Vents.Add(new(x, m.Y + 0.3, (_r.Next() - 0.5) * 0.6, -1, 3 + _r.Next() * 2, Mote.Ice));
        }
    }

    private static readonly string[] Thrown = ["salvageSection", "salvageSlab", "crate", "salvageCrate", Vessels.Chair];

    private void Rock(WreckEvent e, List<WreckScene.Thing> thrown, List<(double X, double Y)> spots)
    {
        double ccx = e.X + 0.5, top = e.Y, R = e.Radius;
        int side = Math.Sign(e.DirX) is var s && s != 0 ? s : 1;
        double fx = side, fy = -0.38, fl = Math.Sqrt(fx * fx + fy * fy);
        (fx, fy) = (fx / fl, fy / fl);
        double sl = Math.Sqrt(e.DirX * e.DirX + e.DirY * e.DirY), ux = e.DirX / sl, uy = e.DirY / sl;
        foreach (var m in thrown) Eject(m, ccx, top - 1, fx, fy, 4 + _r.Next() * 40);
        spots.AddRange(Edges(e));
        Trail(ccx, top - 1, fx, fy, _big ? 70 : 46, 1, _big ? 70 : 50, 9, rocks: true, machines: Thrown, machineShare: 0.08);
        for (int k = 0; k < 8; k++)
        {
            double x = ccx - ux * (4 + _r.Next() * 20) + (_r.Next() - 0.5) * 6, y = top - uy * (4 + _r.Next() * 12);
            Drift(Make(DriftKind.Rock, x, y, -ux, -uy, 0.3 + _r.Next() * 1.2, RockShape(_r)), ccx, top);
        }
        Cloud(Mote.Dust, _big ? 900 : 550, ccx + fx * 4, top - 2, R * 1.4, R * 0.7, (0.06, 0.2));
        Cloud(Mote.RockDust, _big ? 400 : 260, ccx, top + 1, R * 0.9, R * 0.6, (0.08, 0.22));
        for (int j = 0; j < (_big ? 1100 : 700); j++)
        {
            double d = 2 + Math.Pow(_r.Next(), 0.8) * (_big ? 72 : 52), sp = (_r.Next() - 0.5) * (2 + d * 0.32);
            double x = ccx + fx * d - fy * sp, y = top - 1 + fy * d + fx * sp, roll = _r.Next();
            Dot(roll < 0.5 ? Mote.Dust : roll < 0.75 ? Mote.Paint : roll < 0.92 ? Mote.Bit : Mote.RockDust, x, y, 0.05 + _r.Next() * 0.16);
        }
        // A strike freezes its own breach.
        Freeze(ccx, top, e.DirX, -1, e.Compartments);
    }

    private void Break(WreckEvent e, List<WreckScene.Thing> thrown, List<(double X, double Y)> spots, List<WreckLooks.End> ends)
    {
        var b = e.Break!;
        var (top, bottom) = Body();
        int gy0 = top + Math.Min(b.SternRows, b.BowRows), gy1 = bottom + Math.Max(b.SternRows, b.BowRows);
        double gcx = e.X, gcy = (gy0 + gy1) / 2.0, gap = b.Gap;
        foreach (var m in thrown)
        {
            if (_r.Chance(0.3)) continue;
            double x = gcx + (_r.Next() - 0.5) * gap * 0.6, y = m.Y + (b.SternRows + b.BowRows) / 2.0 + (_r.Next() - 0.5) * 6;
            Drift(Make(DriftKind.Machine, x, y, (_r.Next() - 0.5) * 0.3, _r.Next() < 0.5 ? -1 : 1, 1, machine: m.Kind, spin: (_r.Next() - 0.5) * 0.12), b.Column, m.Y + m.H / 2.0);
        }
        double gx0 = b.Column - gap / 2;
        for (int k = 0; k < 22; k++)
        {
            double x = gx0 + 1 + _r.Next() * (gap - 2), y = gy0 - 2 + _r.Next() * (gy1 - gy0 + 4);
            if (_r.Next() < 0.65) Drift(Make(DriftKind.Shard, x, y, 0.2, _r.Next() - 0.5, 0.5 + _r.Next(), ShardShape(), colour: _r.Next() < 0.3 ? 1u : 0u, bent: _r.Next() < 0.4), b.Column, y);
            else Drift(Make(DriftKind.Bit, x, y, 0.3, 1, 0.3, colour: 0x8f9599), b.Column, y);
        }
        Cloud(Mote.Bit, 260, gcx, gcy, gap * 0.4, (gy1 - gy0) * 0.45, (0.05, 0.14), 0.7);
        spots.AddRange(Edges(e));
        if (e.Cause == null)
        {
            Cloud(Mote.Rivet, 70, gcx, gcy, gap * 0.6, (gy1 - gy0) * 0.5, (0.12, 0.2), 0.8);
            if (_s.Built.Vessel.Class == "tanker")
            {
                // Fuel out of the broken bay: crystals fanning from the face nearer a tank bay, venting while fresh.
                var bay = _s.Built.Vessel.Compartments.Select((c, i) => (c, i)).Where(t => t.c.Role is VesselRole.TankBay or VesselRole.PumpRoom)
                    .OrderBy(t => Math.Min(Math.Abs(t.c.Left + t.c.Width - (gcx - gap / 2)), Math.Abs(t.c.Left - (gcx + gap / 2)))).Select(t => (Vessel.Compartment?)t.c).FirstOrDefault();
                bool stern = bay == null || bay.Left + bay.Width / 2.0 < gcx;
                double fx = stern ? gcx - gap / 2 : gcx + gap / 2, fy = bay != null ? bay.Top + bay.Height / 2.0 : gcy;
                Fan(Mote.Fuel, 460, fx, fy, stern ? 1 : -1, -0.2, 1.3, 0.5, gap * 1.6, (0.05, 0.14));
                if (!_old) Vents.Add(new(fx, fy, stern ? 1 : -1, -0.1, gap * 0.7, Mote.Fuel));
            }
        }
        else if (e.Cause == WreckKind.Asteroid)
        {
            for (int k = 0; k < 10; k++)
                Drift(Make(DriftKind.Rock, gcx + (_r.Next() - 0.5) * gap * 1.4, gy0 - 2 - _r.Next() * 10, 0, -1, 0.3 + _r.Next() * 1.1, RockShape(_r)), gcx, gy0);
            Cloud(Mote.Dust, 600, gcx, gy0, 10, 6, (0.06, 0.2));
            Cloud(Mote.RockDust, 220, gcx, gy0 + 2, 6, 4, (0.08, 0.2));
            Cloud(Mote.Bit, 300, gcx, gcy, gap * 0.45, (gy1 - gy0) * 0.45, (0.05, 0.15));
        }
        else
        {
            Cloud(Mote.Ash, 500, gcx, gcy, gap * 0.6, (gy1 - gy0) * 0.5, (0.06, 0.2));
            if (!_old) Cloud(Mote.Ember, 40, gcx, gcy, gap * 0.3, (gy1 - gy0) * 0.3, (0.08, 0.14), 0.4);
        }
        // Pipes torn at the faces: frozen spray out of the first three, venting while fresh.
        foreach (var end in ends.Where(x => x.Pipe).Take(3))
        {
            double dx = end.Stern ? 0.6 : -0.6;
            Fan(Mote.Ice, 90, end.X, end.Y, dx, 0.8, 1.2, 0.3, 5, (0.04, 0.12));
            if (!_old) Vents.Add(new(end.X, end.Y, end.Stern ? 0.7 : -0.7, 0.7, 4, Mote.Ice));
        }
    }

    /// <summary>The motes of <paramref name="kind"/> in each chunk they hang over (<see cref="WreckPaint.ChunkCells"/>): what each picture of them holds.</summary>
    public Dictionary<(int X, int Y), List<Speck>> Chunks(Mote kind)
    {
        var chunks = new Dictionary<(int, int), List<Speck>>();
        foreach (var p in Motes[kind])
        {
            var key = WreckPaint.ChunkOf((int)Math.Floor(p.X), (int)Math.Floor(p.Y));
            if (!chunks.TryGetValue(key, out var list)) chunks[key] = list = [];
            list.Add(p);
        }
        return chunks;
    }

    /// <summary>The items of <paramref name="items"/> whose place, by <paramref name="at"/>, lies within <paramref name="margin"/> cells of the view (in cells).</summary>
    public static IEnumerable<T> InView<T>(IEnumerable<T> items, Func<T, (double X, double Y)> at, (double X0, double Y0, double X1, double Y1) view, double margin) =>
        items.Where(i => at(i) is var (x, y) && x >= view.X0 - margin && x <= view.X1 + margin && y >= view.Y0 - margin && y <= view.Y1 + margin);
}
