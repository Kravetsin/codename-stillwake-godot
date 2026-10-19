using System.Diagnostics;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Site;
using Xunit;
using Xunit.Abstractions;

namespace Stillwake.Sim.Tests;

/// <summary>SPIKE (planet): the reference ship's tick with its crew aboard, and with them down on a site digging.</summary>
[Trait("Category", "Spike")]
public class SiteSpikeBudgetTests(ITestOutputHelper output)
{
    private static double Mean(World world, int ticks, Func<IReadOnlyList<Command>> commands)
    {
        var watch = new Stopwatch();
        for (int i = 0; i < ticks; i++)
        {
            var these = commands();
            watch.Start();
            world.Step(these);
            watch.Stop();
        }
        return watch.Elapsed.TotalMilliseconds / ticks;
    }

    [Fact]
    public void TheReferenceShipWithItsCrewAboardAndDown()
    {
        foreach (bool down in new[] { false, true })
        {
            var m = new Registry();
            var world = World.Create(1, BudgetTests.Reference(1024, 512), m.All);
            for (int p = 0; p < 4; p++) world.Step([new Enter(300 * 1024 + 300) { From = p }]);
            Mean(world, 100, () => []);
            if (down) world.Step([.. Enumerable.Range(0, 4).Select(p => new Beam(1) { From = p })]);
            int spawn = m.Site.SpawnOf(1), w = Content.Site.Width, n = 0;
            var ground = m.Site.GroundOf(1);
            double mean = Mean(world, 600, () =>
            {
                if (!down) return [];
                // Each hero hits the ground below its own column, a column apart from the next.
                var hits = new List<Command>();
                for (int p = 0; p < 4; p++)
                {
                    int x = spawn % w + p * 30 - 45;
                    int cell = Enumerable.Range(0, Content.Site.Height).Select(y => y * w + x).FirstOrDefault(c => ground!.WallAt(c) != null, -1);
                    if (cell >= 0) hits.Add(new Dig(cell, "fireAxe") { From = p });
                }
                n += hits.Count;
                return hits;
            });
            output.WriteLine($"reference ship 256×128 in 1024×512, 4 heroes {(down ? $"down on a site, digging ({n} hits, {m.Site.Sites[0].Dug.Count} cells broken)" : "aboard")}: {mean:F3} ms a tick");
        }
    }
}
