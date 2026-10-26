namespace Stillwake.Net;

/// <summary>Where a host gets the world it welcomes a joining peer with: peer 0's, the host player's.</summary>
public interface IWorldSource
{
    string Build { get; }

    /// <summary>The world saved on the tick the host releases next.</summary>
    string Save();
}

/// <summary>
/// Orders the session (packages/net/src/host.ts): collects every peer's commands and releases them
/// tick by tick. It runs no world of its own; the host player's game is a client like any other, peer
/// <paramref name="hostId"/>, and the one every other peer's hashes are checked against: a peer that
/// differs is resynced. <paramref name="firstTick"/> is the world's current tick; a host with no
/// <paramref name="world"/> admits no one, as tests and single-player need none.
/// </summary>
public sealed class Host(int firstTick, IWorldSource? world = null, int hostId = 0)
{
    /// <summary>Peer 0's hashes are kept for this many checks: a peer further behind is not checked.</summary>
    private const int Kept = 30;

    private List<Stamped> _pending = [];
    private readonly List<(int Id, IChannel<ToHost, ToClient> Channel)> _peers = [];
    private int _nextPeer;
    // Peer 0's hash by tick, oldest first; others' reports that came first wait for it.
    private readonly SortedDictionary<int, ulong> _truth = new();
    private readonly SortedDictionary<int, List<(int Id, ulong Hash)>> _waiting = new();
    // The tick each peer was last resynced on: reports older than it are of the world it dropped.
    private readonly Dictionary<int, int> _resyncedAt = new();

    /// <summary>The next tick <see cref="Advance"/> releases.</summary>
    public int Tick { get; private set; } = firstTick;

    /// <summary>How many peers are connected, the host player's own among them.</summary>
    public int Peers => _peers.Count;

    /// <summary>
    /// Adds a peer, which receives every tick released from now on and every other peer's hero, and
    /// returns its id: every command and hero it sends is stamped with it. The id is <paramref name="seat"/>,
    /// the one its player always has in this world (0032), or else the next of a count that never hands
    /// one out twice; a seat already connected is our bug.
    /// </summary>
    public int Connect(IChannel<ToHost, ToClient> channel, int? seat = null)
    {
        int id = seat ?? _nextPeer;
        if (_peers.Exists(p => p.Id == id)) throw new InvalidOperationException($"Peer {id} is connected already.");
        _nextPeer = Math.Max(_nextPeer, id + 1);
        _peers.Add((id, channel));
        channel.OnMessage(message =>
        {
            switch (message)
            {
                case HeroMessage hero:
                    var passed = new PeerHeroMessage(id, hero.State);
                    foreach (var peer in _peers.ToList()) if (peer.Id != id) peer.Channel.Send(passed);
                    break;
                case HashMessage hash:
                    Report(id, hash.Tick, hash.Hash);
                    break;
                case CommandMessage command:
                    // Stamped here, so the sender is the host's word, whatever the client wrote.
                    _pending.Add(new Stamped(id, command.Command));
                    break;
            }
        });
        return id;
    }

    /// <summary>
    /// Connects a peer that joins mid-session and holds no world yet: it is welcomed first with the
    /// world as it stands, then receives every tick released from now on, as <paramref name="seat"/> if
    /// given. Returns its id.
    /// </summary>
    public int Admit(IChannel<ToHost, ToClient> channel, int? seat = null)
    {
        // Our bug: only a session that knows its world opens to joining peers.
        if (world == null) throw new InvalidOperationException("This host has no world to welcome a peer with.");
        // Saved before connecting, so the peer misses no tick and gets none twice.
        var save = world.Save();
        int id = Connect(channel, seat);
        channel.Send(new WelcomeMessage(id, world.Build, save));
        return id;
    }

    /// <summary>
    /// Drops peer <paramref name="id"/>, gone from the session: it is sent nothing more, and its hashes
    /// are checked no more. Its commands already received still go out in the next tick, then
    /// <paramref name="farewell"/>, an encoded command, if given, stamped with its id: what the game does
    /// with a peer that left, such as take its hero out.
    /// </summary>
    public void Disconnect(int id, byte[]? farewell = null)
    {
        int at = _peers.FindIndex(p => p.Id == id);
        if (at < 0) return;
        _peers.RemoveAt(at);
        _resyncedAt.Remove(id);
        // Its seat may come back: what it reported before must not be checked against the next world.
        foreach (var list in _waiting.Values) list.RemoveAll(w => w.Id == id);
        if (farewell != null) _pending.Add(new Stamped(id, farewell));
    }

    /// <summary>
    /// Releases <paramref name="command"/>, encoded, in the next tick as though peer <paramref name="from"/>
    /// sent it, connected or not: how a world loaded with heroes of players not here takes them out.
    /// </summary>
    public void Release(int from, byte[] command) => _pending.Add(new Stamped(from, command));

    /// <summary>Releases the next tick, carrying every command received since the last one.</summary>
    public void Advance()
    {
        var message = new TickMessage(Tick, _pending);
        _pending = [];
        Tick += 1;
        foreach (var peer in _peers.ToList()) peer.Channel.Send(message);
    }

    private void Report(int id, int at, ulong hash)
    {
        if (id != hostId)
        {
            if (_truth.ContainsKey(at)) Check(id, at, hash);
            else
            {
                if (!_waiting.TryGetValue(at, out var list)) _waiting[at] = list = [];
                list.Add((id, hash));
            }
            return;
        }
        _truth[at] = hash;
        while (_truth.Count > Kept) _truth.Remove(_truth.Keys.First());
        if (_waiting.Remove(at, out var others))
            foreach (var (other, theirs) in others) Check(other, at, theirs);
        foreach (var old in _waiting.Keys.Where(k => k < at).ToList()) _waiting.Remove(old);
    }

    private void Check(int id, int at, ulong hash)
    {
        if (at <= _resyncedAt.GetValueOrDefault(id, -1) || _truth.GetValueOrDefault(at) == hash) return;
        var peer = _peers.Find(p => p.Id == id);
        if (world == null || peer.Channel == null) return;
        _resyncedAt[id] = Tick;
        peer.Channel.Send(new ResyncMessage(world.Save()));
    }
}
