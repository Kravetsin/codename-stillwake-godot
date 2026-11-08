using Stillwake.Content;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Xunit;
using Tree = Stillwake.Content.Research;

namespace Stillwake.Sim.Tests;

/// <summary>The power driver (power-driver): a builder bought on the black market, made at the workbench, run on the belt.</summary>
public class PowerDriverTests
{
    private const string Driver = "powerDriver";
    private static readonly int Belt = Inventory.Slots + 2;

    /// <summary>A room with the lifepod, a crate and a workbench, knowing nothing at the start.</summary>
    private const string Room = """
        ##########
        #........#
        #.OOO..II#
        #.OOO..II#
        #.OOOKKII#
        ##########
        """;

    [Fact]
    public void RunsOnTheBeltDrawingOnePowerTickATick()
    {
        var w = new Scenario("S.......").GiveItems(0, (Driver, 1));
        w.Put(Belt, new Stack("battery", 1, Charge.Battery));
        w.Do(new Enter(1)).Run().Do(new Run(Driver, true)).Run(10);
        Assert.Equal(1, Content.Items.Get(Driver).Draw);
        Assert.Equal(Driver, w.M.Items.Running(0));
        Assert.Equal(Charge.Battery - 10 * Charge.Scale, w.M.Items.Inventory(0)[Belt]!.Charge);
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void IsSoldByTheMafiaInTheSecondOrdersRingAsAnItemAndNeverCrafted()
    {
        var w = new Scenario(Layout.Parse(Room) with { Known = Tree.StartKnown }, doubled: true);
        Assert.False(Crafts.TryGet(Driver, out _));
        Assert.Equal("order2", Tree.Node(Driver)!.Needs.Single());
        w.M.Knowledge.Earn(Prices.Of(Tree.Node(Driver)!));
        Mafia.Reach(w, 2);
        w.Do(new BuyBlock(Driver)).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Knowledge.InFlight(Driver));
        Mafia.Receive(w);
        Assert.Contains(w.M.Items.Inventory(0), s => s?.Item == Driver);
    }

    [Fact]
    public void ItCostsFourTimesItsWorthToTwoFigures()
    {
        int worth = Market.WorthOf(Driver), price = Prices.Of(Tree.Node(Driver)!);
        Assert.True(worth > 0);
        int unit = 1;
        while (4 * worth / unit >= 100) unit *= 10;
        Assert.Equal((4 * worth + unit - 1) / unit * unit, price);
    }

    [Fact]
    public void EveryBlockOfTheMarketHasAPrice()
    {
        foreach (var node in Tree.Nodes.Where(n => !n.Gate)) Assert.True(Prices.Of(node) > 0, node.Id);
    }

    [Fact]
    public void EveryBuilderWorksAtLeastATickOnAtLeastOneCell()
    {
        foreach (var builder in Screwdrivers.All)
        {
            Assert.True(builder.Work >= 1, builder.Item);
            Assert.True(builder.Area >= 1, builder.Item);
            Assert.Equal(ItemGroup.Tool, Content.Items.Get(builder.Item).Group);
        }
    }
}
