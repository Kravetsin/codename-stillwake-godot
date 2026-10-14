using Xunit;

namespace Stillwake.Net.Tests;

/// <summary>
/// apps/game/src/session/bad-network.test.ts, ported to channels: a link made bad on purpose, the
/// local transport with lag and jitter. Then a whole session over one, as 0015 was measured.
/// </summary>
public class BadNetworkTests
{
    [Fact]
    public void IsReadFromTheCommandLineAndNoneWithoutIt()
    {
        Assert.Equal(new BadNetwork(100, 60), BadNetwork.From(["--join", "--lag=100", "--jitter=60"]));
        Assert.Equal(new BadNetwork(0, 40), BadNetwork.From(["--host", "--jitter=40"]));
        Assert.Equal(new BadNetwork(0, 0), BadNetwork.From(["--host", "--lag=oops"]));
        Assert.Null(BadNetwork.From(["--host"]));
    }

    /// <summary>A channel that records what it sends, when, and hears what the test says.</summary>
    private sealed class Recorder(TestTiming timing) : IChannel<string, string>
    {
        private Action<string> _receive = _ => { };
        public List<(double At, string Message)> Sent { get; } = [];
        public void Send(string message) => Sent.Add((timing.Now, message));
        public void OnMessage(Action<string> handler) => _receive = handler;
        public void Hear(string message) => _receive(message);
    }

    [Fact]
    public void SendsEachMessageLagPlusUpToJitterLate()
    {
        var timing = new TestTiming();
        var inner = new Recorder(timing);
        var bad = new BadNetwork(100, 60).Over(inner, timing);
        timing.Draw = 0.5;
        bad.Send("a");
        timing.Pass(129);
        Assert.Empty(inner.Sent);
        timing.Pass(1000);
        Assert.Equal([(130.0, "a")], inner.Sent);
    }

    [Fact]
    public void HandsALoneMessageOverWhenItsTimerFiresHoweverEarlyByTheClock()
    {
        var timing = new TestTiming(early: 0.5);
        var inner = new Recorder(timing);
        var bad = new BadNetwork(100, 0).Over(inner, timing);
        bad.Send("alone");
        timing.Pass(1000);
        Assert.Equal(["alone"], inner.Sent.Select(s => s.Message));
    }

    [Fact]
    public void NeverHandsAMessageOverBeforeTheOneSentAheadOfIt()
    {
        var timing = new TestTiming();
        var inner = new Recorder(timing);
        var bad = new BadNetwork(100, 60).Over(inner, timing);
        timing.Draw = 1;
        bad.Send("slow");
        timing.Pass(10);
        timing.Draw = 0;
        bad.Send("quick");
        timing.Pass(1000);
        // The quick one would be due at 110; it waits for the slow one, due at 160.
        Assert.Equal([(160.0, "slow"), (160.0, "quick")], inner.Sent);
    }

    [Fact]
    public void DelaysWhatIsHeardTheSameWayInOrder()
    {
        var timing = new TestTiming();
        var inner = new Recorder(timing);
        var bad = new BadNetwork(50, 0).Over(inner, timing);
        var heard = new List<(string, double)>();
        bad.OnMessage(m => heard.Add((m, timing.Now)));
        inner.Hear("joined");
        timing.Pass(20);
        inner.Hear("message");
        timing.Pass(49);
        Assert.Empty(heard);
        timing.Pass(1000);
        Assert.Equal([("joined", 50.0), ("message", 70.0)], heard);
    }

    [Fact]
    public void ASessionOverABadNetworkStaysInStepAndShowsItsCommandsBackWithinAFewHundredMs()
    {
        var timing = new TestTiming();
        var game = Definition.Board();
        var (host, own) = LocalSession.Create(game.Create(1), "b");
        var network = new BadNetwork(100, 60);
        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        var draws = new Sim.Rng(new Sim.RngState { S = 9 });
        // One end made bad, both ways, as a guest across the internet sees its link.
        host.Admit(toPeer);
        Session? peer = null;
        Client.Join(game.Load, network.Over(fromHost, timing), "b", c => peer = new Session(null, c), why => throw why);
        var mine = new Session(host, own);
        const double frame = 1000.0 / 60;
        for (int n = 1; n <= 1200; n++)
        {
            timing.Draw = draws.Below(1000) / 1000.0;
            timing.Pass(n * frame);
            mine.Frame(frame);
            peer?.Frame(frame);
            if (n % 60 == 0) peer?.Client.Send(new Write($"n{n}"));
        }
        Assert.NotNull(peer);
        // 100 ms each way, up to 60 more, a tick's wait and the reserve: 0015 measured about 354.
        Assert.InRange(peer.RoundTrip.Median ?? 0, 250, 420);
        // The host stops; what is on its way arrives, and the peer plays it out.
        timing.Pass(1200 * frame + 1000);
        for (int n = 0; n < 120; n++) peer.Frame(frame);
        Assert.Equal(own.World.Tick, peer.Client.World.Tick);
        Assert.Equal(own.World.Hash(), peer.Client.World.Hash());
        Assert.Empty(peer.Desyncs);
        Assert.Equal(19, game.Module<BoardModule>(peer.Client.World).Count);
    }
}
