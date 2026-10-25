using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The right button's layer (0107): a weapon's fan strikes the natural back walls of the open cells its rays cross, at their
/// material's strength, never a ship's (0108), and a wall stops it; and the screwdriver every hero comes aboard with, which
/// builds what the menu plans.
/// </summary>
public class BackDigTests
{
    /// <summary>Hull at 0, soil at 1 to 3, stone at 4 to 6, bedrock at 7; over them a row backed by the hull and a top row
    /// backed by soil at 1 to 4, stone at 5, bedrock at 6.</summary>
    private const string Map = """
        ........
        ........
        ########
        ########
        """;

    private const string Ground = """
        .sssstb.
        ........
        .ssstttb
        .ssstttb
        """;

    /// <summary>A sealed room of air, its back walls the hull's scrap.</summary>
    private const string Room = """
        #####
        #...#
        #####
        """;

    private static readonly string Axe = Weapons.Axe.Item, Drill = Weapons.Drill.Item;
    private const int Right = 0, Down = 64;
    private static Scenario World() => new(Sim.Layout.Parse(Map).WithMaterials(Ground));
    private static int At(int x, int y) => y * 8 + x;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Count(Scenario w, string item, int peer = 0) => w.M.Items.Totals(peer).Where(t => t.Item == item).Sum(t => t.Count);
    private static Crack? CrackAt(Scenario w, int cell, bool back) => w.M.Building.Cracks.FirstOrDefault(c => c.Cell == cell && c.Back == back);

    [Fact]
    public void TheAxeStrikesTheSoilBackWallsOfTheOpenCellsItsRaysCrossBreakingThemInTwoHitsTheSoilGoingToTheDigger()
    {
        var w = World();
        // From the top row's (2, 0), aimed right: the row's back walls to the bedrock, the hull's in the row below passed by.
        Assert.Equal([At(2, 0), At(3, 0), At(4, 0), At(5, 0)], w.M.Building.Struck(Axe, 2500, 500, Right, back: true).Cells);
        w.Do(HewTesting.Dig(w, At(2, 0), Axe, Back: true, heading: Right)).Run();
        Assert.Equal(Weapons.Axe.Damage, CrackAt(w, At(3, 0), back: true)?.Damage);
        Assert.Equal(Weapons.Axe.Damage, CrackAt(w, At(5, 0), back: true)?.Damage);
        w.Do(HewTesting.Dig(w, At(2, 0), Axe, Back: true, heading: Right)).Run();
        Assert.Empty(w.Refusals);
        Assert.All([At(2, 0), At(3, 0), At(4, 0)], c => Assert.Null(w.M.Grid.BackAt(c)));
        Assert.Equal("stone", w.M.Grid.BackAt(At(5, 0))?.Id);
        Assert.Equal("bedrock", w.M.Grid.BackAt(At(6, 0))?.Id);
        Assert.Equal("scrap", w.M.Grid.BackAt(At(3, 1))?.Id);
        Assert.Equal(3, Count(w, "soil"));
    }

    [Fact]
    public void AWallStopsTheBackLayersRaysSoNoBackWallBehindAWallIsStruck()
    {
        var w = World();
        // Down from the hull-backed row: the walls below stop every ray, their soil behind them untouched.
        w.Do(HewTesting.Dig(w, At(2, 1), Axe, Back: true, heading: Down)).Run();
        Assert.Equal(["building.nothingToDig"], Reasons(w));
        Assert.Empty(w.M.Building.Cracks);
        Assert.Equal("soil", w.M.Grid.BackAt(At(2, 2))?.Id);
    }

    [Fact]
    public void AWallAndABackWallKeepCracksOfTheirOwnInCellOrderThroughASave()
    {
        var w = World();
        w.Do(HewTesting.Dig(w, At(4, 2), Axe)).Do(HewTesting.Dig(w, At(4, 0), Axe, Back: true, heading: Right)).Run();
        Assert.Equal([(At(4, 0), true), (At(5, 0), true), (At(4, 2), false)], w.M.Building.Cracks.Select(c => (c.Cell, c.Back)));
        w.Reload();
        Assert.Equal(3, w.M.Building.Cracks.Count);
        w.Run(Builds.CrackHeals);
        Assert.Empty(w.M.Building.Cracks);
    }

    [Fact]
    public void NeitherTheAxeNorTheDrillBreaksAShipsWallOrBackWallTheRoomStayingSealed()
    {
        var w = new Scenario(Room).GiveItems(0, (Drill, 1));
        int cell = w.M.Grid.Width * (w.M.Grid.Height / 2) + w.M.Grid.Width / 2;
        foreach (var tool in (string[])[Axe, Drill])
            for (int i = 0; i < 4; i++) w.Do(HewTesting.Dig(w, cell, tool, Back: true)).Do(HewTesting.Dig(w, 0, tool)).Run();
        // A ship's walls stop the rays unhurt and its back walls are passed by: the fan strikes nothing.
        Assert.All(Reasons(w), r => Assert.Equal("building.nothingToDig", r));
        Assert.Equal(16, w.Refusals.Count);
        Assert.Equal("scrap", w.M.Grid.BackAt(cell)?.Id);
        Assert.NotNull(w.M.Grid.WallAt(0));
        Assert.Empty(w.M.Building.Cracks);
        Assert.Empty(w.M.Rooms.All.Single().Openings);
    }

    [Fact]
    public void RefusesOnlyBedrockOrAShipsBackWallAsNothingToDigAndAFlatDrillAsTheWeaponSays()
    {
        var w = World();
        w.Do(HewTesting.Dig(w, At(6, 0), Axe, Back: true, heading: Right)).Do(HewTesting.Dig(w, -1, Axe, Back: true)).Run();
        Assert.Equal(["building.nothingToDig", "building.nothingToDig"], Reasons(w));
        w.GiveItems(0, (Drill, 1)).Do(HewTesting.Dig(w, At(2, 0), Drill, Back: true, heading: Right)).Run();
        Assert.Equal("items.flat", Reasons(w)[^1]);
        Assert.Equal("bedrock", w.M.Grid.BackAt(At(6, 0))?.Id);
        Assert.Equal("soil", w.M.Grid.BackAt(At(2, 0))?.Id);
    }

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
