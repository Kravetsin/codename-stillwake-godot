using System.Text.Json;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Net.Tests;

/// <summary>Commands and messages as the network carries them, packed (<see cref="Packing"/>), and back.</summary>
public class WireTests
{
    private static readonly World Game = World.Create(1, Registry.Wreck, new Registry().All);

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

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(3u)]
    public void EveryCommandTheModulesDrawComesBackAsItWent(uint seed)
    {
        var rng = new Rng(new RngState { S = seed });
        var drawn = new HashSet<Type>();
        for (int i = 0; i < 400; i++)
            foreach (var module in Game.ModulesOn(0).Where(m => m.Fuzzes))
            {
                var command = (i % 5 == 0 ? Game.Fuzz(rng, i % 4) : module.Fuzz(rng, i % 4)) is { } c ? c with { From = i % 4, Surface = i % 3 == 0 ? null : i % 2 } : null;
                if (command == null) continue;
                drawn.Add(command.GetType());
                Assert.Equal(Json(command), Json(Game.Decode(Game.Encode(command))));
            }
        Assert.True(drawn.Count > Game.CommandTypes.Count / 2, $"drew {drawn.Count} of {Game.CommandTypes.Count} command types");
    }

    [Fact]
    public void CutOrGarbledBytesAreRefused()
    {
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
