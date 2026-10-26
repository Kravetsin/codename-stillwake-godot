using System.Text;
using System.Text.Json.Nodes;
using Stillwake.Game.Net;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The playtest relay's frames, as packages/net/src/relay.ts of the TypeScript game reads and writes them.</summary>
public class RelayFramesTests
{
    private static JsonNode Parsed(string frame) => JsonNode.Parse(frame)!;

    [Fact]
    public void WritesTheFramesTheRelayReadsByItsEventsAndFields()
    {
        Assert.Equal("stillwake:host", (string)Parsed(RelayFrames.HostFrame("ship"))["event"]!);
        Assert.Equal("ship", (string)Parsed(RelayFrames.HostFrame("ship"))["data"]!["name"]!);
        Assert.Null(Parsed(RelayFrames.HostFrame(null))["data"]!["name"]);
        var join = Parsed(RelayFrames.JoinFrame("ship"));
        Assert.Equal(("stillwake:join", "ship", 1), ((string)join["event"]!, (string)join["data"]!["session"]!, (int)join["data"]!["token"]!));
        var toPeer = Parsed(RelayFrames.ToPeerFrame(3, "{\"a\":1}"));
        Assert.Equal(("stillwake:to-peer", 3, "{\"a\":1}"), ((string)toPeer["event"]!, (int)toPeer["data"]!["peer"]!, (string)toPeer["data"]!["message"]!));
        Assert.Equal("{\"a\":1}", (string)Parsed(RelayFrames.ToHostFrame("{\"a\":1}"))["data"]!["message"]!);
    }

    [Fact]
    public void ReadsWhatTheRelaySendsAndNothingFromAFrameThatIsNotOne()
    {
        Assert.Equal(new RelayFrames.Frame("stillwake:hosted", null, null, "1"),
            RelayFrames.Read("{\"event\":\"stillwake:hosted\",\"data\":{\"session\":\"1\"}}"));
        Assert.Equal(new RelayFrames.Frame("stillwake:from-peer", 2, "{\"x\":true}", null),
            RelayFrames.Read("{\"event\":\"stillwake:from-peer\",\"data\":{\"peer\":2,\"message\":\"{\\\"x\\\":true}\"}}"));
        Assert.Equal(new RelayFrames.Frame("stillwake:host-left", null, null, null), RelayFrames.Read("{\"event\":\"stillwake:host-left\"}"));
        Assert.Null(RelayFrames.Read("not json"));
        Assert.Null(RelayFrames.Read("{\"data\":{}}"));
    }

    [Fact]
    public void NamesASessionAsTheRelayTakesItWhateverCaseOrSpacesWereTyped()
    {
        Assert.Equal("kravets", RelayFrames.SessionName(" Kravets "));
        Assert.Equal("our-ship-2", RelayFrames.SessionName("Our Ship_2!"));
        Assert.Equal(24, RelayFrames.SessionName(new string('a', 40)).Length);
        Assert.Equal("kravets", (string)Parsed(RelayFrames.JoinFrame("Kravets"))["data"]!["session"]!);
        Assert.Equal("kravets", (string)Parsed(RelayFrames.HostFrame("Kravets"))["data"]!["name"]!);
    }

    [Fact]
    public void CarriesAFramesMessagesBatchedAsTextInOrderAndReadsNothingFromTextItDidNotWrite()
    {
        byte[][] messages = [[1, 2, 3], [], [0xFF]];
        string frame = RelayFrames.ToPeerFrame(2, RelayFrames.Batch(messages));
        var read = RelayFrames.Unbatch(RelayFrames.Read(frame)!.Value.Message!);
        Assert.Equal(messages, read);
        Assert.Null(RelayFrames.Unbatch("{\"kind\":\"tick\"}"));
        Assert.Null(RelayFrames.Unbatch("AQID"));
    }

    [Fact]
    public void AsksWithThePasswordUnderAnyUserAsTheServerChecksIt()
    {
        string header = RelayFrames.Authorization("secret");
        Assert.StartsWith("Authorization: Basic ", header);
        Assert.Equal(":secret", Encoding.UTF8.GetString(System.Convert.FromBase64String(header["Authorization: Basic ".Length..])));
    }
}
