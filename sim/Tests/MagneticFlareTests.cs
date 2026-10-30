using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;
using Xunit;
using Pairs = System.Collections.Generic.IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>;

namespace Stillwake.Sim.Tests;

public sealed record BreakMachine(int Id) : Command;

/// <summary>Breaks a machine as a flare would, so a test picks which.</summary>
public sealed class Breaker : Module<Breaker.Nothing>
{
    public sealed class Nothing;
    public override string Id => "breaker";

    public Breaker(IPowerOps power) => Handles<BreakMachine>(_ => null, c => power.Break(c.Id));

    protected override Nothing Init(Layout layout) => new();
}

/// <summary>Runs <c>look</c> in construction, after every registered module's: what a module there sees on a strike's tick.</summary>
public sealed class Watcher : Module<Watcher.Nothing>
{
    public sealed class Nothing;
    public override string Id => "watcher";

    public Watcher(Action look) => System(Phase.Construction, look);

    protected override Nothing Init(Layout layout) => new();
}

/// <summary>Magnetic flares breaking machines and their repair (2026-10-30-magnetic-flare).</summary>
public class MagneticFlareTests
{
    /// <summary>
    /// A panel out in space feeding a generator and two lamps in a chain, and a capacitor feeding a burner generator; a third
    /// lamp cabled to nothing. Drawn a character a 16 px cell, doubled.
    /// </summary>
    private static readonly string[] Rows =
    [
        "PPP             ",
        "                ",
        "################",
        "#GG.L.L.QQ.BB.L#",
        "#GG.....QQ.BBKK#",
        "################",
    ];
    private static readonly Pairs Cables = [((0, 0), (1, 3)), ((1, 3), (4, 3)), ((4, 3), (6, 3)), ((0, 0), (8, 3)), ((8, 3), (11, 3))];
    private const int Width = 2 * 16;
    private static int At(int x, int y) => 2 * y * Width + 2 * x;
    private static readonly int Panel = At(0, 0), Generator = At(1, 3), Lamp = At(4, 3), Far = At(6, 3), Capacitor = At(8, 3),
        Burner = At(11, 3), Loose = At(14, 3), Crate = At(13, 4);
    /// <summary>The machines a flare may pick: every powered one with a cable.</summary>
    private static readonly int[] Cabled = [Panel, Generator, Lamp, Far, Capacitor, Burner];

    private static MagneticFlares.Flare Starting => MagneticFlares.StartingSpace;

    private static Scenario World(bool meteors = false, IReadOnlyList<string>? known = null, uint seed = 1) =>
        new(Layout.Parse(Rows, null, Cables) with { Meteors = meteors, Known = known }, seed, with: m => [new Breaker(m.Power)], doubled: true);

    private static int Id(Scenario w, int cell) => w.M.Grid.MachineAt(cell)!.Id;
    private static Scenario Break(Scenario w, int cell) => w.Do(new BreakMachine(Id(w, cell))).Run();
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Count(Scenario w, string item, int peer = 0) => w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);

    /// <summary>Ticks till a flare's warning shows, at most <paramref name="limit"/>; null when none does.</summary>
    private static int? UntilComing(Scenario w, int limit)
    {
        for (int t = 0; t < limit; t++)
        {
            if (w.M.Hazards.FlareComing) return t;
            w.Run();
        }
        return null;
    }

    // When.

    [Fact]
    public void NoFlareUntilTheGeneratorIsKnownThenOneWithinItsIntervalThatStrikesUnarmedToo()
    {
        var w = World(meteors: true, known: []);
        Assert.Null(UntilComing(w, Starting.EveryMax + 10));
        // Creative mode knows the generator: armed, the next tick draws a flare, then the warning comes at its end.
        w.Do(new SetCreative(true)).Run();
        int until = UntilComing(w, Starting.EveryMax) ?? throw new InvalidOperationException("no flare");
        Assert.InRange(until, Starting.EveryMin - Starting.Warning, Starting.EveryMax - Starting.Warning);
        Assert.Equal(Starting.Warning, w.M.Hazards.FlareLeft);
        // Disarmed during the warning: the flare drawn strikes all the same.
        w.Do(new SetCreative(false)).Run(Starting.Warning);
        Assert.False(w.M.Hazards.FlareComing);
        Assert.NotEmpty(w.M.Power.Broken);
    }

    [Fact]
    public void NoneInACalmWorldNorOnAPlanet()
    {
        Assert.Null(UntilComing(World(meteors: false), Starting.EveryMax + 10));
        Assert.All(PlanetTypes.All, p => Assert.Null(p.Flares));
        Assert.Same(Starting, Surfaces.Ship.Flares);
    }

    [Fact]
    public void FlareNowStartsTheWarningInACalmWorldThenStrikes()
    {
        var w = World().Do(new FlareNow(Starting.Warning)).Run();
        Assert.True(w.M.Hazards.FlareComing);
        Assert.Equal(Starting.Warning - 1, w.M.Hazards.FlareLeft);
        Assert.Empty(w.M.Power.Broken);
        w.Do(new FlareNow(Starting.Warning)).Do(new FlareNow(0)).Run(Starting.Warning - 1);
        Assert.Equal(new[] { "hazards.flareComing", "hazards.badTicks" }, Reasons(w));
        Assert.False(w.M.Hazards.FlareComing);
        Assert.NotEmpty(w.M.Power.Broken);
        // A calm world draws no next one.
        Assert.Null(UntilComing(w, Starting.EveryMax + 10));
    }

    [Fact]
    public void FlareNowReplacesAFlareDrawnButNotYetComing()
    {
        var w = World(meteors: true).Run();
        Assert.InRange(FlareIn(w), Starting.EveryMin, Starting.EveryMax);
        w.Do(new FlareNow(1)).Run();
        Assert.Equal(2, w.M.Power.Broken.Count);
        Assert.Empty(w.Refusals);
        // Struck, the next is drawn on the same tick, a whole interval on.
        Assert.InRange(FlareIn(w), Starting.EveryMin, Starting.EveryMax);
    }

    [Fact]
    public void ASaveFromBeforeFlaresDrawsTheFirstOnItsFirstTick()
    {
        var w = World(meteors: true);
        var root = JsonNode.Parse(w.World.Save())!.AsObject();
        Assert.DoesNotContain("flareIn", root["slices"]!["hazards"]!.ToJsonString());
        var loaded = World(meteors: true).Reload(root.ToJsonString());
        Assert.Equal(0, FlareIn(loaded));
        loaded.Run();
        Assert.InRange(FlareIn(loaded), Starting.EveryMin, Starting.EveryMax);
    }

    private static int FlareIn(Scenario w) => ((HazardsState)w.M.Hazards.Slice).FlareIn;

    // The strike.

    [Fact]
    public void AStrikeBreaksOnlyCabledMachinesNotBrokenTillNoneIsLeft()
    {
        var w = World();
        var broken = new List<int>();
        for (int i = 0; i < Cabled.Length; i++)
        {
            int n = Cabled.Length - broken.Count;
            if (n == 0) break;
            w.Do(new FlareNow(1)).Run();
            var now = w.M.Power.Broken.Except(broken).ToList();
            Assert.InRange(now.Count, (n * Starting.PercentMin + 99) / 100, (n * Starting.PercentMax + 99) / 100);
            broken.AddRange(now);
        }
        Assert.Equal(Cabled.Select(c => Id(w, c)).Order(), w.M.Power.Broken);
        Assert.False(w.M.Power.IsBroken(Id(w, Loose)));
        // Nothing left to pick: a strike breaks nothing.
        w.Do(new FlareNow(1)).Run();
        Assert.Equal(Cabled.Length, w.M.Power.Broken.Count);
    }

    [Fact]
    public void AFixedSeedBreaksTheMachinesItDraws()
    {
        int[] Strike(uint seed) => [.. World(seed: seed).Do(new FlareNow(1)).Run().M.Power.Broken];
        Assert.Equal(new[] { 3, 6 }, Strike(7));
        // The picks are drawn, not the first in id order: seeds differ.
        Assert.True(Enumerable.Range(1, 10).Select(s => string.Join(",", Strike((uint)s))).Distinct().Count() > 2);
    }

    [Fact]
    public void AMachineWhoseCableIsCutOnTheStrikesTickIsNoCandidate()
    {
        for (uint seed = 1; seed <= 30; seed++)
        {
            var w = World(seed: seed).Do(new Cut(Lamp, Far)).Do(new FlareNow(1)).Run();
            Assert.Empty(w.Refusals);
            Assert.False(w.M.Power.IsBroken(Id(w, Far)));
        }
    }

    [Fact]
    public void SatisfactionAndNeededSeeAStrikeOnItsOwnTick()
    {
        int users = 0;
        for (uint seed = 1; seed <= 10; seed++)
        {
            // Each machine's satisfaction and whether its fire is wanted, read in construction after the strike, before Power's
            // derivation finds the networks again.
            var seen = new Dictionary<int, (int Satisfaction, bool Needed)>();
            var w = new Scenario(Layout.Parse(Rows, null, Cables), seed, with: m =>
                [new Watcher(() => { foreach (var x in m.Grid.Machines) seen[x.Id] = (m.Power.Satisfaction(x.Id), m.Power.Needed(x.Id)); })],
                doubled: true).Run();
            // Every user on the network is served and a fire is wanted, the generator drawing more than the panel makes.
            Assert.All(new[] { Generator, Lamp, Far }, c => Assert.True(seen[Id(w, c)].Satisfaction > 0));
            Assert.True(seen[Id(w, Burner)].Needed);
            w.Do(new FlareNow(1)).Run();
            Assert.All(w.M.Power.Broken, id => Assert.Equal((0, false), seen[id]));
            users += w.M.Power.Broken.Count(id => id == Id(w, Generator) || id == Id(w, Lamp) || id == Id(w, Far) || id == Id(w, Burner));
        }
        Assert.True(users > 2);
    }

    // Broken.

    [Fact]
    public void ABrokenLampDrawsNothingAndTheNetworkStillJoinsThroughIt()
    {
        var w = World();
        var before = w.M.Power.NetworkOf(Panel)!;
        Break(w, Lamp);
        var after = w.M.Power.NetworkOf(Panel)!;
        Assert.Equal(before.Nodes, after.Nodes);
        Assert.Equal((6, 5), (before.Demand, after.Demand));
        Assert.Equal(0, w.M.Power.Satisfaction(Id(w, Lamp)));
        Assert.Equal(1000, w.M.Power.Satisfaction(Id(w, Far)));
    }

    [Fact]
    public void ABrokenGeneratorMakesNoAirAndABrokenPanelNoPower()
    {
        var w = World().Run(10);
        int air = w.M.Atmosphere.OxygenIn(Id(w, Generator));
        Assert.True(w.Run(10).M.Atmosphere.OxygenIn(Id(w, Generator)) > air);
        Break(w, Generator);
        air = w.M.Atmosphere.OxygenIn(Id(w, Generator));
        Assert.Equal(air, w.Run(20).M.Atmosphere.OxygenIn(Id(w, Generator)));
        Assert.Equal(0, w.M.Power.Satisfaction(Id(w, Generator)));
        Assert.Equal(1000, w.M.Power.Satisfaction(Id(w, Lamp)));
        Break(w, Panel);
        Assert.Equal(0, w.M.Power.NetworkOf(Panel)!.Supply);
    }

    [Fact]
    public void ABrokenStoreKeepsWhatItHoldsTakingNothingInAndGivingNothingOut()
    {
        var w = World();
        Break(w, Generator).Run(20);
        int held = w.M.Power.Store(Id(w, Capacitor))!.Energy;
        // Panel 5, lamps 2: the spare 3 a tick goes in.
        Assert.Equal(3 * 21, held);
        Break(w, Capacitor).Run(20);
        Assert.Equal(new StoreLevel(held, 12000, 0), w.M.Power.Store(Id(w, Capacitor)));
        // Short of supply, it gives nothing to the lamps.
        Break(w, Panel).Run(20);
        Assert.Equal(new StoreLevel(held, 12000, 0), w.M.Power.Store(Id(w, Capacitor)));
        Assert.Equal(0, w.M.Power.Satisfaction(Id(w, Lamp)));
    }

    [Fact]
    public void ABrokenBurnerIsNotNeeded()
    {
        var w = World();
        Break(w, Panel);
        Assert.True(w.M.Power.Needed(Id(w, Burner)));
        Break(w, Burner);
        Assert.False(w.M.Power.Needed(Id(w, Burner)));
    }

    // Repair.

    [Fact]
    public void RepairCostsAShareOfTheBuildRoundedUpNeverACircuit()
    {
        var w = World();
        foreach (int cell in new[] { Lamp, Generator, Panel }) Break(w, cell);
        Assert.Equal(new[] { ("wire", 1), ("scrap", 1) }, w.M.Building.RepairCost(Id(w, Lamp)));
        Assert.Equal(new[] { ("frame", 1), ("rotor", 1), ("tube", 1), ("wire", 1) }, w.M.Building.RepairCost(Id(w, Generator)));
        Assert.Equal(new[] { ("frame", 1), ("solarCell", 2), ("wire", 1) }, w.M.Building.RepairCost(Id(w, Panel)));
        Assert.Null(w.M.Building.RepairCost(Id(w, Far)));
        Assert.Equal(new[] { ("arcLamp", 1) }, Costs.RepairOf("arcLamp"));
    }

    [Fact]
    public void RepairIsRefusedForAMachineNotBrokenAndWhenShortTakingNothing()
    {
        var w = World().GiveItems(0, ("wire", 1));
        Break(w, Lamp);
        w.Do(new Repair(Id(w, Far))).Do(new Repair(Id(w, Lamp))).Do(new Repair(9999)).Do(new Repair(Id(w, Crate))).Run();
        Assert.Equal(new[] { "building.notBroken", "items.notEnough", "building.notBroken", "building.notBroken" }, Reasons(w));
        Assert.Equal(1, Count(w, "wire"));
        Assert.Equal((0, 15), w.M.Building.RepairProgress(Id(w, Lamp)));
    }

    [Fact]
    public void TheFirstTickPaysTwoHeroesWorkTwiceAsFastAndTheLastMends()
    {
        var w = World().GiveItems(0, ("wire", 1), ("scrap", 1));
        int lamp = Id(w, Lamp);
        Break(w, Lamp);
        int ticks = 0;
        while (w.M.Power.IsBroken(lamp))
        {
            w.Do(new Repair(lamp) { From = 0 }).Do(new Repair(lamp) { From = 1 }).Run();
            ticks++;
            if (ticks == 1) Assert.Equal((2, 15), w.M.Building.RepairProgress(lamp));
        }
        Assert.Equal(8, ticks);
        Assert.Equal((0, 0), (Count(w, "wire"), Count(w, "scrap")));
        // Peer 1 carried nothing and paid nothing; the work after the last tick is refused, quietly in the client.
        Assert.Equal(new[] { "building.notBroken" }, Reasons(w));
        w.Run();
        // Supply 5 against the generator's 4 and both lamps.
        Assert.Equal(1000 * 5 / 6, w.M.Power.Satisfaction(lamp));
    }

    [Fact]
    public void ProgressIsKeptAndCreativeTakesNothing()
    {
        var w = World().GiveItems(0, ("wire", 1), ("scrap", 1)).Do(new SetCreative(true)).Run();
        int lamp = Id(w, Lamp);
        Break(w, Lamp);
        w.Do(new Repair(lamp)).Run(100);
        Assert.Equal((1, 15), w.M.Building.RepairProgress(lamp));
        Assert.Equal((1, 1), (Count(w, "wire"), Count(w, "scrap")));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void TakingABrokenMachineApartRefundsItsCostLessTheRepairPaidOrNot()
    {
        foreach (bool paid in new[] { false, true })
        {
            var w = World().GiveItems(0, ("wire", 1), ("scrap", 1));
            int lamp = Id(w, Lamp);
            Break(w, Lamp);
            if (paid) w.Do(new Repair(lamp)).Run();
            w.Do(new Deconstruct(Lamp, "machine")).Run();
            int plan = w.M.Building.Plans.Single().Id;
            Assert.Equal(new[] { ("wire", 1), ("scrap", 1) }, w.M.Building.RefundOf(plan));
            while (w.M.Building.Plans.Count > 0) w.Do(new Work(plan)).Run();
            Assert.Equal(paid ? (1, 1) : (2, 2), (Count(w, "wire"), Count(w, "scrap")));
            Assert.Empty(w.M.Power.Broken);
            Assert.Empty(((BuildingState)w.M.Building.Slice).Repairs);
        }
    }

    // Saves.

    [Fact]
    public void ASaveMidWarningAndMidRepairPlaysOnAlike()
    {
        var w = World(meteors: true).GiveItems(0, ("wire", 1), ("scrap", 1));
        Break(w, Lamp);
        w.Do(new Repair(Id(w, Lamp))).Do(new FlareNow(Starting.Warning)).Run(5);
        var loaded = new Scenario(Layout.Parse(Rows, null, Cables) with { Meteors = true }, with: m => [new Breaker(m.Power)], doubled: true)
            .Reload(w.World.Save());
        Assert.Equal(w.World.Hash(), loaded.World.Hash());
        Assert.Equal(w.M.Building.RepairProgress(Id(w, Lamp)), loaded.M.Building.RepairProgress(Id(w, Lamp)));
        w.Run(Starting.Warning);
        loaded.Run(Starting.Warning);
        Assert.Equal(w.M.Power.Broken, loaded.M.Power.Broken);
        Assert.Equal(w.World.Hash(), loaded.World.Hash());
    }

    /// <summary>
    /// Strikes, repairs from two peers, a cable cut and laid again, with a copy loaded from the save before every tick: the copy plays
    /// that tick alike. The generic suites rarely see machines broken; this one sees them most ticks.
    /// </summary>
    [Fact]
    public void AWorldLoadedBeforeEveryTickPlaysOnAlikeThroughStrikesAndRepairs()
    {
        var w = World(seed: 3);
        foreach (int peer in new[] { 0, 1 })
            w.GiveItems(peer, ("wire", 30), ("scrap", 30), ("frame", 30), ("rotor", 30), ("tube", 30), ("solarCell", 30), ("plate", 30), ("coil", 30));
        int brokenTicks = 0, repairTicks = 0;
        for (int t = 0; t < 300; t++)
        {
            var commands = new List<Command>();
            if (t % 25 == 0) commands.Add(new FlareNow(1));
            if (t == 100) commands.Add(new Cut(Lamp, Far));
            if (t == 160) commands.Add(new Lay(Lamp, Far));
            if (w.M.Power.Broken.Count > 0)
                foreach (int peer in new[] { 0, 1 }) commands.Add(new Repair(w.M.Power.Broken[peer % w.M.Power.Broken.Count]) { From = peer });
            var loaded = World(seed: 3).Reload(w.World.Save());
            foreach (var c in commands) { w.Do(c); loaded.Do(c); }
            w.Run();
            loaded.Run();
            Assert.Equal(w.World.Hash(), loaded.World.Hash());
            if (w.M.Power.Broken.Count > 0) brokenTicks++;
            if (((BuildingState)w.M.Building.Slice).Repairs.Count > 0) repairTicks++;
        }
        Assert.True(brokenTicks > 100 && repairTicks > 100, $"{brokenTicks} ticks broken, {repairTicks} repairing");
    }

    // Invariants.

    /// <summary>
    /// Strikes and repairs drawn at random: <c>Broken</c> holds existing powered machines once, sorted; each strike breaks its share
    /// of the candidates, rounded up; a broken machine's satisfaction is 0; a repair takes its cost exactly once.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property StrikesAndRepairsKeepBrokenAndCostsTrue()
    {
        var gen =
            from seed in Gen.Choose(1, 10000)
            from count in Gen.Choose(1, 30)
            from steps in Gen.Choose(0, 2).ArrayOf(count)
            select (seed: (uint)seed, steps);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            string[] kinds = ["wire", "scrap", "frame", "rotor", "tube", "solarCell", "plate", "coil"];
            var w = World(seed: t.seed).GiveItems(0, [.. kinds.Select(k => (k, 60))]);
            var bag = kinds.ToDictionary(k => k, k => Count(w, k));
            var paid = new List<IReadOnlyList<(string Item, int Count)>>();
            foreach (int step in t.steps)
            {
                var before = w.M.Power.Broken.ToList();
                if (step == 0)
                {
                    int n = Cabled.Count(c => !before.Contains(Id(w, c)));
                    w.Do(new FlareNow(1)).Run();
                    int broke = w.M.Power.Broken.Count - before.Count;
                    if (broke < (n * Starting.PercentMin + 99) / 100 || broke > (n * Starting.PercentMax + 99) / 100) return false.Label($"{broke} of {n}");
                }
                else if (before.Count > 0)
                {
                    int machine = before[step == 1 ? 0 : before.Count - 1];
                    var cost = w.M.Building.RepairCost(machine)!;
                    bool unpaid = w.M.Building.RepairProgress(machine) is (0, _);
                    w.Do(new Repair(machine)).Run();
                    if (unpaid && w.M.Building.RepairProgress(machine) is not (0, _)) paid.Add(cost);
                    for (int i = 0; i < 20; i++) w.Do(new Repair(machine)).Run();
                }
                var broken = w.M.Power.Broken;
                if (!broken.SequenceEqual(broken.Distinct().Order())) return false.Label("unsorted");
                if (!broken.All(id => w.M.Grid.MachineById(id) is { } m && Machines.Get(m.Kind).Powered && w.M.Power.Satisfaction(id) == 0))
                    return false.Label("a broken machine not powered or working");
            }
            // Every repair begun took its cost once; nothing else left the bag.
            foreach (var cost in paid)
                foreach (var (item, count) in cost) bag[item] -= count;
            return bag.All(kv => Count(w, kv.Key) == kv.Value).Label("a repair paid more or less than once");
        });
    }
}
