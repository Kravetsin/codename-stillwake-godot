using Stillwake.Content;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Machine traits (0113): what each machine is to the systems that ask, and the grid's lists of them.</summary>
public class MachineTraitsTests
{
    /// <summary>
    /// The machines of each trait: the rules that once named a crate, a tank, a generator, a vent, the bay, a lamp or a fire
    /// read these.
    /// </summary>
    [Theory]
    [InlineData(MachineTrait.Container, new[] { "crate" })]
    [InlineData(MachineTrait.Gas, new[] { "tank" })]
    [InlineData(MachineTrait.AirSource, new[] { "tank", "generator" })]
    [InlineData(MachineTrait.Vents, new[] { "vent" })]
    [InlineData(MachineTrait.Zone, new[] { "droneBay" })]
    [InlineData(MachineTrait.Glow, new[] { "lamp", "arcLamp" })]
    [InlineData(MachineTrait.Fire, new[] { "furnace", "burnerGenerator" })]
    public void EachTraitIsWhatItsMachinesAre(MachineTrait trait, string[] kinds)
    {
        Assert.Equal(kinds, Machines.All.Where(m => m.Is(trait)).Select(m => m.Kind));
        Assert.All(kinds, k => Assert.True(Machines.Is(k, trait)));
        Assert.False(Machines.Is("nothing", trait));
    }

    [Fact]
    public void AFireBurnsWhereAFuelSlotIsAndIsNeverDeclared()
    {
        Assert.All(Machines.All, m => Assert.Equal(m.FuelSlot != null, m.Is(MachineTrait.Fire)));
        Assert.All(Machines.All, m => Assert.False(m.Has.HasFlag(MachineTrait.Fire)));
        // A fire with inputs smelts them; one without makes power.
        Assert.All(Machines.All.Where(m => m.Is(MachineTrait.Fire)), m => Assert.NotEqual(m.Inputs > 0, m.Makes != null));
    }

    [Fact]
    public void AnAirSourceStoresGasOrMakesItOnPower()
    {
        foreach (var m in Machines.All.Where(m => m.Is(MachineTrait.AirSource)))
        {
            Assert.NotEmpty(m.Ports);
            Assert.NotEqual(m.Is(MachineTrait.Gas), m.Draws != null);
        }
        Assert.All(Machines.All.Where(m => m.Is(MachineTrait.Gas)), m => Assert.True(m.Is(MachineTrait.AirSource)));
    }

    /// <summary>Two rooms, door between: a bay, a generator, a tank, a furnace and a burner generator over vents and a crate.</summary>
    private static readonly string Ship = string.Join('\n',
        "########################",
        "#.........D............#",
        "#RR.GG.TT.D...FF.BB....#",
        "#RR.GGVTTVD..VFF.BBKK..#",
        "########################");

    /// <summary>The grid's list of each trait is its machines in id order: as the map drew them, built, taken apart and loaded.</summary>
    [Fact]
    public void TheGridListsEachTraitsMachinesInIdOrder()
    {
        var w = new Scenario(Ship);
        AssertListed(w);
        Assert.Equal(["generator", "tank"], w.M.Grid.MachinesWith(MachineTrait.AirSource).Select(m => m.Kind));
        w.Do(new Place(w.At(20, 1), new Placement("machine", "crate"))).Do(new Place(w.At(1, 1), new Placement("machine", "vent"))).Run();
        Assert.Equal(2, w.M.Grid.MachinesWith(MachineTrait.Container).Count);
        AssertListed(w);
        w.Do(new ClearAt(w.At(7, 2), "machine")).Run();
        Assert.Equal(["generator"], w.M.Grid.MachinesWith(MachineTrait.AirSource).Select(m => m.Kind));
        AssertListed(w);
        w.Reload();
        AssertListed(w);
    }

    private static void AssertListed(Scenario w)
    {
        var grid = w.M.Grid;
        foreach (var trait in Enum.GetValues<MachineTrait>().Where(t => t != MachineTrait.None))
            Assert.Equal(grid.Machines.Where(m => Machines.Is(m.Kind, trait)), grid.MachinesWith(trait));
    }

    [Fact]
    public void MachinesWithTakesOneTrait()
    {
        var grid = new Scenario(Ship).M.Grid;
        Assert.Throws<ArgumentException>(() => grid.MachinesWith(MachineTrait.Gas | MachineTrait.AirSource));
        Assert.Throws<ArgumentException>(() => grid.MachinesWith(MachineTrait.None));
    }

    /// <summary>A vent piped to a generator is fed as one piped to a tank is: both are air sources.</summary>
    [Fact]
    public void AVentPipedToAGeneratorIsFed()
    {
        var w = new Scenario("#########\n#.....GG#\n#.....GG#\n#.......#\n#########");
        w.Do(new Place(w.At(1, 1), new Placement("machine", "vent"))).Run();
        int vent = w.M.Grid.MachineAt(w.At(1, 1))!.Id;
        Assert.False(w.M.Atmosphere.Fed(vent));
        w.GiveItems(0, ("tube", 6)).Pipe(Port(w, w.At(1, 1)), w.At(3, 3)).Run();
        Assert.False(w.M.Atmosphere.Fed(vent));
        w.Pipe(w.At(3, 3), Port(w, w.At(6, 1))).Run();
        Assert.True(w.M.Atmosphere.Fed(vent));
    }

    private static int Port(Scenario w, int cell) => w.M.Grid.PortsOf(w.M.Grid.MachineAt(cell)!).First();
}
