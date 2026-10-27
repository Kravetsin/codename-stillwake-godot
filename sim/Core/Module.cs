using System.Text.Json;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// The surface its sender stood on when it sent it, stamped by the client (0076): one whose sender is elsewhere
    /// by its tick is refused. Null, as the host's farewells and tests send it, goes wherever the sender is.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Surface { get; init; }
}

/// <summary>A world's surfaces as a module sees them (0076).</summary>
public interface ISurfaces
{
    /// <summary>The surface this module runs on: 0 for the ship.</summary>
    int Here { get; }

    /// <summary>The rules of the kind of surface this module runs on: gravity, air, sky, debris, what may be built.</summary>
    Content.Surfaces.Kind Kind { get; }

    /// <summary>
    /// Moves peer <paramref name="peer"/>'s hero and what it carries to <paramref name="surface"/>, at its spawn, once
    /// every surface has run this tick, in the order asked: no module sees another surface change under it.
    /// </summary>
    void Send(int peer, int surface);

    /// <summary>Whether this world can open surfaces: one whose modules cannot make another set is the ship alone.</summary>
    bool CanOpen { get; }

    /// <summary>
    /// The planet a descent lands on, opened if need be — a change to the world, so called only by a system on its tick: the lowest-id
    /// open surface of a planet's kind, else one opening this tick, else one of <c>Content.Landing.Planet</c> opened once every
    /// surface has run, before the sends. Returns its id, to <see cref="Send"/> to.
    /// </summary>
    int OpenPlanet();
}

/// <summary>
/// A module holding what is a peer's own, which goes with its hero from surface to surface (0076): crew's hero,
/// items' inventory. Its twin on the surface left gives it up; this one takes it.
/// </summary>
public interface ICarries
{
    /// <summary>What <paramref name="peer"/> takes along, gone from here; null for nothing.</summary>
    object? Depart(int peer);

    /// <summary>Takes <paramref name="carried"/>, what the twin gave up, for <paramref name="peer"/> arriving at <paramref name="cell"/>, or the spawn with none.</summary>
    void Arrive(int peer, object? carried, int? cell);
}

/// <summary>
/// The modules of one surface in run order, which can make those of another surface of the same world (0076): a
/// registry's. A world made of modules that cannot is the ship alone.
/// </summary>
public interface IModules : IReadOnlyList<Module>
{
    /// <summary>A fresh set for another surface, sharing whatever is the world's rather than a surface's.</summary>
    IModules Another();
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
    internal ISurfaces Where = null!;

    internal abstract object Slice { get; }
    /// <summary>What saves and hashes hold of the slice: the slice, or less of it (<see cref="Module{TState}"/>).</summary>
    internal abstract object Saved { get; }
    internal abstract void Create(Layout layout);
    /// <summary>The slice from a save; <paramref name="ground"/> what its surface grew from (0077), null for one drawn as its map.</summary>
    internal abstract void Restore(JsonElement saved, JsonSerializerOptions options, Layout? ground);

    /// <summary>The world's random numbers: the only randomness a module may use.</summary>
    protected Rng Random => Rng;

    /// <summary>The world's surfaces as a module sees them (0076): which one it runs on, and moving a peer to another.</summary>
    protected ISurfaces Surfaces => Where;

    /// <summary>The rules of the kind of surface this module runs on (0076): the ship's until it is part of a world.</summary>
    public Content.Surfaces.Kind Rules => Where?.Kind ?? Content.Surfaces.Ship;

    /// <summary>Rebuilds derived state from the rest of the slice: after a create and after a load.</summary>
    public virtual void Rebuild() { }

    /// <summary>
    /// Takes what a new surface keeps of the surface it was opened from, <paramref name="from"/> being this module's
    /// twin there: after the create, before the rebuild. Creative mode is the one thing so far.
    /// </summary>
    protected internal virtual void Inherit(Module from) { }

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
    internal override object Saved => Save();
    internal override void Create(Layout layout) => State = Init(layout);

    internal override void Restore(JsonElement saved, JsonSerializerOptions options, Layout? ground) =>
        State = Load(saved, options, ground) ?? throw new InvalidDataException($"No {Id} in the save.");

    /// <summary>
    /// What a save and a hash hold of the slice: the slice itself, but for a module of a grown surface that saves only
    /// what changed since its ground grew (0077: the grid's edits).
    /// </summary>
    protected virtual object Save() => State;

    /// <summary>The slice from what <see cref="Save"/> wrote; <paramref name="ground"/> what its surface grew from (0077), null for a drawn one.</summary>
    protected virtual TState? Load(JsonElement saved, JsonSerializerOptions options, Layout? ground) => saved.Deserialize<TState>(options);
}
