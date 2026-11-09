using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What one frame of <see cref="AxeSwing.Advance"/> did: whether a swing began, whether its blade struck, when what it touches
/// is hit, and whether its whoosh is due (<see cref="Stroke.WhooshMsOf"/>).
/// </summary>
public readonly record struct SwingStep(bool Begun, bool Hits, bool Whooshes = false);

/// <summary>
/// A melee tool's strokes (0034, 0038, 0040, swing-motions, axe-combos): one clock for the arm and the hit. A stroke, once begun,
/// is aimed at <see cref="Aim"/> from the hero, wherever the hero is carried, and plays its weapon's motion for the sector it was
/// aimed in (<see cref="Play"/>) through its <see cref="MotionPlay.Ms"/>; it strikes at its <see cref="MotionPlay.HitShare"/>, let
/// go or not, and what its fan touches then is hit. Wanting a stroke while one plays changes nothing; still wanted as its pace
/// ends, the next begins at once, so held it strikes at its pace, and clicking is no faster. A weapon's combo chains instead:
/// held, each step begins the next at its own end; let go, a step that left the body away from rest plays the back to rest, and a
/// press during it once the pace allows begins the first step from where the arm is. Others' heroes replay the strokes they are
/// sent with the same clock, sending nothing.
/// </summary>
public struct AxeSwing
{
    /// <summary>How long a swing that bit something is drawn held at the strike: three frames of weight at 60 Hz (0075).</summary>
    public const double StopMs = 3 * 1000.0 / FrameHz;

    /// <summary>The frame rate the stop on contact is counted in frames of.</summary>
    private const double FrameHz = 60;

    /// <summary>
    /// The share of <paramref name="play"/>, if it bit something, to draw at <paramref name="phase"/> (0075): its own up to the
    /// strike, held there for <see cref="StopMs"/>, then the rest of it hurried to end with it, so the arm stops on contact while
    /// the play's clock, every hit it times and the body run on untouched. One that strikes nothing never stops.
    /// </summary>
    public static double Shown(double phase, MotionPlay play)
    {
        if (play.HitShare is not double hit) return phase;
        double stop = StopMs / play.Ms;
        if (phase <= hit) return phase;
        if (phase <= hit + stop) return hit;
        return hit + (phase - hit - stop) * (1 - hit) / (1 - hit - stop);
    }

    /// <summary>
    /// A frame longer than this counts as this long: a stall of the game never owes a burst of swings,
    /// and no frame passes two strikes.
    /// </summary>
    public const double MostPerFrameMs = 100;

    private double? _ms;
    private bool _struck, _whooshed;
    /// <summary>Real time run, in ms, and when each stroke last began by it: every tool keeps its own pace, none another's.</summary>
    private double _clock;
    private Dictionary<Stroke, double>? _begunAt;
    /// <summary>The stroke playing: the axe's, or the tool's it began with (<see cref="Strokes"/>).</summary>
    private Stroke? _stroke;

    /// <summary>The stroke playing or last played: the axe's before any.</summary>
    public readonly Stroke Stroke => _stroke ?? Strokes.Axe;
    /// <summary>Following another's hero, the last of its swings shown.</summary>
    private int? _followed;

    /// <summary>How many swings have begun: a new one is news to the others (<see cref="Busy.Swings"/>).</summary>
    public int Count { get; private set; }

    /// <summary>The combo's step the latest swing began, none for a single stroke: what the others play it as (<see cref="Busy.Step"/>).</summary>
    public int? Step { get; private set; }

    /// <summary>How far through its play the tool is, from 0 to 1; none between strokes, nor while one rests out its pace.</summary>
    public readonly double? Phase => _ms is double ms && Play is { } play && ms < play.Ms ? ms / play.Ms : null;

    /// <summary>
    /// The point of the world the stroke playing was aimed at as it began, in thousandths of a cell: the pointer, or the point
    /// another's hero sent with it. What the others are sent (<see cref="Busy.At"/>).
    /// </summary>
    public Point At { get; private set; }

    /// <summary>
    /// Where the stroke playing is aimed from the hero, in thousandths of a cell (swing-motions): <see cref="At"/> less where the
    /// hero was as it began, kept in the world's axes, so a hero carried away carries its stroke along.
    /// </summary>
    public Point Aim { get; private set; }

    /// <summary>The motion playing, chosen at the press by its aim's sector: a single stroke, a combo's step or the back to rest; none before any.</summary>
    public MotionPlay? Play { get; private set; }

    /// <summary>
    /// The stroke on by <paramref name="elapsedMs"/>, a new one begun at <paramref name="at"/> by the hero at
    /// <paramref name="place"/> when <paramref name="wanted"/> and none is playing: the left button held or clicked, or, for
    /// another's hero, a swing sent and not yet shown. <paramref name="angle"/> is the aim angle <paramref name="at"/> has from the
    /// shoulder of the figure faced to strike it (<see cref="Sectors"/>); none, the offset's own. A stroke just begun is drawn
    /// from its start. <paramref name="step"/> plays that step of a combo (another's hero, as sent); none, the chain's own.
    /// </summary>
    public SwingStep Advance(double elapsedMs, bool wanted, Point at = default, Point place = default, Stroke? stroke = null, double? angle = null,
        int? step = null)
    {
        bool begun = false, hits = false, whooshes = false;
        _clock += elapsedMs;
        double ms;
        // A stroke of another tool than the one playing ends it: its pace is its own, the next tool's waits for none of it.
        if (_ms != null && stroke != null && !ReferenceEquals(stroke, _stroke)) _ms = null;
        if (_ms is double playing)
        {
            ms = playing + Math.Min(elapsedMs, MostPerFrameMs);
            // Pressed during the back to rest once the pace allows: the first step, from where the arm and the body are.
            if (Play!.HitShare == null && wanted && Ready(Stroke))
                (ms, begun) = (0, Begin(at, place, stroke, angle, step ?? 0, Play.SpliceAt(Math.Min(ms, Play.Ms))));
        }
        else if (wanted && Ready(stroke ?? Strokes.Axe)) (ms, begun) = (0, Begin(at, place, stroke, angle, step ?? 0, null));
        else return default;
        var play = Play!;
        if (play.HitShare is double hit)
        {
            if (!_whooshed && ms >= Stroke.WhooshMsOf(play)) whooshes = _whooshed = true;
            if (!_struck && ms >= hit * play.Ms) hits = _struck = true;
        }
        // A combo's step and the back end with themselves; a single stroke rests out its pace.
        bool combo = play.Step != null || play.HitShare == null;
        double end = combo ? play.Ms : Stroke.PaceMs;
        if (ms >= end)
        {
            ms -= end;
            // Held, the next begins where this one ended, so the rhythm keeps the pace exactly; a combo spliced from its end.
            if (wanted)
                begun = Begin(at, place, stroke, angle, step ?? NextStep(play, angle ?? AngleOf(OffsetOf(at, place))), null, ms, play);
            else if (play.Step != null && !play.EndsAtRest) Play = MotionPlay.Back(play);
            else
            {
                _ms = null;
                return new(begun, hits, whooshes);
            }
        }
        _ms = ms;
        return new(begun, hits, whooshes);
    }

    /// <summary>
    /// The step a held chain plays after <paramref name="ended"/>, aimed <paramref name="aim"/> now: the next of its combo, round to
    /// the first, while the new aim's sector chains the same one; else the first, or that sector's single stroke.
    /// </summary>
    private readonly int NextStep(MotionPlay ended, double aim)
    {
        var combo = Stroke.ComboIn(Sectors.Of(aim));
        return ended.Step is int last && combo != null && ReferenceEquals(combo, Stroke.ComboIn(ended.Sector)) ? (last + 1) % combo.Count : 0;
    }

    /// <summary>
    /// Another's hero, as sent: <paramref name="sent"/> is the last chop heard from it (<see cref="Busy.Chopping"/>),
    /// the swings it has begun, the point and the combo's step of the latest, and <paramref name="place"/> where it was in the packet
    /// that brought that swing (<see cref="Busy.Hear"/>); none while it does not wield the axe. Each number not shown yet is swung
    /// once, aimed from the hero as it was sent, after the swing playing, if any, ends; numbers passed meanwhile are swung as one. No
    /// new number by the end of a step plays the back to rest, as a hero let go does.
    /// </summary>
    public SwingStep Follow(double elapsedMs, Busy? sent, Point place = default, Stroke? stroke = null, double? angle = null)
    {
        var step = Advance(elapsedMs, sent is { Swings: var now } && now != _followed, sent?.At ?? default, place, stroke, angle, sent?.Step ?? 0);
        if (step.Begun) _followed = sent!.Value.Swings;
        return step;
    }

    /// <summary>Ends a swing at once, unstruck: the axe put away, the hand taken, the hero out. The next begins its combo afresh.</summary>
    public void Stop() => _ms = null;

    /// <summary>Whether <paramref name="stroke"/>'s tool has rested out its pace since its own last stroke began, whatever was swung since.</summary>
    public readonly bool Ready(Stroke stroke) => _begunAt?.GetValueOrDefault(stroke, double.NegativeInfinity) is not double at || _clock - at >= stroke.PaceMs;

    /// <summary>
    /// Begins a stroke aimed at <paramref name="at"/>, <paramref name="agoMs"/> ago: its combo's <paramref name="step"/> where its
    /// sector has one. Spliced from <paramref name="from"/>, or from the end of <paramref name="ended"/> where either is a combo's
    /// step or the back, so the arm and the body run on unbroken; a single stroke after another as it always was.
    /// </summary>
    private bool Begin(Point at, Point place, Stroke? stroke, double? angle, int step, Splice? from, double agoMs = 0, MotionPlay? ended = null)
    {
        _stroke = stroke ?? Strokes.Axe;
        (_begunAt ??= new(ReferenceEqualityComparer.Instance))[_stroke] = _clock - agoMs;
        Count++;
        (At, Aim) = (at, OffsetOf(at, place));
        double aim = angle ?? AngleOf(Aim);
        var play = MotionPlay.Of(_stroke, aim, step, from);
        if (from == null && ended is { } was && (was.Step != null || play.Step != null)) play = MotionPlay.Of(_stroke, aim, step, was.SpliceAt(was.Ms));
        (Play, Step) = (play, play.Step);
        _struck = _whooshed = false;
        return true;
    }

    /// <summary><paramref name="at"/> from <paramref name="from"/>, in thousandths of a cell.</summary>
    public static Point OffsetOf(Point at, Point from) => new(at.X - from.X, at.Y - from.Y);

    /// <summary>Where <paramref name="hero"/> stands, its feet, in thousandths of a cell: what a stroke's aim is kept from.</summary>
    public static Point PlaceOf(Hero hero) => new(hero.X, hero.Y);

    /// <summary>The angle of <paramref name="aim"/> itself, radians from straight ahead, down positive; none, straight ahead.</summary>
    public static double AngleOf(Point aim) => aim == default ? 0 : Math.Atan2(aim.Y, aim.X);

    /// <summary>
    /// The point <paramref name="hero"/>, where it is now, strikes the stroke playing toward: <see cref="Aim"/> from it, or
    /// straight ahead of its shoulder the way it faces for none.
    /// </summary>
    public readonly Point Toward(Hero hero)
    {
        if (Aim != default) return new(hero.X + Aim.X, hero.Y + Aim.Y);
        var hand = Aiming.HandOf(hero);
        return new(hand.X + hero.Facing * Hero.Unit, hand.Y);
    }

    /// <summary>The way <paramref name="hero"/> faces to strike at <paramref name="aim"/> from it: toward its side, as it is when nearly straight above or below.</summary>
    public static int FaceOf(Point aim, Hero hero) =>
        Poses.FaceOf(((double)(hero.X + aim.X) / Hero.Unit, (double)(hero.Y + aim.Y) / Hero.Unit), hero) ?? hero.Facing;

    /// <summary>
    /// The front arm swinging, <paramref name="angle"/> radians from straight ahead of the shoulder, down
    /// positive: turned about the shoulder as one piece, the elbow bent <paramref name="elbow"/> as the item held wants,
    /// straight for the axe (0040, 2026-10-27-one-arm-holds).
    /// </summary>
    public static Bend ArmAt(double angle, double elbow = 0) => new(Math.PI / 2 - angle, elbow);

    /// <summary>
    /// Where the ends of <paramref name="look"/>'s edge are, in pixels from the shoulder, swung with the
    /// arm at <paramref name="angle"/> (<see cref="ArmAt"/>), held as the figure draws it (0040): the elbow bent
    /// <paramref name="elbow"/>, the look's own unless given, the wrist turned <paramref name="wrist"/> (<see cref="MotionKey.Wrist"/>).
    /// </summary>
    public static (Vec From, Vec To) EdgeAt(HeldLook look, double angle, double? elbow = null, double wrist = 0)
    {
        var arm = ArmAt(angle, elbow ?? look.Elbow);
        return (Rig.HeldAt(arm, look, look.EdgeFrom, wrist), Rig.HeldAt(arm, look, look.EdgeTo, wrist));
    }

    /// <summary>
    /// How far past the aim line the arm swings while the middle of <paramref name="look"/>'s edge is on it (0040): a stroke turns
    /// the arm by this beside its motion, so the blade, held at an angle to the arm, meets what it is aimed at as it strikes.
    /// </summary>
    public static double Lead(HeldLook look)
    {
        var (from, to) = EdgeAt(look, 0);
        return -Math.Atan2((from.Y + to.Y) / 2, (from.X + to.X) / 2);
    }
}
