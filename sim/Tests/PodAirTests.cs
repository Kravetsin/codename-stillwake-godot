using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Xunit;
using Air = Stillwake.Content.Air;
using FurnaceContent = Stillwake.Content.Furnace;
using Machines = Stillwake.Content.Machines;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>The lifepod as a first generator (2026-10-31-the-scoop): on its pipe it makes a room's air with no power.</summary>
public class PodAirTests
{
    private const int Width = 20;
    private static int Cell(int x, int y) => y * Width + x;
    private static readonly int Port = Cell(1, 6), FreeEnd = Cell(2, 6), Door = Cell(17, 3);

    private static readonly string[] OneRoom =
    [
        "####################",
        "#OOOOOO.........####",
        "#OOOOOO.........####",
        "#OOOOOO.........DD  ",
        "#OOOOOO.........DD  ",
        "#OOOOOO.........DD  ",
        "#OOOOOO.........DD  ",
        "####################",
    ];

    private static readonly string[] Piped = ["", "", "", "", "", "", " --"];

    private static Scenario Room(bool piped = true) => new(Layout.Parse(OneRoom, piped ? Piped : null));

    private static int Aired(Scenario w) => w.M.Atmosphere.AiredCells.Count;

    private static int Pod(Scenario w) => w.M.Grid.MachinesOf("lifepod").Single().Id;

    private static Scenario Filled(Scenario w) => w.Run(600);

    [Fact]
    public void FillsItsRoomWithNoPowerAndNoGeneratorOnItsOwnPipe()
    {
        var w = Filled(Room());
        Assert.Empty(w.M.Power.Networks);
        Assert.Equal(15 * 6, Aired(w));
        Assert.Equal(1000, w.M.Atmosphere.MadeShare(Pod(w)));
    }

    [Fact]
    public void WithNoPipeAirsNothingAndStoresNothing()
    {
        var w = Room(piped: false).Run(600);
        Assert.Equal(0, Aired(w));
        Assert.Equal(0, w.M.Atmosphere.OxygenIn(Pod(w)));
    }

    [Fact]
    public void MakesAUnitATickUpToItsStore()
    {
        // Its room walled off from it would be the test; here its first ticks, before the vent draws faster than it makes.
        Assert.Equal(1, Air.GeneratorRate / Air.LeakEvery);
        var w = new Scenario(Layout.Parse(
            ["##########", "#OOOOOO###", "#OOOOOO###", "#OOOOOO###", "#OOOOOO###", "#OOOOOO###", "#OOOOOO###", "##########"], Piped));
        // Nothing to fill but the pod's own cells: the store reaches its top and stays.
        w.Run(2 * Air.GeneratorStore + 200);
        Assert.Equal(Air.GeneratorStore, w.M.Atmosphere.OxygenIn(Pod(w)));
    }

    [Fact]
    public void CountsOnceInItsRoomsAir()
    {
        var w = Filled(Room());
        var air = w.M.Atmosphere.RoomsAir().Single();
        Assert.Equal(Air.GeneratorRate, air.Made);
    }

    [Fact]
    public void VentsThroughAnOpenDoorAndRefillsOnceItIsShut()
    {
        var w = Filled(Room());
        w.Do(new SetDoor(Door, true)).Run(20);
        Assert.Equal(0, Aired(w));
        w.Do(new SetDoor(Door, false)).Run();
        Filled(w);
        Assert.Equal(15 * 6, Aired(w));
    }

    [Fact]
    public void LightsAFurnaceInItsRoom()
    {
        string[] rows = [.. OneRoom];
        for (int y = 3; y <= 6; y++) rows[y] = rows[y][..9] + "FFFF" + rows[y][13..];
        var w = Filled(new Scenario(Layout.Parse(rows, Piped)));
        int furnace = w.M.Grid.MachinesOf("furnace").Single().Id;
        w.GiveItems(0, ("planks", 1), ("scrapChunk", 1));
        var bag = w.M.Items.Inventory(0).ToList();
        w.Do(new Move(new Slot(bag.FindIndex(s => s?.Item == "planks")), new Target(Machines.Get("furnace").FuelSlot, furnace), 1)).Run();
        w.Do(new Move(new Slot(bag.FindIndex(s => s?.Item == "scrapChunk")), new Target(Crate: furnace), 1)).Run();
        Assert.True(w.M.Furnace.At(furnace).Burning);
    }

    [Fact]
    public void FeedsAVentPipedFromItsPortInASecondRoom()
    {
        string[] rows =
        [
            "####################",
            "#OOOOOO..#.........#",
            "#OOOOOO..#.........#",
            "#OOOOOO..#.........#",
            "#OOOOOO..#.........#",
            "#OOOOOO..#..VV.....#",
            "#OOOOOO..#..VV.....#",
            "####################",
        ];
        string[] under = ["", "", "", "", "", "            -", " ------------"];
        var w = new Scenario(Layout.Parse(rows, under)).Run(1200);
        // Both rooms, every cell.
        Assert.Equal(8 * 6 + 9 * 6, Aired(w));
    }

    [Fact]
    public void ItsOwnPipeCannotBeCutThoughAPipeFromItCan()
    {
        var w = Room();
        w.Do(new CutPipe(Port, FreeEnd)).Run();
        Assert.Equal("pipes.fixed", w.Refusals.Single().Reason);
        Assert.Single(w.M.Pipes.Spans);

        string[] rows = [.. OneRoom];
        var far = new Scenario(Layout.Parse(rows, ["", "", "", "", "", "", " -----------"]));
        var span = far.M.Pipes.Spans.Single();
        far.Do(new CutPipe(span.A, span.B)).Run();
        Assert.Empty(far.Refusals);
        Assert.Empty(far.M.Pipes.Spans);
    }
}
