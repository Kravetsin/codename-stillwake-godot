using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A spot pinged with a tap of the gesture key: one a player, lasting a while, heard by friends once per new one.</summary>
public class PingTests
{
    [Fact]
    public void APingShowsForItsTimeThenGoes()
    {
        var ping = new Ping();
        Assert.Null(ping.Sent);
        ping.Place(new Point(1_000, 2_000));
        ping.Step(Ping.LastsMs / 2);
        Assert.Equal(new PingSeen(new Point(1_000, 2_000), 1), ping.Sent);
        Assert.Equal(1, ping.Shown);
        ping.Step(Ping.LastsMs / 2);
        Assert.Null(ping.At);
        Assert.Null(ping.Sent);
        Assert.Equal(0, ping.Shown);
    }

    [Fact]
    public void ItFadesInAndOut()
    {
        var ping = new Ping();
        ping.Place(default);
        Assert.Equal(0, ping.Shown);
        ping.Step(Ping.InMs / 2);
        Assert.Equal(0.5, ping.Shown, 3);
        ping.Step(Ping.LastsMs - Ping.InMs / 2 - Ping.OutMs / 2);
        Assert.Equal(0.5, ping.Shown, 3);
    }

    [Fact]
    public void TheNextPingReplacesTheLastAndStartsAnew()
    {
        var ping = new Ping();
        ping.Place(new Point(1, 1));
        ping.Step(Ping.LastsMs - 10);
        ping.Place(new Point(2, 2));
        ping.Step(100);
        Assert.Equal(new PingSeen(new Point(2, 2), 2), ping.Sent);
    }

    [Fact]
    public void AFriendsPingShowsOncePerNewOne()
    {
        var heard = new Ping();
        heard.Hear(new PingSeen(new Point(5, 5), 3));
        heard.Step(Ping.LastsMs - 100);
        // Sent again in the next packets while it lasts there: the same one plays on, and ends here.
        heard.Hear(new PingSeen(new Point(5, 5), 3));
        heard.Step(200);
        Assert.Null(heard.At);
        heard.Hear(new PingSeen(new Point(5, 5), 3));
        Assert.Null(heard.At);
        // Nothing sent keeps nothing new; a new one shows from its start, even on the same spot.
        heard.Hear(null);
        heard.Hear(new PingSeen(new Point(5, 5), 4));
        Assert.Equal((new Point(5, 5), 0.0), (heard.At!.Value, heard.Ms));
    }

    [Fact]
    public void APingGoesOverTheWireWithItsHero()
    {
        var ping = new PingSeen(new Point(12_345, -6_789), 7);
        var heard = SentHero.Decode(new SentHero(new Seen(new Hero(0, 0, 0, 0, false, 1), Ping: ping), 10).Encode())!.Value.Seen;
        Assert.Equal(ping, heard.Ping);
    }

    [Fact]
    public void EveryPingIsMarkedInItsPlacersColour()
    {
        var list = Markers.Of(0, 20, [], pings: [(2, new Point(4_500, 3_000), Suits.All[1]), (1, new Point(0, 0), Suits.All[0])]);
        var ping = Assert.Single(list, m => m.Icon == MarkerIcon.Ping && m.Peer == 2);
        Assert.Equal((4.5, 3.0, Suits.All[1]), (ping.X, ping.Y, ping.Colour));
        Assert.Equal(2, list.Count(m => m.Icon == MarkerIcon.Ping));
    }
}
