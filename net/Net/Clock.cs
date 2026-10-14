namespace Stillwake.Net;

/// <summary>
/// Turns frame time into whole steps of <paramref name="stepMs"/>: the host's ticks
/// (apps/game/src/session/clock.ts). A long gap — a breakpoint, a stalled window — releases at most
/// 250 ms of steps, so the game resumes instead of racing.
/// </summary>
public sealed class Clock(double stepMs)
{
    /// <summary>The most game time a long gap releases; the rest is dropped.</summary>
    private const double MaxCatchUpMs = 250;

    private readonly int _maxCatchUp = Math.Max(1, (int)Math.Floor(MaxCatchUpMs / stepMs));
    private double _carried;

    /// <summary>How many steps are due after <paramref name="elapsedMs"/> more time.</summary>
    public int Advance(double elapsedMs)
    {
        _carried += elapsedMs;
        int due = (int)Math.Floor(_carried / stepMs);
        _carried -= due * stepMs;
        if (due <= _maxCatchUp) return due;
        _carried = 0;
        return _maxCatchUp;
    }

    /// <summary>How far time is into the next step, from 0 to 1, for smoothing what is drawn.</summary>
    public double Progress => _carried / stepMs;
}
