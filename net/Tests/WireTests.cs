using System.Text.Json;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Net.Tests;

/// <summary>Commands and messages as the network carries them, packed (<see cref="Packing"/>), and back.</summary>
public class WireTests
{
    private static readonly World Game = World.Create(1, PoweredWreck.Layout, new Registry().All);

    private static string Json(object value) => JsonSerializer.Serialize(value, value.GetType(), Game.Json);

    [Fact]
    public void ACommandCarriesItsSenderAndSurfaceAndAnUnstampedOneNone()
    {
        Assert.Equal(new SetDoor(12, true) { From = 2, Surface = 3 }, Game.Decode(Game.Encode(new SetDoor(12, true) { From = 2, Surface = 3 })));
        Assert.Equal(new SetDoor(12, false), Game.Decode(Game.Encode(new SetDoor(12, false))));
        Assert.Equal(new Transfer(1) { From = 1 }, Game.Decode(Game.Encode(new Transfer(1) { From = 1 })));
        Assert.Equal(new Transfer(1, -5), Game.Decode(Game.Encode(new Transfer(1, -5))));
        Assert.Equal(new OpenSurface(Content.Surfaces.TestStrip.Id), Game.Decode(Game.Encode(new OpenSurface(Content.Surfaces.TestStrip.Id))));
    }

    // Its tag, the cell in three bytes, whether it opens, its sender, and its surface stamped: there, then which.
    [Fact]
    public void ACommandIsAFewBytes() => Assert.Equal(8, Game.Encode(new SetDoor(1_048_575, true) { From = 7, Surface = 0 }).Length);

    [Fact]
    public void EveryCommandTypeTheModulesDrawComesBackAsItWent()
    {
        var drawn = new HashSet<Type>();
        foreach (uint seed in new[] { 1u, 2u, 3u })
        {
            var rng = new Rng(new RngState { S = seed });
            for (int i = 0; i < 400; i++)
                foreach (var module in Game.ModulesOn(0).Where(m => m.Fuzzes))
                {
                    var command = (i % 5 == 0 ? Game.Fuzz(rng, i % 4) : module.Fuzz(rng, i % 4)) is { } c ? c with { From = i % 4, Surface = i % 3 == 0 ? null : i % 2 } : null;
                    if (command == null) continue;
                    drawn.Add(command.GetType());
                    Assert.Equal(Json(command), Json(Game.Decode(Game.Encode(command))));
                }
        }
        Assert.Equal(Game.CommandTypes.Select(t => t.Name).Order(), drawn.Select(t => t.Name).Order());
    }

    public sealed record Hidden(int A)
    {
        public int B { get; private set; }
    }

    public sealed record TwoWays
    {
        public TwoWays(int a) => A = a;
        public TwoWays(string b) => A = b.Length;
        public int A { get; init; }
    }

    [Fact]
    public void ARecordTheWireCouldNotCarryWholeOrReadBackOneWayIsRefusedAtOnce()
    {
        Assert.Throws<NotSupportedException>(() => Packing.Pack(new Hidden(1)));
        Assert.Throws<NotSupportedException>(() => Packing.Pack(new TwoWays(1)));
        Assert.Throws<NotSupportedException>(() => Packing.Pack(new[] { 1, 2 }));
    }

    [Fact]
    public void PeersPackingCommandsAlikeShareAFingerprintAndOthersDoNot()
    {
        Assert.Equal(Wire.Fingerprint(Game.CommandTypes), Wire.Fingerprint(World.CommandTypesOf(new Registry().All)));
        Assert.NotEqual(Wire.Fingerprint([typeof(SetDoor), typeof(Transfer)]), Wire.Fingerprint([typeof(Transfer), typeof(SetDoor)]));
        Assert.NotEqual(Wire.Fingerprint(Game.CommandTypes), Wire.Fingerprint(Game.CommandTypes.Skip(1).ToList()));
    }

    [Fact]
    public void CutOrGarbledBytesAreRefused()
    {
        var tick = Protocol.Write<ToClient>(new TickMessage(5, [new Stamped(1, [1, 2]), new Stamped(2, [3])]));
        for (int cut = 0; cut < tick.Length; cut++) Assert.Throws<InvalidDataException>(() => Protocol.Read<ToClient>(tick[..cut]));
        // Whether the first stamped command is there: neither.
        Assert.Equal([0, 10, 3, 1], tick[..4]);
        tick[3] = 2;
        Assert.Throws<InvalidDataException>(() => Protocol.Read<ToClient>(tick));
        // Counts beyond the bytes left, the first wrapping round to "none" if let through; a number past 32 bits.
        Assert.Throws<InvalidDataException>(() => Protocol.Read<ToClient>([0, 0, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F]));
        Assert.Throws<InvalidDataException>(() => Protocol.Read<ToClient>([0, 0, 0xFF, 0xFF, 0xFF, 0x7F]));
        Assert.Throws<InvalidDataException>(() => Protocol.Read<ToClient>([0, 0x80, 0x80, 0x80, 0x80, 0x7F, 1]));
        var whole = Game.Encode(new Transfer(1, 5) { From = 1, Surface = 0 });
        for (int cut = 0; cut < whole.Length; cut++) Assert.Throws<InvalidDataException>(() => Game.Decode(whole[..cut]));
        Assert.Throws<InvalidDataException>(() => Game.Decode([.. whole, 0]));
        Assert.Throws<InvalidDataException>(() => Game.Decode([0xFF, 0x7F]));
        Assert.Throws<InvalidDataException>(() => Protocol.Read<ToClient>([0x09]));
        // A bool that is neither: the tag, the cell, then whether it opens.
        var door = Game.Encode(new SetDoor(1, true));
        door[2] = 2;
        Assert.Throws<InvalidDataException>(() => Game.Decode(door));
        // A message of a build that wrote JSON.
        Assert.Throws<InvalidDataException>(() => Protocol.Read<ToClient>("""{"kind":"tick","tick":1,"commands":[]}"""u8));
    }

    public static readonly TheoryData<ToHost> ToHosts = new()
    {
        new HelloMessage("player-1"),
        new CommandMessage([1, 2, 3]),
        new HeroMessage([]),
        new HashMessage(-4, 0xDEADBEEF_CAFEF00DUL),
    };

    public static readonly TheoryData<ToClient> ToClients = new()
    {
        new TickMessage(1_000_000, [new Stamped(0, [4]), new Stamped(3, [])]),
        new TickMessage(0, []),
        new WelcomeMessage(2, "build", "{\"save\":\"é\"}"),
        new PeerHeroMessage(5, [9, 9]),
        new ResyncMessage("{}"),
        new RefusedMessage("aboard"),
    };

    [Theory]
    [MemberData(nameof(ToHosts))]
    public void EveryMessageToTheHostComesBackAsItWent(ToHost message) =>
        Assert.Equal(JsonSerializer.Serialize<object>(message), JsonSerializer.Serialize<object>(Protocol.Read<ToHost>(Protocol.Write(message))));

    [Theory]
    [MemberData(nameof(ToClients))]
    public void EveryMessageToAPeerComesBackAsItWent(ToClient message) =>
        Assert.Equal(JsonSerializer.Serialize<object>(message), JsonSerializer.Serialize<object>(Protocol.Read<ToClient>(Protocol.Write(message))));
}
