using System.Text.Json.Nodes;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Rooms;
using Xunit;

namespace Stillwake.Net.Tests;

/// <summary>packages/net/src/desync.test.ts, ported.</summary>
public class DesyncTests
{
    public static readonly TheoryData<uint> Seeds = new() { 1u, 2u, 3u, 42u, 2026u };

    // The game.

    [Theory]
    [MemberData(nameof(Seeds))]
    public void KeepsEveryClientInSyncOnTheStartingWreck(uint seed)
    {
        // Heroes entered anywhere on the big world: the far field round them spawns, drifts and is gone in sync too.
        var game = Definition.Game();
        int far = 0;
        var run = Desync.Fuzz(game, seed, layout: Registry.Wreck, tamper: (_, clients) =>
            far = Math.Max(far, game.Module<DebrisModule>(clients[0].World).Pieces.Count(p => p.Zone == Content.Debris.Far.Id)));
        Assert.Null(run.Desync);
        Assert.Equal(600, run.Ticks);
        Assert.True(far > 0, "no far piece spawned");
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
        int surfaces = 0, away = 0, count = 0;
        var run = Desync.Fuzz(Definition.Game(), seed, layout: Registry.Wreck, joinAt: 300, worldEvery: 8, tamper: (_, clients) =>
        {
            var world = clients[0].World;
            surfaces = Math.Max(surfaces, world.Surfaces.Count);
            away += clients.Count(c => world.SurfaceOf(c.Id) != 0);
            count = clients.Count;
        });
        Assert.Null(run.Desync);
        Assert.Equal(4, count);
        Assert.True(surfaces >= 3, $"{surfaces} surfaces at most");
        Assert.True(away > 100, $"a client off the ship on {away} ticks in all");
    }

    [Fact]
    public void KeepsEveryClientInSyncDiggingTheGroundOfSmallPlanets()
    {
        // Small planets opened by the world's fuzz, heroes beamed down to them, their ground grown on every peer from one
        // seed and dug there (0077): walls cracked, broken out, their soil and rock given or spilt, plants felled (0079), alike on
        // every peer. Long enough that digs, few in a fuzz, break a wall and fell a growth among the caves' (0080); seed 19 reaches every
        // case, as about one seed in five does, 7 no longer since fitting modules (0086) shifted the stream.
        int planets = 0, cracked = 0, dug = 0, felled = 0;
        var run = Desync.Fuzz(Definition.Game(), 19, layout: Registry.Wreck, ticks: 1800, worldEvery: 6, tamper: (_, clients) =>
        {
            var world = clients[0].World;
            foreach (var surface in world.Surfaces.Where(s => s.Kind == Content.Surfaces.SmallPlanet.Id))
            {
                var m = Registry.On(world, surface.Id);
                cracked = Math.Max(cracked, m.Building.Cracks.Count);
                int got = m.Items.Floor.Where(s => s.Item is "soil" or "rock").Sum(s => s.Count)
                    + clients.Sum(c => m.Items.Totals(c.Id).Where(t => t.Item is "soil" or "rock").Sum(t => t.Count));
                dug = Math.Max(dug, got);
                felled = Math.Max(felled, Terrain.Generate(Content.Ground.Small, surface.Seed).Flora!.Count - m.Building.Plants.Count);
            }
            planets = Math.Max(planets, world.Surfaces.Count(s => s.Kind == Content.Surfaces.SmallPlanet.Id));
        });
        Assert.Null(run.Desync);
        Assert.True(planets >= 1 && cracked >= 1 && dug >= 1 && felled >= 1, $"{planets} small planets, {cracked} cracks at most, {dug} soil and rock dug, {felled} plants felled");
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
            [.. blank.Concat(pipes).Select(Pad)]);
        var game = Definition.Game();
        var seen = new Dictionary<Client, int>();
        int around = 0, aired = 0;
        // Tuned on a stream of modules' commands alone: the world's are drawn in KeepsEveryClientAndAJoinerInSyncAsHeroesMoveBetweenSurfaces.
        var run = Desync.Fuzz(game, 7, layout: layout, joinAt: 150, ticks: 400, worldEvery: 0, tamper: (_, clients) =>
        {
            aired = Math.Max(aired, game.Module<AtmosphereModule>(clients[0].World).AiredCells.Count);
            foreach (var client in clients)
            {
                var rooms = game.Module<RoomsModule>(client.World);
                if (seen.TryGetValue(client, out int version) && rooms.ChangedSince(version) is { Count: > 0 }) around++;
                seen[client] = rooms.Version;
            }
        });
        Assert.Null(run.Desync);
        Assert.True(around > 10, $"rooms found around a change {around} times");
        Assert.True(aired > 50, $"{aired} cells aired at most");
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
        ]);
        static int Cell(int x, int y) => y * 13 + x;
        var pipe = new Placement("conduit", "pipe");
        const int tank = 2;
        int aired = 0, peak = 0;
        bool drawn = false, refilled = false, woke = false;
        var suits = new Dictionary<int, (int Suit, int Out)>();
        var game = Definition.Game();
        var run = Desync.Fuzz(game, 9, layout: room,
            // Past two leak beats: an aired room leaks once a minute, 1200 ticks.
            ticks: 2600,
            tamper: (tick, clients) =>
            {
                var first = clients[0];
                // Building and loading are free, whatever creative mode the fuzz switched to meanwhile.
                if (tick <= 40) first.Send(new SetCreative(true));
                if (tick < 40)
                {
                    // Each missing pipe planned, or its plan worked: the fuzz may cancel a plan meanwhile.
                    foreach (int at in new[] { Cell(2, 2), Cell(2, 3), Cell(2, 4) })
                    {
                        if (game.Module<GridModule>(first.World).ConduitAt(at) != Conduit.None) continue;
                        var plan = game.Module<BuildingModule>(first.World).PlansAt(at)
                            .FirstOrDefault(p => p.Place?.Layer == "conduit");
                        first.Send(plan != null ? new Work(plan.Id) : new PlacePlan(at, pipe));
                    }
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
                        if (before.Out == 0 && h.Out == 0 && h.Suit > before.Suit) refilled = true;
                        if (before.Out > 0 && h.Out == 0) woke = true;
                    }
                    suits[h.Id] = (h.Suit, h.Out);
                }
            });
        Assert.Null(run.Desync);
        Assert.True(aired > 0);
        Assert.True(drawn);
        // Breathing ran both ways: a suit refilled from room air, and a hero passed out and woke.
        Assert.True(refilled);
        Assert.True(woke);
    }

    [Fact]
    public void KeepsEveryClientInSyncWhileBatteriesChargeAndMoveBetweenBeltsAndAStation()
    {
        // A panel out in space cabled to a charging station by the lifepod, each peer carrying a drill and batteries of
        // every charge. The fuzz runs the drills and moves the batteries onto belts, into the station, into crates and onto the floor, and
        // cuts the cables; the first client keeps the station cabled and, when it stands empty, fills it.
        var layout = Layout.Parse(
            ["PPP          ", "             ", "#############", "#.OOO.C.....#", "#.OOO.C.....#", "#.OOO.......#", "#############"],
            null,
            [((0, 0), (6, 3))]);
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
                        : i == 3 ? new JsonObject { ["item"] = "drill", ["count"] = 1 } : null);
                inventories.Add(new JsonObject { ["id"] = peer, ["slots"] = slots });
            }
            root["slices"]!["items"]!["inventories"] = inventories;
            return root.ToJsonString();
        }
        var game = new Definition(() => new Registry().All, Stocked);
        int rose = 0, worn = 0, inStation = 0, ran = 0, last = -1;
        var run = Desync.Fuzz(game, 11, layout: layout, ticks: 1500, tamper: (tick, clients) =>
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
        Assert.Null(run.Desync);
        Assert.True(inStation > 100, $"a battery stood in the station on {inStation} ticks");
        Assert.True(rose > 50, $"a battery charged on {rose} ticks");
        Assert.True(worn > 100, $"a battery was worn on {worn} ticks");
        Assert.True(ran > 100, $"a drill ran on {ran} ticks");
    }

    [Fact]
    public void KeepsEveryClientInSyncWhileEveryKindOfStationWorks()
    {
        // An aired room with the lifepod, a furnace, and a Former, an electric smelter and a circuit assembler cabled
        // to a burner generator, and a grow capsule to one of its own (0057, 0060), each peer carrying chunks, planks,
        // rock and seeds. The fuzz crafts and cancels at every station; the first client, in creative mode, keeps the
        // Former's and the assembler's queues going and the furnace, the smelter, the capsule and the generators fed.
        // The capsule starts a hundred ticks of full power from its crop, so the seeds it gives back are drawn on every
        // peer.
        var layout = Layout.Parse(
            ["############################", "#.......OOO..............gg#", "#FF.TT..OOOMMBB.EE.YY.BB.gg#", "#FF.TTV.OOOMMBB.EE.YY.BB.gg#", "############################"],
            ["", "", "    o", "    ---"],
            [((13, 2), (11, 2)), ((13, 2), (16, 2)), ((13, 2), (19, 2)), ((22, 2), (25, 2))]);
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
                        : i < 9 ? new JsonObject { ["item"] = "rock", ["count"] = 10 }
                        : i < 11 ? new JsonObject { ["item"] = "seed", ["count"] = 10 } : null);
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
        var game = new Definition(() => new Registry().All, Stocked);
        int formed = 0, smelted = 0, heated = 0, assembled = 0, grew = 0, harvested = 0;
        // Tuned on a stream of modules' commands alone, as above.
        var run = Desync.Fuzz(game, 16, layout: layout, ticks: 1500, worldEvery: 0, tamper: (tick, clients) =>
        {
            var first = clients[0];
            var grid = game.Module<GridModule>(first.World);
            var crafting = game.Module<CraftingModule>(first.World);
            int former = grid.MachinesOf("former")[0].Id, furnace = grid.MachinesOf("furnace")[0].Id;
            int burner = grid.MachinesOf("burnerGenerator")[0].Id, smelter = grid.MachinesOf("smelter")[0].Id;
            int assembler = grid.MachinesOf("assembler")[0].Id, capsule = grid.MachinesOf("growCapsule")[0].Id;
            int second = grid.MachinesOf("burnerGenerator")[1].Id;
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
            }
            var own = game.Module<ItemsModule>(first.World).Inventory(first.Id).ToList();
            int chunk = own.FindIndex(s => s is { Item: "scrapChunk" }), planks = own.FindIndex(s => s is { Item: "planks" });
            int rock = own.FindIndex(s => s is { Item: "rock" }), seed = own.FindIndex(s => s is { Item: "seed" });
            if (tick % 50 == 25 && chunk >= 0) first.Send(new Move(new Place(chunk), new Target(0, furnace), 2));
            if (tick % 50 == 30 && planks >= 0) first.Send(new Move(new Place(planks), new Target(1, furnace), 1));
            if (tick % 50 == 35 && planks >= 0) first.Send(new Move(new Place(planks), new Target(0, burner), 1));
            if (tick % 50 == 40 && rock >= 0) first.Send(new Move(new Place(rock), new Target(0, smelter), 2));
            if (tick % 50 == 20 && planks >= 0) first.Send(new Move(new Place(planks), new Target(0, second), 1));
            if (tick % 50 == 45 && seed >= 0) first.Send(new Move(new Place(seed), new Target(0, capsule), 2));
            if (crafting.Works(former)) formed++;
            if (crafting.Progress(furnace) > 0) smelted++;
            if (crafting.Works(smelter)) heated++;
            if (crafting.Works(assembler)) assembled++;
            if (crafting.Works(capsule)) grew++;
            if (game.Module<ItemsModule>(first.World).Crate(capsule).Skip(1).Any(s => s is { Item: "biomass" })) harvested++;
        });
        Assert.Null(run.Desync);
        Assert.True(formed > 100, $"the Former worked on {formed} ticks");
        Assert.True(smelted > 30, $"the furnace smelted on {smelted} ticks");
        Assert.True(heated > 30, $"the smelter smelted on {heated} ticks");
        Assert.True(assembled > 100, $"the assembler worked on {assembled} ticks");
        Assert.True(grew > 100, $"the capsule grew on {grew} ticks");
        Assert.True(harvested > 0, "the capsule's crop never came in");
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
