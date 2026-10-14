namespace Stillwake.Net;

/// <summary>How long this peer's commands took from sent to run, in ms: the last, and the median of late.</summary>
public sealed record RoundTripTimes(double? Last, double? Median);

/// <summary>
/// Times this peer's commands from sent to run by watching how many are in flight
/// (apps/game/src/session/round-trip.ts): each new one is stamped when first seen, and one leaves,
/// oldest first, when its tick runs. Seen once a frame, so a time may read up to a frame short.
/// </summary>
public sealed class RoundTrip
{
    /// <summary>How many round trips the median is taken over.</summary>
    private const int Kept = 20;

    private readonly Queue<double> _sentAt = new();
    private readonly Queue<double> _times = new();
    private double? _last;

    /// <summary>Stamps the commands sent since the last look; called before the ticks run.</summary>
    public void Sent(int inFlight, double now)
    {
        while (_sentAt.Count < inFlight) _sentAt.Enqueue(now);
    }

    /// <summary>Times the commands that ran; called after the ticks run.</summary>
    public void Ran(int inFlight, double now)
    {
        while (_sentAt.Count > inFlight)
        {
            _last = now - _sentAt.Dequeue();
            _times.Enqueue(_last.Value);
            while (_times.Count > Kept) _times.Dequeue();
        }
    }

    public RoundTripTimes Read()
    {
        var sorted = _times.Order().ToList();
        return new RoundTripTimes(_last, sorted.Count > 0 ? sorted[sorted.Count / 2] : null);
    }
}
