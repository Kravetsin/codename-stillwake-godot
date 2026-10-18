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

    private static int IdOf(Registry m, string kind) => m.Grid.Machines.Single(x => x.Kind == kind).Id;

    /// <summary>The crate with everything and the farm's, by their cells on the map.</summary>
    private static int[] Crates(Registry m) => [CrateAt(m, Testbed.Full), CrateAt(m, Testbed.FarmAt)];

    private static int CrateAt(Registry m, (int X, int Y) cell) => m.Grid.MachineAt(cell.Y * Testbed.Map.Width + cell.X)!.Id;

    [Fact]
    public void DrawsEveryKindOfMachine()
    {
        var drawn = Testbed.Map.Machines().Select(x => x.Kind).ToHashSet();
        Assert.All(Spec.Machines.All, spec => Assert.Contains(spec.Kind, drawn));
    }

    [Fact]
    public void ItsCratesHoldAFullStackOfEveryItemAHeroCanCarryAndNoWreckageTheFarmsInASecond()
    {
        Assert.True(Testbed.Carried.Count(i => !Testbed.Farm.Contains(i.Id)) < Spec.Machines.Get("crate").Slots);
        var m = Built.Value;
        var crate = m.Items.Crate(Crates(m)[0]);
        var farm = m.Items.Crate(Crates(m)[1]);
        Assert.All(Testbed.Carried, item => Assert.Contains(new Stack(item.Id, item.Stack), Testbed.Farm.Contains(item.Id) ? farm : crate));
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
    public void IsReadyTheMomentItOpens()
    {
        var m = Built.Value;
        Assert.True(m.Items.Creative);
        Assert.True(m.Furnace.At(IdOf(m, "furnace")).Burning);
        Assert.True(m.Furnace.At(IdOf(m, "burnerGenerator")).Burning);
        var airlock = m.Rooms.RoomAt(Testbed.Map.Width * 6 + 46);
        Assert.NotNull(airlock);
        Assert.All(m.Rooms.All, room => Assert.Equal(room.Id == airlock ? RoomStatus.Vacuum : RoomStatus.Aired, m.Atmosphere.StatusOf(room.Id)));
        Assert.All(m.Grid.Machines.Where(x => Spec.Machines.Get(x.Kind).Draws != null), x => Assert.Equal(1000, m.Power.Satisfaction(x.Id)));
        Assert.Equal(new Analyzing(Testbed.Analysing, 2000, Spec.Research.Ticks * 1000, null), m.Research.At(IdOf(m, "analyzer")));
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
    public void ItsTwoDronesServeTheFurnaceTheFormerAndTheCapsuleFromTheSupplyCrateTheFullCrateSwitchedOff()
    {
        var m = Built.Value;
        int bay = IdOf(m, Spec.Logistics.Bay), supply = CrateAt(m, Testbed.Supply);
        Assert.False(m.Logistics.Served(Crates(m)[0]));
        Assert.True(m.Logistics.Served(supply));
        // Room one, beside the pod: through the closed door from room two, powered to charge its drones.
        Assert.True(m.Grid.MachineById(bay)!.Cell % Testbed.Map.Width < 17);
        Assert.Equal(Testbed.Drones, m.Items.Crate(bay).Count(s => s?.Item == Spec.Logistics.Drone) + m.Logistics.Drones.Count);
        Assert.Equal(1000, m.Power.Satisfaction(bay));
        Assert.All((string[])["furnace", "former", "growCapsule", "smelter", "assembler"], kind => Assert.True(m.Logistics.Reached(IdOf(m, kind)), kind));
        Assert.True(m.Logistics.Reached(supply));
    }

    [Fact]
    public void ItsDronesKeepTheFurnaceTheFormerAndTheCapsuleGoingAndCarryWhatTheyMakeToTheSupplyCrateWithinAMinute()
    {
        var m = new Registry();
        var world = Testbed.Create(m.All);
        int supply = CrateAt(m, Testbed.Supply), furnace = IdOf(m, "furnace"), former = IdOf(m, "former"), capsule = IdOf(m, "growCapsule");
        bool chained = false;
        var launched = new HashSet<int>();
        for (int t = 0; t < 1200; t++)
        {
            world.Step([]);
            foreach (var d in m.Logistics.Drones) launched.Add(d.Id);
        }
        int Held(int id, string item) => m.Items.HeldFor(id, item);
        // Brought a stack each, or near it as they work.
        Assert.True(Held(furnace, "planks") > 25 && Held(former, "iron") > 250 && Held(capsule, "seed") >= 5);
        var held = m.Items.Crate(supply).Where(s => s != null).Select(s => s!.Item).ToList();
        Assert.Contains("scrap", held);
        Assert.Contains("plate", held);
        // Two drones, many jobs, few launches: a drone takes its next job from where it is.
        chained = launched.Count < 20;
        Assert.True(chained, $"{launched.Count} launches");
        Assert.Empty(m.Items.Floor);
    }
}
