namespace Stillwake.Net;

/// <summary>What delays run on: real time in the game, a test's own in tests.</summary>
public interface ITiming
{
    /// <summary>Calls <paramref name="then"/> once <paramref name="ms"/> have passed.</summary>
    void Wait(double ms, Action then);

    /// <summary>A random share, 0 to 1.</summary>
    double Random();
}

/// <summary>
/// How bad to make a link, in ms: every message waits <c>Lag</c> plus up to <c>Jitter</c>
/// (apps/game/src/session/bad-network.ts). For reproducing on one machine what friends across the
/// internet see (0013): a netcode bug is reproduced here before it is fixed.
/// </summary>
public sealed record BadNetwork(double Lag, double Jitter)
{
    /// <summary><c>--lag=&lt;ms&gt;</c> and <c>--jitter=&lt;ms&gt;</c> from the command line; none when neither is there.</summary>
    public static BadNetwork? From(IEnumerable<string> args)
    {
        var given = args.Select(a => a.Split('=', 2)).Where(p => p[0] is "--lag" or "--jitter")
            .ToDictionary(p => p[0], p => p.Length > 1 && double.TryParse(p[1], System.Globalization.CultureInfo.InvariantCulture, out var ms) ? Math.Max(0, ms) : 0);
        if (given.Count == 0) return null;
        return new BadNetwork(given.GetValueOrDefault("--lag"), given.GetValueOrDefault("--jitter"));
    }

    /// <summary><paramref name="channel"/> as this network would carry it, both ways, each way in order.</summary>
    public IChannel<TIn, TOut> Over<TIn, TOut>(IChannel<TIn, TOut> channel, ITiming timing) =>
        new Late<TIn, TOut>(channel, new DelayLine(this, timing), new DelayLine(this, timing));

    private sealed class Late<TIn, TOut>(IChannel<TIn, TOut> channel, DelayLine output, DelayLine input) : IChannel<TIn, TOut>
    {
        private Action<TIn>? _handler;

        public void Send(TOut message) => output.Add(() => channel.Send(message));

        public void OnMessage(Action<TIn> handler)
        {
            // Listened to only once there is a receiver, so what comes first waits in the channel.
            bool first = _handler == null;
            _handler = handler;
            if (first) channel.OnMessage(m => input.Add(() => _handler!(m)));
        }
    }

    /// <summary>
    /// A line that hands things over late, each lag plus a random share of jitter after it came, but
    /// never before the one ahead of it: one ordered stream, which keeps order whatever it costs.
    /// </summary>
    private sealed class DelayLine(BadNetwork network, ITiming timing)
    {
        private readonly Queue<Entry> _line = new();

        public void Add(Action deliver)
        {
            var entry = new Entry(deliver);
            _line.Enqueue(entry);
            // Marked by its own timer, not judged by a clock that may say it is a hair early.
            timing.Wait(network.Lag + timing.Random() * network.Jitter, () =>
            {
                entry.Ready = true;
                // Only from the head: one not ready yet holds back every one behind it.
                while (_line.TryPeek(out var head) && head.Ready) _line.Dequeue().Deliver();
            });
        }

        private sealed class Entry(Action deliver)
        {
            public bool Ready;
            public Action Deliver => deliver;
        }
    }
}
