using System.Diagnostics;
using System.Globalization;
using Stillwake.Sim.Grid;
using Xunit;
using Xunit.Abstractions;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Sim.Tests;

/// <summary>
/// A sim tick on the reference world (CLAUDE.md's budgets: 512×256, the 256×128 drawing doubled to the 8 px cells, 0104, every
/// layer in use), timed with a
/// Stopwatch here, never in the sim. The bounds asserted are a desk's, times <see cref="Slack"/> on CI's slower
/// machines; the output says how far under a tick stays. Timed alone, after the other tests: a CI runner's few
/// cores shared with them made a 2 ms tick read as 10.
/// </summary>
[Collection(nameof(BudgetTests))]
public class BudgetTests(ITestOutputHelper output)
{
    /// <summary>
    /// How many times slower than a desk the machine may be: <c>STILLWAKE_BUDGET_SLACK</c>, which CI sets for its
    /// shared runners, or 1 on a desk, where the bounds are strict.
    /// </summary>
    internal static readonly double Slack =
      double.TryParse(Environment.GetEnvironmentVariable("STILLWAKE_BUDGET_SLACK"), CultureInfo.InvariantCulture, out double s) && s >= 1 ? s : 1;

    /// <summary>A bound of <paramref name="ms"/> on a desk, as this machine allows it.</summary>
    private static double Within(double ms) => ms * Slack;

    /// <summary>
    /// apps/game/src/dev/perf-map.ts: an 8×8 tile repeated, every chunk holding every kind of cell; here
    /// each tile's tank also starts with a canister and is piped to its vent, so every room fills and leaks.
    /// </summary>
    internal static Layout Reference() => Reference(256, 128);

    /// <summary>The reference world as a sim runs it (0104): the drawing doubled, 512×256.</summary>
    internal static Layout ReferenceWorld() => Reference().Doubled();

    /// <summary>
    /// The reference tile over <paramref name="shipWidth"/>×<paramref name="shipHeight"/>, <paramref name="margin"/>
    /// cells of space in from the top-left of a <paramref name="width"/>×<paramref name="height"/> world.
    /// </summary>
    internal static Layout Reference(int width, int height, int shipWidth = 256, int shipHeight = 128, int margin = 0)
    {
        string[] tile =
        [
            "########",
            "#TT.V.-#",
            "#TT.H.v#",
            "#=.DH.-#",
            "#--x-H.#",
            "#.V.dH.#",
            "#==..=.#",
            "########",
        ];
        string[] under = ["", " o --", " ---", "", "", "", "", ""];
        string Row(int y, string[] of) =>
            (y >= margin && y < margin + shipHeight
                ? new string(' ', margin) + string.Concat(Enumerable.Repeat(of[(y - margin) % 8].PadRight(8), shipWidth / 8))
                : "").PadRight(width);
        var rows = Enumerable.Range(0, height).Select(y => Row(y, tile)).ToArray();
        var below = Enumerable.Range(0, height).Select(y => Row(y, under)).ToArray();
        return Layout.Parse(rows, below);
    }

    [Fact]
    public void SavesASmallShipInALargeWorldNoBiggerThanTheShipAlone()
    {
        // Bytes rather than time, so it never flakes: what a save holds is what a hash, a welcome and a load cost.
        string ship = World.Create(1, ReferenceWorld(), new Registry().All).Save();
        string large = World.Create(1, Reference(1024, 512).Doubled(), new Registry().All).Save();
        output.WriteLine($"the ship alone {ship.Length / 1024} KB, in a 2048×1024 world {large.Length / 1024} KB");
        Assert.True(large.Length < ship.Length * 3 / 2, $"{large.Length} bytes for the large world, {ship.Length} for the ship");
    }

    private (double Mean, double Max) Time(World world, int ticks, Func<int, IReadOnlyList<Command>> commands)
    {
        var watch = new Stopwatch();
        double max = 0;
        for (int i = 0; i < ticks; i++)
        {
            var these = commands(i);
            long start = Stopwatch.GetTimestamp();
            watch.Start();
            world.Step(these);
            watch.Stop();
            max = Math.Max(max, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        return (watch.Elapsed.TotalMilliseconds / ticks, max);
    }

    /// <summary>
    /// The first planet (0077): grown from its seed within 15 ms as the surface opens, a tick of it idle with a hero on it,
    /// and what it adds to a save, every cell of it saved whole. A hero digs a shaft down meanwhile, to time a dig too.
    /// </summary>
    [Fact]
    public void GrowsAPlanetQuicklyTicksItIdleCheaplyAndSavesItSmall()
    {
        var type = Content.PlanetTypes.First;
        var ground = type.Ground;
        Terrain.Generate(type, 1);
        var growing = new Stopwatch();
        for (uint seed = 2; seed < 12; seed++)
        {
            growing.Start();
            Terrain.Generate(type, seed);
            growing.Stop();
        }
        double grow = growing.Elapsed.TotalMilliseconds / 10;
        // The first flood of the rooms over the whole ground, caves outside: what the tick it opens on spends on them.
        var floodRooms = new Registry();
        foreach (var mod in floodRooms.All) mod.Create(Terrain.Generate(type, 2));
        floodRooms.Grid.Rebuild();
        var flooding = Stopwatch.StartNew();
        floodRooms.Rooms.Rebuild();
        double flood = flooding.Elapsed.TotalMilliseconds;
        var w = new Scenario(Layout.Parse(["......", ".S....", "######"]));
        int ship = w.World.Save().Length;
        w.Do(new Crew.Enter(w.M.Crew.Spawn)).Run();
        long before = GC.GetTotalMemory(forceFullCollection: true);
        var opening = Time(w.World, 1, _ => [new OpenSurface(Content.PlanetTypes.First.Id)]);
        long memory = GC.GetTotalMemory(forceFullCollection: true) - before;
        w.Do(new Transfer(1)).Run(20);
        int planet = w.World.Save().Length - ship;
        var loading = Stopwatch.StartNew();
        w.Reload();
        double load = loading.Elapsed.TotalMilliseconds;
        Time(w.World, 50, _ => []);
        var idle = Time(w.World, 300, _ => []);
        var m = w.On(1);
        int spawn = m.Crew.Spawn, width = m.Grid.Width;
        // A shaft dug straight down under the spawn with the axe, a hit a tick, every wall broken along with the next one cracked.
        var digging = Time(w.World, 600, t => [HewAt(spawn + (1 + t / 6) * width, width)]);
        int dugSave = w.World.Save().Length - ship;
        output.WriteLine($"planet {ground.Width}×{ground.Height}: grown in {grow:F1} ms mean; opened on a tick of {opening.Mean:F0} ms, the rooms' first flood {flood:F0} ms of it; {memory / (1024 * 1024)} MB held");
        output.WriteLine($"a save {planet} bytes more than the ship's alone, {dugSave} after a shaft of a hundred cells; loaded, ground grown again, in {load:F0} ms");
        output.WriteLine($"idle tick with a hero on it: {idle.Mean:F3} ms mean, {idle.Max:F3} ms max; digging: {digging.Mean:F3} ms mean, {digging.Max:F3} ms max");
        // A desk grows one in about 700 ms.
        Assert.True(grow < Within(1500), $"growing a planet took {grow:F1} ms");
        Assert.True(planet < 512 * 1024, $"a planet saves {planet} bytes");
        Assert.True(idle.Mean < Within(2) && digging.Mean < Within(2), "a planet's tick is over its budget");
    }

    /// <summary>
    /// The first planet with its cap of forty lopers round a hero (0102), let out over its ground and spawned, walking, idling,
    /// struck now and then and fleeing: the tick against the planet's idle one.
    /// </summary>
    [Fact]
    public void KeepsAPlanetsTickWithFortyCreaturesUnderBudget()
    {
        var w = new Scenario(Layout.Parse(["......", ".S....", "######"]));
        w.Do(new Crew.Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Content.PlanetTypes.First.Id)).Run();
        w.Do(new Transfer(1)).Run();
        var m = w.On(1);
        w.Do(new Items.SetCreative(true)).Run();
        var idle = Time(w.World, 300, _ => []);
        int width = m.Grid.Width, spawn = m.Crew.Spawn, row = spawn / width;
        // Over the ground either side of the hero, a body's width apart, a cell over the first wall down any of its columns; let
        // out again till all forty stay a few beats, for on the open plains one wanders out of the hero's reach and goes.
        int body = Content.Creatures.Loper.Body.Width;
        for (int round = 0; round < 4 && m.Creatures.Creatures.Count < Content.Creatures.Max; round++)
        {
            for (int x = spawn % width - 100; m.Creatures.Creatures.Count < Content.Creatures.Max && x < spawn % width + 100; x += body)
            {
                int y = row - 40;
                while (y < row + 40 && Enumerable.Range(x, body).All(c => m.Grid.StructureAt((y + 1) * width + c) != Stillwake.Content.Structure.Wall)) y++;
                w.Do(new Creatures.Release(Content.Creatures.Loper.Id, y * width + x)).Run();
            }
            w.Run(Content.Creatures.Beat * 4);
        }
        Assert.Equal(Content.Creatures.Max, m.Creatures.Creatures.Count);
        var living = Time(w.World, 600, t => t % 10 == 0 && m.Creatures.Creatures.Count > 0
            ? [new Creatures.Strike(m.Creatures.Creatures[t / 10 % m.Creatures.Creatures.Count].Id, Content.Weapons.Axe.Item)]
            : []);
        output.WriteLine($"the first planet idle {idle.Mean:F3} ms a tick; with {Content.Creatures.Max} creatures {living.Mean:F3} ms mean, {living.Max:F3} ms max");
        Assert.True(living.Mean < Within(2), $"a planet's tick with its creatures took {living.Mean:F3} ms");
        // The creatures' own share: some 0.12 ms for forty on a desk (2026-10-26, Release); a rule gone slow shows here first.
        Assert.True(living.Mean - idle.Mean < Within(0.4), $"forty creatures took {living.Mean - idle.Mean:F3} ms of the tick");
    }

    /// <summary>
    /// Ten thousand cracks at once, the worst case (0077): hit in descending cell order, so every one is inserted at the
    /// list's head; what they hold, their save and hash, and the tick that sweeps them. Healing keeps real counts tiny.
    /// </summary>
    [Fact]
    public void KeepsTenThousandCracksCheap()
    {
        const int width = 128, height = 80, count = width * height;
        var rows = Enumerable.Repeat(new string('#', width), height).ToArray();
        var w = new Scenario(Layout.Parse(rows).WithMaterials(string.Join("\n", Enumerable.Repeat(new string('t', width), height))).OnPlanet());
        int bare = w.World.Save().Length;
        long before = GC.GetTotalMemory(forceFullCollection: true);
        var hitting = Stopwatch.StartNew();
        w.World.Step([.. Enumerable.Range(0, count).Reverse().Select(c => (Command)HewAt(c, width))]);
        double hit = hitting.Elapsed.TotalMilliseconds;
        long held = GC.GetTotalMemory(forceFullCollection: true) - before;
        Assert.Equal(count, w.M.Building.Cracks.Count);
        int saved = w.World.Save().Length - bare;
        var hashing = Stopwatch.StartNew();
        w.World.Hash();
        double hash = hashing.Elapsed.TotalMilliseconds;
        var sweep = Time(w.World, 20, _ => []);
        output.WriteLine($"{count} cracks: hit in one tick {hit:F1} ms (every insert at the head); {held / 1024} KB held; {saved / 1024} KB more in a save; a hash {hash:F1} ms; a tick sweeping them {sweep.Mean:F2} ms");
        Assert.True(sweep.Mean < Within(2), $"a sweep of {count} cracks took {sweep.Mean:F2} ms");
    }

    /// <summary>The reference ship alone, and in the corner of a world of space: rooms are found again only around a door, so the space costs nothing.</summary>
    [Theory]
    [InlineData(512, 256)]
    [InlineData(2048, 1024)]
    public void KeepsASimTickWithTheReferenceShipUnderBudget(int width, int height)
    {
        var m = new Registry();
        var world = World.Create(1, Reference(width / 2, height / 2).Doubled(), m.All);
        Assert.True(m.Rooms.All.Count > 500);
        // Filling first, so the timed ticks fill, leak and survey hundreds of rooms.
        Time(world, 100, _ => []);
        var still = Time(world, 300, _ => []);
        Assert.True(m.Atmosphere.AiredCells.Count > 20000);
        // A door opened and shut every tick: rooms found again, orders and leaks with them.
        var door = Enumerable.Range(0, width * height).First(c => m.Grid.StructureAt(c) == Structure.DoorClosed);
        var toggling = Time(world, 300, t => [new SetDoor(door, t % 2 == 0)]);
        // The same without atmosphere, for its share of the tick.
        var bare = new Registry();
        var without = World.Create(1, Reference(width / 2, height / 2).Doubled(), bare.All.Where(x => x != bare.Atmosphere).ToList());
        Time(without, 100, _ => []);
        var others = Time(without, 300, _ => []);
        var othersToggling = Time(without, 300, t => [new SetDoor(door, t % 2 == 0)]);
        output.WriteLine($"{width}×{height}: rooms {m.Rooms.All.Count}, aired cells {m.Atmosphere.AiredCells.Count}");
        output.WriteLine($"without atmosphere: {others.Mean:F3} ms mean, {othersToggling.Mean:F3} ms toggling a door");
        output.WriteLine($"tick: {still.Mean:F3} ms mean, {still.Max:F3} ms max");
        output.WriteLine($"tick toggling a door: {toggling.Mean:F3} ms mean, {toggling.Max:F3} ms max");
        Assert.True(still.Mean < Within(2) && toggling.Mean < Within(2), "a tick is over its budget");
    }

    /// <summary>
    /// The reference ship in a world of space, or the whole of one built, a hull wall beside space taken
    /// apart and built again every tick, and a wall built against the hull out in space and taken apart
    /// again: rooms are found again only round the change, so neither the world nor the ship costs anything.
    /// </summary>
    [Theory]
    [InlineData(2048, 1024, 512, 256, 16)]
    [InlineData(2048, 1024, 512, 256, 400)]
    [InlineData(2048, 1024, 2048, 1024, 0)]
    public void KeepsABreachBesideSpaceAndAWallBuiltOutsideAsCheapInALargeWorldAsInASmallShip(int width, int height, int shipWidth, int shipHeight, int margin)
    {
        // Drawn at half each way and doubled (0104): margins and cells in the drawing's characters.
        var w = new Scenario(Reference(width / 2, height / 2, shipWidth / 2, shipHeight / 2, margin / 2), doubled: true);
        // The hull's top wall is two cells thick: a breach is both, a column through it.
        int wall = w.At(margin / 2 + 9, margin / 2), under = wall + w.M.Grid.Width;
        Assert.Equal((Structure.Wall, Structure.Wall), (w.M.Grid.StructureAt(wall), w.M.Grid.StructureAt(under)));
        Time(w.World, 100, _ => []);
        // What a tick costs anyway, the air of every room in it: a change may add a little, not the ship or the world.
        var still = Time(w.World, 100, _ => []);
        double bound = 4 * still.Mean + 1;
        IReadOnlyList<Command> Breach(int i) =>
            i % 2 == 0 ? [new ClearAt(wall, "structure"), new ClearAt(under, "structure")] : [new Place(wall, Placement.Wall()), new Place(under, Placement.Wall())];
        Time(w.World, 10, Breach);
        var breaching = Time(w.World, 100, Breach);
        Assert.Equal(Structure.Wall, w.M.Grid.StructureAt(wall));
        output.WriteLine($"{width}×{height}, the reference ship {shipWidth}×{shipHeight} {margin} cells in: rooms {w.M.Rooms.All.Count}");
        output.WriteLine($"tick: {still.Mean:F3} ms mean; breaching and mending a hull wall: {breaching.Mean:F3} ms mean, {breaching.Max:F3} ms max");
        // Against the tick on the same machine, for CI's shared runners: the whole-world flood took 105 ms
        // locally, the ship's 11 ms on a fully built 1024×512.
        Assert.True(breaching.Mean < bound, "a breach costs the ship or the world");
        if (margin == 0) return;
        // A cell of space it closes, whose space must be walked to know it still reaches the outside.
        int outside = wall - w.M.Grid.Width;
        IReadOnlyList<Command> Build(int i) => [i % 2 == 0 ? new Place(outside, Placement.Wall()) : new ClearAt(outside, "structure")];
        var building = Time(w.World, 100, Build);
        output.WriteLine($"tick building and taking apart a wall outside the hull: {building.Mean:F3} ms mean, {building.Max:F3} ms max");
        Assert.True(building.Mean < bound, "a wall built beside space costs the ship or the world");
    }

    /// <summary>
    /// The wreck's big world with 600 pieces of the far field out round four heroes far apart (0052), under
    /// its cap so its beat runs every tick: every tick drifts them all and tests each against every hero's
    /// boxes, and counts each hero's share, so the tick grows with pieces times heroes.
    /// </summary>
    [Fact]
    public void KeepsATickWithSixHundredPiecesRoundFourHeroesUnderBudget()
    {
        const int Out = 600;
        var around = (Content.AroundHeroes)Content.Debris.Far.Anchor;
        Assert.True(Out < Content.Debris.Far.Max);
        (int X, int Y)[] heroes = [(500, 250), (1550, 250), (500, 800), (1550, 800)];
        Scenario HeroesOut()
        {
            var w = new Scenario(Registry.Wreck);
            for (int i = 0; i < heroes.Length; i++) w.Do(new Sim.Crew.Enter(heroes[i].Y * w.M.Grid.Width + heroes[i].X) { From = i });
            return w.Run();
        }
        var bare = new Scenario(Registry.Wreck).Run();
        var w = HeroesOut();
        var debris = (Sim.Debris.DebrisState)w.M.Debris.Slice;
        var rng = new Rng(new RngState { S = 51 });
        for (int i = 0; i < Out; i++)
        {
            var (hx, hy) = heroes[i % heroes.Length];
            int x = (hx - around.Keep.X + (int)rng.Below((uint)(2 * around.Keep.X + 1))) * 1000 + 500, y = (hy - around.Keep.Y + (int)rng.Below((uint)(2 * around.Keep.Y + 1))) * 1000 + 500;
            debris.Pieces.Add(new Sim.Debris.Piece
            {
                Id = debris.NextId++,
                Zone = Content.Debris.Far.Id,
                Item = "scrapChunk",
                X = x,
                Y = y,
                Speed = Content.Debris.DriftMin + (int)rng.Below(Content.Debris.DriftMax - Content.Debris.DriftMin + 1),
                Start = x,
                End = 0,
                SeenAt = debris.Clock,
            });
        }
        Time(bare.World, 50, _ => []);
        Time(w.World, 50, _ => []);
        var without = Time(bare.World, 300, _ => []);
        var with = Time(w.World, 300, _ => []);
        output.WriteLine($"the wreck's world, no hero out: {without.Mean:F3} ms mean, {without.Max:F3} ms max");
        output.WriteLine($"with {Out} pieces round them ({w.M.Debris.Pieces.Count} out after): {with.Mean:F3} ms mean, {with.Max:F3} ms max");
        Assert.True(w.M.Debris.Pieces.Count >= Out, "the pieces were let go");
        Assert.True(with.Mean < Within(2), "a tick is over its budget");
    }

    /// <summary>
    /// A thousand furnaces and a thousand trade stations at work, in aired rooms of their own: each finds its
    /// machine, slots and records at once, so the tick grows with the machines, not with their square.
    /// </summary>
    [Fact]
    public void KeepsAThousandFurnacesAndTradeStationsAtWorkUnderBudget()
    {
        string[] tile = ["########", "#FF.$$.#", "#FF.$$.#", "#......#", "#.TT.V.#", "#.TT...#", "#......#", "########"];
        string[] under = ["", "", "", "", "  o  -", "  ----", "", ""];
        string[] Rows(string[] of) => [.. Enumerable.Range(0, 256).Select(y => string.Concat(Enumerable.Repeat(of[y % 8].PadRight(8), 32)))];
        // Knowing little, so the rule's walk is timed too: the furnaces move what it follows every tick.
        var w = new Scenario(Layout.Parse(Rows(tile), Rows(under)) with { Known = Content.Research.StartKnown }, doubled: true);
        foreach (var m in w.M.Grid.Machines)
        {
            if (m.Kind == "furnace") w.FillCrate(m.Id, ("scrapChunk", 10), ("planks", 50));
            if (m.Kind == "tradeStation") w.FillCrate(m.Id, ("scrap", 100));
        }
        // Aired first, so the timed ticks burn, smelt and breathe.
        Time(w.World, 100, _ => []);
        var ticks = Time(w.World, 300, _ => []);
        var furnaces = w.M.Grid.Machines.Where(m => m.Kind == "furnace").ToList();
        int burning = furnaces.Count(m => w.M.Furnace.At(m.Id).Burning);
        int selling = w.M.Grid.Machines.Count(m => m.Kind == "tradeStation" && w.M.Trade.Worth(m.Id) > 0);
        output.WriteLine($"{w.M.Grid.Machines.Count} machines, {burning} of {furnaces.Count} furnaces burning, {selling} trade stations selling");
        output.WriteLine($"tick: {ticks.Mean:F3} ms mean, {ticks.Max:F3} ms max");
        Assert.True(burning > 1000 && selling > 1000, "the machines stand idle");
        // Two thousand machines, more than a world will hold: twice the budget, where a desk takes about 2 ms.
        Assert.True(ticks.Mean < Within(4), "a tick is far over its budget");
    }

    /// <summary>
    /// The rule's walk (docs/plans/board.md) at its dearest: nothing known yet, every part looked up, and something counted before
    /// every pass, as a world of busy furnaces has. Timed by itself: its share is too small to tell from a whole tick's noise.
    /// </summary>
    [Fact]
    public void TheRulesWalkTakesAFractionOfATick()
    {
        var w = new Scenario(Layout.Parse(["##########", "#II......#", "##########"], null) with { Known = [] }, doubled: true).Run();
        const int Passes = 2000;
        var clock = Stopwatch.StartNew();
        for (int i = 0; i < Passes; i++)
        {
            w.M.Knowledge.Obtain("scrapChunk", 1);
            w.M.Research.Walk();
        }
        clock.Stop();
        double mean = clock.Elapsed.TotalMilliseconds / Passes;
        output.WriteLine($"a walk: {mean:F4} ms mean");
        Assert.True(mean < Within(0.2), $"a walk takes {mean:F4} ms");
    }

    /// <summary>
    /// Drones (0062): four bays of four drones in one aired hall with 43 machines, furnaces along it burning and smelting
    /// chunks from crates of chunks and planks, so their inputs want refilling and their scrap carrying off the whole time:
    /// every bay floods its zone each look, every drone looks again from where it is after each job and finds its path
    /// from each cell it enters.
    /// </summary>
    [Fact]
    public void KeepsFourBaysOfFourDronesAmongFortyMachinesUnderBudget()
    {
        const int W = 72, H = 16;
        var rows = Enumerable.Range(0, H).Select(y => (y == 0 || y == H - 1 ? new string('#', W) : "#" + new string('.', W - 2) + "#").ToCharArray()).ToArray();
        var under = Enumerable.Range(0, H).Select(_ => new string(' ', W).ToCharArray()).ToArray();
        void Stamp(char c, int x, int y, int w, int h)
        {
            for (int dy = 0; dy < h; dy++)
                for (int dx = 0; dx < w; dx++) rows[y + dy][x + dx] = c;
        }
        foreach (int x in new[] { 6, 24, 42, 60 })
        {
            Stamp('R', x, 12, 2, 2);
            Stamp('T', x + 4, 12, 2, 2);
            Stamp('V', x + 7, 13, 1, 1);
            under[12][x + 4] = 'o';
            for (int px = x + 4; px <= x + 7; px++) under[13][px] = '-';
        }
        for (int k = 0; k < 17; k++) Stamp('K', 3 + 4 * k, 3, 2, 1);
        for (int k = 0; k < 11; k++) Stamp('F', 4 + 6 * k, 7, 2, 2);
        foreach (int x in new[] { 10, 30, 50 }) Stamp('K', x, 10, 2, 1);
        var w = new Scenario(Layout.Parse([.. rows.Select(r => new string(r))], [.. under.Select(r => new string(r))]), doubled: true);
        Assert.True(w.M.Grid.Machines.Count >= 40);
        foreach (var bay in w.M.Grid.MachinesOf(Content.Logistics.Bay)) LogisticsTests.Dock(w, bay.Id, 4);
        var crates = w.M.Grid.MachinesOf("crate").Where(c => c.Cell == w.At(c.Cell % w.M.Grid.Width / 2, 3)).ToList();
        for (int k = 0; k < crates.Count; k += 2)
            w.FillCrate(crates[k].Id, [.. Enumerable.Repeat(("scrapChunk", 10), 12), .. Enumerable.Repeat(("planks", 50), 4)]);
        foreach (var f in w.M.Grid.MachinesOf("furnace")) w.Put(0, new Items.Stack("scrapChunk", 1), crate: f.Id).Put(1, new Items.Stack("planks", 10), crate: f.Id);
        // Aired, and the drones under way, before the timed ticks.
        w.Run(600);
        int flying = 0;
        var ticks = Time(w.World, 600, i =>
        {
            flying += w.M.Logistics.Drones.Count;
            return [];
        });
        output.WriteLine($"{w.M.Grid.Machines.Count} machines, {w.M.Logistics.Drones.Count} drones flying, {flying / 600.0:F1} on average");
        output.WriteLine($"tick: {ticks.Mean:F3} ms mean, {ticks.Max:F3} ms max");
        Assert.True(flying > 600 * 8, "the drones stand docked");
        // A desk takes about 1.3 ms.
        Assert.True(ticks.Mean < Within(4), "a tick is far over its budget");
    }

    /// <summary>The axe hit from inside <paramref name="cell"/>, aimed up: it strikes that cell's wall alone.</summary>
    private static Building.Hew HewAt(int cell, int width) =>
        new(Content.Weapons.Axe.Item, cell % width * 1000 + 500, cell / width * 1000 + 500, HewTesting.Up);
}

/// <summary>The budget tests run with no other test beside them, so their timings are the sim's alone.</summary>
[CollectionDefinition(nameof(BudgetTests), DisableParallelization = true)]
public class BudgetTestsAlone;
