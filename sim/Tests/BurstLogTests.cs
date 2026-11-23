using Stillwake.Sim.Building;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Building's log of bursts (debris-bursts): every burst asked for, whoever set it off, with a running serial, power and push reach; the last
/// <see cref="BuildingModule.BurstsKept"/> kept; derived, so empty after a load. What the host player's bodies are pushed by.
/// </summary>
public class BurstLogTests
{
    private static RecentBurst[] Log(Scenario w) => [.. w.M.Building.Bursts];

    [Fact]
    public void ABurstIsLoggedWithItsSerialCellPowerAndReachWhetherItWentOffOrNot()
    {
        var w = new Scenario("..........\n##########", cells: true);
        Assert.Empty(Log(w));
        Assert.True(w.M.Building.Burst(FloorStack.Nobody, 3, 500, 8, 30, Spoils.Drop));
        Assert.Equal([new RecentBurst(1, 3, 500, 30)], Log(w));
        // One of no push is logged too, pushing nothing; off the grid there is nothing to log.
        w.M.Building.Burst(FloorStack.Nobody, 4, 300, 0, 0, Spoils.Lost);
        Assert.False(w.M.Building.Burst(FloorStack.Nobody, -1, 300, 6, 30, Spoils.Drop));
        Assert.Equal([new RecentBurst(1, 3, 500, 30), new RecentBurst(2, 4, 300, 0)], Log(w));
    }

    [Fact]
    public void OnlyTheLastAreKeptTheirSerialsRunningOn()
    {
        var w = new Scenario("..........\n##########", cells: true);
        int many = BuildingModule.BurstsKept + 8;
        for (int i = 0; i < many; i++) w.M.Building.Burst(FloorStack.Nobody, i % 10, 100 + i, 2, 6, Spoils.Drop);
        var log = Log(w);
        Assert.Equal(BuildingModule.BurstsKept, log.Length);
        Assert.Equal(Enumerable.Range(9, BuildingModule.BurstsKept), log.Select(b => b.Serial));
        Assert.Equal(new RecentBurst(many, (many - 1) % 10, 100 + many - 1, 6), log[^1]);
    }

    [Fact]
    public void AfterALoadTheLogIsEmptyAndItsSerialsBeginAgain()
    {
        var w = new Scenario("..........\n##########", cells: true);
        for (int i = 0; i < 5; i++) w.M.Building.Burst(FloorStack.Nobody, 3, 500, 8, 30, Spoils.Drop);
        w.Run().Reload();
        Assert.Empty(Log(w));
        w.M.Building.Burst(FloorStack.Nobody, 3, 500, 8, 30, Spoils.Drop);
        Assert.Equal(1, Log(w).Single().Serial);
    }

    [Fact]
    public void AThrownChargesBurstIsLoggedByItsBlast()
    {
        string charge = Spec.Shots.Thrown.Item;
        var w = new Scenario(".....#....\n..........\n##########", cells: true).Do(new Crew.Enter(12)).GiveItems(0, (charge, 1)).Wields(charge).Run();
        w.Do(new Fire(charge, CreaturesModule.Unit + CreaturesModule.Unit / 2, CreaturesModule.Unit / 2, 0));
        for (int i = 0; i < 200 && w.M.Creatures.Ended.Count == 0; i++) w.Run();
        var end = w.M.Creatures.Ended.Single();
        var blast = Spec.Blasting.Charge;
        Assert.Equal([new RecentBurst(1, end.Burst, blast.Power, blast.Push)], Log(w));
    }

    [Fact]
    public void AMeteorsBurstIsLoggedByItsShower()
    {
        var w = new Scenario(Layout.Parse("....DD..#\n....DD..#")).Do(new MeteorHit(17, 5)).Run();
        Assert.Empty(w.Refusals);
        var shower = Spec.Meteors.StartingSpace;
        Assert.Equal([new RecentBurst(1, 17, HazardsModule.Charge(5, shower), shower.Reach * Spec.Blasting.PushPerReach)], Log(w));
    }
}
