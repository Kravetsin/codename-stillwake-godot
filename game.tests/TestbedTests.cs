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
    }
}
