using System.Text.Json.Nodes;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Net.Tests;

/// <summary>A command goes out stamped with the surface its sender is on (0076); sessions carry every surface.</summary>
public class SurfaceStampTests
{
    private readonly Definition _game = Definition.Board();

    private static readonly string Strip = Content.Surfaces.TestStrip.Id;

    [Fact]
    public void ACommandSentIsStampedWithTheSendersSurfaceAndRunsThere()
    {
        var (host, client) = LocalSession.Create(_game.Create(1));
        client.Send(new Write("hello"));
        Assert.Equal(0, Assert.Single(client.InFlight).Surface);
        host.Advance();
        Assert.Empty(Assert.Single(client.Update()).Refused);
        Assert.Equal(["0:hello"], _game.Module<BoardModule>(client.World).Notes);
    }

    [Fact]
    public void WhatIsInFlightFromASurfaceLeftSinceIsNotShownAndIsRefusedOnItsTick()
    {
        var game = Definition.Game();
        var (host, client) = LocalSession.Create(game.Create(1, Layout.Parse(["#.D.#"])));
        client.Send(new OpenSurface(Strip));
        client.Send(new Transfer(1));
        host.Advance();
        client.Update();
        Assert.Equal(1, client.World.SurfaceOf(client.Id));
        // Back aboard, and a toggle sent from the strip in the same tick: it lands with its hero gone, refused.
        client.Send(new Transfer(0));
        client.Send(new SetDoor(5, true));
        host.Advance();
        // Not run yet: the hero is still on the strip, the toggle shown there.
        Assert.Equal(2, client.InFlight.Count);
        var report = Assert.Single(client.Update());
        Assert.Equal([World.Elsewhere], report.Refused.Select(r => r.Reason));
        Assert.Empty(client.InFlight);
        Assert.Equal(Structure.DoorClosed, game.Module<GridModule>(client.World).StructureAt(2));
    }

    [Fact]
    public void TheInFlightOfAnotherSurfaceAreLeftOutAsSoonAsTheHeroIsElsewhere()
    {
        var game = Definition.Game();
        var (host, client) = LocalSession.Create(game.Create(1, Layout.Parse(["#.D.#"])));
        client.Send(new OpenSurface(Strip));
        host.Advance();
        client.Update();
        client.Send(new Transfer(1));
        host.Advance();
        // Sent from the ship before the transfer's tick runs here: once it has, the toggle is the ship's, not the strip's grid's.
        client.Send(new SetDoor(2, true));
        Assert.Equal(2, client.InFlight.Count);
        client.Update();
        Assert.Equal(1, client.World.SurfaceOf(client.Id));
        Assert.Empty(client.InFlight);
        host.Advance();
        Assert.Equal([World.Elsewhere], Assert.Single(client.Update()).Refused.Select(r => r.Reason));
    }

    [Fact]
    public void AJoinerIsWelcomedWithEverySurfaceAndWhereEachPeerIs()
    {
        var game = Definition.Game();
        var (host, own) = LocalSession.Create(game.Create(1, Layout.Parse(["#.S.#"])), "b1");
        own.Send(new OpenSurface(Strip));
        own.Send(new Transfer(1));
        host.Advance();
        own.Update();
        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        host.Admit(toPeer);
        Client? joined = null;
        Client.Join(game.Load, fromHost, "b1", c => joined = c, why => throw why);
        Assert.NotNull(joined);
        Assert.Equal(2, joined!.World.Surfaces.Count);
        Assert.Equal(1, joined.World.SurfaceOf(own.Id));
        Assert.Equal(own.World.Hash(), joined.World.Hash());
        joined.Send(new Transfer(1));
        host.Advance();
        own.Update();
        joined.Update();
        Assert.Equal(own.World.Hash(), joined.World.Hash());
        Assert.Equal(1, own.World.SurfaceOf(joined.Id));
    }

    [Fact]
    public void AJoinerHasTheHostsWorldId()
    {
        var game = Definition.Game();
        var world = game.Load(World.Create(1, Layout.Parse(["#.S.#"]), game.Make(), 7).Save());
        var (host, own) = LocalSession.Create(world, "b1");
        var (toPeer, fromHost) = Loopback.Create<ToClient, ToHost>();
        host.Admit(toPeer);
        Client? joined = null;
        Client.Join(game.Load, fromHost, "b1", c => joined = c, why => throw why);
        Assert.Equal(7u, joined!.World.Id);
        Assert.Equal(own.World.Hash(), joined.World.Hash());
    }

    [Fact]
    public void ADesyncOnAnotherSurfaceIsReportedByThatSurfaceAndModule()
    {
        var game = Definition.Game();
        var world = game.Create(1, Layout.Parse(["#.S.#"]));
        world.Step([new OpenSurface(Strip)]);
        string theirs = world.Save();
        var root = JsonNode.Parse(theirs)!;
        root["surfaces"]![0]!["slices"]!["crew"]!["clock"] = 99;
        root["where"] = new JsonArray(new JsonObject { ["peer"] = 3, ["surface"] = 1 });
        var report = Diff.Saves(root.ToJsonString(), theirs);
        Assert.Equal(["1:crew", "where"], report.Modules.Select(m => m.Id));
        Assert.Equal(["crew.clock"], report.Modules[0].Paths);
    }
}
