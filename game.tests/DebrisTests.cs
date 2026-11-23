using System.Text.Json;
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
    public void AWreckGoneShortOfItsPathsEndWasBrokenEvenByOneHitAndOneThatDriftedOffOrWasCaughtWasNot()
    {
        // Last seen at 10 400, drifting 300 a tick to an end at 10 000.
        Assert.True(Fading.Broken(wreck: true, caught: false, x: 10_400, speed: 300, end: 10_000, ticks: 1));
        // Not a wreck, so nothing breaks it; caught by hand, it was taken.
        Assert.False(Fading.Broken(wreck: false, caught: false, x: 10_400, speed: 300, end: 10_000, ticks: 1));
        Assert.False(Fading.Broken(wreck: true, caught: true, x: 10_400, speed: 300, end: 10_000, ticks: 1));
        Assert.False(Fading.Broken(wreck: true, caught: false, x: 10_200, speed: 300, end: 10_000, ticks: 1));
        // A stall of two ticks between frames: it drifted off, though one tick on it would still be out.
        Assert.False(Fading.Broken(wreck: true, caught: false, x: 10_400, speed: 300, end: 10_000, ticks: 2));
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

    // This peer's own hits (0042), felt as they are sent (0014).

    [Fact]
    public void AnOwnHitIsSentOnItsPiecesAtTheReadItIsFirstSeenAndRanOnThemAtTheReadItIsGone()
    {
        var hits = new OwnHits();
        var hew = new HewWrecks("axe", 0, 0, 1);
        int asked = 0;
        IReadOnlyList<int> Struck(HewWrecks _)
        {
            asked++;
            return [5, 7];
        }
        hits.Read([hew], Struck);
        Assert.Equal("axe", hits.SentOn(5));
        Assert.Equal("axe", hits.SentOn(7));
        Assert.False(hits.Ran(5));
        // Still flying: felt once only, and its pieces kept from when it was first seen.
        hits.Read([hew], Struck);
        Assert.Null(hits.SentOn(5));
        Assert.Equal(1, asked);
        hits.Read([], Struck);
        Assert.Null(hits.SentOn(5));
        Assert.Equal("axe", hits.ToolOn(5));
        Assert.True(hits.Ran(7));
    }

    // Where a piece is drawn past its last tick (Session.Drift).

    [Fact]
    public void APieceIsDrawnOnPastItsTickByItsMotionWhateverItsSpeed()
    {
        var drifting = JsonSerializer.Deserialize<Piece>("""{"X":10000,"Y":500,"Speed":100,"Vx":-100}""")!;
        Assert.Equal((9650.0, 500.0), PieceDrawn.At(drifting, 3.5));
        // Fed a motion of its own by the host player's body: that one, up or down too.
        var pulled = JsonSerializer.Deserialize<Piece>("""{"X":10000,"Y":500,"Speed":100,"Vx":200,"Vy":-40}""")!;
        Assert.Equal((10_400.0, 420.0), PieceDrawn.At(pulled, 2));
    }

    [Fact]
    public void APosesPointsGoToTheWorldAndBackTurnedAsTheBoxIs()
    {
        var pose = new PiecePose(1_000, 2_000, 16_384);
        // A quarter turn: the piece's own right is down on screen.
        var (x, y) = pose.At(500, 0);
        Assert.Equal(1_000, x, 6);
        Assert.Equal(2_500, y, 6);
        var (lx, ly) = pose.Local(x, y);
        Assert.Equal(500, lx, 6);
        Assert.Equal(0, ly, 6);
        var (ux, uy) = pose.Unturned(pose.Turned(0.6, 0.8).X, pose.Turned(0.6, 0.8).Y);
        Assert.Equal(0.6, ux, 6);
        Assert.Equal(0.8, uy, 6);
    }
}
