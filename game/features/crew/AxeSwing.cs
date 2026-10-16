using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>What one frame of <see cref="AxeSwing.Advance"/> did: whether a swing began, and whether its blade met the wreck, the hit to send.</summary>
public readonly record struct SwingStep(bool Begun, bool Hits);

/// <summary>
/// The fire axe's swings (0034): one clock for the blade and the hit. A swing, once begun, plays through
/// <see cref="Ms"/>: raised back over the shoulder for <see cref="WindShare"/> of it, brought down through
/// the wreck in <see cref="StrikeShare"/>, eased back; its one hit goes the frame the blade meets the
/// wreck (<see cref="HitAt"/>), let go or not. Wanting a swing while one plays changes nothing; still
/// wanted as it ends, the next begins at once, so held it swings every <see cref="Ms"/>, and clicking is
/// no faster. Others' heroes replay the swings they are sent with the same clock, sending nothing.
/// </summary>
public struct AxeSwing
{
    /// <summary>A swing's length: <c>Axe.SwingTicks</c> of the world's ticks, as real time.</summary>
    public static readonly double Ms = Content.Axe.SwingTicks * 1000.0 / Sim.World.TicksPerSecond;

    /// <summary>
    /// The swing's shape, in shares of it and radians from the line to the wreck: raised
    /// <see cref="Raised"/> back, then down to <see cref="Through"/> past it.
    /// </summary>
    public const double WindShare = 0.35, StrikeShare = 0.12, Raised = 2.1, Through = 0.3;

    /// <summary>The share of a swing at which the blade crosses the line to the wreck: when its hit goes.</summary>
    public const double HitAt = WindShare + StrikeShare * Raised / (Raised + Through);

    /// <summary>
    /// A frame longer than this counts as this long: a stall of the game never owes a burst of swings,
    /// and no frame passes two strikes.
    /// </summary>
    public const double MostPerFrameMs = 100;

    private double? _ms;
    private bool _struck;
    /// <summary>Following another's hero, the last of its swings shown.</summary>
    private int? _followed;

    /// <summary>How many swings have begun: a new one is news to the others (<see cref="Busy.Swings"/>).</summary>
    public int Count { get; private set; }

    /// <summary>How far through its swing the axe is, from 0 to 1; none between swings.</summary>
    public readonly double? Phase => _ms / Ms;

    /// <summary>
    /// The swing on by <paramref name="elapsedMs"/>, a new one begun when <paramref name="wanted"/> and
    /// none is playing: the left button held or clicked on a wreck in reach, or, for another's hero, a
    /// swing sent and not yet shown. A swing just begun is drawn from its start.
    /// </summary>
    public SwingStep Advance(double elapsedMs, bool wanted)
    {
        bool begun = false, hits = false;
        double ms;
        if (_ms is double playing) ms = playing + Math.Min(elapsedMs, MostPerFrameMs);
        else if (wanted) (ms, begun) = (0, Begin());
        else return default;
        if (!_struck && ms >= HitAt * Ms) hits = _struck = true;
        if (ms >= Ms)
        {
            if (!wanted)
            {
                _ms = null;
                return new(begun, hits);
            }
            // Held, the next begins where this one ended, so the rhythm keeps the axe's pace exactly.
            ms -= Ms;
            begun = Begin();
        }
        _ms = ms;
        return new(begun, hits);
    }

    /// <summary>
    /// Another's hero, as sent: <paramref name="sent"/> is the swings it has begun while it chops
    /// (<see cref="Busy.Swings"/>), none while not. Each number not shown yet is swung once, after the
    /// swing playing, if any, ends; numbers passed meanwhile are swung as one.
    /// </summary>
    public SwingStep Follow(double elapsedMs, int? sent)
    {
        var step = Advance(elapsedMs, sent is int now && now != _followed);
        if (step.Begun) _followed = sent;
        return step;
    }

    /// <summary>Ends a swing at once, unstruck: the axe put away, the hand taken, the hero out.</summary>
    public void Stop() => _ms = null;

    private bool Begin()
    {
        Count++;
        _struck = false;
        return true;
    }

    /// <summary>
    /// The axe's turn from the line to the wreck at <paramref name="phase"/> of a swing, back (negative)
    /// or through (positive): eased up, brought down hard, eased back to the line.
    /// </summary>
    public static double Lift(double phase)
    {
        if (phase < WindShare) return -Raised * Math.Sin(phase / WindShare * Math.PI / 2);
        if (phase < WindShare + StrikeShare) return -Raised + (Raised + Through) * ((phase - WindShare) / StrikeShare);
        return Through * (1 - (phase - WindShare - StrikeShare) / (1 - WindShare - StrikeShare));
    }
}
