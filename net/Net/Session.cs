using Stillwake.Sim;

namespace Stillwake.Net;

/// <summary>
/// A session this machine plays in, and the time that drives it (apps/game/src/session/game.ts): its
/// own, with the host that releases its ticks, or one joined over the network, where the host is
/// another machine's and only the client is here. A load replaces the whole session.
/// </summary>
public sealed class Session(Host? host, Client client)
{
    private readonly Clock _clock = new(1000.0 / World.TicksPerSecond);
    private readonly Playout _playout = new();
    private readonly RoundTrip _roundTrip = new();
    private readonly List<DesyncReport> _desyncs = [];
    // The game's own clock, in ms: the time every frame moved on.
    private double _now;
    // The newest tick a joined peer has seen arrive.
    private int _heard = -1;

    /// <summary>This machine's host; none in a session joined from another machine.</summary>
    public Host? Host => host;

    public Client Client => client;

    /// <summary>How many game seconds pass per real second. The host's speed is everyone's: a peer's is unused.</summary>
    public double Speed { get; set; } = 1;

    /// <summary>Fraction of the way to the next tick, for drawing between ticks.</summary>
    public double Progress => host != null ? _clock.Progress : _playout.Progress(client.World.Tick, _now);

    /// <summary>How long a joined peer holds released ticks before it runs them, in ms; none on a host.</summary>
    public double? Reserve => host != null ? null : _playout.Reserve;

    /// <summary>How long this peer's commands take from sent to run, for the dev panel.</summary>
    public RoundTripTimes RoundTrip => _roundTrip.Read();

    /// <summary>
    /// Every time the host caught this machine's world drifting and resynced it: a determinism bug,
    /// already mended, so the report is all that is left of it.
    /// </summary>
    public IReadOnlyList<DesyncReport> Desyncs => _desyncs;

    /// <summary>Advances time by <paramref name="elapsedMs"/>, releases the ticks due if this machine hosts, and runs them.</summary>
    public IReadOnlyList<TickReport> Frame(double elapsedMs)
    {
        _now += elapsedMs;
        _roundTrip.Sent(client.InFlight.Count, _now);
        IReadOnlyList<TickReport> reports;
        if (host != null)
        {
            for (int n = _clock.Advance(elapsedMs * Speed); n > 0; n--) host.Advance();
            reports = client.Update();
        }
        else
        {
            // A joined session runs its host's ticks at a steady pace, never as they arrive (0015).
            int tick = client.World.Tick;
            int newest = tick + client.Queued - 1;
            // A resync may set the world back: what was heard after it is heard again.
            if (newest < _heard) _heard = newest;
            for (int t = Math.Max(_heard + 1, tick); t <= newest; t++) _playout.Arrived(t, _now);
            _heard = newest;
            reports = client.Update(_playout.Due(tick, client.Queued, _now));
        }
        _roundTrip.Ran(client.InFlight.Count, _now);
        _desyncs.AddRange(client.TakeDesyncs());
        return reports;
    }
}
