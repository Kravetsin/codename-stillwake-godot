using Stillwake.Content;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The light drone (light-drone): crafted at the workbench of a plate, wire and a coil, worn in the helmet's mount slot, drawing nothing.</summary>
public class LightDroneTests
{
    private const string Room = """
        ##########
        #........#
        #.OOOII..#
        #.OOOII..#
        #.OOOII..#
        ##########
        """;

    private const int Bench = 2;
    private static readonly string Drone = LightDrone.Item;
    private static readonly int Head = Inventory.Slots, Back = Inventory.Slots + 1, Mount = Inventory.Slots + Inventory.Equipment.ToList().IndexOf(Content.Helmet.Mount);

    [Fact]
    public void TheWorkbenchMakesOneOfAPlateTwoWireAndACoilOnceBought()
    {
        var w = new Scenario(Layout.Parse(Room) with { Known = [] }, doubled: true).GiveItems(0, ("plate", 1), ("wire", 2), ("coil", 1));
        Assert.Contains(Drone, Stations.Get("workbench").Rows.SelectMany(r => r));
        Assert.Equal([("plate", 1), ("wire", 2), ("coil", 1)], Crafts.Get(Drone).Cost);
        Assert.False(w.M.Research.Knows(Drone));
        w.M.Knowledge.Grant(Drone);
        Assert.True(w.M.Research.Knows(Drone));
        w.Do(new Crafting.Craft(Bench, Drone, 1)).Run(Crafts.Get(Drone).Ticks * 1000 / Stations.Get("workbench").Speed + 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack(Drone, 1), w.M.Items.Crate(Bench)[0]);
        Assert.Empty(w.M.Items.Totals(0));
    }

    [Fact]
    public void IsWornOnTheHelmetsMountAloneAndDrawsNothingThere()
    {
        var w = new Scenario(Room).GiveItems(0, (Drone, 1));
        // Neither on the back nor in the helmet's place: in the slot fixed onto any helmet (2026-10-27-suits-are-worn).
        w.Do(new Move(new Items.Place(0), new Target(Back), 1)).Do(new Move(new Items.Place(0), new Target(Head), 1)).Run();
        Assert.Equal(["items.notEquipment", "items.notEquipment"], w.Refusals.Select(r => r.Reason));
        w.Do(new Move(new Items.Place(0), new Target(Mount), 1)).Run(100);
        Assert.True(w.M.Items.Equipped(0, Drone));
        Assert.Null(w.M.Items.Running(0));
        Assert.Null(Content.Items.Get(Drone).Draw);
        // Kept when its hero dies, as all that is worn.
        Assert.True(Content.Items.Get(Drone).Gear);
    }
}
