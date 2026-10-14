using System.Text.Json;

namespace Stillwake.Net;

/// <summary>
/// One end of a two-way link: sends <typeparamref name="TOut"/>, receives <typeparamref name="TIn"/>,
/// in order. Messages are plain data (packages/net/src/channel.ts).
/// </summary>
public interface IChannel<TIn, TOut>
{
    void Send(TOut message);

    /// <summary>Sets the receiver; anything that arrived before it is delivered at once, in order.</summary>
    void OnMessage(Action<TIn> handler);
}

/// <summary>
/// Two ends of an in-process link, used by single-player and by tests. Every message arrives as a
/// copy, through JSON as over a network, so no peer can hold an object another peer also holds.
/// </summary>
public static class Loopback
{
    public static (IChannel<TBtoA, TAtoB> A, IChannel<TAtoB, TBtoA> B) Create<TAtoB, TBtoA>()
    {
        var a = new Inbox<TBtoA>();
        var b = new Inbox<TAtoB>();
        return (new End<TBtoA, TAtoB>(a, b), new End<TAtoB, TBtoA>(b, a));
    }

    private sealed class End<TIn, TOut>(Inbox<TIn> own, Inbox<TOut> other) : IChannel<TIn, TOut>
    {
        public void Send(TOut message) => other.Deliver(Copy(message));
        public void OnMessage(Action<TIn> handler) => own.Listen(handler);
    }

    private static T Copy<T>(T message) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(message, Protocol.Json), Protocol.Json)!;
}

/// <summary>
/// What arrives for one receiver, as every channel keeps it: held until it listens, then handed over
/// in order. A transport delivers into it.
/// </summary>
public sealed class Inbox<T>
{
    private Action<T>? _handler;
    private readonly Queue<T> _waiting = new();

    public void Deliver(T message)
    {
        if (_handler != null) _handler(message);
        else _waiting.Enqueue(message);
    }

    public void Listen(Action<T> handler)
    {
        _handler = handler;
        while (_waiting.TryDequeue(out var message)) handler(message);
    }
}
