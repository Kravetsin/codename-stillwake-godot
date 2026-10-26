using Xunit;

namespace Stillwake.Net.Tests;

/// <summary>packages/net/src/session.test.ts, ported.</summary>
public class SessionTests
{
    private readonly Definition _game = Definition.Board();

    private static Write Note(string text) => new(text);

    private IReadOnlyList<string> Notes(Client client) => _game.Module<BoardModule>(client.World).Notes;

    private (Host Host, Client Client) Single() => LocalSession.Create(_game.Create(1));

    // Single-player.

    [Fact]
    public void AppliesACommandOnTheTickTheHostReleasesIt()
    {
        var (host, client) = Single();
        client.Send(Note("hello"));
        Assert.Empty(client.Update());

        host.Advance();
        Assert.Single(client.Update());
        Assert.Equal(["0:hello"], Notes(client));
    }

    [Fact]
    public void RunsReleasedTicksEvenWithNoCommandsAndCatchesUpOnSeveralAtOnce()
    {
        var (host, client) = Single();
        host.Advance();
        host.Advance();
        host.Advance();
        Assert.Equal(3, client.Update().Count);
        Assert.Equal(3, client.World.Tick);
    }

    [Fact]
    public void PutsACommandSentBetweenReleasesIntoTheNextOne()
    {
        var (host, client) = Single();
        host.Advance();
        client.Send(Note("late"));
        host.Advance();
        client.Update();
        Assert.Equal(["1:late"], Notes(client));
    }

    [Fact]
    public void ReportsRefusalsBackToTheSender()
    {
        var (host, client) = Single();
        client.Send(Note(""));
        host.Advance();
        var report = Assert.Single(client.Update());
        Assert.Equal(["board.empty"], report.Refused.Select(r => r.Reason));
    }

    [Fact]
    public void StartsTheHostAtALoadedWorldsTick()
    {
        var played = _game.Create(1);
        for (int i = 0; i < 7; i++) played.Step([]);
        var (host, client) = LocalSession.Create(_game.Load(played.Save()));
        Assert.Equal(7, host.Tick);
        host.Advance();
        Assert.Single(client.Update());
    }

    // Several players.

    private (Host Host, List<Client> Clients) ThreePlayers()
    {
        var host = new Host(0);
        var clients = Enumerable.Range(0, 3).Select(_ =>
        {
            var (toClient, toHost) = Loopback.Create<ToClient, ToHost>();
            return new Client(_game.Create(9), toHost, host.Connect(toClient));
        }).ToList();
        return (host, clients);
    }

    [Fact]
    public void AppliesEveryonesCommandsInTheOrderTheHostReceivedThemOnEveryPeer()
    {
        var (host, clients) = ThreePlayers();
        var (a, b, c) = (clients[0], clients[1], clients[2]);
        b.Send(Note("b1"));
        a.Send(Note("a1"));
        host.Advance();
        c.Send(Note("c1"));
        b.Send(Note("b2"));
        host.Advance();
        foreach (var client in clients) client.Update();

        foreach (var client in clients) Assert.Equal(["0:b1", "0:a1", "1:c1", "1:b2"], Notes(client));
        Assert.Single(clients.Select(client => client.World.Hash()).Distinct());
    }

    [Fact]
    public void StampsEachPeersCommandsWithItsIdWhateverTheClientWrote()
    {
        var host = new Host(0);
        var peers = Enumerable.Range(0, 2).Select(_ => Loopback.Create<ToClient, ToHost>()).ToList();
        var ids = peers.Select(p => host.Connect(p.A)).ToList();
        Assert.Equal([0, 1], ids);
        var released = new List<int>();
        peers[0].B.OnMessage(message =>
        {
            if (message is TickMessage tick) released.AddRange(tick.Commands.Select(c => c.From));
        });
        var world = _game.Create(1);
        peers[1].B.Send(new CommandMessage(world.Encode(Note("x") with { From = 7 })));
        peers[0].B.Send(new CommandMessage(world.Encode(Note("y") with { From = 7 })));
        host.Advance();
        Assert.Equal([1, 0], released);
    }

    [Fact]
    public void LetsAPeerThatUpdatesLaterCatchUpToTheSameWorld()
    {
        var (host, clients) = ThreePlayers();
        var (eager, lazy) = (clients[0], clients[2]);
        for (int i = 0; i < 10; i++)
        {
            eager.Send(Note($"n{i}"));
            host.Advance();
            eager.Update();
        }
        lazy.Update();
        Assert.Equal(eager.World.Hash(), lazy.World.Hash());
    }

    [Fact]
    public void ThrowsWhenAReleasedTickDoesNotFollowTheWorldsTick()
    {
        var (toClient, toHost) = Loopback.Create<ToClient, ToHost>();
        var client = new Client(_game.Create(1), toHost, 0);
        toClient.Send(new TickMessage(5, []));
        var why = Assert.Throws<InvalidOperationException>(() => client.Update());
        Assert.Equal("Expected tick 0, but the host released 5.", why.Message);
    }

    // The loopback.

    [Fact]
    public void LoopbackDeliversCopiesSoSenderAndReceiverNeverShareAnObject()
    {
        var (a, b) = Loopback.Create<ToClient, ToHost>();
        var received = new List<ToClient>();
        b.OnMessage(received.Add);
        var commands = new List<Stamped> { new(1, [7]) };
        a.Send(new TickMessage(0, commands));
        commands.Add(new Stamped(2, [8]));
        commands[0].Command[0] = 9;
        var tick = Assert.IsType<TickMessage>(Assert.Single(received));
        Assert.Equal([7], Assert.Single(tick.Commands).Command);
    }

    [Fact]
    public void LoopbackHoldsMessagesUntilAReceiverIsSetThenDeliversThemInOrder()
    {
        var (a, b) = Loopback.Create<ToHost, ToClient>();
        a.Send(new HashMessage(1, 1));
        a.Send(new HashMessage(2, 2));
        var received = new List<ToHost>();
        b.OnMessage(received.Add);
        a.Send(new HashMessage(3, 3));
        Assert.Equal([new HashMessage(1, 1), new HashMessage(2, 2), new HashMessage(3, 3)], received);
    }

    private static IReadOnlyList<string> Heard(Client client) =>
        client.TakeHeroes().Select(h => $"{h.From}:{System.Text.Encoding.UTF8.GetString(h.State)}").ToList();

    private static byte[] Hero(string state) => System.Text.Encoding.UTF8.GetBytes(state);

    // A peer leaving.

    private (Host Host, List<Client> Clients) TwoPlayers()
    {
        var host = new Host(0);
        var clients = Enumerable.Range(0, 2).Select(_ =>
        {
            var (toClient, toHost) = Loopback.Create<ToClient, ToHost>();
            return new Client(_game.Create(1), toHost, host.Connect(toClient));
        }).ToList();
        return (host, clients);
    }

    [Fact]
    public void ALeavingPeerIsSentNothingMoreWhileTheOthersPlayOn()
    {
        var (host, clients) = TwoPlayers();
        clients[1].Send(Note("bye"));
        host.Disconnect(1);
        clients[0].SendHero(Hero("x"));
        host.Advance();
        Assert.Empty(clients[1].Update());
        Assert.Empty(clients[1].TakeHeroes());
        // What it sent before it left still goes out.
        clients[0].Update();
        Assert.Equal(["0:bye"], Notes(clients[0]));
    }

    [Fact]
    public void ALeavingPeersFarewellIsSentOnItsBehalfAfterWhatItSentAndOnlyOnce()
    {
        var (host, clients) = TwoPlayers();
        clients[1].Send(Note("bye"));
        var gone = clients[0].World.Encode(Note("gone"));
        host.Disconnect(1, gone);
        host.Disconnect(1, gone);
        host.Advance();
        clients[0].Update();
        var notes = Notes(clients[0]);
        Assert.Equal(2, notes.Count);
        Assert.EndsWith("gone", notes[1]);
    }

    // Heroes.

    [Fact]
    public void HeroesPassThroughTheHostToEveryOtherPeerStampedWithTheSenderAndNeverBack()
    {
        var host = new Host(0);
        var clients = Enumerable.Range(0, 3).Select(_ =>
        {
            var (toClient, toHost) = Loopback.Create<ToClient, ToHost>();
            return new Client(_game.Create(1), toHost, host.Connect(toClient));
        }).ToList();
        clients[1].SendHero(Hero("x1"));
        clients[2].SendHero(Hero("x2"));
        clients[1].SendHero(Hero("x3"));
        Assert.Equal(["1:x1", "2:x2", "1:x3"], Heard(clients[0]));
        Assert.Equal(["2:x2"], Heard(clients[1]));
        Assert.Empty(clients[0].TakeHeroes());
    }

    [Fact]
    public void HeroesStayOutOfTheSimulationNoTickNoCommandTheSameHash()
    {
        var (host, client) = Single();
        var (toClient, toHost) = Loopback.Create<ToClient, ToHost>();
        var other = new Client(_game.Create(1), toHost, host.Connect(toClient));
        other.SendHero(Hero("x5"));
        host.Advance();
        var report = Assert.Single(client.Update());
        Assert.Empty(report.Refused);
        // As a world that ran the same tick with nothing in it.
        var quiet = _game.Create(1);
        quiet.Step([]);
        Assert.Equal(quiet.Hash(), client.World.Hash());
        other.Update();
        Assert.Equal(client.World.Hash(), other.World.Hash());
        Assert.Equal(["1:x5"], Heard(client));
    }

    // A peer playing ticks out at its own pace.

    private (Client Client, Fed<ToClient, ToHost> Feed) Fed(int id = 1, bool loads = true)
    {
        var feed = new Fed<ToClient, ToHost>();
        return (new Client(_game.Create(1), feed, id, loads ? _game.Load : null), feed);
    }

    private TickMessage Tick(Client client, int n, string? text = null, int from = 0) =>
        new(n, text == null ? [] : [new Stamped(from, client.World.Encode(Note(text)))]);

    [Fact]
    public void CountsTheTicksWaitingAndRunsNoMoreThanItIsLet()
    {
        var (client, feed) = Fed();
        for (int n = 0; n < 5; n++) feed.Receive(Tick(client, n));
        Assert.Equal(5, client.Queued);
        Assert.Equal(2, client.Update(2).Count);
        Assert.Empty(client.Update(0));
        Assert.Equal(3, client.Queued);
        Assert.Equal(3, client.Update().Count);
        Assert.Equal(5, client.World.Tick);
    }

    [Fact]
    public void TakesAResyncInItsPlaceAmongTheTicksNotCountingIt()
    {
        var (client, feed) = Fed();
        // The host's world on tick 2, which differs from what this peer will have made of it.
        var truth = _game.Create(1);
        truth.Step([Note("host")]);
        truth.Step([]);
        feed.Receive(Tick(client, 0, "peer"));
        feed.Receive(Tick(client, 1));
        feed.Receive(new ResyncMessage(truth.Save()));
        feed.Receive(Tick(client, 2));
        Assert.Equal(3, client.Queued);
        client.Update(1);
        Assert.Empty(client.TakeDesyncs());
        // Tick 1 runs, then the resync behind it is taken; tick 2 waits.
        Assert.Single(client.Update(1));
        Assert.Single(client.TakeDesyncs());
        Assert.Equal(["0:host"], Notes(client));
        Assert.Equal(1, client.Queued);
        client.Update();
        Assert.Equal(3, client.World.Tick);
    }

    // Commands in flight.

    [Fact]
    public void InFlightAreThisPeersSentAndNotYetRunLeavingInOrderRefusedOnesToo()
    {
        var host = new Host(0);
        var (toA, fromA) = Loopback.Create<ToClient, ToHost>();
        var (toB, fromB) = Loopback.Create<ToClient, ToHost>();
        var a = new Client(_game.Create(1), fromA, host.Connect(toA));
        var b = new Client(_game.Create(1), fromB, host.Connect(toB));
        a.Send(Note("one"));
        b.Send(Note("other"));
        a.Send(Note(""));
        Assert.Equal(["one", ""], a.InFlight.Cast<Write>().Select(c => c.Text));
        host.Advance();
        a.Send(Note("three"));
        var report = Assert.Single(a.Update());
        Assert.Single(report.Refused);
        Assert.Equal(["three"], a.InFlight.Cast<Write>().Select(c => c.Text));
        Assert.Single(b.InFlight);
        host.Advance();
        a.Update();
        Assert.Empty(a.InFlight);
    }

    [Fact]
    public void InFlightStayThroughAResyncAndLeaveWhenTheirTickRunsAfterIt()
    {
        var (client, feed) = Fed(id: 2);
        client.Send(Note("kept"));
        feed.Receive(new ResyncMessage(_game.Create(1).Save()));
        client.Update();
        Assert.Single(client.InFlight);
        feed.Receive(Tick(client, 0, "kept", from: 2));
        client.Update();
        Assert.Empty(client.InFlight);
        Assert.Equal(["0:kept"], Notes(client));
    }

    [Fact]
    public void ThrowsWhenWhatComesBackIsNotWhatWasSentNextOurBug()
    {
        var fixtures = Definition.Fixtures();
        var feed = new Fed<ToClient, ToHost>();
        var client = new Client(fixtures.Create(1), feed, 3);
        client.Send(Note("mine"));
        feed.Receive(new TickMessage(0, [new Stamped(3, client.World.Encode(new Push(1)))]));
        var why = Assert.Throws<InvalidOperationException>(() => client.Update());
        Assert.Equal("Peer 3 sent Write next, but Push came back.", why.Message);
    }
}
