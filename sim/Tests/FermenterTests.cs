using Stillwake.Sim.Items;
using Xunit;
using Machines = Stillwake.Content.Machines;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The fermenter (docs/plans/power-chain.md): a fed station of the "ferment" process, on power, turning biomass and fat into
/// biofuel one for one; the only way to biofuel, the furnace and the smelter leaving both unworked.
/// </summary>
public class FermenterTests
{
    /// <summary>A solar panel under the stars over a room holding a fermenter cabled to it, and peer 0 carrying what it ferments.</summary>
    private static Scenario World()
    {
        string[] rows = ["PPP         ", "            ", "############", "#uu........#", "#uu........#", "############"];
        return new Scenario(Layout.Parse(rows, null, [((0, 0), (1, 3))]), doubled: true)
            .GiveItems(0, ("biomass", 4), ("fat", 2), ("seed", 2), ("scrapChunk", 2)).Run(2);
    }

    /// <summary>A sealed aired room holding a lit furnace and an electric smelter on a panel of its own.</summary>
    private static Scenario Smelting()
    {
        var w = new Scenario(AirTesting.Vaporized(
                ["PPP       ", "          ", "##########", "#FF.TT.EE#", "#FF.TTVEE#", "##########"],
                ["", "", "", "    o", "    ---"], [((0, 0), (7, 3))]), doubled: true)
            .GiveItems(0, ("biomass", 4), ("fat", 4), ("planks", 2));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.At(1, 3)); i++) w.Run();
        return w;
    }

    private static int Fermenter(Scenario w) => w.M.Grid.MachinesOf("fermenter").Single().Id;
    private static int Demand(Scenario w) => w.M.Power.NetworkOf(0)!.Demand;
    private static int Held(Scenario w, int machine, string item) => w.M.Items.Crate(machine).Sum(s => s?.Item == item ? s.Count : 0);
    private static readonly int Ticks = Content.Items.Get("biomass").SmeltTicks!.Value;
    private static readonly int Draws = Machines.Get("fermenter").Draws!.Value;

    /// <summary>Moves <paramref name="count"/> of what peer 0 carries of <paramref name="item"/> into slot <paramref name="slot"/> of <paramref name="machine"/>, and runs the tick.</summary>
    private static Scenario Put(Scenario w, int machine, string item, int count = 1, int slot = 0)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
        return w.Do(new Move(new Slot(from), new Target(slot, machine), count)).Run();
    }

    [Fact]
    public void FermentsBiomassIntoBiofuelOneForOneInItsSmeltTicksAtFullPowerDrawingWhileItWorks()
    {
        var w = World();
        int fermenter = Fermenter(w);
        Assert.Equal(("ferment", 1000, 1, 2), (Content.Stations.Get("fermenter").Process, Content.Stations.Get("fermenter").Speed, Content.Stations.Get("fermenter").Heat, Draws));
        Assert.Equal(("ferment", 160, 1), (Content.Items.Get("biomass").Process, Ticks, Content.Items.Get("biomass").Heat));
        Assert.Equal([("biofuel", 1)], Content.Items.Get("biomass").Yields);
        // Idle it rests, drawing nothing.
        Assert.Equal(0, Demand(w));
        Put(w, fermenter, "biomass", 2);
        Assert.True(w.M.Crafting.Works(fermenter));
        w.Run();
        Assert.Equal(Draws, Demand(w));
        // The tick it is put in is the first of its work.
        w.Run(Ticks - 3);
        Assert.Equal(0, Held(w, fermenter, "biofuel"));
        w.Run();
        Assert.Equal((1, 1), (Held(w, fermenter, "biomass"), Held(w, fermenter, "biofuel")));
        w.Run(Ticks);
        Assert.Equal((0, 2), (Held(w, fermenter, "biomass"), Held(w, fermenter, "biofuel")));
        // Done, it rests again.
        w.Run(2);
        Assert.Equal(0, Demand(w));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void FermentsFatAsBiomassIs()
    {
        var w = World();
        int fermenter = Fermenter(w);
        Assert.Equal(("ferment", Ticks), (Content.Items.Get("fat").Process, Content.Items.Get("fat").SmeltTicks));
        Put(w, fermenter, "fat", 2).Run(2 * Ticks);
        Assert.Equal((0, 2), (Held(w, fermenter, "fat"), Held(w, fermenter, "biofuel")));
    }

    /// <summary>What is not of its process is taken in and sits unworked, the fermenter resting: a seed, a scrap chunk.</summary>
    [Theory]
    [InlineData("seed")]
    [InlineData("scrapChunk")]
    public void LeavesWhatDoesNotFermentUnworkedAndRests(string item)
    {
        var w = World();
        int fermenter = Fermenter(w);
        Put(w, fermenter, item).Run(400);
        Assert.Empty(w.Refusals);
        Assert.Equal(1, Held(w, fermenter, item));
        Assert.Null(w.M.Crafting.WorkOf(fermenter));
        Assert.False(w.M.Crafting.Works(fermenter));
        Assert.Equal(0, Demand(w));
    }

    /// <summary>The furnace and the smelter make biofuel no more: biomass and fat sit in their inputs, the furnace never lit for them.</summary>
    [Fact]
    public void TheFurnaceAndTheSmelterLeaveBiomassAndFatUnworked()
    {
        var w = Smelting();
        int furnace = w.M.Grid.MachinesOf("furnace").Single().Id, smelter = w.M.Grid.MachinesOf("smelter").Single().Id;
        Put(w, furnace, "planks", 1, Machines.Get("furnace").FuelSlot!.Value);
        Put(w, furnace, "biomass", 2);
        Put(w, smelter, "fat", 2);
        w.Run(4 * Ticks);
        Assert.Equal((2, 0), (Held(w, furnace, "biomass"), Held(w, furnace, "biofuel")));
        Assert.Equal((2, 0), (Held(w, smelter, "fat"), Held(w, smelter, "biofuel")));
        Assert.False(w.M.Furnace.At(furnace).Burning);
        Assert.Equal(1, Held(w, furnace, "planks"));
        Assert.Empty(w.Refusals);
    }
}
