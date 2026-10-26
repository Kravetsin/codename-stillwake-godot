using System;
using Stillwake.Net;

namespace Stillwake.Game.Net;

/// <summary>
/// What carries a session between peers, ENet or the playtest relay: peers coming and going, each a
/// channel, delivered when the game calls <see cref="Poll"/> every frame on the main thread.
/// </summary>
public interface ILink<TIn, TOut>
{
    /// <summary>A peer at the other end connected: its id on this link, and the channel to it.</summary>
    event Action<long, IChannel<TIn, TOut>>? Connected;

    /// <summary>A peer at the other end went: its id on this link.</summary>
    event Action<long>? Disconnected;

    void Poll();

    void Close();
}
