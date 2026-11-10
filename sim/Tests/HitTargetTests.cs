using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// What a hit at a cell strikes (hit-targets): the one answer every hitter reads, and the edges each hitter keeps of its own:
/// a burst crosses a plant as open air and stops at a grate, a tool's ray passes what does not block it.
/// </summary>
public class HitTargetTests
{
    private const int Right = 0;

    private static Scenario Ground(string map, string materials) => new(Layout.Parse(map).WithMaterials(materials).OnPlanet());

    private static int At(Scenario w, int x, int y) => y * w.M.Grid.Width + x;

    private static Hewn Struck(Scenario w, string tool, int x, int y, int heading) =>
        w.M.Building.Struck(tool, x * 1000 + 500, y * 1000 + 500, heading);

    private static HitTarget? TargetAt(Scenario w, int x, int y) => w.M.Building.TargetAt(At(w, x, y));

    [Fact]
    public void AWallIsStruckAtItsOwnCellAtItsMaterialsStrengthNothingBreakingBedrock()
    {
        var w = Ground(".##", ".sb");
        Assert.Equal(new HitTarget(TargetKind.Wall, At(w, 1, 0), Materials.Get("soil").Strength, false), TargetAt(w, 1, 0));
        Assert.Equal(new HitTarget(TargetKind.Wall, At(w, 2, 0), null, false), TargetAt(w, 2, 0));
        Assert.Null(TargetAt(w, 0, 0));
        Assert.Null(w.M.Building.TargetAt(-1));
        Assert.Null(w.M.Building.TargetAt(w.M.Grid.Width * w.M.Grid.Height));
    }

    [Fact]
    public void ADoorIsStruckAtItsHeadFromAnyOfItsCellsAtItsBuildsStrength()
    {
        var w = Ground("""
            ....
            .DD.
            .DD.
            ####
            """, """
            ....
            ....
            ....
            ssss
            """);
        var door = new HitTarget(TargetKind.Piece, At(w, 1, 1), Builds.Get("door").Strength, false);
        Assert.All(new[] { (1, 1), (2, 1), (1, 2), (2, 2) }, c => Assert.Equal(door, TargetAt(w, c.Item1, c.Item2)));
    }

    [Fact]
    public void AMachineIsStruckAtItsTopLeftAPileInAWeaponsDamageTheLifepodNever()
    {
        var w = new Scenario(Layout.Parse("""
            ............
            .FFFF.......
            .FFFF.bbb...
            .FFFF.bbb...
            .FFFF.bbb...
            ============
            """));
        var furnace = w.M.Grid.MachinesOf("furnace").Single();
        Assert.Equal(new HitTarget(TargetKind.Machine, furnace.Cell, Damage.MachineHp, false), TargetAt(w, 4, 4));
        var pile = w.M.Grid.MachinesOf("pileScrap").Single();
        Assert.Equal(new HitTarget(TargetKind.Machine, pile.Cell, Machines.Get("pileScrap").Hp, true), TargetAt(w, 8, 3));
        var pod = new Scenario(Registry.Wreck);
        var lifepod = pod.M.Grid.MachinesOf("lifepod").Single();
        Assert.Equal(new HitTarget(TargetKind.Machine, lifepod.Cell, null, false), pod.M.Building.TargetAt(lifepod.Cell + 1));
    }

    [Fact]
    public void APlantIsStruckInItsOpenCellButAWallItStandsOnIsTheWalls()
    {
        var w = new Scenario((Layout.Parse("""
            ........
            ........
            ........
            ........
            ########
            """).WithMaterials("""
            ........
            ........
            ........
            ........
            tttttttt
            """) with { Flora = [(3 * 8 + 3, "bush")] }).OnPlanet());
        var bush = w.M.Building.Plants.Single();
        Assert.Equal(new HitTarget(TargetKind.Plant, bush.Cell, Flora.Get("bush").Strength, false), TargetAt(w, 3, 3));
        Assert.Equal(TargetKind.Wall, TargetAt(w, 3, 4)!.Value.Kind);
    }

    [Fact]
    public void AGrateIsNoTarget() => Assert.Null(TargetAt(Ground(".%.", "..."), 1, 0));

    [Fact]
    public void ABurstCrossesAPlantAsOpenAirNeitherCrackingNorFellingIt()
    {
        var w = new Scenario((Layout.Parse("""
            ........
            ........
            ........
            ........
            ########
            """).WithMaterials("""
            ........
            ........
            ........
            ........
            tttttttt
            """) with { Flora = [(3 * 8 + 3, "bush")] }).OnPlanet());
        var bush = w.M.Building.Plants.Single();
        Assert.True(Flora.Get(bush.Kind).Strength > 0);
        // From beside it, too weak to break the ground under it, which would fell it.
        Assert.True(w.M.Building.Burst(0, At(w, 1, 3), 10, 8, Spoils.Drop));
        Assert.Equal([bush], w.M.Building.Plants);
        Assert.DoesNotContain(w.M.Building.Cracks, c => c.Cell == bush.Cell);
    }

    [Fact]
    public void ABurstStopsAtAGrate()
    {
        var w = Ground(".%#", "..s");
        Assert.True(w.M.Building.Burst(0, 0, 4000, 8, Spoils.Drop));
        Assert.NotNull(w.M.Grid.WallAt(2));
        Assert.Equal(Structure.Grate, w.M.Grid.StructureAt(1));
        Assert.Empty(w.M.Building.Cracks);
    }

    [Fact]
    public void AboardARayStrikesALadderAndPassesItToThePileBehind()
    {
        var w = new Scenario(Layout.Parse("""
            .......
            .H.bbb.
            .H.bbb.
            .H.bbb.
            =======
            """));
        var pile = w.M.Grid.MachinesOf("pileScrap").Single();
        var cells = Struck(w, Weapons.Axe.Item, 0, 2, Right).Cells;
        Assert.Contains(pile.Cell, cells);
        Assert.Contains(w.M.Building.TargetAt(2 * 7 + 1)!.Value.Key, cells);
    }

    [Fact]
    public void OnAPlanetARayStrikesAStationAndPassesItToTheWallBehind()
    {
        var w = Ground("""
            ........
            .FFFF.#.
            .FFFF.#.
            .FFFF.#.
            .FFFF.#.
            ########
            """, """
            ........
            ......s.
            ......s.
            ......s.
            ......s.
            ssssssss
            """);
        var furnace = w.M.Grid.MachinesOf("furnace").Single();
        var cells = Struck(w, Weapons.Drill.Item, 0, 2, Right).Cells;
        Assert.Contains(At(w, 6, 2), cells);
        Assert.Contains(furnace.Cell, cells);
    }
}
