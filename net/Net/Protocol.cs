using System.Runtime.CompilerServices;
using Stillwake.Sim;

namespace Stillwake.Net;

/// <summary>
/// What a peer sends the host. Commands travel encoded by the sender's world
/// (<see cref="Wire.Encode"/>): the host runs no world and never reads them.
/// </summary>
public abstract record ToHost;

/// <summary>
/// A peer joining says who it is, before anything else (0032): the player's key, which the host seats
/// at the same peer id in a world every time, so its hero comes back with what it carried.
/// </summary>
public sealed record HelloMessage(string Player) : ToHost;

/// <summary>A peer asks the host to apply a command.</summary>
public sealed record CommandMessage(byte[] Command) : ToHost;

/// <summary>
/// A peer's hero, as it moves on that peer: not a command, since heroes live outside the simulation
/// (0002). The host passes it to every other peer, stamped with the sender, and keeps nothing of it.
/// </summary>
public sealed record HeroMessage(byte[] State) : ToHost;

/// <summary>A peer's hash of its world once it has run up to <c>Tick</c>, every <see cref="Protocol.HashEvery"/> ticks.</summary>
public sealed record HashMessage(int Tick, ulong Hash) : ToHost;

/// <summary>What the host sends a peer.</summary>
public abstract record ToClient;

/// <summary>The host will not let a joining peer in, and why, in words for the player: its player is aboard already.</summary>
public sealed record RefusedMessage(string Reason) : ToClient;

/// <summary>A command as the host released it: encoded as its sender wrote it, stamped with who sent it.</summary>
public sealed record Stamped(int From, byte[] Command);

/// <summary>The host releases a tick: every peer applies these commands on it, in this order.</summary>
public sealed record TickMessage(int Tick, IReadOnlyList<Stamped> Commands) : ToClient;

/// <summary>
/// The host lets a peer in mid-session: its id, the host's build, and the world saved on the tick the
/// host releases next. Every tick after it follows; commands not yet released ride in the next one.
/// </summary>
public sealed record WelcomeMessage(int Id, string Build, string Save) : ToClient;

/// <summary>Another peer's hero, as the host passed it on; net carries the state without reading it.</summary>
public sealed record PeerHeroMessage(int From, byte[] State) : ToClient;

/// <summary>
/// The host's world for a peer whose hash differed, saved on the tick the host releases next: the
/// peer reports how it differed, then plays on from it.
/// </summary>
public sealed record ResyncMessage(string Save) : ToClient;

/// <summary>How messages are written on the wire: packed (<see cref="Packing"/>), each tagged by its place below.</summary>
public static class Protocol
{
    /// <summary>How often peers hash their worlds: a hash of the reference world is not free.</summary>
    public const int HashEvery = 20;

    private static readonly Packing.Union ToHosts =
        new([typeof(HelloMessage), typeof(CommandMessage), typeof(HeroMessage), typeof(HashMessage)]);

    private static readonly Packing.Union ToClients =
        new([typeof(TickMessage), typeof(WelcomeMessage), typeof(PeerHeroMessage), typeof(ResyncMessage), typeof(RefusedMessage)]);

    public static byte[] Write<T>(T message) => Packing.Pack(message!, UnionOf(typeof(T)));

    /// <summary>The message <see cref="Write"/> wrote; anything else throws <see cref="InvalidDataException"/>.</summary>
    public static T Read<T>(ReadOnlySpan<byte> bytes) => (T)Packing.Unpack(bytes, UnionOf(typeof(T)));

    private static Packing.Union UnionOf(Type type) =>
        typeof(ToHost).IsAssignableFrom(type) ? ToHosts
        : typeof(ToClient).IsAssignableFrom(type) ? ToClients
        : throw new ArgumentException($"{type.Name} is no message.");
}

/// <summary>Commands as the network carries them: packed, tagged by their place among the world's command types.</summary>
public static class Wire
{
    private static readonly ConditionalWeakTable<World, Packing.Union> Unions = new();

    public static byte[] Encode(this World world, Command command) => Packing.Pack(command, UnionOf(world));

    /// <summary>The command <see cref="Encode"/> wrote; garbled bytes throw <see cref="InvalidDataException"/>.</summary>
    public static Command Decode(this World world, ReadOnlySpan<byte> bytes) => (Command)Packing.Unpack(bytes, UnionOf(world));

    private static Packing.Union UnionOf(World world) => Unions.GetValue(world, w => new Packing.Union(w.CommandTypes));
}
