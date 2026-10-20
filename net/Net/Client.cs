using Stillwake.Sim;

namespace Stillwake.Net;

/// <summary>What one released tick did on this peer: the commands it refused.</summary>
public sealed record TickReport(int Tick, IReadOnlyList<Refused> Refused);

/// <summary>
/// A peer's view of the session (packages/net/src/client.ts): it sends commands and runs only the ticks
/// the host released. <paramref name="load"/> makes a world from a save: a client the host may resync
/// needs it. The host's own client, whose world the others are checked against, is never resynced and
/// goes without.
/// </summary>
public sealed class Client
{
    private readonly IChannel<ToClient, ToHost> _channel;
    private readonly Func<string, World>? _load;
    private readonly Queue<ToClient> _released = new();
    private readonly List<Command> _inFlight = [];
    private List<PeerHeroMessage> _heroes = [];
    private List<DesyncReport> _desyncs = [];
    private int _queued;

    public Client(World start, IChannel<ToClient, ToHost> channel, int id, Func<string, World>? load = null)
    {
        World = start;
        _channel = channel;
        Id = id;
        _load = load;
        channel.OnMessage(message =>
        {
            switch (message)
            {
                // A peer that holds its world is connected, never welcomed: a welcome here is our bug.
                case WelcomeMessage:
                    throw new InvalidOperationException("A client holding its world was welcomed.");
                case PeerHeroMessage hero:
                    _heroes.Add(hero);
                    break;
                default:
                    if (message is TickMessage) _queued += 1;
                    _released.Enqueue(message);
                    break;
            }
        });
    }

    /// <summary>This peer's id, which the host stamps on its commands: its hero's id in the sim.</summary>
    public int Id { get; }

    /// <summary>The world this peer runs; a resync from the host replaces it.</summary>
    public World World { get; private set; }

    /// <summary>
    /// The commands this peer sent that have not run yet, oldest first: what the game shows at once,
    /// before the host's tick confirms or refuses it (0014). A command leaves when its tick runs. Those sent from a
    /// surface this peer has left since are not here: they will be refused, and their cells are another grid's (0076).
    /// </summary>
    public IReadOnlyList<Command> InFlight
    {
        get
        {
            int here = World.SurfaceOf(Id);
            foreach (var c in _inFlight)
                if (c.Surface is int sent && sent != here) return _inFlight.FindAll(f => f.Surface is not int s || s == here);
            return _inFlight;
        }
    }

    /// <summary>How many released ticks wait to run; a resync waiting among them is not counted.</summary>
    public int Queued => _queued;

    /// <summary>The hash this peer last told the host, for showing: hashing the world again is not free.</summary>
    public ulong? LastHash { get; private set; }

    /// <summary>Asks the host to apply a command; it takes effect on the tick the host puts it in.</summary>
    public void Send(Command command)
    {
        // Stamped with where this peer is now: should its hero leave before the tick, the command is refused (0076).
        command = command with { Surface = World.SurfaceOf(Id) };
        _inFlight.Add(command);
        _channel.Send(new CommandMessage(World.Encode(command)));
    }

    /// <summary>Tells every other peer, through the host, where this peer's hero is now.</summary>
    public void SendHero(string state) => _channel.Send(new HeroMessage(state));

    /// <summary>The other peers' heroes received since the last call, in order of arrival.</summary>
    public IReadOnlyList<PeerHeroMessage> TakeHeroes()
    {
        var taken = _heroes;
        _heroes = [];
        return taken;
    }

    /// <summary>How this peer's world had drifted, for each resync since the last call.</summary>
    public IReadOnlyList<DesyncReport> TakeDesyncs()
    {
        var taken = _desyncs;
        _desyncs = [];
        return taken;
    }

    /// <summary>
    /// Runs the released ticks not run yet, in order, at most <paramref name="limit"/> of them, and
    /// returns their reports. Every <see cref="Protocol.HashEvery"/> ticks it tells the host its hash; a
    /// resync from the host, met in order, is taken in between and does not count.
    /// </summary>
    /// <summary>
    /// Whether this peer hashes its world for the host to check. A host player alone has no one to be
    /// checked against, and a hash costs milliseconds on a big world: its session turns this off.
    /// </summary>
    public bool Hashing { get; set; } = true;

    public IReadOnlyList<TickReport> Update(int limit = int.MaxValue)
    {
        var reports = new List<TickReport>();
        // Sent once the loop is done: the host may check a hash at once and save the host player's
        // world, which must not happen with this client halfway through its ticks.
        var hashes = new List<HashMessage>();
        while (_released.TryPeek(out var message))
        {
            if (message is ResyncMessage resync)
            {
                _released.Dequeue();
                Resync(resync.Save);
                continue;
            }
            if (reports.Count >= limit) break;
            _released.Dequeue();
            _queued -= 1;
            var tick = (TickMessage)message;
            // The host releases ticks one by one over an ordered link; a gap is our bug.
            if (tick.Tick != World.Tick)
                throw new InvalidOperationException($"Expected tick {World.Tick}, but the host released {tick.Tick}.");
            var commands = tick.Commands.Select(s => World.Decode(s.Command) with { From = s.From }).ToList();
            Landed(commands);
            reports.Add(new TickReport(tick.Tick, World.Step(commands)));
            if (Hashing && World.Tick % Protocol.HashEvery == 0)
            {
                LastHash = World.Hash();
                hashes.Add(new HashMessage(World.Tick, LastHash.Value));
            }
        }
        foreach (var hash in hashes) _channel.Send(hash);
        return reports;
    }

    // The host passes this peer's commands on in the order they came, over an ordered link: each of
    // them in a released tick is the oldest in flight.
    private void Landed(List<Command> commands)
    {
        foreach (var command in commands)
        {
            if (command.From != Id) continue;
            var head = _inFlight.Count > 0 ? _inFlight[0] : null;
            if (head?.GetType() != command.GetType())
                throw new InvalidOperationException(
                    $"Peer {Id} sent {head?.GetType().Name ?? "nothing"} next, but {command.GetType().Name} came back.");
            _inFlight.RemoveAt(0);
        }
    }

    private void Resync(string save)
    {
        // Our bug: the host resyncs only peers it welcomed, which join with a way to load.
        if (_load == null) throw new InvalidOperationException("A client with no way to load a world was resynced.");
        // Every tick before the resync has run, so both saves are of the same tick.
        _desyncs.Add(Diff.Saves(World.Save(), save));
        World = _load(save);
    }

    /// <summary>
    /// Joins a session mid-way over <paramref name="channel"/> (packages/net/src/join.ts): waits for the
    /// host's welcome, loads the world it carries, and calls <paramref name="joined"/> with a client that
    /// catches up on every tick released since. <paramref name="refused"/> is called instead when the
    /// host runs another build, since every peer must run the same simulation, or will not let it in. It
    /// says who it is first, <paramref name="player"/>'s key (0032), when it has one.
    /// </summary>
    public static void Join(
        Func<string, World> load,
        IChannel<ToClient, ToHost> channel,
        string build,
        Action<Client> joined,
        Action<Exception> refused,
        string player = "")
    {
        Action<ToClient>? deliver = null;
        channel.OnMessage(message =>
        {
            if (deliver != null)
            {
                deliver(message);
                return;
            }
            if (message is RefusedMessage no)
            {
                deliver = _ => { };
                refused(new InvalidOperationException(no.Reason));
                return;
            }
            // The host welcomes before it releases any tick to a peer: anything else first is our bug.
            if (message is not WelcomeMessage welcome)
                throw new InvalidOperationException($"A {message.GetType().Name} came before the welcome.");
            if (welcome.Build != build)
            {
                // Whatever the host sends after goes nowhere: this peer will not play.
                deliver = _ => { };
                refused(new InvalidOperationException($"The host runs build {welcome.Build}; this is build {build}."));
                return;
            }
            // The client takes the channel from here: every message after the welcome goes to it.
            var rest = new Rest(channel, handler => deliver = handler);
            joined(new Client(load(welcome.Save), rest, welcome.Id, load));
        });
        if (player != "") channel.Send(new HelloMessage(player));
    }

    private sealed class Rest(IChannel<ToClient, ToHost> channel, Action<Action<ToClient>> listen) : IChannel<ToClient, ToHost>
    {
        public void Send(ToHost message) => channel.Send(message);
        public void OnMessage(Action<ToClient> handler) => listen(handler);
    }
}
