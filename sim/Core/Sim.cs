using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stillwake.Sim;

/// <summary>
/// One world: its surfaces (0076), each a set of modules in dependency order, each owning its slice. A world
/// changes only through <see cref="Step"/>; a loaded one gets a sim of its own (packages/sim/src/core/sim.ts).
/// </summary>
public sealed class World
{
    /// <summary>
    /// Bumped, with its step in <c>Migrations</c>, when saved data is removed, renamed or changes meaning, or a module is
    /// added; a save of another format is refused. A field added with its default needs no bump: an older build refuses
    /// it as unknown (<see cref="Load"/>), this one reads an older save's lack of it as the default.
    /// </summary>
    public const int SaveFormat = 41;

    /// <summary>How many ticks a second of game time holds: the host releases them at this pace.</summary>
    public const int TicksPerSecond = 20;

    /// <summary>Why a command stamped on one surface is refused once its sender stands on another.</summary>
    public const string Elsewhere = "world.elsewhere";

    /// <summary>Why a transfer is refused: no surface of that id.</summary>
    public const string NoSurface = "world.noSurface";

    /// <summary>Why a transfer is refused: its sender is on that surface already.</summary>
    public const string There = "world.there";

    /// <summary>How many surfaces <see cref="Fuzz"/> opens at most: each is a whole set of modules.</summary>
    private const int FuzzedSurfaces = 3;

    // In id order, which is their index: surface 0, the ship, is the modules the world was made with.
    private readonly List<Running> _surfaces = [];
    // Peer → surface, for peers off the ship only: a peer not listed is on the ship.
    private readonly SortedDictionary<int, int> _where = new();
    // Moves asked by modules while the surfaces ran, made once they all have, in the order asked.
    private readonly List<(int Peer, int Surface)> _sends = [];
    // The planet a descent asked for this tick (ISurfaces.OpenPlanet), opened before the sends; never saved, empty between steps.
    private string? _opening;
    private readonly RngState _rng;
    private readonly Rng _rngOver;
    // The world's own commands, which no surface's modules handle.
    private readonly Dictionary<Type, (Func<Command, string?> Validate, Action<Command> Apply)> _own = new();

    /// <summary>The next tick to run.</summary>
    public int Tick { get; private set; }

    /// <summary>How saves are written.</summary>
    public JsonSerializerOptions Json { get; }

    /// <summary>Every command type there is, in order of its name: how the network tags commands.</summary>
    public IReadOnlyList<Type> CommandTypes { get; }

    private World(IReadOnlyList<Module> modules, RngState rng, int tick, string kind)
    {
        _rng = rng;
        _rngOver = new Rng(rng);
        Tick = tick;
        _surfaces.Add(Attach(new Surface(0, kind, 0), modules));
        _own.Add(typeof(OpenSurface), (_ => null, c => Open((OpenSurface)c)));
        _own.Add(typeof(Transfer), (c => TransferProblem((Transfer)c), c => Move(c.From, ((Transfer)c).To, ((Transfer)c).Cell)));
        // Every surface runs the ship's modules, so the ship's commands are every module command there is.
        CommandTypes = CommandTypesOf(modules);
        Json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, IncludeFields = false };
    }

    /// <summary>The command types of a world made with <paramref name="modules"/>, its own included, in order of their names.</summary>
    public static IReadOnlyList<Type> CommandTypesOf(IEnumerable<Module> modules) =>
        modules.SelectMany(m => m.Commands).Select(c => (c.Type, c.Name))
            .Append((typeof(OpenSurface), "world.open")).Append((typeof(Transfer), "world.transfer"))
            .OrderBy(c => c.Item2, StringComparer.Ordinal).Select(c => c.Item1).ToList();

    /// <summary>The world's surfaces, in id order: the ship first.</summary>
    public IReadOnlyList<Surface> Surfaces => _surfaces.ConvertAll(s => s.Surface);

    /// <summary>The surface peer <paramref name="peer"/>'s hero is on: where its commands go.</summary>
    public int SurfaceOf(int peer) => _where.TryGetValue(peer, out int s) ? s : 0;

    /// <summary>The modules surface <paramref name="surface"/> runs, in run order: the ship's are those the world was made with.</summary>
    public IReadOnlyList<Module> ModulesOn(int surface) => _surfaces[surface].Modules;

    /// <summary>Whether this world can open surfaces: its ship's modules can make another set (<see cref="IModules"/>).</summary>
    public bool CanOpen => _surfaces[0].Modules is IModules;

    /// <summary>A new world: every module builds its slice from <paramref name="layout"/>.</summary>
    public static World Create(uint seed, Layout layout, IReadOnlyList<Module> modules)
    {
        foreach (var m in modules) m.Create(layout);
        var world = new World(modules, new RngState { S = seed }, 0, layout.Kind);
        foreach (var m in modules) m.Rebuild();
        return world;
    }

    /// <summary>Restores a world from <see cref="Save"/> into fresh <paramref name="modules"/>, the ship's, which make every other surface's.</summary>
    public static World Load(string text, IReadOnlyList<Module> modules)
    {
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        int format = root.GetProperty("format").GetInt32();
        if (format != SaveFormat)
            throw new InvalidDataException($"A save of format {format} cannot be loaded by format {SaveFormat}.");
        var rng = new RngState { S = root.GetProperty("rng").GetUInt32() };
        var world = new World(modules, rng, root.GetProperty("tick").GetInt32(), root.TryGetProperty("kind", out var kind) ? kind.GetString()! : Surface.Ship);
        // A field this build does not know is a newer build's: refused, never dropped, so an added field needs no bump.
        var json = new JsonSerializerOptions(world.Json) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        Restore(root.GetProperty("slices"), modules, json, null);
        if (root.TryGetProperty("surfaces", out var surfaces))
        {
            var ship = modules as IModules ?? throw new InvalidDataException("The save holds surfaces, and these modules cannot make any.");
            foreach (var saved in surfaces.EnumerateArray())
            {
                var surface = new Surface(saved.GetProperty("id").GetInt32(), saved.GetProperty("kind").GetString()!, saved.GetProperty("seed").GetUInt32());
                if (surface.Id != world._surfaces.Count) throw new InvalidDataException($"Surface {surface.Id} saved out of order.");
                var more = ship.Another();
                world._surfaces.Add(world.Attach(surface, more));
                // A grown surface saves what changed since its ground grew: grown again here, from its seed (0077).
                var ground = Content.Surfaces.Get(surface.Kind) is Content.PlanetType type ? Terrain.Generate(type, surface.Seed) : null;
                Restore(saved.GetProperty("slices"), more, json, ground);
            }
            foreach (var at in root.GetProperty("where").EnumerateArray())
                world._where[at.GetProperty("peer").GetInt32()] = at.GetProperty("surface").GetInt32();
        }
        foreach (var s in world._surfaces)
            foreach (var m in s.Modules) m.Rebuild();
        return world;
    }

    private static void Restore(JsonElement saved, IReadOnlyList<Module> modules, JsonSerializerOptions json, Layout? ground)
    {
        var names = saved.EnumerateObject().Select(p => p.Name).ToList();
        var ids = modules.Select(m => m.Id).ToList();
        if (!names.SequenceEqual(ids))
            throw new InvalidDataException($"The save holds [{string.Join(", ", names)}], not [{string.Join(", ", ids)}].");
        foreach (var m in modules)
        {
            try
            {
                m.Restore(saved.GetProperty(m.Id), json, ground);
            }
            catch (JsonException e)
            {
                throw new InvalidDataException($"The save's {m.Id} holds what this build cannot read, perhaps a newer build's: {e.Message}", e);
            }
        }
    }

    /// <summary>Applies the commands stamped for this tick, runs every phase on every surface, advances the tick.</summary>
    public IReadOnlyList<Refused> Step(IReadOnlyList<Command> commands)
    {
        var refused = new List<Refused>();
        foreach (var c in commands)
        {
            int at = SurfaceOf(c.From);
            // Aimed at the cells of a surface its sender has left since: no module here may read them.
            if (c.Surface is int sent && sent != at)
            {
                refused.Add(new Refused(c, Elsewhere));
                continue;
            }
            // Peers run one build: an unknown command is our bug, not a player's mistake.
            if (!_own.TryGetValue(c.GetType(), out var h) && !_surfaces[at].Handlers.TryGetValue(c.GetType(), out h))
                throw new InvalidOperationException($"No module handles {c.GetType().Name}.");
            var reason = h.Validate(c);
            if (reason != null) refused.Add(new Refused(c, reason));
            else h.Apply(c);
        }
        // By index: a surface opened by this tick's commands runs this tick too.
        for (int i = 0; i < _surfaces.Count; i++)
            foreach (var phase in Enum.GetValues<Phase>())
                foreach (var m in _surfaces[i].Modules)
                    foreach (var (p, run) in m.Systems)
                        if (p == phase) run();
        // Opened by the ship, after every surface ran this tick: it runs from the next.
        if (_opening != null) Open(_opening, 0);
        _opening = null;
        foreach (var (peer, surface) in _sends) Move(peer, surface, null);
        _sends.Clear();
        Tick += 1;
        return refused;
    }

    private void Open(OpenSurface c) => Open(c.Kind, SurfaceOf(c.From));

    /// <summary>
    /// A surface of kind <paramref name="kindId"/>, made from its map and seeded from the world's random numbers, keeping what
    /// surface <paramref name="opener"/> keeps for a new one (its creative flag).
    /// </summary>
    private void Open(string kindId, int opener)
    {
        var kind = Content.Surfaces.Get(kindId);
        var ship = _surfaces[0].Modules as IModules ?? throw new InvalidOperationException("This world cannot open surfaces.");
        uint seed = _rngOver.Below(1UL << 32);
        var layout = kind is Content.PlanetType type ? Terrain.Generate(type, seed) : Layout.Parse(kind.Rows).Doubled();
        var modules = ship.Another();
        var surface = Attach(new Surface(_surfaces.Count, kind.Id, seed), modules);
        foreach (var m in modules) m.Create(layout);
        // What the opener's surface keeps for the new one: its twin of each module.
        var from = _surfaces[opener].Modules;
        foreach (var m in modules)
            if (from.FirstOrDefault(f => f.Id == m.Id) is { } twin) m.Inherit(twin);
        foreach (var m in modules) m.Rebuild();
        _surfaces.Add(surface);
    }

    private string? TransferProblem(Transfer c)
    {
        if (c.To < 0 || c.To >= _surfaces.Count) return NoSurface;
        return c.To == SurfaceOf(c.From) ? There : null;
    }

    /// <summary>
    /// Moves <paramref name="peer"/> to <paramref name="to"/>, at <paramref name="cell"/> or its spawn: what each module
    /// carrying a peer's own holds of it leaves its surface and arrives at its twin, in run order.
    /// </summary>
    private void Move(int peer, int to, int? cell)
    {
        int from = SurfaceOf(peer);
        if (from == to) return;
        var there = _surfaces[to].Modules;
        foreach (var m in _surfaces[from].Modules)
        {
            if (m is not ICarries leaving) continue;
            // Every surface runs the same modules: a twin missing is our bug.
            var twin = there.FirstOrDefault(t => t.Id == m.Id) as ICarries
                ?? throw new InvalidOperationException($"Surface {to} has no {m.Id} to carry peer {peer}'s into.");
            twin.Arrive(peer, leaving.Depart(peer), cell);
        }
        if (to == 0) _where.Remove(peer);
        else _where[peer] = to;
    }

    /// <summary>
    /// Draws a command of the world's own, as peer <paramref name="from"/> would send it, for the desync and save
    /// suites: now and then a strip or a small planet opened, while there are few, else a transfer, to a surface there is or the one
    /// past the last, so both refusals are drawn, at a cell or the spawn.
    /// </summary>
    public Command Fuzz(Rng rng, int from)
    {
        if (CanOpen && _surfaces.Count < FuzzedSurfaces && rng.Below(3) == 0)
            return new OpenSurface((rng.Below(2) == 0 ? Content.Surfaces.TestStrip : Content.PlanetTypes.Small).Id) { From = from };
        int to = (int)rng.Below((uint)_surfaces.Count + 1);
        int? cell = rng.Below(3) == 0 ? (int)rng.Below(400) : null;
        return new Transfer(to, cell) { From = from };
    }

    private Running Attach(Surface surface, IReadOnlyList<Module> modules)
    {
        var where = new Where(this, surface.Id, Content.Surfaces.Get(surface.Kind));
        foreach (var m in modules)
        {
            m.Rng = _rngOver;
            m.Where = where;
        }
        return new Running(surface, modules);
    }

    /// <summary>The world as JSON: the same world always gives the same text in one build.</summary>
    public string Save()
    {
        using var stream = new MemoryStream();
        Write(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// A hash of what a save holds (FNV-1a over its bytes), compared between peers: hashed as it is written,
    /// never held, so a hash a second allocates no copy of the world.
    /// </summary>
    public ulong Hash()
    {
        var hash = new HashingWriter();
        using (var w = new Utf8JsonWriter(hash)) Write(w);
        return hash.Hash;
    }

    private void Write(Stream stream)
    {
        using var w = new Utf8JsonWriter(stream);
        Write(w);
    }

    private void Write(Utf8JsonWriter w)
    {
        w.WriteStartObject();
        w.WriteNumber("format", SaveFormat);
        w.WriteNumber("tick", Tick);
        w.WriteNumber("rng", _rng.S);
        // The ship's grid under another kind's rules: a test's or a dev world's drawn ground; the ship's own, unsaid.
        if (_surfaces[0].Surface.Kind != Surface.Ship) w.WriteString("kind", _surfaces[0].Surface.Kind);
        // The ship's slices, as before surfaces; the others, and who is on them, only when there are any (0076).
        WriteSlices(w, _surfaces[0].Modules);
        if (_surfaces.Count > 1)
        {
            w.WriteStartArray("surfaces");
            foreach (var s in _surfaces.Skip(1))
            {
                w.WriteStartObject();
                w.WriteNumber("id", s.Surface.Id);
                w.WriteString("kind", s.Surface.Kind);
                w.WriteNumber("seed", s.Surface.Seed);
                WriteSlices(w, s.Modules);
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteStartArray("where");
            foreach (var (peer, surface) in _where)
            {
                w.WriteStartObject();
                w.WriteNumber("peer", peer);
                w.WriteNumber("surface", surface);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
        w.WriteEndObject();
    }

    private void WriteSlices(Utf8JsonWriter w, IReadOnlyList<Module> modules)
    {
        w.WriteStartObject("slices");
        foreach (var m in modules)
        {
            w.WritePropertyName(m.Id);
            var slice = m.Saved;
            JsonSerializer.Serialize(w, slice, slice.GetType(), Json);
        }
        w.WriteEndObject();
    }

    /// <summary>A surface as the world runs it: its modules and the handlers of their commands.</summary>
    private sealed class Running
    {
        public Surface Surface { get; }
        public IReadOnlyList<Module> Modules { get; }
        public Dictionary<Type, (Func<Command, string?> Validate, Action<Command> Apply)> Handlers { get; } = new();

        public Running(Surface surface, IReadOnlyList<Module> modules)
        {
            Surface = surface;
            Modules = modules;
            foreach (var m in modules)
                foreach (var c in m.Commands) Handlers.Add(c.Type, (c.Validate, c.Apply));
        }
    }

    /// <summary>The surfaces as the modules of surface <c>here</c> see them.</summary>
    private sealed class Where(World world, int here, Content.Surfaces.Kind kind) : ISurfaces
    {
        public int Here => here;
        public Content.Surfaces.Kind Kind => kind;

        public void Send(int peer, int surface)
        {
            // A planet opening this tick takes the next id, and is there by the time the sends are made.
            int count = world._surfaces.Count + (world._opening != null ? 1 : 0);
            if (surface < 0 || surface >= count) throw new ArgumentOutOfRangeException(nameof(surface));
            world._sends.Add((peer, surface));
        }

        public bool CanOpen => world.CanOpen;

        public bool Landed
        {
            get
            {
                for (int i = 1; i < world._surfaces.Count; i++)
                    if (Content.Surfaces.Get(world._surfaces[i].Surface.Kind) is Content.PlanetType) return true;
                return false;
            }
        }

        public int OpenPlanet()
        {
            for (int i = 1; i < world._surfaces.Count; i++)
                if (Content.Surfaces.Get(world._surfaces[i].Surface.Kind) is Content.PlanetType) return i;
            if (!world.CanOpen) throw new InvalidOperationException("This world cannot open a planet.");
            world._opening = Content.Landing.Planet;
            return world._surfaces.Count;
        }
    }

    /// <summary>Takes the bytes written to it into an FNV-1a hash, writing each batch into the same buffer.</summary>
    private sealed class HashingWriter : System.Buffers.IBufferWriter<byte>
    {
        private byte[] _buffer = new byte[16 * 1024];

        public ulong Hash { get; private set; } = 14695981039346656037;

        public void Advance(int count)
        {
            ulong h = Hash;
            foreach (byte b in _buffer.AsSpan(0, count))
                h = unchecked((h ^ b) * 1099511628211);
            Hash = h;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (sizeHint > _buffer.Length) _buffer = new byte[sizeHint];
            return _buffer;
        }

        public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;
    }
}
