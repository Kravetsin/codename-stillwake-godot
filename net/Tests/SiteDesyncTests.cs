using Stillwake.Sim;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Site;
using Xunit;
using Xunit.Abstractions;

namespace Stillwake.Net.Tests;

/// <summary>SPIKE (planet): peers stay alike while heroes beam down, dig and beam back, and a peer joins with a site made.</summary>
public class SiteDesyncTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(1u)]
    [InlineData(7u)]
    [InlineData(2026u)]
    public void KeepsEveryClientInSyncAcrossBeamsAndDigging(uint seed)
    {
        var game = Definition.Game();
        int sent = 0, maxDug = 0, joined = 0;
        var run = Desync.Fuzz(game, seed, layout: Registry.Wreck, ticks: 1200, joinAt: 600, tamper: (tick, clients) =>
        {
            joined = clients.Count;
            for (int i = 0; i < 2; i++)
            {
                var client = clients[i];
                var site = game.Module<SiteModule>(client.World);
                var crew = game.Module<CrewModule>(client.World);
                var hero = crew.HeroOf(client.Id);
                if (hero == null) continue;
                // Down at 50 and 650, each to its own site; back up at 400 and 1000.
                if (tick % 600 == 50 + i * 10 && hero.Site == 0) client.Send(new Beam(i + 1));
                if (tick % 600 == 400 && hero.Site != 0) client.Send(new Beam(0));
                if (hero.Site == 0 || site.GroundOf(hero.Site) is not { } ground) continue;
                int x = site.SpawnOf(hero.Site) % ground.Width + tick % 7 - 3;
                int cell = Enumerable.Range(0, ground.Height).Select(y => y * ground.Width + x).FirstOrDefault(c => ground.WallAt(c) != null, -1);
                if (cell < 0) continue;
                client.Send(new Dig(cell, "fireAxe"));
                sent++;
                maxDug = Math.Max(maxDug, site.Sites.Sum(s => s.Dug.Count));
            }
        });
        output.WriteLine($"{run.Ticks} ticks, {run.Commands} fuzzed commands ({run.Refused} refused), {sent} digs sent, {maxDug} cells dug, {joined} clients");
        Assert.Null(run.Desync);
        Assert.Equal(1200, run.Ticks);
        Assert.Equal(4, joined);
        Assert.True(maxDug > 20, $"{maxDug} cells dug");
    }
}
