using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
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
        var run = Desync.Fuzz(Definition.Game(), seed, layout: Registry.Wreck);
        Assert.Null(run.Desync);
        Assert.Equal(600, run.Ticks);
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
        var run = Desync.Fuzz(game, 8, layout: room,
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
