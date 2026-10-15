using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Net;

namespace Stillwake.Game.Net;

/// <summary>
/// Net's channels over Godot's ENet, one reliable ordered stream per peer: a host hears its guests
/// (<see cref="Host"/>), a guest its host (<see cref="Join"/>). Messages go as Protocol JSON. Nothing
/// happens until <see cref="Poll"/>, which the game calls every frame on the main thread.
/// </summary>
public sealed class EnetLink<TIn, TOut> : ILink<TIn, TOut>
{
    private readonly ENetMultiplayerPeer _peer = new();
    private readonly Dictionary<long, Channel> _channels = new();

    private EnetLink() { }

    /// <summary>A peer at the other end connected: its ENet id, and the channel to it.</summary>
    public event Action<long, IChannel<TIn, TOut>>? Connected;

    /// <summary>A peer at the other end went: its ENet id.</summary>
    public event Action<long>? Disconnected;

    /// <summary>Delivers what arrived since the last call, and tells of peers that came or went.</summary>
    public void Poll()
    {
        _peer.Poll();
        while (_peer.GetAvailablePacketCount() > 0)
        {
            long from = _peer.GetPacketPeer();
            var bytes = _peer.GetPacket();
            if (_channels.TryGetValue(from, out var channel)) channel.Inbox.Deliver(Protocol.Read<TIn>(bytes));
        }
    }

    public void Close() => _peer.Close();

    private Error Start(Func<ENetMultiplayerPeer, Error> create)
    {
        _peer.PeerConnected += id =>
        {
            var channel = new Channel(_peer, (int)id);
            _channels[id] = channel;
            Connected?.Invoke(id, channel);
        };
        _peer.PeerDisconnected += id =>
        {
            _channels.Remove(id);
            Disconnected?.Invoke(id);
        };
        return create(_peer);
    }

    /// <summary>Listens on <paramref name="port"/> for guests.</summary>
    public static (EnetLink<ToHost, ToClient> Link, Error Error) Host(int port)
    {
        var link = new EnetLink<ToHost, ToClient>();
        return (link, link.Start(peer => peer.CreateServer(port)));
    }

    /// <summary>Connects to a host; it shows as a peer connecting once it answers.</summary>
    public static (EnetLink<ToClient, ToHost> Link, Error Error) Join(string address, int port)
    {
        var link = new EnetLink<ToClient, ToHost>();
        return (link, link.Start(peer => peer.CreateClient(address, port)));
    }

    private sealed class Channel(ENetMultiplayerPeer peer, int target) : IChannel<TIn, TOut>
    {
        public Inbox<TIn> Inbox { get; } = new();

        public void Send(TOut message)
        {
            peer.SetTargetPeer(target);
            peer.TransferMode = MultiplayerPeer.TransferModeEnum.Reliable;
            peer.PutPacket(Protocol.Write(message));
        }

        public void OnMessage(Action<TIn> handler) => Inbox.Listen(handler);
    }
}
