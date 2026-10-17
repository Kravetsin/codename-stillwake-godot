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

    [Fact]
    public void DrawsEveryKindOfMachine()
    {
        var drawn = Testbed.Map.Machines().Select(x => x.Kind).ToHashSet();
        Assert.All(Spec.Machines.All, spec => Assert.Contains(spec.Kind, drawn));
    }

    [Fact]
    public void ItsCrateHoldsAFullStackOfEveryItemAHeroCanCarryAndNoWreckage()
    {
        Assert.True(Testbed.Carried.Count() <= Spec.Machines.Get("crate").Slots);
        var m = Built.Value;
        var crate = m.Items.Crate(IdOf(m, "crate"));
        Assert.All(Testbed.Carried, item => Assert.Contains(new Stack(item.Id, item.Stack), crate));
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
        var airlock = m.Rooms.RoomAt(Testbed.Map.Width * 6 + 34);
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
        Assert.Equal(Testbed.Pressing, m.Crafting.Queue(former).Single().Count);
    }
}
