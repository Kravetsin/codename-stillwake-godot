using Stillwake.Game.Features.Items;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>This peer's new gains in each surface's items journal, once each (2026-11-06-head-tally).</summary>
public class OwnGainsTests
{
    private static Gain G(long seq, int peer = 0, string item = "soil") => new(seq, peer, item, 1, 3);

    [Fact]
    public void GivesEachOfItsOwnGainsOnceAcrossReadsAndSkipsOtherPeers()
    {
        var own = new OwnGains();
        object ship = new();
        var gains = new List<Gain>();
        Assert.Empty(own.Read(ship, gains, 0));
        gains.AddRange([G(1), G(2, peer: 1)]);
        Assert.Equal([1L], own.Read(ship, gains, 0).Select(g => g.Seq));
        Assert.Empty(own.Read(ship, gains, 0));
        gains.Add(G(3));
        Assert.Equal([3L], own.Read(ship, gains, 0).Select(g => g.Seq));
    }

    [Fact]
    public void KeepsAMarkPerSurfaceShowingNothingOldOnComingBackAndANewSurfacesFirstGain()
    {
        var own = new OwnGains();
        object ship = new(), planet = new();
        var onShip = new List<Gain> { G(1), G(2) };
        own.Read(ship, onShip, 0);
        onShip.Add(G(3));
        Assert.Equal([3L], own.Read(ship, onShip, 0).Select(g => g.Seq));
        // On the planet, its sequence from 1 again: a mark of 3 would hide it.
        var onPlanet = new List<Gain>();
        Assert.Empty(own.Read(planet, onPlanet, 0));
        onPlanet.Add(G(1, item: "rock"));
        Assert.Equal(["rock"], own.Read(planet, onPlanet, 0).Select(g => g.Item));
        // Back on the ship: nothing it showed comes again.
        Assert.Empty(own.Read(ship, onShip, 0));
    }
}
