using Stillwake.Sim;

namespace Stillwake.Net.Tests;

// Modules that exist only for net's tests (packages/net/src/testing/fixtures.ts): real ones live in the sim.

public sealed record Write(string Text) : Command;

public sealed record Push(int By) : Command;

public sealed record Note(int Tick, string Text);

public sealed class BoardState
{
    public List<Note> Notes { get; set; } = [];
    public int Ticks { get; set; }
}

/// <summary>A shared log: the order of its entries shows the order commands applied in.</summary>
public sealed class BoardModule : Module<BoardState>
{
    private static readonly string[] Texts = ["", "hello", "air low", "dock"];

    public override string Id => "board";

    public BoardModule()
    {
        Handles<Write>(c => c.Text == "" ? "board.empty" : null, c => State.Notes.Add(new Note(State.Ticks, c.Text)));
        System(Phase.Simulation, () => State.Ticks += 1);
    }

    protected override BoardState Init(Layout layout) => new();

    public IReadOnlyList<string> Notes => State.Notes.Select(n => $"{n.Tick}:{n.Text}").ToList();
    public int Count => State.Notes.Count;

    public override Command? Fuzz(Rng rng, int from) => new Write(Texts[rng.Below((uint)Texts.Length)]) { From = from };
}

public sealed class DriftState
{
    public int Position { get; set; }
    public int Pushes { get; set; }
}

/// <summary>Drifts by the world's random draws and reacts to the board, so both feed the hash.</summary>
public sealed class DriftModule : Module<DriftState>
{
    public override string Id => "drift";

    public DriftModule(BoardModule board)
    {
        Handles<Push>(c => c.By is < 1 or > 3 ? "drift.badPush" : null, c => State.Pushes += c.By);
        System(Phase.Simulation, () =>
        {
            State.Position += (int)Random.Below(3) - 1 + State.Pushes;
            State.Pushes = board.Count % 2;
        });
    }

    protected override DriftState Init(Layout layout) => new();

    /// <summary>Changes this world behind the pipeline's back, as a determinism bug would.</summary>
    public void Nudge(int by) => State.Position += by;

    public override Command? Fuzz(Rng rng, int from) => new Push((int)rng.Below(5)) { From = from };
}

/// <summary>
/// How worlds of one set of modules are made and loaded (SimDefinition): each world remembers its
/// modules, which hold its state, so a test can read the world a client runs now.
/// </summary>
/// <param name="start">What a world is made with past its map: its save, changed before it is played. Only tests use it.</param>
public sealed class Definition(Func<IReadOnlyList<Module>> make, Func<string, string>? start = null)
{
    private readonly Dictionary<World, IReadOnlyList<Module>> _modules = new(ReferenceEqualityComparer.Instance);

    /// <summary>Board alone, as most session tests use.</summary>
    public static Definition Board() => new(() => [new BoardModule()]);

    /// <summary>Board and drift.</summary>
    public static Definition Fixtures() => new(() =>
    {
        var board = new BoardModule();
        return [board, new DriftModule(board)];
    });

    /// <summary>Every registered module of the game.</summary>
    public static Definition Game() => new(() => new Registry().All);

    public IReadOnlyList<Module> Make() => make();

    public World Create(uint seed = 1, Layout? layout = null)
    {
        var modules = make();
        var world = World.Create(seed, layout ?? Layout.Empty, modules);
        if (start != null) world = World.Load(start(world.Save()), modules = make());
        _modules[world] = modules;
        return world;
    }

    public World Load(string save)
    {
        var modules = make();
        var world = World.Load(save, modules);
        _modules[world] = modules;
        return world;
    }

    public IReadOnlyList<Module> ModulesOf(World world) => _modules[world];

    public T Module<T>(World world) where T : Module => ModulesOf(world).OfType<T>().Single();
}

/// <summary>A channel fed by hand, as the host's messages reach a peer over a network; what it sends is kept.</summary>
public sealed class Fed<TIn, TOut> : IChannel<TIn, TOut>
{
    private Action<TIn> _receive = _ => { };

    public List<TOut> Sent { get; } = [];

    public void Send(TOut message) => Sent.Add(message);
    public void OnMessage(Action<TIn> handler) => _receive = handler;
    public void Receive(TIn message) => _receive(message);
}

/// <summary>Time that moves only when told, and a random draw the test sets; timers may fire <c>early</c>.</summary>
public sealed class TestTiming(double early = 0) : ITiming
{
    private readonly List<(double At, Action Then)> _timers = [];

    public double Now { get; private set; }
    public double Draw { get; set; }

    public void Wait(double ms, Action then) => _timers.Add((Now + ms, then));
    public double Random() => Draw;

    /// <summary>Moves time on to <paramref name="to"/>, firing every timer due by then, earliest first.</summary>
    public void Pass(double to)
    {
        while (true)
        {
            int next = -1;
            for (int i = 0; i < _timers.Count; i++)
                if (_timers[i].At <= to && (next < 0 || _timers[i].At < _timers[next].At)) next = i;
            if (next < 0) break;
            var (at, then) = _timers[next];
            _timers.RemoveAt(next);
            Now = at - early;
            then();
        }
        Now = to;
    }
}
