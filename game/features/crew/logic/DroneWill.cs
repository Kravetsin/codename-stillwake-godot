using System;
using Stillwake.Game.Shared;
using T = Stillwake.Game.Features.Crew.DroneFlight.Tune;

namespace Stillwake.Game.Features.Crew;

/// <summary>A danger the sim reports: its cell, the point it is at in px, the surface it is on; a bolt's zone, columns each side of it.</summary>
public readonly record struct DroneDanger(int Cell, Vec At, int Surface, int Spread = 0);

/// <summary>
/// What a light drone senses this frame (drone-motion ADR), pixels of the world: its hero's anchor and surface, where it
/// is, the window's place before the waist while one is open, the lens's aim in lamp or scan, the meteor
/// coming and the bolt glowing, the hunter's place, whether its hero is low on air, passed out, or signing; the machine it visits to learn a form from
/// (drone-lessons). Its own drone's cues for its thoughts and moods (drone-thoughts, <see cref="DroneThoughts"/>): whether it is this player's, whether
/// its player knows the alarms (creative does), its hero's body temperature in degrees, whether a magnetic flare's warning is on, the machine it is curious about, whether an
/// icon or a lesson plays at the drone and an item's icon is scanned, whether a deed lesson just ended or the hero just woke from a death, whether the
/// hero is in its meeting or seated, and whether the scanner reads. Its hero's neck and what stops
/// light on its surface, to perch in the open air.
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
    bool Low = false,
    bool Out = false,
    bool Signing = false,
    Vec? Visit = null,
    bool Own = false,
    bool Alarms = false,
    double Body = BodyHeat.Normal,
    bool Flare = false,
    DroneWonder? Wonder = null,
    bool Icon = false,
    bool Finding = false,
    bool Proud = false,
    bool Woke = false,
    bool Quiet = false,
    bool Reads = false,
    Vec Neck = default,
    Func<double, double, bool>? StopsLight = null);

/// <summary>
/// What the drone wants this frame: where to fly, where its lens looks (a unit vector), how red it is, 0..1, and whether it darts; the thought or
/// mood whose picture its sparks form now (drone-thoughts), none while none forms or holds; how a mood moves its body; whether
/// nothing matters more than play, so it may tow its hero (<see cref="DroneTow"/>).
/// </summary>
public readonly record struct DroneIntent(Vec Goal, Vec Look, double Tint, bool Darting, DroneThought? Thought = null, DroneBearing Bearing = default, bool Free = false);

/// <summary>
/// The light drone's mind (drone-motion ADR): by rank, a window's place before the waist, a dart at a meteor or bolt near, a machine it
/// visits to learn from (drone-lessons), held still while its hero signs, pressed to the helmet in love, else a perch of its own aside of the anchor,
/// picked again when bored. A hunter leans it his way. Its <see cref="Thoughts"/> show over it and move its body (drone-thoughts). Its own <see cref="Random"/> picks the perches: they differ between screens, as decoration may.
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
        _boredLeft = T.AsideMs;
    }

    /// <summary>Its thoughts and moods (drone-thoughts): what shows over it.</summary>
    public DroneThoughts Thoughts { get; } = new();

    /// <summary>Back to its anchor, as it is shown again: on it, soon off to a perch aside; the dangers it darted at stay darted.</summary>
    public void Reset() =>
        (_perch, _boredLeft, _darting, _signHeld) = (default, T.AsideMs, false, null);

    /// <summary>What it wants after <paramref name="ms"/> more, sensing <paramref name="senses"/>.</summary>
    public DroneIntent Pick(in DroneSenses senses, double ms)
    {
        _clockMs += ms;
        var danger = Dart(senses);
        bool darting = danger != null && senses.Window == null && !senses.Out;
        Thoughts.Step(senses, darting ? (_dartBolt ? DroneThought.Bolt : DroneThought.Meteor, (_dartBolt ? _boltDarted : _meteorDarted)!.Value) : null, ms);
        Settle(ms);
        var anchor = senses.Anchor;
        _signHeld = senses.Signing ? _signHeld ?? DroneFlight.Minus(senses.At, anchor) : null;
        Vec goal, look;
        var pressed = Thoughts.Goal(anchor);
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
                : senses.Visit is { } visit ? (visit, Toward(senses.At, visit, _perchLook))
                : _signHeld is { } held ? (DroneFlight.Plus(anchor, held), _perchLook)
                : pressed is { } helmet ? (helmet, _perchLook)
                : (DroneFlight.Plus(anchor, _perch), _perchLook);
            if (senses.Hunter is { } hunter) goal = DroneFlight.Plus(goal, DroneFlight.Scaled(Toward(anchor, hunter, default), T.Lean));
            // A dart, a visit and love go where they go; all else by its hero keeps out of walls and helmet.
            if (senses.StopsLight is { } stops && (senses.Out || danger == null && senses.Visit == null && pressed == null))
                goal = OpenAir.Nearest(goal, senses.Neck, stops, (new Vec(senses.Neck.X, senses.Neck.Y - DroneInk.HelmetUp), T.Clear));
            if (senses.ModeLook is { } aim) look = aim;
            // What shows over it outranks the mode's aim: warnings, then moods.
            if (!senses.Out && Thoughts.Look(senses.At, anchor) is { } eye) look = eye;
        }
        bool free = senses.Window == null && senses.Visit == null && danger == null && pressed == null
            && !senses.Out && !senses.Signing && !senses.Icon && !senses.Quiet && !(Thoughts.Forming is { } shown && DroneThoughts.Alarm(shown));
        return new DroneIntent(goal, look, Math.Max(Tint(danger != null, senses.Low), Thoughts.Red), darting, Thoughts.Forming, Thoughts.Bearing(anchor), free);
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

    /// <summary>Bored of its perch, it picks another either side of the anchor, never over the head, and a look of its own up and out.</summary>
    private void Settle(double ms)
    {
        _boredLeft -= ms;
        if (_boredLeft > 0) return;
        double across = _random.NextDouble() * 2 - 1;
        double aside = (across < 0 ? -T.HomeNear : T.HomeNear) + across * (T.HomeX - T.HomeNear);
        _perch = new Vec(aside, (_random.NextDouble() * 2 - 1) * T.HomeY);
        double angle = -_random.NextDouble() * Math.PI;
        _perchLook = new Vec(Math.Cos(angle), Math.Sin(angle));
        _boredLeft = Bored();
    }

    /// <summary>How red: a dart's danger flashing it, its hero's low air pulsing it once each <see cref="T.PulseMs"/>.</summary>
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
