using Xunit;

namespace Stillwake.Net.Tests;

/// <summary>packages/net/src/join.test.ts, ported.</summary>
public class JoinTests
{
    private readonly Definition _game = Definition.Board();

    private static Write Note(string text) => new(text);

    /// <summary>Joins over <paramref name="channel"/>: the client, or why it was refused.</summary>
    private (Client? Client, Exception? Refused) Join(IChannel<ToClient, ToHost> channel, string build)
    {
        Client? joined = null;
        Exception? refused = null;
        Client.Join(_game.Load, channel, build, c => joined = c, why => refused = why);
        return (joined, refused);
    }

    [Fact]
    public void StartsFromTheHostsWorldWithTheCommandsItHadNotReleasedYetAndKeepsStep()
    {
        var (host, own) = LocalSession.Create(_game.Create(1), "b1");
        own.Send(Note("before"));
        host.Advance();
        host.Advance();
        // Released but not yet run by the host player: the welcome brings the world up to date first.
        own.Send(Note("pending"));

        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        int id = host.Admit(toPeer);
        var joined = Join(fromHost, "b1").Client!;
        Assert.Equal(id, joined.Id);
        Assert.Equal(2, joined.World.Tick);

        joined.Send(Note("joined"));
        host.Advance();
        own.Update();
        joined.Update();
        Assert.Equal(["0:before", "2:pending", "2:joined"], _game.Module<BoardModule>(joined.World).Notes);
        Assert.Equal(own.World.Hash(), joined.World.Hash());
    }

    [Fact]
    public void CatchesUpOnTicksReleasedBetweenTheWelcomeAndItsFirstUpdate()
    {
        var (host, own) = LocalSession.Create(_game.Create(1), "b1");
        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        host.Admit(toPeer);
        host.Advance();
        host.Advance();
        var joined = Join(fromHost, "b1").Client!;
        Assert.Equal(2, joined.Update().Count);
        own.Update();
        Assert.Equal(own.World.Hash(), joined.World.Hash());
    }

    [Fact]
    public void SaysWhoItIsFirstAndIsSeatedWhereTheHostSaysForEveryTime()
    {
        var (host, own) = LocalSession.Create(_game.Create(1), "b1", seat: 3);
        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        string? said = null;
        // The host's game waits for the hello before it seats the peer (0032).
        toPeer.OnMessage(m =>
        {
            if (m is HelloMessage hello) said = hello.Player;
        });
        Client? joined = null;
        Client.Join(_game.Load, fromHost, "b1", c => joined = c, _ => { }, player: "friend");
        Assert.Equal("friend", said);
        host.Admit(toPeer, seat: 7);
        Assert.Equal((3, 7), (own.Id, joined!.Id));
        // A seat that left and comes back is the same id again, never handed to another meanwhile.
        host.Disconnect(7);
        var (again, _) = Loopback.Create<ToClient, ToHost>();
        Assert.Equal(7, host.Admit(again, seat: 7));
        var (newcomer, _) = Loopback.Create<ToClient, ToHost>();
        Assert.Equal(8, host.Admit(newcomer));
    }

    [Fact]
    public void IsTurnedAwayWithTheHostsReasonWhenItsPlayerIsAboardAlready()
    {
        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        Exception? refused = null;
        Client.Join(_game.Load, fromHost, "b1", _ => { }, why => refused = why, player: "friend");
        toPeer.Send(new RefusedMessage("You are aboard already"));
        Assert.Equal("You are aboard already", refused?.Message);
    }

    [Fact]
    public void IsRefusedByAHostRunningAnotherBuild()
    {
        var (host, _) = LocalSession.Create(_game.Create(1), "b1");
        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        host.Admit(toPeer);
        var (client, refused) = Join(fromHost, "b2");
        Assert.Null(client);
        Assert.Contains("build b1; this is build b2", refused?.Message);
    }

    [Fact]
    public void LeavesTheHostQuietAfterARefusalWhatItSendsNextGoesNowhere()
    {
        var (host, _) = LocalSession.Create(_game.Create(1), "b1");
        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        host.Admit(toPeer);
        Assert.NotNull(Join(fromHost, "b2").Refused);
        host.Advance();
    }

    /// <summary>A guest joining while the shuttle flies keeps step with the host through the arrival (shuttle-flight).</summary>
    [Fact]
    public void JoinsMidVoyageAndKeepsStepThroughTheArrival()
    {
        var game = Definition.Game();
        string[] rows =
        [
            "PPP             ",
            "                ",
            "################",
            "#..............#",
            "#.^^^^^^^^^^^^.#",
            "#.^^^^^^^^^^^^.#",
            "#.^^^^^^^^^^^^.#",
            "#.^^^^^^^^^^^^.#",
            "#.^^^^^^^^^^^^.#",
            "##____________##",
            "                ",
        ];
        var layout = Sim.Layout.Parse(rows, null, [((0, 0), (2, 9))]).Doubled();
        var (host, own) = LocalSession.Create(game.Create(1, layout), "b1");
        own.Send(new Sim.Crew.Enter(17 * 32 + 2));
        own.Send(new Sim.Places.Find("shuttle"));
        host.Advance();
        own.Update();
        var places = game.Module<Sim.Places.PlacesModule>(own.World);
        int shuttle = game.Module<Sim.Grid.GridModule>(own.World).MachinesOf("crewShuttle")[0].Id;
        own.Send(new Sim.Landing.Board(shuttle));
        own.Send(new Sim.Places.SetCourse(places.Contacts[^1].Id));
        host.Advance();
        own.Send(new Sim.Places.SetOff());
        for (int i = 0; i < 50; i++) host.Advance();
        own.Update();
        Assert.NotEmpty(game.Module<Sim.Landing.LandingModule>(own.World).Voyages);

        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        host.Admit(toPeer);
        Client? joined = null;
        Client.Join(game.Load, fromHost, "b1", c => joined = c, why => throw why);
        Assert.Equal(own.World.Hash(), joined!.World.Hash());
        for (int i = 0; i < Content.Shuttle.FlightTicks; i++)
        {
            host.Advance();
            own.Update();
            joined.Update();
            Assert.Equal(own.World.Hash(), joined.World.Hash());
        }
        Assert.Equal(1, joined.World.SurfaceOf(own.Id));
    }

    [Fact]
    public void IsOurBugOnAHostWithNoWorldToWelcomeWith()
    {
        var (toPeer, _) = Loopback.Create<ToClient, ToHost>();
        var why = Assert.Throws<InvalidOperationException>(() => new Host(0).Admit(toPeer));
        Assert.Contains("no world", why.Message);
    }
}
