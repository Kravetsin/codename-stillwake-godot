using System;
using System.Collections.Generic;
using Look = Stillwake.Game.WristLook;

namespace Stillwake.Game.Shared;

/// <summary>
/// How the air bar moves (hud-vitals, the air-motion pick E «as now, but smooth»): a piece per part of the air, each lit as far as
/// a <see cref="CriticalSpring"/> following the air, stepped with the real time between frames, never rounded. What a breath
/// changes is spread over <see cref="Look.BreathS"/>, so a draining or filling bar glides instead of stepping once a breath; a
/// hit, a canister or a part put on or taken off moves the aim at once and the springs ease it. A part put on grows from
/// nothing where it goes, one taken off shrinks away with its light. Plain C#: read by its row four times a second, stepped each frame.
/// </summary>
public sealed class AirMotion
{
    /// <summary>The key of the one part the air is drawn in when its parts do not add up to its capacity.</summary>
    public const string Whole = "";

    /// <summary>
    /// A part laid on the bar, in block units: its left, its length, how far along it is lit; its units, whether it is still
    /// worn (one taken off shrinks away), and the units of the worn parts before it.
    /// </summary>
    public readonly record struct Laid(float X, float Length, float Lit, int Size, bool Present, double Before);

    private sealed class Piece(string key, int size, double width, double units)
    {
        public readonly string Key = key;
        public int Size = size;
        public bool Present = true;
        public double Aim;
        public CriticalSpring Width = new(width), Units = new(units);
    }

    private readonly List<Piece> _pieces = [];
    private readonly List<(string Key, int Size)> _wanted = [];
    private readonly List<Laid> _laid = [];
    private CriticalSpring _red, _gain, _edge;
    private bool _read, _hurt;
    private int _last;
    private double _rampFrom, _rampTo, _rampAge = Look.BreathS, _gainAge = double.MaxValue, _surgeWait = -1;
    private double _hitAge = -1, _hitFrom, _hitTo, _canAge = -1, _canFrom, _canTo, _again = -1;
    private bool _hitHurt;

    /// <summary>Whether the suit runs short: the bar turns red.</summary>
    public bool Short { get; set; }

    /// <summary>The air the bar aims at now, in units: the last read, a breath's change spread over a breath.</summary>
    public double Aim => _rampFrom + (_rampTo - _rampFrom) * Math.Min(1, _rampAge / Look.BreathS);

    /// <summary>The air shown lit, in units, the worn parts together.</summary>
    public double Shown { get; private set; }

    /// <summary>How red the bar is, 0 to 1; how much the refill's light shows; how much the moving edge's highlight does.</summary>
    public double Red => Math.Clamp(_red.X, 0, 1);
    public double Gain => Math.Clamp(_gain.X, 0, 1);
    public double Moving => Math.Clamp(_edge.X, 0, 1);

    /// <summary>The parts as laid now, worn ones and those shrinking away, in order.</summary>
    public IReadOnlyList<Laid> Parts => _laid;

    /// <summary>Where the lit light ends, and where the last part does, in block units.</summary>
    public float EdgeX { get; private set; } = Vitals.RowLeft;
    public float End { get; private set; } = Vitals.RowLeft;

    /// <summary>The last hit: seconds since, the air before and after it in units, and whether a hurt tints it; none once it is gone.</summary>
    public (double Age, double From, double To, bool Hurt)? Hit => _hitAge >= 0 ? (_hitAge, _hitFrom, _hitTo, _hitHurt) : null;

    /// <summary>The far edge of a hit's lost air, in units: lit whole a moment, then drained smoothly back to what is left.</summary>
    public double? Lost => Hit is { } h ? h.To + (h.From - h.To) * (1 - Smooth((h.Age - Look.HitHoldS) / Look.HitDrainS)) : null;

    /// <summary>The last canister breathed: seconds since, the air before and after it; none once its light has run.</summary>
    public (double Age, double From, double To)? Canister => _canAge >= 0 ? (_canAge, _canFrom, _canTo) : null;

    /// <summary>Seconds since room air was breathed again, while its wave runs; none after.</summary>
    public double? Again => _again >= 0 ? _again : null;

    /// <summary>
    /// A read of the hero's <paramref name="air"/> of <paramref name="capacity"/> units in its <paramref name="parts"/>, one piece
    /// unless they add up to the capacity. A drop of <paramref name="hit"/> units or more, or any after a <see cref="Hurt"/>, is a
    /// hit; a rise of <paramref name="fill"/> or less a breath's refill; a rise after a <see cref="Surge"/> a canister's.
    /// </summary>
    public void Read(int air, int capacity, IReadOnlyList<(string Key, int Size)> parts, double hit, int fill)
    {
        air = Math.Clamp(air, 0, Math.Max(0, capacity));
        _wanted.Clear();
        int sum = 0;
        foreach (var (_, size) in parts) sum += size;
        if (sum == capacity && parts.Count > 0) _wanted.AddRange(parts);
        else if (capacity > 0) _wanted.Add((Whole, capacity));
        double now = Aim;
        if (!_read)
        {
            _read = true;
            foreach (var (key, size) in _wanted) _pieces.Add(new Piece(key, size, 1, 0));
            Hold(air);
            Aims();
            foreach (var p in _pieces) p.Units = new CriticalSpring(p.Aim);
        }
        else if (Refit())
        {
            // A part put on or taken off: its air is laid anew, nothing lost.
            Fit();
            Hold(air);
        }
        else if (air < _last)
        {
            bool struck = _last - air >= hit || _hurt;
            if (struck) (_hitAge, _hitFrom, _hitTo, _hitHurt) = (0, now, air, _hurt);
            if (struck) Hold(air);
            else Ramp(now, air);
        }
        else if (air > _last)
        {
            bool canister = _surgeWait >= 0;
            if (canister) (_canAge, _canFrom, _canTo, _surgeWait) = (0, now, air, -1);
            else _gainAge = 0;
            if (!canister && air - _last <= fill) Ramp(now, air);
            else Hold(air);
        }
        _hurt = false;
        _last = air;
    }

    /// <summary>A canister breathed: the next rise is its air; read none within <see cref="Look.CanisterWaitS"/>, its light runs anyway.</summary>
    public void Surge() => _surgeWait = 0;

    /// <summary>Room air breathed again: a wave runs across.</summary>
    public void Wave() => _again = 0;

    /// <summary>A hurt: the next read's loss is a hit, tinted in the hurt's colour.</summary>
    public void Hurt() => _hurt = true;

    /// <summary>Moves it <paramref name="seconds"/> on, its bar to end by <paramref name="end"/> (<see cref="Vitals.AirEnd"/>).</summary>
    public void Step(double seconds, float end)
    {
        double s = Math.Max(0, seconds);
        _rampAge += s;
        _gainAge += s;
        Age(ref _hitAge, s, Look.HitS);
        Age(ref _canAge, s, Look.CanisterS);
        Age(ref _again, s, Look.WaveS);
        if (_surgeWait >= 0 && (_surgeWait += s) > Look.CanisterWaitS) (_canAge, _canFrom, _canTo, _surgeWait) = (0, Aim, Aim, -1);
        Aims();
        double shown = 0, speed = 0;
        foreach (var p in _pieces)
        {
            p.Width.Step(p.Present ? 1 : 0, Look.AirResize, s);
            p.Units.Step(p.Aim, Look.AirFollow, s);
            p.Units.X = Math.Clamp(p.Units.X, 0, p.Size);
            if (!p.Present) continue;
            shown += p.Units.X;
            speed += p.Units.V;
        }
        for (int i = _pieces.Count - 1; i >= 0; i--)
            if (!_pieces[i].Present && _pieces[i].Width.X <= 0.003) _pieces.RemoveAt(i);
        _red.Step(Short ? 1 : 0, Look.AirRed, s);
        _gain.Step(_gainAge < Look.GainS ? 1 : 0, Look.AirGain, s);
        _edge.Step(Math.Clamp(Math.Abs(speed) / Look.EdgeSpeed, 0, 1), Look.AirEdge, s);
        Shown = shown;
        Lay(end);
    }

    /// <summary>Where <paramref name="units"/> of air reach along the bar, in block units, the worn parts filled in order.</summary>
    public float XOf(double units)
    {
        foreach (var p in _laid)
            if (p.Present && units <= p.Before + p.Size + 1e-6)
                return p.X + (float)(Math.Clamp(units - p.Before, 0, p.Size) / p.Size) * p.Length;
        return End;
    }

    /// <summary>The stretch of part <paramref name="part"/> from <paramref name="from"/> to <paramref name="to"/> units of air, along it; none if it holds none of it.</summary>
    public (float A, float B)? Along(int part, double from, double to)
    {
        var p = _laid[part];
        if (!p.Present || p.Size <= 0) return null;
        double a = Math.Clamp(from - p.Before, 0, p.Size), b = Math.Clamp(to - p.Before, 0, p.Size);
        return b - a > 1e-4 && p.Length > 0.01f ? ((float)(a / p.Size) * p.Length, (float)(b / p.Size) * p.Length) : null;
    }

    /// <summary>smoothstep from 0 to 1.</summary>
    public static double Smooth(double s)
    {
        s = Math.Clamp(s, 0, 1);
        return s * s * (3 - 2 * s);
    }

    /// <summary>An event's light <paramref name="age"/> seconds on: up over <paramref name="rise"/>, held, down over <paramref name="fall"/>.</summary>
    public static double Env(double age, double rise, double hold, double fall) =>
        age < 0 ? 0 : Smooth(age / rise) * (1 - Smooth((age - rise - hold) / fall));

    private static void Age(ref double age, double seconds, double lasts)
    {
        if (age >= 0 && (age += seconds) > lasts) age = -1;
    }

    private void Hold(double air) => (_rampFrom, _rampTo, _rampAge) = (air, air, Look.BreathS);

    private void Ramp(double from, double to) => (_rampFrom, _rampTo, _rampAge) = (from, to, 0);

    /// <summary>Whether the worn parts differ from those read.</summary>
    private bool Refit()
    {
        int k = 0;
        foreach (var p in _pieces)
        {
            if (!p.Present) continue;
            if (k >= _wanted.Count || p.Key != _wanted[k].Key || p.Size != _wanted[k].Size) return true;
            k++;
        }
        return k != _wanted.Count;
    }

    /// <summary>The parts read kept, a new one put in after the one read before it, one gone kept shrinking.</summary>
    private void Fit()
    {
        foreach (var p in _pieces) p.Present = false;
        int after = -1;
        foreach (var (key, size) in _wanted)
        {
            int at = _pieces.FindIndex(p => p.Key == key);
            if (at < 0) _pieces.Insert(at = after + 1, new Piece(key, size, 0, 0));
            (_pieces[at].Size, _pieces[at].Present) = (size, true);
            after = at;
        }
    }

    /// <summary>What each part aims to show lit: the aim laid in the worn parts, the first first; one gone keeps its light.</summary>
    private void Aims()
    {
        double left = Math.Max(0, Aim);
        foreach (var p in _pieces)
        {
            if (!p.Present)
            {
                p.Aim = p.Units.X;
                continue;
            }
            p.Aim = Math.Min(left, p.Size);
            left -= p.Aim;
        }
    }

    /// <summary>
    /// The parts laid: <see cref="Vitals.AirPerUnit"/> a unit, a <see cref="Vitals.Gap"/> between, shrunk alike to end by
    /// <paramref name="end"/> (<see cref="Vitals.AirLength"/>); a part growing or shrinking takes its share of its length and gap.
    /// </summary>
    private void Lay(float end)
    {
        _laid.Clear();
        double units = 0, gaps = 0;
        for (int i = 0; i < _pieces.Count; i++)
        {
            double w = Math.Clamp(_pieces[i].Width.X, 0, 1);
            units += _pieces[i].Size * w;
            if (i > 0) gaps += w;
        }
        double k = units > 0 ? Math.Min(Vitals.AirPerUnit, Math.Max(0, end - Vitals.RowLeft - Vitals.Gap * gaps) / units) : Vitals.AirPerUnit;
        double x = Vitals.RowLeft, before = 0;
        EdgeX = Vitals.RowLeft;
        for (int i = 0; i < _pieces.Count; i++)
        {
            var p = _pieces[i];
            double w = Math.Clamp(p.Width.X, 0, 1);
            if (i > 0) x += Vitals.Gap * w;
            double length = p.Size * w * k, lit = p.Size > 0 ? p.Units.X / p.Size * length : 0;
            _laid.Add(new Laid((float)x, (float)length, (float)lit, p.Size, p.Present, before));
            if (lit > 0.01) EdgeX = (float)(x + lit);
            x += length;
            if (p.Present) before += p.Size;
        }
        End = (float)x;
    }
}
