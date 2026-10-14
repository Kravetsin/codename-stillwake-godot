using Stillwake.Sim;

namespace Stillwake.Net;

/// <summary>
/// When a joined peer runs the ticks its host released (0015, apps/game/src/session/playout.ts): each
/// at the earliest it has been seen to arrive, plus a reserve sized from how late ticks have come over
/// the last few seconds. A jitter buffer: the link delivers unevenly, and running ticks as they come
/// would move the world in bursts, while a fixed reserve wide enough for a bad link wastes time on a
/// good one. Times are the game's own clock, in ms.
/// </summary>
public sealed class Playout
{
    private const double TickMs = 1000.0 / World.TicksPerSecond;
    /// <summary>How many arrivals the reserve is sized from: five seconds of ticks.</summary>
    private const int Kept = 5 * World.TicksPerSecond;
    /// <summary>Above how late the late ones came, the reserve keeps this much more.</summary>
    private const double MarginMs = 10;
    /// <summary>The reserve before anything is measured, and its bounds.</summary>
    private const double FirstMs = 100, MinMs = 10, MaxMs = 300;
    /// <summary>How fast the reserve may change, per millisecond passed: so the world never jumps or stalls.</summary>
    private const double Ease = 0.1;
    /// <summary>How fast the earliest-arrival estimate may slip later, per millisecond: a path grown slower.</summary>
    private const double Slip = 0.01;
    /// <summary>A tick this much later than the estimate says starts it over: the host paused, say.</summary>
    private const double RestartMs = 500;
    /// <summary>At most this many ticks a frame, draining a backlog; as the host's clock catches up.</summary>
    private const int MaxPerFrame = 5;

    // This machine's clock less the host's tick times, as the quickest arrival says.
    private double? _offset;
    private double _heardAt;
    private readonly Queue<double> _late = new();
    private double _target = FirstMs;
    private double? _easedAt;

    /// <summary>The reserve now, in ms, for the dev panel.</summary>
    public double Reserve { get; private set; } = FirstMs;

    /// <summary>Tick <paramref name="tick"/> arrived at <paramref name="now"/>.</summary>
    public void Arrived(int tick, double now)
    {
        double sample = now - tick * TickMs;
        if (_offset is not double offset || sample - offset > RestartMs)
        {
            _offset = sample;
            _late.Clear();
        }
        else _offset = Math.Min(offset + (now - _heardAt) * Slip, sample);
        _heardAt = now;
        _late.Enqueue(sample - _offset.Value);
        while (_late.Count > Kept) _late.Dequeue();
        var sorted = _late.Order().ToList();
        double p95 = sorted[Math.Min(sorted.Count - 1, (int)Math.Floor(sorted.Count * 0.95))];
        _target = Math.Min(MaxMs, Math.Max(MinMs, p95 + MarginMs));
    }

    /// <summary>How many of the <paramref name="queued"/> ticks after <paramref name="tick"/> are due at <paramref name="now"/>.</summary>
    public int Due(int tick, int queued, double now)
    {
        if (_offset is not double offset) return 0;
        double step = (now - (_easedAt ?? now)) * Ease;
        Reserve += Math.Max(-step, Math.Min(step, _target - Reserve));
        _easedAt = now;
        double last = Math.Floor((now - offset - Reserve) / TickMs);
        return (int)Math.Max(0, Math.Min(Math.Min(queued, MaxPerFrame), last - tick + 1));
    }

    /// <summary>How far time is from tick <paramref name="tick"/> towards the next, 0 to 1, for drawing between ticks.</summary>
    public double Progress(int tick, double now)
    {
        if (_offset is not double offset) return 0;
        double into = (now - (offset + Reserve + (tick - 1) * TickMs)) / TickMs;
        return Math.Clamp(into, 0, 1);
    }
}
