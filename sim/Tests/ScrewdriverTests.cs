using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The screwdriver every hero comes aboard with, which builds what the menu plans (0107).</summary>
public class ScrewdriverTests
{
    /// <summary>A sealed room of air, its back walls the hull's scrap.</summary>
    private const string Room = """
        #####
        #...#
        #####
        """;

    private static int Count(Scenario w, string item, int peer = 0) => w.M.Items.Totals(peer).Where(t => t.Item == item).Sum(t => t.Count);

    [Fact]
    public void AHeroComesAboardWithTheScrewdriverAloneOnceAndItsPlayerBuildsAPlanWithIt()
    {
        var w = new Scenario(Room);
        int cell = w.M.Grid.Width * (w.M.Grid.Height / 2) + w.M.Grid.Width / 2;
        w.Do(new Enter(cell)).Run();
        Assert.Equal(new Stack(Screwdrivers.Basic.Item, 1), w.M.Items.Inventory(0)[0]);
        Assert.Null(w.M.Items.Inventory(0)[1]);
        w.Do(new Leave()).Run().Do(new Enter(cell)).Run();
        Assert.Equal(1, Count(w, Screwdrivers.Basic.Item));

        w.GiveItems(0, ("scrap", 5)).Do(new PlacePlan(cell + 1, Placement.Wall("scrap"))).Run();
        int plan = w.M.Building.Plans.Single().Id;
        int ticks = w.M.Building.TicksFor(plan)!.Value;
        // The screwdriver's pace: its Work ticks of the plan's work a tick held, as the client sends them.
        for (int t = 0; t < ticks; t += Screwdrivers.Basic.Work)
        {
            for (int k = 0; k < Screwdrivers.Basic.Work; k++) w.Do(new Work(plan));
            w.Run();
        }
        w.Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Building.Plans);
        Assert.Equal("scrap", w.M.Grid.WallAt(cell + 1)?.Id);
    }
}
