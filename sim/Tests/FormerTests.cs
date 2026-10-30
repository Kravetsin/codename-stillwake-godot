using Stillwake.Sim.Building;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using Crafts = Stillwake.Content.Crafts;
using Machines = Stillwake.Content.Machines;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>The Former (0057): the first powered station, the workbench's parts at full speed while it draws.</summary>
public class FormerTests
{
    /// <summary>
    /// A solar panel out under the stars over a room with a Former and an oxygen generator; the panel is
    /// cabled to the Former, and with <paramref name="short"/> to the generator too, which draws 4 of its 5.
    /// </summary>
    private static Scenario World(bool cabled = true, bool @short = false)
    {
        string[] rows = ["PPP         ", "            ", "############", "#MM.GG.....#", "#MM.GG.....#", "############"];
        IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>? cables = !cabled ? null
            : @short ? [((0, 0), (1, 3)), ((0, 0), (4, 3))]
            : [((0, 0), (1, 3))];
        return new Scenario(Layout.Parse(rows, null, cables), doubled: true).GiveItems(0, ("iron", 40));
    }

    private static int Former(Scenario w) => w.M.Grid.MachinesOf("former").Single().Id;
    private static int Demand(Scenario w) => w.M.Power.NetworkOf(0)!.Demand;
    private static int Made(Scenario w, string item) => w.M.Items.Crate(Former(w)).Sum(s => s?.Item == item ? s.Count : 0);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static readonly int Plate = Crafts.Get("plate").Ticks * 1000;
    private static readonly int Draws = Machines.Get("former").Draws!.Value;

    [Fact]
    public void MakesAPlateAtFullSpeedInItsTicksDrawingWhileItWorks()
    {
        var w = World();
        int former = Former(w);
        Assert.Equal(1000, Content.Stations.Get("former").Speed);
        // The tick of the craft is the first of its work, at the share the network gave it resting.
        w.Do(new Craft(former, "plate", 2)).Run();
        Assert.Equal(1000, w.M.Crafting.Progress(former));
        Assert.True(w.M.Crafting.Works(former));
        w.Run();
        Assert.Equal(Draws, Demand(w));
        w.Run(Plate / 1000 - 3);
        Assert.Equal(0, Made(w, "plate"));
        w.Run();
        Assert.Equal(1, Made(w, "plate"));
        // Five times the workbench's speed.
        Assert.Equal(Plate / 1000 * 5, Plate / Content.Stations.Get("workbench").Speed);
        w.Run(Plate / 1000);
        Assert.Equal(2, Made(w, "plate"));
    }

    [Fact]
    public void MakesAPlateAtAboutSevenTenthsOnAShortNetwork()
    {
        var w = World(@short: true).Run();
        int former = Former(w);
        w.Do(new Craft(former, "plate", 1)).Run();
        // Resting until now, it drew nothing on the tick it began: the panel's 5 covered the generator's 4.
        Assert.Equal(1000, w.M.Crafting.Progress(former));
        w.Run();
        int share = w.M.Power.Satisfaction(former);
        Assert.Equal(5000 / 7, share);
        Assert.Equal(1000 + share, w.M.Crafting.Progress(former));
        int ticks = (Plate - 1000 + share - 1) / share;
        w.Run(ticks - 2);
        Assert.Equal(0, Made(w, "plate"));
        w.Run();
        Assert.Equal(1, Made(w, "plate"));
    }

    [Fact]
    public void MakesNothingWithNoCable()
    {
        var w = World(cabled: false);
        int former = Former(w);
        w.Do(new Craft(former, "plate", 1)).Run(100);
        Assert.Empty(w.Refusals);
        Assert.Equal(0, w.M.Crafting.Progress(former));
        Assert.False(w.M.Crafting.Works(former));
    }

    [Fact]
    public void RestsDrawingNothingWhileIdleOrWhileItsUnitWaitsForRoom()
    {
        var w = World();
        int former = Former(w);
        w.Run(2);
        Assert.Equal(0, Demand(w));
        w.Do(new Craft(former, "gear", 1)).Run(2);
        Assert.Equal(Draws, Demand(w));
        w.Run(Crafts.Get("gear").Ticks);
        Assert.Equal(1, Made(w, "gear"));
        Assert.Equal(0, Demand(w));

        // Its outputs full of what nothing makes: a plate done waits there, the Former resting.
        // Past its inputs, which a paid craft leaves alone: filled too, for nothing is free to take.
        int output = Machines.Get("former").Inputs!.Value;
        w.Do(new Move(new Slot(output, former), new Target(), 1)).Run();
        w.FillCrate(former, ("canister", Machines.Get("former").Slots!.Value * Content.Items.Get("canister").Stack));
        w.Do(new Craft(former, "plate", 1)).Run(Plate / 1000 + 2);
        Assert.Equal(Plate, w.M.Crafting.Progress(former));
        Assert.False(w.M.Crafting.Works(former));
        Assert.Equal(0, Demand(w));
        w.Do(new Move(new Slot(output, former), new Target(), 999)).Run();
        Assert.Equal(1, Made(w, "plate"));
    }

    [Fact]
    public void RefusesACraftNotInItsRowsAndOnePastItsEightWhoeverSendsIt()
    {
        var w = World().GiveItems(1, ("iron", 40), ("planks", 2));
        int former = Former(w);
        w.Do(new Craft(former, "fireAxe", 1) { From = 1 }).Run();
        Assert.Equal(["crafting.notHere"], Reasons(w));
        Assert.Equal(8, Content.Stations.Get("former").QueueMost);
        for (int i = 0; i < 8; i++) w.Do(new Craft(former, "gear", 1) { From = i % 2 });
        w.Do(new Craft(former, "gear", 1) { From = 1 }).Run();
        Assert.Equal(["crafting.notHere", "crafting.full"], Reasons(w));
        Assert.Equal(8, w.M.Crafting.Queue(former).Count);
    }

    [Fact]
    public void IsNotTakenApartWithCraftsQueuedMarkedOrWorked()
    {
        var w = World(cabled: false);
        int former = Former(w);
        int cell = w.M.Grid.MachineById(former)!.Cell;
        w.Do(new Craft(former, "gear", 1)).Run();
        w.Do(new Deconstruct(cell, "machine")).Run();
        Assert.Equal(["building.notEmpty"], Reasons(w));

        // Marked empty, then a craft queued: its work is refused until the queue is cancelled.
        w.Do(new Cancel(former, 1)).Run();
        w.Do(new Deconstruct(cell, "machine")).Run();
        int mark = w.M.Building.Plans.Single().Id;
        w.Do(new Craft(former, "gear", 1)).Do(new Work(mark)).Run();
        Assert.Equal(["building.notEmpty", "building.notEmpty"], Reasons(w));
        w.Do(new Cancel(former, 2)).Run();
        for (int i = 0; i < 200 && w.M.Grid.MachineById(former) != null; i++) w.Do(new Work(mark)).Run();
        Assert.Null(w.M.Grid.MachineById(former));
        Assert.Equal(2, Reasons(w).Length);
    }

    [Fact]
    public void IsBoughtInTheTree()
    {
        var w = new Scenario(Layout.Parse(["####", "#..#", "####"]) with { Known = Content.Research.StartKnown });
        var former = w.M.Research.Recipes().Single(r => r.Id == "former");
        Assert.False(former.Known);
    }
}
