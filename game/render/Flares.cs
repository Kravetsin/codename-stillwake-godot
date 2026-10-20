using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// A cross flare on a key moment (0075): a long thin spike across, a shorter one up and down, faint diagonals
/// and a soft bloom, <see cref="Radius"/> pixels of the world, whole on its first 60 Hz frame and gone after
/// <see cref="Fx.GlintHold"/> of them, drawn at least once whatever the frame rate.
/// </summary>
public sealed class Glint(double x, double y, double radius, (int R, int G, int B) colour, double turn)
{
    private static readonly double[] Steps = [1, 0.7, 0.45, 0.3];

    public double X { get; } = x;
    public double Y { get; } = y;
    public double Radius { get; } = radius;
    public (int R, int G, int B) Colour { get; } = colour;
    public double Turn { get; } = turn;
    public double AgeMs;
    /// <summary>Whether a frame has drawn it: a glint born and aged past its hold before the next draw still shows once.</summary>
    public bool Drawn;

    /// <summary>How bright it is now, 0 once it has held its frames and been drawn.</summary>
    public double Strength
    {
        get
        {
            int frame = Drawn ? (int)(AgeMs / Glim.SampleMs) : 0;
            return frame < Math.Min(Fx.GlintHold, Steps.Length) ? Steps[frame] : 0;
        }
    }
}

/// <summary>A faint ring of light spreading from a key moment (0075): <see cref="From"/> to <see cref="To"/> pixels across its life, fading as it goes.</summary>
public sealed class Shock(double x, double y, double from, double to, double ms, (int R, int G, int B) colour, double alpha, double width)
{
    public double X { get; } = x;
    public double Y { get; } = y;
    public (int R, int G, int B) Colour { get; } = colour;
    /// <summary>The ring's width, in pixels of the world, before it is held to a screen pixel at least.</summary>
    public double Width { get; } = width;
    public double AgeMs;
    public bool Gone => AgeMs >= ms;

    /// <summary>How far out it is now, eased out: fast at first.</summary>
    public double Radius
    {
        get
        {
            double q = Math.Clamp(AgeMs / ms, 0, 1);
            return from + (to - from) * (1 - (1 - q) * (1 - q));
        }
    }

    public double Alpha => alpha * Math.Max(0, 1 - AgeMs / ms);
}

/// <summary>
/// A light that flares and dies away smoothly (0075): a strike's flash on a wreck, a spark burst's flicker, a
/// molten spot held while a screwdriver works it. Losing <see cref="Decay"/> of itself each 60 Hz frame.
/// </summary>
public sealed class Flash((int R, int G, int B) colour, int reach, float keep, double decay)
{
    public double X, Y, Strength;
    public (int R, int G, int B) Colour { get; } = colour;
    /// <summary>How far it lights, in cells, keeping <see cref="Keep"/> a cell.</summary>
    public int Reach { get; } = reach;
    public float Keep { get; } = keep;
    public double Decay { get; } = decay;
}

/// <summary>
/// The glints, shock lines and flashes of the world's key moments (0075): the features stamp them, the effects
/// layer ages and draws glints and shocks, the light feature lights flashes. Drawn only, each peer its own.
/// </summary>
public sealed class Flares
{
    /// <summary>A flash this faint has died away.</summary>
    private const double Faint = 0.02;

    public static Flares Shared { get; } = new();

    private readonly List<Glint> _glints = [];
    private readonly List<Shock> _shocks = [];
    private readonly List<Flash> _flashes = [];
    private readonly Dictionary<object, Flash> _held = [];

    public IReadOnlyList<Glint> Glints => _glints;
    public IReadOnlyList<Shock> Shocks => _shocks;
    public IReadOnlyList<Flash> Flashes => _flashes;

    public void Glint(double x, double y, double radius, (int R, int G, int B) colour, double turn = 0) =>
        _glints.Add(new Glint(x, y, radius, colour, turn));

    public void Shock(double x, double y, double from, double to, double ms, (int R, int G, int B) colour, double alpha, double width = 0.3) =>
        _shocks.Add(new Shock(x, y, from, to, ms, colour, alpha, width));

    /// <summary>A light flaring to <paramref name="strength"/> at once and dying away by <paramref name="decay"/> a 60 Hz frame.</summary>
    public void Flash(double x, double y, (int R, int G, int B) colour, double strength, int reach, float keep, double decay) =>
        _flashes.Add(new Flash(colour, reach, keep, decay) { X = x, Y = y, Strength = strength });

    /// <summary>
    /// The light kept under <paramref name="key"/> set to <paramref name="strength"/> at (<paramref name="x"/>, <paramref name="y"/>):
    /// held while it is set each frame, dying away by its decay once it is not.
    /// </summary>
    public void Hold(object key, double x, double y, (int R, int G, int B) colour, double strength, int reach, float keep, double decay)
    {
        if (!_held.TryGetValue(key, out var flash) || !_flashes.Contains(flash))
        {
            _held[key] = flash = new Flash(colour, reach, keep, decay);
            _flashes.Add(flash);
        }
        (flash.X, flash.Y, flash.Strength) = (x, y, strength);
    }

    /// <summary>Everything aged by <paramref name="elapsedMs"/>; what has held, spread or died away let go.</summary>
    public void Step(double elapsedMs)
    {
        foreach (var g in _glints) g.AgeMs += elapsedMs;
        _glints.RemoveAll(g => g.Drawn && g.Strength <= 0);
        foreach (var s in _shocks) s.AgeMs += elapsedMs;
        _shocks.RemoveAll(s => s.Gone);
        double frames = elapsedMs / Glim.SampleMs;
        foreach (var f in _flashes) f.Strength *= Math.Pow(f.Decay, frames);
        _flashes.RemoveAll(f => f.Strength < Faint);
        if (_held.Count > 0)
        {
            List<object>? gone = null;
            foreach (var (key, flash) in _held)
                if (flash.Strength < Faint) (gone ??= []).Add(key);
            if (gone != null) foreach (var key in gone) _held.Remove(key);
        }
    }

    public void Clear()
    {
        _glints.Clear();
        _shocks.Clear();
        _flashes.Clear();
        _held.Clear();
    }
}
