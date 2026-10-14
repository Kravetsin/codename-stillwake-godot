using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stillwake.Net;

/// <summary>
/// What a peer sends the host. Commands travel encoded by the sender's world
/// (<see cref="Stillwake.Sim.World.Encode"/>): the host runs no world and never reads them.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(CommandMessage), "command")]
[JsonDerivedType(typeof(HeroMessage), "hero")]
[JsonDerivedType(typeof(HashMessage), "hash")]
public abstract record ToHost;

/// <summary>A peer asks the host to apply a command.</summary>
public sealed record CommandMessage(string Command) : ToHost;

/// <summary>
/// A peer's hero, as it moves on that peer: not a command, since heroes live outside the simulation
/// (0002). The host passes it to every other peer, stamped with the sender, and keeps nothing of it.
/// </summary>
public sealed record HeroMessage(string State) : ToHost;

/// <summary>A peer's hash of its world once it has run up to <c>Tick</c>, every <see cref="Protocol.HashEvery"/> ticks.</summary>
public sealed record HashMessage(int Tick, ulong Hash) : ToHost;

/// <summary>What the host sends a peer.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(TickMessage), "tick")]
[JsonDerivedType(typeof(WelcomeMessage), "welcome")]
[JsonDerivedType(typeof(PeerHeroMessage), "hero")]
[JsonDerivedType(typeof(ResyncMessage), "resync")]
public abstract record ToClient;

/// <summary>A command as the host released it: encoded as its sender wrote it, stamped with who sent it.</summary>
public sealed record Stamped(int From, string Command);

/// <summary>The host releases a tick: every peer applies these commands on it, in this order.</summary>
public sealed record TickMessage(int Tick, IReadOnlyList<Stamped> Commands) : ToClient;

/// <summary>
/// The host lets a peer in mid-session: its id, the host's build, and the world saved on the tick the
/// host releases next. Every tick after it follows; commands not yet released ride in the next one.
/// </summary>
public sealed record WelcomeMessage(int Id, string Build, string Save) : ToClient;

/// <summary>Another peer's hero, as the host passed it on; net carries the state without reading it.</summary>
public sealed record PeerHeroMessage(int From, string State) : ToClient;

/// <summary>
/// The host's world for a peer whose hash differed, saved on the tick the host releases next: the
/// peer reports how it differed, then plays on from it.
/// </summary>
public sealed record ResyncMessage(string Save) : ToClient;

/// <summary>How messages are written on the wire: JSON, as the TypeScript build wrote them.</summary>
public static class Protocol
{
    /// <summary>How often peers hash their worlds: a hash of the reference world is not free.</summary>
    public const int HashEvery = 20;

    public static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static byte[] Write<T>(T message) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, Json));

    public static T Read<T>(ReadOnlySpan<byte> bytes) =>
        JsonSerializer.Deserialize<T>(bytes, Json) ?? throw new InvalidDataException("An empty message.");
}
