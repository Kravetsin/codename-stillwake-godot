using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using Crafts = Stillwake.Content.Crafts;
using Machines = Stillwake.Content.Machines;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>The circuit assembler (0057): a queued station on power making the electronics, circuits and solar cells.</summary>
public class AssemblerTests
{
    /// <summary>
    /// A solar panel out under the stars over a room with a circuit assembler and an oxygen generator; the panel is
    /// cabled to the assembler, and with <paramref name="short"/> to the generator too, which draws 4 of its 5.
    /// </summary>
    private static Scenario World(bool @short = false)
    {
        string[] rows = ["PPP         ", "            ", "############", "#YY.GG.....#", "#YY.GG.....#", "############"];
        IReadOnlyList<((int X, int Y) A, (int X, int Y) B)> cables = @short ? [((0, 0), (1, 3)), ((0, 0), (4, 3))] : [((0, 0), (1, 3))];
        return new Scenario(Layout.Parse(rows, null, cables)).GiveItems(0, ("silicon", 10), ("wire", 20), ("glass", 4));
    }

    private static int Assembler(Scenario w) => w.M.Grid.MachinesOf("assembler").Single().Id;
    private static int Demand(Scenario w) => w.M.Power.NetworkOf(0)!.Demand;
    private static int Made(Scenario w, string item) => w.M.Items.Crate(Assembler(w)).Sum(s => s?.Item == item ? s.Count : 0);
    private static int Carried(Scenario w, string item) => w.M.Items.Inventory(0).Sum(s => s?.Item == item ? s.Count : 0);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static readonly int Circuit = Crafts.Get("circuit").Ticks * 1000;
    private static readonly int Draws = Machines.Get("assembler").Draws!.Value;

    [Fact]
    public void MakesACircuitOfSiliconAndWireInEightyTicksAtFullPowerDrawingWhileItWorks()
    {
        var w = World();
        int assembler = Assembler(w);
        Assert.Equal((1000, 8, 2), (Content.Stations.Get("assembler").Speed, Content.Stations.Get("assembler").QueueMost, Draws));
        Assert.Equal(80, Crafts.Get("circuit").Ticks);
        // Paid when queued; the tick of the craft is the first of its work, at the share the network gave it resting.
        w.Do(new Craft(assembler, "circuit", 2)).Run();
        Assert.Equal((8, 16), (Carried(w, "silicon"), Carried(w, "wire")));
        Assert.Equal(1000, w.M.Crafting.Progress(assembler));
        Assert.True(w.M.Crafting.Works(assembler));
        w.Run();
        Assert.Equal(Draws, Demand(w));
        w.Run(Circuit / 1000 - 3);
        Assert.Equal(0, Made(w, "circuit"));
        w.Run();
        Assert.Equal(1, Made(w, "circuit"));
        w.Run(Circuit / 1000);
        Assert.Equal(2, Made(w, "circuit"));
    }

    [Fact]
    public void MakesACircuitSlowerOnAShortNetwork()
    {
        var w = World(@short: true).Run();
        int assembler = Assembler(w);
        w.Do(new Craft(assembler, "circuit", 1)).Run();
        // Resting until now, it drew nothing on the tick it began: the panel's 5 covered the generator's 4.
        Assert.Equal(1000, w.M.Crafting.Progress(assembler));
        w.Run();
        int share = w.M.Power.Satisfaction(assembler);
        Assert.Equal(5000 / 6, share);
        Assert.Equal(1000 + share, w.M.Crafting.Progress(assembler));
        int ticks = (Circuit - 1000 + share - 1) / share;
        Assert.True(ticks > Circuit / 1000, $"{ticks} ticks");
        w.Run(ticks - 2);
        Assert.Equal(0, Made(w, "circuit"));
        w.Run();
        Assert.Equal(1, Made(w, "circuit"));
    }

    [Fact]
    public void MakesASolarCellOfSiliconGlassAndWireInAHundredTicks()
    {
        var w = World();
        int assembler = Assembler(w);
        Assert.Equal([("silicon", 1), ("glass", 1), ("wire", 1)], Crafts.Get("solarCell").Cost);
        w.Do(new Craft(assembler, "solarCell", 1)).Run();
        Assert.Equal((9, 3, 19), (Carried(w, "silicon"), Carried(w, "glass"), Carried(w, "wire")));
        w.Run(Crafts.Get("solarCell").Ticks - 2);
        Assert.Equal(0, Made(w, "solarCell"));
        w.Run();
        Assert.Equal(1, Made(w, "solarCell"));
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void RestsDrawingNothingWhileIdleOrWhileItsUnitWaitsForRoom()
    {
        var w = World();
        int assembler = Assembler(w);
        w.Run(2);
        Assert.Equal(0, Demand(w));
        w.Do(new Craft(assembler, "circuit", 1)).Run(2);
        Assert.Equal(Draws, Demand(w));
        w.Run(Circuit / 1000);
        Assert.Equal(1, Made(w, "circuit"));
        Assert.Equal(0, Demand(w));
        Assert.False(w.M.Crafting.Works(assembler));

        // Its outputs full of what it does not make: a circuit done waits there, the assembler resting.
        w.Do(new Move(new Slot(0, assembler), new Target(), 1)).Run();
        w.FillCrate(assembler, ("canister", 3 * Content.Items.Get("canister").Stack));
        w.Do(new Craft(assembler, "circuit", 1)).Run(Circuit / 1000 + 2);
        Assert.Equal(Circuit, w.M.Crafting.Progress(assembler));
        Assert.False(w.M.Crafting.Works(assembler));
        Assert.Equal(0, Demand(w));
        w.Do(new Move(new Slot(0, assembler), new Target(), 999)).Run();
        Assert.Equal(1, Made(w, "circuit"));
    }

    [Fact]
    public void MakesOnlyElectronicsAndTheFormerNoneOfThemWhoeverSendsIt()
    {
        var w = new Scenario(Layout.Parse(["########", "#YY.MM.#", "#YY.MM.#", "########"]))
            .GiveItems(1, ("iron", 4), ("silicon", 2), ("wire", 4));
        int assembler = Assembler(w), former = w.M.Grid.MachinesOf("former").Single().Id;
        w.Do(new Craft(assembler, "plate", 1) { From = 1 }).Run();
        w.Do(new Craft(former, "circuit", 1) { From = 1 }).Run();
        w.Do(new Craft(former, "solarCell", 1) { From = 1 }).Run();
        Assert.Equal(["crafting.notHere", "crafting.notHere", "crafting.notHere"], Reasons(w));
        // Nothing taken for a refused craft; the right one at the right station is.
        Assert.Equal(4, w.M.Items.Inventory(1).Sum(s => s?.Item == "iron" ? s.Count : 0));
        w.Do(new Craft(assembler, "circuit", 1) { From = 1 }).Run();
        Assert.Equal(3, w.Refusals.Count);
        Assert.Single(w.M.Crafting.Queue(assembler));
        for (int i = 0; i < 8; i++) w.Do(new SetCreative(true)).Do(new Craft(assembler, "solarCell", 1) { From = i % 2 });
        w.Run();
        Assert.Equal("crafting.full", w.Refusals[^1].Reason);
        Assert.Equal(8, w.M.Crafting.Queue(assembler).Count);
    }
}
