using Stillwake.Game.Dev;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The testbed: every machine on its map, every item in its crate, and its world ready the moment it opens.</summary>
public class TestbedTests
{
    private static readonly Lazy<Registry> Built = new(() =>
    {
        var m = new Registry();
        Testbed.Create(m.All);
        return m;
    });

    [Fact]
    public void ItsPipesJoinTwoPortsOfOneWayOnlyWhereDrawnSoNeverTwoFluidsAndItsCisternsByWater()
    {
        var m = Built.Value;
        Assert.NotEmpty(m.Pipes.Spans);
        // Drawn before ports had a way: the tank piped straight to the generator beside it, kept as drawn (2026-10-29-typed-ports).
        var oneWay = m.Pipes.Spans.Where(s => m.Grid.PortAt(s.A) is { } a && m.Grid.PortAt(s.B) is { } b && a.Way == b.Way)
            .Select(s => (m.Grid.MachineAt(s.A)!.Kind, m.Grid.MachineAt(s.B)!.Kind));
        Assert.Equal([("tank", "generator")], oneWay);
        // By lines, every valve counted open: a span's two ends of one fluid wherever both have one.
        Assert.All(m.Pipes.Spans, s => Assert.True(m.Pipes.FluidAt(s.A) is not { } a || m.Pipes.FluidAt(s.B) is not { } b || a == b, $"span {s.A}–{s.B}"));
        var cisterns = m.Grid.MachinesOf("cistern");
        Assert.Equal(2, cisterns.Count);
        var water = m.Pipes.Networks.Single(n => m.Pipes.FluidAt(n.Cells[0]) == Stillwake.Content.Fluids.Water);
        Assert.Equal(cisterns.Select(c => c.Id).Order(), water.Machines.Order());
    }

    private static int IdOf(Registry m, string kind) => m.Grid.Machines.Single(x => x.Kind == kind).Id;

    /// <summary>The crate with everything and the farm's, by their cells on the map.</summary>
    private static int[] Crates(Registry m) => [CrateAt(m, Testbed.Full), CrateAt(m, Testbed.FarmAt)];

    private static int CrateAt(Registry m, (int X, int Y) cell) => m.Grid.MachineAt(cell.Y * Testbed.Map.Width + cell.X)!.Id;

    [Fact]
    public void DrawsEveryKindOfMachine()
    {
        var drawn = Testbed.Map.Machines().Select(x => x.Kind).ToHashSet();
        // A pile is only ever in the hold of the ship a world starts in.
        Assert.All(Spec.Machines.All.Where(m => m.Menu != null || m.Fixed), spec => Assert.Contains(spec.Kind, drawn));
    }

    [Fact]
    public void ItsCratesHoldAFullStackOfEveryItemAHeroCanCarryAndNoWreckageTheFarmsInASecond()
    {
        Assert.True(Testbed.Carried.Count(i => !Testbed.InFarmCrate(i.Id) && !Testbed.InSupply.Contains(i.Id)) < Spec.Machines.Get("crate").Slots);
        var m = Built.Value;
        var crate = m.Items.Crate(Crates(m)[0]);
        var farm = m.Items.Crate(Crates(m)[1]);
        Assert.All(Testbed.Carried.Where(i => !Testbed.InSupply.Contains(i.Id)), item => Assert.Contains(new Stack(item.Id, item.Stack), Testbed.InFarmCrate(item.Id) ? farm : crate));
        // A drill with the arc fitted (0098), in the supply crate by the door: no bench fits one yet.
        Assert.Contains(new Stack(Spec.Weapons.Drill.Item, 1, Module: Spec.Modules.Arc.Item), m.Items.Crate(CrateAt(m, Testbed.Supply)));
        Assert.Contains(new Stack(Spec.HelmetLamp.Item, 1), m.Items.Crate(CrateAt(m, Testbed.Supply)));
        // An arc module for each machine and tool it fits (0086, 0098, 0100): the dev's way to one before the ruins give its shard.
        Assert.Equal(5, m.Items.Crate(CrateAt(m, Testbed.Supply)).Count(s => s?.Item == Spec.Modules.Arc.Item));
        // A plain drill in the arc bench's vise (0099), to fit there.
        Assert.Equal(new Stack(Spec.Weapons.Drill.Item, 1), m.Items.Crate(IdOf(m, Testbed.BenchHolds.Kind))[0]);
        Assert.Equal(Testbed.FarmCrate.Select(i => new Stack(i, Spec.Items.Get(i).Stack)), farm.Where(s => s != null));
        Assert.DoesNotContain(crate, stack => stack is { } s && Spec.Wreckage.HpOf(s.Item) != null);
        Assert.Contains(crate, stack => stack is { Item: "circuit" });
        Assert.Contains(crate, stack => stack is { Item: "harpoon" });
        Assert.Contains(crate, stack => stack is { Item: "jetpack" });
        Assert.Contains(crate, stack => stack is { Item: "canister" });
        // A battery of each kind: the crate's empty one, as every carried item at its stack, and one charged full.
        Assert.Contains(new Stack("battery", 1, Spec.Charge.Battery), crate);
    }

    [Fact]
    public void LaunchesItsPodFromTheAirlockFloorOverSpace()
    {
        var m = new Registry();
        var world = Testbed.Create(m.All);
        int pod = IdOf(m, "dropPod");
        Assert.Equal(Testbed.PodFuel, m.Items.HeldFor(pod, "biofuel"));
        // A hero in the airlock boards it; its run is powered and its shaft clear, so it flies.
        world.Step([new Stillwake.Sim.Crew.Enter(Testbed.Map.Width * 15 + 94), new Stillwake.Sim.Landing.Board(pod)]);
        Assert.Null(m.Landing.LaunchProblem(0));
        Assert.Empty(world.Step([new Stillwake.Sim.Landing.Launch()]));
        Assert.NotNull(m.Landing.FlightOf(0));
    }

    [Fact]
    public void IsReadyTheMomentItOpens()
    {
        var m = Built.Value;
        Assert.True(m.Items.Creative);
        Assert.True(m.Furnace.At(IdOf(m, "furnace")).Burning);
        Assert.True(m.Furnace.At(IdOf(m, "burnerGenerator")).Burning);
        var airlock = m.Rooms.RoomAt(Testbed.Map.Width * 12 + 92);
        Assert.NotNull(airlock);
        Assert.All(m.Rooms.All, room => Assert.Equal(room.Id == airlock ? RoomStatus.Vacuum : RoomStatus.Aired, m.Atmosphere.StatusOf(room.Id)));
        Assert.All(m.Grid.Machines.Where(x => Spec.Machines.Get(x.Kind).Draws != null), x => Assert.Equal(1000, m.Power.Satisfaction(x.Id)));
        int charger = IdOf(m, "charger");
        Assert.True(m.Power.Charges(charger));
        // Charged both ticks, by its own panel on the roof.
        Assert.Equal(new Stack("battery", 1, Spec.Charge.Battery / 1000 * Testbed.Charging + 2 * Spec.Machines.Get("charger").Draws!.Value * Spec.Charge.Scale), m.Items.Crate(charger)[0]);
        // The Former at work on the second tick, once the generator's power reaches it.
        int former = IdOf(m, "former");
        Assert.True(m.Crafting.Works(former));
        Assert.Equal(Spec.Stations.Get("former").Speed, m.Crafting.Progress(former));
        // Endless plates fed from its inputs: the first took its iron as it began, creative mode or not.
        Assert.True(m.Crafting.Queue(former).Single() is { Recipe: "plate", FromInputs: true, Endless: true });
        Assert.Equal(new Stack(Testbed.Pressing, Testbed.Pressed - 2), m.Items.Crate(former)[0]);
        // The smelter at work on both ticks, its own panel on the roof feeding it.
        int smelter = IdOf(m, "smelter");
        Assert.True(m.Crafting.Works(smelter));
        Assert.Equal(2 * Spec.Stations.Get("smelter").Speed, m.Crafting.Progress(smelter));
        Assert.Equal(Testbed.Smelting, m.Items.Crate(smelter)[0]?.Item);
        // The assembler at work on both ticks, on its own panel on the roof.
        int assembler = IdOf(m, "assembler");
        Assert.True(m.Crafting.Works(assembler));
        Assert.Equal(2 * Spec.Stations.Get("assembler").Speed, m.Crafting.Progress(assembler));
        Assert.Equal(new[] { ("circuit", Testbed.Assembling) }, m.Crafting.Queue(assembler).Select(e => (e.Recipe, e.Count)));
        // The grow capsule growing on both ticks, on the assembler's panel, its seeds planted.
        int capsule = IdOf(m, "growCapsule");
        Assert.True(m.Crafting.Works(capsule));
        Assert.Equal(2 * Spec.Stations.Get("growCapsule").Speed, m.Crafting.Progress(capsule));
        Assert.Equal(new Stack("seed", Testbed.Planted), m.Items.Crate(capsule)[0]);
    }

    [Fact]
    public void ItsTwoDronesServeTheFurnaceTheFormerAndTheCapsuleFromTheSupplyCrateTheFarmCrateSwitchedOff()
    {
        var m = Built.Value;
        int bay = IdOf(m, Spec.Logistics.Bay), supply = CrateAt(m, Testbed.Supply);
        Assert.False(m.Logistics.Served(Crates(m)[1]));
        Assert.True(m.Logistics.Served(supply) && m.Logistics.Served(Crates(m)[0]));
        // Room one, beside the pod: through the closed door from room two, powered to charge its drones.
        Assert.True(m.Grid.MachineById(bay)!.Cell % Testbed.Map.Width < 34);
        Assert.Equal(Testbed.Drones, m.Items.Crate(bay).Count(s => s?.Item == Spec.Logistics.Drone) + m.Logistics.Drones.Count);
        Assert.Equal(1000, m.Power.Satisfaction(bay));
        Assert.All((string[])["furnace", "former", "growCapsule", "smelter", "assembler"], kind => Assert.True(m.Logistics.Reached(IdOf(m, kind)), kind));
        Assert.True(m.Logistics.Reached(supply));
    }

    [Fact]
    public void ItsDronesKeepTheFurnaceTheFormerAndTheCapsuleGoingForTenMinutesInAirThatHolds()
    {
        var m = new Registry();
        var world = Testbed.Create(m.All);
        int furnace = IdOf(m, "furnace"), former = IdOf(m, "former"), capsule = IdOf(m, "growCapsule");
        int[] crates = [CrateAt(m, Testbed.Supply), Crates(m)[0]];
        var launched = new HashSet<int>();
        int empty = 0, longest = 0;
        for (int t = 0; t < 10 * 60 * World.TicksPerSecond; t++)
        {
            world.Step([]);
            foreach (var d in m.Logistics.Drones) launched.Add(d.Id);
            empty = m.Items.Crate(furnace)[0] == null ? empty + 1 : 0;
            longest = Math.Max(longest, empty);
            if (t == 1200)
            {
                // Within a minute: a stack brought each, or near it as they work, and what they made in a crate.
                Assert.True(m.Items.HeldFor(furnace, "planks") > 25 && m.Items.HeldFor(former, "iron") > 250 && m.Items.HeldFor(capsule, "seed") >= 5);
                var held = crates.SelectMany(c => m.Items.Crate(c)).Where(s => s != null).Select(s => s!.Item).ToList();
                Assert.Contains("scrap", held);
                Assert.Contains("plate", held);
            }
        }
        // The furnace fed the whole ten minutes: its input empty only while both drones charge, never half a minute, and
        // most of what the supply crate held smelted.
        Assert.True(longest < 30 * World.TicksPerSecond, $"the furnace stood empty {longest} ticks");
        int left = m.Items.Crate(crates[0]).Sum(s => s?.Item == "scrapChunk" ? s.Count : 0);
        Assert.True(Testbed.Supplies.Where(s => s.Item == "scrapChunk").Sum(s => s.Count) - left >= 120, $"{left} chunks left");
        // The air held: every room but the airlock aired.
        var airlock = m.Rooms.RoomAt(Testbed.Map.Width * 12 + 92);
        Assert.All(m.Rooms.All.Where(r => r.Id != airlock), r => Assert.Equal(RoomStatus.Aired, m.Atmosphere.StatusOf(r.Id)));
        // Two drones, many jobs, few launches: a drone takes its next job from where it is.
        Assert.True(launched.Count < 60, $"{launched.Count} launches");
        Assert.True(m.Items.Floor.Count == 0, string.Join(", ", m.Items.Floor.Select(f => $"{f.Item} {f.Count} at ({f.Cell % Testbed.Map.Width},{f.Cell / Testbed.Map.Width})")));
    }
}
