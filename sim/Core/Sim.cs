using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Stillwake.Sim;

/// <summary>
/// One world: its surfaces (0076), each a set of modules in dependency order, each owning its slice. A world
/// changes only through <see cref="Step"/>; a loaded one gets a sim of its own (packages/sim/src/core/sim.ts).
/// </summary>
public sealed class World
{
    /// <summary>Bumped whenever saved data changes shape; a save of another format is refused.</summary>
    public const int SaveFormat = 19;

    /// <summary>How many ticks a second of game time holds: the host releases them at this pace.</summary>
    public const int TicksPerSecond = 20;

    /// <summary>Why a command stamped on one surface is refused once its sender stands on another.</summary>
    public const string Elsewhere = "world.elsewhere";

    // In id order: surface 0, the ship, is the modules the world was made with, and the only one so far.
    private readonly List<Running> _surfaces = [];
    // Peer → surface; a peer not listed is on the ship. Nothing moves a peer yet (0076, slice 3).
    private readonly SortedDictionary<int, int> _where = new();
    private readonly RngState _rng;

    /// <summary>The next tick to run.</summary>
    public int Tick { get; private set; }

    /// <summary>How commands and saves are written: every module's commands by their names.</summary>
    public JsonSerializerOptions Json { get; }

    private World(IReadOnlyList<Module> modules, RngState rng, int tick)
    {
        _rng = rng;
        Tick = tick;
        _surfaces.Add(new Running(new Surface(0, Surface.Ship, 0), modules));
        var rngOver = new Rng(rng);
        foreach (var m in modules) m.Rng = rngOver;
        // Every surface runs the ship's modules, so the ship's commands are every command there is.
        var names = modules.SelectMany(m => m.Commands).Select(c => new JsonDerivedType(c.Type, c.Name)).ToList();
        Json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = false,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    info =>
                    {
                        if (info.Type != typeof(Command)) return;
                        info.PolymorphismOptions = new JsonPolymorphismOptions { TypeDiscriminatorPropertyName = "type" };
                        foreach (var n in names) info.PolymorphismOptions.DerivedTypes.Add(n);
                    },
                },
            },
        };
    }

    /// <summary>The world's surfaces, in id order: the ship first.</summary>
    public IReadOnlyList<Surface> Surfaces => _surfaces.ConvertAll(s => s.Surface);

    /// <summary>The surface peer <paramref name="peer"/>'s hero is on: where its commands go.</summary>
    public int SurfaceOf(int peer) => _where.TryGetValue(peer, out int s) ? s : 0;

    /// <summary>A new world: every module builds its slice from <paramref name="layout"/>.</summary>
    public static World Create(uint seed, Layout layout, IReadOnlyList<Module> modules)
    {
        foreach (var m in modules) m.Create(layout);
        var world = new World(modules, new RngState { S = seed }, 0);
        foreach (var m in modules) m.Rebuild();
        return world;
    }

    /// <summary>Restores a world from <see cref="Save"/> into fresh <paramref name="modules"/>.</summary>
    public static World Load(string text, IReadOnlyList<Module> modules)
    {
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        int format = root.GetProperty("format").GetInt32();
        if (format != SaveFormat)
            throw new InvalidDataException($"A save of format {format} cannot be loaded by format {SaveFormat}.");
        var saved = root.GetProperty("slices");
        var names = saved.EnumerateObject().Select(p => p.Name).ToList();
        var ids = modules.Select(m => m.Id).ToList();
        if (!names.SequenceEqual(ids))
            throw new InvalidDataException($"The save holds [{string.Join(", ", names)}], not [{string.Join(", ", ids)}].");
        var rng = new RngState { S = root.GetProperty("rng").GetUInt32() };
        var world = new World(modules, rng, root.GetProperty("tick").GetInt32());
        foreach (var m in modules) m.Restore(saved.GetProperty(m.Id), world.Json);
        foreach (var m in modules) m.Rebuild();
        return world;
    }

    /// <summary>Applies the commands stamped for this tick, runs every phase, advances the tick.</summary>
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
            if (!_surfaces.Find(s => s.Surface.Id == at)!.Handlers.TryGetValue(c.GetType(), out var h))
                throw new InvalidOperationException($"No module handles {c.GetType().Name}.");
            var reason = h.Validate(c);
            if (reason != null) refused.Add(new Refused(c, reason));
            else h.Apply(c);
        }
        foreach (var surface in _surfaces)
            foreach (var phase in Enum.GetValues<Phase>())
                foreach (var m in surface.Modules)
                    foreach (var (p, run) in m.Systems)
                        if (p == phase) run();
        Tick += 1;
        return refused;
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
        // The ship's slices, as before surfaces: the others will go under a key of their own (0076).
        w.WriteStartObject("slices");
        foreach (var m in _surfaces[0].Modules)
        {
            w.WritePropertyName(m.Id);
            JsonSerializer.Serialize(w, m.Slice, m.Slice.GetType(), Json);
        }
        w.WriteEndObject();
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

    /// <summary>A command as the network carries it, and back.</summary>
    public string Encode(Command c) => JsonSerializer.Serialize(c, Json);
    public Command Decode(string text) =>
        JsonSerializer.Deserialize<Command>(text, Json) ?? throw new InvalidDataException("No command.");
}
