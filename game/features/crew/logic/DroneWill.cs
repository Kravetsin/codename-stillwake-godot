using System;
using Stillwake.Game.Shared;
using T = Stillwake.Game.Features.Crew.DroneFlight.Tune;

namespace Stillwake.Game.Features.Crew;

/// <summary>A danger the sim reports: its cell, the point it is at in px, the surface it is on; a bolt's zone, columns each side of it.</summary>
public readonly record struct DroneDanger(int Cell, Vec At, int Surface, int Spread = 0);

/// <summary>
/// What a light drone senses this frame (docs/plans/drone-motion.md), pixels of the world: its hero's anchor and surface, where it
/// is, the window's place before the waist while one is open, the lens's aim in lamp or scan, the meteor
/// coming and the bolt glowing, the hunter's place, whether its hero is a friend low on air, passed out, or signing.
/// </summary>
public readonly record struct DroneSenses(
    Vec Anchor,
    Vec At,
    int Surface,
    Vec? Window = null,
    Vec? ModeLook = null,
    DroneDanger? Meteor = null,
    DroneDanger? Bolt = null,
    Vec? Hunter = null,
    bool Friend = false,
    bool Low = false,
    bool Out = false,
    bool Signing = false);

/// <summary>What the drone wants this frame: where to fly, where its lens looks (a unit vector), how red it is, 0..1, and whether it darts.</summary>
public readonly record struct DroneIntent(Vec Goal, Vec Look, double Tint, bool Darting);

/// <summary>
/// The light drone's mind (docs/plans/drone-motion.md): by rank, a window's place before the waist, a dart at a meteor or bolt near,
/// held still while its hero signs, else a perch of its own by the anchor, picked again when bored.
/// A hunter leans it his way. Its own <see cref="Random"/> picks the perches: they differ between screens, as decoration may.
/// </summary>
public sealed class DroneWill
{
    private static readonly Vec Up = new(0, -1);

    private readonly Random _random;
    private Vec _perch, _perchLook = Up;
    private double _boredLeft, _clockMs;
    private int? _meteorDarted, _boltDarted;
    private bool _dartBolt, _darting;
    private Vec? _signHeld;

    /// <summary>A will picking its perches by <paramref name="random"/>: a seed in tests, a new one per figure in the game.</summary>
    public DroneWill(Random random)
    {
        _random = random;
        _boredLeft = Bored();
    }

    /// <summary>Back to its anchor, as it is shown again: on it, bored of it after a while; the dangers it darted at stay darted.</summary>
    public void Reset() =>
        (_perch, _boredLeft, _darting, _signHeld) = (default, Bored(), false, null);

    /// <summary>What it wants after <paramref name="ms"/> more, sensing <paramref name="senses"/>.</summary>
    public DroneIntent Pick(in DroneSenses senses, double ms)
    {
        _clockMs += ms;
        var danger = Dart(senses);
        Settle(ms);
        var anchor = senses.Anchor;
        _signHeld = senses.Signing ? _signHeld ?? DroneFlight.Minus(senses.At, anchor) : null;
        Vec goal, look;
        if (senses.Window is { } window)
        {
            goal = window;
            look = danger is { } there ? Toward(senses.At, there, Up) : Up;
        }
        else
        {
            // Passed out, its hero cannot be left: it keeps to the anchor, looking its own way.
            (goal, look) = senses.Out ? (anchor, _perchLook)
                : danger is { } there ? (there, Toward(senses.At, there, _perchLook))
                : _signHeld is { } held ? (DroneFlight.Plus(anchor, held), _perchLook)
                : (DroneFlight.Plus(anchor, _perch), _perchLook);
            if (senses.Hunter is { } hunter) goal = DroneFlight.Plus(goal, DroneFlight.Scaled(Toward(anchor, hunter, default), T.Lean));
            if (senses.ModeLook is { } aim) look = aim;
        }
        return new DroneIntent(goal, look, Tint(danger != null, senses.Friend && senses.Low), danger != null && senses.Window == null && !senses.Out);
    }

    /// <summary>The point it darts at: the meteor's cell, or the bolt's column at the anchor's height; none while no dart lasts.</summary>
    private Vec? Dart(in DroneSenses senses)
    {
        var meteor = senses.Meteor is { } m && m.Surface == senses.Surface ? m : (DroneDanger?)null;
        var bolt = senses.Bolt is { } b && b.Surface == senses.Surface ? b : (DroneDanger?)null;
        // Darted at once each, while the sim reports it; gone, it may be another next time.
        if (meteor is not { } nowMeteor || nowMeteor.Cell != _meteorDarted) _meteorDarted = null;
        if (bolt is not { } nowBolt || nowBolt.Cell != _boltDarted) _boltDarted = null;
        if (_darting && (_dartBolt ? _boltDarted : _meteorDarted) == null) _darting = false;
        if (meteor is { } coming && _meteorDarted == null && DroneFlight.Length(DroneFlight.Minus(coming.At, senses.Anchor)) <= T.Notice)
            (_meteorDarted, _darting, _dartBolt) = (coming.Cell, true, false);
        if (bolt is { } glowing && _boltDarted == null && Math.Abs(Column(glowing.At.X) - Column(senses.Anchor.X)) <= glowing.Spread + T.BoltMargin)
            (_boltDarted, _darting, _dartBolt) = (glowing.Cell, true, true);
        if (!_darting) return null;
        return _dartBolt ? new Vec(bolt!.Value.At.X, senses.Anchor.Y) : meteor!.Value.At;
    }

    /// <summary>Bored of its perch, it picks another in its home box round the anchor, and a look of its own up and out.</summary>
    private void Settle(double ms)
    {
        _boredLeft -= ms;
        if (_boredLeft > 0) return;
        _perch = new Vec((_random.NextDouble() * 2 - 1) * T.HomeX, (_random.NextDouble() * 2 - 1) * T.HomeY);
        double angle = -_random.NextDouble() * Math.PI;
        _perchLook = new Vec(Math.Cos(angle), Math.Sin(angle));
        _boredLeft = Bored();
    }

    /// <summary>How red: a dart's danger flashing it, a friend's low air pulsing it once each <see cref="T.PulseMs"/>.</summary>
    private double Tint(bool danger, bool low)
    {
        double flash = danger ? (1 - Math.Cos(Math.Tau * _clockMs / T.FlashMs)) / 2 : 0;
        double phase = _clockMs % T.PulseMs / T.PulseMs;
        double pulse = low && phase < 0.4 ? Math.Sin(Math.PI * phase / 0.4) : 0;
        return Math.Max(flash, pulse);
    }

    private double Bored() => _random.Next(T.BoredMinMs, T.BoredMaxMs + 1);

    private static int Column(double x) => (int)Math.Floor(x / Rig.Cell);

    /// <summary>The way from <paramref name="from"/> to <paramref name="to"/>, a unit vector; <paramref name="near"/> when they are within a pixel.</summary>
    private static Vec Toward(Vec from, Vec to, Vec near)
    {
        var way = DroneFlight.Minus(to, from);
        double length = DroneFlight.Length(way);
        return length < 1 ? near : DroneFlight.Scaled(way, 1 / length);
    }
}
