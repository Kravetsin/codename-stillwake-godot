using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using static Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The sparks running electric tools throw off a wreck (damage-looks, "sparks"): thin streaks thrown fast where each touches, a
/// steady <see cref="WorkLooks.SparksPerSecond"/>, white-hot cooling to orange and red; out of the face toward the tool, the
/// grinder's in a tight fan off its wheel; and, but for the grinder's, a white bead where it touches. Each spark keeps where it was thrown from and flies
/// on alone; its way, speed and life are hashes of its number, as the page's were.
/// </summary>
public sealed class ToolSparks
{
    // Thrown from (X, Y) along (Dx, Dy) once it has ridden Ride ms: of the wheel's rim from angle From (grinder-contact), or back
    // along the drill's spiral from From pixels past its point, told at Told ms (drill-contact).
    private readonly record struct Spark(double X, double Y, double Dx, double Dy, double Speed, double Life, double Born, ToolContact.Circle? Rim = null,
        double From = 0, double Ride = 0, ToolContact.Screw? Screw = null, double Told = 0);

    private readonly List<Spark> _sparks = [];
    private readonly Dictionary<(int Hero, int Piece), double> _owed = new();
    private readonly List<(double X, double Y)> _beads = [];
    private readonly HashSet<(int Hero, int Piece)> _touching = [];
    private readonly List<(int Hero, int Piece)> _left = [];
    private double _clock;
    private int _thrown;

    public int Count => _sparks.Count;

    /// <summary>A frame of <paramref name="elapsedMs"/> begins: sparks burnt out go, and every tool's touch is to be told again.</summary>
    public void Begin(double elapsedMs)
    {
        _clock += elapsedMs;
        _sparks.RemoveAll(s => _clock - s.Born >= s.Ride + s.Life);
        _beads.Clear();
        _touching.Clear();
    }

    /// <summary>
    /// A hero's <paramref name="tool"/> touches a wreck at <paramref name="at"/>, the two of them <paramref name="touch"/>, the face's way back toward the
    /// tool <paramref name="toward"/> (a unit vector), through this frame of <paramref name="elapsedMs"/>: its sparks thrown, the
    /// first at once when it starts touching. A grinder's <paramref name="wheel"/> with <paramref name="exits"/> from the wreck
    /// throws them off its rim instead (grinder-contact): each rides the rim along the spin to an exit and flies on along it there.
    /// A drill's <paramref name="screw"/> with its <paramref name="bore"/> into the wreck throws them up its spiral (drill-contact):
    /// each rides it back from inside the bore to the mouth and flies on along it there. Without an exit or a bore they still come
    /// off the haze, from its point nearest the touch.
    /// </summary>
    public void Touch((int Hero, int Piece) touch, string tool, (double X, double Y) at, (double X, double Y) toward, double elapsedMs,
        ToolContact.Circle? wheel = null, IReadOnlyList<ToolContact.Exit>? exits = null, ToolContact.Screw? screw = null, ToolContact.Bore? bore = null)
    {
        _touching.Add(touch);
        if (wheel is { } round && exits is not { Count: > 0 }) exits = [ToolContact.Nearest(round, at, toward)];
        if (screw is { } spiral && bore == null) bore = ToolContact.Under(spiral, at);
        bool rim = wheel != null, up = !rim && screw != null;
        // The grinder's and the drill's sparks come off the tool itself, with no bead where it touches (grinder-contact, drill-contact).
        if (tool != Content.Weapons.Grinder.Item && tool != Content.Weapons.Drill.Item) _beads.Add(at);
        double owed = _owed.TryGetValue(touch, out var o) ? o + WorkLooks.SparksPerSecond * Fx.Count * elapsedMs / 1000 : 1;
        var (bx, by, spread) = rim ? (0.0, 0.0, WorkLooks.GrinderSpread) : up ? (0.0, 0.0, WorkLooks.ScrewSpread) : tool == Content.Weapons.Grinder.Item ? Wheel(toward) : (toward.X, toward.Y, Spread(tool));
        for (; owed >= 1; owed--)
        {
            int i = _thrown++;
            double turn = (Hash(i, 1) - 0.5) * 2 * spread, cos = Math.Cos(turn), sin = Math.Sin(turn);
            double life = WorkLooks.SparkLife.Least + Hash(i, 2) * (WorkLooks.SparkLife.Most - WorkLooks.SparkLife.Least);
            double speed = WorkLooks.SparkSpeed.Least + Hash(i, 3) * (WorkLooks.SparkSpeed.Most - WorkLooks.SparkSpeed.Least);
            // Thrown through the frame, not all at its end.
            double born = _clock - (owed - 1) / WorkLooks.SparksPerSecond * 1000;
            if (up)
            {
                _sparks.Add(UpTheScrew(screw!.Value, bore!.Value, i, speed, life, born, cos, sin));
                continue;
            }
            if (!rim)
            {
                _sparks.Add(new Spark(at.X, at.Y, bx * cos - by * sin, bx * sin + by * cos, speed, life, born));
                continue;
            }
            var w = wheel!.Value;
            // Anywhere on the rim, ridden along the spin to the next exit, up to a whole turn.
            double from = Hash(i, 4) * 2 * Math.PI;
            var (e, way) = NextExit(w, from, exits!);
            double ride = way / (2 * Math.PI * WorkLooks.WheelTurns) * 1000;
            _sparks.Add(new Spark(e.At.X, e.At.Y, e.Way.X * cos - e.Way.Y * sin, e.Way.X * sin + e.Way.Y * cos, speed, life, born, w, from, ride));
        }
        _owed[touch] = owed;
    }

    /// <summary>The frame's touches are all told: a tool no longer touching a wreck starts afresh on it when it touches again.</summary>
    public void End()
    {
        _left.Clear();
        foreach (var touch in _owed.Keys)
            if (!_touching.Contains(touch)) _left.Add(touch);
        foreach (var touch in _left) _owed.Remove(touch);
    }

    // Born anywhere in the bore past the point (behind it while the bore is only round the bit), ridden back at its own speed to the
    // mouth, thrown along the spiral there as it will be when it gets there.
    private Spark UpTheScrew(ToolContact.Screw screw, ToolContact.Bore bore, int i, double speed, double life, double born, double cos, double sin)
    {
        double from = bore.Deepest >= 0 ? Math.Max(bore.Mouth, 0) : bore.Mouth;
        from += Hash(i, 4) * (bore.Deepest - from);
        double ride = (from - bore.Mouth) / speed * 1000, phase = screw.Phase + ToolContact.ScrewSpin * (born + ride - _clock) / 1000;
        var (x, y) = screw.At(bore.Mouth, phase);
        var (wx, wy) = screw.Rides(bore.Mouth, phase, speed);
        return new Spark(x, y, wx * cos - wy * sin, wx * sin + wy * cos, speed, life, born, From: from, Ride: ride, Screw: screw, Told: _clock);
    }

    // The first of exits met riding w's rim along its spin from angle from, and how far, radians.
    private static (ToolContact.Exit Exit, double Way) NextExit(ToolContact.Circle w, double from, IReadOnlyList<ToolContact.Exit> exits)
    {
        var best = (exits[0], double.MaxValue);
        foreach (var e in exits)
        {
            double way = (w.Spin * (e.Angle - from)) % (2 * Math.PI);
            if (way < 0) way += 2 * Math.PI;
            if (way < best.Item2) best = (e, way);
        }
        return best;
    }

    private static double Spread(string tool) => tool == Content.Weapons.PlasmaCutter.Item ? WorkLooks.CutterSpread : WorkLooks.DrillSpread;

    /// <summary>The way the grinder's wheel throws: back toward the tool a little, and down past it, as it is held either way.</summary>
    public static (double X, double Y, double Spread) Wheel((double X, double Y) toward)
    {
        double side = toward.X <= 0 ? 1 : -1, px = toward.Y * side, py = -toward.X * side;
        double x = WorkLooks.WheelBack * toward.X + WorkLooks.WheelDown * px, y = WorkLooks.WheelBack * toward.Y + WorkLooks.WheelDown * py;
        double l = Math.Sqrt(x * x + y * y);
        return (x / l, y / l, WorkLooks.GrinderSpread);
    }

    /// <summary>Where spark <paramref name="n"/> is now, and its streak's tail: none once burnt out.</summary>
    public ((double X, double Y) Tail, (double X, double Y) Head, double Age)? At(int n)
    {
        var s = _sparks[n];
        double age = _clock - s.Born;
        if (age < 0 || age >= s.Ride + s.Life) return null;
        if (age < s.Ride && s.Rim is { } w)
        {
            // On the rim: white-hot, its streak along the way the rim moves.
            double angle = s.From + w.Spin * 2 * Math.PI * WorkLooks.WheelTurns * age / 1000, streak = s.Speed * WorkLooks.SparkStreak;
            var (rx, ry) = w.At(angle);
            var (ux, uy) = w.Along(angle);
            return ((rx - ux * streak, ry - uy * streak), (rx, ry), 0);
        }
        if (age < s.Ride && s.Screw is { } sc)
        {
            // Up the spiral: its streak along the way it rides there.
            double along = s.From - s.Speed * age / 1000, phase = sc.Phase + ToolContact.ScrewSpin * (_clock - s.Told) / 1000, streak = s.Speed * WorkLooks.SparkStreak;
            var (px, py) = sc.At(along, phase);
            var (ux, uy) = sc.Rides(along, phase, s.Speed);
            return ((px - ux * streak, py - uy * streak), (px, py), 0);
        }
        age -= s.Ride;
        double q = age / s.Life;
        double run = s.Speed * age / 1000 * (1 - 0.3 * q), tail = s.Speed * WorkLooks.SparkStreak * (1 - 0.6 * q);
        double x = s.X + s.Dx * run, y = s.Y + s.Dy * run;
        return ((x - s.Dx * tail, y - s.Dy * tail), (x, y), q);
    }

    public void Draw(Ink ink)
    {
        for (int n = 0; n < _sparks.Count; n++)
        {
            if (At(n) is not var (tail, head, q)) continue;
            var tone = q < 0.25 ? WorkLooks.SparkHot : q < 0.6 ? WorkLooks.SparkWarm : WorkLooks.SparkCool;
            ink.Segment(tail, head, WorkLooks.SparkWidth, tone.Faded((float)(1 - q * 0.5)));
        }
        int flicker = (int)(_clock / WorkLooks.BeadMs);
        foreach (var bead in _beads)
            ink.Fill(Blob(new Rng(flicker), bead.X, bead.Y, WorkLooks.BeadRadius, 6, 0.5), bead, WorkLooks.SparkHot);
    }
}
