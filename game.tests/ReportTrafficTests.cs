using Stillwake.Game.Features.Crew;
using Stillwake.Net;
using Stillwake.Sim;
using Xunit;
using Xunit.Abstractions;
using Enter = Stillwake.Sim.Crew.Enter;

namespace Stillwake.Game.Tests;

/// <summary>
/// What Terraria's way of reporting costs the link (2026-10-26-creatures-are-composed): four heroes sprinting in a fight, each
/// threatened and reporting its cell every tick, as the host stamps them into the tick it sends each of three guests; set
/// against what the host already relays of the heroes themselves, every 50 ms (<see cref="RemoteHeroes.SendMs"/>).
/// </summary>
public class ReportTrafficTests(ITestOutputHelper output)
{
    [Fact]
    public void FourThreatenedHeroesCostTheHostLessThanTheHeroesItAlreadyRelays()
    {
        var m = new Registry();
        var world = World.Create(1, Layout.Parse(["...", "###"]).Doubled(), m.All);
        string enter = world.Encode(new Enter(1_048_575) { From = 3 });
        int quiet = Protocol.Write<ToClient>(new TickMessage(100_000, [])).Length;
        int busy = Protocol.Write<ToClient>(new TickMessage(100_000, [.. Enumerable.Range(0, 4).Select(p => new Stamped(p, enter))])).Length;
        int reports = (busy - quiet) * World.TicksPerSecond * 3;
        // A hero running with the axe, as it is relayed to every other peer.
        var running = new Hero(123_456, 45_000, 1320, -600, false, 1);
        string state = new SentHero(new Seen(running, Look: 0.25, Wields: Stillwake.Content.Weapons.Axe.Item), 123_456.5).Encode();
        int hero = Protocol.Write<ToClient>(new PeerHeroMessage(3, state)).Length;
        int heroes = hero * (int)(1000 / RemoteHeroes.SendMs) * 4 * 3;
        output.WriteLine($"an Enter stamped {(busy - quiet) / 4} bytes, a hero relayed {hero}: threatened reports {reports} B/s, heroes {heroes} B/s");
        Assert.True(reports < heroes, $"reports {reports} B/s against heroes {heroes} B/s: report every second tick (Crew.ReportThreatened)");
    }
}
