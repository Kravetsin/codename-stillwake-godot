using System;
using System.Collections.Generic;
using Stillwake.Game.Render;
using static Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The sparks running electric tools throw off a wreck (damage-looks, "sparks"): thin streaks thrown fast where each touches, a
/// steady <see cref="WorkLooks.SparksPerSecond"/>, white-hot cooling to orange and red; out of the face toward the tool, the
/// grinder's in a tight fan off its wheel; and a white bead where it touches. Each spark keeps where it was thrown from and flies
/// on alone; its way, speed and life are hashes of its number, as the page's were.
/// </summary>
public sealed class ToolSparks
{
    private readonly record struct Spark(double X, double Y, double Dx, double Dy, double Speed, double Life, double Born);

    private readonly List<Spark> _sparks = [];
    private readonly Dictionary<int, double> _owed = new();
    private readonly List<(double X, double Y)> _beads = [];
    private readonly HashSet<int> _touching = [];
    private readonly List<int> _left = [];
    private double _clock;
    private int _thrown;

    public int Count => _sparks.Count;

    /// <summary>A frame of <paramref name="elapsedMs"/> begins: sparks burnt out go, and every tool's touch is to be told again.</summary>
    public void Begin(double elapsedMs)
    {
        _clock += elapsedMs;
        _sparks.RemoveAll(s => _clock - s.Born >= s.Life);
        _beads.Clear();
        _touching.Clear();
    }

    /// <summary>
    /// <paramref name="hero"/>'s <paramref name="tool"/> touches a wreck at <paramref name="at"/>, the face's way back toward the
    /// tool <paramref name="toward"/> (a unit vector), through this frame of <paramref name="elapsedMs"/>: its sparks thrown, the
    /// first at once when it starts touching.
    /// </summary>
    public void Touch(int hero, string tool, (double X, double Y) at, (double X, double Y) toward, double elapsedMs)
    {
        _touching.Add(hero);
        _beads.Add(at);
        double owed = _owed.TryGetValue(hero, out var o) ? o + WorkLooks.SparksPerSecond * Fx.Count * elapsedMs / 1000 : 1;
        var (bx, by, spread) = tool == Content.Weapons.Grinder.Item ? Wheel(toward) : (toward.X, toward.Y, Spread(tool));
        for (; owed >= 1; owed--)
        {
            int i = _thrown++;
            double turn = (Hash(i, 1) - 0.5) * 2 * spread, cos = Math.Cos(turn), sin = Math.Sin(turn);
            double life = WorkLooks.SparkLife.Least + Hash(i, 2) * (WorkLooks.SparkLife.Most - WorkLooks.SparkLife.Least);
            double speed = WorkLooks.SparkSpeed.Least + Hash(i, 3) * (WorkLooks.SparkSpeed.Most - WorkLooks.SparkSpeed.Least);
            // Thrown through the frame, not all at its end.
            double born = _clock - (owed - 1) / WorkLooks.SparksPerSecond * 1000;
            _sparks.Add(new Spark(at.X, at.Y, bx * cos - by * sin, bx * sin + by * cos, speed, life, born));
        }
        _owed[hero] = owed;
    }

    /// <summary>The frame's touches are all told: a tool no longer touching starts afresh when it touches again.</summary>
    public void End()
    {
        _left.Clear();
        foreach (int hero in _owed.Keys)
            if (!_touching.Contains(hero)) _left.Add(hero);
        foreach (int hero in _left) _owed.Remove(hero);
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
        double age = _clock - s.Born, q = age / s.Life;
        if (q >= 1 || age < 0) return null;
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
