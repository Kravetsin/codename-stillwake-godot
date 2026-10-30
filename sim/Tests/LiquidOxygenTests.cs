using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Power;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Liquid oxygen (liquid-oxygen): tanks hold liquid, the vaporizer turns it into the air its vents draw.</summary>
public class LiquidOxygenTests
{
    /// <summary>A sealed room: a vaporizer at (4, 1) feeding a vent at (6, 2); under the hull a tank and a panel cabled to it.</summary>
    private static readonly string[] Room = ["##########", "#...TT...#", "#...TTV..#", "##########"];
    private static readonly string[] RoomUnder = ["", "", "    ---"];

    /// <summary>One room, two chains: vaporizers at (3, 1) and (9, 1), vents at (5, 2) and (11, 2), each its tank and panel under the hull.</summary>
    private static readonly string[] Twin = ["##############", "#..TT....TT..#", "#..TTV...TTV.#", "##############"];
    private static readonly string[] TwinUnder = ["", "", "   ---    ---"];

    /// <summary>The cell at (<paramref name="x"/>, <paramref name="y"/>), counted in cells of the doubled map.</summary>
    private static int Cell(Scenario w, int x, int y) => y * w.M.Grid.Width + x;

    private static Scenario Chain(string[] rows, string[] under) =>
        new Scenario(AirTesting.Vaporized(rows, under), doubled: true).GiveItems(0, ("canister", 8), ("tube", 20), ("gear", 4));

    private static int[] Ids(Scenario w, string kind) => [.. w.M.Grid.MachinesOf(kind).Select(m => m.Id)];
    private static int Oxygen(Scenario w, int id) => w.M.Atmosphere.OxygenIn(id);
    private static RoomAir Air(Scenario w) => w.M.Atmosphere.RoomsAir().Single();
    private static int Making(Scenario w, int vaporizer) => w.Air().Making.Find(m => m.Generator == vaporizer)?.Part ?? 0;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>All the oxygen there is: liquid in tanks, gas in stores, a unit in each aired cell.</summary>
    private static long Total(Scenario w) => w.Air().Tanks.Sum(t => (long)t.Oxygen) + w.M.Atmosphere.AiredCells.Count;

    [Fact]
    public void AnEmptyTankLeavesAPoweredVaporizerIdleAndItsRoomInVacuumDry()
    {
        var w = Chain(Room, RoomUnder);
        int vaporizer = Ids(w, "generator")[0], vent = Ids(w, "vent")[0];
        w.Run(Content.Air.LeakEvery);
        Assert.Equal(1000, w.M.Power.Satisfaction(vaporizer));
        Assert.Equal((RoomStatus.Vacuum, 0, "atmosphere.dry"), (Air(w).Status, Air(w).Oxygen, Air(w).Problem));
        Assert.Equal(0, Oxygen(w, vaporizer));
        Assert.Equal(0, Making(w, vaporizer));
        Assert.True(w.M.Atmosphere.Fed(vent));
        Assert.False(w.M.Atmosphere.Supplied(vent));
    }

    [Fact]
    public void TurnsACanisterIntoAirAUnitATickWhatTheTankLosesTheStoreAndTheRoomGain()
    {
        var w = Chain(Room, RoomUnder);
        int tank = Ids(w, "tank")[0], vaporizer = Ids(w, "generator")[0], vent = Ids(w, "vent")[0];
        w.Do(new LoadCanister(tank)).Run();
        Assert.Equal(Content.Air.Canister - 1, Oxygen(w, tank));
        for (int i = 0; i < 30; i++)
        {
            int liquid = Oxygen(w, tank);
            long gas = Oxygen(w, vaporizer) + w.M.Atmosphere.AiredCells.Count;
            w.Run();
            Assert.Equal(liquid - 1, Oxygen(w, tank));
            Assert.Equal(gas + 1, Oxygen(w, vaporizer) + w.M.Atmosphere.AiredCells.Count);
            // Every unit goes out as it is made: the room fills, never in vacuum while the store refills.
            Assert.Equal(RoomStatus.Filling, Air(w).Status);
            Assert.True(w.M.Atmosphere.Supplied(vent));
        }
    }

    [Fact]
    public void MakesNothingUnpoweredAndSaysWhy()
    {
        var w = Chain(Room, RoomUnder);
        int tank = Ids(w, "tank")[0];
        w.Do(new ClearAt(w.M.Grid.MachinesOf("solarPanel")[0].Cell, "machine")).Do(new LoadCanister(tank)).Run(200);
        Assert.Equal(Content.Air.Canister, Oxygen(w, tank));
        Assert.Equal((RoomStatus.Vacuum, Content.Air.Canister, "atmosphere.unpowered"), (Air(w).Status, Air(w).Oxygen, Air(w).Problem));
    }

    [Fact]
    public void AtPartPowerMakesItsShareFromItsOwnTank()
    {
        // The second panel taken away and the first cabled to both vaporizers: 5 for 8, each at 625.
        var w = Chain(Twin, TwinUnder).Do(new SetCreative(true));
        int[] tanks = Ids(w, "tank"), vaporizers = Ids(w, "generator");
        var panels = w.M.Grid.MachinesOf("solarPanel");
        w.Do(new ClearAt(panels[1].Cell, "machine")).Do(new Lay(panels[0].Cell, w.M.Grid.MachineById(vaporizers[1])!.Cell))
            .Do(new LoadCanister(tanks[0])).Do(new LoadCanister(tanks[1])).Run(81);
        Assert.Empty(w.Refusals);
        Assert.All(vaporizers, v => Assert.Equal(625, w.M.Power.Satisfaction(v)));
        // 81 ticks at 0.625 a tick: 50 whole units each, 0.625 of the next in hand.
        Assert.All(tanks, t => Assert.Equal(Content.Air.Canister - 50, Oxygen(w, t)));
        Assert.All(vaporizers, v => Assert.Equal(Content.Air.GeneratorRate * 625, Making(w, v)));
    }

    [Fact]
    public void AFullStoreTakesNoLiquidAndItsWorkStands()
    {
        var w = Chain(Room, RoomUnder);
        int tank = Ids(w, "tank")[0], vaporizer = Ids(w, "generator")[0];
        // No vent: nothing draws the store.
        w.Do(new ClearAt(w.M.Grid.MachinesOf("vent")[0].Cell, "machine")).Do(new LoadCanister(tank)).Run(Content.Air.GeneratorStore + 20);
        Assert.Equal(Content.Air.GeneratorStore, Oxygen(w, vaporizer));
        Assert.Equal(Content.Air.Canister - Content.Air.GeneratorStore, Oxygen(w, tank));
        int making = Making(w, vaporizer);
        w.Run(100);
        Assert.Equal(Content.Air.Canister - Content.Air.GeneratorStore, Oxygen(w, tank));
        Assert.Equal(making, Making(w, vaporizer));
    }

    [Fact]
    public void RefusesAPipeFromATankToAVent()
    {
        var w = Chain(Room, RoomUnder);
        var tank = w.M.Grid.MachinesOf("tank")[0];
        w.Do(new LayPipe(w.M.Grid.PortsOf(tank)[0], w.M.Grid.MachinesOf("vent")[0].Cell)).Run();
        Assert.Equal(["pipes.mixedFluid"], Reasons(w));
    }

    [Fact]
    public void ATankDrawnStraightToAVentAirsNothingNotPiped()
    {
        // Drawn before liquid oxygen: kept, as laid spans are, and feeding nothing.
        var w = new Scenario(Layout.Parse(["########", "#.TT...#", "#.TTV..#", "########"], ["", "  o", "  ---"]), doubled: true).Run(200);
        int vent = Ids(w, "vent")[0];
        Assert.Single(w.M.Pipes.Spans);
        Assert.False(w.M.Atmosphere.Fed(vent));
        Assert.Equal((RoomStatus.Vacuum, (int?)null, "atmosphere.notPiped"), (Air(w).Status, Air(w).Oxygen, Air(w).Problem));
        Assert.Equal(Content.Air.Canister, Oxygen(w, Ids(w, "tank")[0]));
    }

    [Fact]
    public void AValveShutOnTheLiquidLineLeavesTheVaporizerDry()
    {
        var w = Chain(Room, RoomUnder);
        var vaporizer = w.M.Grid.MachinesOf("generator")[0];
        var tank = w.M.Grid.MachinesOf("tank")[0];
        int input = w.M.Grid.PortsOf(vaporizer)[0], port = w.M.Grid.PortsOf(tank)[0], joint = Cell(w, 5, 8);
        // The drawn line replaced by two spans through a joint, made a valve and shut.
        w.Do(new CutPipe(input, port)).Do(new LayPipe(port, joint)).Do(new LayPipe(joint, input))
            .Do(new MakeValve(joint, true)).Do(new SetValve(joint, false)).Do(new LoadCanister(tank.Id)).Run(50);
        Assert.Empty(w.Refusals);
        Assert.Equal(Content.Air.Canister, Oxygen(w, tank.Id));
        Assert.Equal((RoomStatus.Vacuum, 0, "atmosphere.dry"), (Air(w).Status, Air(w).Oxygen, Air(w).Problem));
        w.Do(new SetValve(joint, true)).Run(10);
        Assert.Equal(RoomStatus.Filling, Air(w).Status);
    }

    [Fact]
    public void TwoVaporizersDrawTheTanksBehindThemInIdOrderEachTankCountedOnce()
    {
        // The two liquid lines joined through a joint: both vaporizers draw the lower tank first.
        var w = Chain(Twin, TwinUnder);
        int[] tanks = Ids(w, "tank");
        var ports = tanks.Select(t => w.M.Grid.PortsOf(w.M.Grid.MachineById(t)!)[0]).ToArray();
        int joint = Cell(w, 12, 13);
        w.Do(new LayPipe(ports[0], joint)).Do(new LayPipe(joint, ports[1]))
            .Do(new LoadCanister(tanks[0])).Do(new LoadCanister(tanks[1])).Run();
        Assert.Empty(w.Refusals);
        w.Run(40);
        Assert.Equal(Content.Air.Canister - 2 * 41, Oxygen(w, tanks[0]));
        Assert.Equal(Content.Air.Canister, Oxygen(w, tanks[1]));
        Assert.Equal(2 * Content.Air.Canister - 2 * 41, Air(w).Oxygen);
    }

    [Fact]
    public void ATankTakenApartMidWorkTakesItsLiquidAndTheVaporizerStallsItsWorkKept()
    {
        var w = Chain(Twin, TwinUnder).Do(new SetCreative(true));
        int[] tanks = Ids(w, "tank"), vaporizers = Ids(w, "generator");
        var panels = w.M.Grid.MachinesOf("solarPanel");
        // At part power, so the work holds part of a unit.
        w.Do(new ClearAt(panels[1].Cell, "machine")).Do(new Lay(panels[0].Cell, w.M.Grid.MachineById(vaporizers[1])!.Cell))
            .Do(new LoadCanister(tanks[0])).Run(3);
        int making = Making(w, vaporizers[0]);
        Assert.NotEqual(0, making);
        w.Do(new ClearAt(w.M.Grid.MachineById(tanks[0])!.Cell, "machine")).Run(50);
        Assert.Equal(0, Oxygen(w, tanks[0]));
        Assert.Equal(making, Making(w, vaporizers[0]));
    }

    [Fact]
    public void AnOldSaveLoadsItsTanksAsLiquidAndItsGeneratorsAsVaporizersNotPiped()
    {
        // A format-38 save of a tank drawn straight to a vent: the tank keeps its oxygen as liquid, the room nothing.
        var old = new Scenario(Layout.Parse(["########", "#.TT...#", "#.TTV..#", "########"], ["", "  o", "  ---"]), doubled: true);
        var save = JsonNode.Parse(old.World.Save())!.AsObject();
        save["format"] = 38;
        var w = old.Reload(Migrations.Upgrade(save.ToJsonString())).Run(100);
        Assert.Equal(Content.Air.Canister, Oxygen(w, Ids(w, "tank")[0]));
        Assert.Equal("atmosphere.notPiped", Air(w).Problem);

        // The format-38 wreck: its generator's old span now joins its input to the vent; store and work kept.
        string fixture = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "format-38.json"));
        var slice = JsonNode.Parse(fixture)!["slices"]!["atmosphere"]!;
        var m = new Registry();
        World.Load(Migrations.Upgrade(fixture), m.All);
        int generator = m.Grid.MachinesOf("generator")[0].Id;
        int stored = slice["tanks"]!.AsArray().Where(t => (int)t!["tank"]! == generator).Select(t => (int)t!["oxygen"]!).SingleOrDefault();
        int part = slice["making"]!.AsArray().Where(g => (int)g!["generator"]! == generator).Select(g => (int)g!["part"]!).SingleOrDefault();
        Assert.Equal(stored, m.Atmosphere.OxygenIn(generator));
        Assert.Equal(part, ((AtmosphereState)m.Atmosphere.Slice).Making.Find(g => g.Generator == generator)?.Part ?? 0);
        Assert.False(m.Atmosphere.Fed(m.Grid.MachinesOf("vent")[0].Id));
    }

    [Property(MaxTest = 60)]
    public Property NeverMakesOxygenButFromACanisterWhateverIsBuiltOrTakenApart()
    {
        // Steps: load a canister, take a machine apart, put a tank or a vaporizer back in its place, run a few ticks.
        var step = from op in Gen.Choose(0, 5) from i in Gen.Choose(0, 5) from n in Gen.Choose(1, 30) select (op, i, n);
        var steps = from k in Gen.Choose(1, 25) from s in step.ArrayOf(k) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = Chain(Twin, TwinUnder).Do(new SetCreative(true)).Run();
            var spots = w.M.Grid.Machines.Where(m => m.Kind is "tank" or "generator" or "vent").Select(m => (m.Kind, m.Cell)).ToArray();
            foreach (var (op, i, n) in all)
            {
                var (kind, cell) = spots[i % spots.Length];
                long before = Total(w);
                int loads = w.Refusals.Count;
                w.Do(op switch
                {
                    < 2 => new LoadCanister(w.M.Grid.MachineAt(cell)?.Id ?? 0),
                    2 => new ClearAt(cell, "machine"),
                    _ => new Place(cell, new Grid.Placement("machine", kind)),
                }).Run(n);
                bool loaded = op < 2 && w.Refusals.Count == loads;
                if (Total(w) > before + (loaded ? Content.Air.Canister : 0)) return false.Label($"oxygen grew from {before} to {Total(w)}");
            }
            return true.ToProperty();
        });
    }
}
