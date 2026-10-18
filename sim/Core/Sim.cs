using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Stillwake.Sim;

/// <summary>
/// One world: its modules, in dependency order, each owning its slice. A world changes only through
/// <see cref="Step"/>; a loaded one gets a sim of its own (packages/sim/src/core/sim.ts).
/// </summary>
public sealed class World
{
    /// <summary>Bumped whenever saved data changes shape; a save of another format is refused.</summary>
    public const int SaveFormat = 17;

    /// <summary>How many ticks a second of game time holds: the host releases them at this pace.</summary>
    public const int TicksPerSecond = 20;

    private readonly IReadOnlyList<Module> _modules;
    private readonly Dictionary<Type, (Func<Command, string?> Validate, Action<Command> Apply)> _handlers = new();
    private readonly RngState _rng;

    /// <summary>The next tick to run.</summary>
    public int Tick { get; private set; }

    /// <summary>How commands and saves are written: every module's commands by their names.</summary>
    public JsonSerializerOptions Json { get; }

    private World(IReadOnlyList<Module> modules, RngState rng, int tick)
    {
        _modules = modules;
        _rng = rng;
        Tick = tick;
        var rngOver = new Rng(rng);
        var names = new List<JsonDerivedType>();
        foreach (var m in modules)
        {
            m.Rng = rngOver;
            foreach (var c in m.Commands)
            {
                _handlers.Add(c.Type, (c.Validate, c.Apply));
                names.Add(new JsonDerivedType(c.Type, c.Name));
            }
        }
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
            // Peers run one build: an unknown command is our bug, not a player's mistake.
            if (!_handlers.TryGetValue(c.GetType(), out var h))
                throw new InvalidOperationException($"No module handles {c.GetType().Name}.");
            var reason = h.Validate(c);
            if (reason != null) refused.Add(new Refused(c, reason));
            else h.Apply(c);
        }
        foreach (var phase in Enum.GetValues<Phase>())
            foreach (var m in _modules)
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
        w.WriteStartObject("slices");
        foreach (var m in _modules)
        {
            w.WritePropertyName(m.Id);
            JsonSerializer.Serialize(w, m.Slice, m.Slice.GetType(), Json);
        }
        w.WriteEndObject();
        w.WriteEndObject();
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
