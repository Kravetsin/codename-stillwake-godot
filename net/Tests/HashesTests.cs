using System.Text.Json;
using Xunit;

namespace Stillwake.Net.Tests;

/// <summary>packages/net/src/hashes.test.ts, ported.</summary>
public class HashesTests
{
    private const int Every = Protocol.HashEvery;
    private readonly Definition _game = Definition.Fixtures();

    /// <summary>A host player's session and a peer joined to it, both at tick 0.</summary>
    private ((Host Host, Client Client) Own, Client Peer) Session()
    {
        var own = LocalSession.Create(_game.Create(1), "b");
        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        own.Host.Admit(toPeer);
        Client? peer = null;
        Client.Join(_game.Load, fromHost, "b", c => peer = c, why => throw why);
        return (own, peer!);
    }

    /// <summary>Releases ticks up to <paramref name="tick"/> and runs them, the host player first or the peer first.</summary>
    private static void RunTo((Host Host, Client Client) own, Client peer, int tick, bool peerFirst = false)
    {
        while (own.Host.Tick < tick) own.Host.Advance();
        foreach (var client in peerFirst ? new[] { peer, own.Client } : [own.Client, peer]) client.Update();
    }

    private void Drift(Client client, int by) => _game.Module<DriftModule>(client.World).Nudge(by);

    private static string Describe(IEnumerable<DesyncReport> reports) => string.Join(" | ", reports);

    [Fact]
    public void HashNothingWhileTheHostPlayerPlaysAloneAndCheckAGuestOnceOneJoins()
    {
        var own = LocalSession.Create(_game.Create(1), "b");
        var session = new Session(own.Host, own.Client);
        for (int i = 0; i < 3 * Every; i++) session.Frame(1000.0 / Stillwake.Sim.World.TicksPerSecond);
        Assert.Null(own.Client.LastHash);
        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        own.Host.Admit(toPeer);
        Client? peer = null;
        Client.Join(_game.Load, fromHost, "b", c => peer = c, why => throw why);
        Drift(peer!, 1);
        for (int i = 0; i < 3 * Every; i++)
        {
            session.Frame(1000.0 / Stillwake.Sim.World.TicksPerSecond);
            peer!.Update();
        }
        Assert.NotNull(own.Client.LastHash);
        // The guest drifted after it joined: the host player's hashes catch it.
        Assert.NotEmpty(peer!.TakeDesyncs());
    }

    [Fact]
    public void LeavePeersInStepAlone()
    {
        var (own, peer) = Session();
        peer.Send(new Write("hi"));
        RunTo(own, peer, 3 * Every);
        RunTo(own, peer, 3 * Every + 1);
        Assert.Empty(peer.TakeDesyncs());
        Assert.Equal(own.Client.World.Hash(), peer.World.Hash());
    }

    [Fact]
    public void CatchADriftedPeerAtTheNextCheckReportWhatDriftedAndRepairIt()
    {
        var (own, peer) = Session();
        RunTo(own, peer, 5);
        // Injected: the peer's world changed behind the pipeline's back.
        Drift(peer, 3);
        RunTo(own, peer, Every);
        // The host saw the hashes differ and sent the resync; the peer takes it on its next update.
        RunTo(own, peer, Every + 1);
        Assert.Equal($"tick {Every}: drift at drift.position", Describe(peer.TakeDesyncs()));
        Assert.Equal(own.Client.World.Hash(), peer.World.Hash());
        // Repaired, it stays in step.
        RunTo(own, peer, 3 * Every + 1);
        Assert.Empty(peer.TakeDesyncs());
        Assert.Equal(own.Client.World.Hash(), peer.World.Hash());
    }

    [Fact]
    public void HoldAPeersHashUntilTheHostPlayersArrivesThenCheckIt()
    {
        var (own, peer) = Session();
        Drift(peer, 1);
        RunTo(own, peer, Every, peerFirst: true);
        RunTo(own, peer, Every + 1);
        Assert.Single(peer.TakeDesyncs());
        Assert.Equal(own.Client.World.Hash(), peer.World.Hash());
    }

    [Fact]
    public void ResyncFromTheHostPlayersWorldOnTheHostsTickHoweverTheUpdatesFall()
    {
        var (own, peer) = Session();
        Drift(peer, 1);
        // The peer reports first, its hash waits; the host releases on before its player catches up.
        while (own.Host.Tick < Every) own.Host.Advance();
        peer.Update();
        own.Host.Advance();
        own.Client.Update();
        peer.Update();
        Assert.Equal($"tick {Every + 1}: drift at drift.position", Describe(peer.TakeDesyncs()));
        RunTo(own, peer, 2 * Every + 1);
        Assert.Equal(own.Client.World.Hash(), peer.World.Hash());
    }

    [Fact]
    public void ResyncAPeerOnceNotAgainForTheHashesItSentBeforeTakingTheResync()
    {
        var (own, peer) = Session();
        Drift(peer, 1);
        // The peer runs two checks' worth before it takes the resync the first one brought.
        RunTo(own, peer, 2 * Every);
        RunTo(own, peer, 2 * Every + 1);
        Assert.Single(peer.TakeDesyncs());
        RunTo(own, peer, 4 * Every + 1);
        Assert.Empty(peer.TakeDesyncs());
    }

    // A desync report.

    private static string Save(int tick, int rng, object slices) =>
        JsonSerializer.Serialize(new { format = 1, tick, rng, slices });

    [Fact]
    public void ADesyncReportNamesEachModuleThatDiffersWithThePathsWithinItAndWhetherTheRngDrifted()
    {
        var mine = Save(9, 1, new { a = new { x = new[] { 1, 2, 3 }, y = new { z = 1 } }, b = new { k = 1 }, c = 5 });
        var theirs = Save(9, 2, new { a = new { x = new[] { 1, 5 }, y = new { z = 2 } }, b = new { k = 1 }, c = 6 });
        var report = Diff.Saves(mine, theirs);
        Assert.Equal(9, report.Tick);
        Assert.True(report.Rng);
        Assert.Equal(["a", "c"], report.Modules.Select(m => m.Id));
        Assert.Equal(["a.x.length", "a.x[1]", "a.y.z"], report.Modules[0].Paths);
        Assert.Equal(["c"], report.Modules[1].Paths);
    }

    [Fact]
    public void ADesyncReportListsAFewPathsPerModuleNotAWholeDriftedGrid()
    {
        var many = Enumerable.Range(0, 100).ToArray();
        var report = Diff.Saves(Save(0, 1, new { g = many }), Save(0, 1, new { g = many.Select(n => n + 1) }));
        Assert.Equal(10, report.Modules[0].Paths.Count);
    }
}
