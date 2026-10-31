using System.Text.Json.Nodes;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Rooms;
using Xunit;
using Xunit.Abstractions;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Net.Tests;

/// <summary>packages/net/src/desync.test.ts, ported.</summary>
public class DesyncTests(ITestOutputHelper output)
{
    public static readonly TheoryData<uint> Seeds = new() { 1u, 2u, 3u, 42u, 2026u };

    /// <summary>
    /// How many seeds <see cref="Covering"/> tries, a hundred apart, before it gives up: the stations' run, the least lucky,
    /// covered on 22 seeds of 40, so all twelve miss about once in 15,000 shifts of the stream.
    /// </summary>
    private const int CoverTries = 12;

    /// <summary>
    /// Fuzzes seed <paramref name="first"/>, then the next a hundred on, until <paramref name="fuzz"/> says a run reached what
    /// the test needs (nothing missed): a change that shifts the random stream costs a try, not a seed bumped by hand. Every
    /// run tried must stay in sync.
    /// </summary>
    private void Covering(uint first, Func<uint, (DesyncRun Run, string? Missed)> fuzz)
    {
        var missed = new List<string>();
        for (uint k = 0; k < CoverTries; k++)
        {
            uint seed = first + 100 * k;
            var (run, miss) = fuzz(seed);
            Assert.True(run.Desync == null, $"seed {seed} desynced at tick {run.Desync?.Tick}");
            if (miss == null)
            {
                output.WriteLine(missed.Count == 0 ? $"covered on seed {seed}" : $"covered on seed {seed}, after {string.Join("; ", missed)}");
                return;
            }
            missed.Add($"seed {seed}: {miss}");
        }
        Assert.Fail($"no seed covered it: {string.Join("; ", missed)}");
    }

    // The game.

    [Theory]
    [MemberData(nameof(Seeds))]
    public void KeepsEveryClientInSyncOnTheStartingWreck(uint seed)
    {
        var run = Desync.Fuzz(Definition.Game(), seed, layout: Registry.Wreck);
        Assert.Null(run.Desync);
        Assert.Equal(600, run.Ticks);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void KeepsEveryClientInSyncOnAShipWithPowerAndPipedAir(uint seed)
    {
        var run = Desync.Fuzz(Definition.Game(), seed, layout: PoweredWreck.Layout, joinAt: 300);
        Assert.Null(run.Desync);
        Assert.Equal(600, run.Ticks);
    }

    /// <summary>What the run above is for: its ship's cables and pipes join what they should.</summary>
    [Fact]
    public void ItsShipStartsWithItsGeneratorPoweredAndItsVentFed()
    {
        var m = new Registry();
        World.Create(1, PoweredWreck.Layout, m.All);
        var byKind = m.Grid.Machines.ToDictionary(x => x.Kind);
        Assert.Contains(m.Power.Networks, n => n.Demand > 0);
        Assert.True(m.Atmosphere.Fed(byKind["vent"].Id));
    }

    [Fact]
    public void KeepsTheFarFieldInSyncRoundHeroesEnteredAnywhere()
    {
        Covering(3, tried =>
        {
            var game = Definition.Game();
            int far = 0;
            var run = Desync.Fuzz(game, tried, layout: Registry.Wreck, tamper: (_, clients) =>
                far = Math.Max(far, game.Module<DebrisModule>(clients[0].World).Pieces.Count(p => p.Zone == Content.Debris.Far.Id)));
            return (run, far > 0 ? null : "no far piece spawned");
        });
    }

    [Fact]
    public void KeepsAClientThatJoinsMidRunInSyncWithTheOthers()
    {
        int count = 0;
        var run = Desync.Fuzz(Definition.Game(), 4, layout: Registry.Wreck, joinAt: 300,
            tamper: (_, clients) => count = clients.Count);
        Assert.Null(run.Desync);
        Assert.Equal(4, count);
    }

    [Theory]
    [InlineData(5u)]
    [InlineData(6u)]
    public void KeepsEveryClientAndAJoinerInSyncAsHeroesMoveBetweenSurfaces(uint seed)
    {
        // The host and three clients, a fourth joining once strips are open and heroes on them, the world's commands drawn
        // often: every surface is in every hash, the joiner's welcome carries every one.
        Covering(seed, tried =>
        {
            int surfaces = 0, away = 0, count = 0;
            var run = Desync.Fuzz(Definition.Game(), tried, layout: Registry.Wreck, joinAt: 300, worldEvery: 8, tamper: (_, clients) =>
            {
                var world = clients[0].World;
                surfaces = Math.Max(surfaces, world.Surfaces.Count);
                away += clients.Count(c => world.SurfaceOf(c.Id) != 0);
                count = clients.Count;
            });
            if (run.Desync == null) Assert.Equal(4, count);
            return (run, surfaces >= 3 && away > 100 ? null : $"{surfaces} surfaces at most, a client off the ship on {away} ticks in all");
        });
    }

    [Fact]
    public void KeepsEveryClientInSyncDiggingTheGroundOfSmallPlanets()
    {
        // Small planets opened by the world's fuzz, heroes beamed down to them, their ground grown on every peer from one
        // seed and dug there (0077): walls cracked, broken out, their soil and rock given or spilt, plants felled (0079), alike on
        // every peer. Digs are few in a fuzz, so the first client also digs on its own: beamed down to the first small planet,
        // opened if the fuzz has not, it swings the axe at the topmost soil or stone and the first plant it may hit.
        string planet = Content.PlanetTypes.Small.Id, axe = Content.Weapons.Axe.Item;
        static bool Ground(Content.Material? wall) => wall != null && wall.Cost.Any(c => c.Item is "soil" or "rock");
        Covering(19, tried =>
        {
            int planets = 0, cracked = 0, broke = 0, dug = 0, felled = 0, wall = -1;
            var run = Desync.Fuzz(Definition.Game(), tried, layout: Registry.Wreck, ticks: 1800, worldEvery: 6, tamper: (tick, clients) =>
            {
                var world = clients[0].World;
                foreach (var surface in world.Surfaces.Where(s => s.Kind == planet))
                {
                    var m = Registry.On(world, surface.Id);
                    cracked = Math.Max(cracked, m.Building.Cracks.Count);
                    int got = m.Items.Floor.Where(s => s.Item is "soil" or "rock").Sum(s => s.Count)
                        + clients.Sum(c => m.Items.Totals(c.Id).Where(t => t.Item is "soil" or "rock").Sum(t => t.Count));
                    dug = Math.Max(dug, got);
                    felled = Math.Max(felled, Terrain.Generate(Content.PlanetTypes.Small, surface.Seed).Flora!.Count - m.Building.Plants.Count);
                }
                planets = Math.Max(planets, world.Surfaces.Count(s => s.Kind == planet));

                var first = clients[0];
                if (tick % 10 != 0) return;
                if (world.Surfaces.FirstOrDefault(s => s.Kind == planet) is not { } down)
                {
                    if (tick % 50 == 0) first.Send(new OpenSurface(planet));
                    return;
                }
                if (world.SurfaceOf(first.Id) != down.Id)
                {
                    first.Send(new Transfer(down.Id));
                    return;
                }
                var on = Registry.On(world, down.Id);
                // A hit from inside the cell, aimed up: a wall there is struck alone, a plant from its own cell.
                Hew HewAt(int cell) => new(axe, cell % on.Grid.Width * 1000 + 500, cell / on.Grid.Width * 1000 + 500, 192);
                bool Hits(int cell) => HewAt(cell) is var h && on.Building.HewProblem(first.Id, axe, h.X, h.Y, h.Heading) == null;
                if (tick % 20 == 0)
                {
                    if (wall >= 0 && on.Grid.WallAt(wall) == null) broke++;
                    if (wall < 0 || !Ground(on.Grid.WallAt(wall)) || !Hits(wall))
                        wall = Enumerable.Range(0, on.Grid.Width * on.Grid.Height).FirstOrDefault(c => Ground(on.Grid.WallAt(c)) && Hits(c), -1);
                    if (wall >= 0) first.Send(HewAt(wall));
                }
                else if (on.Building.Plants.FirstOrDefault(p => on.Grid.WallAt(p.Cell) == null && Hits(p.Cell)) is { } plant)
                    first.Send(HewAt(plant.Cell));
            });
            return (run, planets >= 1 && cracked >= 1 && broke >= 1 && dug >= 1 && felled >= 1 ? null
                : $"{planets} small planets, {cracked} cracks at most, {broke} walls broken out, {dug} soil and rock dug, {felled} plants felled");
        });
    }

    [Fact]
    public void KeepsEveryClientAndAJoinerInSyncWithASentryOnAWreck()
    {
        // The sentry (2026-10-26-creatures-are-composed): the first client lets one out on a wreck of the stream, stands its hero
        // by it, strikes it, harpoons its wreck and reports its pounces' hits; the fuzz strikes and releases besides. A joiner
        // comes in mid-fight, Mem, Target and the wreck's blow in its welcome.
        string sentry = Content.Creatures.Sentry.Id, axe = Content.Weapons.Axe.Item;
        Covering(42, tried =>
        {
            int alive = 0, pounced = 0, cut = 0;
            var game = Definition.Game();
            var run = Desync.Fuzz(game, tried, layout: Registry.Wreck, ticks: 1500, joinAt: 700, tamper: (tick, clients) =>
            {
                var first = clients[0];
                var m = Registry.On(first.World, 0);
                var sentries = m.Creatures.Creatures.Where(c => c.Species == sentry).ToList();
                alive += sentries.Count;
                pounced += sentries.Count(c => c.Posture == Sim.Creatures.Posture.Charge);
                cut += sentries.Count(c => Sim.Creatures.CreaturesModule.LastCut(c) > 0);
                if (tick == 1) first.Send(new SetCreative(true));
                foreach (var c in sentries.Where(c => c.Posture == Sim.Creatures.Posture.Charge))
                    first.Send(new Sim.Creatures.HitBy(c.Id));
                if (tick % 10 != 0) return;
                int width = m.Grid.Width;
                if (sentries.Count == 0)
                {
                    if (m.Debris.Pieces.FirstOrDefault(p => Content.Wreckage.SizeOf(p.Item) != null) is { } wreck)
                        first.Send(new Sim.Creatures.Release(sentry, DebrisModule.CellOf(wreck.X, wreck.Y, width)));
                    return;
                }
                var s = sentries[0];
                int cx = s.X / 1000, cy = s.Y / 1000;
                if (tick % 60 == 0) first.Send(new Enter(Math.Clamp(cy - 1, 0, m.Grid.Height - 1) * width + Math.Clamp(cx - 6, 0, width - 1)));
                if (tick % 90 == 30) first.Send(new Sim.Creatures.Strike(s.Id, axe));
                if (tick % 130 == 70 && m.Creatures.ClingsTo(s) is int piece && m.Debris.PieceById(piece) is { } held)
                    first.Send(new Reel(piece, held.X - 8000, held.Y));
            });
            return (run, alive >= 300 && pounced >= 1 && cut >= 1 ? null : $"{alive} sentry-ticks, {pounced} pouncing, {cut} after a cut");
        });
    }

    [Fact]
    public void KeepsEveryClientAndAJoinerInSyncWithCreaturesOnSmallPlanets()
    {
        // Creatures on small planets (0102): the first client beamed down lets lopers out over the ground by its hero and
        // strikes them with the axe, so they are knocked, flee, wander, die into their yields and are gone with the last hero,
        // alike on every peer and in a joiner's welcome; the fuzz releases and strikes besides.
        string planet = Content.PlanetTypes.Small.Id, axe = Content.Weapons.Axe.Item, loper = Content.Creatures.Loper.Id;
        Covering(31, tried =>
        {
            int most = 0, fled = 0, killed = 0, charged = 0;
            var run = Desync.Fuzz(Definition.Game(), tried, layout: Registry.Wreck, ticks: 1200, joinAt: 600, worldEvery: 6, tamper: (tick, clients) =>
            {
                var world = clients[0].World;
                foreach (var surface in world.Surfaces.Where(s => s.Kind == planet))
                {
                    var m = Registry.On(world, surface.Id);
                    most = Math.Max(most, m.Creatures.Creatures.Count);
                    fled += m.Creatures.Creatures.Count(c => c.Posture == Sim.Creatures.Posture.Flee);
                    charged += m.Creatures.Creatures.Count(c => c.Posture == Sim.Creatures.Posture.Charge);
                    killed = Math.Max(killed, m.Items.Floor.Where(s => s.Item == "biomass").Sum(s => s.Count));
                }
                var first = clients[0];
                if (tick % 10 != 0) return;
                if (world.Surfaces.FirstOrDefault(s => s.Kind == planet) is not { } down)
                {
                    if (tick % 50 == 0) first.Send(new OpenSurface(planet));
                    return;
                }
                if (world.SurfaceOf(first.Id) != down.Id)
                {
                    first.Send(new Transfer(down.Id));
                    return;
                }
                var on = Registry.On(world, down.Id);
                int width = on.Grid.Width, x = 4 + tick / 10 % (width - 8);
                if (tick % 30 == 0)
                {
                    int y = 2;
                    while (y < on.Grid.Height - 2 && on.Grid.StructureAt((y + 1) * width + x) == Structure.Empty) y++;
                    first.Send(new Sim.Creatures.Release(loper, y * width + x));
                }
                else if (on.Creatures.Creatures.Count > 0)
                    first.Send(new Sim.Creatures.Strike(on.Creatures.Creatures[tick / 10 % on.Creatures.Creatures.Count].Id, axe));
            });
            return (run, most >= 3 && fled >= 1 && charged >= 1 && killed >= 1 ? null
                : $"{most} creatures at most, {fled} creature-ticks fleeing, {charged} charging, {killed} biomass dropped");
        });
    }

    [Fact]
    public void KeepsAJoinerInSyncWhileRoomsAreFoundAgainAroundDoorsFarFromSpace()
    {
        // Rooms closed by doors three cells and more from space: toggled by the fuzz, they are found
        // again around the door, and the air follows only those rooms.
        string[] ship =
        [
            "#####################",
            "#V.....#.....#.....V#",
            "#TT....D.....D....TT#",
            "#TT....#..H..#....TT#",
            "###D#######D#####D###",
            "#.....V#.....#......#",
            "#......D.....D......#",
            "#......#.....#......#",
            "#####################",
        ];
        string[] pipes = ["", " -                 -", " -o               --", " -                -o"];
        string Pad(string row) => ("   " + row).PadRight(27);
        var blank = Enumerable.Repeat("", 3);
        var layout = Layout.Parse(
            [.. blank.Concat(ship).Concat(blank).Select(Pad)],
            [.. blank.Concat(pipes).Select(Pad)]).Doubled();
        // On a stream of modules' commands alone: the world's are drawn in KeepsEveryClientAndAJoinerInSyncAsHeroesMoveBetweenSurfaces.
        Covering(7, tried =>
        {
            var game = Definition.Game();
            var seen = new Dictionary<Client, int>();
            int around = 0, aired = 0;
            var run = Desync.Fuzz(game, tried, layout: layout, joinAt: 150, ticks: 400, worldEvery: 0, tamper: (_, clients) =>
            {
                aired = Math.Max(aired, game.Module<AtmosphereModule>(clients[0].World).AiredCells.Count);
                foreach (var client in clients)
                {
                    var rooms = game.Module<RoomsModule>(client.World);
                    if (seen.TryGetValue(client, out int version) && rooms.ChangedSince(version) is { Count: > 0 }) around++;
                    seen[client] = rooms.Version;
                }
            });
            return (run, around > 10 && aired > 50 ? null : $"rooms found around a change {around} times, {aired} cells aired at most");
        });
    }

    [Fact]
    public void KeepsEveryClientInSyncWhileAirFillsLeaksAndVentsAndHeroesBreatheIt()
    {
        // A room with a vent above a tank and the spawn beside them. The first client pipes them and
        // loads the tank through the pipeline, then the fuzz plays on past two leak beats, breaching
        // walls and moving heroes in and out as it goes, one kept outside till it passes out.
        var room = Layout.Parse([
            ".............",
            ".###########.",
            ".#V..S.....#.",
            ".#TT.......#.",
            ".#TT.......#.",
            ".###########.",
            ".............",
        ]).Doubled();
        // The cell of the drawing's character at (x, y): its block's top-left, doubled (0104).
        static int Cell(int x, int y) => 2 * y * 26 + 2 * x;
        const int tank = 2;
        Covering(9, tried =>
        {
            int aired = 0, peak = 0;
            bool drawn = false, refilled = false, woke = false;
            var suits = new Dictionary<int, (int Suit, int Out)>();
            var game = Definition.Game();
            var run = Desync.Fuzz(game, tried, layout: room,
                // Past two leak beats: an aired room leaks once a minute, 1200 ticks.
                ticks: 2600,
                tamper: (tick, clients) =>
                {
                    var first = clients[0];
                    // Building and loading are free, whatever creative mode the fuzz switched to meanwhile.
                    if (tick <= 40) first.Send(new SetCreative(true));
                    if (tick < 40)
                    {
                        // The pipe laid again while missing, from the vent's port down to the tank's (0109): the fuzz may cut it meanwhile.
                        int from = Cell(2, 2), to = Cell(2, 2) + 4 * 26;
                        if (!game.Module<PipesModule>(first.World).Spans.Any(s => s.A == from && s.B == to)) first.Send(new LayPipe(from, to));
                    }
                    else if (tick == 40)
                    {
                        for (int i = 0; i < 3; i++) first.Send(new LoadCanister(tank));
                    }
                    // One hero is kept out in space, whatever the fuzz does with it, until a hero passes out and wakes.
                    if (clients.Count > 1 && tick > 40 && !woke) clients[1].Send(new Enter(Cell(0, 0)));
                    var air = game.Module<AtmosphereModule>(first.World);
                    aired = Math.Max(aired, air.AiredCells.Count);
                    int oxygen = air.OxygenIn(tank);
                    if (oxygen < peak) drawn = true;
                    peak = Math.Max(peak, oxygen);
                    foreach (var h in game.Module<CrewModule>(first.World).Heroes)
                    {
                        if (suits.TryGetValue(h.Id, out var before))
                        {
                            if (before.Out == 0 && h.Out == 0 && h.Air > before.Suit) refilled = true;
                            if (before.Out > 0 && h.Out == 0) woke = true;
                        }
                        suits[h.Id] = (h.Air, h.Out);
                    }
                });
            // Breathing ran both ways: a suit refilled from room air, and a hero passed out and woke.
            return (run, aired > 0 && drawn && refilled && woke ? null
                : $"{aired} cells aired at most, the tank drawn: {drawn}, a suit refilled: {refilled}, a hero woke: {woke}");
        });
    }

    [Fact]
    public void KeepsEveryClientInSyncWhileBatteriesChargeAndMoveBetweenBeltsAndAStation()
    {
        // A panel out in space cabled to a charging station by the lifepod, each peer carrying a drill, the second's fitted with
        // the arc (0098), and batteries of every charge. The fuzz runs the drills and moves the batteries onto belts, into the station, into crates and onto the floor, and
        // cuts the cables; the first client keeps the station cabled and, when it stands empty, fills it.
        var layout = Layout.Parse(
            ["PPP          ", "             ", "#############", "#.OOO.C.....#", "#.OOO.C.....#", "#.OOO.......#", "#############"],
            null,
            [((0, 0), (6, 3))]).Doubled();
        int full = Content.Items.Get("battery").Capacity!.Value;
        string Stocked(string save)
        {
            var root = JsonNode.Parse(save)!;
            var inventories = new JsonArray();
            for (int peer = 0; peer < 3; peer++)
            {
                var slots = new JsonArray();
                for (int i = 0; i < Content.Inventory.Total; i++)
                    slots.Add(i < 3 ? new JsonObject { ["item"] = "battery", ["count"] = 1, ["charge"] = full / 2 * i }
                        : i == 3 && peer == 1 ? new JsonObject { ["item"] = "drill", ["count"] = 1, ["module"] = Content.Modules.Arc.Item }
                        : i == 3 ? new JsonObject { ["item"] = "drill", ["count"] = 1 } : null);
                inventories.Add(new JsonObject { ["id"] = peer, ["slots"] = slots });
            }
            root["slices"]!["items"]!["inventories"] = inventories;
            return root.ToJsonString();
        }
        Covering(11, tried =>
        {
            var game = new Definition(() => new Registry().All, Stocked);
            int rose = 0, worn = 0, inStation = 0, ran = 0, last = -1;
            var run = Desync.Fuzz(game, tried, layout: layout, ticks: 1500, tamper: (tick, clients) =>
            {
                var first = clients[0];
                var grid = game.Module<GridModule>(first.World);
                var items = game.Module<ItemsModule>(first.World);
                int station = grid.MachinesOf("charger")[0].Id;
                // The power fuzz cuts and moves cables: the station's laid again often, free in creative mode.
                if (tick % 20 == 10)
                {
                    first.Send(new SetCreative(true));
                    first.Send(new Sim.Power.Lay(grid.MachinesOf("solarPanel")[0].Cell, grid.MachinesOf("charger")[0].Cell));
                }
                // And an empty station gets a battery short of full from the first client, wherever it carries one.
                var own = items.Inventory(first.Id);
                int carried = own.ToList().FindIndex(s => s is { Item: "battery" } b && b.Charge < full);
                if (tick % 100 == 50 && items.Crate(station)[0] == null && carried >= 0)
                    first.Send(new Move(new Place(carried), new Target(0, station), 1));
                if (items.Crate(station)[0] is { Charge: var charge })
                {
                    inStation++;
                    if (last >= 0 && charge > last) rose++;
                    last = charge;
                }
                else last = -1;
                foreach (var client in clients)
                    if (items.BeltCharge(client.Id).Capacity > 0) worn++;
                // And runs its drill now and then, which draws from its belt every tick it runs (0055).
                if (tick % 40 == 0) first.Send(new Run("drill", true));
                foreach (var client in clients)
                    if (items.Running(client.Id) != null) ran++;
            });
            return (run, inStation > 100 && rose > 50 && worn > 100 && ran > 100 ? null
                : $"a battery stood in the station on {inStation} ticks, charged on {rose}, was worn on {worn}; a drill ran on {ran}");
        });
    }

    [Fact]
    public void KeepsEveryClientInSyncWhileCapacitorBanksFillAndGiveOut()
    {
        // Two panels out in space cabled to two banks and two lamps (0101). The fuzz cuts and moves the cables; the first
        // client lays the banks' and the lamps' again often and the first panel's now and then, so the banks fill while it
        // is cabled and light the lamps while it is not.
        var layout = Layout.Parse(
            ["PPP PPP          ", "                 ", "#################", "#.OOO.QQ.QQ.L.L.#", "#.OOO.QQ.QQ.....#", "#.OOO...........#", "#################"],
            null,
            [((0, 0), (6, 3)), ((4, 0), (6, 3)), ((9, 3), (6, 3)), ((12, 3), (6, 3)), ((14, 3), (9, 3))]).Doubled();
        Covering(21, tried =>
        {
            var game = new Definition(() => new Registry().All);
            int filled = 0, gave = 0;
            var run = Desync.Fuzz(game, tried, layout: layout, ticks: 1500, tamper: (tick, clients) =>
            {
                var first = clients[0];
                var grid = game.Module<GridModule>(first.World);
                var power = game.Module<Sim.Power.PowerModule>(first.World);
                // A machine the fuzz took apart is let be: its cable is laid no more.
                int? Cell(string kind, int nth) => grid.MachinesOf(kind) is var all && all.Count > nth ? all[nth].Cell : null;
                void Lay(int? a, int? b)
                {
                    if (a is int from && b is int to) first.Send(new Sim.Power.Lay(from, to));
                }
                if (tick % 20 == 10)
                {
                    first.Send(new SetCreative(true));
                    Lay(Cell("capacitorBank", 0), Cell("capacitorBank", 1));
                    Lay(Cell("capacitorBank", 0), Cell("lamp", 0));
                    Lay(Cell("capacitorBank", 1), Cell("lamp", 1));
                }
                if (tick % 300 == 150) Lay(Cell("solarPanel", 0), Cell("capacitorBank", 0));
                foreach (var bank in grid.MachinesOf("capacitorBank"))
                {
                    int flow = power.Store(bank.Id)!.Flow;
                    if (flow > 0) filled++;
                    if (flow < 0) gave++;
                }
            });
            return (run, filled > 100 && gave > 50 ? null : $"a bank filled on {filled} ticks and gave out on {gave}");
        });
    }

    [Fact]
    public void KeepsEveryClientInSyncWhileEveryKindOfStationWorks()
    {
        // An aired room with the lifepod, a furnace, and a Former, an electric smelter and a circuit assembler cabled
        // to a burner generator, and a grow capsule and a crusher to one of their own (0057, 0060), each peer carrying chunks,
        // planks, quartz, asteroid ore and seeds. The fuzz crafts and cancels at every station; the first client, in creative
        // mode, keeps the Former's and the assembler's queues going and the furnace, the smelter, the crusher, the capsule and
        // the generators fed. The capsule starts a hundred ticks of full power from its crop, so the seeds it gives back are
        // drawn on every peer; the crusher's quartz, one ore in two, is drawn too.
        var layout = Layout.Parse(
            ["###############################", "#.......OOO..............gg...#", "#FF.TT..OOOMMBB.EE.YY.BB.gg.kk#", "#FF.TTV.OOOMMBB.EE.YY.BB.gg.kk#", "###############################"],
            ["", "", "    o", "    ---"],
            [((13, 2), (11, 2)), ((13, 2), (16, 2)), ((13, 2), (19, 2)), ((22, 2), (25, 2)), ((22, 2), (28, 2))]).Doubled();
        int crop = Content.Items.Get("seed").SmeltTicks!.Value * 1000;
        string Stocked(string save)
        {
            var root = JsonNode.Parse(save)!;
            var inventories = new JsonArray();
            for (int peer = 0; peer < 3; peer++)
            {
                var slots = new JsonArray();
                for (int i = 0; i < Content.Inventory.Total; i++)
                    slots.Add(i < 3 ? new JsonObject { ["item"] = "scrapChunk", ["count"] = 10 }
                        : i < 6 ? new JsonObject { ["item"] = "planks", ["count"] = 50 }
                        : i < 9 ? new JsonObject { ["item"] = "quartz", ["count"] = 10 }
                        : i < 11 ? new JsonObject { ["item"] = "seed", ["count"] = 10 }
                        : i < 13 ? new JsonObject { ["item"] = "asteroidOre", ["count"] = 10 } : null);
                inventories.Add(new JsonObject { ["id"] = peer, ["slots"] = slots });
            }
            root["slices"]!["items"]!["inventories"] = inventories;
            int capsule = (int)root["slices"]!["grid"]!["machines"]!.AsArray().First(m => (string?)m!["kind"] == "growCapsule")!["id"]!;
            var crates = root["slices"]!["items"]!["crates"]!.AsArray();
            if (crates.Any(c => (int)c!["id"]! > capsule)) throw new InvalidOperationException("the capsule's entry goes last only if it has the highest id");
            crates.Add(new JsonObject { ["id"] = capsule, ["slots"] = new JsonArray(new JsonObject { ["item"] = "seed", ["count"] = 10 }, null, null) });
            root["slices"]!["crafting"]!["smelts"] = new JsonArray(new JsonObject { ["machine"] = capsule, ["item"] = "seed", ["progress"] = crop - 100 * 1000 });
            return root.ToJsonString();
        }
        // On a stream of modules' commands alone, as above.
        Covering(16, tried =>
        {
            var game = new Definition(() => new Registry().All, Stocked);
            int formed = 0, smelted = 0, heated = 0, crushed = 0, assembled = 0, grew = 0, harvested = 0;
            var run = Desync.Fuzz(game, tried, layout: layout, ticks: 1500, worldEvery: 0, tamper: (tick, clients) =>
            {
                var first = clients[0];
                var grid = game.Module<GridModule>(first.World);
                var crafting = game.Module<CraftingModule>(first.World);
                int former = grid.MachinesOf("former")[0].Id, furnace = grid.MachinesOf("furnace")[0].Id;
                int burner = grid.MachinesOf("burnerGenerator")[0].Id, smelter = grid.MachinesOf("smelter")[0].Id;
                int assembler = grid.MachinesOf("assembler")[0].Id, capsule = grid.MachinesOf("growCapsule")[0].Id;
                int second = grid.MachinesOf("burnerGenerator")[1].Id, crusher = grid.MachinesOf("crusher")[0].Id;
                if (tick % 20 == 10)
                {
                    first.Send(new SetCreative(true));
                    first.Send(new Craft(former, "plate", 3));
                    first.Send(new Craft(assembler, tick % 40 == 10 ? "circuit" : "solarCell", 2));
                    // The power fuzz cuts and moves cables: the Former's laid again, free in creative mode.
                    first.Send(new Sim.Power.Lay(grid.MachineById(burner)!.Cell, grid.MachineById(former)!.Cell));
                    first.Send(new Sim.Power.Lay(grid.MachineById(burner)!.Cell, grid.MachineById(smelter)!.Cell));
                    first.Send(new Sim.Power.Lay(grid.MachineById(burner)!.Cell, grid.MachineById(assembler)!.Cell));
                    first.Send(new Sim.Power.Lay(grid.MachineById(second)!.Cell, grid.MachineById(capsule)!.Cell));
                    first.Send(new Sim.Power.Lay(grid.MachineById(second)!.Cell, grid.MachineById(crusher)!.Cell));
                }
                var own = game.Module<ItemsModule>(first.World).Inventory(first.Id).ToList();
                int chunk = own.FindIndex(s => s is { Item: "scrapChunk" }), planks = own.FindIndex(s => s is { Item: "planks" });
                int quartz = own.FindIndex(s => s is { Item: "quartz" }), seed = own.FindIndex(s => s is { Item: "seed" });
                int ore = own.FindIndex(s => s is { Item: "asteroidOre" });
                if (tick % 50 == 25 && chunk >= 0) first.Send(new Move(new Place(chunk), new Target(0, furnace), 2));
                if (tick % 50 == 30 && planks >= 0) first.Send(new Move(new Place(planks), new Target(1, furnace), 1));
                if (tick % 50 == 35 && planks >= 0) first.Send(new Move(new Place(planks), new Target(0, burner), 1));
                if (tick % 50 == 40 && quartz >= 0) first.Send(new Move(new Place(quartz), new Target(0, smelter), 2));
                if (tick % 50 == 15 && ore >= 0) first.Send(new Move(new Place(ore), new Target(0, crusher), 2));
                if (tick % 50 == 20 && planks >= 0) first.Send(new Move(new Place(planks), new Target(0, second), 1));
                if (tick % 50 == 45 && seed >= 0) first.Send(new Move(new Place(seed), new Target(0, capsule), 2));
                if (crafting.Works(former)) formed++;
                if (crafting.Progress(furnace) > 0) smelted++;
                if (crafting.Works(smelter)) heated++;
                if (crafting.Works(crusher)) crushed++;
                if (crafting.Works(assembler)) assembled++;
                if (crafting.Works(capsule)) grew++;
                if (game.Module<ItemsModule>(first.World).Crate(capsule).Skip(1).Any(s => s is { Item: "biomass" })) harvested++;
            });
            return (run, formed > 100 && smelted > 30 && heated > 30 && crushed > 30 && assembled > 100 && grew > 100 && harvested > 0 ? null
                : $"the Former worked on {formed} ticks, the furnace smelted on {smelted}, the smelter on {heated}, the crusher on {crushed}, the assembler worked on "
                    + $"{assembled}, the capsule grew on {grew}, its crop in on {harvested}");
        });
    }

    [Fact]
    public void KeepsEveryClientAndAJoinerInSyncSellingAndBuyingTheTree()
    {
        // Trade and the tree (2026-10-30-trade-and-tech): a stocked trade station earns on its beat, a joiner comes in mid-beat,
        // the credits and the counter in its welcome, and before its next sale the first client spends them on the first ring's blocks: without them in its welcome it refuses.
        var layout = Layout.Parse(["##########", "#$$......#", "#$$......#", "##########"]).Doubled() with { Known = Content.Research.StartKnown };
        static string Stocked(string save)
        {
            var root = JsonNode.Parse(save)!;
            int station = (int)root["slices"]!["grid"]!["machines"]!.AsArray().First(m => (string?)m!["kind"] == "tradeStation")!["id"]!;
            var slots = new JsonArray(new JsonObject { ["item"] = "frame", ["count"] = 50 }, new JsonObject { ["item"] = "wire", ["count"] = 50 }, null, null);
            root["slices"]!["items"]!["crates"]!.AsArray().Add(new JsonObject { ["id"] = station, ["slots"] = slots });
            return root.ToJsonString();
        }
        string[] blocks = ["workbench", "furnace"];
        Covering(23, tried =>
        {
            var game = new Definition(() => new Registry().All, Stocked);
            int earned = 0, bought = 0;
            var run = Desync.Fuzz(game, tried, layout: layout, ticks: 1000, joinAt: 503, tamper: (tick, clients) =>
            {
                var first = clients[0];
                var knowledge = game.Module<Sim.Knowledge.KnowledgeModule>(first.World);
                earned = Math.Max(earned, knowledge.Credits);
                bought = Math.Max(bought, blocks.Count(knowledge.Granted));
                if (tick <= 503 || tick % 40 != 25) return;
                foreach (string block in blocks) first.Send(new Sim.Research.BuyBlock(block));
            });
            return (run, earned > 100 && bought == 2 ? null : $"{earned} credits at most, {bought} blocks bought");
        });
    }

    [Fact]
    public void KeepsEveryClientAndAJoinerInSyncAsTheRuleGrants()
    {
        // The rule (docs/plans/board.md): one plate short of an iron wall until a joiner is in, whose welcome holds the count; then
        // the first client's plates reach it and everyone's walk grants on the same tick, the joiner's too.
        var layout = Layout.Parse(["##########", "#II......#", "##########"]).Doubled() with { Known = [.. Content.Research.StartKnown, "plate"] };
        static string Seeded(string save)
        {
            var root = JsonNode.Parse(save)!;
            root["slices"]!["knowledge"]!["obtained"] = new JsonArray(new JsonObject { ["item"] = "plate", ["count"] = Sim.Research.Understanding.Enough("plate") - 1 });
            var inventories = new JsonArray();
            for (int id = 0; id < 4; id++)
            {
                var slots = new JsonArray();
                for (int i = 0; i < Content.Inventory.Total; i++) slots.Add(null);
                slots[0] = new JsonObject { ["item"] = "iron", ["count"] = 200 };
                inventories.Add(new JsonObject { ["id"] = id, ["slots"] = slots });
            }
            root["slices"]!["items"]!["inventories"] = inventories;
            return root.ToJsonString();
        }
        Covering(29, tried =>
        {
            var game = new Definition(() => new Registry().All, Seeded);
            bool early = false, granted = false;
            var run = Desync.Fuzz(game, tried, layout: layout, ticks: 1000, joinAt: 503, tamper: (tick, clients) =>
            {
                var first = clients[0];
                bool known = game.Module<Sim.Knowledge.KnowledgeModule>(first.World).Granted("wallIron");
                if (tick <= 503) early |= known;
                granted |= known;
                if (tick <= 503 || tick % 40 != 25) return;
                int bench = game.Module<Sim.Grid.GridModule>(first.World).MachinesOf("workbench")[0].Id;
                first.Send(new Sim.Crafting.Craft(bench, "plate", 1));
            });
            return (run, granted && !early ? null : $"granted {granted}, before the joiner {early}");
        });
    }

    // The harness itself.

    [Theory]
    [MemberData(nameof(Seeds))]
    public void KeepsClientsInSyncUnderRandomCommands(uint seed)
    {
        var run = Desync.Fuzz(Definition.Fixtures(), seed);
        Assert.Null(run.Desync);
        Assert.True(run.Commands > 500, $"{run.Commands} commands");
        // Refusals go through the same path on every peer, so they are fuzzed too.
        Assert.True(run.Refused > 50, $"{run.Refused} refused");
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void KeepsAJoinerInStepAndCatchesItDrifting(uint seed)
    {
        int joinAt = 20 + (int)(seed % 37);
        var steady = Desync.Fuzz(Definition.Fixtures(), seed, joinAt: joinAt, ticks: 200);
        Assert.Null(steady.Desync);
        var fixtures = Definition.Fixtures();
        var drifting = Desync.Fuzz(fixtures, seed, joinAt: joinAt, ticks: 200, tamper: (tick, clients) =>
        {
            if (tick == joinAt + 10) fixtures.Module<DriftModule>(clients[3].World).Nudge(1);
        });
        // Caught by its hash at the next check, or already resynced, saying what drifted.
        Assert.NotNull(drifting.Desync);
        foreach (var report in drifting.Desync.Reports)
            Assert.Equal(["drift"], report.Modules.Select(m => m.Id));
    }

    [Fact]
    public void CatchesAClientWhoseWorldWasChangedBehindThePipelinesBack()
    {
        var fixtures = Definition.Fixtures();
        var run = Desync.Fuzz(fixtures, 7, tamper: (tick, clients) =>
        {
            if (tick == 30) fixtures.Module<DriftModule>(clients[2].World).Nudge(1);
        });
        Assert.Equal(40, run.Desync?.Tick);
        // The host caught it on the first hash after, and resynced the client: it says what drifted.
        var report = Assert.Single(run.Desync!.Reports);
        Assert.Equal(40, report.Tick);
        Assert.False(report.Rng);
        var module = Assert.Single(report.Modules);
        Assert.Equal("drift", module.Id);
        Assert.Equal(["drift.position"], module.Paths);
    }

    private sealed record Bump(int By) : Command;

    private sealed class Silent : Module<Silent.Count>
    {
        public sealed class Count { public int N { get; set; } }
        public override string Id => "silent";
        public Silent() => Handles<Bump>(_ => null, c => State.N += c.By);
        protected override Count Init(Layout layout) => new();
    }

    private sealed class Quiet : Module<Quiet.Nothing>
    {
        public sealed class Nothing;
        public override string Id => "quiet";
        protected override Nothing Init(Layout layout) => new();
    }

    [Fact]
    public void RefusesAModuleThatHasCommandsButNoFuzzGenerator()
    {
        var both = new Definition(() => [new BoardModule(), new Silent()]);
        var why = Assert.Throws<InvalidOperationException>(() => Desync.Fuzz(both, 1));
        Assert.Equal("Modules with commands but no fuzz generator: 'silent'. Override Fuzz.", why.Message);
    }

    [Fact]
    public void DoesNotAskAModuleWithoutCommandsForAGenerator()
    {
        var both = new Definition(() => [new BoardModule(), new Quiet()]);
        Assert.Null(Desync.Fuzz(both, 1, ticks: 40).Desync);
    }
}
