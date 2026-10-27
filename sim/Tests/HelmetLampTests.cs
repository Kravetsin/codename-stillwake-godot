using Stillwake.Content;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The helmet lamp (0077): crafted at the lifepod of a plate, wire and silicon, worn on the head, drawing nothing.</summary>
public class HelmetLampTests
{
    private const string Room = """
        ##########
        #........#
        #.OOO....#
        #.OOO....#
        #.OOO....#
        ##########
        """;

    private const int Pod = 1;
    private static readonly string Lamp = HelmetLamp.Item;
    private static readonly int Head = Inventory.Slots, Back = Inventory.Slots + 1, LampSlot = Inventory.Slots + Inventory.Equipment.ToList().IndexOf(Content.Helmet.Fitting);

    [Fact]
    public void TheLifepodMakesOneOfAPlateTwoWireAndSiliconOnceTheyAreAnalysed()
    {
        var w = new Scenario(Layout.Parse(Room) with { Known = [] }, doubled: true).GiveItems(0, ("plate", 1), ("wire", 2), ("silicon", 1));
        Assert.Contains(Lamp, Stations.Get("lifepod").Rows.SelectMany(r => r));
        Assert.Equal([("plate", 1), ("wire", 2), ("silicon", 1)], Crafts.Get(Lamp).Cost);
        Assert.False(w.M.Research.Knows(Lamp));
        foreach (var item in new[] { "plate", "wire", "silicon" }) w.M.Knowledge.Learn(item);
        Assert.True(w.M.Research.Knows(Lamp));
        w.Do(new Crafting.Craft(Pod, Lamp, 1)).Run(Crafts.Get(Lamp).Ticks * 1000 / Stations.Get("lifepod").Speed + 1);
        Assert.Empty(w.Refusals);
        Assert.Equal(new Stack(Lamp, 1), w.M.Items.Crate(Pod)[0]);
        Assert.Empty(w.M.Items.Totals(0));
    }

    [Fact]
    public void IsWornInTheHelmetsLampSlotAloneAndDrawsNothingThere()
    {
        var w = new Scenario(Room).GiveItems(0, (Lamp, 1));
        // Neither on the back nor in the helmet's place: in the slot fixed onto any helmet (2026-10-27-suits-are-worn).
        w.Do(new Move(new Items.Place(0), new Target(Back), 1)).Do(new Move(new Items.Place(0), new Target(Head), 1)).Run();
        Assert.Equal(["items.notEquipment", "items.notEquipment"], w.Refusals.Select(r => r.Reason));
        w.Do(new Move(new Items.Place(0), new Target(LampSlot), 1)).Run(100);
        Assert.True(w.M.Items.Equipped(0, Lamp));
        Assert.Null(w.M.Items.Running(0));
        Assert.Null(Content.Items.Get(Lamp).Draw);
        // Kept when its hero dies, as all that is worn.
        Assert.True(Content.Items.Get(Lamp).Gear);
    }
}
