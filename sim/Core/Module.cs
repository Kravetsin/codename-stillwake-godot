using System.Text.Json;

namespace Stillwake.Sim;

/// <summary>Systems run in these phases, in this order, after the tick's commands are applied.</summary>
public enum Phase { Construction, Derivation, Simulation, Events }

/// <summary>
/// A player's intent. Each module declares its own as records deriving from this one; <c>From</c> is
/// the peer that sent it, stamped by the host over whatever the client put there.
/// </summary>
public abstract record Command
{
    public int From { get; init; }
}

/// <summary>A command turned down on its tick: <c>Reason</c> is an i18n key the player reads.</summary>
public sealed record Refused(Command Command, string Reason);

/// <summary>
/// A sim module (ADR 0004): it owns one slice of the world, and others change it only through what it
/// chooses to expose. Its dependencies come in through its constructor as interfaces, so the
/// compiler, not a lint rule, keeps a module from reaching into another's state.
/// </summary>
public abstract class Module
{
    public abstract string Id { get; }

    internal readonly List<(Phase Phase, Action Run)> Systems = [];
    internal readonly List<(Type Type, string Name, Func<Command, string?> Validate, Action<Command> Apply)> Commands = [];
    internal Rng Rng = null!;

    internal abstract object Slice { get; }
    internal abstract void Create(Layout layout);
    internal abstract void Restore(JsonElement saved, JsonSerializerOptions options);

    /// <summary>The world's random numbers: the only randomness a module may use.</summary>
    protected Rng Random => Rng;

    /// <summary>
    /// SPIKE (planet): <paramref name="module"/> made from <paramref name="layout"/> outside any world, its slice never
    /// saved or hashed: derived state of the module that holds it (a site's ground, made from its seed).
    /// </summary>
    public static T Standalone<T>(T module, Layout layout) where T : Module
    {
        module.Create(layout);
        module.Rebuild();
        return module;
    }

    /// <summary>Rebuilds derived state from the rest of the slice: after a create and after a load.</summary>
    public virtual void Rebuild() { }

    protected void System(Phase phase, Action run) => Systems.Add((phase, run));

    /// <summary>Whether the module has commands: then it must draw them in <see cref="Fuzz"/>.</summary>
    public bool HasCommands => Commands.Count > 0;

    /// <summary>
    /// Draws a random command, as peer <paramref name="from"/> would send it, for the desync and save
    /// suites; refused ones too, so refusals are fuzzed. Null skips a turn. Reads the module's own state.
    /// </summary>
    public virtual Command? Fuzz(Rng rng, int from) => null;

    /// <summary>Whether this module draws its commands: one with commands and no generator fails the suites.</summary>
    public bool Fuzzes => GetType().GetMethod(nameof(Fuzz))!.DeclaringType != typeof(Module);

    /// <summary>
    /// Declares a command: <paramref name="validate"/> returns a refusal key or null, and runs on the
    /// command's tick; <paramref name="apply"/> changes this module's slice only.
    /// </summary>
    protected void Handles<T>(Func<T, string?> validate, Action<T> apply) where T : Command
    {
        var name = typeof(T).Name;
        Commands.Add((typeof(T), $"{Id}.{char.ToLowerInvariant(name[0])}{name[1..]}",
            c => validate((T)c), c => apply((T)c)));
    }
}

/// <summary>A module with its slice of type <typeparamref name="TState"/>: what saves and hashes hold.</summary>
public abstract class Module<TState> : Module where TState : class
{
    protected TState State { get; private set; } = null!;

    /// <summary>The starting slice, read from the map: a module reads only what concerns it.</summary>
    protected abstract TState Init(Layout layout);

    internal override object Slice => State;
    internal override void Create(Layout layout) => State = Init(layout);

    internal override void Restore(JsonElement saved, JsonSerializerOptions options) =>
        State = saved.Deserialize<TState>(options) ?? throw new InvalidDataException($"No {Id} in the save.");
}
