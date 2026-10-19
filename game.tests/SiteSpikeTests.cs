using System.Diagnostics;
using System.Text.Json.Nodes;
using Stillwake.Game.Dev;
using Stillwake.Sim;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Rooms;
using Stillwake.Sim.Site;
using Xunit;
using Xunit.Abstractions;
using SiteSpec = Stillwake.Content.Site;

namespace Stillwake.Game.Tests;

/// <summary>
/// SPIKE (planet): what a lean sortie site costs, measured with a Stopwatch here, never in the sim. Run with
/// <c>dotnet test game.tests --filter SiteSpike --logger "console;verbosity=detailed"</c> to read the numbers.
/// </summary>
[Trait("Category", "Spike")]
public class SiteSpikeTests(ITestOutputHelper output)
{
    private static double Ms(long since) => Stopwatch.GetElapsedTime(since).TotalMilliseconds;

    private static (double Mean, double P99, double Max) Time(World world, int ticks, Func<int, IReadOnlyList<Command>> commands)
    {
        var each = new List<double>(ticks);
        for (int i = 0; i < ticks; i++)
        {
            var these = commands(i);
            long start = Stopwatch.GetTimestamp();
            world.Step(these);
            each.Add(Ms(start));
        }
        each.Sort();
        return (each.Average(), each[(int)(ticks * 0.99)], each[^1]);
    }

    private static string Row(string what, (double Mean, double P99, double Max) t) => $"| {what} | {t.Mean:F3} | {t.P99:F3} | {t.Max:F3} |";

    /// <summary>The worlds the ship is measured on: the wreck in the big world, and the testbed, every machine at work.</summary>
    private static (World World, Registry M) Ship(string which)
    {
        var m = new Registry();
        var world = which == "testbed" ? Testbed.Create(m.All) : World.Create(1, Registry.Wreck, m.All);
        return (world, m);
    }

    private static Command As(Command c, int peer) => c with { From = peer };

    /// <summary>
    /// A digger's next cell: a staircase down and right from where it stands, then a gallery, a cell of ground at
    /// a time, the hero reported at the cell above it so the next hit is beside it.
    /// </summary>
    private sealed class Digger(int peer, int startX, int startY, int width)
    {
        private int _x = startX, _y = startY + 1;
        public int Peer => peer;

        public IEnumerable<Command> Next(SiteModule site, int siteId)
        {
            var ground = site.GroundOf(siteId)!;
            for (int guard = 0; guard < 64 && ground.WallAt(_y * width + _x) == null; guard++)
            {
                if (_y < startY + 40) _y++;
                else _x = Math.Min(width - 2, _x + 1);
                if (guard % 2 == 1) _x = Math.Min(width - 2, _x + 1);
            }
            yield return As(new Enter((_y - 1) * width + _x), peer);
            yield return As(new Dig(_y * width + _x, "fireAxe"), peer);
        }
    }

    [Fact]
    public void Terrain()
    {
        var made = new List<double>();
        var site = Sim.Site.Terrain.Make(1000);
        for (int i = 0; i < 12; i++)
        {
            long start = Stopwatch.GetTimestamp();
            site = Sim.Site.Terrain.Make((uint)(1000 + i));
            Module.Standalone(new GridModule(), site.Layout);
            if (i > 1) made.Add(Ms(start));
        }
        var again = Sim.Site.Terrain.Make(1011);
        Assert.Equal(site.Layout.Rows, again.Layout.Rows);
        Assert.NotEqual(Sim.Site.Terrain.Make(7).Layout.Rows, again.Layout.Rows);
        var counts = string.Concat(site.Layout.Rows).GroupBy(c => c).OrderBy(g => g.Key).Select(g => $"'{g.Key}' {g.Count()}");
        output.WriteLine($"a {SiteSpec.Width}×{SiteSpec.Height} site made and its grid built: {made.Average():F2} ms mean, {made.Max():F2} max; cells {string.Join(", ", counts)}");

        // What rooms would cost on it, were a site a full world: one flood of the whole ground.
        var grid = Module.Standalone(new GridModule(), site.Layout);
        long flood = Stopwatch.GetTimestamp();
        var (roomOf, rooms) = RoomsModule.Find(grid.Width, grid.Height, grid.StructureLayer.ToArray(), grid.BackLayer.ToArray());
        output.WriteLine($"rooms flooded over it: {Ms(flood):F2} ms, {rooms.Count} rooms (sealed caves), {roomOf.Count(r => r == RoomsModule.Space)} space cells");
        File.WriteAllLines(Path.Combine(Path.GetTempPath(), "stillwake-site-1011.txt"), site.Layout.Rows);
    }

    [Fact]
    public void Ticks()
    {
        output.WriteLine("| world | mean ms | p99 ms | max ms |");
        output.WriteLine("|---|---|---|---|");
        foreach (var which in new[] { "wreck", "testbed" })
        {
            // The ship alone, its crew of four aboard.
            var (world, m) = Ship(which);
            for (int p = 0; p < 4; p++) world.Step([As(new Enter(m.Crew.Spawn), p)]);
            Time(world, 200, _ => []);
            output.WriteLine(Row($"{which}: ship alone, 4 heroes aboard", Time(world, 1000, _ => [])));

            // One hero beams down and digs a cell a hit, a hit every tick (the axe's pace is every 10).
            (world, m) = Ship(which);
            for (int p = 0; p < 4; p++) world.Step([As(new Enter(m.Crew.Spawn), p)]);
            Time(world, 200, _ => []);
            long beam = Stopwatch.GetTimestamp();
            world.Step([As(new Beam(1), 0)]);
            double beamMs = Ms(beam);
            long again = Stopwatch.GetTimestamp();
            world.Step([As(new Beam(0), 0)]);
            world.Step([As(new Beam(1), 0)]);
            double beamAgainMs = Ms(again) / 2;
            int spawn = m.Site.SpawnOf(1), w = SiteSpec.Width;
            var one = new Digger(0, spawn % w, spawn / w, w);
            var t = Time(world, 1000, _ => [.. one.Next(m.Site, 1)]);
            output.WriteLine(Row($"{which}: + 1 hero digging on a site", t));
            int dug = m.Site.Sites[0].Dug.Count;

            // Four heroes on one site, spread out, each digging every tick.
            (world, m) = Ship(which);
            for (int p = 0; p < 4; p++) world.Step([As(new Enter(m.Crew.Spawn), p)]);
            Time(world, 200, _ => []);
            world.Step([.. Enumerable.Range(0, 4).Select(p => As(new Beam(1), p))]);
            spawn = m.Site.SpawnOf(1);
            var four = Enumerable.Range(0, 4).Select(p => new Digger(p, 40 + p * 100, Surface(m.Site.GroundOf(1)!, 40 + p * 100), w)).ToList();
            t = Time(world, 1000, _ => [.. four.SelectMany(d => d.Next(m.Site, 1))]);
            output.WriteLine(Row($"{which}: + 4 heroes digging, spread over one site", t));

            // Four heroes on four sites.
            (world, m) = Ship(which);
            for (int p = 0; p < 4; p++) world.Step([As(new Enter(m.Crew.Spawn), p)]);
            Time(world, 200, _ => []);
            world.Step([.. Enumerable.Range(0, 4).Select(p => As(new Beam(p + 1), p))]);
            var apart = Enumerable.Range(0, 4).Select(p => (Site: p + 1, D: new Digger(p, m.Site.SpawnOf(p + 1) % w, m.Site.SpawnOf(p + 1) / w, w))).ToList();
            t = Time(world, 1000, _ => [.. apart.SelectMany(a => a.D.Next(m.Site, a.Site))]);
            output.WriteLine(Row($"{which}: + 4 heroes digging, one on each of four sites", t));
            output.WriteLine($"{which}: first beam to a site (made then) {beamMs:F2} ms; a beam to a site made already {beamAgainMs:F3} ms; one digger broke {dug} cells in 1000 ticks");
            Assert.True(dug > 50, $"{dug} cells dug");
        }
    }

    private static int Surface(GridModule g, int x)
    {
        int y = 0;
        while (g.StructureAt(y * g.Width + x) == Structure.Empty && y < g.Height - 1) y++;
        return y - 1;
    }

    [Fact]
    public void SavesAndLoads()
    {
        output.WriteLine("| world | save bytes | site slice bytes | load ms | hash ms |");
        output.WriteLine("|---|---|---|---|---|");
        foreach (int dug in new[] { 0, 200, 2000, 10000 })
        {
            var (world, m) = Ship("wreck");
            world.Step([As(new Enter(m.Crew.Spawn), 0)]);
            world.Step([As(new Beam(1), 0)]);
            var ground = m.Site.GroundOf(1)!;
            var walls = Enumerable.Range(0, ground.Width * ground.Height).Where(c => ground.WallAt(c) != null).Take(dug).ToList();
            // Fifty cells a tick, each hit till it breaks, so the suit outlasts the digging.
            foreach (var chunk in walls.Chunk(50))
                world.Step([.. chunk.SelectMany(c => Enumerable.Repeat<Command>(As(new Dig(c, "fireAxe"), 0), (SiteSpec.HpOf(ground.WallAt(c)!.Id) + 1) / 2))]);
            Assert.Equal(dug, m.Site.Sites[0].Dug.Count);
            world.Step([As(new Beam(0), 0)]);
            Report($"wreck + a kept site, {dug} cells dug", world, m);
            var g = m.Site.Sites[0];
            var bitmap = new int[SiteSpec.Width * SiteSpec.Height];
            foreach (int c in g.Dug) bitmap[c] = 1;
            var gaps = g.Dug.Select((c, i) => i == 0 ? c : c - g.Dug[i - 1]);
            output.WriteLine($"  of the slice: dug as a list {System.Text.Json.JsonSerializer.Serialize(g.Dug).Length} B, as gaps {System.Text.Json.JsonSerializer.Serialize(gaps).Length} B, as a bitmap in runs {Runs.Node(bitmap).ToJsonString().Length} B; drops {g.Drops.Count} ({System.Text.Json.JsonSerializer.Serialize(g.Drops).Length} B), hits {g.Hit.Count}");
        }
        var (fresh, fm) = Ship("wreck");
        Report("wreck, no site ever", fresh, fm);

        // Without the seed: the site's ground saved as a grid, layer by layer as runs, as the ship's is.
        var siteGrid = World.Create(1, Sim.Site.Terrain.Make(1011).Layout, [new GridModule()]);
        output.WriteLine($"the same site saved whole as a grid (runs), no seed trick: {siteGrid.Save().Length} bytes");
    }

    private void Report(string what, World world, Registry m)
    {
        string save = world.Save();
        int slice = JsonNode.Parse(save)!["slices"]!["site"]!.ToJsonString().Length;
        var loads = new List<double>();
        for (int i = 0; i < 21; i++)
        {
            long start = Stopwatch.GetTimestamp();
            World.Load(save, new Registry().All);
            if (i > 0) loads.Add(Ms(start));
        }
        var hashes = new List<double>();
        for (int i = 0; i < 21; i++)
        {
            long start = Stopwatch.GetTimestamp();
            world.Hash();
            if (i > 0) hashes.Add(Ms(start));
        }
        output.WriteLine($"| {what} | {save.Length} | {slice} | {loads.Average():F1} | {hashes.Average():F2} |");
    }

    /// <summary>The road not taken: the site as a full world of every module, its ground broken a cell a tick.</summary>
    [Fact]
    public void AFullRegistryOnTheGround()
    {
        var (layout, spawn) = Sim.Site.Terrain.Make(1011);
        var m = new Registry();
        long made = Stopwatch.GetTimestamp();
        var world = World.Create(1, layout, m.All);
        double createMs = Ms(made);
        world.Step([As(new Enter(spawn), 0)]);
        Time(world, 100, _ => []);
        var idle = Time(world, 500, _ => []);
        int w = layout.Width;
        // Under the hero first, then on along the surface: a cell beside the sky every tick, as a digger opens the ground.
        var cells = Enumerable.Range(0, layout.Width * layout.Height).Where(c => m.Grid.WallAt(c) != null).OrderBy(c => c % w).ThenBy(c => c / w).ToList();
        int next = 0;
        var digging = Time(world, 500, _ => [As(new Sim.Hazards.MeteorHit(cells[next++ * 7 % cells.Count]), 0)]);
        output.WriteLine($"the ground as a full world: made in {createMs:F1} ms; {m.Rooms.All.Count} rooms, {m.Debris.Pieces.Count} debris pieces after 1100 ticks");
        output.WriteLine(Row("full registry on the ground, idle", idle));
        output.WriteLine(Row("full registry on the ground, a cell broken a tick", digging));
        output.WriteLine($"save {world.Save().Length} bytes");
    }

    [Fact]
    public void AFreshSiteSavesNothingOnceLeft()
    {
        var kept = new Registry();
        var fresh = new Registry { };
        var modules = fresh.All.Select(x => x.Id == "site" ? new SiteModule(fresh.Crew, fresh.Crew, fresh.Items, fresh.Items) { Kept = false } : x).ToList();
        var world = World.Create(1, Registry.Wreck, modules);
        var site = (SiteModule)modules.Single(x => x.Id == "site");
        world.Step([As(new Enter(fresh.Crew.Spawn), 0)]);
        world.Step([As(new Beam(1), 0)]);
        uint first = site.Sites[0].Seed;
        int spawn = site.SpawnOf(1);
        for (int i = 0; i < 40; i++) world.Step([As(new Dig(spawn + SiteSpec.Width, "fireAxe"), 0)]);
        Assert.NotEmpty(site.Sites[0].Dug);
        world.Step([As(new Beam(0), 0)]);
        Assert.Empty(site.Sites);
        string slice = JsonNode.Parse(world.Save())!["slices"]!["site"]!.ToJsonString();
        output.WriteLine($"a fresh site, left: the slice is {slice}");
        world.Step([As(new Beam(1), 0)]);
        Assert.NotEqual(first, site.Sites[0].Seed);
        _ = kept;
    }

    [Fact]
    public void ASiteNoOneIsOnCostsNothingAndNeedsNoCatchUp()
    {
        // Left for an hour of game time, a kept site is exactly as it was: nothing on it moves with time.
        var (world, m) = Ship("wreck");
        world.Step([As(new Enter(m.Crew.Spawn), 0)]);
        world.Step([As(new Beam(1), 0)]);
        int spawn = m.Site.SpawnOf(1);
        for (int i = 0; i < 40; i++) world.Step([As(new Dig(spawn + SiteSpec.Width, "fireAxe"), 0)]);
        world.Step([As(new Beam(0), 0)]);
        string before = JsonNode.Parse(world.Save())!["slices"]!["site"]!.ToJsonString();
        var idle = Time(world, 600, _ => []);
        for (int i = 0; i < 3600 * 20 - 600; i++) world.Step([]);
        string after = JsonNode.Parse(world.Save())!["slices"]!["site"]!.ToJsonString();
        Assert.Equal(before, after);
        world.Step([As(new Beam(1), 0)]);
        Assert.Equal(spawn, m.Crew.HeroOf(0)!.Cell);
        output.WriteLine($"an hour away: the site's slice unchanged ({before.Length} bytes); the ship's tick meanwhile {idle.Mean:F3} ms");
    }
}
