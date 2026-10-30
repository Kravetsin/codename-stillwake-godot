using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The screwdriver held on a machine a magnetic flare broke repairs it (magnetic-flare).</summary>
public class RepairAimTests
{
    private const int Unit = Hero.Unit, Block = HeroTests.Block;

    private const string Hall = """
        #########
        #.L.....#
        #########
        """;

    private static Hero HeroOn(int x, int y) => new(x * Block + Unit, y * Block + Unit + Hero.MiddleUp, 0, 0, false, 1);

    private static (double X, double Y) Centre(int x, int y) => (2 * x + 0.5, 2 * y + 0.5);

    /// <summary>The hall with its lamp broken, through the save, and peer 0 carrying <paramref name="items"/>.</summary>
    private static (TestWorld W, int Lamp) Broken(params (string Item, int Count)[] items)
    {
        var w = new TestWorld(Layout.Parse(Hall).Doubled()).Give(0, items);
        int lamp = w.M.Grid.Machines.First(m => m.Kind == "lamp").Id;
        w.Edit(root => root["slices"]!["power"]!["broken"] = new JsonArray(lamp));
        return (w, lamp);
    }

    [Fact]
    public void HeldOnABrokenMachineTheScrewdriverRepairsIt()
    {
        var (w, lamp) = Broken(("wire", 1), ("scrap", 1));
        var aim = Aiming.AimAt(w.M, 0, HeroOn(3, 1), Centre(2, 1));
        Assert.Equal(new WorkAim(0, null, lamp), aim!.Work);
        Assert.Equal(new Prompt("use.repair", null), Prompts.Of(w.M, aim));
        var uses = Aiming.UsesOf(aim, pressed: false, held: true, 3, -1, pace: 2);
        Assert.Equal(new Command[] { new Repair(lamp), new Repair(lamp) }, uses.Commands);
        // Not held, nothing is sent.
        Assert.Empty(Aiming.UsesOf(aim, pressed: false, held: false, 3, -1).Commands);
    }

    [Fact]
    public void ShortOfItsCostItSaysWhyAndSendsNothing()
    {
        var (w, lamp) = Broken();
        var aim = Aiming.AimAt(w.M, 0, HeroOn(3, 1), Centre(2, 1));
        Assert.Equal(new WorkAim(0, "items.notEnough", lamp), aim!.Work);
        Assert.Equal(new Prompt("use.repair", "items.notEnough"), Prompts.Of(w.M, aim));
        Assert.Empty(Aiming.UsesOf(aim, pressed: false, held: true, 3, -1).Commands);
    }

    [Fact]
    public void AMarkOnABrokenMachineWinsAndAWorkingOneIsNoRepair()
    {
        var (w, lamp) = Broken(("wire", 1), ("scrap", 1));
        w.Step(new Deconstruct(w.M.Grid.MachineById(lamp)!.Cell, "machine"));
        var marked = Aiming.AimAt(w.M, 0, HeroOn(3, 1), Centre(2, 1));
        Assert.Null(marked!.Work!.Repair);
        Assert.Equal("use.takeApart", Prompts.Of(w.M, marked)?.Action);
        var working = new TestWorld(Layout.Parse(Hall).Doubled());
        Assert.Null(Aiming.AimAt(working.M, 0, HeroOn(3, 1), Centre(2, 1))!.Work);
    }
}
