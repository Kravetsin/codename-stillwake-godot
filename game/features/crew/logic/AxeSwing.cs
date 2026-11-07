using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What one frame of <see cref="AxeSwing.Advance"/> did: whether a swing began, whether its blade struck, when what it touches
/// is hit, and whether its whoosh is due (<see cref="Stroke.WhooshMsOf"/>).
/// </summary>
public readonly record struct SwingStep(bool Begun, bool Hits, bool Whooshes = false);

/// <summary>
/// A melee tool's strokes (0034, 0038, 0040, swing-motions): one clock for the arm and the hit. A stroke, once begun, is aimed
/// at <see cref="Aim"/> from the hero, wherever the hero is carried, and plays its weapon's motion for the sector it was aimed in
/// (<see cref="Play"/>) through its <see cref="Stroke.Ms"/>; it strikes at its <see cref="Stroke.HitShare"/>, let go or not, and
/// what its fan touches then is hit. Wanting a stroke while one plays changes nothing; still wanted as its pace ends, the next
/// begins at once, so held it strikes at its pace, and clicking is no faster. Others' heroes replay the strokes they are sent
/// with the same clock, sending nothing.
/// </summary>
public struct AxeSwing
{
    /// <summary>A swing's length: <c>Weapons.Axe.Pace</c> of the world's ticks, as real time.</summary>
    public static readonly double Ms = Content.Weapons.Axe.Pace * 1000.0 / Sim.World.TicksPerSecond;

    /// <summary>How long a swing that bit something is drawn held at the strike: three 60 Hz frames of weight (0075).</summary>
    public const double StopMs = 3 * 1000.0 / 60;

    /// <summary>
    /// The share of a stroke that bit something to draw at <paramref name="phase"/> (0075): its own up to the strike,
    /// held there for <see cref="StopMs"/>, then the rest of it hurried to end with the stroke, so the arm stops on
    /// contact while the stroke's clock, and every hit it times, runs on untouched. Of <paramref name="stroke"/>, the axe's by default.
    /// </summary>
    public static double Shown(double phase, Stroke? stroke = null)
    {
        var of = stroke ?? Strokes.Axe;
        double hit = of.HitShare, stop = StopMs / of.Ms;
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

    /// <summary>How far through its stroke the tool is, from 0 to 1; none between strokes, nor while one rests out its pace.</summary>
    public readonly double? Phase => _ms is double ms && ms < Stroke.Ms ? ms / Stroke.Ms : null;

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

    /// <summary>The motion the stroke playing plays, chosen at the press by its aim's sector: none before any.</summary>
    public MotionPlay? Play { get; private set; }

    /// <summary>
    /// The stroke on by <paramref name="elapsedMs"/>, a new one begun at <paramref name="at"/> by the hero at
    /// <paramref name="place"/> when <paramref name="wanted"/> and none is playing: the left button held or clicked, or, for
    /// another's hero, a swing sent and not yet shown. <paramref name="angle"/> is the aim angle <paramref name="at"/> has from the
    /// shoulder of the figure faced to strike it (<see cref="Sectors"/>); none, the offset's own. A stroke just begun is drawn
    /// from its start.
    /// </summary>
    public SwingStep Advance(double elapsedMs, bool wanted, Point at = default, Point place = default, Stroke? stroke = null, double? angle = null)
    {
        bool begun = false, hits = false, whooshes = false;
        _clock += elapsedMs;
        double ms;
        // A stroke of another tool than the one playing ends it: its pace is its own, the next tool's waits for none of it.
        if (_ms != null && stroke != null && stroke != _stroke) _ms = null;
        if (_ms is double playing) ms = playing + Math.Min(elapsedMs, MostPerFrameMs);
        else if (wanted && Ready(stroke ?? Strokes.Axe)) (ms, begun) = (0, Begin(at, place, stroke, angle));
        else return default;
        var now = Stroke;
        if (!_whooshed && ms >= now.WhooshMsOf(Play!.Motion)) whooshes = _whooshed = true;
        if (!_struck && ms >= now.HitShare * now.Ms) hits = _struck = true;
        if (ms >= now.PaceMs)
        {
            if (!wanted)
            {
                _ms = null;
                return new(begun, hits, whooshes);
            }
            // Held, the next begins where this one's pace ended, so the rhythm keeps the tool's pace exactly.
            ms -= now.PaceMs;
            begun = Begin(at, place, stroke, angle, ms);
        }
        _ms = ms;
        return new(begun, hits, whooshes);
    }

    /// <summary>
    /// Another's hero, as sent: <paramref name="sent"/> is the last chop heard from it (<see cref="Busy.Chopping"/>),
    /// the swings it has begun and the point of the latest, and <paramref name="place"/> where it was in the packet that brought
    /// that swing (<see cref="Busy.Hear"/>); none while it does not wield the axe. Each number not shown yet is swung once, aimed
    /// from the hero as it was sent, after the swing playing, if any, ends; numbers passed meanwhile are swung as one.
    /// </summary>
    public SwingStep Follow(double elapsedMs, Busy? sent, Point place = default, Stroke? stroke = null, double? angle = null)
    {
        var step = Advance(elapsedMs, sent is { Swings: var now } && now != _followed, sent?.At ?? default, place, stroke, angle);
        if (step.Begun) _followed = sent!.Value.Swings;
        return step;
    }

    /// <summary>Ends a swing at once, unstruck: the axe put away, the hand taken, the hero out.</summary>
    public void Stop() => _ms = null;

    /// <summary>Whether <paramref name="stroke"/>'s tool has rested out its pace since its own last stroke began, whatever was swung since.</summary>
    public readonly bool Ready(Stroke stroke) => _begunAt?.GetValueOrDefault(stroke, double.NegativeInfinity) is not double at || _clock - at >= stroke.PaceMs;

    private bool Begin(Point at, Point place, Stroke? stroke, double? angle, double agoMs = 0)
    {
        _stroke = stroke ?? Strokes.Axe;
        (_begunAt ??= [])[_stroke] = _clock - agoMs;
        Count++;
        (At, Aim) = (at, new Point(at.X - place.X, at.Y - place.Y));
        Play = MotionPlay.Of(_stroke, angle ?? AngleOf(Aim));
        _struck = _whooshed = false;
        return true;
    }

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
    /// arm at <paramref name="angle"/> (<see cref="ArmAt"/>), held as the figure draws it (0040).
    /// </summary>
    public static (Vec From, Vec To) EdgeAt(HeldLook look, double angle)
    {
        var arm = ArmAt(angle, look.Elbow);
        return (Rig.HeldAt(arm, look, look.EdgeFrom), Rig.HeldAt(arm, look, look.EdgeTo));
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

    /// <summary>
    /// The farthest back over the head a swing raises the straight arm, in radians from hanging (<see cref="Bend.Upper"/>): a
    /// little past straight up, as far as a shoulder goes. A swing aimed high is raised less, so it never twists behind the back.
    /// </summary>
    public const double Overhead = 3.6;

    /// <summary>The farthest behind hanging a swing brings the straight arm through, in radians: a swing aimed low goes on less.</summary>
    public const double Behind = 1.0;
}
