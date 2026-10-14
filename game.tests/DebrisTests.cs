using Stillwake.Game.Features.Debris;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The client's debris: fade.test.ts and the debris test of session/predicted.test.ts, ported; catching
/// is aimed as everything else E works is, in <see cref="AimTests"/>.
/// </summary>
public class DebrisTests
{
    [Fact]
    public void APieceComesOutOfTheDarkOverTheStartOfItsPathAndGoesBackIntoItAtTheEnd()
    {
        double Fade(int x) => Fading.FadeOf(x, 100_000, 10_000);
        Assert.Equal([0, 0.5, 1, 0.5, 0], new[] { Fade(100_000), Fade(96_000), Fade(50_000), Fade(14_000), Fade(9_000) });
    }

    // The debris this peer is grabbing (predicted.test.ts).

    /// <summary>A link whose host is not here: the test releases ticks by hand.</summary>
    private sealed class Fed : IChannel<ToClient, ToHost>
    {
        private Action<ToClient> _receive = _ => { };
        public void Send(ToHost message) { }
        public void OnMessage(Action<ToClient> handler) => _receive = handler;
        public void Receive(ToClient message) => _receive(message);
    }

    [Fact]
    public void IsEachPieceWithAGrabInFlightAndNoneOnceItHasRun()
    {
        var link = new Fed();
        var client = new Client(World.Create(1, Layout.Parse("#D#\n#D#\n..."), new Registry().All), link, 1);
        Command[] sent = [new Grab(4), new ToggleDoor(1), new Grab(9)];
        foreach (var c in sent) client.Send(c);
        Assert.Equal([4, 9], Predicted.Grabbing(client.InFlight).Order());
        link.Receive(new TickMessage(client.World.Tick, sent.Select(c => new Stamped(1, client.World.Encode(c))).ToList()));
        client.Update();
        Assert.Empty(Predicted.Grabbing(client.InFlight));
    }
}
