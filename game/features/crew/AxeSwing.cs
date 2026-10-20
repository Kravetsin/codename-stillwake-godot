using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What one frame of <see cref="AxeSwing.Advance"/> did: whether a swing began, whether its blade struck, when what it touches
/// is hit, and whether its whoosh is due (<see cref="AxeSwing.WhooshMs"/>).
/// </summary>
public readonly record struct SwingStep(bool Begun, bool Hits, bool Whooshes = false);

/// <summary>
/// The fire axe's swings (0034, 0038, 0039, 0040): one clock for the blade and the hit. A swing, once begun, is
/// aimed at <see cref="At"/> and plays through <see cref="Ms"/>: raised back over the shoulder for
/// <see cref="WindShare"/> of it, brought down through the line to its point in <see cref="StrikeShare"/>,
/// eased back (<see cref="Lift"/>), the arm straight throughout (<see cref="ArmAt"/>); it strikes the frame the blade crosses that line (<see cref="HitAt"/>), let go or not, and
/// what the downstroke touches then is hit. Wanting a swing while one plays changes nothing; still
/// wanted as it ends, the next begins at once, so held it swings every <see cref="Ms"/>, and clicking is
/// no faster. Others' heroes replay the swings they are sent with the same clock, sending nothing.
/// </summary>
public struct AxeSwing
{
    /// <summary>A swing's length: <c>Weapons.Axe.Pace</c> of the world's ticks, as real time.</summary>
    public static readonly double Ms = Content.Weapons.Axe.Pace * 1000.0 / Sim.World.TicksPerSecond;

    /// <summary>
    /// The swing's shape, in shares of it and radians from the line to its point: raised
    /// <see cref="Raised"/> back, then down to <see cref="Through"/> past it.
    /// </summary>
    public const double WindShare = 0.35, StrikeShare = 0.12, Raised = 2.1, Through = 0.3;

    /// <summary>The share of a swing at which the blade crosses the line to its point: when it strikes.</summary>
    public const double HitAt = WindShare + StrikeShare * Raised / (Raised + Through);

    /// <summary>How long a swing that bit something is drawn held at the strike: three 60 Hz frames of weight (0075).</summary>
    public const double StopMs = 3 * 1000.0 / 60;

    /// <summary>
    /// The share of a swing that bit something to draw at <paramref name="phase"/> (0075): its own up to the strike,
    /// held there for <see cref="StopMs"/>, then the rest of it hurried to end with the swing, so the arm stops on
    /// contact while the swing's clock, and every hit it times, runs on untouched.
    /// </summary>
    public static double Shown(double phase)
    {
        double stop = StopMs / Ms;
        if (phase <= HitAt) return phase;
        if (phase <= HitAt + stop) return HitAt;
        return HitAt + (phase - HitAt - stop) * (1 - HitAt) / (1 - HitAt - stop);
    }

    /// <summary>
    /// A frame longer than this counts as this long: a stall of the game never owes a burst of swings,
    /// and no frame passes two strikes.
    /// </summary>
    public const double MostPerFrameMs = 100;

    /// <summary>When in a swing its whoosh begins: its loudest moment (<c>Sound.SwingPeakS</c> in) in the middle of the downstroke.</summary>
    public static readonly double WhooshMs = (WindShare + StrikeShare / 2) * Ms - Stillwake.Game.Sound.SwingPeakS * 1000;

    private double? _ms;
    private bool _struck, _whooshed;
    /// <summary>Following another's hero, the last of its swings shown.</summary>
    private int? _followed;

    /// <summary>How many swings have begun: a new one is news to the others (<see cref="Busy.Swings"/>).</summary>
    public int Count { get; private set; }

    /// <summary>How far through its swing the axe is, from 0 to 1; none between swings.</summary>
    public readonly double? Phase => _ms / Ms;

    /// <summary>
    /// Where the swing playing is aimed, in thousandths of a cell: the pointer as it began, or the point
    /// another's hero sent with it, fixed for the swing (0039).
    /// </summary>
    public Point At { get; private set; }

    /// <summary>
    /// The swing on by <paramref name="elapsedMs"/>, a new one begun at <paramref name="at"/> when
    /// <paramref name="wanted"/> and none is playing: the left button held or clicked, or, for another's
    /// hero, a swing sent and not yet shown. A swing just begun is drawn from its start.
    /// </summary>
    public SwingStep Advance(double elapsedMs, bool wanted, Point at = default)
    {
        bool begun = false, hits = false, whooshes = false;
        double ms;
        if (_ms is double playing) ms = playing + Math.Min(elapsedMs, MostPerFrameMs);
        else if (wanted) (ms, begun) = (0, Begin(at));
        else return default;
        if (!_whooshed && ms >= WhooshMs) whooshes = _whooshed = true;
        if (!_struck && ms >= HitAt * Ms) hits = _struck = true;
        if (ms >= Ms)
        {
            if (!wanted)
            {
                _ms = null;
                return new(begun, hits, whooshes);
            }
            // Held, the next begins where this one ended, so the rhythm keeps the axe's pace exactly.
            ms -= Ms;
            begun = Begin(at);
        }
        _ms = ms;
        return new(begun, hits, whooshes);
    }

    /// <summary>
    /// Another's hero, as sent: <paramref name="sent"/> is the last chop heard from it (<see cref="Busy.Chopping"/>),
    /// the swings it has begun and the point of the latest; none while it does not wield the axe. Each
    /// number not shown yet is swung once, at the point sent with it, after the swing playing, if any,
    /// ends; numbers passed meanwhile are swung as one.
    /// </summary>
    public SwingStep Follow(double elapsedMs, Busy? sent)
    {
        var step = Advance(elapsedMs, sent is { Swings: var now } && now != _followed, sent?.At ?? default);
        if (step.Begun) _followed = sent!.Value.Swings;
        return step;
    }

    /// <summary>Ends a swing at once, unstruck: the axe put away, the hand taken, the hero out.</summary>
    public void Stop() => _ms = null;

    private bool Begin(Point at)
    {
        Count++;
        At = at;
        _struck = _whooshed = false;
        return true;
    }

    /// <summary>
    /// The front arm swinging, <paramref name="angle"/> radians from straight ahead of the shoulder, down
    /// positive: straight, turned about the shoulder as one piece, the elbow never bent (0040).
    /// </summary>
    public static Bend ArmAt(double angle) => new(Math.PI / 2 - angle, 0);

    /// <summary>
    /// Where the ends of <paramref name="look"/>'s edge are, in pixels from the shoulder, swung with the
    /// arm at <paramref name="angle"/> (<see cref="ArmAt"/>), held as the figure draws it (0040), turned
    /// <paramref name="turned"/> more in the fist.
    /// </summary>
    public static (Vec From, Vec To) EdgeAt(HeldLook look, double angle, double turned = 0)
    {
        var arm = ArmAt(angle);
        return (Rig.HeldAt(arm, look, look.EdgeFrom, turned), Rig.HeldAt(arm, look, look.EdgeTo, turned));
    }

    /// <summary>
    /// How far past the line to its point the arm swings while the middle of <paramref name="look"/>'s
    /// edge is on it (0040): a swing turns the arm by this beside <see cref="Lift"/>, so the blade, held
    /// at an angle to the arm, meets what the swing is aimed at as it strikes.
    /// </summary>
    public static double Lead(HeldLook look)
    {
        var (from, to) = EdgeAt(look, 0);
        return -Math.Atan2((from.Y + to.Y) / 2, (from.X + to.X) / 2);
    }

    /// <summary>
    /// The axe's turn from the line to its point at <paramref name="phase"/> of a swing, back (negative)
    /// or through (positive): eased up, brought down hard, eased back to the line, still at either end, so
    /// a swing leaves the arm at rest and the next held begins from it unbroken.
    /// </summary>
    public static double Lift(double phase)
    {
        if (phase < WindShare) return -Raised * (1 - Math.Cos(phase / WindShare * Math.PI)) / 2;
        if (phase < WindShare + StrikeShare) return -Raised + (Raised + Through) * ((phase - WindShare) / StrikeShare);
        return Through * (1 + Math.Cos((phase - WindShare - StrikeShare) / (1 - WindShare - StrikeShare) * Math.PI)) / 2;
    }
}
