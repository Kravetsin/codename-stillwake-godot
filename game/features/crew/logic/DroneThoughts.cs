using System;
using System.Collections.Generic;
using Stillwake.Game.Shared;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Features.Crew;

/// <summary>What the drone shows over itself (drone-thoughts): the alarms, then its moods; a find has no picture, only the hop.</summary>
public enum DroneThought { Meteor, Bolt, Cold, Heat, Curious, Find, Bored, Love, Pride }

/// <summary>A machine that teaches a form its player does not know, within <c>Lessons.Wonder</c>: its cell, which it is, and its top's middle in px.</summary>
public readonly record struct DroneWonder(int Cell, Vec At);

/// <summary>
/// How a mood moves the drone's body (the pick «Контур»'s MOTION): drawn this far off where it flies, px; its orbits tilted, radians; its body
/// swollen and its orbits hurried by these shares (negative slows); dimmed and glowing, 0..1. The default is still.
/// </summary>
public readonly record struct DroneBearing(Vec Off, double Tilt, double Swell, double Hurry, double Dim, double Glow);

/// <summary>
/// The light drone's thoughts and moods (drone-thoughts), its own drone only, client only: one picture over it at a time, a thought or mood
/// starting only when nothing higher shows (a window, a gesture, an icon or lesson at the drone, the dart, a thought before a mood), love, pride
/// and curious never resumed once cut; nothing while its hero is passed out, in its meeting or seated. The alarms show once its player knows them: the dart's
/// meteor or bolt while it darts, cold and heat when the body's level, a <see cref="Tune.Step"/> of degrees off normal each, rises above
/// the highest shown and held (one cut sooner shows again), which resets once the body is back within a step of normal. 
/// Love on waking from a death and pride after a deed lesson play once nothing higher shows, never lost; curious about a teaching machine (once
/// each) waits a plain gap; bored after standing still shows till the hero moves, back after any cut while it still stands, under every other
/// mood. Moods wait while the scanner reads. A find hops the body only.
/// Milliseconds.
/// </summary>
public sealed class DroneThoughts
{
    /// <summary>Its tunables: the client's alone, they never enter the sim.</summary>
    public static class Tune
    {
        /// <summary>Degrees of the body under or over normal a level: a thought at each, from the first (34° and 40° from 37°).</summary>
        public const double Step = 3;

        /// <summary>The least time between two curious moods, ms.</summary>
        public const double MoodGapMs = 120_000;

        /// <summary>Its anchor kept within <see cref="BoredBox"/> px this long, ms, its hero awake and no window open, it is bored till it leaves the box.</summary>
        public const double BoredAfterMs = 60_000, BoredBox = 8;
    }

    /// <summary>The pictures in <see cref="DroneThought"/>'s order; none for a find.</summary>
    private static readonly float[][]?[] Glyphs = [I.GlyphMeteor, I.GlyphBolt, I.GlyphCold, I.GlyphHeat, I.GlyphCurious, null, I.GlyphBored, I.GlyphLove, I.GlyphPride];

    private static readonly double InMs = I.ThoughtIn * 1000, HoldMs = I.ThoughtHold * 1000, OutMs = I.ThoughtOut * 1000, MoodInMs = I.MoodIn * 1000;

    private double _nowMs;
    private DroneThought? _showing;
    private double _startMs;
    private double? _outMs;
    private (DroneThought, int)? _dart;
    private int _coldMark, _heatMark, _shownLevel;
    private bool _love, _pride;
    private double _sinceCurious = Tune.MoodGapMs;
    private readonly HashSet<int> _wondered = [];
    private DroneWonder? _curious;
    private Vec? _box;
    private double _stillMs;
    /// <summary>The mood moving the body, since when, how far in (0..1, eased in and out), and which side of its hero it pressed to in love.</summary>
    private DroneThought? _mood;
    private double _moodMs, _moodIn;
    private double _loveSide = 1;

    /// <summary>The picture of <paramref name="thought"/>, box -1..1 y down; none for a find.</summary>
    public static float[][]? GlyphOf(DroneThought thought) => Glyphs[(int)thought];

    /// <summary>Whether <paramref name="thought"/> is an alarm: red, fast, never a mood.</summary>
    public static bool Alarm(DroneThought thought) => thought <= DroneThought.Heat;

    /// <summary>The thought or mood over it, forming, holding or falling apart; none.</summary>
    public DroneThought? Showing => _showing;

    /// <summary>The picture its sparks form now: the one showing, till it falls apart.</summary>
    public DroneThought? Forming => _showing is { } shown && _outMs == null && GlyphOf(shown) != null ? shown : null;

    /// <summary>How far the picture showing has formed, 0..1: in over <see cref="DroneInk.ThoughtIn"/>, out over <see cref="DroneInk.ThoughtOut"/>.</summary>
    public double Formed => _showing == null ? 0
        : _outMs is { } outMs ? 1 - Smooth((_nowMs - outMs) / OutMs)
        : Smooth((_nowMs - _startMs) / InMs);

    /// <summary>
    /// Moves it <paramref name="ms"/> on, sensing <paramref name="senses"/>; <paramref name="dart"/> is the danger its will darts at now, which and
    /// its cell, none while it does not.
    /// </summary>
    public void Step(in DroneSenses senses, (DroneThought Kind, int Cell)? dart, double ms)
    {
        _nowMs += ms;
        _sinceCurious += ms;
        if (!senses.Own)
        {
            (_showing, _mood, _moodIn) = (null, null, 0);
            return;
        }
        _love |= senses.Woke;
        _pride |= senses.Proud;
        int cold = Level(BodyHeat.Normal - senses.Body), heat = Level(senses.Body - BodyHeat.Normal);
        if (cold == 0) _coldMark = 0;
        if (heat == 0) _heatMark = 0;
        Still(senses, ms);
        bool bored = _stillMs >= Tune.BoredAfterMs;
        bool higher = senses.Window != null || senses.Signing || senses.Icon || senses.Out || senses.Quiet;
        // A dart ended lets its picture fall apart, boredom the hero moving; any other picture falls apart after it held.
        if (_showing is { } shown && _outMs == null)
        {
            bool darted = shown is DroneThought.Meteor or DroneThought.Bolt;
            if (darted ? dart == null || dart != _dart : shown == DroneThought.Bored ? !bored : _nowMs - _startMs >= InMs + HoldMs)
            {
                _outMs = _nowMs;
                // Cold and heat count as shown once their picture has held: one cut sooner shows again when free.
                if (shown == DroneThought.Cold) _coldMark = Math.Max(_coldMark, _shownLevel);
                if (shown == DroneThought.Heat) _heatMark = Math.Max(_heatMark, _shownLevel);
            }
        }
        if (_outMs is { } outAt && _nowMs - outAt >= OutMs) _showing = null;
        // Higher things cut it, the dart all but its own picture.
        if (higher || (dart != null && _showing is { } under && Rank(under) > 0)) _showing = null;
        if (!higher)
        {
            bool newDart = dart != null && dart != _dart;
            var due = newDart && senses.Alarms ? dart!.Value.Kind
                : dart != null ? (DroneThought?)null
                : senses.Alarms && cold > _coldMark ? DroneThought.Cold
                : senses.Alarms && heat > _heatMark ? DroneThought.Heat
                : senses.Reads ? null
                : _love ? DroneThought.Love
                : _pride ? DroneThought.Pride
                : _sinceCurious >= Tune.MoodGapMs && senses.Wonder is { } wonder && !_wondered.Contains(wonder.Cell) ? DroneThought.Curious
                : bored ? DroneThought.Bored
                : null;
            // A dart cuts whatever shows; a thought cuts a mood, a mood boredom; else it waits its turn.
            if (due is { } next && (_showing is not { } now || Rank(next) < Rank(now) || (newDart && Rank(next) == 0)))
                Start(next, senses, cold, heat);
        }
        _dart = dart;
        Move(senses, ms);
    }

    private void Start(DroneThought thought, in DroneSenses senses, int cold, int heat)
    {
        (_showing, _startMs, _outMs) = (thought, _nowMs, null);
        switch (thought)
        {
            case DroneThought.Cold: _shownLevel = cold; break;
            case DroneThought.Heat: _shownLevel = heat; break;
            case DroneThought.Love:
                _love = false;
                _loveSide = senses.At.X < senses.Anchor.X ? -1 : 1;
                break;
            case DroneThought.Pride: _pride = false; break;
            case DroneThought.Curious:
                _curious = senses.Wonder;
                _wondered.Add(senses.Wonder!.Value.Cell);
                _sinceCurious = 0;
                break;
        }
    }

    /// <summary>The anchor kept in its box counts toward boredom while its hero is awake and no window open; leaving the box starts it over.</summary>
    private void Still(in DroneSenses senses, double ms)
    {
        if (_box is not { } box || Math.Abs(senses.Anchor.X - box.X) > Tune.BoredBox || Math.Abs(senses.Anchor.Y - box.Y) > Tune.BoredBox)
            (_box, _stillMs) = (senses.Anchor, 0);
        else if (senses.Out || senses.Window != null) return;
        else _stillMs += ms;
    }

    /// <summary>The mood moving the body: the one showing, else a find while an item's icon is scanned; it eases in and out over <see cref="DroneInk.MoodIn"/>.</summary>
    private void Move(in DroneSenses senses, double ms)
    {
        var now = _showing is { } shown && !Alarm(shown) ? shown
            : senses.Finding && !senses.Out && !senses.Quiet && senses.Window == null && !senses.Signing ? DroneThought.Find
            : (DroneThought?)null;
        if (now != null && now != _mood) (_mood, _moodMs) = (now, 0);
        _moodMs += ms;
        _moodIn = Math.Clamp(_moodIn + (now != null ? ms : -ms) / MoodInMs, 0, 1);
        if (_moodIn == 0) _mood = null;
    }

    /// <summary>How the mood moves the body now, its hero's anchor at <paramref name="anchor"/>.</summary>
    public DroneBearing Bearing(Vec anchor)
    {
        double f = Smooth(_moodIn), u = _moodMs / 1000, w = I.WorldPx;
        return _mood switch
        {
            DroneThought.Curious when _curious is { } wonder => Curious(wonder.At.X < anchor.X ? -1 : 1),
            DroneThought.Find => new(new Vec(0, -Math.Abs(Math.Sin(u * I.FindHopPace)) * I.FindHop * f * w), I.FindRock * Math.Sin(u * I.FindHopPace) * f,
                I.FindSwell * f, I.FindHurry * f, 0, I.FindGlow * f),
            DroneThought.Bored => new(new Vec(Math.Sin(u * I.BoredSwayPace) * I.BoredSway * f * w, I.BoredSink * f * w), I.BoredRock * Math.Sin(u * I.BoredRockPace) * f,
                0, -I.BoredSlow * f, I.BoredDim * f, 0),
            DroneThought.Love => new(new Vec(0, Math.Sin(u * I.LoveBobPace) * I.LoveBob * f * w), I.LoveTilt * f * -_loveSide, 0, -I.LoveSlow * f, 0, I.LoveGlow * f),
            DroneThought.Pride => new(new Vec(0, -I.PrideRise * f * w), 0, I.PrideSwell * f, I.PrideHurry * f, 0, I.PrideGlow * f),
            _ => default,
        };

        DroneBearing Curious(int side) => new(new Vec(side * I.CuriousLean * f * w, -I.CuriousRise * f * w), -side * I.CuriousTilt * f, 0, 0, 0, 0);
    }

    /// <summary>Where it flies for its mood: pressed to the helmet in love, its anchor at <paramref name="anchor"/> (<see cref="DroneFlight.Tune.Above"/> over the neck); none.</summary>
    public Vec? Goal(Vec anchor) => _mood == DroneThought.Love && _showing == DroneThought.Love
        ? new Vec(anchor.X + _loveSide * I.LoveBehind, Helmet(anchor).Y - I.LoveOver)
        : null;

    /// <summary>
    /// Where its eye looks for what shows, from <paramref name="at"/>: up at an alarm's picture, at the machine it is curious about, at the helmet
    /// in love, about and down bored, up and back proud; none for a dart (its danger) or a find (the icon).
    /// </summary>
    public Vec? Look(Vec at, Vec anchor)
    {
        double u = _moodMs / 1000;
        return _showing switch
        {
            DroneThought.Cold or DroneThought.Heat => new Vec(0, -1),
            DroneThought.Curious when _curious is { } wonder => Unit(new Vec(wonder.At.X - at.X, wonder.At.Y - at.Y)),
            DroneThought.Love => Unit(new Vec(Helmet(anchor).X - at.X, Helmet(anchor).Y - at.Y)),
            DroneThought.Bored => Unit(new Vec(Math.Sin(u * I.BoredLookPace), I.BoredLookDown)),
            DroneThought.Pride => Unit(new Vec(I.PrideLookX, I.PrideLookY)),
            _ => null,
        };
    }

    /// <summary>How red an alarm's picture turns it now, 0..1: pulsing while it shows.</summary>
    public double Red => _showing is { } shown && Alarm(shown)
        ? Formed * (I.AlarmLow + I.AlarmSwing * Math.Sin((_nowMs - _startMs) / 1000 * Math.Tau * I.AlarmBeats))
        : 0;

    /// <summary>The helmet's middle, <see cref="DroneInk.HelmetUp"/> over the neck its anchor is over.</summary>
    private static Vec Helmet(Vec anchor) => new(anchor.X, anchor.Y + DroneFlight.Tune.Above - I.HelmetUp);

    /// <summary>The dart's picture first, then cold and heat, then the moods, boredom last.</summary>
    private static int Rank(DroneThought thought) => thought is DroneThought.Meteor or DroneThought.Bolt ? 0 : Alarm(thought) ? 1 : thought == DroneThought.Bored ? 3 : 2;

    /// <summary>The level of a body <paramref name="degrees"/> off normal: one at each whole <see cref="Tune.Step"/>.</summary>
    private static int Level(double degrees) => degrees <= 0 ? 0 : (int)Math.Floor(degrees / Tune.Step);

    private static Vec Unit(Vec v)
    {
        double length = Math.Sqrt(v.X * v.X + v.Y * v.Y);
        return length < 1e-9 ? new Vec(0, -1) : new Vec(v.X / length, v.Y / length);
    }

    private static double Smooth(double u)
    {
        u = Math.Clamp(u, 0, 1);
        return u * u * (3 - 2 * u);
    }
}
