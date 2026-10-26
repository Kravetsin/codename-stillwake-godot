using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Net;

namespace Stillwake.Game.Net;

/// <summary>
/// Net's channels over the playtest server's relay (ADR 0012, <see cref="RelayFrames"/>): a WebSocket
/// out to the server, so neither end needs an open port; the relay passes a host's messages to its
/// guests and theirs back. A host is told its session's name (<see cref="Hosted"/>); a guest joins it
/// by that name. The socket's buffers fit a world sent whole to a guest joining.
/// </summary>
public sealed class RelayLink<TIn, TOut> : ILink<TIn, TOut>
{
    /// <summary>The most a frame may hold, either way: a world's save rides in one.</summary>
    private const int Buffer = 32 * 1024 * 1024;

    private readonly WebSocketPeer _socket = new() { InboundBufferSize = Buffer, OutboundBufferSize = Buffer, MaxQueuedPackets = 4096 };
    private readonly Dictionary<long, Channel> _channels = new();
    private readonly bool _hosting;
    private readonly string _opening;
    private bool _open, _closed;

    private RelayLink(bool hosting, string opening)
    {
        _hosting = hosting;
        _opening = opening;
    }

    public event Action<long, IChannel<TIn, TOut>>? Connected;
    public event Action<long>? Disconnected;

    /// <summary>The relay opened this host's session, by the name a guest joins with.</summary>
    public event Action<string>? Hosted;

    /// <summary>The link could not be made or was lost, and why, in a few words.</summary>
    public event Action<string>? Failed;

    /// <summary>Hosts a session named <paramref name="name"/> on the relay at <paramref name="url"/>.</summary>
    public static RelayLink<ToHost, ToClient> Host(string url, string password, string? name) =>
        Open(new RelayLink<ToHost, ToClient>(true, RelayFrames.HostFrame(name)), url, password);

    /// <summary>Joins session <paramref name="session"/> on the relay at <paramref name="url"/>.</summary>
    public static RelayLink<ToClient, ToHost> Join(string url, string password, string session) =>
        Open(new RelayLink<ToClient, ToHost>(false, RelayFrames.JoinFrame(session)), url, password);

    private static RelayLink<I, O> Open<I, O>(RelayLink<I, O> link, string url, string password)
    {
        link._socket.HandshakeHeaders = [RelayFrames.Authorization(password)];
        var error = link._socket.ConnectToUrl(url);
        if (error != Error.Ok) link._closed = true;
        return link;
    }

    public void Poll()
    {
        if (_closed) return;
        if (_open) Flush();
        _socket.Poll();
        switch (_socket.GetReadyState())
        {
            case WebSocketPeer.State.Open:
                if (!_open)
                {
                    _open = true;
                    _socket.SendText(_opening);
                }
                while (_socket.GetAvailablePacketCount() > 0) Take(_socket.GetPacket().GetStringFromUtf8());
                break;
            case WebSocketPeer.State.Closed:
                _closed = true;
                Failed?.Invoke(_open
                    ? $"the relay closed the link ({_socket.GetCloseCode()})"
                    : "cannot reach the relay: wrong password, or the server is down");
                foreach (long id in new List<long>(_channels.Keys)) Disconnected?.Invoke(id);
                _channels.Clear();
                break;
        }
    }

    public void Close()
    {
        if (_open && !_closed) Flush();
        _socket.Close();
        _closed = true;
    }

    private void Take(string text)
    {
        if (RelayFrames.Read(text) is not { } frame) return;
        switch (frame.Event)
        {
            case RelayFrames.Hosted when frame.Session is { } session:
                Hosted?.Invoke(session);
                break;
            case RelayFrames.Joined when _hosting && frame.Peer is int peer:
                Add(peer);
                break;
            case RelayFrames.Connected when !_hosting:
                // A guest's only peer is its host.
                Add(0);
                break;
            case RelayFrames.Refused:
                Failed?.Invoke("no session by that name on the relay");
                break;
            case RelayFrames.FromPeer when frame is { Peer: int from, Message: { } message }:
                if (_channels.TryGetValue(from, out var channel)) Deliver(channel, message);
                break;
            case RelayFrames.FromHost when frame.Message is { } message:
                if (_channels.TryGetValue(0, out var host)) Deliver(host, message);
                break;
            case RelayFrames.Left when frame.Peer is int gone:
                if (_channels.Remove(gone)) Disconnected?.Invoke(gone);
                break;
            case RelayFrames.HostLeft:
                if (_channels.Remove(0)) Disconnected?.Invoke(0);
                break;
        }
    }

    // What the other end batched into a frame, in order.
    private static void Deliver(Channel channel, string text)
    {
        if (RelayFrames.Unbatch(text) is not { } batch)
        {
            Links.Read<TIn>(null);
            return;
        }
        foreach (var bytes in batch)
            if (Links.Read<TIn>(bytes) is { } message) channel.Inbox.Deliver(message);
    }

    // Each channel's messages since the last poll, in one frame: sent a frame late, to cost one frame's text, not one a message.
    private void Flush()
    {
        foreach (var (peer, channel) in _channels)
        {
            if (channel.Outbox.Count == 0) continue;
            string batch = RelayFrames.Batch(channel.Outbox);
            _socket.SendText(_hosting ? RelayFrames.ToPeerFrame((int)peer, batch) : RelayFrames.ToHostFrame(batch));
            channel.Outbox.Clear();
        }
    }

    private void Add(long id)
    {
        var channel = new Channel();
        _channels[id] = channel;
        Connected?.Invoke(id, channel);
    }

    private sealed class Channel : IChannel<TIn, TOut>
    {
        public Inbox<TIn> Inbox { get; } = new();
        public List<byte[]> Outbox { get; } = [];

        public void Send(TOut message) => Outbox.Add(Protocol.Write(message));

        public void OnMessage(Action<TIn> handler) => Inbox.Listen(handler);
    }
}
