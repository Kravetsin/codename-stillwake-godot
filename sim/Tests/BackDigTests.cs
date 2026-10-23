using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The right button's layer (0107): a weapon's hit on a natural back wall at its material's strength, never a ship's (0108); and the
/// screwdriver every hero comes aboard with, which builds what the menu plans.
/// </summary>
public class BackDigTests
{
    /// <summary>Hull at 0, soil at 1 to 3, stone at 4 to 6, bedrock at 7, all back-walled as their walls; open hull-backed rows above.</summary>
    private const string Map = """
        ........
        ........
        ########
        ########
        """;

    private const string Ground = """
        ........
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
    private static Scenario World() => new(Sim.Layout.Parse(Map).WithMaterials(Ground));
    private static int At(int x, int y) => y * 8 + x;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Count(Scenario w, string item, int peer = 0) => w.M.Items.Totals(peer).Where(t => t.Item == item).Sum(t => t.Count);
    private static Crack? CrackAt(Scenario w, int cell, bool back) => w.M.Building.Cracks.FirstOrDefault(c => c.Cell == cell && c.Back == back);

    [Fact]
    public void TheAxeBreaksASoilBackWallBehindItsWallInTwoHitsTheWallStayingAndTheSoilGoingToTheDigger()
    {
        var w = World();
        int cell = At(2, 3);
        w.Do(new Dig(cell, Axe, Back: true)).Run();
        Assert.Equal(Weapons.Axe.Damage, CrackAt(w, cell, back: true)?.Damage);
        Assert.Null(CrackAt(w, cell, back: false));
        w.Do(new Dig(cell, Axe, Back: true)).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.BackAt(cell));
        Assert.Equal("soil", w.M.Grid.WallAt(cell)?.Id);
        Assert.Empty(w.M.Building.Cracks);
        Assert.Equal(1, Count(w, "soil"));
    }

    [Fact]
    public void AWallAndItsBackWallKeepCracksOfTheirOwnTheFrontsFirstAndBothThroughASave()
    {
        var w = World();
        int cell = At(5, 2);
        w.Do(new Dig(cell, Axe, Back: true)).Do(new Dig(cell, Axe)).Do(new Dig(At(4, 2), Axe, Back: true)).Run();
        Assert.Equal([(At(4, 2), true), (cell, false), (cell, true)], w.M.Building.Cracks.Select(c => (c.Cell, c.Back)));
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
            for (int i = 0; i < 4; i++) w.Do(new Dig(cell, tool, Back: true)).Do(new Dig(0, tool)).Run();
        Assert.All(Reasons(w), r => Assert.Equal("building.builtWall", r));
        Assert.Equal(16, w.Refusals.Count);
        Assert.Equal("building.builtWall", w.M.Building.DigProblem(0, cell, Axe, back: true));
        Assert.Equal("scrap", w.M.Grid.BackAt(cell)?.Id);
        Assert.NotNull(w.M.Grid.WallAt(0));
        Assert.Empty(w.M.Building.Cracks);
        Assert.Empty(w.M.Rooms.All.Single().Openings);
    }

    [Fact]
    public void RefusesNoBackWallAsNothingToDigBedrockAsTooHardAndAFlatDrillAsTheWeaponSays()
    {
        var w = World();
        w.Do(new Dig(At(2, 0), Axe, Back: true)).Do(new Dig(-1, Axe, Back: true)).Do(new Dig(At(7, 2), Axe, Back: true)).Do(new Dig(At(2, 0), Axe)).Run();
        Assert.Equal(["building.builtWall", "building.nothingToDig", "building.bedrock", "building.nothingToDig"], Reasons(w));
        Assert.Equal("building.bedrock", w.M.Building.DigProblem(0, At(7, 2), Axe, back: true));
        w.GiveItems(0, (Drill, 1)).Do(new Dig(At(2, 2), Drill, Back: true)).Run();
        Assert.Equal("items.flat", Reasons(w)[^1]);
        Assert.Equal("bedrock", w.M.Grid.BackAt(At(7, 2))?.Id);
        Assert.Equal("soil", w.M.Grid.BackAt(At(2, 2))?.Id);
    }

    [Fact]
    public void AHeroComesAboardWithTheScrewdriverAndTheStripperOnceAndItsPlayerBuildsAPlanWithIt()
    {
        var w = new Scenario(Room);
        int cell = w.M.Grid.Width * (w.M.Grid.Height / 2) + w.M.Grid.Width / 2;
        w.Do(new Enter(cell)).Run();
        Assert.Equal(new Stack(Screwdrivers.Basic.Item, 1), w.M.Items.Inventory(0)[0]);
        Assert.Equal(new Stack(HandTools.Stripper, 1), w.M.Items.Inventory(0)[1]);
        w.Do(new Leave()).Run().Do(new Enter(cell)).Run();
        Assert.Equal(1, Count(w, Screwdrivers.Basic.Item));
        Assert.Equal(1, Count(w, HandTools.Stripper));

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
