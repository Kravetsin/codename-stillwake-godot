using Stillwake.Game.Features.Crew;
using Stillwake.Game.Net;
using Stillwake.Net;
using Stillwake.Sim;
using Xunit;
using Xunit.Abstractions;
using Enter = Stillwake.Sim.Crew.Enter;
using Strike = Stillwake.Sim.Creatures.Strike;

namespace Stillwake.Game.Tests;

/// <summary>
/// What a co-op session costs the host's link in a heavy fight: every hero sprinting and threatened, so each reports its cell
/// every tick (Terraria's way), strikes three times a second and sends its hero every 50 ms (<see cref="RemoteHeroes.SendMs"/>),
/// each peer hashing once a second. The host's own player plays in place; every other peer is a guest on the wire. Counted as
/// written on a direct link (ENet) and through the playtest relay, which carries one frame of text a peer a game frame.
/// </summary>
public class NetTrafficTests(ITestOutputHelper output)
{
    private static readonly World Sample = World.Create(1, Layout.Parse(["...", "###"]).Doubled(), new Registry().All);

    /// <summary>A hero sprinting with the axe, its look turned, as it is sent.</summary>
    private static byte[] Hero(int peer) =>
        new SentHero(new Seen(new Hero(123_456 + peer * 7_919, 45_000, 1320, -600, false, 1), Look: 0.25, Wields: Stillwake.Content.Weapons.Axe.Item), 123_456.5).Encode();

    /// <summary>The bytes a second the host sends and takes with <paramref name="players"/> in the session, on a direct link and the relay.</summary>
    public static (int Out, int In, int RelayOut, int RelayIn) Fight(int players)
    {
        int guests = players - 1;
        int sentOut = 0, takenIn = 0, relayOut = 0, relayIn = 0;
        for (int t = 0; t < World.TicksPerSecond; t++)
        {
            // What goes to each guest on a tick, and what each sends, rides in one relay frame.
            var outbox = Enumerable.Range(0, players).Select(_ => new List<byte[]>()).ToList();
            var inbox = Enumerable.Range(0, players).Select(_ => new List<byte[]>()).ToList();
            var stamped = new List<Stamped>();
            for (int p = 0; p < players; p++)
            {
                stamped.Add(new Stamped(p, Sample.Encode(new Enter(1_048_575 - t * 3 + p) { Surface = 0 })));
                if (t % 7 == p % 7) stamped.Add(new Stamped(p, Sample.Encode(new Strike(40 + p, Stillwake.Content.Weapons.Axe.Item) { Surface = 0 })));
            }
            foreach (var s in stamped.Where(s => s.From > 0)) inbox[s.From].Add(Protocol.Write<ToHost>(new CommandMessage(s.Command)));
            for (int g = 1; g <= guests; g++) outbox[g].Add(Protocol.Write<ToClient>(new TickMessage(100_000 + t, stamped)));
            // Heroes every 50 ms, a tick: a guest's comes in and goes out to every other guest; the host's goes out to all.
            for (int p = 0; p < players; p++)
            {
                if (p > 0) inbox[p].Add(Protocol.Write<ToHost>(new HeroMessage(Hero(p))));
                for (int g = 1; g <= guests; g++)
                    if (g != p) outbox[g].Add(Protocol.Write<ToClient>(new PeerHeroMessage(p, Hero(p))));
            }
            if (t == 0)
                for (int g = 1; g <= guests; g++) inbox[g].Add(Protocol.Write<ToHost>(new HashMessage(100_000, 0xDEADBEEF_CAFEF00DUL)));
            for (int g = 1; g <= guests; g++)
            {
                sentOut += outbox[g].Sum(m => m.Length);
                takenIn += inbox[g].Sum(m => m.Length);
                relayOut += RelayFrames.ToPeerFrame(g, RelayFrames.Batch(outbox[g])).Length;
                relayIn += RelayFrames.ToHostFrame(RelayFrames.Batch(inbox[g])).Length;
            }
        }
        return (sentOut, takenIn, relayOut, relayIn);
    }

    /// <summary>
    /// The most the host may send and take a second, in KB, direct and on the relay, by players: a fifth over what it does now,
    /// so a regression shows. Before the wire was packed (2026-10-26-compact-wire), four players cost the host 81 KB/s out
    /// direct, 121 relayed.
    /// </summary>
    [Theory]
    [InlineData(2, 1.8, 1.3, 4.0, 3.1)]
    [InlineData(4, 13.3, 4.0, 22.9, 9.5)]
    [InlineData(6, 35.8, 6.6, 57.2, 15.8)]
    [InlineData(8, 68.8, 9.1, 107.0, 22.2)]
    public void AFightCostsTheHostNoMoreThan(int players, double outKb, double inKb, double relayOutKb, double relayInKb)
    {
        var (o, i, ro, ri) = Fight(players);
        output.WriteLine($"{players} players: direct out {o / 1024.0:F1} KB/s, in {i / 1024.0:F1} KB/s; relay out {ro / 1024.0:F1} KB/s, in {ri / 1024.0:F1} KB/s");
        Assert.True(o <= outKb * 1024, $"direct out {o / 1024.0:F1} KB/s");
        Assert.True(i <= inKb * 1024, $"direct in {i / 1024.0:F1} KB/s");
        Assert.True(ro <= relayOutKb * 1024, $"relay out {ro / 1024.0:F1} KB/s");
        Assert.True(ri <= relayInKb * 1024, $"relay in {ri / 1024.0:F1} KB/s");
    }

    /// <summary>
    /// Terraria's way of reporting (2026-10-26-creatures-are-composed) costs the host less than the heroes it already relays:
    /// four heroes threatened, each reporting its cell every tick, stamped into the tick sent to each of three guests.
    /// </summary>
    [Fact]
    public void FourThreatenedHeroesCostTheHostLessThanTheHeroesItAlreadyRelays()
    {
        var enter = Sample.Encode(new Enter(1_048_575) { From = 3, Surface = 0 });
        int quiet = Protocol.Write<ToClient>(new TickMessage(100_000, [])).Length;
        int busy = Protocol.Write<ToClient>(new TickMessage(100_000, [.. Enumerable.Range(0, 4).Select(p => new Stamped(p, enter))])).Length;
        int reports = (busy - quiet) * World.TicksPerSecond * 3;
        int hero = Protocol.Write<ToClient>(new PeerHeroMessage(3, Hero(3))).Length;
        int heroes = hero * (int)(1000 / RemoteHeroes.SendMs) * 4 * 3;
        output.WriteLine($"an Enter stamped {(busy - quiet) / 4} bytes, a hero relayed {hero}: threatened reports {reports} B/s, heroes {heroes} B/s");
        Assert.True(reports < heroes, $"reports {reports} B/s against heroes {heroes} B/s: report every second tick (Crew.ReportThreatened)");
    }
}
