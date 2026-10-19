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
    public void AWreckGoneWithHitsShortOfItsPathsEndWasBrokenAndOneThatDriftedOffWasNot()
    {
        // Last seen at 10 400, drifting 300 a tick to an end at 10 000.
        Assert.True(Fading.Broken(damage: 14, x: 10_400, speed: 300, end: 10_000, ticks: 1));
        Assert.False(Fading.Broken(damage: 0, x: 10_400, speed: 300, end: 10_000, ticks: 1));
        Assert.False(Fading.Broken(damage: 14, x: 10_200, speed: 300, end: 10_000, ticks: 1));
        // A stall of two ticks between frames: it drifted off, though one tick on it would still be out.
        Assert.False(Fading.Broken(damage: 14, x: 10_400, speed: 300, end: 10_000, ticks: 2));
    }

    // The debris this peer is grabbing (predicted.test.ts).

    [Fact]
    public void IsEachPieceWithAGrabInFlightAndNoneOnceItHasRun()
    {
        var link = new Fed();
        var client = new Client(World.Create(1, Layout.Parse("#D#\n#D#\n..."), new Registry().All), link, 1);
        Command[] sent = [new Grab(4), new SetDoor(1, true), new Grab(9)];
        foreach (var c in sent) client.Send(c);
        Assert.Equal([4, 9], Predicted.Grabbing(client.InFlight).Order());
        link.Receive(new TickMessage(client.World.Tick, sent.Select(c => new Stamped(1, client.World.Encode(c))).ToList()));
        client.Update();
        Assert.Empty(Predicted.Grabbing(client.InFlight));
    }
}
