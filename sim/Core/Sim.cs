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
    public const int SaveFormat = 73;

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
    // Surfaces asked for this tick (ISurfaces.Open, OpenPlanet), opened after the phases in the order asked, before the sends: a
    // null seed drawn from the world's Rng as it opens. Never saved, empty between steps.
    private readonly List<(string Kind, uint? Seed, int Opener, IReadOnlyList<string>? Ores, IReadOnlyList<Content.WreckStep>? Wreck)> _opening = [];
    // Whether the tick's commands are applying: the one moment a module may open a surface and move a hero at once (Visit).
    private bool _applying;
    // The vessel a surface opening now was built with (vessel-wrecks), read by what fills it (ISurfaces.Vessel): one build a
    // jump, not one for the map and one to write it. Set only while it opens; never saved.
    private (int Surface, Built Vessel)? _built;
    private readonly RngState _rng;
    private readonly Rng _rngOver;
    // The world's own commands, which no surface's modules handle.
    private readonly Dictionary<Type, (Func<Command, string?> Validate, Action<Command> Apply)> _own = new();

    /// <summary>The next tick to run.</summary>
    public int Tick { get; private set; }

    /// <summary>Which world this is on every machine, for what a client keeps of it by itself; 0 for one made with none. The sim never reads it.</summary>
    public uint Id { get; }

    /// <summary>How saves are written.</summary>
    public JsonSerializerOptions Json { get; }

    /// <summary>Every command type there is, in order of its name: how the network tags commands.</summary>
    public IReadOnlyList<Type> CommandTypes { get; }

    private World(IReadOnlyList<Module> modules, RngState rng, int tick, string kind, uint id)
    {
        Id = id;
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
    public static World Create(uint seed, Layout layout, IReadOnlyList<Module> modules, uint id = 0)
    {
        foreach (var m in modules) m.Create(layout);
        var world = new World(modules, new RngState { S = seed }, 0, layout.Kind, id);
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
        var world = new World(modules, rng, root.GetProperty("tick").GetInt32(), root.TryGetProperty("kind", out var kind) ? kind.GetString()! : Surface.Ship, root.TryGetProperty("id", out var id) ? id.GetUInt32() : 0);
        // A field this build does not know is a newer build's: refused, never dropped, so an added field needs no bump.
        var json = new JsonSerializerOptions(world.Json) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        Restore(root.GetProperty("slices"), modules, json, null);
        if (root.TryGetProperty("surfaces", out var surfaces))
        {
            var ship = modules as IModules ?? throw new InvalidDataException("The save holds surfaces, and these modules cannot make any.");
            foreach (var saved in surfaces.EnumerateArray())
            {
                var surface = new Surface(saved.GetProperty("id").GetInt32(), saved.GetProperty("kind").GetString()!, saved.GetProperty("seed").GetUInt32())
                {
                    Ores = saved.TryGetProperty("ores", out var ores) ? [.. ores.EnumerateArray().Select(o => o.GetString()!)] : null,
                    Wreck = saved.TryGetProperty("wreck", out var wreck)
                        ? [.. wreck.EnumerateArray().Select(e => new Content.WreckStep(Enum.Parse<Content.WreckKind>(e.GetProperty("kind").GetString()!), e.GetProperty("old").GetBoolean()))]
                        : null,
                };
                if (surface.Id != world._surfaces.Count) throw new InvalidDataException($"Surface {surface.Id} saved out of order.");
                // A closed one keeps its id and nothing else (vessel-worlds).
                if (saved.TryGetProperty("closed", out var closed) && closed.GetBoolean())
                {
                    world._surfaces.Add(new Running(surface with { Closed = true }, []));
                    continue;
                }
                var more = ship.Another();
                world._surfaces.Add(world.Attach(surface, more));
                // A grown surface saves what changed since its ground grew: grown again here, from its seed (0077).
                var ground = Content.Surfaces.Get(surface.Kind) is Content.PlanetType type ? Terrain.Generate(type, surface.Seed, surface.Ores) : null;
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
        _applying = true;
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
        _applying = false;
        // By index: a surface opened by this tick's commands runs this tick too; a closed one has no modules.
        for (int i = 0; i < _surfaces.Count; i++)
            foreach (var phase in Enum.GetValues<Phase>())
                foreach (var m in _surfaces[i].Modules)
                    foreach (var (p, run) in m.Systems)
                        if (p == phase) run();
        // After every surface ran this tick, in the order asked: each runs from the next.
        foreach (var (kind, seed, opener, ores, wreck) in _opening) Open(kind, opener, seed, ores, wreck);
        _opening.Clear();
        foreach (var (peer, surface) in _sends) Move(peer, surface, null);
        _sends.Clear();
        Close();
        Tick += 1;
        return refused;
    }

    /// <summary>
    /// Closes, in id order, every open surface of a transient kind no peer stands on (vessel-worlds): once the tick's
    /// transfers, farewells and sends are made. What outlives it (<see cref="IOutlives"/>) goes first to its module's twin on
    /// the ship, in run order. Its slot stays, empty, so ids keep their meaning and are never reused.
    /// </summary>
    private void Close()
    {
        for (int i = 1; i < _surfaces.Count; i++)
        {
            var s = _surfaces[i].Surface;
            if (s.Closed || !Content.Surfaces.Get(s.Kind).Transient || _where.ContainsValue(i)) continue;
            foreach (var m in _surfaces[i].Modules)
            {
                if (m is not IOutlives closing || closing.Closing() is not { } left) continue;
                var twin = _surfaces[0].Modules.FirstOrDefault(t => t.Id == m.Id) as IOutlives
                    ?? throw new InvalidOperationException($"The ship has no {m.Id} to take what outlives surface {i}.");
                twin.Outlived(left);
            }
            _surfaces[i] = new Running(s with { Closed = true }, []);
        }
    }

    private void Open(OpenSurface c) => Open(c.Kind, SurfaceOf(c.From), null);

    /// <summary>Whether surface <paramref name="i"/> is a planet still open: not one left (solar-system).</summary>
    private bool OpenPlanetAt(int i) => !_surfaces[i].Surface.Closed && Content.Surfaces.Get(_surfaces[i].Surface.Kind) is Content.PlanetType;

    /// <summary>
    /// A surface of kind <paramref name="kindId"/>, made from its map, grown or described from <paramref name="seed"/> (drawn from
    /// the world's random numbers when none is given), keeping what surface <paramref name="opener"/> keeps for a new one (its
    /// creative flag), then filled (<see cref="Module.Opened"/>). Returns its id.
    /// </summary>
    /// <param name="ores">A planet's ores (<see cref="Surface.Ores"/>), null for all.</param>
    /// <param name="wreck">A vessel's forced chain (<see cref="Surface.Wreck"/>), null for its seed's own.</param>
    private int Open(string kindId, int opener, uint? given, IReadOnlyList<string>? ores = null, IReadOnlyList<Content.WreckStep>? wreck = null)
    {
        var kind = Content.Surfaces.Get(kindId);
        var ship = _surfaces[0].Modules as IModules ?? throw new InvalidOperationException("This world cannot open surfaces.");
        uint seed = given ?? _rngOver.Below(1UL << 32);
        var built = kind is Content.VesselKind vessel ? Shipyard.Build(Content.Vessels.Class(vessel.Class)!, seed, wreck) : null;
        var layout = kind switch
        {
            // Not calm: a planet's storms bring their lightning (docs/plans/surface-storms.md); it has no meteors or flares to start.
            Content.PlanetType type => Terrain.Generate(type, seed, ores) with { Meteors = true },
            Content.VesselKind => Shipyard.Space(built!),
            _ => Layout.Parse(kind.Rows).Doubled(),
        };
        var modules = ship.Another();
        var surface = Attach(new Surface(_surfaces.Count, kind.Id, seed) { Ores = kind is Content.PlanetType ? ores : null, Wreck = kind is Content.VesselKind ? wreck : null }, modules);
        foreach (var m in modules) m.Create(layout);
        // What the opener's surface keeps for the new one: its twin of each module.
        var from = _surfaces[opener].Modules;
        foreach (var m in modules)
            if (from.FirstOrDefault(f => f.Id == m.Id) is { } twin) m.Inherit(twin);
        foreach (var m in modules) m.Rebuild();
        _surfaces.Add(surface);
        // Once every module stands: what fills it writes through the others' operations, as a command would.
        _built = built is null ? null : (surface.Surface.Id, built);
        foreach (var m in modules) m.Opened();
        _built = null;
        return surface.Surface.Id;
    }

    private string? TransferProblem(Transfer c)
    {
        if (c.To < 0 || c.To >= _surfaces.Count || _surfaces[c.To].Surface.Closed) return NoSurface;
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
        // Open ones only: closed vessel worlds take no modules, and the fuzz goes on opening planets past them.
        if (CanOpen && _surfaces.Count(s => !s.Surface.Closed) < FuzzedSurfaces && rng.Below(3) == 0)
            return new OpenSurface(rng.Below(3) switch { 0 => Content.Surfaces.TestStrip.Id, 1 => Content.PlanetTypes.Small.Id, _ => Content.PlanetTypes.SmallSpire.Id }) { From = from };
        int to = (int)rng.Below((uint)_surfaces.Count + 1);
        int? cell = rng.Below(3) == 0 ? (int)rng.Below(400) : null;
        return new Transfer(to, cell) { From = from };
    }

    private Running Attach(Surface surface, IReadOnlyList<Module> modules)
    {
        var where = new Where(this, surface.Id, Content.Surfaces.Get(surface.Kind), surface.Seed, surface.Wreck);
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
        // Unsaid while 0: an old save or a dev world writes and hashes as it did.
        if (Id != 0) w.WriteNumber("id", Id);
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
                if (s.Surface.Ores is { } ores)
                {
                    w.WriteStartArray("ores");
                    foreach (var ore in ores) w.WriteStringValue(ore);
                    w.WriteEndArray();
                }
                if (s.Surface.Wreck is { } wreck)
                {
                    w.WriteStartArray("wreck");
                    foreach (var step in wreck)
                    {
                        w.WriteStartObject();
                        w.WriteString("kind", step.Kind.ToString());
                        w.WriteBoolean("old", step.Old);
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                }
                if (s.Surface.Closed) w.WriteBoolean("closed", true);
                else WriteSlices(w, s.Modules);
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
    private sealed class Where(World world, int here, Content.Surfaces.Kind kind, uint seed, IReadOnlyList<Content.WreckStep>? wreck) : ISurfaces
    {
        public int Here => here;
        public Content.Surfaces.Kind Kind => kind;
        public uint Seed => seed;
        public IReadOnlyList<Content.WreckStep>? Wreck => wreck;
        public Built? Vessel => world._built is { } b && b.Surface == here ? b.Vessel : null;

        public void Send(int peer, int surface)
        {
            // A surface opening this tick takes its id, and is there by the time the sends are made.
            int count = world._surfaces.Count + world._opening.Count;
            if (surface < 0 || surface >= count) throw new ArgumentOutOfRangeException(nameof(surface));
            if (surface < world._surfaces.Count && world._surfaces[surface].Surface.Closed)
                throw new InvalidOperationException($"Surface {surface} is closed: nothing is sent there.");
            world._sends.Add((peer, surface));
        }

        public int Visit(int peer, string kindId, uint seed, IReadOnlyList<Content.WreckStep>? wreck = null)
        {
            if (!world._applying) throw new InvalidOperationException("A visit is made only as a command applies.");
            int id = world.Open(kindId, world.SurfaceOf(peer), seed, wreck: wreck);
            world.Move(peer, id, null);
            return id;
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

        public bool Away => world._where.Count > 0;

        public void LeavePlanet()
        {
            if (!world._applying) throw new InvalidOperationException("A planet is left only as a command applies.");
            for (int i = 1; i < world._surfaces.Count; i++)
            {
                if (!world.OpenPlanetAt(i)) continue;
                if (world._where.ContainsValue(i) || world._sends.Exists(s => s.Surface == i))
                    throw new InvalidOperationException($"Planet {i} is left with a hero on it or on its way there.");
                world._surfaces[i] = new Running(world._surfaces[i].Surface with { Closed = true }, []);
            }
        }

        public int OpenPlanet(string kind, IReadOnlyList<string>? ores)
        {
            for (int i = 1; i < world._surfaces.Count; i++)
                if (world.OpenPlanetAt(i)) return i;
            int asked = world._opening.FindIndex(o => Content.Surfaces.Get(o.Kind) is Content.PlanetType);
            if (asked >= 0) return world._surfaces.Count + asked;
            // Opened by the ship, as before any other surface could be asked for.
            return Ask(kind, null, 0, ores);
        }

        public int Open(string kind, uint seed, IReadOnlyList<Content.WreckStep>? wreck = null) => Ask(kind, seed, here, wreck: wreck);

        /// <summary>A surface of <paramref name="kind"/> asked for, opened after the phases by <paramref name="opener"/>; returns the id it will take.</summary>
        private int Ask(string kind, uint? seed, int opener, IReadOnlyList<string>? ores = null, IReadOnlyList<Content.WreckStep>? wreck = null)
        {
            if (!world.CanOpen) throw new InvalidOperationException("This world cannot open a surface.");
            world._opening.Add((kind, seed, opener, ores, wreck));
            return world._surfaces.Count + world._opening.Count - 1;
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
